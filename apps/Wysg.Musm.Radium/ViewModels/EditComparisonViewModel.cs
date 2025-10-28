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
            
            OpenStudynameMapCommand = new RelayCommand(p =>
            {
                if (p is ComparisonStudyItem study && !study.HasLoincMap)
                {
                    Views.StudynameLoincWindow.Open(study.Studyname);
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
            public DateTime StudyDateTime { get; set; }
            public string Modality { get; set; } = string.Empty;
            public string Studyname { get; set; } = string.Empty;
            public string DisplayText { get; set; } = string.Empty;
            
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
            private readonly Action<object?> _execute;
            private readonly Predicate<object?>? _canExecute;
            
            public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
            {
                _execute = execute ?? throw new ArgumentNullException(nameof(execute));
                _canExecute = canExecute;
            }
            
            public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;
            public void Execute(object? parameter) => _execute(parameter);
            
            public event EventHandler? CanExecuteChanged
            {
                add { CommandManager.RequerySuggested += value; }
                remove { CommandManager.RequerySuggested -= value; }
            }
        }
    }
}
