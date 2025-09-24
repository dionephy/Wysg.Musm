using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Diagnostics;
using System.ComponentModel;
using System.Windows.Data;
using Wysg.Musm.Infrastructure.ViewModels;
using Wysg.Musm.Radium.Services;

namespace Wysg.Musm.Radium.ViewModels
{
    public class StudynameLoincViewModel : BaseViewModel
    {
        private static readonly string[] KnownPartTypes = new[]
        {
            "Rad.Anatomic Location.Imaging Focus",
            "Rad.Anatomic Location.Laterality",
            "Rad.Anatomic Location.Laterality.Presence",
            "Rad.Anatomic Location.Region Imaged",
            "Rad.Guidance for.Action",
            "Rad.Guidance for.Approach",
            "Rad.Guidance for.Object",
            "Rad.Guidance for.Presence",
            "Rad.Maneuver.Maneuver Type",
            "Rad.Modality.Modality Subtype",
            "Rad.Modality.Modality Type",
            "Rad.Pharmaceutical.Route",
            "Rad.Pharmaceutical.Substance Given",
            "Rad.Reason for Exam",
            "Rad.Subject",
            "Rad.Timing",
            "Rad.View.Aggregation",
            "Rad.View.View Type"
        };

        private readonly IStudynameLoincRepository _repo;
        public StudynameLoincViewModel(IStudynameLoincRepository repo)
        {
            _repo = repo;
            Studynames = new ObservableCollection<StudynameItem>();
            StudynamesView = CollectionViewSource.GetDefaultView(Studynames);
            StudynamesView.Filter = o =>
            {
                if (o is not StudynameItem it) return false;
                if (string.IsNullOrWhiteSpace(StudynameFilter)) return true;
                return it.Studyname?.IndexOf(StudynameFilter, StringComparison.OrdinalIgnoreCase) >= 0;
            };
            PartsBySubcategory = new ObservableCollection<PartGroup>();
            Categories = new ObservableCollection<CategoryGroup>();
            CategoriesCol0 = new ObservableCollection<CategoryGroup>();
            CategoriesCol1 = new ObservableCollection<CategoryGroup>();
            CategoriesCol2 = new ObservableCollection<CategoryGroup>();
            CategoryByName = new Dictionary<string, CategoryGroup>(StringComparer.Ordinal);
            SelectedParts = new ObservableCollection<MappingPreviewItem>();
            SelectedParts.CollectionChanged += (_, __) => _ = RefreshPlaybookMatchesAsync();

            AddStudynameCommand = new RelayCommand(async _ => await AddStudynameAsync());
            SaveCommand = new RelayCommand(async _ => await SaveAsync(), _ => SelectedStudyname != null);
            AddPartCommand = new RelayCommand(p =>
            {
                if (p is PartItem it) AddPartToPreview(it);
            });

            SequenceOrderInput = "A";
            _ = LoadAsync();
        }

        public ObservableCollection<StudynameItem> Studynames { get; }
        public ICollectionView StudynamesView { get; }

