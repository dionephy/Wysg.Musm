using System;
using System.Collections.Generic;
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
        private const string ElseIfMessageModuleName = "Else If Message is No";
        private const string IfModalityWithHeaderModuleName = "If Modality with Header";

        private enum MessageBranchState
        {
            None,
            RunningIf,
            RunningElse,
            SkipAll
        }

        private sealed class AutomationIfBlock
        {
            public int StartIndex { get; init; }
            public bool ConditionMet { get; set; }
            public bool IsMessageBox { get; init; }
            public int ElseIndex { get; init; }
            public int EndIndex { get; init; }
            public MessageBranchState BranchState { get; set; }
        }

        private async Task RunNewStudyProcedureAsync() => await (_newStudyProc != null ? _newStudyProc.ExecuteAsync(this) : Task.Run(OnNewStudy));

        // Execute modules sequentially to preserve user-defined order
        private async Task RunModulesSequentially(string[] modules, string sequenceName = "automation")
        {
            // Generate a new session ID for this automation run
            // This allows element caching within the session (fast) while clearing between sessions (fresh data)
            var sessionId = $"{sequenceName}_{DateTime.Now.Ticks}";
            Services.ProcedureExecutor.SetSessionId(sessionId);
            Debug.WriteLine($"[Automation] Starting sequence '{sequenceName}' with session ID: {sessionId}");
            
            var customStore = Wysg.Musm.Radium.Models.CustomModuleStore.Load();
            var labelPositions = BuildLabelPositions(modules);
            var gotoHopLimit = Math.Max(modules.Length * 5, 100);
            int gotoHopCount = 0;
            
            var ifStack = new Stack<AutomationIfBlock>();
            bool skipExecution = false;
            
            void UpdateSkipExecution() => skipExecution = ifStack.Any(entry => !entry.ConditionMet);
            
            for (int i = 0; i < modules.Length; i++)
            {
                var m = modules[i];
                
                try
                {
                    // Treat label entries (e.g., "Label:") as no-op markers
                    if (Wysg.Musm.Radium.Models.CustomModuleStore.TryParseLabelDisplay(m, out var labelName))
                    {
                        Debug.WriteLine($"[Automation] Reached label '{labelName}' at position {i}");
                        continue;
                    }
                    
                    // Handle built-in "End if" module (must be checked first, before skipExecution)
                    if (string.Equals(m, "End if", StringComparison.OrdinalIgnoreCase))
                    {
                        if (ifStack.Count == 0)
                        {
                            Debug.WriteLine($"[Automation] WARNING: 'End if' without matching 'If' at position {i}");
                            continue;
                        }
                        
                        ifStack.Pop();
                        UpdateSkipExecution();
                        Debug.WriteLine($"[Automation] End if - resuming execution (skipExecution={skipExecution})");
                        continue;
                    }
                    
                    if (string.Equals(m, ElseIfMessageModuleName, StringComparison.OrdinalIgnoreCase))
                    {
                        HandleElseIf(ref i);
                        continue;
                    }
                    
                    if (string.Equals(m, IfModalityWithHeaderModuleName, StringComparison.OrdinalIgnoreCase))
                    {
                        var (hasHeader, modality) = await EvaluateModalityHeaderSettingAsync();
                        var block = new AutomationIfBlock
                        {
                            StartIndex = i,
                            ConditionMet = hasHeader,
                            IsMessageBox = false,
                            ElseIndex = -1,
                            EndIndex = -1,
                            BranchState = hasHeader ? MessageBranchState.RunningIf : MessageBranchState.SkipAll
                        };
                        ifStack.Push(block);
                        UpdateSkipExecution();
                        var modalityLabel = string.IsNullOrWhiteSpace(modality) ? "(unknown)" : modality;
                        Debug.WriteLine($"[Automation][If Modality with Header] modality='{modalityLabel}', conditionMet={hasHeader}");
                        SetStatus($"[{IfModalityWithHeaderModuleName}] {(hasHeader ? $"'{modalityLabel}' includes header" : $"'{modalityLabel}' excluded - skipping block")}");
                        continue;
                    }
                    
                    // Check if this is a custom module (If, If not, AbortIf, Set, Run, Goto, MessageIf)
                    var customModule = customStore.GetModule(m);

                    if (customModule != null)
                    {
                        // Handle If, If not, and message-if modules before skipExecution
                        if (customModule.Type == Wysg.Musm.Radium.Models.CustomModuleType.If ||
                            customModule.Type == Wysg.Musm.Radium.Models.CustomModuleType.IfNot ||
                            customModule.Type == Wysg.Musm.Radium.Models.CustomModuleType.IfMessageYes)
                        {
                            if (customModule.Type == Wysg.Musm.Radium.Models.CustomModuleType.IfMessageYes)
                            {
                                if (skipExecution)
                                {
                                    var skippedBlock = new AutomationIfBlock
                                    {
                                        StartIndex = i,
                                        ConditionMet = false,
                                        IsMessageBox = true,
                                        ElseIndex = -1,
                                        EndIndex = -1,
                                        BranchState = MessageBranchState.SkipAll
                                    };
                                    ifStack.Push(skippedBlock);
                                    UpdateSkipExecution();
                                    continue;
                                }
                                var bounds = FindMessageBlockBounds(modules, i, customStore);
                                if (!bounds.HasEnd)
                                {
                                    SetStatus($"[{customModule.Name}] Missing 'End if' - skipping block", true);
                                }
                                var messageResult = await Services.ProcedureExecutor.ExecuteAsync(customModule.ProcedureName);
                                var prompt = string.IsNullOrWhiteSpace(messageResult)
                                    ? "Proceed?"
                                    : messageResult.Trim();
                                const string caption = "Confirmation";
                                bool userChoseYes = false;
                                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                                {
                                    var dialogResult = System.Windows.MessageBox.Show(
                                        prompt,
                                        caption,
                                        System.Windows.MessageBoxButton.YesNo,
                                        System.Windows.MessageBoxImage.Question);
                                    userChoseYes = dialogResult == System.Windows.MessageBoxResult.Yes;
                                });
                                Debug.WriteLine($"[Automation] {customModule.Name}: userChoseYes={userChoseYes}");
                                SetStatus($"[{customModule.Name}] User selected {(userChoseYes ? "Yes" : "No")}");

                                if (userChoseYes)
                                {
                                    var messageBlock = new AutomationIfBlock
                                    {
                                        StartIndex = i,
                                        ConditionMet = true,
                                        IsMessageBox = true,
                                        ElseIndex = bounds.ElseIndex,
                                        EndIndex = bounds.EndIndex,
                                        BranchState = MessageBranchState.RunningIf
                                    };
                                    ifStack.Push(messageBlock);
                                    UpdateSkipExecution();
                                }
                                else if (bounds.ElseIndex >= 0)
                                {
                                    var messageBlock = new AutomationIfBlock
                                    {
                                        StartIndex = i,
                                        ConditionMet = true,
                                        IsMessageBox = true,
                                        ElseIndex = bounds.ElseIndex,
                                        EndIndex = bounds.EndIndex,
                                        BranchState = MessageBranchState.RunningElse
                                    };
                                    ifStack.Push(messageBlock);
                                    UpdateSkipExecution();
                                    if (bounds.ElseIndex > i)
                                    {
                                        i = bounds.ElseIndex - 1;
                                    }
                                }
                                else
                                {
                                    var messageBlock = new AutomationIfBlock
                                    {
                                        StartIndex = i,
                                        ConditionMet = false,
                                        IsMessageBox = true,
                                        ElseIndex = -1,
                                        EndIndex = bounds.EndIndex,
                                        BranchState = MessageBranchState.SkipAll
                                    };
                                    ifStack.Push(messageBlock);
                                    UpdateSkipExecution();
                                }
                                continue;
                            }

                            if (skipExecution)
                            {
                                var skippedBlock = new AutomationIfBlock
                                {
                                    StartIndex = i,
                                    ConditionMet = false,
                                    IsMessageBox = false,
                                    ElseIndex = -1,
                                    EndIndex = -1,
                                    BranchState = MessageBranchState.SkipAll
                                };
                                ifStack.Push(skippedBlock);
                                UpdateSkipExecution();
                                Debug.WriteLine($"[Automation] Skipping condition '{customModule.Name}' (parent block false)");
                                continue;
                            }
 
                            var ifSw = Stopwatch.StartNew();
                            var result = await Services.ProcedureExecutor.ExecuteAsync(customModule.ProcedureName);
                            ifSw.Stop();
                            bool conditionValue = !string.IsNullOrWhiteSpace(result) &&
                                                  !string.Equals(result, "false", StringComparison.OrdinalIgnoreCase);
                            bool conditionMet = customModule.Type == Wysg.Musm.Radium.Models.CustomModuleType.IfNot
                                ? !conditionValue
                                : conditionValue;
                            var block = new AutomationIfBlock
                            {
                                StartIndex = i,
                                ConditionMet = conditionMet,
                                IsMessageBox = false,
                                ElseIndex = -1,
                                EndIndex = -1,
                                BranchState = conditionMet ? MessageBranchState.RunningIf : MessageBranchState.SkipAll
                            };
                            ifStack.Push(block);
                            UpdateSkipExecution();
                            Debug.WriteLine($"[Automation] {customModule.Name}: condition={conditionValue}, negated={customModule.Type == Wysg.Musm.Radium.Models.CustomModuleType.IfNot}, conditionMet={conditionMet}, skipExecution={skipExecution}");
                            SetStatus($"[{customModule.Name}] {(conditionMet ? "Condition met." : "Condition not met.")} ({ifSw.ElapsedMilliseconds} ms)");
                            continue;
                        }
                        
                        if (customModule.Type == Wysg.Musm.Radium.Models.CustomModuleType.Goto)
                        {
                            if (string.IsNullOrWhiteSpace(customModule.TargetLabelName) ||
                                !labelPositions.TryGetValue(customModule.TargetLabelName, out var targetIndex))
                            {
                                SetStatus($"[{customModule.Name}] Target label '{customModule.TargetLabelName}' not found - sequence aborted.", true);
                                return;
                            }
                            
                            gotoHopCount++;
                            if (gotoHopCount > gotoHopLimit)
                            {
                                SetStatus($"[{customModule.Name}] Too many goto hops - possible loop detected.", true);
                                Debug.WriteLine($"[Automation] Aborting due to excessive goto hops (>{gotoHopLimit})");
                                return;
                            }
                            
                            Debug.WriteLine($"[Automation] {customModule.Name}: jumping to label '{customModule.TargetLabelName}' at index {targetIndex}");
                            i = targetIndex;
                            continue;
                        }
                        
                        // If we're inside a false if-block, skip execution of custom modules (except goto handled above)
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
                        SetStatus("[Abort]");
                        SetStatus($">> {sequenceName} aborted");
                        return; // Immediately abort the entire sequence
                    }

                    // Standard modules...
                    if (string.Equals(m, "NewStudy", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(m, "NewStudy(obs)", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(m, "NewStudy(Obsolete)", StringComparison.OrdinalIgnoreCase))
                    {
                        await RunNewStudyProcedureAsync();
                    }
                    else if (string.Equals(m, "SetStudyLocked", StringComparison.OrdinalIgnoreCase) && _setStudyLockedProc != null) { await _setStudyLockedProc.ExecuteAsync(this); }
                    else if (string.Equals(m, "LockStudy", StringComparison.OrdinalIgnoreCase) && _setStudyLockedProc != null) { await _setStudyLockedProc.ExecuteAsync(this); }
                    else if (string.Equals(m, "SetStudyOpened", StringComparison.OrdinalIgnoreCase) && _setStudyOpenedProc != null) { await _setStudyOpenedProc.ExecuteAsync(this); }
                    else if (string.Equals(m, "UnlockStudy", StringComparison.OrdinalIgnoreCase))
                    {
                        PatientLocked = false;
                        StudyOpened = false;
                        SetStatus("[UnlockStudy] Done.");
                    }
                    else if (string.Equals(m, "SetCurrentTogglesOff", StringComparison.OrdinalIgnoreCase))
                    {
                        ProofreadMode = false;
                        Reportified = false;
                        SetStatus("[SetCurrentTogglesOff] Done.");
                    }
                    else if (string.Equals(m, "ClearCurrentFields", StringComparison.OrdinalIgnoreCase) && _clearCurrentFieldsProc != null)
                    {
                        await _clearCurrentFieldsProc.ExecuteAsync(this);
                        SetStatus("[ClearCurrentFields] Done.");
                    }
                    else if (string.Equals(m, "AutofillCurrentHeader", StringComparison.OrdinalIgnoreCase))
                    {
                        await AutofillCurrentHeaderAsync();
                    }
                    else if (string.Equals(m, "GetStudyRemark", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(m, "GetStudyRemark(obs)", StringComparison.OrdinalIgnoreCase)) { await AcquireStudyRemarkAsync(); }
                    else if (string.Equals(m, "GetPatientRemark", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(m, "GetPatientRemark(obs)", StringComparison.OrdinalIgnoreCase)) { await AcquirePatientRemarkAsync(); }
                    else if (string.Equals(m, "AddPreviousStudy", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(m, "AddPreviousStudy(obs)", StringComparison.OrdinalIgnoreCase)) { await RunAddPreviousStudyModuleAsync(); }
                    else if (string.Equals(m, "FetchPreviousStudies", StringComparison.OrdinalIgnoreCase)) { await RunFetchPreviousStudiesAsync(); }
                    else if (string.Equals(m, "SetComparison", StringComparison.OrdinalIgnoreCase)) { await RunSetComparisonAsync(); }
                    else if (string.Equals(m, "AbortIfWorklistClosed", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(m, "AbortIfWorklistClosed(obs)", StringComparison.OrdinalIgnoreCase))
                    {
                        var isVisible = await _pacs.WorklistIsVisibleAsync();
                        if (string.Equals(isVisible, "false", StringComparison.OrdinalIgnoreCase))
                        {
                            SetStatus("Worklist closed - automation aborted", true);
                            return;
                        }
                        SetStatus("Worklist visible - continuing");
                    }
                    else if (string.Equals(m, "AbortIfPatientNumberNotMatch", StringComparison.OrdinalIgnoreCase))
                    {
                        var matchResult = await _pacs.PatientNumberMatchAsync();
                        if (string.Equals(matchResult, "false", StringComparison.OrdinalIgnoreCase))
                        {
                            var pacsPatientNumber = await _pacs.GetCurrentPatientNumberAsync();
                            var mainPatientNumber = PatientNumber;
                            Debug.WriteLine($"[Automation][AbortIfPatientNumberNotMatch] MISMATCH - PACS: '{pacsPatientNumber}' Main: '{mainPatientNumber}'");
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
                                return;
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
                            var pacsStudyDateTime = await _pacs.GetCurrentStudyDateTimeAsync();
                            var mainStudyDateTime = StudyDateTime;
                            Debug.WriteLine($"[Automation][AbortIfStudyDateTimeNotMatch] MISMATCH - PACS: '{pacsStudyDateTime}' Main: '{mainStudyDateTime}'");
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
                                return;
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
                    else if (string.Equals(m, "OpenStudy", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(m, "OpenStudy(obs)", StringComparison.OrdinalIgnoreCase)) { await RunOpenStudyAsync(); }
                    else if (string.Equals(m, "MouseClick1", StringComparison.OrdinalIgnoreCase)) { await _pacs.CustomMouseClick1Async(); }
                    else if (string.Equals(m, "MouseClick2", StringComparison.OrdinalIgnoreCase)) { await _pacs.CustomMouseClick2Async(); }
                    else if (string.Equals(m, "TestInvoke", StringComparison.OrdinalIgnoreCase)) { await _pacs.InvokeTestAsync(); }
                    else if (string.Equals(m, "ShowTestMessage", StringComparison.OrdinalIgnoreCase)) { System.Windows.MessageBox.Show("Test", "Test", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information); }
                    else if (string.Equals(m, "SetCurrentInMainScreen", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(m, "SetCurrentInMainScreen(obs)", StringComparison.OrdinalIgnoreCase)) { await RunSetCurrentInMainScreenAsync(); }
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
                        SetStatus("[Reportify] Done.");
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
                    else if (string.Equals(m, "InsertPreviousStudy", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(m, "InsertPreviousStudyReport", StringComparison.OrdinalIgnoreCase))
                    {
                        Debug.WriteLine("[Automation] InsertPreviousStudyReport module - START");
                        await RunInsertPreviousStudyAsync();
                        Debug.WriteLine("[Automation] InsertPreviousStudyReport module - COMPLETED");
                    }
                    else if (string.Equals(m, "ClearPreviousFields", StringComparison.OrdinalIgnoreCase) && _clearPreviousFieldsProc != null)
                    {
                        await _clearPreviousFieldsProc.ExecuteAsync(this);
                        SetStatus("[ClearPreviousFields] Done.");
                    }
                    else if (string.Equals(m, "ClearPreviousStudies", StringComparison.OrdinalIgnoreCase) && _clearPreviousStudiesProc != null)
                    {
                        await _clearPreviousStudiesProc.ExecuteAsync(this);
                        SetStatus("[ClearPreviousStudies] Done.");
                    }
                    else if (string.Equals(m, "ClearTempVariables", StringComparison.OrdinalIgnoreCase)) { await RunClearTempVariablesAsync(); }
                    else if (string.Equals(m, "SetCurrentStudyTechniques", StringComparison.OrdinalIgnoreCase) && _setCurrentStudyTechniquesProc != null)
                    {
                        await _setCurrentStudyTechniquesProc.ExecuteAsync(this);
                    }
                    else if (string.Equals(m, "FocusEditorFindings", StringComparison.OrdinalIgnoreCase))
                    {
                        await RunFocusEditorFindingsAsync();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("[Automation] Module '" + m + "' error: " + ex.Message);
                    SetStatus($"Module '{m}' failed - procedure aborted", true);
                    return;
                }
            }
            
            // Check for unclosed if-blocks
            if (ifStack.Count > 0)
            {
                Debug.WriteLine($"[Automation] WARNING: {ifStack.Count} unclosed if-block(s) at end of sequence");
                SetStatus($"? {sequenceName} completed with {ifStack.Count} unclosed if-block(s)", isError: true);
                return;
            }
            
            SetStatus($">> {sequenceName} completed successfully", isError: false);
            
            Dictionary<string, int> BuildLabelPositions(string[] source)
            {
                var positions = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                for (int index = 0; index < source.Length; index++)
                {
                    if (Wysg.Musm.Radium.Models.CustomModuleStore.TryParseLabelDisplay(source[index], out var name))
                    {
                        if (!positions.ContainsKey(name))
                        {
                            positions.Add(name, index);
                        }
                        else
                        {
                            Debug.WriteLine($"[Automation] Duplicate label '{name}' detected at index {index} (first at {positions[name]})");
                        }
                    }
                }
                return positions;
            }
            
            (bool HasEnd, int ElseIndex, int EndIndex) FindMessageBlockBounds(string[] source, int startIndex, Wysg.Musm.Radium.Models.CustomModuleStore store)
            {
                int depth = 0;
                int elseIdx = -1;
                for (int idx = startIndex + 1; idx < source.Length; idx++)
                {
                    var entry = source[idx];
                    if (string.Equals(entry, "End if", StringComparison.OrdinalIgnoreCase))
                    {
                        if (depth == 0)
                        {
                            return (true, elseIdx, idx);
                        }
                        depth--;
                        continue;
                    }
                    
                    if (string.Equals(entry, ElseIfMessageModuleName, StringComparison.OrdinalIgnoreCase))
                    {
                        if (depth == 0)
                        {
                            elseIdx = idx;
                        }
                        continue;
                    }
                    
                    var nested = store.GetModule(entry);
                    if (nested != null && (nested.Type == Wysg.Musm.Radium.Models.CustomModuleType.If ||
                                           nested.Type == Wysg.Musm.Radium.Models.CustomModuleType.IfNot ||
                                           nested.Type == Wysg.Musm.Radium.Models.CustomModuleType.IfMessageYes))
                    {
                        depth++;
                    }
                }
                return (false, elseIdx, source.Length - 1);
            }
            
            void HandleElseIf(ref int index)
            {
                if (skipExecution)
                {
                    return;
                }
                if (ifStack.Count == 0)
                {
                    Debug.WriteLine($"[Automation] WARNING: '{ElseIfMessageModuleName}' without matching message-if at position {index}");
                    return;
                }
                var current = ifStack.Peek();
                if (!current.IsMessageBox)
                {
                    Debug.WriteLine($"[Automation] WARNING: '{ElseIfMessageModuleName}' encountered outside message-if at position {index}");
                    return;
                }
                if (current.BranchState == MessageBranchState.RunningIf)
                {
                    current.ConditionMet = false;
                    current.BranchState = MessageBranchState.SkipAll;
                    UpdateSkipExecution();
                    if (current.EndIndex >= 0)
                    {
                        index = current.EndIndex - 1;
                    }
                    return;
                }
                if (current.BranchState == MessageBranchState.RunningElse)
                {
                    return;
                }
            }
        }

        /// <summary>
        /// Built-in module: AutofillCurrentHeader
        /// Handles Chief Complaint and Patient History auto-filling based on toggle states.
        /// </summary>
        private async Task AutofillCurrentHeaderAsync()
        {
            Debug.WriteLine("[Automation][AutofillCurrentHeader] START");
            
            if (CopyStudyRemarkToChiefComplaint)
            {
                Debug.WriteLine("[Automation][AutofillCurrentHeader] Copy toggle ON - copying Study Remark to Chief Complaint");
                ChiefComplaint = StudyRemark ?? string.Empty;
                SetStatus("[AutofillCurrentHeader] Chief Complaint filled from Study Remark.");
            }
            else if (AutoChiefComplaint)
            {
                Debug.WriteLine("[Automation][AutofillCurrentHeader] Auto toggle ON - invoking generate for Chief Complaint");
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
                SetStatus("[AutofillCurrentHeader] Chief Complaint generate invoked.");
            }
            else
            {
                Debug.WriteLine("[Automation][AutofillCurrentHeader] Both copy and auto toggles OFF - Chief Complaint not updated");
            }
            
            if (AutoPatientHistory)
            {
                Debug.WriteLine("[Automation][AutofillCurrentHeader] Auto toggle ON - invoking generate for Patient History");
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
                SetStatus("[AutofillCurrentHeader] Patient History generate invoked.");
            }
            else
            {
                Debug.WriteLine("[Automation][AutofillCurrentHeader] Auto toggle OFF - Patient History not updated");
            }
            
            Debug.WriteLine("[Automation][AutofillCurrentHeader] COMPLETED");
            SetStatus("[AutofillCurrentHeader] Done.");
        }
        
        /// <summary>
        /// Built-in module: FocusEditorFindings
        /// Brings the MainWindow to the front, activates it, and focuses the Findings editor.
        /// </summary>
        private async Task RunFocusEditorFindingsAsync()
        {
            Debug.WriteLine("[Automation][FocusEditorFindings] START");
            
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    var mainWindow = System.Windows.Application.Current.MainWindow;
                    if (mainWindow == null)
                    {
                        Debug.WriteLine("[Automation][FocusEditorFindings] MainWindow is null");
                        SetStatus("[FocusEditorFindings] MainWindow not found.", isError: true);
                        return;
                    }
                    
                    if (mainWindow.WindowState == System.Windows.WindowState.Minimized)
                    {
                        mainWindow.WindowState = System.Windows.WindowState.Normal;
                    }
                    
                    mainWindow.Activate();
                    mainWindow.Focus();
                    Debug.WriteLine("[Automation][FocusEditorFindings] MainWindow activated");
                    
                    if (mainWindow is Views.MainWindow mw)
                    {
                        var gridCenter = mw.FindName("gridCenter") as Controls.CenterEditingArea;
                        if (gridCenter != null)
                        {
                            var editorFindings = gridCenter.EditorFindings;
                            if (editorFindings != null)
                            {
                                var musmEditor = editorFindings.FindName("Editor") as ICSharpCode.AvalonEdit.TextEditor;
                                if (musmEditor != null)
                                {
                                    musmEditor.Focus();
                                    musmEditor.TextArea?.Caret.BringCaretToView();
                                    Debug.WriteLine("[Automation][FocusEditorFindings] Focused MusmEditor");
                                }
                                else
                                {
                                    editorFindings.Focus();
                                    Debug.WriteLine("[Automation][FocusEditorFindings] Focused EditorControl (MusmEditor not found)");
                                }
                            }
                            else
                            {
                                Debug.WriteLine("[Automation][FocusEditorFindings] EditorFindings not found in gridCenter");
                            }
                        }
                        else
                        {
                            Debug.WriteLine("[Automation][FocusEditorFindings] gridCenter not found in MainWindow");
                        }
                    }
                    else
                    {
                        Debug.WriteLine("[Automation][FocusEditorFindings] MainWindow is not of type Views.MainWindow");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Automation][FocusEditorFindings] Error: {ex.Message}");
                    SetStatus($"[FocusEditorFindings] Error: {ex.Message}", isError: true);
                }
            });
            
            SetStatus("[FocusEditorFindings] Done.");
            Debug.WriteLine("[Automation][FocusEditorFindings] COMPLETED");
        }
    }
}
