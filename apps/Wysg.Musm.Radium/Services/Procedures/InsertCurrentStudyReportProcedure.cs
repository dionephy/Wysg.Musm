using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using Wysg.Musm.Radium.Services.ApiClients;
using Wysg.Musm.Radium.ViewModels;

namespace Wysg.Musm.Radium.Services.Procedures
{
    /// <summary>
    /// Implementation for InsertCurrentStudyReport built-in module.
    /// Inserts the current study report into med.rad_report when it does not already exist.
    /// Additionally, if unresolved phrases (red-colored words) are detected in findings or conclusion,
    /// the unreportified text is exported to the central DB (radium.exported_report) via API.
    /// </summary>
    public sealed class InsertCurrentStudyReportProcedure : IInsertCurrentStudyReportProcedure
    {
        private readonly IRadStudyRepository? _studyRepo;
        private readonly IExportedReportsApiClient? _exportedReportsApiClient;
        private readonly ITenantContext? _tenantContext;

        public InsertCurrentStudyReportProcedure(
            IRadStudyRepository? studyRepo,
            IExportedReportsApiClient? exportedReportsApiClient = null,
            ITenantContext? tenantContext = null)
        {
            _studyRepo = studyRepo;
            _exportedReportsApiClient = exportedReportsApiClient;
            _tenantContext = tenantContext;
        }

