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

        public ICommand RegenerateCommand { get; }
        public ICommand SelectAllNewCommand { get; }
        public ICommand ClearSelectionCommand { get; }
        public ICommand SaveSelectedCommand { get; }
        private readonly SimpleCommand _saveCmd;

        public PhraseExtractionViewModel(IPhraseService phraseService, ITenantContext tenant)
        {
            _phraseService = phraseService;
            _tenant = tenant;
            RegenerateCommand = new SimpleCommand(() => _ = GenerateCandidatesAsync(), () => SelectedLine != null);
            SelectAllNewCommand = new SimpleCommand(SelectAllNew, () => Candidates.Any());
            ClearSelectionCommand = new SimpleCommand(ClearSelections, () => Candidates.Any());
            _saveCmd = new SimpleCommand(async () => await SaveSelectedAsync(), () => !IsBusy && Candidates.Any(c => c.Selected && c.IsEnabled));
            SaveSelectedCommand = _saveCmd;
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

        private void RefreshCanExec() => _saveCmd.RaiseCanExecute();

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
