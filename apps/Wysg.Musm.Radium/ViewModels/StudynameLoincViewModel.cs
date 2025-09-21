using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Wysg.Musm.Infrastructure.ViewModels;
using Wysg.Musm.Radium.Services;

namespace Wysg.Musm.Radium.ViewModels
{
    public class StudynameLoincViewModel : BaseViewModel
    {
        private readonly IStudynameLoincRepository _repo;
        public StudynameLoincViewModel(IStudynameLoincRepository repo)
        {
            _repo = repo;
            Studynames = new ObservableCollection<StudynameItem>();
            PartsBySubcategory = new ObservableCollection<PartGroup>();
            SelectedParts = new ObservableCollection<PartItem>();

            AddStudynameCommand = new RelayCommand(async _ => await AddStudynameAsync());
            SaveCommand = new RelayCommand(async _ => await SaveAsync(), _ => SelectedStudyname != null);

            _ = LoadAsync();
        }

        public ObservableCollection<StudynameItem> Studynames { get; }
        private StudynameItem? _selectedStudyname;
        public StudynameItem? SelectedStudyname
        {
            get => _selectedStudyname;
            set
            {
                if (SetProperty(ref _selectedStudyname, value))
                    _ = ReloadPartsAsync();
            }
        }

        public string? StudynameFilter { get; set; }

        public ObservableCollection<PartGroup> PartsBySubcategory { get; }
        public ObservableCollection<PartItem> SelectedParts { get; }

        public ICommand AddStudynameCommand { get; }
        public ICommand SaveCommand { get; }

        public async Task LoadAsync()
        {
            Studynames.Clear();
            var rows = await _repo.GetStudynamesAsync();
            foreach (var row in rows)
                Studynames.Add(new StudynameItem { Id = row.Id, Studyname = row.Studyname });

            // Auto-select the first studyname so parts are visible immediately
            if (SelectedStudyname == null && Studynames.Count > 0)
                SelectedStudyname = Studynames[0];
        }

        public void Preselect(string studyname)
        {
            _ = PreselectAsync(studyname);
        }

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
            else
            {
                SelectedStudyname = found;
            }
        }

        private async Task AddStudynameAsync()
        {
            var name = $"MRI NEW {DateTime.Now:HHmmss}";
            var id = await _repo.EnsureStudynameAsync(name);
            var item = new StudynameItem { Id = id, Studyname = name };
            Studynames.Add(item);
            SelectedStudyname = item;
        }

        private async Task ReloadPartsAsync()
        {
            PartsBySubcategory.Clear();
            SelectedParts.Clear();
            if (SelectedStudyname == null)
                return;

            var parts = await _repo.GetPartsAsync();
            var mappings = (await _repo.GetMappingsAsync(SelectedStudyname.Id)).ToDictionary(m => m.PartNumber, m => m.PartSequenceOrder);

            // Group by inferred subcategory prefix from part_type_name if it begins with "rad." else use part_type_name
            foreach (var grp in parts.GroupBy(p => (p.PartTypeName?.StartsWith("rad.") ?? false) ? p.PartTypeName : (string.IsNullOrEmpty(p.PartTypeName) ? "(none)" : p.PartTypeName)))
            {
                var g = new PartGroup { Subcategory = grp.Key };
                foreach (var p in grp)
                {
                    var item = new PartItem
                    {
                        PartNumber = p.PartNumber,
                        PartTypeName = p.PartTypeName,
                        PartName = p.PartName,
                        PartDisplay = string.IsNullOrEmpty(p.PartName) ? p.PartNumber : $"{p.PartName} ({p.PartNumber})",
                        IsSelected = mappings.ContainsKey(p.PartNumber),
                        PartSequenceOrder = mappings.TryGetValue(p.PartNumber, out var ord) ? ord : "A"
                    };
                    item.PropertyChanged += (_, __) => SyncSelectedParts(item);
                    g.Parts.Add(item);
                    if (item.IsSelected) SelectedParts.Add(item);
                }
                PartsBySubcategory.Add(g);
            }
        }

        private void SyncSelectedParts(PartItem item)
        {
            if (item.IsSelected)
            {
                if (!SelectedParts.Contains(item)) SelectedParts.Add(item);
            }
            else
            {
                if (SelectedParts.Contains(item)) SelectedParts.Remove(item);
            }
        }

        private async Task SaveAsync()
        {
            if (SelectedStudyname == null) return;
            var payload = SelectedParts.Select(p => new MappingRow(p.PartNumber, string.IsNullOrWhiteSpace(p.PartSequenceOrder) ? "A" : p.PartSequenceOrder));
            await _repo.SaveMappingsAsync(SelectedStudyname.Id, payload);
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

        public class PartItem : BaseViewModel
        {
            public string PartNumber { get; set; } = string.Empty;
            public string PartTypeName { get; set; } = string.Empty;
            public string PartName { get; set; } = string.Empty;
            public string PartDisplay { get; set; } = string.Empty;

            private bool _isSelected;
            public bool IsSelected { get => _isSelected; set => SetProperty(ref _isSelected, value); }

            private string _order = "A";
            public string PartSequenceOrder { get => _order; set => SetProperty(ref _order, value); }
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
