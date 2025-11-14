using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Wysg.Musm.Radium.ViewModels
{
    /// <summary>
    /// Partial: AddPreviousStudy automation module implementation.
    /// </summary>
    public partial class MainViewModel
    {
        private async Task RunAddPreviousStudyModuleAsync()
        {
            try
            {
                Debug.WriteLine("[AddPreviousStudyModule] ===== START =====");
                var stopwatch = Stopwatch.StartNew();

                // REMEMBER: Capture the currently selected previous study BEFORE loading new one
                // This will be used to update Comparison field later
                var previouslySelectedStudy = SelectedPreviousStudy;
                Debug.WriteLine($"[AddPreviousStudyModule] Previously selected study: {previouslySelectedStudy?.Title ?? "(none)"}");

                // NEW STEP 0: Validate Related Studies list patient number matches current study
                Debug.WriteLine("[AddPreviousStudyModule] Step 0: Validating Related Studies patient number...");
                var relatedStudiesPatientNumber = await _pacs.GetSelectedIdFromRelatedStudiesAsync();
                if (string.IsNullOrWhiteSpace(relatedStudiesPatientNumber))
                {
                    SetStatus("AddPreviousStudy: Could not read patient number from Related Studies list", true);
                    return;
                }

                // Inline normalization (remove non-alphanumeric, uppercase)
                string Normalize(string? s) => string.IsNullOrWhiteSpace(s) ? string.Empty : 
                    System.Text.RegularExpressions.Regex.Replace(s, "[^A-Za-z0-9]", "").ToUpperInvariant();

                var normalizedRelatedStudies = Normalize(relatedStudiesPatientNumber);
                var normalizedCurrent = Normalize(PatientNumber);
                
                Debug.WriteLine($"[AddPreviousStudyModule] Related Studies patient number (raw): '{relatedStudiesPatientNumber}'");
                Debug.WriteLine($"[AddPreviousStudyModule] Related Studies patient number (normalized): '{normalizedRelatedStudies}'");
                Debug.WriteLine($"[AddPreviousStudyModule] Current study patient number (normalized): '{normalizedCurrent}'");
                
                if (!string.Equals(normalizedRelatedStudies, normalizedCurrent, StringComparison.OrdinalIgnoreCase))
                {
                    SetStatus($"AddPreviousStudy: Patient number mismatch - Related Studies patient ({normalizedRelatedStudies}) does not match current study patient ({normalizedCurrent})", true);
                    Debug.WriteLine($"[AddPreviousStudyModule] ABORT: Patient mismatch");
                    return;
                }
                
                Debug.WriteLine($"[AddPreviousStudyModule] Patient number validated: {normalizedCurrent}");

                // Step 1: Validate current patient matches PACS
                Debug.WriteLine("[AddPreviousStudyModule] Step 1: Validating patient match...");
                var pacsPatientNumber = await _pacs.GetCurrentPatientNumberAsync();
                if (string.IsNullOrWhiteSpace(pacsPatientNumber))
                {
                    SetStatus("AddPreviousStudy: Could not read patient number from PACS", true);
                    return;
                }

                var normalizedPacs = Normalize(pacsPatientNumber);
                var normalizedApp = Normalize(PatientNumber);
                if (!string.Equals(normalizedPacs, normalizedApp, StringComparison.OrdinalIgnoreCase))
                {
                    SetStatus($"AddPreviousStudy: Patient mismatch (PACS={normalizedPacs}, App={normalizedApp})", true);
                    return;
                }
                Debug.WriteLine($"[AddPreviousStudyModule] Patient match confirmed: {normalizedApp}");

                // Step 2: Read selected study metadata from Related Studies list
                Debug.WriteLine("[AddPreviousStudyModule] Step 2: Reading study metadata...");
                var studynameTask = _pacs.GetSelectedStudynameFromRelatedStudiesAsync();
                var studyDateTimeTask = _pacs.GetSelectedStudyDateTimeFromRelatedStudiesAsync();
                var radiologistTask = _pacs.GetSelectedRadiologistFromRelatedStudiesAsync();
                var reportDateTimeTask = _pacs.GetSelectedReportDateTimeFromRelatedStudiesAsync();

                await Task.WhenAll(studynameTask, studyDateTimeTask, radiologistTask, reportDateTimeTask);

                var studyname = studynameTask.Result;
                var studyDateTimeStr = studyDateTimeTask.Result;
                var radiologist = radiologistTask.Result;
                var reportDateTimeStr = reportDateTimeTask.Result;

                if (string.IsNullOrWhiteSpace(studyname) || string.IsNullOrWhiteSpace(studyDateTimeStr))
                {
                    SetStatus("AddPreviousStudy: Missing studyname or study datetime", true);
                    return;
                }

                if (!DateTime.TryParse(studyDateTimeStr, out var studyDateTime))
                {
                    SetStatus($"AddPreviousStudy: Invalid study datetime: {studyDateTimeStr}", true);
                    return;
                }

                // NEW VALIDATION: Check if this is the CURRENT study (not a previous study)
                // Compare with the current study loaded in the application
                bool isCurrentStudy = false;
                if (!string.IsNullOrWhiteSpace(StudyName) && !string.IsNullOrWhiteSpace(StudyDateTime))
                {
                    if (string.Equals(studyname, StudyName, StringComparison.OrdinalIgnoreCase))
                    {
                        // Same studyname - now check datetime
                        if (DateTime.TryParse(StudyDateTime, out var currentStudyDt))
                        {
                            // Consider same if within 1 minute tolerance (account for slight timing differences)
                            if (Math.Abs((studyDateTime - currentStudyDt).TotalSeconds) < 60)
                            {
                                isCurrentStudy = true;
                                Debug.WriteLine($"[AddPreviousStudyModule] VALIDATION FAILED: Selected study is the CURRENT study");
                                Debug.WriteLine($"[AddPreviousStudyModule] Current: {StudyName} @ {StudyDateTime}");
                                Debug.WriteLine($"[AddPreviousStudyModule] Selected: {studyname} @ {studyDateTimeStr}");
                            }
                        }
                    }
                }

                if (isCurrentStudy)
                {
                    SetStatus("AddPreviousStudy: Cannot add current study as previous study (select a different study from Related Studies list)", true);
                    return;
                }

                // Parse report datetime from PACS
                DateTime? reportDateTime = null;
                if (!string.IsNullOrWhiteSpace(reportDateTimeStr) && DateTime.TryParse(reportDateTimeStr, out var reportDt))
                {
                    reportDateTime = reportDt;
                    Debug.WriteLine($"[AddPreviousStudyModule] Report datetime from PACS: {reportDateTime:yyyy-MM-dd HH:mm:ss}");
                }
                else
                {
                    Debug.WriteLine($"[AddPreviousStudyModule] No report datetime from PACS (reportDateTimeStr='{reportDateTimeStr}')");
                }

                // NEW VALIDATION: Don't save/load if report doesn't have datetime
                if (!reportDateTime.HasValue)
                {
                    Debug.WriteLine($"[AddPreviousStudyModule] VALIDATION FAILED: Report datetime is missing");
                    Debug.WriteLine($"[AddPreviousStudyModule] Study: {studyname} @ {studyDateTimeStr}");
                    Debug.WriteLine($"[AddPreviousStudyModule] Report datetime from PACS: '{reportDateTimeStr}'");
                    SetStatus("AddPreviousStudy: Report datetime is missing - report not saved (this is expected for studies without finalized reports)", false);
                    return;
                }

                // FIXED: Check for duplicate in memory, considering BOTH study datetime AND report datetime
                Debug.WriteLine("[AddPreviousStudyModule] Step 2.5: Checking for duplicate in-memory tabs...");
                
                PreviousStudyTab? duplicate = null;
                if (reportDateTime.HasValue)
                {
                    // If we have a report datetime, check for exact match (study + report datetime)
                    duplicate = PreviousStudies.FirstOrDefault(tab =>
                        string.Equals(tab.SelectedReport?.Studyname, studyname, StringComparison.OrdinalIgnoreCase) &&
                        tab.StudyDateTime == studyDateTime &&
                        tab.SelectedReport?.ReportDateTime.HasValue == true &&
                        Math.Abs((tab.SelectedReport.ReportDateTime.Value - reportDateTime.Value).TotalSeconds) < 1);
                    
                    if (duplicate != null)
                    {
                        Debug.WriteLine($"[AddPreviousStudyModule] Exact duplicate found (same study + same report datetime): {duplicate.Title}");
                    }
                    else
                    {
                        Debug.WriteLine($"[AddPreviousStudyModule] No exact duplicate in memory - checking if study has different reports loaded");
                        
                        // Check if study exists with different report datetime
                        var existingStudyWithDifferentReport = PreviousStudies.FirstOrDefault(tab =>
                            string.Equals(tab.SelectedReport?.Studyname, studyname, StringComparison.OrdinalIgnoreCase) &&
                            tab.StudyDateTime == studyDateTime);
                        
                        if (existingStudyWithDifferentReport != null)
                        {
                            var existingReportDt = existingStudyWithDifferentReport.SelectedReport?.ReportDateTime;
                            Debug.WriteLine($"[AddPreviousStudyModule] Study exists in memory with different report datetime: {existingReportDt:yyyy-MM-dd HH:mm:ss}");
                            Debug.WriteLine($"[AddPreviousStudyModule] Will check database and potentially fetch new report");
                        }
                    }
                }
                else
                {
                    // No report datetime available - fall back to old behavior (check study only)
                    duplicate = PreviousStudies.FirstOrDefault(tab =>
                        string.Equals(tab.SelectedReport?.Studyname, studyname, StringComparison.OrdinalIgnoreCase) &&
                        tab.StudyDateTime == studyDateTime);
                    
                    if (duplicate != null)
                    {
                        Debug.WriteLine($"[AddPreviousStudyModule] Duplicate found (no report datetime available): {duplicate.Title}");
                    }
                }

                if (duplicate != null)
                {
                    Debug.WriteLine($"[AddPreviousStudyModule] Duplicate confirmed: {duplicate.Title}");
                    SelectedPreviousStudy = duplicate;
                    PreviousReportSplitted = true;
                    
                    // NEW: Update comparison field if there was a previously selected study
                    await UpdateComparisonFromPreviousStudyAsync(previouslySelectedStudy);
                    
                    stopwatch.Stop();
                    SetStatus($"Previous study already loaded: {duplicate.Title} ({stopwatch.ElapsedMilliseconds} ms)", false);
                    Debug.WriteLine($"[AddPreviousStudyModule] ===== END: DUPLICATE DETECTED ===== ({stopwatch.ElapsedMilliseconds} ms)");
                    return;
                }

                // Check if study exists in database and if this specific report exists
                Debug.WriteLine("[AddPreviousStudyModule] Step 2.6: Checking database for existing study and report...");
                long? existingStudyId = null;
                bool reportExistsInDb = false;

                if (_studyRepo != null)
                {
                    // Check if study exists
                    existingStudyId = await _studyRepo.GetStudyIdAsync(PatientNumber, studyname, studyDateTime);
                    
                    if (existingStudyId.HasValue && reportDateTime.HasValue)
                    {
                        // Study exists - now check if this specific report exists
                        Debug.WriteLine($"[AddPreviousStudyModule] Study exists with ID: {existingStudyId.Value}");
                        
                        // Get all reports for this study to check if this report datetime already exists
                        var existingReports = await _studyRepo.GetReportsForPatientAsync(PatientNumber);
                        reportExistsInDb = existingReports.Any(r => 
                            r.StudyId == existingStudyId.Value && 
                            r.ReportDateTime.HasValue && 
                            Math.Abs((r.ReportDateTime.Value - reportDateTime.Value).TotalSeconds) < 1);
                        
                        if (reportExistsInDb)
                        {
                            Debug.WriteLine($"[AddPreviousStudyModule] Report with datetime {reportDateTime:yyyy-MM-dd HH:mm:ss} already exists in database");
                        }
                        else
                        {
                            Debug.WriteLine($"[AddPreviousStudyModule] Study exists but report with datetime {reportDateTime:yyyy-MM-dd HH:mm:ss} NOT found - will save new report");
                        }
                    }
                    else if (existingStudyId.HasValue)
                    {
                        Debug.WriteLine($"[AddPreviousStudyModule] Study exists with ID: {existingStudyId.Value}, but no report datetime from PACS");
                    }
                    else
                    {
                        Debug.WriteLine($"[AddPreviousStudyModule] Study does NOT exist in database - will create new study");
                    }
                }

                // Step 3: Read report text (only if we need to save it to DB)
                bool needsReportFetch = !reportExistsInDb; // Fetch if report doesn't exist or if study doesn't exist
                         
                string findings = string.Empty;
                string conclusion = string.Empty;

                if (needsReportFetch)
                {
                    Debug.WriteLine("[AddPreviousStudyModule] Step 3: Reading report text from PACS...");
                    
                    // Run all getters in parallel
                    var f1Task = Task.Run(async () => await _pacs.GetCurrentFindingsAsync());
                    var f2Task = Task.Run(async () => await _pacs.GetCurrentFindings2Async());
                    var c1Task = Task.Run(async () => await _pacs.GetCurrentConclusionAsync());
                    var c2Task = Task.Run(async () => await _pacs.GetCurrentConclusion2Async());

                    await Task.WhenAll(f1Task, f2Task, c1Task, c2Task);

                    var f1 = f1Task.Result ?? string.Empty;
                    var f2 = f2Task.Result ?? string.Empty;
                    var c1 = c1Task.Result ?? string.Empty;
                    var c2 = c2Task.Result ?? string.Empty;

                    // Pick the longer variant for each field
                    findings = (f2.Length > f1.Length) ? f2 : f1;
                    conclusion = (c2.Length > c1.Length) ? c2 : c1;

                    Debug.WriteLine($"[AddPreviousStudyModule] Findings length: {findings.Length} (used {(f2.Length > f1.Length ? "GetCurrentFindings2" : "GetCurrentFindings")})");
                    Debug.WriteLine($"[AddPreviousStudyModule] Conclusion length: {conclusion.Length} (used {(c2.Length > c1.Length ? "GetCurrentConclusion2" : "GetCurrentConclusion")})");
                }
                else
                {
                    Debug.WriteLine("[AddPreviousStudyModule] Step 3: Skipping report text fetch (report already exists in DB)");
                }

                // Step 4: Persist to database (create study if needed, save new report if needed)
                if (_studyRepo != null && needsReportFetch)
                {
                    Debug.WriteLine("[AddPreviousStudyModule] Step 4: Persisting to database...");
                    
                    // Ensure study exists (will return existing ID if already exists)
                    long studyId = existingStudyId ?? 0;
                    if (studyId == 0)
                    {
                        var newStudyId = await _studyRepo.EnsureStudyAsync(
                            PatientNumber, 
                            PatientName, 
                            PatientSex, 
                            birthDateRaw: null, 
                            studyname, 
                            studyDateTime);
                        
                        if (newStudyId.HasValue)
                        {
                            studyId = newStudyId.Value;
                            Debug.WriteLine($"[AddPreviousStudyModule] Created new study with ID: {studyId}");
                        }
                    }

                    if (studyId > 0)
                    {
                        // Save the new report
                        var reportJson = JsonSerializer.Serialize(new
                        {
                            header_and_findings = findings,
                            final_conclusion = conclusion,
                            chief_complaint = string.Empty,
                            patient_history = string.Empty,
                            study_techniques = string.Empty,
                            comparison = string.Empty
                        });

                        await _studyRepo.UpsertPartialReportAsync(studyId, reportDateTime, reportJson, isMine: false);
                        Debug.WriteLine($"[AddPreviousStudyModule] Saved new report to database (study_id={studyId}, report_datetime={reportDateTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "null"})");
                    }
                }
                else if (!needsReportFetch)
                {
                    Debug.WriteLine("[AddPreviousStudyModule] Step 4: Skipping database save (report already exists)");
                }

                // Step 5: Load previous studies and select the new/existing tab
                Debug.WriteLine("[AddPreviousStudyModule] Step 5: Loading previous studies...");
                await LoadPreviousStudiesForPatientAsync(PatientNumber);
                
                // FIXED: When selecting the tab, try to find the one with matching report datetime
                PreviousStudyTab? newTab = null;
                if (reportDateTime.HasValue)
                {
                    // Try to find tab with matching report datetime
                    newTab = PreviousStudies.FirstOrDefault(tab =>
                        string.Equals(tab.SelectedReport?.Studyname, studyname, StringComparison.OrdinalIgnoreCase) &&
                        tab.StudyDateTime == studyDateTime &&
                        tab.SelectedReport?.ReportDateTime.HasValue == true &&
                        Math.Abs((tab.SelectedReport.ReportDateTime.Value - reportDateTime.Value).TotalSeconds) < 1);
                    
                    if (newTab != null)
                    {
                        Debug.WriteLine($"[AddPreviousStudyModule] Found tab with matching report datetime: {newTab.Title}");
                    }
                    else
                    {
                        Debug.WriteLine($"[AddPreviousStudyModule] No tab found with matching report datetime, trying study-only match");
                    }
                }
                
                // Fallback: find by study datetime only
                if (newTab == null)
                {
                    newTab = PreviousStudies.FirstOrDefault(tab =>
                        string.Equals(tab.SelectedReport?.Studyname, studyname, StringComparison.OrdinalIgnoreCase) &&
                        tab.StudyDateTime == studyDateTime);
                    
                    if (newTab != null)
                    {
                        Debug.WriteLine($"[AddPreviousStudyModule] Found tab with matching study datetime: {newTab.Title}");
                    }
                }

                if (newTab != null)
                {
                    SelectedPreviousStudy = newTab;
                    PreviousReportSplitted = true;
                    
                    // NEW: Update comparison field if there was a previously selected study
                    await UpdateComparisonFromPreviousStudyAsync(previouslySelectedStudy);
                    
                    stopwatch.Stop();
                    
                    string action = existingStudyId.HasValue && !reportExistsInDb ? "New report saved" : 
                                   !existingStudyId.HasValue ? "New study created" : 
                                   "Existing report loaded";
                    SetStatus($"{action}: {newTab.Title} ({stopwatch.ElapsedMilliseconds} ms)", false);
                    Debug.WriteLine($"[AddPreviousStudyModule] ===== END: SUCCESS ===== ({stopwatch.ElapsedMilliseconds} ms)");
                }
                else
                {
                    stopwatch.Stop();
                    SetStatus($"Previous study processed but tab not found: {studyname} ({stopwatch.ElapsedMilliseconds} ms)", true);
                    Debug.WriteLine($"[AddPreviousStudyModule] ===== END: TAB NOT FOUND ===== ({stopwatch.ElapsedMilliseconds} ms)");
                }
            }
            catch (Exception ex)
            {
                SetStatus($"AddPreviousStudy error: {ex.Message}", true);
                Debug.WriteLine($"[AddPreviousStudyModule] ===== ERROR: {ex.Message} =====");
            }
        }

        /// <summary>
        /// Updates the Comparison field from a previously selected previous study.
        /// Skips update if current study is XR and "Do not update header in XR" setting is checked.
        /// </summary>
        private async Task UpdateComparisonFromPreviousStudyAsync(PreviousStudyTab? previousStudy)
        {
            await Task.CompletedTask; // Make method async-compatible
            
            if (previousStudy == null)
            {
                Debug.WriteLine("[UpdateComparisonFromPreviousStudy] No previously selected study - skipping comparison update");
                return;
            }
            
            try
            {
                Debug.WriteLine($"[UpdateComparisonFromPreviousStudy] Previously selected study: {previousStudy.Title}");
                
                // Check if current study is XR modality
                bool isXRModality = false;
                if (!string.IsNullOrWhiteSpace(StudyName))
                {
                    // Extract modality from studyname (typically first word or first few characters)
                    var studyNameUpper = StudyName.ToUpperInvariant();
                    isXRModality = studyNameUpper.StartsWith("XR") || studyNameUpper.Contains(" XR ");
                    Debug.WriteLine($"[UpdateComparisonFromPreviousStudy] Current study modality check: isXR={isXRModality}, StudyName='{StudyName}'");
                }
                
                // Check "Do not update header in XR" setting
                bool doNotUpdateHeaderInXR = false;
                if (_localSettings != null)
                {
                    var settingValue = _localSettings.DoNotUpdateHeaderInXR;
                    doNotUpdateHeaderInXR = string.Equals(settingValue, "true", StringComparison.OrdinalIgnoreCase);
                    Debug.WriteLine($"[UpdateComparisonFromPreviousStudy] DoNotUpdateHeaderInXR setting: '{settingValue}' -> {doNotUpdateHeaderInXR}");
                }
                
                // Skip update if XR and setting is enabled
                if (isXRModality && doNotUpdateHeaderInXR)
                {
                    Debug.WriteLine("[UpdateComparisonFromPreviousStudy] Skipping comparison update - XR modality and DoNotUpdateHeaderInXR is enabled");
                    SetStatus("Comparison not updated (XR modality with 'Do not update header in XR' enabled)");
                    return;
                }
                
                // Build comparison string from previous study
                // Format: "{Modality} {Date}"
                var comparisonText = $"{previousStudy.Modality} {previousStudy.StudyDateTime:yyyy-MM-dd}";
                Debug.WriteLine($"[UpdateComparisonFromPreviousStudy] Built comparison text: '{comparisonText}'");
                
                // Update Comparison property
                Comparison = comparisonText;
                Debug.WriteLine($"[UpdateComparisonFromPreviousStudy] Updated Comparison property: '{Comparison}'");
                SetStatus($"Comparison updated: {comparisonText}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[UpdateComparisonFromPreviousStudy] ERROR: {ex.Message}");
                Debug.WriteLine($"[UpdateComparisonFromPreviousStudy] StackTrace: {ex.StackTrace}");
                // Don't throw - allow AddPreviousStudy to continue even if comparison update fails
            }
        }
    }
}
