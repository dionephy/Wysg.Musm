using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Text.RegularExpressions;
using Wysg.Musm.Radium.Services;

namespace Wysg.Musm.Radium.ViewModels
{
    public sealed class PhraseExtractionViewModel : ViewModelBase
    {
        private readonly IPhraseService _phraseService;
        private readonly ITenantContext _tenant;
        private readonly ISnomedMapService _snomedMapService;
        private readonly ISnowstormClient _snowstormClient;

        public ObservableCollection<string> Lines { get; } = new();
        public ObservableCollection<PhraseCandidate> Candidates { get; } = new();

        private string? _selectedLine;
        public string? SelectedLine
        {
            get => _selectedLine;
            set { if (Set(ref _selectedLine, value)) _ = GenerateCandidatesAsync(); }
        }

        private int _newCount;
        public int NewCount { get => _newCount; set => Set(ref _newCount, value); }

        private bool _isBusy;
        public bool IsBusy { get => _isBusy; set { if (Set(ref _isBusy, value)) RefreshCanExec(); } }

        // NEW: Combined editor text (header + findings + conclusion, proofread or raw)
        private string _editorText = string.Empty;
        public string EditorText
        {
            get => _editorText;
            set => Set(ref _editorText, value ?? string.Empty);
        }

        // NEW: Selected text from duplicate editor (bound to TextBox)
        private string _selectedText = string.Empty;
        public string SelectedText
        {
            get => _selectedText;
            set
            {
                if (Set(ref _selectedText, value ?? string.Empty))
                {
                    // Update can-execute for Map to SNOMED button and Save Phrase Only button
                    (_mapToSnomedCmd as SimpleCommand)?.RaiseCanExecute();
                    (_savePhraseOnlyCmd as SimpleCommand)?.RaiseCanExecute();
                }
            }
        }

        // NEW: Temporary SNOMED concept mapping (until Save is clicked)
        private SnomedConcept? _tempSnomedMapping;
        public SnomedConcept? TempSnomedMapping
        {
            get => _tempSnomedMapping;
            set
            {
                if (Set(ref _tempSnomedMapping, value))
                {
                    // Update display text for temporary mapping
                    Raise(nameof(HasTempMapping));
                    Raise(nameof(TempMappingDisplay));
                    (_savePhraseSnomedCmd as SimpleCommand)?.RaiseCanExecute();
                }
            }
        }

        public bool HasTempMapping => TempSnomedMapping != null;
        public string TempMappingDisplay => TempSnomedMapping != null
            ? $"Mapped to: {TempSnomedMapping.Fsn} ({TempSnomedMapping.ConceptId})"
            : string.Empty;

        public ICommand RegenerateCommand { get; }
        public ICommand SelectAllNewCommand { get; }
        public ICommand ClearSelectionCommand { get; }
        public ICommand SaveSelectedCommand { get; }
        private readonly SimpleCommand _saveCmd;

        // NEW: SNOMED mapping commands
        private readonly ICommand _mapToSnomedCmd;
        public ICommand MapToSnomedCommand => _mapToSnomedCmd;

        private readonly ICommand _savePhraseSnomedCmd;
        public ICommand SaveCommand => _savePhraseSnomedCmd;
        
        // NEW: Save phrase without SNOMED mapping command
        private readonly ICommand _savePhraseOnlyCmd;
        public ICommand SavePhraseOnlyCommand => _savePhraseOnlyCmd;

