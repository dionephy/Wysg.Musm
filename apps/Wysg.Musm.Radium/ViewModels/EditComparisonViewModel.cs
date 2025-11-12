using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using Wysg.Musm.Infrastructure.ViewModels;
using Wysg.Musm.Radium.Services;

namespace Wysg.Musm.Radium.ViewModels
{
    /// <summary>
    /// ViewModel for the Edit Comparison window - manages previous studies selection for comparison field.
    /// </summary>
    public class EditComparisonViewModel : BaseViewModel
    {
        private readonly IStudynameLoincRepository _studynameRepo;
        private readonly string _patientNumber;
        private readonly string _patientName;
        private readonly string _patientSex;
        
        public EditComparisonViewModel(IStudynameLoincRepository studynameRepo, string patientNumber, string patientName, string patientSex, List<MainViewModel.PreviousStudyTab> existingStudies, string currentComparison)
        {
            _studynameRepo = studynameRepo;
            _patientNumber = patientNumber;
            _patientName = patientName;
            _patientSex = patientSex;
            
            AvailableStudies = new ObservableCollection<ComparisonStudyItem>();
            SelectedStudies = new ObservableCollection<ComparisonStudyItem>();
            
            AddStudyCommand = new RelayCommand(p =>
            {
                if (p is ComparisonStudyItem study && !study.IsSelected)
                {
                    study.IsSelected = true;
                    SelectedStudies.Add(study);
                    UpdateComparisonString();
                }
            });
            
            RemoveStudyCommand = new RelayCommand(p =>
            {
                if (p is ComparisonStudyItem study && study.IsSelected)
                {
                    study.IsSelected = false;
                    SelectedStudies.Remove(study);
                    UpdateComparisonString();
                }
            });
            
            OpenStudynameMapCommand = new RelayCommand(async p =>
            {
                if (p is ComparisonStudyItem study && !study.HasLoincMap)
                {
                    Views.StudynameLoincWindow.Open(study.Studyname);
                    
                    // After the window closes, refresh the modality for this study
                    await RefreshModalityForStudyAsync(study);
                }
            });
            
            // Load existing previous studies
            LoadPreviousStudies(existingStudies, currentComparison);
        }
        
        public ObservableCollection<ComparisonStudyItem> AvailableStudies { get; }
        public ObservableCollection<ComparisonStudyItem> SelectedStudies { get; }
        
        private string _comparisonString = string.Empty;
        public string ComparisonString
        {
            get => _comparisonString;
            set => SetProperty(ref _comparisonString, value);
        }
        
        public ICommand AddStudyCommand { get; }
        public ICommand RemoveStudyCommand { get; }
        public ICommand OpenStudynameMapCommand { get; }
        
        private void LoadPreviousStudies(List<MainViewModel.PreviousStudyTab> existingStudies, string currentComparison)
        {
            try
            {
                Debug.WriteLine($"[EditComparison] Loading {existingStudies.Count} previous studies");
                
                // Parse current comparison string to determine which studies are selected
                var selectedModalities = ParseComparisonString(currentComparison);
                Debug.WriteLine($"[EditComparison] Parsed {selectedModalities.Count} selected studies from comparison string");
                
                foreach (var study in existingStudies.OrderByDescending(s => s.StudyDateTime))
                {
                    var item = new ComparisonStudyItem
                    {
                        StudyDateTime = study.StudyDateTime,
                        Modality = study.Modality,
                        Studyname = study.SelectedReport?.Studyname ?? "Unknown",
                        DisplayText = study.Title,
                        HasLoincMap = false // Will check asynchronously
                    };
                    
                    // Check if this study is in the current comparison string
                    var matchKey = $"{study.Modality}|{study.StudyDateTime:yyyy-MM-dd}";
                    if (selectedModalities.Contains(matchKey))
                    {
                        item.IsSelected = true;
                        SelectedStudies.Add(item);
                        Debug.WriteLine($"[EditComparison] Marked as selected: {matchKey}");
                    }
                    
                    AvailableStudies.Add(item);
                }
                
                // Check LOINC maps asynchronously
                _ = CheckLoincMapsAsync();
                
                // Initialize comparison string
                UpdateComparisonString();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[EditComparison] Error loading studies: {ex.Message}");
            }
        }
        
