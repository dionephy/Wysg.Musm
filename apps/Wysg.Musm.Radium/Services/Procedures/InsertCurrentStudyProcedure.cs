using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Wysg.Musm.Radium.ViewModels;

namespace Wysg.Musm.Radium.Services.Procedures
{
    /// <summary>
    /// Implementation for InsertCurrentStudy built-in module.
    /// Ensures the current study metadata exists in the local med.rad_study table.
    /// </summary>
    public sealed class InsertCurrentStudyProcedure : IInsertCurrentStudyProcedure
    {
        private readonly IRadStudyRepository? _studyRepo;

        public InsertCurrentStudyProcedure(IRadStudyRepository? studyRepo)
        {
            _studyRepo = studyRepo;
        }

        public async Task ExecuteAsync(MainViewModel vm)
        {
            Debug.WriteLine("[InsertCurrentStudy] ===== START =====");

            try
            {
                if (_studyRepo == null)
                {
                    vm.SetStatusInternal("InsertCurrentStudy: Study repository not available", true);
                    Debug.WriteLine("[InsertCurrentStudy] ERROR: Study repository is null");
                    return;
                }

                var patientNumber = vm.PatientNumber?.Trim() ?? string.Empty;
                var studyName = vm.StudyName?.Trim();
                var studyDateTimeRaw = vm.StudyDateTime?.Trim();

                if (string.IsNullOrWhiteSpace(patientNumber))
                {
                    vm.SetStatusInternal("InsertCurrentStudy: Current Patient Number is required", true);
                    Debug.WriteLine("[InsertCurrentStudy] VALIDATION FAILED: Patient number empty");
                    return;
                }

                if (string.IsNullOrWhiteSpace(studyName))
                {
                    vm.SetStatusInternal("InsertCurrentStudy: Current Study Studyname is required", true);
                    Debug.WriteLine("[InsertCurrentStudy] VALIDATION FAILED: Studyname empty");
                    return;
                }

                if (!DateTime.TryParse(studyDateTimeRaw, out var studyDateTime))
                {
                    vm.SetStatusInternal("InsertCurrentStudy: Current Study Datetime is required", true);
                    Debug.WriteLine("[InsertCurrentStudy] VALIDATION FAILED: Invalid study datetime");
                    return;
                }

                var studyId = await _studyRepo.EnsureStudyAsync(
                    patientNumber: patientNumber,
                    patientName: vm.PatientName,
                    sex: vm.PatientSex,
                    birthDateRaw: null,
                    studyName: studyName,
                    studyDateTime: studyDateTime);

                if (studyId.HasValue && studyId.Value > 0)
                {
                    vm.SetStatusInternal($"InsertCurrentStudy: Study ensured (study_id={studyId.Value})");
                    Debug.WriteLine($"[InsertCurrentStudy] SUCCESS: study_id={studyId.Value}");
                }
                else
                {
                    vm.SetStatusInternal("InsertCurrentStudy: Failed to ensure study exists (database error)", true);
                    Debug.WriteLine("[InsertCurrentStudy] FAILED: EnsureStudyAsync returned null");
                }
            }
            catch (Exception ex)
            {
                vm.SetStatusInternal($"InsertCurrentStudy: Error - {ex.Message}", true);
                Debug.WriteLine($"[InsertCurrentStudy] EXCEPTION: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                Debug.WriteLine("[InsertCurrentStudy] ===== END =====");
            }
        }
    }
}