        public PhraseExtractionViewModel(IPhraseService phraseService, ITenantContext tenant, ISnomedMapService snomedMapService, ISnowstormClient snowstormClient)
        {
            _phraseService = phraseService;
            _tenant = tenant;
            _snomedMapService = snomedMapService;
            _snowstormClient = snowstormClient;
            
            RegenerateCommand = new SimpleCommand(() => _ = GenerateCandidatesAsync(), () => SelectedLine != null);
            SelectAllNewCommand = new SimpleCommand(SelectAllNew, () => Candidates.Any());
            ClearSelectionCommand = new SimpleCommand(ClearSelections, () => Candidates.Any());
            _saveCmd = new SimpleCommand(async () => await SaveSelectedAsync(), () => !IsBusy && Candidates.Any(c => c.Selected && c.IsEnabled));
            SaveSelectedCommand = _saveCmd;

            // NEW: Map to SNOMED-CT command
            _mapToSnomedCmd = new SimpleCommand(() => OpenSnomedMappingWindow(), () => !string.IsNullOrWhiteSpace(SelectedText));

            // NEW: Save phrase with SNOMED mapping command
            _savePhraseSnomedCmd = new SimpleCommand(async () => await SavePhraseWithSnomedAsync(), () => !IsBusy && !string.IsNullOrWhiteSpace(SelectedText) && TempSnomedMapping != null);
            
            // NEW: Save phrase without SNOMED mapping command
            _savePhraseOnlyCmd = new SimpleCommand(async () => await SavePhraseOnlyAsync(), () => !IsBusy && !string.IsNullOrWhiteSpace(SelectedText));
        }

        // Safe lazy regex creation (prevents type initializer crash if pattern invalid on some locale/encoder)
        private static readonly Lazy<Regex> _rxNumbering = new(() => CreateRegex(@"^\s*(?:\d+[.)]|[A-Z]\)|-+|\u2022)\s+"));
        private static readonly Lazy<Regex> _rxArrow = new(() => CreateRegex(@"^\s*--?>\s*"));
        private static Regex RxNumbering => _rxNumbering.Value;
        private static Regex RxArrow => _rxArrow.Value;
        private static Regex CreateRegex(string pattern)
        {
            try { return new Regex(pattern, RegexOptions.Compiled); }
            catch { return new Regex("^$"); } // never matches
        }

        // Load dereportified header/findings/conclusion text: split and normalize lines
        public void LoadFromDeReportified(string header, string findings, string conclusion)
        {
            Lines.Clear();
            foreach (var s in SplitLinesAndDereportify(header)) Lines.Add(s);
            foreach (var s in SplitLinesAndDereportify(findings)) Lines.Add(s);
            foreach (var s in SplitLinesAndDereportify(conclusion)) Lines.Add(s);

            // NEW: Set editor text to concatenation of header, findings, conclusion (with double newlines as separator)
            var parts = new System.Collections.Generic.List<string>();
            if (!string.IsNullOrWhiteSpace(header)) parts.Add(header);
            if (!string.IsNullOrWhiteSpace(findings)) parts.Add(findings);
            if (!string.IsNullOrWhiteSpace(conclusion)) parts.Add(conclusion);
            EditorText = string.Join("\n\n", parts);
        }

        private static string CleanLine(string line)
        {
            var l = line.Trim();
            if (l.Length == 0) return l;
            for (int i = 0; i < 2; i++)
            {
                var n = RxNumbering.Replace(l, string.Empty);
                if (n == l) break; l = n.TrimStart();
            }
            l = RxArrow.Replace(l, string.Empty).TrimStart();
            return l;
        }