        private StudynameItem? _selectedStudyname;
        public StudynameItem? SelectedStudyname
        {
            get => _selectedStudyname;
            set
            {
                if (SetProperty(ref _selectedStudyname, value))
                {
                    _ = ReloadPartsAsync();
                    (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        private string _studynameFilter = string.Empty;
        public string? StudynameFilter
        {
            get => _studynameFilter;
            set
            {
                if (SetProperty(ref _studynameFilter, value ?? string.Empty))
                    StudynamesView.Refresh();
            }
        }

        private string _newStudynameInput = string.Empty;
        public string NewStudynameInput
        {
            get => _newStudynameInput;
            set => SetProperty(ref _newStudynameInput, value ?? string.Empty);
        }

        private string _statusMessage = string.Empty;
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        private string _sequenceOrderInput = "A";
        public string SequenceOrderInput
        {
            get => _sequenceOrderInput;
            set => SetProperty(ref _sequenceOrderInput, string.IsNullOrWhiteSpace(value) ? "A" : value.Trim());
        }

        public ObservableCollection<PartGroup> PartsBySubcategory { get; }
        public ObservableCollection<CategoryGroup> Categories { get; }
        public ObservableCollection<CategoryGroup> CategoriesCol0 { get; }
        public ObservableCollection<CategoryGroup> CategoriesCol1 { get; }
        public ObservableCollection<CategoryGroup> CategoriesCol2 { get; }
        public Dictionary<string, CategoryGroup> CategoryByName { get; }

        private CategoryGroup? _catImagingFocus; public CategoryGroup? Cat_ImagingFocus { get => _catImagingFocus; private set => SetProperty(ref _catImagingFocus, value); }
        private CategoryGroup? _catRegionImaged; public CategoryGroup? Cat_RegionImaged { get => _catRegionImaged; private set => SetProperty(ref _catRegionImaged, value); }
        private CategoryGroup? _catLateralityPresence; public CategoryGroup? Cat_LateralityPresence { get => _catLateralityPresence; private set => SetProperty(ref _catLateralityPresence, value); }
        private CategoryGroup? _catLaterality; public CategoryGroup? Cat_Laterality { get => _catLaterality; private set => SetProperty(ref _catLaterality, value); }
        private CategoryGroup? _catModalityType; public CategoryGroup? Cat_ModalityType { get => _catModalityType; private set => SetProperty(ref _catModalityType, value); }
        private CategoryGroup? _catModalitySubtype; public CategoryGroup? Cat_ModalitySubtype { get => _catModalitySubtype; private set => SetProperty(ref _catModalitySubtype, value); }
        private CategoryGroup? _catReasonForExam; public CategoryGroup? Cat_ReasonForExam { get => _catReasonForExam; private set => SetProperty(ref _catReasonForExam, value); }
        private CategoryGroup? _catTiming; public CategoryGroup? Cat_Timing { get => _catTiming; private set => SetProperty(ref _catTiming, value); }
        private CategoryGroup? _catPharmSubstance; public CategoryGroup? Cat_Pharm_SubstanceGiven { get => _catPharmSubstance; private set => SetProperty(ref _catPharmSubstance, value); }
        private CategoryGroup? _catPharmRoute; public CategoryGroup? Cat_Pharm_Route { get => _catPharmRoute; private set => SetProperty(ref _catPharmRoute, value); }
        private CategoryGroup? _catViewAggregation; public CategoryGroup? Cat_View_Aggregation { get => _catViewAggregation; private set => SetProperty(ref _catViewAggregation, value); }
        private CategoryGroup? _catViewType; public CategoryGroup? Cat_View_ViewType { get => _catViewType; private set => SetProperty(ref _catViewType, value); }
        private CategoryGroup? _catManeuverType; public CategoryGroup? Cat_Maneuver_Type { get => _catManeuverType; private set => SetProperty(ref _catManeuverType, value); }
        private CategoryGroup? _catSubject; public CategoryGroup? Cat_Subject { get => _catSubject; private set => SetProperty(ref _catSubject, value); }
        private CategoryGroup? _catGuidancePresence; public CategoryGroup? Cat_Guidance_Presence { get => _catGuidancePresence; private set => SetProperty(ref _catGuidancePresence, value); }
        private CategoryGroup? _catGuidanceAction; public CategoryGroup? Cat_Guidance_Action { get => _catGuidanceAction; private set => SetProperty(ref _catGuidanceAction, value); }
        private CategoryGroup? _catGuidanceApproach; public CategoryGroup? Cat_Guidance_Approach { get => _catGuidanceApproach; private set => SetProperty(ref _catGuidanceApproach, value); }
        private CategoryGroup? _catGuidanceObject; public CategoryGroup? Cat_Guidance_Object { get => _catGuidanceObject; private set => SetProperty(ref _catGuidanceObject, value); }

        private CategoryGroup? _commonGroup; public CategoryGroup? CommonGroup { get => _commonGroup; private set => SetProperty(ref _commonGroup, value); }

        public ObservableCollection<MappingPreviewItem> SelectedParts { get; }

        public ICommand AddStudynameCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand AddPartCommand { get; }

        private PlaybookItem? _selectedPlaybook;
        public PlaybookItem? SelectedPlaybook
        {
            get => _selectedPlaybook;
            set
            {
                if (SetProperty(ref _selectedPlaybook, value))
                    _ = LoadPlaybookPartsAsync(value);
            }
        }
        public ObservableCollection<PlaybookItem> PlaybookMatches { get; } = new();
        public ObservableCollection<PlaybookPartItem> PlaybookParts { get; } = new();

        public async Task LoadAsync()
        {
            try
            {
                Studynames.Clear();
                var rows = await _repo.GetStudynamesAsync();
                foreach (var row in rows)
                    Studynames.Add(new StudynameItem { Id = row.Id, Studyname = row.Studyname });
                if (SelectedStudyname == null && Studynames.Count > 0)
                {
                    SelectedStudyname = Studynames[0];
                }
            }
            catch { }
        }

        public void Preselect(string studyname) => _ = PreselectAsync(studyname);
        private async Task PreselectAsync(string studyname)
        {
            var found = Studynames.FirstOrDefault(s => s.Studyname == studyname);
            if (found == null)
            {
                var id = await _repo.EnsureStudynameAsync(studyname);
                var item = new StudynameItem { Id = id, Studyname = studyname };
                Studynames.Add(item);
                SelectedStudyname = item;
            }
            else SelectedStudyname = found;
        }

        private async Task AddStudynameAsync()
        {
            var name = string.IsNullOrWhiteSpace(NewStudynameInput) ? $"MRI NEW {DateTime.Now:HHmmss}" : NewStudynameInput.Trim();
            var id = await _repo.EnsureStudynameAsync(name);
            var item = new StudynameItem { Id = id, Studyname = name };
            Studynames.Add(item);
            SelectedStudyname = item;
            NewStudynameInput = string.Empty;
            StatusMessage = $"Studyname '{name}' added.";
        }

        private async Task ReloadPartsAsync()
        {
            PartsBySubcategory.Clear();
            Categories.Clear();
            CategoriesCol0.Clear();
            CategoriesCol1.Clear();
            CategoriesCol2.Clear();
            CategoryByName.Clear();
            CommonGroup = null;
            SelectedParts.Clear();
            PlaybookMatches.Clear();
            PlaybookParts.Clear();
            if (SelectedStudyname == null) return;

            var parts = await _repo.GetPartsAsync();
            var partsByNumber = parts.ToDictionary(p => p.PartNumber, p => p);
            var mappings = (await _repo.GetMappingsAsync(SelectedStudyname.Id));

            var col0 = new[] { "Rad.Anatomic Location.Imaging Focus" };
            var col1 = new[] { "Rad.Anatomic Location.Region Imaged", "Rad.Anatomic Location.Laterality.Presence", "Rad.Anatomic Location.Laterality" };

            var catMap = new Dictionary<string, CategoryGroup>(StringComparer.Ordinal);
            foreach (var name in KnownPartTypes)
            {
                var g = new CategoryGroup(name);
                Categories.Add(g);
                catMap[name] = g;
                CategoryByName[name] = g;
                if (col0.Contains(name)) CategoriesCol0.Add(g);
                else if (col1.Contains(name)) CategoriesCol1.Add(g);
                else CategoriesCol2.Add(g);
            }

            Cat_ImagingFocus = CategoryByName.GetValueOrDefault("Rad.Anatomic Location.Imaging Focus");
            Cat_RegionImaged = CategoryByName.GetValueOrDefault("Rad.Anatomic Location.Region Imaged");
            Cat_LateralityPresence = CategoryByName.GetValueOrDefault("Rad.Anatomic Location.Laterality.Presence");
            Cat_Laterality = CategoryByName.GetValueOrDefault("Rad.Anatomic Location.Laterality");
            Cat_ModalityType = CategoryByName.GetValueOrDefault("Rad.Modality.Modality Type");
            Cat_ModalitySubtype = CategoryByName.GetValueOrDefault("Rad.Modality.Modality Subtype");
            Cat_ReasonForExam = CategoryByName.GetValueOrDefault("Rad.Reason for Exam");
            Cat_Timing = CategoryByName.GetValueOrDefault("Rad.Timing");
            Cat_Pharm_SubstanceGiven = CategoryByName.GetValueOrDefault("Rad.Pharmaceutical.Substance Given");
            Cat_Pharm_Route = CategoryByName.GetValueOrDefault("Rad.Pharmaceutical.Route");
            Cat_View_Aggregation = CategoryByName.GetValueOrDefault("Rad.View.Aggregation");
            Cat_View_ViewType = CategoryByName.GetValueOrDefault("Rad.View.View Type");
            Cat_Maneuver_Type = CategoryByName.GetValueOrDefault("Rad.Maneuver.Maneuver Type");
            Cat_Subject = CategoryByName.GetValueOrDefault("Rad.Subject");
            Cat_Guidance_Presence = CategoryByName.GetValueOrDefault("Rad.Guidance for.Presence");
            Cat_Guidance_Action = CategoryByName.GetValueOrDefault("Rad.Guidance for.Action");
            Cat_Guidance_Approach = CategoryByName.GetValueOrDefault("Rad.Guidance for.Approach");
            Cat_Guidance_Object = CategoryByName.GetValueOrDefault("Rad.Guidance for.Object");

            foreach (var p in parts)
            {
                if (string.IsNullOrWhiteSpace(p.PartTypeName)) continue;
                if (!catMap.TryGetValue(p.PartTypeName, out var g)) continue;
                g.Items.Add(new PartItem { PartNumber = p.PartNumber, PartTypeName = p.PartTypeName, PartName = p.PartName, PartDisplay = p.PartName });
            }

            foreach (var m in mappings)
            {
                if (partsByNumber.TryGetValue(m.PartNumber, out var p))
                {
                    SelectedParts.Add(new MappingPreviewItem { PartNumber = p.PartNumber, PartDisplay = p.PartName, PartSequenceOrder = string.IsNullOrWhiteSpace(m.PartSequenceOrder) ? "A" : m.PartSequenceOrder });
                }
                else
                {
                    SelectedParts.Add(new MappingPreviewItem { PartNumber = m.PartNumber, PartDisplay = string.Empty, PartSequenceOrder = string.IsNullOrWhiteSpace(m.PartSequenceOrder) ? "A" : m.PartSequenceOrder });
                }
            }

            try
            {
                var common = await _repo.GetCommonPartsAsync(50);
                var cg = new CategoryGroup("Common");
                foreach (var cp in common)
                {
                    cg.Items.Add(new PartItem { PartNumber = cp.PartNumber, PartTypeName = cp.PartTypeName, PartName = cp.PartName, PartDisplay = cp.PartName });
                }
                CommonGroup = cg;
            }
            catch { }

            foreach (var g in Categories) g.Refresh();
            CommonGroup?.Refresh();
        }

        private async Task RefreshPlaybookMatchesAsync()
        {
            PlaybookMatches.Clear();
            PlaybookParts.Clear();
            var parts = SelectedParts.Select(p => p.PartNumber).Distinct().ToArray();
            if (parts.Length < 2) return;
            try
            {
                var matches = await _repo.GetPlaybookMatchesAsync(parts);
                foreach (var m in matches)
                    PlaybookMatches.Add(new PlaybookItem { LoincNumber = m.LoincNumber, LongCommonName = m.LongCommonName });
            }
            catch { }
        }

        private async Task LoadPlaybookPartsAsync(PlaybookItem? item)
        {
            PlaybookParts.Clear();
            if (item == null) return;
            try
            {
                var rows = await _repo.GetPlaybookPartsAsync(item.LoincNumber);
                foreach (var r in rows)
                    PlaybookParts.Add(new PlaybookPartItem { PartNumber = r.PartNumber, PartName = r.PartName, PartSequenceOrder = r.PartSequenceOrder });
            }
            catch { }
        }

        private void AddPartToPreview(PartItem p)
        {
            if (p == null) return;
            var order = string.IsNullOrWhiteSpace(SequenceOrderInput) ? "A" : SequenceOrderInput;
            SelectedParts.Add(new MappingPreviewItem { PartNumber = p.PartNumber, PartDisplay = p.PartDisplay, PartSequenceOrder = order });
        }

        private async Task SaveAsync()
        {
            if (SelectedStudyname == null) return;
            var payload = SelectedParts.Select(p => new MappingRow(p.PartNumber, string.IsNullOrWhiteSpace(p.PartSequenceOrder) ? "A" : p.PartSequenceOrder));
            await _repo.SaveMappingsAsync(SelectedStudyname.Id, payload);
            StatusMessage = $"Studyname '{SelectedStudyname.Studyname}' mappings saved ({SelectedParts.Count}).";
        }

        public class StudynameItem : BaseViewModel
        {
            public long Id { get; set; }
            public string Studyname { get; set; } = string.Empty;
            public override string ToString() => Studyname;
        }

        public class PartGroup
        {
            public string Subcategory { get; set; } = string.Empty;
            public ObservableCollection<PartItem> Parts { get; } = new();
        }

        public class CategoryGroup : BaseViewModel
        {
            public string Name { get; }
            private string _filter = string.Empty;
            public string Filter
            {
                get => _filter;
                set
                {
                    if (SetProperty(ref _filter, value))
                        View.Refresh();
                }
            }
            public ObservableCollection<PartItem> Items { get; } = new();
            public ICollectionView View { get; }
            public CategoryGroup(string name)
            {
                Name = name;
                View = CollectionViewSource.GetDefaultView(Items);
                View.Filter = o =>
                {
                    if (o is not PartItem it) return false;
                    if (string.IsNullOrWhiteSpace(Filter)) return true;
                    return it.PartDisplay?.IndexOf(Filter, StringComparison.OrdinalIgnoreCase) >= 0;
                };
            }
            public void Refresh() => View.Refresh();
        }

        public class PartItem : BaseViewModel
        {
            public string PartNumber { get; set; } = string.Empty;
            public string PartTypeName { get; set; } = string.Empty;
            public string PartName { get; set; } = string.Empty;
            public string PartDisplay { get; set; } = string.Empty;
        }

        public class MappingPreviewItem : BaseViewModel
        {
            public string PartNumber { get; set; } = string.Empty;
            public string PartDisplay { get; set; } = string.Empty;
            public string PartSequenceOrder { get; set; } = "A";
        }

        public class PlaybookItem : BaseViewModel
        {
            public string LoincNumber { get; set; } = string.Empty;
            public string LongCommonName { get; set; } = string.Empty;
        }
        public class PlaybookPartItem : BaseViewModel
        {
            public string PartNumber { get; set; } = string.Empty;
            public string PartName { get; set; } = string.Empty;
            public string PartSequenceOrder { get; set; } = string.Empty;
        }

        private sealed class RelayCommand : ICommand
        {
            private readonly System.Action<object?> _execute;
            private readonly System.Predicate<object?>? _can;
            public RelayCommand(System.Action<object?> execute, System.Predicate<object?>? can = null) { _execute = execute; _can = can; }
            public bool CanExecute(object? parameter) => _can?.Invoke(parameter) ?? true;
            public void Execute(object? parameter) => _execute(parameter);
            public event System.EventHandler? CanExecuteChanged;
            public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, System.EventArgs.Empty);
        }
    }
}
