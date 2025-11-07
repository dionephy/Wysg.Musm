using System;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Diagnostics;

namespace Wysg.Musm.Radium.ViewModels
{
    /// <summary>
    /// Partial: ICommand definitions & handlers (New Study, Add, Send, Select, Procedure execution).
    /// Keeps UI action logic compact and discoverable.
    /// </summary>
    public partial class MainViewModel
    {
        // ------------- Command properties (initialized in InitializeCommands) -------------
        public ICommand NewStudyCommand { get; private set; } = null!;
        public ICommand TestNewStudyProcedureCommand { get; private set; } = null!;
        public ICommand AddStudyCommand { get; private set; } = null!;
        public ICommand SendReportPreviewCommand { get; private set; } = null!;
        public ICommand SendReportCommand { get; private set; } = null!;
        public ICommand SelectPreviousStudyCommand { get; private set; } = null!;
        public ICommand OpenStudynameMapCommand { get; private set; } = null!;
        public ICommand GenerateFieldCommand { get; private set; } = null!;
        public ICommand EditStudyTechniqueCommand { get; private set; } = null!;
        public ICommand EditComparisonCommand { get; private set; } = null!;
        public ICommand SavePreorderCommand { get; private set; } = null!;
        public ICommand SavePreviousStudyToDBCommand { get; private set; } = null!;

        // UI mode toggles
        private bool _proofreadMode; 
        public bool ProofreadMode 
        { 
            get => _proofreadMode; 
            set 
            { 
                if (SetProperty(ref _proofreadMode, value))
                {
                    // Notify computed display properties for editors (Findings, Conclusion, and Header components)
                    OnPropertyChanged(nameof(FindingsDisplay));
                    OnPropertyChanged(nameof(ConclusionDisplay));
                    // NEW: Notify header component display properties and HeaderDisplay
                    OnPropertyChanged(nameof(ChiefComplaintDisplay));
                    OnPropertyChanged(nameof(PatientHistoryDisplay));
                    OnPropertyChanged(nameof(StudyTechniquesDisplay));
                    OnPropertyChanged(nameof(ComparisonDisplay));
                    OnPropertyChanged(nameof(HeaderDisplay));
                }
            } 
        }
        
        private bool _previousProofreadMode=true; 
        public bool PreviousProofreadMode 
        { 
            get => _previousProofreadMode; 
            set 
            { 
                if (SetProperty(ref _previousProofreadMode, value))
                {
                    Debug.WriteLine($"[PreviousProofreadMode] Changed to: {value}");
                    
                    // Notify all previous report computed display properties
                    OnPropertyChanged(nameof(PreviousChiefComplaintDisplay));
                    OnPropertyChanged(nameof(PreviousPatientHistoryDisplay));
                    OnPropertyChanged(nameof(PreviousStudyTechniquesDisplay));
                    OnPropertyChanged(nameof(PreviousComparisonDisplay));
                    OnPropertyChanged(nameof(PreviousFindingsDisplay));
                    OnPropertyChanged(nameof(PreviousConclusionDisplay));
                    
                    // CRITICAL FIX: Notify editor properties when proofread mode changes
                    // These properties must be notified so editors update in real-time
                    OnPropertyChanged(nameof(PreviousFindingsEditorText));
                    OnPropertyChanged(nameof(PreviousConclusionEditorText));
                    
                    Debug.WriteLine("[PreviousProofreadMode] All editor properties notified");
                }
            } 
        }

        // Study opened toggle (set when OpenStudy module runs)
        private bool _studyOpened; public bool StudyOpened { get => _studyOpened; set => SetProperty(ref _studyOpened, value); }

        // Auto toggles for generation on current report fields
        private bool _autoChiefComplaint; public bool AutoChiefComplaint { get => _autoChiefComplaint; set => SetProperty(ref _autoChiefComplaint, value); }
        private bool _autoPatientHistory; public bool AutoPatientHistory { get => _autoPatientHistory; set => SetProperty(ref _autoPatientHistory, value); }
        private bool _autoConclusion; public bool AutoConclusion { get => _autoConclusion; set => SetProperty(ref _autoConclusion, value); }

        // Auto toggles for previous/bottom extra fields
        private bool _autoStudyTechniques; public bool AutoStudyTechniques { get => _autoStudyTechniques; set => SetProperty(ref _autoStudyTechniques, value); }
        private bool _autoComparison; public bool AutoComparison { get => _autoComparison; set => SetProperty(ref _autoComparison, value); }

