using System.Collections.ObjectModel;
using System.Windows.Input;
using Wysg.Musm.Infrastructure.ViewModels;
using Wysg.Musm.Radium.Services;

namespace Wysg.Musm.Radium.ViewModels
{
    public sealed partial class StudyTechniqueViewModel : BaseViewModel
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
            SetDefaultForStudynameCommand = new RelayCommand(async _ =>
            {
                if (StudynameId.HasValue && SelectedCombination != null)
                {
                    await _repo.SetDefaultForStudynameAsync(StudynameId.Value, SelectedCombination.CombinationId);
                    // refresh list
                    await ReloadStudynameCombinationsAsync();
                }
            });
            SaveForStudyAndStudynameCommand = new RelayCommand(async _ =>
            {
                if (StudynameId == null || CurrentCombinationItems.Count == 0) return;

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
                
                if (_isStudyMode && _mainVm != null)
                {
                    // Study mode: Link combination to specific study in med.rad_study_technique_combination
                    var studyId = await ResolveCurrentStudyIdAsync();
                    if (studyId.HasValue)
                    {
                        await _repo.LinkStudyTechniqueCombinationAsync(studyId.Value, combId);
                        System.Diagnostics.Debug.WriteLine($"[StudyTechniqueViewModel] Linked study {studyId.Value} to combination {combId}");
                    }
                    CurrentCombinationItems.Clear();
                }
                else
                {
                    // Studyname mode: Link to studyname as before
                    if (SetAsDefaultAfterSave)
                    {
                        await _repo.SetDefaultForStudynameAsync(StudynameId.Value, combId);
                    }
                    else
                    {
                        await _repo.LinkStudynameCombinationAsync(StudynameId.Value, combId, isDefault: false);
                    }
                    await ReloadStudynameCombinationsAsync();
                    CurrentCombinationItems.Clear();
                    // Notify main VM to refresh study_techniques from new default (if applicable)
                    try { await NotifyDefaultChangedAsync(); } catch { }
                }
            });
        }

        private async System.Threading.Tasks.Task ReloadStudynameCombinationsAsync()
        {
            if (!StudynameId.HasValue) return;
            StudynameCombinations.Clear();
            var rows = await _repo.GetCombinationsForStudynameAsync(StudynameId.Value);
            foreach (var c in rows) StudynameCombinations.Add(new CombinationRow { CombinationId = c.CombinationId, Display = c.Display, IsDefault = c.IsDefault });
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
            await ReloadLookupsAsync();
            // Load combos
            await ReloadStudynameCombinationsAsync();
        }

        public void InitializeForStudy(MainViewModel mainVm)
        {
            _mainVm = mainVm;
            _isStudyMode = true;
            
            // Set display labels
            CurrentStudyLabel = $"{mainVm.PatientName} - {mainVm.StudyName} - {mainVm.StudyDateTime}";
            Studyname = mainVm.StudyName ?? string.Empty;
            
            // Load lookups and attempt to load existing study technique combination
            System.Threading.Tasks.Task.Run(async () =>
            {
                await ReloadLookupsAsync();
                
                // Try to resolve studyname ID
                try
                {
                    if (!string.IsNullOrWhiteSpace(Studyname))
                    {
                        var snId = await _repo.GetStudynameIdByNameAsync(Studyname);
                        if (snId.HasValue)
                        {
                            StudynameId = snId.Value;
                            await ReloadStudynameCombinationsAsync();
                        }
                    }
                    
                    // Load existing study technique if mapped
                    await LoadExistingStudyTechniqueAsync();
                }
                catch { }
            });
        }

        private MainViewModel? _mainVm;
        private bool _isStudyMode;
        
        private async System.Threading.Tasks.Task LoadExistingStudyTechniqueAsync()
        {
            if (_mainVm == null || !_isStudyMode) return;
            
            try
            {
                // Get study ID from patient number, studyname, and datetime
                var studyId = await ResolveCurrentStudyIdAsync();
                if (!studyId.HasValue) return;
                
                // Check if study has custom technique combination
                var combId = await _repo.GetStudyTechniqueCombinationAsync(studyId.Value);
                if (!combId.HasValue) return;
                
                // Load items into current combination
                await LoadCombinationIntoCurrentAsync(combId.Value);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[StudyTechniqueViewModel] LoadExistingStudyTechnique error: {ex.Message}");
            }
        }
        
        private async System.Threading.Tasks.Task<long?> ResolveCurrentStudyIdAsync()
        {
            if (_mainVm == null || _studies == null) return null;
            
            try
            {
                var patientNumber = _mainVm.PatientNumber;
                var studyName = _mainVm.StudyName;
                var studyDateTimeStr = _mainVm.StudyDateTime;
                
                if (string.IsNullOrWhiteSpace(patientNumber) || string.IsNullOrWhiteSpace(studyName) || string.IsNullOrWhiteSpace(studyDateTimeStr))
                    return null;
                
                if (!System.DateTime.TryParse(studyDateTimeStr, out var studyDateTime))
                    return null;
                
                return await _studies.GetStudyIdAsync(patientNumber, studyName, studyDateTime);
            }
            catch
            {
                return null;
            }
        }

        public async System.Threading.Tasks.Task ReloadLookupsAsync()
        {
            Prefixes.Clear(); foreach (var p in await _repo.GetPrefixesAsync()) Prefixes.Add(new TechText { Id = p.Id, Text = p.Text });
            Techs.Clear(); foreach (var t in await _repo.GetTechsAsync()) Techs.Add(new TechText { Id = t.Id, Text = t.Text });
            Suffixes.Clear(); foreach (var s in await _repo.GetSuffixesAsync()) Suffixes.Add(new TechText { Id = s.Id, Text = s.Text });
        }

        // Helpers used by window for inline add
        public async System.Threading.Tasks.Task AddPrefixAndSelectAsync(string text)
        {
            var id = await _repo.AddPrefixAsync(text.Trim());
            await ReloadLookupsAsync();
            SelectedPrefix = Prefixes.FirstOrDefault(p => p.Id == id) ?? Prefixes.FirstOrDefault(p => p.Text == text) ?? SelectedPrefix;
        }
        public async System.Threading.Tasks.Task AddTechAndSelectAsync(string text)
        {
            var id = await _repo.AddTechAsync(text.Trim());
            await ReloadLookupsAsync();
            SelectedTech = Techs.FirstOrDefault(p => p.Id == id) ?? Techs.FirstOrDefault(p => p.Text == text) ?? SelectedTech;
        }
        public async System.Threading.Tasks.Task AddSuffixAndSelectAsync(string text)
        {
            var id = await _repo.AddSuffixAsync(text.Trim());
            await ReloadLookupsAsync();
            SelectedSuffix = Suffixes.FirstOrDefault(p => p.Id == id) ?? Suffixes.FirstOrDefault(p => p.Text == text) ?? SelectedSuffix;
        }

        public sealed class TechText { public long Id { get; set; } public string Text { get; set; } = string.Empty; }
        public sealed class CombinationItem { public int SequenceOrder { get; set; } public string TechniqueDisplay { get; set; } = string.Empty; public long? PrefixId { get; set; } public long TechId { get; set; } public long? SuffixId { get; set; } }
        public sealed class CombinationRow { public long CombinationId { get; set; } public string Display { get; set; } = string.Empty; public bool IsDefault { get; set; } }

        private async System.Threading.Tasks.Task LoadCombinationIntoCurrentAsync(long combinationId)
        {
            var items = await _repo.GetCombinationItemsAsync(combinationId);
            
            // Convert to CombinationItem and add if not duplicate
            foreach (var (prefix, tech, suffix, seq) in items)
            {
                // Find matching IDs from lookups
                long? prefixId = string.IsNullOrWhiteSpace(prefix) ? null : Prefixes.FirstOrDefault(p => p.Text == prefix)?.Id;
                long? techId = Techs.FirstOrDefault(t => t.Text == tech)?.Id;
                long? suffixId = string.IsNullOrWhiteSpace(suffix) ? null : Suffixes.FirstOrDefault(s => s.Text == suffix)?.Id;
                
                if (techId == null) continue; // Skip if tech not found
                
                // Check for duplicate
                bool isDuplicate = false;
                foreach (var existing in CurrentCombinationItems)
                {
                    if (existing.PrefixId == prefixId && existing.TechId == techId.Value && existing.SuffixId == suffixId)
                    {
                        isDuplicate = true;
                        break;
                    }
                }
                
                if (!isDuplicate)
                {
                    var display = string.Join(" ", new[] 
                    { 
                        string.IsNullOrWhiteSpace(prefix) ? null : prefix, 
                        tech, 
                        string.IsNullOrWhiteSpace(suffix) ? null : suffix 
                    }.Where(s => !string.IsNullOrWhiteSpace(s))) ?? tech;
                    
                    var nextSeq = CurrentCombinationItems.Count + 1;
                    CurrentCombinationItems.Add(new CombinationItem
                    {
                        SequenceOrder = nextSeq,
                        TechniqueDisplay = display,
                        PrefixId = prefixId,
                        TechId = techId.Value,
                        SuffixId = suffixId
                    });
                }
            }
        }

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
