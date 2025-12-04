using System;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Wysg.Musm.Radium.ViewModels
{
    /// <summary>
    /// Partial: Automation database operations.
    /// Contains methods for saving current and previous studies to the database.
    /// </summary>
    public partial class MainViewModel
    {
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
        
        private async Task RunInsertPreviousStudyAsync()
        {
            if (_insertPreviousStudyProc != null)
            {
                await _insertPreviousStudyProc.ExecuteAsync(this);
            }
            else
            {
                Debug.WriteLine("[Automation][InsertPreviousStudy] Procedure not available");
                SetStatus("InsertPreviousStudy: Module not available", true);
            }
        }
        
        /// <summary>
        /// FetchPreviousStudies automation module.
        /// Fetches all previous studies from PostgreSQL database (excluding current study) and populates the UI.
        /// After loading, selects the study and report matching the temporary variables set by custom procedures.
        /// </summary>
        private async Task RunFetchPreviousStudiesAsync()
        {
            try
            {
                Debug.WriteLine("[FetchPreviousStudies] ===== START =====");
                var stopwatch = Stopwatch.StartNew();
                
                // Ensure patient number is available
                if (string.IsNullOrWhiteSpace(PatientNumber))
                {
                    Debug.WriteLine("[FetchPreviousStudies] FAILED - PatientNumber is empty");
                    SetStatus("FetchPreviousStudies: Patient number is required", true);
                    return;
                }
                
                Debug.WriteLine($"[FetchPreviousStudies] Fetching studies for patient: {PatientNumber}");
                
                // Load all previous studies for the patient from database
                await LoadPreviousStudiesForPatientAsync(PatientNumber);
                
                Debug.WriteLine($"[FetchPreviousStudies] Loaded {PreviousStudies.Count} previous studies");
                
                // If temporary variables are set, select the matching study and report
                if (!string.IsNullOrWhiteSpace(TempPreviousStudyStudyname) && 
                    TempPreviousStudyDatetime.HasValue)
                {
                    Debug.WriteLine($"[FetchPreviousStudies] Attempting to select study: '{TempPreviousStudyStudyname}' @ {TempPreviousStudyDatetime:yyyy-MM-dd HH:mm:ss}");
                    
                    // Find the matching study tab
                    PreviousStudyTab? matchingTab = null;
                    
                    foreach (var tab in PreviousStudies)
                    {
                        // Match by studyname and study datetime
                        var tabStudyname = tab.SelectedReport?.Studyname ?? string.Empty;
                        
                        if (string.Equals(tabStudyname, TempPreviousStudyStudyname, StringComparison.OrdinalIgnoreCase) &&
                            Math.Abs((tab.StudyDateTime - TempPreviousStudyDatetime.Value).TotalSeconds) < 60)
                        {
                            matchingTab = tab;
                            Debug.WriteLine($"[FetchPreviousStudies] Found matching study tab: {tab.Title}");
                            break;
                        }
                    }
                    
                    if (matchingTab != null)
                    {
                        // Select the study tab
                        SelectedPreviousStudy = matchingTab;
                        PreviousReportSplitted = true;
                        
                        Debug.WriteLine($"[FetchPreviousStudies] Selected study tab: {matchingTab.Title}");
                        
                        // If report datetime is specified, select the matching report
                        if (TempPreviousStudyReportDatetime.HasValue)
                        {
                            Debug.WriteLine($"[FetchPreviousStudies] Attempting to select report: {TempPreviousStudyReportDatetime:yyyy-MM-dd HH:mm:ss}");
                            
                            var matchingReport = matchingTab.Reports.FirstOrDefault(r =>
                                r.ReportDateTime.HasValue &&
                                Math.Abs((r.ReportDateTime.Value - TempPreviousStudyReportDatetime.Value).TotalSeconds) < 1);
                            
                            if (matchingReport != null)
                            {
                                matchingTab.SelectedReport = matchingReport;
                                Debug.WriteLine($"[FetchPreviousStudies] Selected report: {matchingReport.Display}");
                            }
                            else
                            {
                                Debug.WriteLine($"[FetchPreviousStudies] No matching report found for datetime {TempPreviousStudyReportDatetime:yyyy-MM-dd HH:mm:ss}");
                                Debug.WriteLine($"[FetchPreviousStudies] Using default report (most recent)");
                            }
                        }
                        else
                        {
                            Debug.WriteLine("[FetchPreviousStudies] No report datetime specified - using default report");
                        }
                        
                        stopwatch.Stop();
                        SetStatus($"Previous studies loaded and selected: {matchingTab.Title} ({stopwatch.ElapsedMilliseconds} ms)");
                        Debug.WriteLine($"[FetchPreviousStudies] ===== END: SUCCESS ===== ({stopwatch.ElapsedMilliseconds} ms)");
                    }
                    else
                    {
                        stopwatch.Stop();
                        SetStatus($"Previous studies loaded but matching study not found: {TempPreviousStudyStudyname} ({stopwatch.ElapsedMilliseconds} ms)", true);
                        Debug.WriteLine($"[FetchPreviousStudies] ===== END: STUDY NOT FOUND ===== ({stopwatch.ElapsedMilliseconds} ms)");
                    }
                }
                else
                {
                    stopwatch.Stop();
                    SetStatus($"Previous studies loaded: {PreviousStudies.Count} studies ({stopwatch.ElapsedMilliseconds} ms)");
                    Debug.WriteLine($"[FetchPreviousStudies] ===== END: LOADED WITHOUT SELECTION ===== ({stopwatch.ElapsedMilliseconds} ms)");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FetchPreviousStudies] ===== ERROR: {ex.Message} =====");
                Debug.WriteLine($"[FetchPreviousStudies] StackTrace: {ex.StackTrace}");
                SetStatus($"FetchPreviousStudies error: {ex.Message}", true);
            }
        }
        
        /// <summary>
        /// SetComparison automation module.
        /// Sets the Comparison field using the temporary variables "Previous Study Studyname" and "Previous Study Datetime".
        /// Extracts modality from the studyname (via LOINC mapping) and formats as "{Modality} {Date}".
        /// Skips update if current study modality is in the ModalitiesNoHeaderUpdate list.
        /// </summary>
        private async Task RunSetComparisonAsync()
        {
            try
            {
                Debug.WriteLine("[SetComparison] ===== START =====");
                var stopwatch = Stopwatch.StartNew();
                
                // Validate required variables
                if (string.IsNullOrWhiteSpace(TempPreviousStudyStudyname))
                {
                    Debug.WriteLine("[SetComparison] FAILED - TempPreviousStudyStudyname is empty");
                    SetStatus("SetComparison: Previous Study Studyname is required", true);
                    return;
                }
                
                if (!TempPreviousStudyDatetime.HasValue)
                {
                    Debug.WriteLine("[SetComparison] FAILED - TempPreviousStudyDatetime is null");
                    SetStatus("SetComparison: Previous Study Datetime is required", true);
                    return;
                }
                
                Debug.WriteLine($"[SetComparison] Using studyname: '{TempPreviousStudyStudyname}'");
                Debug.WriteLine($"[SetComparison] Using datetime: {TempPreviousStudyDatetime:yyyy-MM-dd HH:mm:ss}");
                
                // Extract modality from current study (for ModalitiesNoHeaderUpdate check)
                string? currentModality = null;
                if (!string.IsNullOrWhiteSpace(StudyName))
                {
                    currentModality = await ExtractModalityAsync(StudyName);
                    Debug.WriteLine($"[SetComparison] Current study modality: '{currentModality}', StudyName: '{StudyName}'");
                }
                
                // Check ModalitiesNoHeaderUpdate setting
                bool shouldSkipUpdate = false;
                if (_localSettings != null && !string.IsNullOrWhiteSpace(currentModality))
                {
                    var modalitiesNoHeaderUpdate = _localSettings.ModalitiesNoHeaderUpdate ?? string.Empty;
                    Debug.WriteLine($"[SetComparison] ModalitiesNoHeaderUpdate setting: '{modalitiesNoHeaderUpdate}'");
                    
                    if (!string.IsNullOrWhiteSpace(modalitiesNoHeaderUpdate))
                    {
                        var excludedModalities = modalitiesNoHeaderUpdate
                            .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(m => m.Trim().ToUpperInvariant())
                            .Where(m => !string.IsNullOrEmpty(m))
                            .ToList();
                        
                        shouldSkipUpdate = excludedModalities.Contains(currentModality.ToUpperInvariant());
                        Debug.WriteLine($"[SetComparison] Excluded modalities: [{string.Join(", ", excludedModalities)}]");
                        Debug.WriteLine($"[SetComparison] Should skip update: {shouldSkipUpdate}");
                    }
                }
                
                if (shouldSkipUpdate)
                {
                    Debug.WriteLine($"[SetComparison] Skipping comparison update - modality '{currentModality}' is in ModalitiesNoHeaderUpdate list");
                    SetStatus($"Comparison not updated (modality '{currentModality}' excluded by settings)");
                    stopwatch.Stop();
                    Debug.WriteLine($"[SetComparison] ===== END: SKIPPED ===== ({stopwatch.ElapsedMilliseconds} ms)");
                    return;
                }
                
                // Extract modality from previous study studyname
                var previousModality = await ExtractModalityAsync(TempPreviousStudyStudyname);
                Debug.WriteLine($"[SetComparison] Previous study modality: '{previousModality}'");
                
                // Build comparison string: "{Modality} {Date}"
                var comparisonText = $"{previousModality} {TempPreviousStudyDatetime:yyyy-MM-dd}";
                Debug.WriteLine($"[SetComparison] Built comparison text: '{comparisonText}'");
                
                // Update Comparison property
                Comparison = comparisonText;
                Debug.WriteLine($"[SetComparison] Updated Comparison property: '{Comparison}'");
                
                stopwatch.Stop();
                SetStatus($"Comparison set: {comparisonText} ({stopwatch.ElapsedMilliseconds} ms)");
                Debug.WriteLine($"[SetComparison] ===== END: SUCCESS ===== ({stopwatch.ElapsedMilliseconds} ms)");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SetComparison] ===== ERROR: {ex.Message} =====");
                Debug.WriteLine($"[SetComparison] StackTrace: {ex.StackTrace}");
                SetStatus($"SetComparison error: {ex.Message}", true);
            }
        }
        
        /// <summary>
        /// ClearTempVariables automation module.
        /// Clears all temporary variables used for data exchange between custom procedures and built-in modules.
        /// This includes: Previous Study Studyname, Datetime, Report Datetime, Reporter, Header/Findings, and Conclusion.
        /// </summary>
        private async Task RunClearTempVariablesAsync()
        {
            try
            {
                Debug.WriteLine("[ClearTempVariables] ===== START =====");
                
                // Clear all temporary variables
                TempPreviousStudyStudyname = null;
                TempPreviousStudyDatetime = null;
                TempPreviousStudyReportDatetime = null;
                TempPreviousStudyReportReporter = null;
                TempPreviousStudyReportHeaderAndFindings = null;
                TempPreviousStudyReportConclusion = null;
                
                Debug.WriteLine("[ClearTempVariables] All temporary variables cleared:");
                Debug.WriteLine("[ClearTempVariables] - TempPreviousStudyStudyname = null");
                Debug.WriteLine("[ClearTempVariables] - TempPreviousStudyDatetime = null");
                Debug.WriteLine("[ClearTempVariables] - TempPreviousStudyReportDatetime = null");
                Debug.WriteLine("[ClearTempVariables] - TempPreviousStudyReportReporter = null");
                Debug.WriteLine("[ClearTempVariables] - TempPreviousStudyReportHeaderAndFindings = null");
                Debug.WriteLine("[ClearTempVariables] - TempPreviousStudyReportConclusion = null");
                
                SetStatus("Temporary variables cleared");
                Debug.WriteLine("[ClearTempVariables] ===== END: SUCCESS =====");
                
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ClearTempVariables] ===== ERROR: {ex.Message} =====");
                Debug.WriteLine($"[ClearTempVariables] StackTrace: {ex.StackTrace}");
                SetStatus($"ClearTempVariables error: {ex.Message}", true);
            }
        }
    }
}
