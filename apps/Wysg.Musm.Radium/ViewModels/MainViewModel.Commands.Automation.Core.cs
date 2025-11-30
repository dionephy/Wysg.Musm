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
            foreach (var m in modules)
            {
                try
                {
                    // Check if this is a custom module
                    var customStore = Wysg.Musm.Radium.Models.CustomModuleStore.Load();
                    var customModule = customStore.GetModule(m);

                    if (customModule != null)
                    {
                        await RunCustomModuleAsync(customModule);
                        continue;
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
            
            // NEW: Append green completion message after all modules succeed
            SetStatus($"? {sequenceName} completed successfully", isError: false);
        }
    }
}
