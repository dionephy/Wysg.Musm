using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using Wysg.Musm.Radium.ViewModels;

namespace Wysg.Musm.Radium.Services.Procedures
{
    /// <summary>
    /// Implementation for InsertCurrentStudyReport built-in module.
    /// Inserts the current study report into med.rad_report when it does not already exist.
    /// </summary>
    public sealed class InsertCurrentStudyReportProcedure : IInsertCurrentStudyReportProcedure
    {
        private readonly IRadStudyRepository? _studyRepo;

        public InsertCurrentStudyReportProcedure(IRadStudyRepository? studyRepo)
        {
            _studyRepo = studyRepo;
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
    }
}
