using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Wysg.Musm.Radium.ViewModels
{
    /// <summary>
    /// Partial: Automation report operations.
    /// Contains methods for report acquisition, sending, and related workflows.
    /// </summary>
    public partial class MainViewModel
    {
        private async Task RunGetUntilReportDateTimeAsync()
        {
            const int maxRetries = 30;
            const int delayMs = 200;

            try
            {
                Debug.WriteLine("[Automation][GetUntilReportDateTime] Starting - max retries: 30");
                
                for (int attempt = 1; attempt <= maxRetries; attempt++)
                {
                    var result = await _pacs.GetSelectedReportDateTimeFromSearchResultsAsync();
                    Debug.WriteLine($"[Automation][GetUntilReportDateTime] Attempt {attempt}/{maxRetries}: '{result}'");
                    
                    // Check if result is in DateTime format
                    if (!string.IsNullOrWhiteSpace(result) && DateTime.TryParse(result, out var reportDateTime))
                    {
                        Debug.WriteLine($"[Automation][GetUntilReportDateTime] SUCCESS on attempt {attempt} - valid DateTime: '{result}'");
                        
                        // Save the report datetime to the property
                        CurrentReportDateTime = reportDateTime;
                        Debug.WriteLine($"[Automation][GetUntilReportDateTime] Saved to CurrentReportDateTime: {reportDateTime:yyyy-MM-dd HH:mm:ss}");
                        
                        SetStatus($"Report DateTime acquired: {result}");
                        return; // Success - valid DateTime format detected and saved
                    }
                    
                    // Not yet valid DateTime - wait and retry
                    if (attempt < maxRetries)
                    {
                        Debug.WriteLine($"[Automation][GetUntilReportDateTime] Invalid format, waiting {delayMs}ms before retry");
                        await Task.Delay(delayMs);
                    }
                }
                
                // Failed after all retries
                Debug.WriteLine("[Automation][GetUntilReportDateTime] FAILED - max retries reached without valid DateTime");
                SetStatus("Report DateTime acquisition failed - aborting procedure", true);
                throw new InvalidOperationException("GetUntilReportDateTime failed to acquire valid DateTime after 30 retries");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Automation][GetUntilReportDateTime] EXCEPTION: {ex.GetType().Name} - {ex.Message}");
                SetStatus("Report DateTime acquisition error - aborting", true);
                throw; // Re-throw to abort the procedure sequence
            }
        }

        private async Task RunGetReportedReportAsync()
        {
            try
            {
                Debug.WriteLine("[Automation][GetReportedReport] Starting acquisition");
                
                // Execute GetCurrentFindings and GetCurrentConclusion from PACS
                var findingsTask = _pacs.GetCurrentFindingsWaitAsync();
                var conclusionTask = _pacs.GetCurrentConclusionAsync();
                
                await Task.WhenAll(findingsTask, conclusionTask);
                
                var findings = findingsTask.Result ?? string.Empty;
                var conclusion = conclusionTask.Result ?? string.Empty;
                
                Debug.WriteLine($"[Automation][GetReportedReport] Findings length: {findings.Length} characters");
                Debug.WriteLine($"[Automation][GetReportedReport] Conclusion length: {conclusion.Length} characters");
                
                // NEW: Execute GetSelectedRadiologistFromSearchResultsList
                Debug.WriteLine("[Automation][GetReportedReport] Fetching radiologist from search results list...");
                var radiologist = await _pacs.GetSelectedRadiologistFromSearchResultsAsync();
                
                Debug.WriteLine($"[Automation][GetReportedReport] Radiologist: '{radiologist ?? "(null)"}'");
                Debug.WriteLine($"[Automation][GetReportedReport] Radiologist length: {radiologist?.Length ?? 0} characters");
                
                // CRITICAL FIX: Set reported report fields (header_and_findings, final_conclusion)
                // These are NOT bound to any UI editor - they preserve the original PACS report
                ReportedHeaderAndFindings = findings;
                ReportedFinalConclusion = conclusion;
                ReportRadiologist = radiologist ?? string.Empty;
                
                Debug.WriteLine("[Automation][GetReportedReport] Updated ReportedHeaderAndFindings, ReportedFinalConclusion, and ReportRadiologist properties");
                Debug.WriteLine("[Automation][GetReportedReport] CurrentReportJson should now contain header_and_findings, final_conclusion, and report_radiologist fields");
                
                SetStatus($"Reported report acquired: {findings.Length + conclusion.Length} total characters, radiologist: {(string.IsNullOrWhiteSpace(radiologist) ? "(none)" : radiologist)}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Automation][GetReportedReport] EXCEPTION: {ex.GetType().Name} - {ex.Message}");
                Debug.WriteLine($"[Automation][GetReportedReport] StackTrace: {ex.StackTrace}");
                SetStatus("Get reported report acquisition error", true);
                // Do not throw - allow sequence to continue
            }
        }

        private async Task RunSendReportAsync()
        {
            try
            {
                // CRITICAL FIX: Use RAW (unreportified) values for sending to PACS
                // We want to send the original text, not the formatted/reportified version
                var findings = RawFindingsText;
                var conclusion = RawConclusionText;
                
                Debug.WriteLine($"[Automation][SendReport] Sending FINDINGS (raw, length={findings.Length})");
                Debug.WriteLine($"[Automation][SendReport] Sending CONCLUSION (raw, length={conclusion.Length})");
                
                await _pacs.SendReportAsync(findings, conclusion);
                SetStatus("Report sent");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[Automation] SendReport error: " + ex.Message);
                SetStatus("Send report failed", true);
            }
        }

        /// <summary>
        /// SendReport module with retry logic:
        /// 1. Run SendReport custom procedure (or SendReportWithoutHeader if modality is in exclusion list)
        /// 2. If result is "true", run InvokeSendReport and succeed
        /// 3. If result is "false", show "Send failed. Retry?" messagebox
        /// 4. If user clicks OK, run ClearReport custom procedure
        /// 5. If ClearReport returns "true", go back to step 1
        /// 6. If ClearReport returns "false", show "Clear Report failed. Retry?" messagebox
        /// 7. If user clicks OK, go back to step 4
        /// 8. If user clicks Cancel, abort procedure
        /// 
        /// NEW: If current study modality is in ModalitiesNoHeaderUpdate list, uses SendReportWithoutHeader instead of SendReport
        /// </summary>
        private async Task RunSendReportModuleWithRetryAsync()
        {
            // Determine which send procedure to use based on modality
            string sendProcedureName = await DetermineSendReportProcedureAsync();
            Debug.WriteLine($"[SendReportModule] Using procedure: {sendProcedureName}");
            
            while (true)  // Main retry loop for SendReport
            {
                // Step 1: Run SendReport (or SendReportWithoutHeader) custom procedure
                Debug.WriteLine($"[SendReportModule] Step 1: Running {sendProcedureName} custom procedure");
                SetStatus("Sending report to PACS...");
                
                string? sendReportResult = null;
                try
                {
                    sendReportResult = await Services.ProcedureExecutor.ExecuteAsync(sendProcedureName);
                    Debug.WriteLine($"[SendReportModule] {sendProcedureName} result: '{sendReportResult}'");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[SendReportModule] {sendProcedureName} EXCEPTION: {ex.Message}");
                    sendReportResult = "false";  // Treat exception as failure
                }

                // Step 2: Check if SendReport succeeded
                if (string.Equals(sendReportResult, "true", StringComparison.OrdinalIgnoreCase))
                {
                    // Success - run InvokeSendReport and end
                    Debug.WriteLine($"[SendReportModule] Step 2: {sendProcedureName} succeeded, running InvokeSendReport");
                    SetStatus("Report sent successfully, finalizing...");
                    
                    try
                    {
                        await Services.ProcedureExecutor.ExecuteAsync("InvokeSendReport");
                        Debug.WriteLine("[SendReportModule] InvokeSendReport completed");
                        SetStatus("? Report sent and finalized successfully");
                        return;  // Success - exit module
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[SendReportModule] InvokeSendReport EXCEPTION: {ex.Message}");
                        SetStatus("Report sent but finalization failed", true);
                        return;  // Exit even if finalize fails
                    }
                }
                else
                {
                    // Step 3: SendReport failed - show retry dialog
                    Debug.WriteLine($"[SendReportModule] Step 3: {sendProcedureName} failed, showing retry dialog");
                    SetStatus("Report send failed", true);
                    
                    bool retrySend = false;
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        var result = System.Windows.MessageBox.Show(
                            "Send report failed.\n\nDo you want to clear the report and retry?",
                            "Send Failed",
                            System.Windows.MessageBoxButton.OKCancel,
                            System.Windows.MessageBoxImage.Warning);
                        
                        retrySend = (result == System.Windows.MessageBoxResult.OK);
                    });
                    
                    if (!retrySend)
                    {
                        // User cancelled - abort
                        Debug.WriteLine("[SendReportModule] User cancelled send retry - aborting");
                        SetStatus("Send report aborted by user", true);
                        return;  // Abort
                    }
                    
                    // Step 4: User wants to retry - attempt ClearReport
                    while (true)  // Inner retry loop for ClearReport
                    {
                        Debug.WriteLine("[SendReportModule] Step 4: Running ClearReport custom procedure");
                        SetStatus("Clearing report for retry...");
                        
                        string? clearReportResult = null;
                        try
                        {
                            clearReportResult = await Services.ProcedureExecutor.ExecuteAsync("ClearReport");
                            Debug.WriteLine($"[SendReportModule] ClearReport result: '{clearReportResult}'");
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[SendReportModule] ClearReport EXCEPTION: {ex.Message}");
                            clearReportResult = "false";  // Treat exception as failure
                        }
                        
                        // Step 5: Check if ClearReport succeeded
                        if (string.Equals(clearReportResult, "true", StringComparison.OrdinalIgnoreCase))
                        {
                            // ClearReport succeeded - go back to SendReport (main loop)
                            Debug.WriteLine("[SendReportModule] Step 5: ClearReport succeeded, retrying SendReport");
                            SetStatus("Report cleared, retrying send...");
                            break;  // Exit inner loop, continue main loop
                        }
                        else
                        {
                            // Step 6: ClearReport failed - show retry dialog
                            Debug.WriteLine("[SendReportModule] Step 6: ClearReport failed, showing retry dialog");
                            SetStatus("Clear report failed", true);
                            
                            bool retryClear = false;
                            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                            {
                                var result = System.Windows.MessageBox.Show(
                                    "Clear report failed.\n\nDo you want to retry clearing?",
                                    "Clear Failed",
                                    System.Windows.MessageBoxButton.OKCancel,
                                    System.Windows.MessageBoxImage.Warning);
                        
                                retryClear = (result == System.Windows.MessageBoxResult.OK);
                            });
                            
                            if (!retryClear)
                            {
                                // User cancelled - abort entire procedure
                                Debug.WriteLine("[SendReportModule] User cancelled clear retry - aborting");
                                SetStatus("Send report aborted (clear failed)", true);
                                return;  // Abort entire module
                            }
                            
                            // User wants to retry clear - continue inner loop
                            Debug.WriteLine("[SendReportModule] User chose to retry ClearReport");
                        }
                    }  // End inner ClearReport retry loop
                }
            }  // End main SendReport retry loop
        }

        /// <summary>
        /// Determines which send report procedure to use based on current study modality and settings.
        /// Returns "SendReportWithoutHeader" if modality is in ModalitiesNoHeaderUpdate list, otherwise "SendReport".
        /// </summary>
        private async Task<string> DetermineSendReportProcedureAsync()
        {
            try
            {
                var (hasHeader, modality) = await EvaluateModalityHeaderSettingAsync();
                if (!hasHeader)
                {
                    var modalityLabel = string.IsNullOrWhiteSpace(modality) ? "(unknown)" : modality;
                    Debug.WriteLine($"[DetermineSendReportProcedure] Using SendReportWithoutHeader - modality '{modalityLabel}' is in exclusion list");
                    SetStatus($"Using send without header (modality '{modalityLabel}' configured for header-less send)");
                    return "SendReportWithoutHeader";
                }

                Debug.WriteLine("[DetermineSendReportProcedure] Using standard SendReport procedure");
                return "SendReport";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DetermineSendReportProcedure] ERROR: {ex.Message}");
                Debug.WriteLine($"[DetermineSendReportProcedure] Falling back to standard SendReport procedure");
                return "SendReport"; // Fallback to standard procedure on error
            }
        }

        private HashSet<string> GetModalitiesWithoutHeader()
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var raw = _localSettings?.ModalitiesNoHeaderUpdate ?? string.Empty;
            if (string.IsNullOrWhiteSpace(raw))
            {
                return set;
            }

            foreach (var token in raw.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var trimmed = token.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                {
                    set.Add(trimmed);
                }
            }

            return set;
        }

        private async Task<(bool HasHeader, string? Modality)> EvaluateModalityHeaderSettingAsync()
        {
            string? currentModality = null;

            try
            {
                if (!string.IsNullOrWhiteSpace(StudyName))
                {
                    currentModality = await ExtractModalityAsync(StudyName);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ModalityHeaderEval] Failed to extract modality: {ex.Message}");
            }

            var excluded = GetModalitiesWithoutHeader();
            var hasHeader = true;

            if (!string.IsNullOrWhiteSpace(currentModality) && excluded.Count > 0)
            {
                hasHeader = !excluded.Contains(currentModality);
            }

            Debug.WriteLine($"[ModalityHeaderEval] modality='{currentModality ?? "(unknown)"}', excludedCount={excluded.Count}, hasHeader={hasHeader}");
            return (hasHeader, currentModality);
        }

        private async Task<bool> CurrentModalityHasHeaderAsync()
        {
            var (hasHeader, _) = await EvaluateModalityHeaderSettingAsync();
            return hasHeader;
        }
    }
}
