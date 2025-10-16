using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Linq;
using Wysg.Musm.Infrastructure.ViewModels;
using Wysg.Musm.Radium.Services;

namespace Wysg.Musm.Radium.ViewModels
{
    public sealed class StudynameTechniqueViewModel : BaseViewModel
    {
        private readonly ITechniqueRepository _repo;
        private RelayCommand? _setDefaultCommand;
        private RelayCommand? _addTechniqueCommand;
        private RelayCommand? _saveNewCombinationCommand;
        private RelayCommand? _deleteCombinationCommand;
        
        public StudynameTechniqueViewModel(ITechniqueRepository repo) 
        { 
            _repo = repo;
            _setDefaultCommand = new RelayCommand(async _ => 
            { 
                if (_studynameId.HasValue && SelectedCombination != null) 
                {
                    await _repo.SetDefaultForStudynameAsync(_studynameId.Value, SelectedCombination.CombinationId); 
                    await ReloadAsync(); // Reload to show updated default status
                }
            }, _ => _studynameId.HasValue && SelectedCombination != null);

            _addTechniqueCommand = new RelayCommand(_ =>
            {
                if (SelectedTech == null) return;

                // Prevent duplicate (prefix, tech, suffix) within the in-memory combination
                long? candP = SelectedPrefix?.Id;
                long candT = SelectedTech.Id;
                long? candS = SelectedSuffix?.Id;
                foreach (var e in CurrentCombinationItems)
                {
                    if (e.PrefixId == candP && e.TechId == candT && e.SuffixId == candS)
                    {
                        // Duplicate; ignore add
                        return;
                    }
                }

                var prefixText = SelectedPrefix?.Text?.Trim() ?? string.Empty;
                var suffixText = SelectedSuffix?.Text?.Trim() ?? string.Empty;
                var display = string.Join(" ", new[] { string.IsNullOrWhiteSpace(prefixText) ? null : prefixText, SelectedTech.Text, string.IsNullOrWhiteSpace(suffixText) ? null : suffixText }.Where(s => !string.IsNullOrWhiteSpace(s))) ?? SelectedTech.Text;
                var seq = CurrentCombinationItems.Count + 1;
                CurrentCombinationItems.Add(new CombinationItem
                {
                    SequenceOrder = seq,
                    TechniqueDisplay = display,
                    PrefixId = candP,
                    TechId = candT,
                    SuffixId = candS
                });
            });

            _saveNewCombinationCommand = new RelayCommand(async _ =>
            {
                if (!_studynameId.HasValue || CurrentCombinationItems.Count == 0) return;

                // Deduplicate (prefix, tech, suffix) across the combination before save, preserving first occurrence order
                var seen = new System.Collections.Generic.HashSet<string>(System.StringComparer.Ordinal);
                var items = new System.Collections.Generic.List<(long techniqueId, int seq)>();
                int seq = 1;
                foreach (var ci in CurrentCombinationItems)
                {
                    var key = $"{ci.PrefixId?.ToString() ?? "_"}|{ci.TechId}|{ci.SuffixId?.ToString() ?? "_"}";
                    if (!seen.Add(key)) continue; // skip duplicates

                    long? pId = ci.PrefixId;
                    long tId = ci.TechId;
                    long? sId = ci.SuffixId;
                    var techId = await _repo.EnsureTechniqueAsync(pId, tId, sId);
                    items.Add((techId, seq++));
                }
                if (items.Count == 0) return;

                var combId = await _repo.CreateCombinationAsync(null);
                await _repo.AddCombinationItemsAsync(combId, items);
                await _repo.LinkStudynameCombinationAsync(_studynameId.Value, combId, isDefault: false);
                await ReloadAsync();
                CurrentCombinationItems.Clear();
            }, _ => _studynameId.HasValue && CurrentCombinationItems.Count > 0);

            _deleteCombinationCommand = new RelayCommand(async _ =>
            {
                if (!_studynameId.HasValue || SelectedCombination == null) return;

                // Confirm deletion
                var result = System.Windows.MessageBox.Show(
                    $"Are you sure you want to delete this combination?\n\n{SelectedCombination.Display}",
                    "Confirm Delete",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Question);

                if (result != System.Windows.MessageBoxResult.Yes) return;

                try
                {
                    await _repo.DeleteStudynameCombinationLinkAsync(_studynameId.Value, SelectedCombination.CombinationId);
                    await ReloadAsync();
                    SelectedCombination = null; // Clear selection after delete
                }
                catch (System.Exception ex)
                {
                    System.Windows.MessageBox.Show(
                        $"Failed to delete combination: {ex.Message}",
                        "Delete Failed",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Error);
                }
            }, _ => _studynameId.HasValue && SelectedCombination != null);
        }
        
        public string Header { get => _header; set => SetProperty(ref _header, value); }
        private string _header = string.Empty;
        
        public ObservableCollection<ComboRow> Combinations { get; } = new();
        
        public ComboRow? SelectedCombination 
        { 
            get => _sel; 
            set 
            { 
                if (SetProperty(ref _sel, value))
                {
                    _setDefaultCommand?.RaiseCanExecuteChanged();
                    _deleteCombinationCommand?.RaiseCanExecuteChanged();
                }
            } 
        }
        private ComboRow? _sel;

