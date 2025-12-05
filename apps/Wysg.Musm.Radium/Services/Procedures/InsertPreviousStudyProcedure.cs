using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using Wysg.Musm.Radium.ViewModels;

namespace Wysg.Musm.Radium.Services.Procedures
{
    /// <summary>
    /// Implementation of the InsertPreviousStudyReport built-in module.
    /// This module inserts a previous study report to the database using MainViewModel temporary properties.
    /// 
    /// Required Variables (set by Custom Procedures via "Set" operations before calling this module):
    /// - Current Patient Number (MainViewModel.PatientNumber)
    /// - Previous Study Studyname (TempPreviousStudyStudyname)
    /// - Previous Study Datetime (TempPreviousStudyDatetime)
    /// - Previous Study Report Datetime (TempPreviousStudyReportDatetime)
    /// 
    /// Optional Variables:
    /// - Previous Study Report Header and Findings (TempPreviousStudyReportHeaderAndFindings)
    /// - Previous Study Report Conclusion (TempPreviousStudyReportConclusion)
    /// - Previous Study Report Reporter (TempPreviousStudyReportReporter)
    /// 
    /// Database Operation:
    /// 1. Validates all required variables are present
    /// 2. Ensures patient record exists (or creates it)
    /// 3. Ensures studyname record exists (or creates it)
    /// 4. Ensures study record exists in med.rad_study (or creates it)
    /// 5. Inserts report to med.rad_report ONLY if it doesn't already exist (skips if exists)
    /// 
    /// ON CONFLICT behavior:
    /// - Study: If exists (same patient_id, studyname_id, study_datetime), does UPDATE (no-op), returns existing study_id
    /// - Report: If exists (same study_id, report_datetime), SKIPS (does not update or insert)
    /// </summary>
    public sealed class InsertPreviousStudyProcedure : IInsertPreviousStudyProcedure
    {
        private readonly IRadStudyRepository? _studyRepo;

        public InsertPreviousStudyProcedure(IRadStudyRepository? studyRepo)
        {
            _studyRepo = studyRepo;
        }

