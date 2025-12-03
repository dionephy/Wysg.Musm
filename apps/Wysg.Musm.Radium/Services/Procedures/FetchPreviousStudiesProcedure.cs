using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Wysg.Musm.Radium.ViewModels;

namespace Wysg.Musm.Radium.Services.Procedures
{
    /// <summary>
    /// Fetches all previous studies for current patient from PostgreSQL database (excluding current study).
    /// Populates PreviousReportEditorPanel with study tabs and report combo boxes.
    /// Selects specific study/report based on temp variables if provided.
    /// </summary>
    public sealed class FetchPreviousStudiesProcedure : IFetchPreviousStudiesProcedure
    {
        public async Task ExecuteAsync(MainViewModel vm)
        {
            if (vm == null)
            {
                Debug.WriteLine("[FetchPreviousStudiesProcedure] MainViewModel is null");
                return;
            }

            Debug.WriteLine("[FetchPreviousStudiesProcedure] ===== START =====");
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Step 1: Validate PatientNumber is present
                var patientNumber = vm.PatientNumber;
                if (string.IsNullOrWhiteSpace(patientNumber))
                {
                    vm.SetStatusInternal("FetchPreviousStudies: Patient number is required", true);
                    Debug.WriteLine("[FetchPreviousStudiesProcedure] Patient number is empty - aborting");
                    return;
                }

                Debug.WriteLine($"[FetchPreviousStudiesProcedure] Patient number: '{patientNumber}'");

                // Step 2: Load all previous studies from database
                Debug.WriteLine("[FetchPreviousStudiesProcedure] Loading previous studies from database...");
                await vm.LoadPreviousStudiesAsync(patientNumber);
                
                int studyCount = vm.PreviousStudies.Count;
                Debug.WriteLine($"[FetchPreviousStudiesProcedure] Loaded {studyCount} previous studies");

                if (studyCount == 0)
                {
                    stopwatch.Stop();
                    vm.SetStatusInternal($"FetchPreviousStudies: No previous studies found for patient ({stopwatch.ElapsedMilliseconds} ms)");
                    Debug.WriteLine("[FetchPreviousStudiesProcedure] ===== END: NO STUDIES FOUND =====");
                    return;
                }

                // Step 3: Try to select specific study/report if temp variables are set
                string? targetStudyname = vm.TempPreviousStudyStudyname;
                DateTime? targetStudyDatetime = vm.TempPreviousStudyDatetime;
                DateTime? targetReportDatetime = vm.TempPreviousStudyReportDatetime;

                Debug.WriteLine($"[FetchPreviousStudiesProcedure] Target studyname: '{targetStudyname ?? "(not set)"}'");
                Debug.WriteLine($"[FetchPreviousStudiesProcedure] Target study datetime: {targetStudyDatetime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "(not set)"}");
                Debug.WriteLine($"[FetchPreviousStudiesProcedure] Target report datetime: {targetReportDatetime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "(not set)"}");

                // Find matching study tab by examining the collection
                var selectedTab = false;

                // Try to find matching study tab
                if (!string.IsNullOrWhiteSpace(targetStudyname) && targetStudyDatetime.HasValue)
                {
                    Debug.WriteLine("[FetchPreviousStudiesProcedure] Searching for matching study tab...");

                    foreach (var tab in vm.PreviousStudies)
                    {
                        var tabStudyname = tab.SelectedReport?.Studyname;
                        var tabStudyDateTime = tab.StudyDateTime;

                        if (string.Equals(tabStudyname, targetStudyname, StringComparison.OrdinalIgnoreCase) &&
                            Math.Abs((tabStudyDateTime - targetStudyDatetime.Value).TotalSeconds) < 60) // 1-minute tolerance
                        {
                            Debug.WriteLine($"[FetchPreviousStudiesProcedure] Found matching study tab: {tab.Title}");

                            // Try to find matching report within the tab
                            if (targetReportDatetime.HasValue)
                            {
                                Debug.WriteLine("[FetchPreviousStudiesProcedure] Searching for matching report in tab...");

                                foreach (var report in tab.Reports)
                                {
                                    if (report.ReportDateTime.HasValue &&
                                        Math.Abs((report.ReportDateTime.Value - targetReportDatetime.Value).TotalSeconds) < 1)
                                    {
                                        Debug.WriteLine($"[FetchPreviousStudiesProcedure] Found matching report: {report.ReportDateTime:yyyy-MM-dd HH:mm:ss}");
                                        tab.SelectedReport = report;

                                        // Update tab fields from selected report
                                        tab.Findings = report.Findings;
                                        tab.Conclusion = report.Conclusion;
                                        tab.OriginalFindings = report.Findings;
                                        tab.OriginalConclusion = report.Conclusion;

                                        Debug.WriteLine("[FetchPreviousStudiesProcedure] Tab fields updated from selected report");
                                        break;
                                    }
                                }
                            }

                            vm.SelectedPreviousStudy = tab;
                            Debug.WriteLine($"[FetchPreviousStudiesProcedure] Selected target tab: {tab.Title}");
                            selectedTab = true;
                            break;
                        }
                    }

                    if (!selectedTab)
                    {
                        Debug.WriteLine($"[FetchPreviousStudiesProcedure] No matching study tab found for '{targetStudyname}' @ {targetStudyDatetime:yyyy-MM-dd HH:mm:ss}");
                    }
                }
                else
                {
                    Debug.WriteLine("[FetchPreviousStudiesProcedure] Target variables not set - will use first tab");
                }

                // Step 4: Select first tab if no specific target was found
                if (!selectedTab && vm.PreviousStudies.Count > 0)
                {
                    vm.SelectedPreviousStudy = vm.PreviousStudies.First();
                    Debug.WriteLine($"[FetchPreviousStudiesProcedure] Selected first tab (default): {vm.SelectedPreviousStudy.Title}");
                }

                // Step 5: Set PreviousReportSplitted to true to show the panel
                vm.PreviousReportSplitted = true;
                Debug.WriteLine("[FetchPreviousStudiesProcedure] PreviousReportSplitted set to true");

                stopwatch.Stop();

                string selectionMsg = selectedTab
                    ? "selected matching study"
                    : vm.PreviousStudies.Count > 0
                        ? "defaulted to first study"
                        : "no selection";

                vm.SetStatusInternal($"FetchPreviousStudies: Loaded {studyCount} studies, {selectionMsg} ({stopwatch.ElapsedMilliseconds} ms)");
                Debug.WriteLine($"[FetchPreviousStudiesProcedure] ===== END: SUCCESS ===== ({stopwatch.ElapsedMilliseconds} ms)");
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Debug.WriteLine($"[FetchPreviousStudiesProcedure] ERROR: {ex.Message}");
                Debug.WriteLine($"[FetchPreviousStudiesProcedure] StackTrace: {ex.StackTrace}");
                vm.SetStatusInternal($"FetchPreviousStudies error: {ex.Message}", true);
            }
        }
    }
}
