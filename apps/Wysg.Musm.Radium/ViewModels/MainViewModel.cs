using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using ICSharpCode.AvalonEdit.CodeCompletion;
using Wysg.Musm.Editor.Controls;
using Wysg.Musm.Editor.Snippets;
using Wysg.Musm.Infrastructure.ViewModels;
using Wysg.Musm.Radium.Services;
using System.Diagnostics;

namespace Wysg.Musm.Radium.ViewModels
{
    public partial class MainViewModel : BaseViewModel
    {
        private readonly IPhraseService _phrases;
        private readonly ITenantContext _tenant;
        private readonly IPhraseCache _cache;

        public MainViewModel(IPhraseService phrases, ITenantContext tenant, IPhraseCache cache)
        {
            _phrases = phrases;
            _tenant = tenant;
            _cache = cache;

            PreviousStudies = new ObservableCollection<PreviousStudyTab>();
            NewStudyCommand = new DelegateCommand(_ => OnNewStudy());
            AddStudyCommand = new DelegateCommand(_ => OnAddStudy(), _ => PatientLocked);
            SendReportPreviewCommand = new DelegateCommand(_ => OnSendReportPreview(), _ => PatientLocked);
            SendReportCommand = new DelegateCommand(_ => OnSendReport(), _ => PatientLocked);
            SelectPreviousStudyCommand = new DelegateCommand(o => OnSelectPrevious(o), _ => PatientLocked);

            OpenStudynameMapCommand = new DelegateCommand(_ => Views.StudynameLoincWindow.Open(), _ => true);
        }

        private void OnSelectPrevious(object? o)
        {
            if (o is not PreviousStudyTab tab)
                return;

            if (SelectedPreviousStudy?.Id == tab.Id)
            {
                // already selected; ensure flags are consistent
                foreach (var t in PreviousStudies)
                    t.IsSelected = (t.Id == tab.Id);
                return;
            }

            SelectedPreviousStudy = tab; // setter will set flags
        }

        // --- Editor initialization ---
        public void InitializeEditor(EditorControl editor)
        {
            editor.MinCharsForSuggest = 1; // open when กร 1 letter
            editor.SnippetProvider = new PhraseSnippetProvider(_phrases, _tenant, _cache);
            editor.DebugSeedGhosts();
            editor.EnableGhostDebugAnchors(false);
            // kick off prefetch ASAP
            _ = Task.Run(async () =>
            {
                var all = await _phrases.GetPhrasesForTenantAsync(_tenant.TenantId);
                _cache.Set(_tenant.TenantId, all);
            });
        }