        public async Task ExecuteAsync(MainViewModel vm)
        {
            Debug.WriteLine("[InsertPreviousStudyReport] ===== START =====");

            try
            {
                // Validate study repository
                if (_studyRepo == null)
                {
                    Debug.WriteLine("[InsertPreviousStudyReport] ERROR: Study repository is null");
                    vm.SetStatusInternal("InsertPreviousStudyReport: Study repository not available", true);
                    return;
                }

                // Get required variables from MainViewModel properties
                string patientNumber = vm.PatientNumber ?? string.Empty;
                string? studyname = vm.TempPreviousStudyStudyname;
                DateTime? studyDateTime = vm.TempPreviousStudyDatetime;
                DateTime? reportDateTime = vm.TempPreviousStudyReportDatetime;

                Debug.WriteLine($"[InsertPreviousStudyReport] Current Patient Number: '{patientNumber}'");
                Debug.WriteLine($"[InsertPreviousStudyReport] Previous Study Studyname: '{studyname}'");
                Debug.WriteLine($"[InsertPreviousStudyReport] Previous Study Datetime: {studyDateTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "(null)"}");
                Debug.WriteLine($"[InsertPreviousStudyReport] Previous Study Report Datetime: {reportDateTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "(null)"}");

                // Validate required variables
                if (string.IsNullOrWhiteSpace(patientNumber))
                {
                    Debug.WriteLine("[InsertPreviousStudyReport] VALIDATION FAILED: Current Patient Number is empty");
                    vm.SetStatusInternal("InsertPreviousStudyReport: Current Patient Number is required", true);
                    return;
                }

                if (string.IsNullOrWhiteSpace(studyname))
                {
                    Debug.WriteLine("[InsertPreviousStudyReport] VALIDATION FAILED: Previous Study Studyname is empty");
                    vm.SetStatusInternal("InsertPreviousStudyReport: Previous Study Studyname is required", true);
                    return;
                }

                if (!studyDateTime.HasValue)
                {
                    Debug.WriteLine("[InsertPreviousStudyReport] VALIDATION FAILED: Previous Study Datetime is empty");
                    vm.SetStatusInternal("InsertPreviousStudyReport: Previous Study Datetime is required", true);
                    return;
                }

                if (!reportDateTime.HasValue)
                {
                    Debug.WriteLine("[InsertPreviousStudyReport] VALIDATION FAILED: Previous Study Report Datetime is empty");
                    vm.SetStatusInternal("InsertPreviousStudyReport: Previous Study Report Datetime is required", true);
                    return;
                }

                Debug.WriteLine($"[InsertPreviousStudyReport] All required variables validated");

                // Get optional patient metadata and report content
                string? patientName = vm.PatientName;
                string? patientSex = vm.PatientSex;
                string headerAndFindings = vm.TempPreviousStudyReportHeaderAndFindings ?? string.Empty;
                string conclusion = vm.TempPreviousStudyReportConclusion ?? string.Empty;
                string reporter = vm.TempPreviousStudyReportReporter ?? string.Empty;
                
                Debug.WriteLine($"[InsertPreviousStudyReport] Patient name: '{patientName}'");
                Debug.WriteLine($"[InsertPreviousStudyReport] Patient sex: '{patientSex}'");
                Debug.WriteLine($"[InsertPreviousStudyReport] Header and findings length: {headerAndFindings.Length}");
                Debug.WriteLine($"[InsertPreviousStudyReport] Conclusion length: {conclusion.Length}");
                Debug.WriteLine($"[InsertPreviousStudyReport] Reporter: '{reporter}'");

                // Step 1: Ensure study exists in database
                Debug.WriteLine("[InsertPreviousStudyReport] Step 1: Ensuring study exists in database...");
                var studyId = await _studyRepo.EnsureStudyAsync(
                    patientNumber: patientNumber,
                    patientName: patientName,
                    sex: patientSex,
                    birthDateRaw: null,  // Not available
                    studyName: studyname,
                    studyDateTime: studyDateTime.Value
                );

                if (!studyId.HasValue || studyId.Value == 0)
                {
                    Debug.WriteLine("[InsertPreviousStudyReport] FAILED: EnsureStudyAsync returned null or 0");
                    vm.SetStatusInternal("InsertPreviousStudyReport: Failed to ensure study exists (database error)", true);
                    return;
                }
                
                Debug.WriteLine($"[InsertPreviousStudyReport] Study ID: {studyId.Value}");

                // Step 2: Build report JSON (same format as AddPreviousStudy module)
                Debug.WriteLine("[InsertPreviousStudyReport] Step 2: Building report JSON...");
                var reportJson = JsonSerializer.Serialize(new
                {
                    header_and_findings = headerAndFindings,
                    final_conclusion = conclusion,
                    report_radiologist = reporter,  // FIX: Use report_radiologist to match loader expectations
                    chief_complaint = string.Empty,
                    patient_history = string.Empty,
                    study_techniques = string.Empty,
                    comparison = string.Empty
                });
                
                Debug.WriteLine($"[InsertPreviousStudyReport] Report JSON length: {reportJson.Length}");

                // Step 3: Insert report to database ONLY if it doesn't already exist
                Debug.WriteLine("[InsertPreviousStudyReport] Step 3: Inserting report to database (if not exists)...");
                var (reportId, wasInserted) = await _studyRepo.InsertReportIfNotExistsAsync(
                    studyId: studyId.Value,
                    reportDateTime: reportDateTime.Value,
                    reportJson: reportJson,
                    isMine: false  // Previous studies are not "mine"
                );

                if (wasInserted && reportId.HasValue && reportId.Value > 0)
                {
                    Debug.WriteLine($"[InsertPreviousStudyReport] SUCCESS: Report inserted (study_id={studyId.Value}, report_id={reportId.Value})");
                    vm.SetStatusInternal($"InsertPreviousStudyReport: Report inserted (study_id={studyId.Value}, report_id={reportId.Value})");
                }
                else if (!wasInserted && reportId.HasValue)
                {
                    Debug.WriteLine($"[InsertPreviousStudyReport] SKIPPED: Report already exists (study_id={studyId.Value}, report_id={reportId.Value})");
                    vm.SetStatusInternal($"InsertPreviousStudyReport: Report already exists, skipped (report_id={reportId.Value})");
                }
                else if (!wasInserted)
                {
                    Debug.WriteLine($"[InsertPreviousStudyReport] SKIPPED: Report already exists (study_id={studyId.Value})");
                    vm.SetStatusInternal($"InsertPreviousStudyReport: Report already exists, skipped");
                }
                else
                {
                    Debug.WriteLine("[InsertPreviousStudyReport] FAILED: InsertReportIfNotExistsAsync returned unexpected result");
                    vm.SetStatusInternal("InsertPreviousStudyReport: Failed to insert report (database error)", true);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[InsertPreviousStudyReport] EXCEPTION: {ex.GetType().Name} - {ex.Message}");
                Debug.WriteLine($"[InsertPreviousStudyReport] StackTrace: {ex.StackTrace}");
                vm.SetStatusInternal($"InsertPreviousStudyReport: Error - {ex.Message}", true);
            }
            finally
            {
                Debug.WriteLine("[InsertPreviousStudyReport] ===== END =====");
            }
        }
    }
}
