using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Wysg.Musm.Radium.ViewModels;

namespace Wysg.Musm.Radium.Services.Procedures
{
    /// <summary>
    /// Implementation of the InsertPreviousStudy built-in module.
    /// This module inserts a previous study record to the database using MainViewModel temporary properties.
    /// 
    /// Required Properties (set by Custom Procedures before calling this module):
    /// - TempPreviousStudyPatientNumber (from "Current Patient Number" variable)
    /// - TempPreviousStudyStudyname (from "Previous Study Studyname" variable)
    /// - TempPreviousStudyDatetime (from "Previous Study Datetime" variable, parsed from string)
    /// 
    /// Database Operation:
    /// - Ensures patient record exists (or creates it)
    /// - Ensures studyname record exists (or creates it)
    /// - Inserts study record to med.rad_study (if doesn't exist)
    /// 
    /// ON CONFLICT behavior:
    /// - If study already exists (same patient_id, studyname_id, study_datetime), does UPDATE (no-op)
    /// - Returns existing study_id
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
            Debug.WriteLine("[InsertPreviousStudyProcedure] ===== START =====");

            try
            {
                // Validate study repository
                if (_studyRepo == null)
                {
                    Debug.WriteLine("[InsertPreviousStudyProcedure] ERROR: Study repository is null");
                    vm.SetStatusInternal("InsertPreviousStudy: Study repository not available", true);
                    return;
                }

                // Get variables from MainViewModel properties
                // These should be set by Custom Procedures using Set operations before calling this module
                string patientNumber = vm.PatientNumber ?? string.Empty;
                string? studyname = vm.TempPreviousStudyStudyname;
                DateTime? studyDateTime = vm.TempPreviousStudyDatetime;

                Debug.WriteLine($"[InsertPreviousStudyProcedure] Patient Number: '{patientNumber}' (from MainViewModel.PatientNumber)");
                Debug.WriteLine($"[InsertPreviousStudyProcedure] Previous Study Studyname: '{studyname}' (from Temp property)");
                Debug.WriteLine($"[InsertPreviousStudyProcedure] Previous Study Datetime: {studyDateTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "(null)"} (from Temp property)");

                // Validate required variables
                if (string.IsNullOrWhiteSpace(patientNumber))
                {
                    Debug.WriteLine("[InsertPreviousStudyProcedure] FAILED: Current Patient Number is empty");
                    vm.SetStatusInternal("InsertPreviousStudy: Current Patient Number is required", true);
                    return;
                }

                if (string.IsNullOrWhiteSpace(studyname))
                {
                    Debug.WriteLine("[InsertPreviousStudyProcedure] FAILED: Previous Study Studyname is empty");
                    vm.SetStatusInternal("InsertPreviousStudy: Previous Study Studyname is required", true);
                    return;
                }

                if (!studyDateTime.HasValue)
                {
                    Debug.WriteLine("[InsertPreviousStudyProcedure] FAILED: Previous Study Datetime is empty");
                    vm.SetStatusInternal("InsertPreviousStudy: Previous Study Datetime is required", true);
                    return;
                }

                Debug.WriteLine($"[InsertPreviousStudyProcedure] All required variables present");

                // Get optional patient metadata from MainViewModel
                string? patientName = vm.PatientName;
                string? patientSex = vm.PatientSex;
                
                Debug.WriteLine($"[InsertPreviousStudyProcedure] Patient name: '{patientName}' (optional)");
                Debug.WriteLine($"[InsertPreviousStudyProcedure] Patient sex: '{patientSex}' (optional)");

                // Insert study to database (creates patient/studyname if needed)
                Debug.WriteLine("[InsertPreviousStudyProcedure] Calling EnsureStudyAsync...");
                var studyId = await _studyRepo.EnsureStudyAsync(
                    patientNumber: patientNumber,
                    patientName: patientName,
                    sex: patientSex,
                    birthDateRaw: null,  // Not available
                    studyName: studyname,
                    studyDateTime: studyDateTime.Value
                );

                if (studyId.HasValue && studyId.Value > 0)
                {
                    Debug.WriteLine($"[InsertPreviousStudyProcedure] SUCCESS: Study ID = {studyId.Value}");
                    vm.SetStatusInternal($"InsertPreviousStudy: Study inserted/updated (ID: {studyId.Value})");
                }
                else
                {
                    Debug.WriteLine("[InsertPreviousStudyProcedure] FAILED: EnsureStudyAsync returned null or 0");
                    vm.SetStatusInternal("InsertPreviousStudy: Failed to insert study (database error)", true);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[InsertPreviousStudyProcedure] EXCEPTION: {ex.GetType().Name} - {ex.Message}");
                Debug.WriteLine($"[InsertPreviousStudyProcedure] StackTrace: {ex.StackTrace}");
                vm.SetStatusInternal($"InsertPreviousStudy: Error - {ex.Message}", true);
            }
            finally
            {
                Debug.WriteLine("[InsertPreviousStudyProcedure] ===== END =====");
            }
        }
    }
}