        // --- Placeholder state & models for Previous Studies Tabs ---
        private bool _patientLocked;
        public bool PatientLocked
        {
            get => _patientLocked;
            set
            {
                if (SetProperty(ref _patientLocked, value))
                {
                    // Notify command requery
                    (AddStudyCommand as DelegateCommand)?.RaiseCanExecuteChanged();
                    (SendReportPreviewCommand as DelegateCommand)?.RaiseCanExecuteChanged();
                    (SendReportCommand as DelegateCommand)?.RaiseCanExecuteChanged();
                    (SelectPreviousStudyCommand as DelegateCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public ObservableCollection<PreviousStudyTab> PreviousStudies { get; }

        private PreviousStudyTab? _selectedPreviousStudy;
        public PreviousStudyTab? SelectedPreviousStudy
        {
            get => _selectedPreviousStudy;
            set
            {
                if (SetProperty(ref _selectedPreviousStudy, value))
                {
                    // enforce radio-like selection
                    foreach (var t in PreviousStudies)
                        t.IsSelected = (value != null && t.Id == value.Id);
                }
            }
        }

        public ICommand NewStudyCommand { get; }
        public ICommand AddStudyCommand { get; }
        public ICommand SendReportPreviewCommand { get; }
        public ICommand SendReportCommand { get; }
        public ICommand SelectPreviousStudyCommand { get; }
        public ICommand OpenStudynameMapCommand { get; }

        private void OnNewStudy()
        {
            // Placeholder: toggle lock and clear previous studies
            PatientLocked = true;
            PreviousStudies.Clear();
            SelectedPreviousStudy = null;
        }

        private static readonly Random _rng = new Random();
        private void OnAddStudy()
        {
            // TEST LOGIC: add several dummy previous studies with varying datetimes and JSON payloads
            int toAdd = 3; // add 3 per click to test overflow
            for (int i = 0; i < toAdd; i++)
            {
                var dt = DateTime.Now.AddDays(-_rng.Next(0, 365)).AddMinutes(_rng.Next(0, 1440));
                var header = $"Technique: {_pick(_techniques)} on {dt:yyyy-MM-dd}. Comparison: {_pick(_comparisons)}.";
                var findings = $"Findings: {_lorem(_rng.Next(12, 24))}";
                var conclusion = $"Conclusion: {_lorem(_rng.Next(8, 16))}";

                var tab = new PreviousStudyTab
                {
                    Id = Guid.NewGuid(),
                    Title = $"{dt:yyyy-MM-dd} Prev {PreviousStudies.Count + 1}",
                    StudyDateTime = dt,
                    Header = header,
                    Findings = findings,
                    Conclusion = conclusion,
                    IsSelected = false,
                };
                tab.RawJson = JsonSerializer.Serialize(new { header = header, findings = findings, conclusion = conclusion });

                PreviousStudies.Add(tab);
                SelectedPreviousStudy = tab;
            }
        }

        private static readonly string[] _techniques = new[] { "MRI Brain", "CT Abdomen", "MRI Knee", "CT Chest" };
        private static readonly string[] _comparisons = new[] { "no prior available", "compared to 2024-01-04", "improvement since last exam" };
        private static string _pick(string[] arr) => arr[_rng.Next(arr.Length)];
        private static string _lorem(int words)
        {
            string[] pool = new[] { "lorem", "ipsum", "dolor", "sit", "amet", "consectetur", "adipiscing", "elit", "sed", "do", "eiusmod", "tempor", "incididunt", "ut", "labore", "et", "dolore", "magna", "aliqua", "ut", "enim", "ad", "minim", "veniam" };
            return string.Join(' ', Enumerable.Range(0, words).Select(_ => pool[_rng.Next(pool.Length)])) + ".";
        }

        private void OnSendReportPreview()
        {
            // Placeholder: no-op; keep for enabling button
        }

        private void OnSendReport()
        {
            // Placeholder: unlock after send
            PatientLocked = false;
            PreviousStudies.Clear();
            SelectedPreviousStudy = null;
        }

        public sealed class PreviousStudyTab : BaseViewModel
        {
            public Guid Id { get; set; }
            public string Title { get; set; } = string.Empty;
            public DateTime StudyDateTime { get; set; }
            public string Header { get; set; } = string.Empty;
            public string Findings { get; set; } = string.Empty;
            public string Conclusion { get; set; } = string.Empty;
            public string RawJson { get; set; } = string.Empty;

            private bool _isSelected;
            public bool IsSelected { get => _isSelected; set => SetProperty(ref _isSelected, value); }
            public override string ToString() => Title;
        }

        private sealed class PhraseSnippetProvider : ISnippetProvider
        {
            private readonly IPhraseService _svc;
            private readonly ITenantContext _ctx;
            private readonly IPhraseCache _cache;
            private volatile bool _prefetching;

            public PhraseSnippetProvider(IPhraseService svc, ITenantContext ctx, IPhraseCache cache)
            { _svc = svc; _ctx = ctx; _cache = cache; }

            public IEnumerable<ICompletionData> GetCompletions(ICSharpCode.AvalonEdit.TextEditor editor)
            {
                var (prefix, start) = GetWordBeforeCaret(editor);
                if (string.IsNullOrEmpty(prefix)) yield break;

                if (!_cache.Has(_ctx.TenantId) && !_prefetching)
                {
                    _prefetching = true;
                    _ = Task.Run(async () =>
                    {
                        var all = await _svc.GetPhrasesForTenantAsync(_ctx.TenantId);
                        _cache.Set(_ctx.TenantId, all);
                        _prefetching = false;
                    });
                    yield break; // no data yet; popup will open next time
                }

                var list = _cache.Get(_ctx.TenantId);
                if (list.Count == 0) yield break;

                var filtered = list.Where(t => t.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                                   .OrderBy(t => t.Length).ThenBy(t => t);

                foreach (var t in filtered)
                    yield return MusmCompletionData.Token(t);
            }

            private static (string word, int startOffset) GetWordBeforeCaret(ICSharpCode.AvalonEdit.TextEditor editor)
            {
                int caret = editor.CaretOffset;
                var line = editor.Document.GetLineByOffset(caret);
                var text = editor.Document.GetText(line.Offset, caret - line.Offset);

                int i = text.Length - 1;
                while (i >= 0)
                {
                    char ch = text[i];
                    if (!char.IsLetter(ch)) break;
                    i--;
                }
                int start = line.Offset + i + 1;
                string word = editor.Document.GetText(start, caret - start);
                return (word, start);
            }
        }

        // Lightweight ICommand implementation
        private sealed class DelegateCommand : ICommand
        {
            private readonly Action<object?> _execute;
            private readonly Predicate<object?>? _canExecute;
            public DelegateCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
            { _execute = execute; _canExecute = canExecute; }
            public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;
            public void Execute(object? parameter) => _execute(parameter);
            public event EventHandler? CanExecuteChanged;
            public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}