using System;
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
    }
}
