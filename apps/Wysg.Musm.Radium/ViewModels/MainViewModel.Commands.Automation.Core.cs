using System;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Wysg.Musm.Radium.ViewModels
{
    /// <summary>
    /// Partial: Core automation module execution.
    /// Contains the main module execution orchestration methods.
    /// </summary>
    public partial class MainViewModel
    {
        private async Task RunNewStudyProcedureAsync() => await (_newStudyProc != null ? _newStudyProc.ExecuteAsync(this) : Task.Run(OnNewStudy));

        // Execute modules sequentially to preserve user-defined order
        private async Task RunModulesSequentially(string[] modules, string sequenceName = "automation")
        {
            // Generate a new session ID for this automation run
            // This allows element caching within the session (fast) while clearing between sessions (fresh data)
            var sessionId = $"{sequenceName}_{DateTime.Now.Ticks}";
            Services.ProcedureExecutor.SetSessionId(sessionId);
            Debug.WriteLine($"[Automation] Starting sequence '{sequenceName}' with session ID: {sessionId}");
            
            // Stack to track if-block state: (startIndex, conditionMet)
            var ifStack = new Stack<(int startIndex, bool conditionMet, bool isNegated)>();
            bool skipExecution = false;
            
            for (int i = 0; i < modules.Length; i++)
            {
                var m = modules[i];
                
                try
                {
                    // Handle built-in "End if" module (must be checked first, before skipExecution)
                    if (string.Equals(m, "End if", StringComparison.OrdinalIgnoreCase))
                    {
                        if (ifStack.Count == 0)
                        {
                            Debug.WriteLine($"[Automation] WARNING: 'End if' without matching 'If' at position {i}");
                            continue;
                        }
                        
                        ifStack.Pop();
                        skipExecution = ifStack.Any(entry => !entry.conditionMet);
                        Debug.WriteLine($"[Automation] End if - resuming execution (skipExecution={skipExecution})");
                        continue;
                    }
                    
                    // Check if this is a custom module (If, If not, AbortIf, Set, Run)
                    var customStore = Wysg.Musm.Radium.Models.CustomModuleStore.Load();
                    var customModule = customStore.GetModule(m);

                    if (customModule != null)
                    {
                        // Handle If and If not modules (must be checked before skipExecution)
                        if (customModule.Type == Wysg.Musm.Radium.Models.CustomModuleType.If || 
                            customModule.Type == Wysg.Musm.Radium.Models.CustomModuleType.IfNot)
                        {
                            // Execute the procedure to get the condition result
                            var result = await Services.ProcedureExecutor.ExecuteAsync(customModule.ProcedureName);
                            
                            // Evaluate condition (true if result is non-empty and not "false")
                            bool conditionValue = !string.IsNullOrWhiteSpace(result) && 
                                                  !string.Equals(result, "false", StringComparison.OrdinalIgnoreCase);
                            
                            // Apply negation for If not
                            bool conditionMet = customModule.Type == Wysg.Musm.Radium.Models.CustomModuleType.IfNot 
                                ? !conditionValue 
                                : conditionValue;
                            
                            // Push to stack
                            ifStack.Push((i, conditionMet, customModule.Type == Wysg.Musm.Radium.Models.CustomModuleType.IfNot));
                            
                            // Update skip flag (skip if any parent condition is false)
                            skipExecution = !conditionMet || ifStack.Any(entry => !entry.conditionMet);
                            
                            Debug.WriteLine($"[Automation] {customModule.Name}: condition={conditionValue}, negated={customModule.Type == Wysg.Musm.Radium.Models.CustomModuleType.IfNot}, conditionMet={conditionMet}, skipExecution={skipExecution}");
                            SetStatus($"{customModule.Name}: {(conditionMet ? "condition met" : "condition not met")}");
                            continue;
                        }
                        
                        // If we're inside a false if-block, skip execution of custom modules
                        if (skipExecution)
                        {
                            Debug.WriteLine($"[Automation] Skipping custom module '{m}' (inside false if-block)");
                            continue;
                        }
                        
                        // Execute other custom module types (AbortIf, Set, Run)
                        await RunCustomModuleAsync(customModule);
                        continue;
                    }

                    // If we're inside a false if-block, skip standard modules (including Abort)
                    if (skipExecution)
                    {
                        Debug.WriteLine($"[Automation] Skipping module '{m}' (inside false if-block)");
                        continue;
                    }

                    // Handle built-in "Abort" module (after skipExecution check)
                    if (string.Equals(m, "Abort", StringComparison.OrdinalIgnoreCase))
                    {
                        SetStatus("Automation aborted by Abort module", true);
                        return; // Immediately abort the entire sequence
                    }

                    // Standard modules...
                    if (string.Equals(m, "NewStudy", StringComparison.OrdinalIgnoreCase) || 
                        string.Equals(m, "NewStudy(Obsolete)", StringComparison.OrdinalIgnoreCase)) 
                    { 
                        await RunNewStudyProcedureAsync(); 
                    }
                    else if (string.Equals(m, "LockStudy", StringComparison.OrdinalIgnoreCase) && _lockStudyProc != null) { await _lockStudyProc.ExecuteAsync(this); }
                    else if (string.Equals(m, "UnlockStudy", StringComparison.OrdinalIgnoreCase)) 
                    { 
                        PatientLocked = false; 
                        StudyOpened = false; 
                        SetStatus("Study unlocked (patient/study toggles off)"); 
                    }
                    else if (string.Equals(m, "SetCurrentTogglesOff", StringComparison.OrdinalIgnoreCase)) 
                    { 
                        ProofreadMode = false;
                        Reportified = false;
                        SetStatus("Toggles off (proofread/reportified off)"); 
                    }
                    else if (string.Equals(m, "AutofillCurrentHeader", StringComparison.OrdinalIgnoreCase))
                    {
                        await AutofillCurrentHeaderAsync();
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
                    else if (string.Equals(m, "ClearPreviousFields", StringComparison.OrdinalIgnoreCase) && _clearPreviousFieldsProc != null)
                    {
                        await _clearPreviousFieldsProc.ExecuteAsync(this);
                    }
                    else if (string.Equals(m, "ClearPreviousStudies", StringComparison.OrdinalIgnoreCase) && _clearPreviousStudiesProc != null)
                    {
                        await _clearPreviousStudiesProc.ExecuteAsync(this);
                    }
                    else if (string.Equals(m, "SetCurrentStudyTechniques", StringComparison.OrdinalIgnoreCase) && _setCurrentStudyTechniquesProc != null)
                    {
                        await _setCurrentStudyTechniquesProc.ExecuteAsync(this);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("[Automation] Module '" + m + "' error: " + ex.Message);
                    SetStatus($"Module '{m}' failed - procedure aborted", true);
                    return; // ABORT entire sequence on any exception (including GetUntilReportDateTime failure)
                }
            }
            
            // Check for unclosed if-blocks
            if (ifStack.Count > 0)
            {
                Debug.WriteLine($"[Automation] WARNING: {ifStack.Count} unclosed if-block(s) at end of sequence");
                SetStatus($"? {sequenceName} completed with {ifStack.Count} unclosed if-block(s)", isError: true);
                return;
            }
            
            // NEW: Append green completion message after all modules succeed
            SetStatus($"? {sequenceName} completed successfully", isError: false);
        }
        
        /// <summary>
        /// Built-in module: AutofillCurrentHeader
        /// Handles Chief Complaint and Patient History auto-filling based on toggle states.
        /// 
        /// Logic:
        /// - If copy toggle is ON: Copy Study Remark to Chief Complaint
        /// - Else if auto toggle is ON: Invoke generate button for Chief Complaint
        /// - If Patient History auto toggle is ON: Invoke generate button for Patient History
        /// </summary>
        private async Task AutofillCurrentHeaderAsync()
        {
            Debug.WriteLine("[Automation][AutofillCurrentHeader] START");
            
            // Chief Complaint logic
            if (CopyStudyRemarkToChiefComplaint)
            {
                Debug.WriteLine("[Automation][AutofillCurrentHeader] Copy toggle ON - copying Study Remark to Chief Complaint");
                ChiefComplaint = StudyRemark ?? string.Empty;
                SetStatus("Chief Complaint filled from Study Remark");
            }
            else if (AutoChiefComplaint)
            {
                Debug.WriteLine("[Automation][AutofillCurrentHeader] Auto toggle ON - invoking generate for Chief Complaint");
                
                // Invoke generate command on UI thread
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (GenerateFieldCommand != null && GenerateFieldCommand.CanExecute("chief_complaint"))
                    {
                        GenerateFieldCommand.Execute("chief_complaint");
                        Debug.WriteLine("[Automation][AutofillCurrentHeader] Generate command executed for Chief Complaint");
                    }
                    else
                    {
                        Debug.WriteLine("[Automation][AutofillCurrentHeader] WARNING: Generate command not available for Chief Complaint");
                    }
                });
                
                SetStatus("Chief Complaint generate invoked");
            }
            else
            {
                Debug.WriteLine("[Automation][AutofillCurrentHeader] Both copy and auto toggles OFF - Chief Complaint not updated");
            }
            
            // Patient History logic
            if (AutoPatientHistory)
            {
                Debug.WriteLine("[Automation][AutofillCurrentHeader] Auto toggle ON - invoking generate for Patient History");
                
                // Invoke generate command on UI thread
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (GenerateFieldCommand != null && GenerateFieldCommand.CanExecute("patient_history"))
                    {
                        GenerateFieldCommand.Execute("patient_history");
                        Debug.WriteLine("[Automation][AutofillCurrentHeader] Generate command executed for Patient History");
                    }
                    else
                    {
                        Debug.WriteLine("[Automation][AutofillCurrentHeader] WARNING: Generate command not available for Patient History");
                    }
                });
                
                SetStatus("Patient History generate invoked");
            }
            else
            {
                Debug.WriteLine("[Automation][AutofillCurrentHeader] Auto toggle OFF - Patient History not updated");
            }
            
            Debug.WriteLine("[Automation][AutofillCurrentHeader] COMPLETED");
            SetStatus("AutofillCurrentHeader completed");
        }
    }
}
