using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Wysg.Musm.Radium.Services;

namespace Wysg.Musm.Radium.ViewModels
{
    /// <summary>
    /// Partial: Current study metadata (patient demographics + live PACS selection fetch)
    /// - Maintains patient / study identification fields
    /// - Fetches from PACS via PacsService with lightweight retry for transient UIA failures
    /// - Persists patient + study record via repository (if available)
    /// </summary>
    public partial class MainViewModel
    {
        // ---------------- Current Study Fields ----------------
        private string _patientName = string.Empty; public string PatientName { get => _patientName; set { if (SetProperty(ref _patientName, value)) UpdateCurrentStudyLabel(); } }
        private string _patientNumber = string.Empty; public string PatientNumber { get => _patientNumber; set { if (SetProperty(ref _patientNumber, value)) UpdateCurrentStudyLabel(); } }
        private string _patientSex = string.Empty; public string PatientSex { get => _patientSex; set { if (SetProperty(ref _patientSex, value)) UpdateCurrentStudyLabel(); } }
        private string _patientAge = string.Empty; public string PatientAge { get => _patientAge; set { if (SetProperty(ref _patientAge, value)) UpdateCurrentStudyLabel(); } }
        private string _studyName = string.Empty; public string StudyName { get => _studyName; set { if (SetProperty(ref _studyName, value)) UpdateCurrentStudyLabel(); } }
        private string _studyDateTime = string.Empty; public string StudyDateTime { get => _studyDateTime; set { if (SetProperty(ref _studyDateTime, value)) UpdateCurrentStudyLabel(); } }

        // Report DateTime: when the current study's report was created (distinct from StudyDateTime which is when the study was performed)
        private DateTime? _currentReportDateTime; 
        public DateTime? CurrentReportDateTime 
        { 
            get => _currentReportDateTime; 
            set => SetProperty(ref _currentReportDateTime, value); 
        }

        private string _currentStudyLabel = "Current\nStudy"; public string CurrentStudyLabel { get => _currentStudyLabel; private set => SetProperty(ref _currentStudyLabel, value); }

        private void UpdateCurrentStudyLabel()
        {
            string fmt(string s) => string.IsNullOrWhiteSpace(s) ? "?" : s.Trim();
            string dt = StudyDateTime;
            if (!string.IsNullOrWhiteSpace(dt) && DateTime.TryParse(dt, out var parsed)) dt = parsed.ToString("yyyy-MM-dd HH:mm:ss");
            else if (string.IsNullOrWhiteSpace(dt)) dt = "?";
            CurrentStudyLabel = $"{fmt(PatientName)}({fmt(PatientSex)}/{fmt(PatientAge)}) - {fmt(PatientNumber)}\n{fmt(StudyName)} ({dt})";
        }

        // ---------------- PACS Fetch ----------------
        private async Task FetchCurrentStudyAsync()
        {
            try
            {
                // Start PACS queries concurrently
                var nameTask = _pacs.GetSelectedNameFromSearchResultsAsync();
                var numberTask = _pacs.GetSelectedIdFromSearchResultsAsync();
                var sexTask = _pacs.GetSelectedSexFromSearchResultsAsync();
                var ageTask = _pacs.GetSelectedAgeFromSearchResultsAsync();
                var studyNameTask = _pacs.GetSelectedStudynameFromSearchResultsAsync();
                var dtTask = _pacs.GetSelectedStudyDateTimeFromSearchResultsAsync();
                var birthTask = _pacs.GetSelectedBirthDateFromSearchResultsAsync();

                await Task.WhenAll(nameTask, numberTask, sexTask, ageTask, studyNameTask, dtTask); // birthTask awaited separately

                PatientName = nameTask.Result ?? string.Empty;
                PatientNumber = numberTask.Result ?? string.Empty;

                // Retry patient number if empty (transient UIA value loss) - up to 5 attempts with short delay
                if (string.IsNullOrWhiteSpace(PatientNumber))
                {
                    for (int attempt = 1; attempt <= 5 && string.IsNullOrWhiteSpace(PatientNumber); attempt++)
                    {
                        await Task.Delay(150);
                        try
                        {
                            var retry = await _pacs.GetSelectedIdFromSearchResultsAsync();
                            if (!string.IsNullOrWhiteSpace(retry))
                            {
                                PatientNumber = retry;
                                Debug.WriteLine($"[Retry] PatientNumber recovered on attempt {attempt}");
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[Retry] PatientNumber attempt {attempt} failed: {ex.Message}");
                        }
                    }
                }

                PatientSex = sexTask.Result ?? string.Empty;
                PatientAge = ageTask.Result ?? string.Empty;
                StudyName = studyNameTask.Result ?? string.Empty;
                StudyDateTime = dtTask.Result ?? string.Empty;

                await LoadPreviousStudiesForPatientAsync(PatientNumber);
                var birth = await birthTask; // ensure done
                await PersistCurrentStudyAsync(birth);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[FetchCurrentStudy] error: " + ex.Message);
            }
        }
        
        /// <summary>
        /// Public method to reload previous studies for a patient.
        /// Used by EditComparisonWindow to refresh modality after LOINC mapping changes.
        /// </summary>
        public async Task LoadPreviousStudiesAsync(string patientNumber)
        {
            if (string.IsNullOrWhiteSpace(patientNumber))
            {
                Debug.WriteLine("[MainViewModel] LoadPreviousStudiesAsync: empty patient number");
                return;
            }
            
            Debug.WriteLine($"[MainViewModel] LoadPreviousStudiesAsync: loading for patient {patientNumber}");
            await LoadPreviousStudiesForPatientAsync(patientNumber);
        }

        // Persistence of patient/study basic metadata
        private async Task PersistCurrentStudyAsync(string? birthDate = null)
        {
            try
            {
                if (_studyRepo == null) return;
                if (!DateTime.TryParse(StudyDateTime, out var dt)) dt = DateTime.MinValue;
                await _studyRepo.EnsurePatientStudyAsync(PatientNumber, PatientName, PatientSex, birthDate, StudyName, dt == DateTime.MinValue ? null : dt);
            }
            catch { }
        }
    }
}