        public async Task ExecuteAsync(MainViewModel vm)
        {
            Debug.WriteLine("[InsertCurrentStudyReport] ===== START =====");

            try
            {
                if (_studyRepo == null)
                {
                    vm.SetStatusInternal("InsertCurrentStudyReport: Study repository not available", true);
                    Debug.WriteLine("[InsertCurrentStudyReport] ERROR: Study repository is null");
                    return;
                }

                var patientNumber = vm.PatientNumber?.Trim() ?? string.Empty;
                var studyName = vm.StudyName?.Trim();
                var studyDateTimeRaw = vm.StudyDateTime?.Trim();
                var reportDateTime = vm.CurrentReportDateTime;

                if (string.IsNullOrWhiteSpace(patientNumber))
                {
                    vm.SetStatusInternal("InsertCurrentStudyReport: Current Patient Number is required", true);
                    Debug.WriteLine("[InsertCurrentStudyReport] VALIDATION FAILED: Patient number empty");
                    return;
                }

                if (string.IsNullOrWhiteSpace(studyName))
                {
                    vm.SetStatusInternal("InsertCurrentStudyReport: Current Study Studyname is required", true);
                    Debug.WriteLine("[InsertCurrentStudyReport] VALIDATION FAILED: Studyname empty");
                    return;
                }

                if (!DateTime.TryParse(studyDateTimeRaw, out var studyDateTime))
                {
                    vm.SetStatusInternal("InsertCurrentStudyReport: Current Study Datetime is required", true);
                    Debug.WriteLine("[InsertCurrentStudyReport] VALIDATION FAILED: Invalid study datetime");
                    return;
                }

                if (!reportDateTime.HasValue)
                {
                    vm.SetStatusInternal("InsertCurrentStudyReport: Current Study Report Datetime is required", true);
                    Debug.WriteLine("[InsertCurrentStudyReport] VALIDATION FAILED: Report datetime empty");
                    return;
                }

                var studyId = await _studyRepo.EnsureStudyAsync(
                    patientNumber: patientNumber,
                    patientName: vm.PatientName,
                    sex: vm.PatientSex,
                    birthDateRaw: null,
                    studyName: studyName,
                    studyDateTime: studyDateTime);

                if (!studyId.HasValue || studyId.Value == 0)
                {
                    vm.SetStatusInternal("InsertCurrentStudyReport: Failed to ensure study exists (database error)", true);
                    Debug.WriteLine("[InsertCurrentStudyReport] FAILED: EnsureStudyAsync returned null");
                    return;
                }

                var headerAndFindings = vm.ReportedHeaderAndFindings ?? string.Empty;
                var conclusion = vm.ReportedFinalConclusion ?? string.Empty;
                var reporter = vm.ReportRadiologist ?? string.Empty;

                var reportJson = !string.IsNullOrWhiteSpace(vm.CurrentReportJson)
                    ? vm.CurrentReportJson!
                    : JsonSerializer.Serialize(new
                    {
                        header_and_findings = headerAndFindings,
                        final_conclusion = conclusion,
                        report_radiologist = reporter,
                        chief_complaint = vm.ChiefComplaint ?? string.Empty,
                        patient_history = vm.PatientHistory ?? string.Empty,
                        study_techniques = vm.StudyTechniques ?? string.Empty,
                        comparison = vm.Comparison ?? string.Empty
                    });

                var (reportId, wasInserted) = await _studyRepo.InsertReportIfNotExistsAsync(
                    studyId: studyId.Value,
                    reportDateTime: reportDateTime.Value,
                    reportJson: reportJson,
                    isMine: true);

                if (wasInserted && reportId.HasValue && reportId.Value > 0)
                {
                    vm.SetStatusInternal($"InsertCurrentStudyReport: Report inserted (study_id={studyId.Value}, report_id={reportId.Value})");
                    Debug.WriteLine($"[InsertCurrentStudyReport] SUCCESS: study_id={studyId.Value}, report_id={reportId.Value}");
                }
                else if (!wasInserted && reportId.HasValue)
                {
                    vm.SetStatusInternal($"InsertCurrentStudyReport: Report already exists, skipped (report_id={reportId.Value})");
                    Debug.WriteLine($"[InsertCurrentStudyReport] SKIPPED: Existing report id={reportId.Value}");
                }
                else
                {
                    vm.SetStatusInternal("InsertCurrentStudyReport: Report already exists, skipped");
                    Debug.WriteLine("[InsertCurrentStudyReport] SKIPPED: Report already exists (no id returned)");
                }

                // ==================================================================================
                // PHASE 2: Check for unresolved phrases and export to central DB if found
                // Unresolved phrases = words colored RED in editor (not in phrase snapshot)
                // Excludes: digits, dates (YYYY-MM-DD), punctuation-only tokens
                // ==================================================================================
                await CheckAndExportUnresolvedPhrasesAsync(vm, reportDateTime.Value);
            }
            catch (Exception ex)
            {
                vm.SetStatusInternal($"InsertCurrentStudyReport: Error - {ex.Message}", true);
                Debug.WriteLine($"[InsertCurrentStudyReport] EXCEPTION: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                Debug.WriteLine("[InsertCurrentStudyReport] ===== END =====");
            }
        }

        /// <summary>
        /// Checks if findings or conclusion contain unresolved phrases (red-colored words).
        /// If found, exports the unreportified text to central DB (radium.exported_report) via API.
        /// </summary>
        private async Task CheckAndExportUnresolvedPhrasesAsync(MainViewModel vm, DateTime reportDateTime)
        {
            try
            {
                Debug.WriteLine("[InsertCurrentStudyReport] Checking for unresolved phrases...");

                // Get dereportified (raw) text from findings and conclusion
                var (_, findings, conclusion) = vm.GetDereportifiedSections();

                // Check if either findings or conclusion has unresolved phrases
                bool findingsHasUnresolved = vm.HasUnresolvedPhrases(findings);
                bool conclusionHasUnresolved = vm.HasUnresolvedPhrases(conclusion);

                Debug.WriteLine($"[InsertCurrentStudyReport] Findings has unresolved: {findingsHasUnresolved}");
                Debug.WriteLine($"[InsertCurrentStudyReport] Conclusion has unresolved: {conclusionHasUnresolved}");

                if (!findingsHasUnresolved && !conclusionHasUnresolved)
                {
                    Debug.WriteLine("[InsertCurrentStudyReport] No unresolved phrases detected, skipping export");
                    return;
                }

                // Validate dependencies for export
                if (_exportedReportsApiClient == null)
                {
                    Debug.WriteLine("[InsertCurrentStudyReport] ExportedReportsApiClient not available, skipping export");
                    return;
                }

                var accountId = _tenantContext?.AccountId ?? 0;
                if (accountId <= 0)
                {
                    Debug.WriteLine("[InsertCurrentStudyReport] AccountId not available, skipping export");
                    return;
                }

                // Check API availability
                bool apiAvailable = await _exportedReportsApiClient.IsAvailableAsync();
                if (!apiAvailable)
                {
                    Debug.WriteLine("[InsertCurrentStudyReport] ExportedReports API not available, skipping export");
                    vm.SetStatusInternal("InsertCurrentStudyReport: API not available for export", true);
                    return;
                }

                // Get unreportified text for export
                var exportText = vm.GetUnreportifiedTextForExport();
                if (string.IsNullOrWhiteSpace(exportText))
                {
                    Debug.WriteLine("[InsertCurrentStudyReport] No text to export");
                    return;
                }

                // Get list of unresolved words for logging
                var unresolvedFindings = vm.GetUnresolvedWords(findings);
                var unresolvedConclusion = vm.GetUnresolvedWords(conclusion);
                var allUnresolved = new System.Collections.Generic.HashSet<string>(
                    unresolvedFindings.Concat(unresolvedConclusion), 
                    StringComparer.OrdinalIgnoreCase);

                Debug.WriteLine($"[InsertCurrentStudyReport] Unresolved words: {string.Join(", ", allUnresolved)}");

                // Export to central DB via API
                Debug.WriteLine($"[InsertCurrentStudyReport] Exporting unreported text to central DB for account {accountId}...");
                
                var exportedReport = await _exportedReportsApiClient.CreateAsync(
                    accountId: accountId,
                    report: exportText,
                    reportDateTime: reportDateTime);

                Debug.WriteLine($"[InsertCurrentStudyReport] Export SUCCESS: exported_report_id={exportedReport.Id}");
                vm.SetStatusInternal($"InsertCurrentStudyReport: Exported unresolved text (id={exportedReport.Id}, {allUnresolved.Count} unresolved words)");
            }
            catch (Exception ex)
            {
                // Export failure is non-fatal - log and continue
                Debug.WriteLine($"[InsertCurrentStudyReport] Export FAILED: {ex.Message}\n{ex.StackTrace}");
                vm.SetStatusInternal($"InsertCurrentStudyReport: Export failed - {ex.Message}", true);
            }
        }
    }
}