        private static string[] SplitLinesAndDereportify(string block) => (block ?? string.Empty)
            .Replace("\r", string.Empty)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(CleanLine)
            .Where(l => l.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        private async Task GenerateCandidatesAsync()
        {
            Candidates.Clear();
            NewCount = 0;
            var line = SelectedLine;
            if (string.IsNullOrWhiteSpace(line)) { RefreshCanExec(); return; }
            var words = line.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (words.Length == 0) { RefreshCanExec(); return; }

            var existing = await _phraseService.GetPhrasesForAccountAsync(_tenant.AccountId).ConfigureAwait(false);
            var existingSet = existing.ToHashSet(StringComparer.OrdinalIgnoreCase);

            var uniq = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int order = 0;
            for (int i = 0; i < words.Length; i++)
            {
                for (int j = i; j < words.Length; j++)
                {
                    var phrase = string.Join(' ', words[i..(j + 1)]).Trim();
                    if (phrase.Length == 0) continue;
                    if (!uniq.Add(phrase)) continue;
                    bool exists = existingSet.Contains(phrase);
                    bool singleWord = !phrase.Contains(' ');
                    var candidate = new PhraseCandidate(OnCandidateStateChanged)
                    {
                        Order = ++order,
                        Text = phrase,
                        Status = exists ? "Existing" : "New",
                        IsEnabled = !exists,
                        Tooltip = exists ? "Already in phrase table" : (singleWord ? "New single-word phrase" : "New multi-word phrase"),
                        Selected = !exists && singleWord
                    };
                    Candidates.Add(candidate);
                }
            }
            Recount();
            ReorderCandidates();
            RefreshCanExec();
        }

        private void OnCandidateStateChanged()
        {
            Recount();
            ReorderCandidates();
            RefreshCanExec();
        }

        private void ReorderCandidates()
        {
            if (Candidates.Count <= 1) return;
            var ordered = Candidates
                .OrderByDescending(c => c.IsEnabled)
                .ThenBy(c => c.Order)
                .ToList();
            if (!ordered.SequenceEqual(Candidates))
            {
                Candidates.Clear();
                foreach (var c in ordered) Candidates.Add(c);
            }
        }

        private void RefreshCanExec() 
        {
            (_saveCmd as SimpleCommand)?.RaiseCanExecute();
            (_savePhraseOnlyCmd as SimpleCommand)?.RaiseCanExecute();
            (_savePhraseSnomedCmd as SimpleCommand)?.RaiseCanExecute();
        }

        private void SelectAllNew()
        {
            foreach (var c in Candidates.Where(c => c.IsEnabled)) c.Selected = true;
            Recount();
            RefreshCanExec();
        }
        private void ClearSelections()
        {
            foreach (var c in Candidates.Where(c => c.IsEnabled)) c.Selected = false;
            Recount();
            RefreshCanExec();
        }
        private void Recount() => NewCount = Candidates.Count(c => c.Selected && c.IsEnabled);

        private async Task SaveSelectedAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                var toSave = Candidates.Where(c => c.Selected && c.IsEnabled).ToList();
                foreach (var c in toSave)
                {
                    try
                    {
                        // Save central (UpsertPhraseAsync already updates in-memory snapshot dictionary)
                        await _phraseService.UpsertPhraseAsync(_tenant.AccountId, c.Text).ConfigureAwait(false);
                        c.IsEnabled = false;
                        c.Status = "Saved";
                        c.Tooltip = "Saved to phrase table";
                    }
                    catch (Exception ex)
                    {
                        c.Status = "Error";
                        c.Tooltip = ex.Message;
                    }
                }
                Recount();
                ReorderCandidates();
            }
            finally
            {
                IsBusy = false;
                RefreshCanExec();
            }
        }

