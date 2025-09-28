using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using Wysg.Musm.Editor.Controls;
using Wysg.Musm.Infrastructure.ViewModels;
using Wysg.Musm.Radium.Services;
using System.Text.RegularExpressions;

namespace Wysg.Musm.Radium.ViewModels
{
    public partial class MainViewModel : BaseViewModel
    {
        private readonly IPhraseService _phrases;
        private readonly ITenantContext _tenant;
        private readonly IPhraseCache _cache;

        public MainViewModel(IPhraseService phrases, ITenantContext tenant, IPhraseCache cache)
        {
            _phrases = phrases; _tenant = tenant; _cache = cache;
            PreviousStudies = new ObservableCollection<PreviousStudyTab>();
            NewStudyCommand = new DelegateCommand(_ => OnNewStudy());
            AddStudyCommand = new DelegateCommand(_ => OnAddStudy(), _ => PatientLocked);
            SendReportPreviewCommand = new DelegateCommand(_ => OnSendReportPreview(), _ => PatientLocked);
            SendReportCommand = new DelegateCommand(_ => OnSendReport(), _ => PatientLocked);
            SelectPreviousStudyCommand = new DelegateCommand(o => OnSelectPrevious(o), _ => PatientLocked);
            OpenStudynameMapCommand = new DelegateCommand(_ => Views.StudynameLoincWindow.Open());
            OpenPhraseManagerCommand = new DelegateCommand(_ => Views.PhrasesWindow.Open());
        }

        // Text bound to editors
        private string _headerText = string.Empty; public string HeaderText { get => _headerText; set => SetProperty(ref _headerText, value); }
        private string _findingsText = string.Empty; public string FindingsText { get => _findingsText; set => SetProperty(ref _findingsText, value); }
        private string _conclusionText = string.Empty; public string ConclusionText { get => _conclusionText; set => SetProperty(ref _conclusionText, value); }

        // Phrase capitalization dictionary
        private HashSet<string>? _keepCapsFirstTokens; // tokens for which we keep initial capitalization
        private bool _capsLoaded;
        private async Task EnsureCapsAsync()
        {
            if (_capsLoaded) return;
            try
            {
                var list = await _phrases.GetPhrasesForAccountAsync(_tenant.AccountId).ConfigureAwait(false);
                _keepCapsFirstTokens = new HashSet<string>(list.Select(p => FirstToken(p)).Where(t => t.Length > 0 && char.IsUpper(t[0])), System.StringComparer.OrdinalIgnoreCase);
            }
            catch { _keepCapsFirstTokens = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase); }
            finally { _capsLoaded = true; }
        }
        private static string FirstToken(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;
            int i = 0; while (i < s.Length && !char.IsWhiteSpace(s[i])) i++; return s.Substring(0, i);
        }

        public void InitializeEditor(EditorControl editor)
        {
            editor.MinCharsForSuggest = 1;
            editor.SnippetProvider = new PhraseCompletionProvider(_phrases, _tenant, _cache);
            editor.DebugSeedGhosts();
            editor.EnableGhostDebugAnchors(false);
            _ = Task.Run(async () =>
            {
                var all = await _phrases.GetPhrasesForAccountAsync(_tenant.AccountId);
                _cache.Set(_tenant.AccountId, all);
                await EnsureCapsAsync();
            });
        }

