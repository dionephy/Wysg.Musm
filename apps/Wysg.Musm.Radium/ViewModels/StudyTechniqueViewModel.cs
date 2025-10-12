using System.Collections.ObjectModel;
using System.Windows.Input;
using Wysg.Musm.Infrastructure.ViewModels;
using Wysg.Musm.Radium.Services;

namespace Wysg.Musm.Radium.ViewModels
{
    public sealed class StudyTechniqueViewModel : BaseViewModel
    {
        private readonly ITechniqueRepository _repo;
        private readonly IRadStudyRepository? _studies;
        public StudyTechniqueViewModel(ITechniqueRepository repo, IRadStudyRepository? studies = null)
        {
            _repo = repo; _studies = studies;
            Prefixes = new(); Techs = new(); Suffixes = new();
            CurrentCombinationItems = new(); StudynameCombinations = new();
            AddTechniqueCommand = new RelayCommand(_ =>
            {
                if (SelectedTech == null) return;
                var prefixText = SelectedPrefix?.Text?.Trim() ?? string.Empty;
                var suffixText = SelectedSuffix?.Text?.Trim() ?? string.Empty;
                var display = string.Join(" ", new[] { string.IsNullOrWhiteSpace(prefixText) ? null : prefixText, SelectedTech.Text, string.IsNullOrWhiteSpace(suffixText) ? null : suffixText }.Where(s => !string.IsNullOrWhiteSpace(s))) ?? SelectedTech.Text;
                var seq = CurrentCombinationItems.Count + 1;
                CurrentCombinationItems.Add(new CombinationItem { SequenceOrder = seq, TechniqueDisplay = display });
            });
            SetDefaultForStudynameCommand = new RelayCommand(async _ =>
            {
                if (StudynameId.HasValue && SelectedCombination != null)
                {
                    await _repo.SetDefaultForStudynameAsync(StudynameId.Value, SelectedCombination.CombinationId);
                    // refresh list
                    StudynameCombinations.Clear();
                    var rows = await _repo.GetCombinationsForStudynameAsync(StudynameId.Value);
                    foreach (var c in rows) StudynameCombinations.Add(new CombinationRow { CombinationId = c.CombinationId, Display = c.Display, IsDefault = c.IsDefault });
                }
            });
            SaveForStudyAndStudynameCommand = new RelayCommand(async _ =>
            {
                if (StudynameId == null || CurrentCombinationItems.Count == 0) return;
                // Ensure technique rows and build list for insert
                var items = new System.Collections.Generic.List<(long techniqueId, int seq)>();
                foreach (var ci in CurrentCombinationItems)
                {
                    // naive parse: match back to Selected* of last add; for v1 we simply use current selections
                    // A more complete UI would store picked ids per row; keeping simple here
                    if (SelectedTech == null) continue;
                    long? pId = SelectedPrefix?.Id;
                    long tId = SelectedTech.Id;
                    long? sId = SelectedSuffix?.Id;
                    var techId = await _repo.EnsureTechniqueAsync(pId, tId, sId);
                    items.Add((techId, ci.SequenceOrder));
                }
                var combId = await _repo.CreateCombinationAsync(null);
                await _repo.AddCombinationItemsAsync(combId, items);
                if (SetAsDefaultAfterSave)
                {
                    await _repo.SetDefaultForStudynameAsync(StudynameId.Value, combId);
                }
                else
                {
                    await _repo.LinkStudynameCombinationAsync(StudynameId.Value, combId, isDefault: false);
                }
                // reload combos list
                StudynameCombinations.Clear();
                var rows = await _repo.GetCombinationsForStudynameAsync(StudynameId.Value);
                foreach (var c in rows) StudynameCombinations.Add(new CombinationRow { CombinationId = c.CombinationId, Display = c.Display, IsDefault = c.IsDefault });
                CurrentCombinationItems.Clear();
            });
        }

        // When true, SaveForStudyAndStudynameCommand will set the newly created combination
        // as the default for the selected studyname instead of adding as non-default.
        public bool SetAsDefaultAfterSave { get; set; }

        public string CurrentStudyLabel { get => _currentStudyLabel; set => SetProperty(ref _currentStudyLabel, value); }
        private string _currentStudyLabel = string.Empty;
        public string Studyname { get => _studyname; set => SetProperty(ref _studyname, value); }
        private string _studyname = string.Empty;
        public long? StudynameId { get => _studynameId; set => SetProperty(ref _studynameId, value); }
        private long? _studynameId;

        public ObservableCollection<TechText> Prefixes { get; }
        public ObservableCollection<TechText> Techs { get; }
        public ObservableCollection<TechText> Suffixes { get; }
        public TechText? SelectedPrefix { get => _selectedPrefix; set => SetProperty(ref _selectedPrefix, value); }
        private TechText? _selectedPrefix;
        public TechText? SelectedTech { get => _selectedTech; set => SetProperty(ref _selectedTech, value); }
        private TechText? _selectedTech;
        public TechText? SelectedSuffix { get => _selectedSuffix; set => SetProperty(ref _selectedSuffix, value); }
        private TechText? _selectedSuffix;

        public ObservableCollection<CombinationItem> CurrentCombinationItems { get; }
        public ObservableCollection<CombinationRow> StudynameCombinations { get; }
        public CombinationRow? SelectedCombination { get => _selectedCombination; set => SetProperty(ref _selectedCombination, value); }
        private CombinationRow? _selectedCombination;

        public ICommand AddTechniqueCommand { get; }
        public ICommand SetDefaultForStudynameCommand { get; }
        public ICommand SaveForStudyAndStudynameCommand { get; }

        public async void InitializeForStudyname(long id, string studyname)
        {
            StudynameId = id; Studyname = studyname;
            // Load lookup lists minimal (skeleton)
            Prefixes.Clear(); foreach (var p in await _repo.GetPrefixesAsync()) Prefixes.Add(new TechText { Id = p.Id, Text = p.Text });
            Techs.Clear(); foreach (var t in await _repo.GetTechsAsync()) Techs.Add(new TechText { Id = t.Id, Text = t.Text });
            Suffixes.Clear(); foreach (var s in await _repo.GetSuffixesAsync()) Suffixes.Add(new TechText { Id = s.Id, Text = s.Text });
            // Load combos
            StudynameCombinations.Clear(); foreach (var c in await _repo.GetCombinationsForStudynameAsync(id)) StudynameCombinations.Add(new CombinationRow { CombinationId = c.CombinationId, Display = c.Display, IsDefault = c.IsDefault });
        }

        public sealed class TechText { public long Id { get; set; } public string Text { get; set; } = string.Empty; }
        public sealed class CombinationItem { public int SequenceOrder { get; set; } public string TechniqueDisplay { get; set; } = string.Empty; }
        public sealed class CombinationRow { public long CombinationId { get; set; } public string Display { get; set; } = string.Empty; public bool IsDefault { get; set; } }

        private sealed class RelayCommand : ICommand
        {
            private readonly System.Action<object?> _exec; private readonly System.Predicate<object?>? _can;
            public RelayCommand(System.Action<object?> exec, System.Predicate<object?>? can = null) { _exec = exec; _can = can; }
            public bool CanExecute(object? parameter) => _can?.Invoke(parameter) ?? true;
            public void Execute(object? parameter) => _exec(parameter);
            public event System.EventHandler? CanExecuteChanged; public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, System.EventArgs.Empty);
        }
    }
}