        // Technique building UI properties
        public ObservableCollection<TechText> Prefixes { get; } = new();
        public ObservableCollection<TechText> Techs { get; } = new();
        public ObservableCollection<TechText> Suffixes { get; } = new();
        public TechText? SelectedPrefix { get => _selectedPrefix; set => SetProperty(ref _selectedPrefix, value); }
        private TechText? _selectedPrefix;
        public TechText? SelectedTech { get => _selectedTech; set => SetProperty(ref _selectedTech, value); }
        private TechText? _selectedTech;
        public TechText? SelectedSuffix { get => _selectedSuffix; set => SetProperty(ref _selectedSuffix, value); }
        private TechText? _selectedSuffix;
        
        public ObservableCollection<CombinationItem> CurrentCombinationItems { get; } = new();
        
        public ICommand SetDefaultCommand => _setDefaultCommand!;
        public ICommand AddTechniqueCommand => _addTechniqueCommand!;
        public ICommand SaveNewCombinationCommand => _saveNewCombinationCommand!;
        public ICommand DeleteCombinationCommand => _deleteCombinationCommand!;
        
        private long? _studynameId;
        public long? StudynameIdPublic => _studynameId;
        
        public async void Initialize(long studynameId, string studyname)
        {
            _studynameId = studynameId; 
            Header = $"Studyname: {studyname}";
            _setDefaultCommand?.RaiseCanExecuteChanged();
            _saveNewCombinationCommand?.RaiseCanExecuteChanged();
            _deleteCombinationCommand?.RaiseCanExecuteChanged();
            await ReloadAsync();
        }
        
        public async System.Threading.Tasks.Task ReloadAsync()
        {
            if (!_studynameId.HasValue) return;
            
            // Reload combinations list
            Combinations.Clear();
            var rows = await _repo.GetCombinationsForStudynameAsync(_studynameId.Value);
            foreach (var c in rows) 
                Combinations.Add(new ComboRow { CombinationId = c.CombinationId, Display = c.Display, IsDefault = c.IsDefault });

            // Reload lookup lists for building
            Prefixes.Clear(); 
            foreach (var p in await _repo.GetPrefixesAsync()) 
                Prefixes.Add(new TechText { Id = p.Id, Text = p.Text });
            
            Techs.Clear(); 
            foreach (var t in await _repo.GetTechsAsync()) 
                Techs.Add(new TechText { Id = t.Id, Text = t.Text });
            
            Suffixes.Clear(); 
            foreach (var s in await _repo.GetSuffixesAsync()) 
                Suffixes.Add(new TechText { Id = s.Id, Text = s.Text });
        }

        // Helpers used by window for inline add
        public async System.Threading.Tasks.Task AddPrefixAndSelectAsync(string text)
        {
            var id = await _repo.AddPrefixAsync(text.Trim());
            Prefixes.Clear();
            foreach (var p in await _repo.GetPrefixesAsync()) 
                Prefixes.Add(new TechText { Id = p.Id, Text = p.Text });
            SelectedPrefix = Prefixes.FirstOrDefault(p => p.Id == id) ?? Prefixes.FirstOrDefault(p => p.Text == text);
        }
        
        public async System.Threading.Tasks.Task AddTechAndSelectAsync(string text)
        {
            var id = await _repo.AddTechAsync(text.Trim());
            Techs.Clear();
            foreach (var t in await _repo.GetTechsAsync()) 
                Techs.Add(new TechText { Id = t.Id, Text = t.Text });
            SelectedTech = Techs.FirstOrDefault(t => t.Id == id) ?? Techs.FirstOrDefault(t => t.Text == text);
        }
        
        public async System.Threading.Tasks.Task AddSuffixAndSelectAsync(string text)
        {
            var id = await _repo.AddSuffixAsync(text.Trim());
            Suffixes.Clear();
            foreach (var s in await _repo.GetSuffixesAsync()) 
                Suffixes.Add(new TechText { Id = s.Id, Text = s.Text });
            SelectedSuffix = Suffixes.FirstOrDefault(s => s.Id == id) ?? Suffixes.FirstOrDefault(s => s.Text == text);
        }
        
        public sealed class TechText 
        { 
            public long Id { get; set; } 
            public string Text { get; set; } = string.Empty;
            public override string ToString() => Text;
        }
        
        public sealed class CombinationItem 
        { 
            public int SequenceOrder { get; set; } 
            public string TechniqueDisplay { get; set; } = string.Empty; 
            public long? PrefixId { get; set; } 
            public long TechId { get; set; } 
            public long? SuffixId { get; set; }
            public override string ToString() => TechniqueDisplay;
        }
        
        public sealed class ComboRow : BaseViewModel 
        { 
            public long CombinationId { get; set; } 
            public string Display { get; set; } = string.Empty; 
            public bool IsDefault { get; set; }
            public override string ToString() => Display; // FIX: Override ToString to show Display text
        }
        
        private sealed class RelayCommand : ICommand
        {
            private readonly System.Action<object?> _exec; 
            private readonly System.Predicate<object?>? _can;
            
            public RelayCommand(System.Action<object?> exec, System.Predicate<object?>? can = null) 
            { 
                _exec = exec; 
                _can = can; 
            }
            
            public bool CanExecute(object? parameter) => _can?.Invoke(parameter) ?? true;
            public void Execute(object? parameter) => _exec(parameter);
            public event System.EventHandler? CanExecuteChanged; 
            public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, System.EventArgs.Empty);
        }
    }
}