        private bool _patientLocked; public bool PatientLocked
        {
            get => _patientLocked;
            set
            {
                if (SetProperty(ref _patientLocked, value))
                {
                    (AddStudyCommand as DelegateCommand)?.RaiseCanExecuteChanged();
                    (SendReportPreviewCommand as DelegateCommand)?.RaiseCanExecuteChanged();
                    (SendReportCommand as DelegateCommand)?.RaiseCanExecuteChanged();
                    (SelectPreviousStudyCommand as DelegateCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public ObservableCollection<PreviousStudyTab> PreviousStudies { get; }
        private PreviousStudyTab? _selectedPreviousStudy; public PreviousStudyTab? SelectedPreviousStudy
        {
            get => _selectedPreviousStudy;
            set
            {
                if (SetProperty(ref _selectedPreviousStudy, value))
                    foreach (var t in PreviousStudies) t.IsSelected = (value != null && t.Id == value!.Id);
            }
        }

        private void OnSelectPrevious(object? o)
        {
            if (o is not PreviousStudyTab tab) return;
            if (SelectedPreviousStudy?.Id == tab.Id)
            {
                foreach (var t in PreviousStudies) t.IsSelected = (t.Id == tab.Id);
                return;
            }
            SelectedPreviousStudy = tab;
        }

        // Commands
        public ICommand NewStudyCommand { get; }
        public ICommand AddStudyCommand { get; }
        public ICommand SendReportPreviewCommand { get; }
        public ICommand SendReportCommand { get; }
        public ICommand SelectPreviousStudyCommand { get; }
        public ICommand OpenStudynameMapCommand { get; }
        public ICommand OpenPhraseManagerCommand { get; }

        private static readonly Random _rng = new();
        private void OnNewStudy()
        {
            PatientLocked = true;
            PreviousStudies.Clear();
            SelectedPreviousStudy = null;
            HeaderText = FindingsText = ConclusionText = string.Empty;
            _reportified = false; OnPropertyChanged(nameof(Reportified));
        }

        private void OnAddStudy()
        {
            var dt = System.DateTime.Now.AddDays(-_rng.Next(0, 120));
            var header = "Technique: MRI Brain. Comparison: none.";
            var findings = "Mild chronic microangiopathy.";
            var conclusion = "No acute intracranial hemorrhage.";
            var tab = new PreviousStudyTab { Id = Guid.NewGuid(), Title = dt.ToString("yyyy-MM-dd"), StudyDateTime = dt, Header = header, Findings = findings, Conclusion = conclusion, RawJson = JsonSerializer.Serialize(new { header, findings, conclusion }) };
            PreviousStudies.Add(tab); SelectedPreviousStudy = tab;
        }
        private void OnSendReportPreview() { }
        private void OnSendReport() { PatientLocked = false; }

        // Reportify Toggle
        private bool _reportified; public bool Reportified
        {
            get => _reportified;
            set
            {
                if (SetProperty(ref _reportified, value))
                {
                    if (value) { HeaderText = ReportifyBlock(HeaderText, false); FindingsText = ReportifyBlock(FindingsText, false); ConclusionText = ReportifyBlock(ConclusionText, true); }
                    else { HeaderText = DereportifyBlock(HeaderText, false); FindingsText = DereportifyBlock(FindingsText, false); ConclusionText = DereportifyBlock(ConclusionText, true); }
                }
            }
        }

        // Reportify (paragraph-based for conclusion)
        private string ReportifyBlock(string input, bool isConclusion)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            // Normalize newlines
            input = input.Replace("\r\n", "\n");
            // Split paragraphs by double newline
            var paragraphs = input.Split(new string[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries);

            for (int p = 0; p < paragraphs.Length; p++)
            {
                var lines = paragraphs[p].Split('\n');
                for (int i = 0; i < lines.Length; i++)
                    lines[i] = ReportifySentence(lines[i]);
                paragraphs[p] = string.Join(System.Environment.NewLine, lines);
            }

            if (isConclusion && paragraphs.Length > 1)
            {
                // Number per paragraph: first line numbered; subsequent lines in paragraph indented
                for (int i = 0; i < paragraphs.Length; i++)
                {
                    var lines = paragraphs[i].Split(new[] { System.Environment.NewLine }, StringSplitOptions.None);
                    if (lines.Length > 0)
                    {
                        lines[0] = $"{i + 1}. {ReportifyLineExt(lines[0])}"; // ensure punctuation on first line
                        for (int j = 1; j < lines.Length; j++)
                            lines[j] = "   " + lines[j];
                        paragraphs[i] = string.Join(System.Environment.NewLine, lines);
                    }
                }
            }

            return string.Join(System.Environment.NewLine + System.Environment.NewLine, paragraphs);
        }

        // Dereportify (inverse transform without original cache)
        private string DereportifyBlock(string input, bool isConclusion)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            input = input.Replace("\r\n", "\n");
            var paragraphs = input.Split(new string[] { "\n\n" }, StringSplitOptions.None);
            for (int p = 0; p < paragraphs.Length; p++)
            {
                var lines = paragraphs[p].Split(new[] { '\n' }, StringSplitOptions.None).ToList();
                for (int i = 0; i < lines.Count; i++)
                {
                    var line = lines[i];
                    // remove numbered prefix only on first line of a paragraph
                    if (i == 0)
                        line = Regex.Replace(line, @"^\s*\d+\.\s+", string.Empty);
                    // remove indentation from continuation lines
                    line = Regex.Replace(line, @"^ {1,4}", string.Empty);
                    // remove trailing period (keep ellipsis)
                    if (line.EndsWith('.') && !line.EndsWith("..")) line = line[..^1];
                    // normalize arrow to have single following space
                    line = Regex.Replace(line, @"^\s*-->\s*", "--> ");
                    // decap
                    line = DecapUnlessDictionary(line);
                    lines[i] = line;
                }
                paragraphs[p] = string.Join(System.Environment.NewLine, lines);
            }
            return string.Join(System.Environment.NewLine + System.Environment.NewLine, paragraphs);
        }

        private string ReportifySentence(string sentence)
        {
            if (string.IsNullOrWhiteSpace(sentence)) return string.Empty;
            string res = PurifySentence(sentence, true);
            res = GetBody(res, true);
            if (res.Length == 0) return string.Empty;
            res = char.ToUpper(res[0]) + (res.Length > 1 ? res[1..] : string.Empty);
            char last = res[^1];
            if (char.IsLetterOrDigit(last) || last == ')') res += ".";
            return res;
        }
        private string ReportifyLineExt(string str)
        {
            if (string.IsNullOrEmpty(str)) return str;
            string r = char.ToUpper(str[0]) + (str.Length > 1 ? str[1..] : string.Empty);
            if (r.EndsWith('.')) return r;
            if (char.IsLetterOrDigit(r[^1])) r += '.';
            return r;
        }
        private string DecapUnlessDictionary(string line)
        {
            if (string.IsNullOrEmpty(line)) return line;
            int idx = 0; while (idx < line.Length && char.IsWhiteSpace(line[idx])) idx++;
            if (idx >= line.Length) return line;
            char c = line[idx]; if (!char.IsUpper(c)) return line;
            int end = idx; while (end < line.Length && char.IsLetter(line[end])) end++;
            var token = line.Substring(idx, end - idx);
            if (_keepCapsFirstTokens != null && _keepCapsFirstTokens.Contains(token)) return line;
            var lowered = char.ToLower(c) + line[(idx + 1)..];
            return idx == 0 ? lowered : line[..idx] + lowered;
        }

        // Purify helpers (from legacy)
        private string PurifySentence(string sentence, bool reportify)
        {
            string res = sentence.Trim();
            res = _rxArrow.Replace(res, reportify ? " $1 " : "$1 ");
            res = _rxBullet.Replace(res, "$1 ");
            res = _rxAfterPunct.Replace(res, "$1 ");
            res = _rxParensSpace.Replace(res, " $& ");
            res = _rxNumberUnit.Replace(res, "$1 $3");
            res = _rxDot.Replace(res, ". ");
            res = _rxSpaces.Replace(res, " ");
            res = _rxLParen.Replace(res, "(");
            res = _rxRParen.Replace(res, ")");
            return res.TrimEnd();
        }
        private string GetBody(string input, bool onlyNumber)
        { string pattern = onlyNumber ? @"^(\d+\. |\d+\.)" : @"^(--> |-->|->|- |-|\*|\d+\. |\d+\.|\d+\))"; return Regex.Replace(input, pattern, string.Empty).TrimEnd(); }

        // Regexes
        private static readonly Regex _rxArrow = new(@"^(--?>)", RegexOptions.Compiled);
        private static readonly Regex _rxBullet = new(@"^(-(?!\->)|\*)(?<!-)", RegexOptions.Compiled);
        private static readonly Regex _rxAfterPunct = new(@"([;,:](?<!\d:))(?!\s)", RegexOptions.Compiled);
        private static readonly Regex _rxParensSpace = new(@"(?<=\S)\((?=\S)(?!\s*s\s*\))|(?<=\S)\)(?=[^\.,\s])(?!:)", RegexOptions.Compiled);
        private static readonly Regex _rxNumberUnit = new(@"(\d+(\.\d+)?)(cm|mm|ml)", RegexOptions.Compiled);
        private static readonly Regex _rxDot = new(@"(?<=\D)\.(?=[^\.\)]|$)", RegexOptions.Compiled);
        private static readonly Regex _rxSpaces = new(@"\s+", RegexOptions.Compiled);
        private static readonly Regex _rxLParen = new(@"\(\s+", RegexOptions.Compiled);
        private static readonly Regex _rxRParen = new(@"\s+\)", RegexOptions.Compiled);

        // Previous study model
        public sealed class PreviousStudyTab : BaseViewModel
        {
            public Guid Id { get; set; }
            public string Title { get; set; } = string.Empty;
            public System.DateTime StudyDateTime { get; set; }
            public string Header { get; set; } = string.Empty;
            public string Findings { get; set; } = string.Empty;
            public string Conclusion { get; set; } = string.Empty;
            public string RawJson { get; set; } = string.Empty;
            private bool _isSelected; public bool IsSelected { get => _isSelected; set => SetProperty(ref _isSelected, value); }
            public override string ToString() => Title;
        }

        private sealed class DelegateCommand : ICommand
        {
            private readonly Action<object?> _exec; private readonly Predicate<object?>? _can;
            public DelegateCommand(Action<object?> exec, Predicate<object?>? can = null) { _exec = exec; _can = can; }
            public bool CanExecute(object? parameter) => _can?.Invoke(parameter) ?? true;
            public void Execute(object? parameter) => _exec(parameter);
            public event System.EventHandler? CanExecuteChanged;
            public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, System.EventArgs.Empty);
        }

        public (string header, string findings, string conclusion) GetDereportifiedSections()
        {
            string h = Reportified ? DereportifyBlock(HeaderText, false) : HeaderText;
            string f = Reportified ? DereportifyBlock(FindingsText, false) : FindingsText;
            string c = Reportified ? DereportifyBlock(ConclusionText, true) : ConclusionText;
            return (h, f, c);
        }
    }
}