        private HashSet<string> ParseComparisonString(string comparison)
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            
            if (string.IsNullOrWhiteSpace(comparison))
                return result;
            
            try
            {
                // Expected format: "CT 2024-01-15, MR 2024-01-10"
                // Split by comma and parse each entry
                var entries = comparison.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                
                foreach (var entry in entries)
                {
                    var parts = entry.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2)
                    {
                        var modality = parts[0].Trim();
                        var date = parts[1].Trim();
                        var key = $"{modality}|{date}";
                        result.Add(key);
                        Debug.WriteLine($"[EditComparison] Parsed comparison entry: {key}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[EditComparison] Error parsing comparison string: {ex.Message}");
            }
            
            return result;
        }
        
        private async Task CheckLoincMapsAsync()
        {
            try
            {
                var studynames = AvailableStudies.Select(s => s.Studyname).Distinct().ToArray();
                Debug.WriteLine($"[EditComparison] Checking LOINC maps for {studynames.Length} unique studynames");
                
                var mappedStudynames = await _studynameRepo.GetMappedStudynamesAsync();
                var mappedSet = new HashSet<string>(mappedStudynames.Select(m => m.Studyname), StringComparer.OrdinalIgnoreCase);
                
                foreach (var study in AvailableStudies)
                {
                    study.HasLoincMap = mappedSet.Contains(study.Studyname);
                }
                
                Debug.WriteLine($"[EditComparison] LOINC map check completed");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[EditComparison] Error checking LOINC maps: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Refreshes the modality for a study after LOINC mapping has been added.
        /// </summary>
        private async Task RefreshModalityForStudyAsync(ComparisonStudyItem study)
        {
            try
            {
                Debug.WriteLine($"[EditComparison] Refreshing modality for studyname: '{study.Studyname}'");
                
                // Get the studyname ID
                var studynames = await _studynameRepo.GetStudynamesAsync();
                var studynameRow = studynames.FirstOrDefault(s => 
                    string.Equals(s.Studyname, study.Studyname, StringComparison.OrdinalIgnoreCase));
                
                if (studynameRow == null)
                {
                    Debug.WriteLine($"[EditComparison] Studyname '{study.Studyname}' not found in database");
                    return;
                }
                
                // Get the mappings for this studyname
                var mappings = await _studynameRepo.GetMappingsAsync(studynameRow.Id);
                
                // Check if mapping now exists
                if (mappings.Any())
                {
                    study.HasLoincMap = true;
                    
                    // Get all parts to lookup part details
                    var parts = await _studynameRepo.GetPartsAsync();
                    var partsByNumber = parts.ToDictionary(p => p.PartNumber, p => p);
                    
                    // Find the modality part
                    var modalityMapping = mappings.FirstOrDefault(m => 
                    {
                        if (partsByNumber.TryGetValue(m.PartNumber, out var part))
                        {
                            return part.PartTypeName.Equals("Rad.Modality.Modality Type", StringComparison.OrdinalIgnoreCase);
                        }
                        return false;
                    });
                    
                    if (modalityMapping != null && partsByNumber.TryGetValue(modalityMapping.PartNumber, out var modalityPart))
                    {
                        var newModality = ExtractModalityFromPartName(modalityPart.PartName);
                        
                        if (!string.IsNullOrEmpty(newModality) && newModality != study.Modality)
                        {
                            Debug.WriteLine($"[EditComparison] Updating modality from '{study.Modality}' to '{newModality}' for studyname '{study.Studyname}'");
                            study.Modality = newModality;
                            
                            // Update display text
                            study.DisplayText = $"{study.Modality} {study.StudyDateTime:yyyy-MM-dd}";
                            
                            // Update comparison string if this study is selected
                            if (study.IsSelected)
                            {
                                UpdateComparisonString();
                            }
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"[EditComparison] No modality part found in mapping for '{study.Studyname}', keeping '{study.Modality}'");
                    }
                }
                else
                {
                    Debug.WriteLine($"[EditComparison] No mapping found for studyname '{study.Studyname}' after window close");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[EditComparison] Error refreshing modality: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Extracts modality abbreviation from LOINC part name.
        /// </summary>
        private string ExtractModalityFromPartName(string partName)
        {
            if (string.IsNullOrWhiteSpace(partName))
                return "OT";
            
            // Common LOINC modality patterns
            if (partName.Contains("CT", StringComparison.OrdinalIgnoreCase))
                return "CT";
            if (partName.Contains("MR", StringComparison.OrdinalIgnoreCase) || partName.Contains("Magnetic", StringComparison.OrdinalIgnoreCase))
                return "MR";
            if (partName.Contains("US", StringComparison.OrdinalIgnoreCase) || partName.Contains("Ultrasound", StringComparison.OrdinalIgnoreCase))
                return "US";
            if (partName.Contains("XR", StringComparison.OrdinalIgnoreCase) || partName.Contains("X-ray", StringComparison.OrdinalIgnoreCase))
                return "XR";
            if (partName.Contains("NM", StringComparison.OrdinalIgnoreCase) || partName.Contains("Nuclear", StringComparison.OrdinalIgnoreCase))
                return "NM";
            if (partName.Contains("PET", StringComparison.OrdinalIgnoreCase))
                return "PT";
            if (partName.Contains("Mammography", StringComparison.OrdinalIgnoreCase))
                return "MG";
            if (partName.Contains("Fluoroscopy", StringComparison.OrdinalIgnoreCase))
                return "RF";
            
            return "OT"; // Other
        }
        
        private void UpdateComparisonString()
        {
            try
            {
                // Generate comparison string from selected studies
                // Format: "{Modality} {Date}" separated by ", "
                var entries = SelectedStudies
                    .OrderByDescending(s => s.StudyDateTime)
                    .Select(s => $"{s.Modality} {s.StudyDateTime:yyyy-MM-dd}")
                    .ToList();
                
                ComparisonString = string.Join(", ", entries);
                Debug.WriteLine($"[EditComparison] Updated comparison string: '{ComparisonString}'");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[EditComparison] Error updating comparison string: {ex.Message}");
                ComparisonString = string.Empty;
            }
        }
        
        public class ComparisonStudyItem : BaseViewModel
        {
            private DateTime _studyDateTime;
            public DateTime StudyDateTime
            {
                get => _studyDateTime;
                set => SetProperty(ref _studyDateTime, value);
            }
            
            private string _modality = string.Empty;
            public string Modality
            {
                get => _modality;
                set => SetProperty(ref _modality, value);
            }
            
            private string _studyname = string.Empty;
            public string Studyname
            {
                get => _studyname;
                set => SetProperty(ref _studyname, value);
            }
            
            private string _displayText = string.Empty;
            public string DisplayText
            {
                get => _displayText;
                set => SetProperty(ref _displayText, value);
            }
            
            private bool _isSelected;
            public bool IsSelected
            {
                get => _isSelected;
                set => SetProperty(ref _isSelected, value);
            }
            
            private bool _hasLoincMap;
            public bool HasLoincMap
            {
                get => _hasLoincMap;
                set => SetProperty(ref _hasLoincMap, value);
            }
        }
        
        private sealed class RelayCommand : ICommand
        {
            private readonly Func<object?, Task> _executeAsync;
            private readonly Action<object?>? _execute;
            private readonly Predicate<object?>? _canExecute;
            
            public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
            {
                _execute = execute ?? throw new ArgumentNullException(nameof(execute));
                _executeAsync = null;
                _canExecute = canExecute;
            }
            
            public RelayCommand(Func<object?, Task> executeAsync, Predicate<object?>? canExecute = null)
            {
                _executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
                _execute = null;
                _canExecute = canExecute;
            }
            
            public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;
            
            public void Execute(object? parameter)
            {
                if (_executeAsync != null)
                {
                    _ = _executeAsync(parameter);
                }
                else
                {
                    _execute?.Invoke(parameter);
                }
            }
            
            public event EventHandler? CanExecuteChanged
            {
                add { CommandManager.RequerySuggested += value; }
                remove { CommandManager.RequerySuggested -= value; }
            }
        }
    }
}