        // Auto toggles for proofread fields
        private bool _autoChiefComplaintProofread; public bool AutoChiefComplaintProofread { get => _autoChiefComplaintProofread; set => SetProperty(ref _autoChiefComplaintProofread, value); }
        private bool _autoPatientHistoryProofread; public bool AutoPatientHistoryProofread { get => _autoPatientHistoryProofread; set => SetProperty(ref _autoPatientHistoryProofread, value); }
        private bool _autoStudyTechniquesProofread; public bool AutoStudyTechniquesProofread { get => _autoStudyTechniquesProofread; set => SetProperty(ref _autoStudyTechniquesProofread, value); }
        private bool _autoComparisonProofread; public bool AutoComparisonProofread { get => _autoComparisonProofread; set => SetProperty(ref _autoComparisonProofread, value); }
        private bool _autoFindingsProofread; public bool AutoFindingsProofread { get => _autoFindingsProofread; set => SetProperty(ref _autoFindingsProofread, value); }
        private bool _autoConclusionProofread; public bool AutoConclusionProofread { get => _autoConclusionProofread; set => SetProperty(ref _autoConclusionProofread, value); }

        // Patient locked state influences several command CanExecute states
        private bool _patientLocked; public bool PatientLocked
        {
            get => _patientLocked;
            set
            {
                if (SetProperty(ref _patientLocked, value))
                {
                    (AddStudyCommand as DelegateCommand)?.RaiseCanExecuteChanged();
                    (SendReportPreviewCommand as DelegateCommand)?.RaiseCanExecuteChanged();
                    (SendReportCommand as DelegateCommand)?.RaiseCanExecuteChanged();
                    (SelectPreviousStudyCommand as DelegateCommand)?.RaiseCanExecuteChanged();
                    (SavePreviousStudyToDBCommand as DelegateCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        private void InitializeCommands()
        {
            NewStudyCommand = new DelegateCommand(_ => OnNewStudy());
            TestNewStudyProcedureCommand = new DelegateCommand(_ => OnRunTestAutomation());
            AddStudyCommand = new DelegateCommand(_ => OnRunAddStudyAutomation(), _ => PatientLocked);
            SendReportPreviewCommand = new DelegateCommand(_ => OnSendReportPreview(), _ => PatientLocked);
            SendReportCommand = new DelegateCommand(_ => OnSendReport(), _ => PatientLocked);
            SelectPreviousStudyCommand = new DelegateCommand(o => OnSelectPrevious(o), _ => PatientLocked);
            OpenStudynameMapCommand = new DelegateCommand(_ => Views.StudynameLoincWindow.Open());
            GenerateFieldCommand = new DelegateCommand(p => OnGenerateField(p));
            EditStudyTechniqueCommand = new DelegateCommand(_ => OnEditStudyTechnique(), _ => PatientLocked);
            EditComparisonCommand = new DelegateCommand(_ => OnEditComparison(), _ => PatientLocked);
            SavePreorderCommand = new DelegateCommand(_ => OnSavePreorder());
            SavePreviousStudyToDBCommand = new DelegateCommand(_ => OnSavePreviousStudyToDB(), _ => PatientLocked && SelectedPreviousStudy != null);
        }

        // ------------- Handlers -------------
        private void OnSendReportPreview() 
        { 
            var seqRaw = GetAutomationSequenceForCurrentPacs(static s => s.SendReportPreviewSequence);
            var modules = seqRaw.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (modules.Length > 0)
            {
                _ = RunModulesSequentially(modules, "Send Report Preview");
            }
            else
            {
                SetStatus("No Send Report Preview sequence configured", true);
            }
        }
        
        private void OnSendReport() 
        { 
            var seqRaw = GetAutomationSequenceForCurrentPacs(static s => s.SendReportSequence);
            var modules = seqRaw.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (modules.Length > 0)
            {
                _ = RunModulesSequentially(modules, "Send Report");
            }
            else
            {
                // Fallback: just unlock patient if no sequence configured
                PatientLocked = false;
            }
        }

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
                    else if (string.Equals(m, "SendReport", StringComparison.OrdinalIgnoreCase)) { await RunSendReportAsync(); }
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
                await _pacs.SetCurrentStudyInMainScreenAsync();
                await _pacs.SetPreviousStudyInSubScreenAsync();
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

        private void OnRunAddStudyAutomation()
        {
            var seqRaw = GetAutomationSequenceForCurrentPacs(static s => s.AddStudySequence);
            var modules = seqRaw.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (modules.Length == 0) return;
            _ = RunModulesSequentially(modules, "Add Study");
        }

        private void OnRunTestAutomation()
        {
            var seqRaw = GetAutomationSequenceForCurrentPacs(static s => s.TestSequence);
            var modules = seqRaw.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (modules.Length == 0) 
            {
                SetStatus("No Test sequence configured", true);
                return;
            }
            _ = RunModulesSequentially(modules, "Test");
        }

        private void OnNewStudy()
        {
            var seqRaw = GetAutomationSequenceForCurrentPacs(static s => s.NewStudySequence);
            var modules = seqRaw.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (modules.Length == 0) return;
            _ = RunModulesSequentially(modules, "New Study");
        }

        // Executes configured modules for OpenStudy shortcut depending on lock/opened state
        public void RunOpenStudyShortcut()
        {
            string seqRaw;
            string sequenceName;
            if (!PatientLocked) 
            {
                seqRaw = GetAutomationSequenceForCurrentPacs(static s => s.ShortcutOpenNew);
                sequenceName = "Shortcut: Open study (new)";
            }
            else if (!StudyOpened) 
            {
                seqRaw = GetAutomationSequenceForCurrentPacs(static s => s.ShortcutOpenAdd);
                sequenceName = "Shortcut: Open study (add)";
            }
            else 
            {
                seqRaw = GetAutomationSequenceForCurrentPacs(static s => s.ShortcutOpenAfterOpen);
                sequenceName = "Shortcut: Open study (after open)";
            }

            var modules = seqRaw.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (modules.Length == 0) return;
            _ = RunModulesSequentially(modules, sequenceName);
        }

        // Executes configured modules for SendReport shortcut depending on Reportified state
        public void RunSendReportShortcut()
        {
            string seqRaw;
            string sequenceName;
            if (Reportified)
            {
                seqRaw = GetAutomationSequenceForCurrentPacs(static s => s.ShortcutSendReportReportified);
                sequenceName = "Shortcut: Send report (reportified)";
                Debug.WriteLine("[SendReportShortcut] Reportified=true, using ShortcutSendReportReportified sequence");
            }
            else
            {
                seqRaw = GetAutomationSequenceForCurrentPacs(static s => s.ShortcutSendReportPreview);
                sequenceName = "Shortcut: Send report (preview)";
                Debug.WriteLine("[SendReportShortcut] Reportified=false, using ShortcutSendReportPreview sequence");
            }

            var modules = seqRaw.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (modules.Length == 0) 
            {
                SetStatus("No Send Report shortcut sequence configured", true);
                return;
            }
            _ = RunModulesSequentially(modules, sequenceName);
        }

        private async Task RunAddPreviousStudyModuleAsync()
        {
            var sw = Stopwatch.StartNew(); // Start timing
            Debug.WriteLine("[AddPreviousStudyModule] ===== START =====");
            
            try
            {
                // 1) Ensure PACS current patient equals application's current patient number
                var pacsCurrent = await _pacs.GetCurrentPatientNumberAsync();
                string Normalize(string? s) => string.IsNullOrWhiteSpace(s) ? string.Empty : Regex.Replace(s, "[^A-Za-z0-9]", "").ToUpperInvariant();
                if (Normalize(pacsCurrent) != Normalize(PatientNumber)) 
                { 
                    sw.Stop();
                    Debug.WriteLine($"[AddPreviousStudyModule] ===== ABORTED: Patient mismatch ===== ({sw.ElapsedMilliseconds} ms)");
                    SetStatus("Patient mismatch cannot add", true); 
                    return; 
                }

                // Optional: also verify selected related study belongs to same patient
                var relatedSelected = await _pacs.GetSelectedIdFromRelatedStudiesAsync();
                if (!string.IsNullOrWhiteSpace(relatedSelected) && Normalize(relatedSelected) != Normalize(PatientNumber))
                { 
                    sw.Stop();
                    Debug.WriteLine($"[AddPreviousStudyModule] ===== ABORTED: Related study patient mismatch ===== ({sw.ElapsedMilliseconds} ms)");
                    SetStatus("Related study not for current patient", true); 
                    return; 
                }

                // 2) Fetch ONLY studyname and datetime first (for early duplicate detection)
                var studyName = await _pacs.GetSelectedStudynameFromRelatedStudiesAsync();
                var dtStr = await _pacs.GetSelectedStudyDateTimeFromRelatedStudiesAsync();

                // Abort when studyname/datetime missing
                if (string.IsNullOrWhiteSpace(studyName) || string.IsNullOrWhiteSpace(dtStr))
                { 
                    sw.Stop();
                    Debug.WriteLine($"[AddPreviousStudyModule] ===== ABORTED: Missing metadata ===== ({sw.ElapsedMilliseconds} ms)");
                    SetStatus("Related study is incomplete (name/datetime)", true); 
                    return; 
                }

                // Parse datetimes
                if (!DateTime.TryParse(dtStr, out var studyDt)) 
                { 
                    sw.Stop();
                    Debug.WriteLine($"[AddPreviousStudyModule] ===== ABORTED: Invalid datetime ===== ({sw.ElapsedMilliseconds} ms)");
                    SetStatus("Related study datetime invalid", true); 
                    return; 
                }

                // EARLY EXIT: Check if same as current study (before fetching radiologist/reportDate)
                if (DateTime.TryParse(StudyDateTime, out var currentDt))
                {
                    if (string.Equals(studyName?.Trim(), StudyName?.Trim(), StringComparison.OrdinalIgnoreCase) && studyDt == currentDt)
                    { 
                        sw.Stop();
                        Debug.WriteLine($"[AddPreviousStudyModule] ===== ABORTED: Same as current study (early exit) ===== ({sw.ElapsedMilliseconds} ms)");
                        SetStatus("Same as current study; skipping add", true); 
                        return; 
                    }
                }

                // 3) Fetch remaining metadata (only if not duplicate)
                var radiologist = await _pacs.GetSelectedRadiologistFromRelatedStudiesAsync();
                var reportDateStr = await _pacs.GetSelectedReportDateTimeFromRelatedStudiesAsync();
                DateTime? reportDt = DateTime.TryParse(reportDateStr, out var rdt) ? rdt : null;

                // CRITICAL FIX: Check if study already exists in database with same studyname and studydatetime
                if (_studyRepo != null)
                {
                    var existingStudyId = await _studyRepo.GetStudyIdAsync(PatientNumber, studyName, studyDt);
                    if (existingStudyId.HasValue)
                    {
                        Debug.WriteLine($"[AddPreviousStudyModule] Study already exists in DB: studyId={existingStudyId.Value}");
                        
                        // Check if a report with the same reportDateTime already exists
                        // If it does, skip the save to preserve any manual edits (like proofread texts)
                        var existingReports = await _studyRepo.GetReportsForPatientAsync(PatientNumber);
                        var matchingReport = existingReports.FirstOrDefault(r => 
                            r.StudyId == existingStudyId.Value && 
                            r.ReportDateTime.HasValue && 
                            reportDt.HasValue && 
                            r.ReportDateTime.Value == reportDt.Value);
                        
                        if (matchingReport != null)
                        {
                            sw.Stop();
                            Debug.WriteLine($"[AddPreviousStudyModule] ===== ABORTED: Report with same datetime already exists in DB ===== ({sw.ElapsedMilliseconds} ms)");
                            Debug.WriteLine($"[AddPreviousStudyModule] Preserving existing report to keep manual edits (proofread texts, etc.)");
                            
                            // Still reload and select the existing study (in case UI wasn't showing it)
                            await LoadPreviousStudiesForPatientAsync(PatientNumber);
                            var existingTab = PreviousStudies.FirstOrDefault(t => t.StudyDateTime == studyDt);
                            if (existingTab != null) SelectedPreviousStudy = existingTab;
                            
                            // Append to comparison even though we skipped save
                            try
                            {
                                var modality = ExtractModality(studyName);
                                var dateStr = studyDt.ToString("yyyy-MM-dd");
                                var simplifiedStudy = $"{modality} {dateStr}";
                                var currentComparison = Comparison ?? string.Empty;
                                if (string.IsNullOrWhiteSpace(currentComparison))
                                {
                                    Comparison = simplifiedStudy;
                                }
                                else
                                {
                                    Comparison = currentComparison.TrimEnd() + ", " + simplifiedStudy;
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"[AddPreviousStudyModule] Comparison append error: {ex.Message}");
                            }
                            
                            SetStatus("Previous study already exists (preserved existing data)");
                            return;
                        }
                        else
                        {
                            Debug.WriteLine($"[AddPreviousStudyModule] Study exists but report datetime is different - will add new report");
                        }
                    }
                }

                // 4) CRITICAL OPTIMIZATION: Skip the slow ReportTextIsVisible check (takes 58 seconds!)
                // Instead, always try ALL getters in parallel and pick the longest result
                Debug.WriteLine("[AddPreviousStudy] Trying ALL getters in parallel (bypassing slow visibility check)");
                
                string findings = string.Empty, conclusion = string.Empty;
                
                // Launch all 4 getters simultaneously
                var f1Task = Task.Run(async () => 
                {
                    try 
                    { 
                        return await _pacs.GetCurrentFindingsAsync() ?? string.Empty;
                    } 
                    catch (Exception ex) 
                    { 
                        Debug.WriteLine($"[AddPreviousStudy][GetCurrentFindings] Exception: {ex.Message}");
                        return string.Empty; 
                    }
                });
                
                var c1Task = Task.Run(async () => 
                {
                    try 
                    { 
                        return await _pacs.GetCurrentConclusionAsync() ?? string.Empty;
                    } 
                    catch (Exception ex) 
                    { 
                        Debug.WriteLine($"[AddPreviousStudy][GetCurrentConclusion] Exception: {ex.Message}");
                        return string.Empty; 
                    }
                });
                
                var f2Task = Task.Run(async () => 
                {
                    try 
                    { 
                        return await _pacs.GetCurrentFindings2Async() ?? string.Empty;
                    } 
                    catch (Exception ex) 
                    { 
                        Debug.WriteLine($"[AddPreviousStudy][GetCurrentFindings2] Exception: {ex.Message}");
                        return string.Empty; 
                    }
                });
                
                var c2Task = Task.Run(async () => 
                {
                    try 
                    { 
                        return await _pacs.GetCurrentConclusion2Async() ?? string.Empty;
                    } 
                    catch (Exception ex) 
                    { 
                        Debug.WriteLine($"[AddPreviousStudy][GetCurrentConclusion2] Exception: {ex.Message}");
                        return string.Empty; 
                    }
                });
                
                // Wait for all to complete (max time = slowest getter, not sum of all)
                await Task.WhenAll(f1Task, f2Task, c1Task, c2Task);
                
                // Pick the longer result from each pair
                string f1 = f1Task.Result;
                string f2 = f2Task.Result;
                string c1 = c1Task.Result;
                string c2 = c2Task.Result;
                
                findings = (f2.Length > f1.Length) ? f2 : f1;
                conclusion = (c2.Length > c1.Length) ? c2 : c1;
                
                Debug.WriteLine($"[AddPreviousStudy] Primary findings: {f1.Length}ch, Alternate findings: {f2.Length}ch ¡æ Using: {findings.Length}ch");
                Debug.WriteLine($"[AddPreviousStudy] Primary conclusion: {c1.Length}ch, Alternate conclusion: {c2.Length}ch ¡æ Using: {conclusion.Length}ch");
                SetStatus($"Report text captured (findings={findings.Length}ch, conclusion={conclusion.Length}ch)");

                // 5) Persist as previous study via existing repo methods
                if (_studyRepo == null) 
                {
                    sw.Stop();
                    Debug.WriteLine($"[AddPreviousStudyModule] ===== ABORTED: Repository unavailable ===== ({sw.ElapsedMilliseconds} ms)");
                    return;
                }
                
                var studyId = await _studyRepo.EnsureStudyAsync(PatientNumber, PatientName, PatientSex, null, studyName, studyDt);
                if (studyId == null) 
                { 
                    sw.Stop();
                    Debug.WriteLine($"[AddPreviousStudyModule] ===== ABORTED: Study save failed ===== ({sw.ElapsedMilliseconds} ms)");
                    SetStatus("Study save failed", true); 
                    return; 
                }

                // NEW: Only save report if it doesn't already exist (or if report datetime is different)
                Debug.WriteLine($"[AddPreviousStudyModule] Saving new report with datetime: {reportDt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "(null)"}");
                var reportObj = new { header_and_findings = findings, final_conclusion = conclusion };
                string json = JsonSerializer.Serialize(reportObj);
                await _studyRepo.UpsertPartialReportAsync(studyId.Value, reportDt, json, isMine: false);
                Debug.WriteLine($"[AddPreviousStudyModule] Report saved successfully");

                // Fire-and-forget reload to avoid blocking the sequence (OpenStudy can run immediately)
                async void ReloadAndSelectAsync()
                {
                    try
                    {
                        await LoadPreviousStudiesForPatientAsync(PatientNumber);
                        var newTabLocal = PreviousStudies.FirstOrDefault(t => t.StudyDateTime == studyDt);
                        if (newTabLocal != null) SelectedPreviousStudy = newTabLocal;
                    }
                    catch (Exception ex) { Debug.WriteLine("[AddPreviousStudyModule][Reload] " + ex.Message); }
                }
                ReloadAndSelectAsync();
                
                // Append simplified study string to current report's Comparison field
                try
                {
                    Debug.WriteLine("[AddPreviousStudyModule] Appending to Comparison field");
                    var modality = ExtractModality(studyName);
                    var dateStr = studyDt.ToString("yyyy-MM-dd");
                    var simplifiedStudy = $"{modality} {dateStr}";
                    Debug.WriteLine($"[AddPreviousStudyModule] Simplified string: '{simplifiedStudy}'");
                    
                    // Append to existing Comparison with proper separator
                    var currentComparison = Comparison ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(currentComparison))
                    {
                        Comparison = simplifiedStudy;
                        Debug.WriteLine($"[AddPreviousStudyModule] Set Comparison to: '{simplifiedStudy}'");
                    }
                    else
                    {
                        // Append with comma separator
                        Comparison = currentComparison.TrimEnd() + ", " + simplifiedStudy;
                        Debug.WriteLine($"[AddPreviousStudyModule] Appended to Comparison: '{Comparison}'");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[AddPreviousStudyModule] Comparison append error: {ex.Message}");
                    // Don't fail the entire operation if comparison append fails
                }
                
                sw.Stop();
                Debug.WriteLine($"[AddPreviousStudyModule] ===== END: SUCCESS ===== ({sw.ElapsedMilliseconds} ms)");
                SetStatus("Previous study added");
            }
            catch (Exception ex)
            {
                sw.Stop();
                Debug.WriteLine($"[AddPreviousStudyModule] ===== EXCEPTION ===== ({sw.ElapsedMilliseconds} ms)");
                Debug.WriteLine("[AddPreviousStudyModule] error: " + ex.Message);
                SetStatus("Add previous study module failed", true);
            }
        }

        private void OnSelectPrevious(object? o)
        {
            if (o is not PreviousStudyTab tab) return;
            if (SelectedPreviousStudy?.Id == tab.Id)
            {
                foreach (var t in PreviousStudies) t.IsSelected = (t.Id == tab.Id);
                return;
            }
            SelectedPreviousStudy = tab;
        }

        private void OnGenerateField(object? param)
        {
            try
            {
                var key = (param as string) ?? string.Empty;
                SetStatus(string.IsNullOrWhiteSpace(key) ? "Generate requested" : $"Generate {key} requested");
            }
            catch { }
        }

        private void OnEditStudyTechnique()
        {
            try
            {
                // Open window to edit technique combination for current study
                Views.StudyTechniqueWindow.OpenForStudy(this);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[EditStudyTechnique] Error: {ex.Message}");
                SetStatus("Failed to open study technique editor", true);
            }
        }

        private void OnEditComparison()
        {
            try
            {
                Debug.WriteLine("[EditComparison] Opening Edit Comparison window");
                
                // Check if we have patient info and previous studies
                if (string.IsNullOrWhiteSpace(PatientNumber))
                {
                    SetStatus("No patient loaded - cannot edit comparison", true);
                    return;
                }
                
                if (PreviousStudies.Count == 0)
                {
                    SetStatus("No previous studies available for this patient", true);
                    return;
                }
                
                // Open the Edit Comparison window and get the updated comparison string
                var newComparison = Views.EditComparisonWindow.Open(
                    PatientNumber,
                    PatientName,
                    PatientSex,
                    PreviousStudies.ToList(),
                    Comparison
                );
                
                // Update comparison if user clicked OK
                if (newComparison != null)
                {
                    Comparison = newComparison;
                    SetStatus("Comparison updated");
                    Debug.WriteLine($"[EditComparison] Updated comparison: '{newComparison}'");
                }
                else
                {
                    Debug.WriteLine("[EditComparison] User cancelled");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[EditComparison] Error: {ex.Message}");
                SetStatus("Failed to open comparison editor", true);
            }
        }

        private void OnSavePreorder()
        {
            try
            {
                Debug.WriteLine("[SavePreorder] Saving current findings to findings_preorder JSON field");
                
                // Get the raw findings text (unreportified)
                var findingsText = RawFindingsText;
                
                if (string.IsNullOrWhiteSpace(findingsText))
                {
                    SetStatus("No findings text available to save as preorder", true);
                    Debug.WriteLine("[SavePreorder] Findings text is empty");
                    return;
                }
                
                Debug.WriteLine($"[SavePreorder] Captured findings text: length={findingsText.Length} chars");
                
                // Save to FindingsPreorder property (which will trigger JSON update)
                FindingsPreorder = findingsText;
                
                SetStatus($"Pre-order findings saved ({findingsText.Length} chars)");
                Debug.WriteLine("[SavePreorder] Successfully saved to FindingsPreorder property");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SavePreorder] Error: {ex.Message}");
                SetStatus("Save preorder operation failed", true);
            }
        }

        private void OnSavePreviousStudyToDB()
        {
            // Reuse the existing RunSavePreviousStudyToDBAsync implementation
            _ = RunSavePreviousStudyToDBAsync();
        }

        // Internal AutomationSettings class for deserialization
        private sealed class AutomationSettings
        {
            public string? NewStudySequence { get; set; }
            public string? AddStudySequence { get; set; }
            public string? ShortcutOpenNew { get; set; }
            public string? ShortcutOpenAdd { get; set; }
            public string? ShortcutOpenAfterOpen { get; set; }
            public string? SendReportSequence { get; set; }
            public string? SendReportPreviewSequence { get; set; }
            public string? ShortcutSendReportPreview { get; set; }
            public string? ShortcutSendReportReportified { get; set; }
            public string? TestSequence { get; set; }
        }

        // Helper to get automation sequence for current PACS
        private string GetAutomationSequenceForCurrentPacs(Func<AutomationSettings, string?> selector)
        {
            try
            {
                var pacsKey = _tenant?.CurrentPacsKey ?? "default_pacs";
                var automationFile = GetAutomationFilePath(pacsKey);
                
                if (!System.IO.File.Exists(automationFile))
                {
                    Debug.WriteLine($"[GetAutomationSequence] No automation file found at {automationFile}");
                    return string.Empty;
                }
                
                var json = System.IO.File.ReadAllText(automationFile);
                var settings = System.Text.Json.JsonSerializer.Deserialize<AutomationSettings>(json);
                
                if (settings == null)
                {
                    Debug.WriteLine($"[GetAutomationSequence] Failed to deserialize automation settings");
                    return string.Empty;
                }
                
                var sequence = selector(settings);
                Debug.WriteLine($"[GetAutomationSequence] PACS={pacsKey}, Sequence='{sequence}'");
                return sequence ?? string.Empty;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GetAutomationSequence] Error: {ex.Message}");
                return string.Empty;
            }
        }

        private static string GetAutomationFilePath(string pacsKey)
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return System.IO.Path.Combine(appData, "Wysg.Musm", "Radium", "Pacs", SanitizeFileName(pacsKey), "automation.json");
        }

        private static string SanitizeFileName(string name)
        {
            var invalid = System.IO.Path.GetInvalidFileNameChars();
            return string.Join("_", name.Split(invalid, StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.');
        }

        // DelegateCommand helper class
        private sealed class DelegateCommand : ICommand
        {
            private readonly Action<object?> _execute;
            private readonly Predicate<object?>? _canExecute;

            public DelegateCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
            {
                _execute = execute ?? throw new ArgumentNullException(nameof(execute));
                _canExecute = canExecute;
            }

            public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

            public void Execute(object? parameter) => _execute(parameter);

            public event EventHandler? CanExecuteChanged
            {
                add { CommandManager.RequerySuggested += value; }
                remove { CommandManager.RequerySuggested -= value; }
            }

            public void RaiseCanExecuteChanged() => CommandManager.InvalidateRequerySuggested();
        }
    }
}