        // NEW: Open SNOMED mapping window with selected text pre-filled
        private void OpenSnomedMappingWindow()
        {
            if (string.IsNullOrWhiteSpace(SelectedText)) return;

            try
            {
                // We need a placeholder phrase ID since we haven't saved the phrase yet
                // Use -1 to indicate this is a temporary phrase not yet in DB
                long tempPhraseId = -1;
                var phraseText = SelectedText.Trim();

                var window = new Views.PhraseSnomedLinkWindow(tempPhraseId, phraseText, _tenant.AccountId, _snomedMapService, _snowstormClient);
                window.Owner = System.Windows.Application.Current.MainWindow;

                // Subscribe to window closed event to capture the selected concept
                window.Closed += (s, e) =>
                {
                    // Check if a concept was selected and mapped
                    if (window.VM.SelectedConcept != null)
                    {
                        // Store temporarily until Save is clicked
                        TempSnomedMapping = window.VM.SelectedConcept;
                    }
                };

                window.ShowDialog();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PhraseExtractionViewModel] Error opening SNOMED window: {ex.Message}");
            }
        }

        // NEW: Save phrase without SNOMED mapping to database
        private async Task SavePhraseOnlyAsync()
        {
            if (IsBusy || string.IsNullOrWhiteSpace(SelectedText)) return;

            IsBusy = true;
            try
            {
                var phraseText = SelectedText.Trim();

                // Save phrase as account-specific phrase (not global)
                var phraseInfo = await _phraseService.UpsertPhraseAsync(_tenant.AccountId, phraseText, active: true).ConfigureAwait(false);

                // Success: clear selection and temp mapping
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    // Clear temp mapping if any
                    TempSnomedMapping = null;

                    // Clear selected text (this will trigger editor selection release in view)
                    SelectedText = string.Empty;

                    // Show success message
                    System.Windows.MessageBox.Show(
                        $"Phrase '{phraseText}' saved successfully.",
                        "Success",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information);
                });
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Error saving phrase:\n{ex.Message}",
                    "Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
                RefreshCanExec();
            }
        }

        // NEW: Save phrase with SNOMED mapping to database
        private async Task SavePhraseWithSnomedAsync()
        {
            if (IsBusy || string.IsNullOrWhiteSpace(SelectedText) || TempSnomedMapping == null) return;

            IsBusy = true;
            try
            {
                var phraseText = SelectedText.Trim();

                // 1. Upsert phrase to get phrase ID (save as account-specific phrase)
                var phraseInfo = await _phraseService.UpsertPhraseAsync(_tenant.AccountId, phraseText, active: true).ConfigureAwait(false);

                // 2. Cache SNOMED concept
                await _snomedMapService.CacheConceptAsync(TempSnomedMapping).ConfigureAwait(false);

                // 3. Map phrase to SNOMED concept
                // IMPORTANT: Use phraseInfo.AccountId instead of _tenant.AccountId to ensure consistency
                // The phrase was saved with _tenant.AccountId, so the mapping must use the same accountId
                bool mapped = await _snomedMapService.MapPhraseAsync(
                    phraseInfo.Id,
                    phraseInfo.AccountId, // Use the accountId from the saved phrase, not _tenant.AccountId
                    TempSnomedMapping.ConceptId,
                    mappingType: "exact",
                    confidence: null,
                    notes: "Mapped from phrase extraction window"
                ).ConfigureAwait(false);

                if (mapped)
                {
                    // Success: clear selection and temp mapping
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        // Clear temp mapping
                        TempSnomedMapping = null;

                        // Clear selected text (this will trigger editor selection release in view)
                        SelectedText = string.Empty;

                        // Show success message
                        System.Windows.MessageBox.Show(
                            $"Phrase '{phraseText}' saved and mapped to SNOMED-CT successfully.",
                            "Success",
                            System.Windows.MessageBoxButton.OK,
                            System.Windows.MessageBoxImage.Information);
                    });
                }
                else
                {
                    System.Windows.MessageBox.Show(
                        $"Failed to map phrase '{phraseText}' to SNOMED-CT.",
                        "Error",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Error saving phrase with SNOMED mapping:\n{ex.Message}",
                    "Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
                RefreshCanExec();
            }
        }

        // NEW: Public method to be called from view when selection changes
        public void OnEditorSelectionCleared()
        {
            // When editor selection is cleared programmatically, also clear temp mapping
            TempSnomedMapping = null;
        }
    }

    public sealed class PhraseCandidate : ViewModelBase
    {
        private readonly Action _notify;
        public PhraseCandidate(Action notify) { _notify = notify; }
        private bool _selected;
        private bool _isEnabled = true;
        private string _status = string.Empty;
        private string _tooltip = string.Empty;
        public int Order { get; set; }
        public string Text { get; set; } = string.Empty;
        public bool Selected { get => _selected; set { if (Set(ref _selected, value)) _notify(); } }
        public bool IsEnabled { get => _isEnabled; set { if (Set(ref _isEnabled, value)) _notify(); } }
        public string Status { get => _status; set { if (Set(ref _status, value)) _notify(); } }
        public string Tooltip { get => _tooltip; set { if (Set(ref _tooltip, value)) _notify(); } }
    }
}
