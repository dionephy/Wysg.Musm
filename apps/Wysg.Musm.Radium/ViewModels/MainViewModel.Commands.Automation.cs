using System;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Wysg.Musm.Radium.ViewModels
{
    /// <summary>
    /// Partial: Automation module execution and helpers.
    /// Contains all the RunXxxxAsync methods for automation sequences.
    /// </summary>
    public partial class MainViewModel
    {
        private async Task RunNewStudyProcedureAsync() => await (_newStudyProc != null ? _newStudyProc.ExecuteAsync(this) : Task.Run(OnNewStudy));

        // New automation helpers (return Task for proper sequencing)
        private async Task AcquireStudyRemarkAsync()
        {
            const int maxRetries = 3;
            const int delayMs = 200;
            
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    Debug.WriteLine($"[Automation][GetStudyRemark] Attempt {attempt}/{maxRetries} - Starting acquisition");
                    
                    // Small delay before attempt to allow PACS UI to stabilize
                    if (attempt > 1)
                    {
                        Debug.WriteLine($"[Automation][GetStudyRemark] Waiting {delayMs * attempt}ms before retry...");
                        await Task.Delay(delayMs * attempt);
                    }
                    
                    var s = await _pacs.GetCurrentStudyRemarkAsync();
                    Debug.WriteLine($"[Automation][GetStudyRemark] Raw result from PACS: '{s}'");
                    Debug.WriteLine($"[Automation][GetStudyRemark] Result length: {s?.Length ?? 0} characters");
                    
                    // Success check: if we got non-empty content, accept it
                    if (!string.IsNullOrEmpty(s))
                    {
                        Debug.WriteLine($"[Automation][GetStudyRemark] SUCCESS on attempt {attempt}");
                        StudyRemark = s;
                        Debug.WriteLine($"[Automation][GetStudyRemark] Set StudyRemark property: '{StudyRemark}'");
                        
                        // NEW: Also fill ChiefComplaint with the same text
                        ChiefComplaint = s;
                        Debug.WriteLine($"[Automation][GetStudyRemark] Set ChiefComplaint property: '{ChiefComplaint}'");
                        
                        SetStatus($"Study remark captured ({s.Length} chars)");
                        return; // Success - exit retry loop
                    }
                    else
                    {
                        Debug.WriteLine($"[Automation][GetStudyRemark] Attempt {attempt} returned empty/null");
                        if (attempt < maxRetries)
                        {
                            Debug.WriteLine($"[Automation][GetStudyRemark] Will retry...");
                            continue; // Try again
                        }
                        else
                        {
                            Debug.WriteLine($"[Automation][GetStudyRemark] Max retries reached - accepting empty result");
                            StudyRemark = string.Empty;
                            ChiefComplaint = string.Empty; // Also clear ChiefComplaint
                            SetStatus("Study remark empty after retries");
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Automation][GetStudyRemark] Attempt {attempt} EXCEPTION: {ex.GetType().Name} - {ex.Message}");
                    Debug.WriteLine($"[Automation][GetStudyRemark] StackTrace: {ex.StackTrace}");
                    
                    if (attempt < maxRetries)
                    {
                        Debug.WriteLine($"[Automation][GetStudyRemark] Will retry after exception...");
                        continue; // Try again
                    }
                    else
                    {
                        Debug.WriteLine($"[Automation][GetStudyRemark] Max retries reached after exception");
                        SetStatus("Study remark capture failed after retries", true);
                        StudyRemark = string.Empty; // Set empty to prevent stale data
                        ChiefComplaint = string.Empty; // Also clear ChiefComplaint
                        return;
                    }
                }
            }
        }
        
        private async Task AcquirePatientRemarkAsync()
        {
            const int maxRetries = 3;
            const int delayMs = 200;
            
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    Debug.WriteLine($"[Automation][GetPatientRemark] Attempt {attempt}/{maxRetries} - Starting acquisition");
                    
                    // Small delay before attempt to allow PACS UI to stabilize
                    if (attempt > 1)
                    {
                        Debug.WriteLine($"[Automation][GetPatientRemark] Waiting {delayMs * attempt}ms before retry...");
                        await Task.Delay(delayMs * attempt);
                    }
                    
                    var s = await _pacs.GetCurrentPatientRemarkAsync();
                    Debug.WriteLine($"[Automation][GetPatientRemark] Raw result from PACS: '{s}'");
                    Debug.WriteLine($"[Automation][GetPatientRemark] Result length: {s?.Length ?? 0} characters");
                    
                    // Success check: if we got non-empty content, accept it
                    if (!string.IsNullOrEmpty(s))
                    {
                        Debug.WriteLine($"[Automation][GetPatientRemark] SUCCESS on attempt {attempt}");
                        
                        // Remove duplicate lines based on text between < and >
                        var originalLength = s.Length;
                        s = RemoveDuplicateLinesInPatientRemark(s);
                        Debug.WriteLine($"[Automation][GetPatientRemark] After deduplication: length {originalLength} -> {s.Length}");
                        
                        PatientRemark = s;
                        Debug.WriteLine($"[Automation][GetPatientRemark] Set PatientRemark property: '{PatientRemark}'");
                        SetStatus($"Patient remark captured ({s.Length} chars)");
                        return; // Success - exit retry loop
                    }
                    else
                    {
                        Debug.WriteLine($"[Automation][GetPatientRemark] Attempt {attempt} returned empty/null");
                        if (attempt < maxRetries)
                        {
                            Debug.WriteLine($"[Automation][GetPatientRemark] Will retry...");
                            continue; // Try again
                        }
                        else
                        {
                            Debug.WriteLine($"[Automation][GetPatientRemark] Max retries reached - accepting empty result");
                            PatientRemark = string.Empty;
                            SetStatus("Patient remark empty after retries");
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Automation][GetPatientRemark] Attempt {attempt} EXCEPTION: {ex.GetType().Name} - {ex.Message}");
                    Debug.WriteLine($"[Automation][GetPatientRemark] StackTrace: {ex.StackTrace}");
                    
                    if (attempt < maxRetries)
                    {
                        Debug.WriteLine($"[Automation][GetPatientRemark] Will retry after exception...");
                        continue; // Try again
                    }
                    else
                    {
                        Debug.WriteLine($"[Automation][GetPatientRemark] Max retries reached after exception");
                        SetStatus("Patient remark capture failed after retries", true);
                        PatientRemark = string.Empty; // Set empty to prevent stale data
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Removes duplicate lines from patient remark text.
        /// Lines are considered duplicate if the text wrapped within angle brackets (&lt; and &gt;) is the same.
        /// </summary>
        private static string RemoveDuplicateLinesInPatientRemark(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return input;

            var lines = input.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var result = new System.Collections.Generic.List<string>();

            foreach (var line in lines)
            {
                // Extract text between < and >
                var key = ExtractAngleBracketContent(line);
                
                // If we haven't seen this key before, or if line has no angle bracket content, keep it
                if (string.IsNullOrEmpty(key) || seen.Add(key))
                {
                    result.Add(line);
                }
            }

            return string.Join("\n", result);
        }

        /// <summary>
        /// Extracts the text content wrapped within angle brackets (&lt; and &gt;).
        /// Returns empty string if no angle bracket content is found.
        /// </summary>
        private static string ExtractAngleBracketContent(string line)
        {
            if (string.IsNullOrEmpty(line)) return string.Empty;

            int startIndex = line.IndexOf('<');
            int endIndex = line.IndexOf('>');

            // Both brackets must exist and be in correct order
            if (startIndex >= 0 && endIndex > startIndex)
            {
                return line.Substring(startIndex + 1, endIndex - startIndex - 1).Trim();
            }

            return string.Empty;
        }

        // Execute modules sequentially to preserve user-defined order
        private async Task RunModulesSequentially(string[] modules, string sequenceName = "automation")
        {
            foreach (var m in modules)
            {
                try
                {
                    if (string.Equals(m, "NewStudy", StringComparison.OrdinalIgnoreCase)) { await RunNewStudyProcedureAsync(); }
                    else if (string.Equals(m, "LockStudy", StringComparison.OrdinalIgnoreCase) && _lockStudyProc != null) { await _lockStudyProc.ExecuteAsync(this); }
                    else if (string.Equals(m, "UnlockStudy", StringComparison.OrdinalIgnoreCase)) 
                    { 
                        PatientLocked = false; 
                        StudyOpened = false; 
                        ProofreadMode = false;
                        Reportified = false;
                        SetStatus("Study unlocked (all toggles off)"); 
                    }
                    else if (string.Equals(m, "GetStudyRemark", StringComparison.OrdinalIgnoreCase)) { await AcquireStudyRemarkAsync(); }
                    else if (string.Equals(m, "GetPatientRemark", StringComparison.OrdinalIgnoreCase)) { await AcquirePatientRemarkAsync(); }
                    else if (string.Equals(m, "AddPreviousStudy", StringComparison.OrdinalIgnoreCase)) { await RunAddPreviousStudyModuleAsync(); }
                    else if (string.Equals(m, "AbortIfWorklistClosed", StringComparison.OrdinalIgnoreCase))
                    {
                        var isVisible = await _pacs.WorklistIsVisibleAsync();
                        if (string.Equals(isVisible, "false", StringComparison.OrdinalIgnoreCase))
                        {
                            SetStatus("Worklist closed - automation aborted", true);
                            return; // Abort the rest of the sequence
                        }
                        SetStatus("Worklist visible - continuing");
                    }
                    else if (string.Equals(m, "AbortIfPatientNumberNotMatch", StringComparison.OrdinalIgnoreCase))
                    {
                        var matchResult = await _pacs.PatientNumberMatchAsync();
                        if (string.Equals(matchResult, "false", StringComparison.OrdinalIgnoreCase))
                        {
                            // Get the actual patient numbers for display
                            var pacsPatientNumber = await _pacs.GetCurrentPatientNumberAsync();
                            var mainPatientNumber = PatientNumber;
                            Debug.WriteLine($"[Automation][AbortIfPatientNumberNotMatch] MISMATCH - PACS: '{pacsPatientNumber}' Main: '{mainPatientNumber}'");
                            
                            // Show confirmation MessageBox on UI thread
                            bool forceContinue = false;
                            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                            {
                                var result = System.Windows.MessageBox.Show(
                                    $"Patient number mismatch detected!\n\n" +
                                    $"PACS: {pacsPatientNumber}\n" +
                                    $"Radium: {mainPatientNumber}\n\n" +
                                    $"Do you want to force continue the procedure?",
                                    "Patient Number Mismatch",
                                    System.Windows.MessageBoxButton.YesNo,
                                    System.Windows.MessageBoxImage.Warning);
                                
                                forceContinue = (result == System.Windows.MessageBoxResult.Yes);
                            });
                            
                            if (!forceContinue)
                            {
                                SetStatus($"Patient number mismatch - automation aborted by user (PACS: '{pacsPatientNumber}', Main: '{mainPatientNumber}')", true);
                                Debug.WriteLine($"[Automation][AbortIfPatientNumberNotMatch] User chose to ABORT");
                                return; // Abort the rest of the sequence
                            }
                            else
                            {
                                SetStatus($"Patient number mismatch - user forced continue (PACS: '{pacsPatientNumber}', Main: '{mainPatientNumber}')");
                                Debug.WriteLine($"[Automation][AbortIfPatientNumberNotMatch] User chose to FORCE CONTINUE");
                            }
                        }
                        else
                        {
                            SetStatus("Patient number match - continuing");
                        }
                    }
                    else if (string.Equals(m, "AbortIfStudyDateTimeNotMatch", StringComparison.OrdinalIgnoreCase))
                    {
                        var matchResult = await _pacs.StudyDateTimeMatchAsync();
                        if (string.Equals(matchResult, "false", StringComparison.OrdinalIgnoreCase))
                        {
                            // Get the actual study datetimes for display
                            var pacsStudyDateTime = await _pacs.GetCurrentStudyDateTimeAsync();
                            var mainStudyDateTime = StudyDateTime;
                            Debug.WriteLine($"[Automation][AbortIfStudyDateTimeNotMatch] MISMATCH - PACS: '{pacsStudyDateTime}' Main: '{mainStudyDateTime}'");
                            
                            // Show confirmation MessageBox on UI thread
                            bool forceContinue = false;
                            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                            {
                                var result = System.Windows.MessageBox.Show(
                                    $"Study date/time mismatch detected!\n\n" +
                                    $"PACS: {pacsStudyDateTime}\n" +
                                    $"Radium: {mainStudyDateTime}\n\n" +
                                    $"Do you want to force continue the procedure?",
                                    "Study DateTime Mismatch",
                                    System.Windows.MessageBoxButton.YesNo,
                                    System.Windows.MessageBoxImage.Warning);
                                
                                forceContinue = (result == System.Windows.MessageBoxResult.Yes);
                            });
                            
                            if (!forceContinue)
                            {
                                SetStatus($"Study date/time mismatch - automation aborted by user (PACS: '{pacsStudyDateTime}', Main: '{mainStudyDateTime}')", true);
                                Debug.WriteLine($"[Automation][AbortIfStudyDateTimeNotMatch] User chose to ABORT");
                                return; // Abort the rest of the sequence
                            }
                            else
                            {
                                SetStatus($"Study date/time mismatch - user forced continue (PACS: '{pacsStudyDateTime}', Main: '{mainStudyDateTime}')");
                                Debug.WriteLine($"[Automation][AbortIfStudyDateTimeNotMatch] User chose to FORCE CONTINUE");
                            }
                        }
                        else
                        {
                            SetStatus("Study date/time match - continuing");
                        }
                    }
                    else if (string.Equals(m, "GetUntilReportDateTime", StringComparison.OrdinalIgnoreCase))
                    {
                        await RunGetUntilReportDateTimeAsync();
                    }
                    else if (string.Equals(m, "GetReportedReport", StringComparison.OrdinalIgnoreCase))
                    {
                        await RunGetReportedReportAsync();
                    }
                    else if (string.Equals(m, "OpenStudy", StringComparison.OrdinalIgnoreCase)) { await RunOpenStudyAsync(); }
                    else if (string.Equals(m, "MouseClick1", StringComparison.OrdinalIgnoreCase)) { await _pacs.CustomMouseClick1Async(); }
                    else if (string.Equals(m, "MouseClick2", StringComparison.OrdinalIgnoreCase)) { await _pacs.CustomMouseClick2Async(); }
                    else if (string.Equals(m, "TestInvoke", StringComparison.OrdinalIgnoreCase)) { await _pacs.InvokeTestAsync(); }
                    else if (string.Equals(m, "ShowTestMessage", StringComparison.OrdinalIgnoreCase)) { System.Windows.MessageBox.Show("Test", "Test", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information); }
                    else if (string.Equals(m, "SetCurrentInMainScreen", StringComparison.OrdinalIgnoreCase)) { await RunSetCurrentInMainScreenAsync(); }
                    else if (string.Equals(m, "OpenWorklist", StringComparison.OrdinalIgnoreCase)) { await RunOpenWorklistAsync(); }
                    else if (string.Equals(m, "ResultsListSetFocus", StringComparison.OrdinalIgnoreCase)) { await RunResultsListSetFocusAsync(); }
                    else if (string.Equals(m, "SendReport", StringComparison.OrdinalIgnoreCase)) 
                    { 
                        await RunSendReportModuleWithRetryAsync(); 
                    }
                    else if (string.Equals(m, "Reportify", StringComparison.OrdinalIgnoreCase)) 
                    {
                        ProofreadMode = true;
                        Debug.WriteLine("[Automation] Reportify module - START");
                        Debug.WriteLine($"[Automation] Reportify module - Current Reportified value BEFORE: {Reportified}");
                        Reportified = true;
                        Debug.WriteLine($"[Automation] Reportify module - Current Reportified value AFTER: {Reportified}");
                        SetStatus("Reportified toggled ON");
                        Debug.WriteLine("[Automation] Reportify module - COMPLETED");
                    }
                    else if (string.Equals(m, "Delay", StringComparison.OrdinalIgnoreCase))
                    {
                        Debug.WriteLine("[Automation] Delay module - waiting 300ms");
                        await Task.Delay(300);
                        Debug.WriteLine("[Automation] Delay module - completed");
                    }
                    else if (string.Equals(m, "SaveCurrentStudyToDB", StringComparison.OrdinalIgnoreCase))
                    {
                        Debug.WriteLine("[Automation] SaveCurrentStudyToDB module - START");
                        await RunSaveCurrentStudyToDBAsync();
                        Debug.WriteLine("[Automation] SaveCurrentStudyToDB module - COMPLETED");
                    }
                    else if (string.Equals(m, "SavePreviousStudyToDB", StringComparison.OrdinalIgnoreCase))
                    {
                        Debug.WriteLine("[Automation] SavePreviousStudyToDB module - START");
                        await RunSavePreviousStudyToDBAsync();
                        Debug.WriteLine("[Automation] SavePreviousStudyToDB module - COMPLETED");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("[Automation] Module '" + m + "' error: " + ex.Message);
                    SetStatus($"Module '{m}' failed - procedure aborted", true);
                    return; // ABORT entire sequence on any exception (including GetUntilReportDateTime failure)
                }
            }
            
            // NEW: Append green completion message after all modules succeed
            SetStatus($"? {sequenceName} completed successfully", isError: false);
        }

        private async Task RunOpenStudyAsync()
        {
            try
            {
                await _pacs.InvokeOpenStudyAsync();
                StudyOpened = true;
                SetStatus("Open study invoked");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[Automation] OpenStudy error: " + ex.Message);
                SetStatus("Open study failed", true);
            }
        }

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

        private async Task RunSetCurrentInMainScreenAsync()
        {
            try
            {                
                await _pacs.SetPreviousStudyInSubScreenAsync();
                await _pacs.SetCurrentStudyInMainScreenAsync();
                SetStatus("Screen layout set: current study in main, previous study in sub");
                
                // NEW: Request focus on Study Remark textbox in top grid after screen layout is complete
                // Small delay to allow PACS UI to settle before focusing our textbox
                await Task.Delay(150);
                RequestFocusStudyRemark?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[Automation] SetCurrentInMainScreen error: " + ex.Message);
                SetStatus("Screen layout failed", true);
            }
        }

        private async Task RunOpenWorklistAsync()
        {
            try
            {
                await _pacs.InvokeOpenWorklistAsync();
                SetStatus("Worklist opened");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[Automation] OpenWorklist error: " + ex.Message);
                SetStatus("Open worklist failed", true);
            }
        }

        private async Task RunResultsListSetFocusAsync()
        {
            try
            {
                Debug.WriteLine("[Automation] ResultsListSetFocus starting");
                
                // Execute the PACS method (which contains GetSelectedElement + ClickElementAndStay)
                await _pacs.SetFocusSearchResultsListAsync();
                
                // Add small delay to allow UI to respond to click (timing fix for automation vs manual execution)
                await Task.Delay(150);
                
                Debug.WriteLine("[Automation] ResultsListSetFocus completed successfully");
                SetStatus("Search results list focused");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Automation] ResultsListSetFocus error: {ex.Message}");
                Debug.WriteLine($"[Automation] ResultsListSetFocus stack: {ex.StackTrace}");
                SetStatus("Set focus results list failed", true);
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
                System.Diagnostics.Debug.WriteLine("[Automation] SendReport error: " + ex.Message);
                SetStatus("Send report failed", true);
            }
        }

        /// <summary>
        /// SendReport module with retry logic:
        /// 1. Run SendReport custom procedure
        /// 2. If result is "true", run InvokeSendReport and succeed
        /// 3. If result is "false", show "Send failed. Retry?" messagebox
        /// 4. If user clicks OK, run ClearReport custom procedure
        /// 5. If ClearReport returns "true", go back to step 1
        /// 6. If ClearReport returns "false", show "Clear Report failed. Retry?" messagebox
        /// 7. If user clicks OK, go back to step 4
        /// 8. If user clicks Cancel, abort procedure
        /// </summary>
        private async Task RunSendReportModuleWithRetryAsync()
        {
            while (true)  // Main retry loop for SendReport
            {
                // Step 1: Run SendReport custom procedure
                Debug.WriteLine("[SendReportModule] Step 1: Running SendReport custom procedure");
                SetStatus("Sending report to PACS...");
                
                string? sendReportResult = null;
                try
                {
                    sendReportResult = await Services.ProcedureExecutor.ExecuteAsync("SendReport");
                    Debug.WriteLine($"[SendReportModule] SendReport result: '{sendReportResult}'");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[SendReportModule] SendReport EXCEPTION: {ex.Message}");
                    sendReportResult = "false";  // Treat exception as failure
                }

                // Step 2: Check if SendReport succeeded
                if (string.Equals(sendReportResult, "true", StringComparison.OrdinalIgnoreCase))
                {
                    // Success - run InvokeSendReport and end
                    Debug.WriteLine("[SendReportModule] Step 2: SendReport succeeded, running InvokeSendReport");
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
                    Debug.WriteLine("[SendReportModule] Step 3: SendReport failed, showing retry dialog");
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

        private async Task RunSaveCurrentStudyToDBAsync()
        {
            try
            {
                Debug.WriteLine("[Automation][SaveCurrentStudyToDB] Starting save to database");
                
                // Ensure study repository is available
                if (_studyRepo == null)
                {
                    Debug.WriteLine("[Automation][SaveCurrentStudyToDB] FAILED - study repository is null");
                    SetStatus("Save to DB failed - repository unavailable", true);
                    return;
                }
                
                // Get current study ID from patient/study metadata
                if (string.IsNullOrWhiteSpace(PatientNumber) || string.IsNullOrWhiteSpace(StudyName) || string.IsNullOrWhiteSpace(StudyDateTime))
                {
                    Debug.WriteLine("[Automation][SaveCurrentStudyToDB] FAILED - missing patient/study metadata");
                    Debug.WriteLine($"[Automation][SaveCurrentStudyToDB] PatientNumber='{PatientNumber}', StudyName='{StudyName}', StudyDateTime='{StudyDateTime}'");
                    SetStatus("Save to DB failed - missing study context", true);
                    return;
                }
                
                // Ensure CurrentReportDateTime was set by GetUntilReportDateTime module
                if (!CurrentReportDateTime.HasValue)
                {
                    Debug.WriteLine("[Automation][SaveCurrentStudyToDB] FAILED - CurrentReportDateTime is null");
                    SetStatus("Save to DB failed - report datetime not set (run GetUntilReportDateTime first)", true);
                    return;
                }
                
                // Parse study datetime
                if (!DateTime.TryParse(StudyDateTime, out var studyDt))
                {
                    Debug.WriteLine($"[Automation][SaveCurrentStudyToDB] FAILED - invalid StudyDateTime format: '{StudyDateTime}'");
                    SetStatus("Save to DB failed - invalid study datetime", true);
                    return;
                }
                
                // Ensure study record exists in database
                Debug.WriteLine("[Automation][SaveCurrentStudyToDB] Ensuring study exists in database");
                var studyId = await _studyRepo.EnsureStudyAsync(PatientNumber, PatientName, PatientSex, null, StudyName, studyDt);
                
                if (!studyId.HasValue)
                {
                    Debug.WriteLine("[Automation][SaveCurrentStudyToDB] FAILED - could not create/retrieve study record");
                    SetStatus("Save to DB failed - study record error", true);
                    return;
                }
                
                Debug.WriteLine($"[Automation][SaveCurrentStudyToDB] Study ID: {studyId.Value}");
                
                // Get current report JSON (which includes all fields)
                var reportJson = CurrentReportJson ?? "{}";
                Debug.WriteLine($"[Automation][SaveCurrentStudyToDB] Report JSON length: {reportJson.Length} characters");
                
                // Save report with is_mine=true and the captured CurrentReportDateTime
                var reportId = await _studyRepo.UpsertPartialReportAsync(
                    studyId: studyId.Value, 
                    reportDateTime: CurrentReportDateTime.Value, 
                    reportJson: reportJson, 
                    isMine: true
                );
                
                if (reportId.HasValue)
                {
                    Debug.WriteLine($"[Automation][SaveCurrentStudyToDB] SUCCESS - Report ID: {reportId.Value}");
                    SetStatus($"Current study saved to DB (report ID: {reportId.Value})");
                }
                else
                {
                    Debug.WriteLine("[Automation][SaveCurrentStudyToDB] FAILED - upsert returned null");
                    SetStatus("Save to DB failed - upsert error", true);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Automation][SaveCurrentStudyToDB] EXCEPTION: {ex.GetType().Name} - {ex.Message}");
                Debug.WriteLine($"[Automation][SaveCurrentStudyToDB] StackTrace: {ex.StackTrace}");
                SetStatus("Save to DB error", true);
            }
        }

        private async Task RunSavePreviousStudyToDBAsync()
        {
            try
            {
                Debug.WriteLine("[Automation][SavePreviousStudyToDB] Starting save to database");
                
                // Ensure study repository is available
                if (_studyRepo == null)
                {
                    Debug.WriteLine("[Automation][SavePreviousStudyToDB] FAILED - study repository is null");
                    SetStatus("Save previous to DB failed - repository unavailable", true);
                    return;
                }
                
                // Get selected previous study tab
                var prevTab = SelectedPreviousStudy;
                if (prevTab == null)
                {
                    Debug.WriteLine("[Automation][SavePreviousStudyToDB] FAILED - no previous study selected");
                    SetStatus("Save previous to DB failed - no previous study selected", true);
                    return;
                }
                
                // Get the visible JSON from the selected previous study
                var reportJson = PreviousReportJson ?? "{}";
                Debug.WriteLine($"[Automation][SavePreviousStudyToDB] Previous report JSON length: {reportJson.Length} characters");
                
                // We need to identify the database record being updated
                // The SelectedReport contains the report datetime which is the key
                var selectedReport = prevTab.SelectedReport;
                if (selectedReport == null)
                {
                    Debug.WriteLine("[Automation][SavePreviousStudyToDB] FAILED - no report selected in previous study tab");
                    SetStatus("Save previous to DB failed - no report selected", true);
                    return;
                }
                
                // Get study datetime and studyname from the tab
                var studyDt = prevTab.StudyDateTime;
                var studyName = selectedReport.Studyname;
                
                Debug.WriteLine($"[Automation][SavePreviousStudyToDB] Study: '{studyName}', DateTime: {studyDt:yyyy-MM-dd HH:mm:ss}");
                
                // Ensure study record exists in database (should already exist since it was loaded from DB)
                Debug.WriteLine("[Automation][SavePreviousStudyToDB] Ensuring study exists in database");
                var studyId = await _studyRepo.EnsureStudyAsync(PatientNumber, PatientName, PatientSex, null, studyName, studyDt);
                
                if (!studyId.HasValue)
                {
                    Debug.WriteLine("[Automation][SavePreviousStudyToDB] FAILED - could not retrieve study record");
                    SetStatus("Save previous to DB failed - study record error", true);
                    return;
                }
                
                Debug.WriteLine($"[Automation][SavePreviousStudyToDB] Study ID: {studyId.Value}");
                
                // Update report with the edited JSON
                // Use the report datetime from the selected report (maintains the existing report datetime key)
                var reportDateTime = selectedReport.ReportDateTime;
                Debug.WriteLine($"[Automation][SavePreviousStudyToDB] Report DateTime: {reportDateTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "(null)"}");
                
                var reportId = await _studyRepo.UpsertPartialReportAsync(
                    studyId: studyId.Value, 
                    reportDateTime: reportDateTime, 
                    reportJson: reportJson, 
                    isMine: false  // Keep is_mine as false for previous studies
                );
                
                if (reportId.HasValue)
                {
                    Debug.WriteLine($"[Automation][SavePreviousStudyToDB] SUCCESS - Report ID: {reportId.Value}");
                    SetStatus($"Previous study saved to DB (report ID: {reportId.Value})");
                }
                else
                {
                    Debug.WriteLine("[Automation][SavePreviousStudyToDB] FAILED - upsert returned null");
                    SetStatus("Save previous to DB failed - upsert error", true);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Automation][SavePreviousStudyToDB] EXCEPTION: {ex.GetType().Name} - {ex.Message}");
                Debug.WriteLine($"[Automation][SavePreviousStudyToDB] StackTrace: {ex.StackTrace}");
                SetStatus("Save previous to DB error", true);
            }
        }
    }
}
