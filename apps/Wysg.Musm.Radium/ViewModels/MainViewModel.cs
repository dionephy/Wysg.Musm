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
using Wysg.Musm.Radium.Services.Procedures;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Threading;

namespace Wysg.Musm.Radium.ViewModels
{
    public partial class MainViewModel : BaseViewModel
    {
        private readonly IPhraseService _phrases;
        private readonly ITenantContext _tenant;
        private readonly IPhraseCache _cache;
        private readonly PacsService _pacs = new();
        private readonly ICentralDataSourceProvider? _centralProvider; // unused placeholder
        private readonly IRadStudyRepository? _studyRepo;
        private readonly INewStudyProcedure? _newStudyProc;
        private readonly IRadiumLocalSettings? _localSettings;
        private readonly ILockStudyProcedure? _lockStudyProc;

        public MainViewModel(IPhraseService phrases, ITenantContext tenant, IPhraseCache cache, IRadStudyRepository? studyRepo = null, INewStudyProcedure? newStudyProc = null, IRadiumLocalSettings? localSettings = null, ILockStudyProcedure? lockStudyProc = null)
        {
            _phrases = phrases; _tenant = tenant; _cache = cache; _studyRepo = studyRepo; _newStudyProc = newStudyProc; _localSettings = localSettings; _lockStudyProc = lockStudyProc;
            PreviousStudies = new ObservableCollection<PreviousStudyTab>();
            NewStudyCommand = new DelegateCommand(_ => OnNewStudy());
            TestNewStudyProcedureCommand = new DelegateCommand(async _ => await RunNewStudyProcedureAsync());
            AddStudyCommand = new DelegateCommand(_ => OnAddStudy(), _ => PatientLocked);
            SendReportPreviewCommand = new DelegateCommand(_ => OnSendReportPreview(), _ => PatientLocked);
            SendReportCommand = new DelegateCommand(_ => OnSendReport(), _ => PatientLocked);
            SelectPreviousStudyCommand = new DelegateCommand(o => OnSelectPrevious(o), _ => PatientLocked);
            OpenStudynameMapCommand = new DelegateCommand(_ => Views.StudynameLoincWindow.Open());
            OpenPhraseManagerCommand = new DelegateCommand(_ => Views.PhrasesWindow.Open());
        }

        #region Current Study Metadata
        private string _patientName = string.Empty; public string PatientName { get => _patientName; set { if (SetProperty(ref _patientName, value)) UpdateCurrentStudyLabel(); } }
        private string _patientNumber = string.Empty; public string PatientNumber { get => _patientNumber; set { if (SetProperty(ref _patientNumber, value)) UpdateCurrentStudyLabel(); } }
        private string _patientSex = string.Empty; public string PatientSex { get => _patientSex; set { if (SetProperty(ref _patientSex, value)) UpdateCurrentStudyLabel(); } }
        private string _patientAge = string.Empty; public string PatientAge { get => _patientAge; set { if (SetProperty(ref _patientAge, value)) UpdateCurrentStudyLabel(); } }
        private string _studyName = string.Empty; public string StudyName { get => _studyName; set { if (SetProperty(ref _studyName, value)) UpdateCurrentStudyLabel(); } }
        private string _studyDateTime = string.Empty; public string StudyDateTime { get => _studyDateTime; set { if (SetProperty(ref _studyDateTime, value)) UpdateCurrentStudyLabel(); } }

        private string _currentStudyLabel = "Current\nStudy"; public string CurrentStudyLabel { get => _currentStudyLabel; private set => SetProperty(ref _currentStudyLabel, value); }
        private void UpdateCurrentStudyLabel()
        {
            string fmt(string s) => string.IsNullOrWhiteSpace(s) ? "?" : s.Trim();
            string dt = StudyDateTime;
            if (!string.IsNullOrWhiteSpace(dt) && DateTime.TryParse(dt, out var parsed)) dt = parsed.ToString("yyyy-MM-dd HH:mm:ss");
            else if (string.IsNullOrWhiteSpace(dt)) dt = "?";
            CurrentStudyLabel = $"{fmt(PatientName)}({fmt(PatientSex)}/{fmt(PatientAge)}) - {fmt(PatientNumber)}\n{fmt(StudyName)} ({dt})";
        }

        // Simple status (future: bind to status textbox)
        private string _statusText = "Ready"; public string StatusText { get => _statusText; set => SetProperty(ref _statusText, value); }
        private bool _statusIsError; public bool StatusIsError { get => _statusIsError; set => SetProperty(ref _statusIsError, value); }

        private void SetStatus(string message, bool isError=false){ StatusText = message; StatusIsError = isError; }

        private async Task FetchCurrentStudyAsync()
        {
            try
            {
                var nameTask = _pacs.GetSelectedNameFromSearchResultsAsync();
                var numberTask = _pacs.GetSelectedIdFromSearchResultsAsync();
                var sexTask = _pacs.GetSelectedSexFromSearchResultsAsync();
                var ageTask = _pacs.GetSelectedAgeFromSearchResultsAsync();
                var studyNameTask = _pacs.GetSelectedStudynameFromSearchResultsAsync();
                var dtTask = _pacs.GetSelectedStudyDateTimeFromSearchResultsAsync();
                var birthTask = _pacs.GetSelectedBirthDateFromSearchResultsAsync();
                await Task.WhenAll(nameTask, numberTask, sexTask, ageTask, studyNameTask, dtTask); await birthTask;
                PatientName = nameTask.Result ?? string.Empty;
                PatientNumber = numberTask.Result ?? string.Empty;
                // Retry patient number if empty (UIA transient)
                if (string.IsNullOrWhiteSpace(PatientNumber))
                {
                    for (int attempt = 1; attempt <= 5 && string.IsNullOrWhiteSpace(PatientNumber); attempt++)
                    {
                        await Task.Delay(150);
                        try
                        {
                            var retry = await _pacs.GetSelectedIdFromSearchResultsAsync();
                            if (!string.IsNullOrWhiteSpace(retry))
                            {
                                PatientNumber = retry;
                                Debug.WriteLine($"[Retry] PatientNumber recovered on attempt {attempt}");
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[Retry] PatientNumber attempt {attempt} failed: {ex.Message}");
                        }
                    }
                }
                PatientSex = sexTask.Result ?? string.Empty;
                PatientAge = ageTask.Result ?? string.Empty;
                StudyName = studyNameTask.Result ?? string.Empty;
                StudyDateTime = dtTask.Result ?? string.Empty;
                await LoadPreviousStudiesForPatientAsync(PatientNumber);
                await PersistCurrentStudyAsync(birthTask.Result);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[FetchCurrentStudy] error: " + ex.Message);
            }
        }
        #endregion

        #region Editor Text
// Add raw fields to preserve original un-reportified text
        private string _rawHeader = string.Empty;
        private string _rawFindings = string.Empty;
        private string _rawConclusion = string.Empty;

        private void CaptureRawIfNeeded()
        {
            if (!_reportified)
            {
                _rawHeader = HeaderText;
                _rawFindings = ReportFindings;
                _rawConclusion = ConclusionText;
            }
        }

        private bool _suppressAutoToggle; // prevent recursive loops

        private bool _reportified; public bool Reportified
        {
            get => _reportified;
            set
            {
                if (SetProperty(ref _reportified, value))
                {
                    if (value)
                    {
                        // entering reportified: capture current raw text and apply simple formatting while suppressing auto toggle
                        CaptureRawIfNeeded();
                        _suppressAutoToggle = true;
                        HeaderText = SimpleReportifyBlock(_rawHeader);
                        FindingsText = SimpleReportifyBlock(_rawFindings);
                        ConclusionText = ReportifyConclusion(_rawConclusion);
                        _suppressAutoToggle = false;
                    }
                    else
                    {
                        // exiting: restore raw (suppress auto toggle during restore)
                        _suppressAutoToggle = true;
                        HeaderText = _rawHeader;
                        FindingsText = _rawFindings;
                        ConclusionText = _rawConclusion;
                        _suppressAutoToggle = false;
                    }
                }
            }
        }

        private string SimpleReportifyBlock(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            var lines = input.Replace("\r\n","\n").Split('\n');
            for (int i=0;i<lines.Length;i++)
            {
                var l = lines[i].Trim();
                if (l.Length==0){ lines[i]=string.Empty; continue; }
                l = char.ToUpper(l[0]) + (l.Length>1? l[1..]:string.Empty);
                if (!l.EndsWith('.') && char.IsLetterOrDigit(l[^1])) l += '.';
                lines[i] = l;
            }
            return string.Join("\n", lines);
        }

        // Override setters to trigger auto-unreportify when editing while reportified (raw captured BEFORE json update to avoid lag)
        private string _headerText = string.Empty; public string HeaderText { get => _headerText; set { if (value != _headerText) { AutoUnreportifyOnEdit(); if (!_reportified) _rawHeader = value; if (SetProperty(ref _headerText, value)) { UpdateCurrentReportJson(); } } } }
        private string _findingsText = string.Empty; public string FindingsText { get => _findingsText; set { if (value != _findingsText) { AutoUnreportifyOnEdit(); if (!_reportified) _rawFindings = value; if (SetProperty(ref _findingsText, value)) { UpdateCurrentReportJson(); } } } }
        private string _conclusionText = string.Empty; public string ConclusionText { get => _conclusionText; set { if (value != _conclusionText) { AutoUnreportifyOnEdit(); if (!_reportified) _rawConclusion = value; if (SetProperty(ref _conclusionText, value)) { UpdateCurrentReportJson(); } } } }
        private string _reportFindings = string.Empty; public string ReportFindings { get => _findingsText; set { if (value != _findingsText) { AutoUnreportifyOnEdit(); if (!_reportified) _rawFindings = value; if (SetProperty(ref _findingsText, value)) { UpdateCurrentReportJson(); } } } }

        // Simplified conclusion reportify: sentence capitalization + ensure trailing period
        private string ReportifyConclusion(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            var lines = input.Replace("\r\n", "\n").Split('\n');
            for (int i=0;i<lines.Length;i++)
            {
                var l = lines[i].Trim();
                if (l.Length==0){ lines[i]=string.Empty; continue; }
                l = char.ToUpper(l[0]) + (l.Length>1? l[1..]:string.Empty);
                if (!l.EndsWith('.') && char.IsLetterOrDigit(l[^1])) l += '.';
                lines[i] = l;
            }
            return string.Join("\n", lines);
        }

// Replace CurrentReportJson property and add guards + parser
        private bool _updatingFromEditors; // guard when we push JSON from editors
        private bool _updatingFromJson;    // guard when we apply JSON into editors
        private string _currentReportJson = "{}"; 
        public string CurrentReportJson 
        { 
            get => _currentReportJson; 
            set 
            { 
                if (SetProperty(ref _currentReportJson, value)) 
                { 
                    if (_updatingFromEditors) return; // ignore self-generated updates
                    ApplyJsonToEditors(value); 
                } 
            } 
        }

        private void ApplyJsonToEditors(string json)
        {
            if (_updatingFromJson) return;
            try
            {
                if (string.IsNullOrWhiteSpace(json) || json.Length < 2) return; // ignore trivial
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                string newFindings = root.TryGetProperty("findings", out var fEl) ? (fEl.GetString() ?? string.Empty) : string.Empty;
                string newConclusion = root.TryGetProperty("conclusion", out var cEl) ? (cEl.GetString() ?? string.Empty) : string.Empty;
                _updatingFromJson = true;
                // Update raw stores
                _rawFindings = newFindings;
                _rawConclusion = newConclusion;
                if (!_reportified)
                {
                    // directly reflect in editors
                    if (FindingsText != newFindings) _findingsText = newFindings; // assign backing field to avoid auto-unreportify
                    if (ConclusionText != newConclusion) _conclusionText = newConclusion;
                    OnPropertyChanged(nameof(FindingsText));
                    OnPropertyChanged(nameof(ConclusionText));
                }
                else
                {
                    // reapply transformed state
                    FindingsText = SimpleReportifyBlock(newFindings);
                    ConclusionText = ReportifyConclusion(newConclusion);
                }
            }
            catch
            {
                // Ignore parse errors while user is typing invalid JSON
            }
            finally { _updatingFromJson = false; }
        }

        private void UpdateCurrentReportJson()
        {
            try
            {
                var obj = new {
                    findings = _rawFindings == string.Empty ? ReportFindings : _rawFindings,
                    conclusion = _rawConclusion == string.Empty ? ConclusionText : _rawConclusion
                };
                _updatingFromEditors = true;
                CurrentReportJson = JsonSerializer.Serialize(obj, new JsonSerializerOptions{WriteIndented=true});
            }
            catch { }
            finally { _updatingFromEditors = false; }
        }
        #endregion

        #region Phrase Caps
        private HashSet<string>? _keepCapsFirstTokens; private bool _capsLoaded;
        private async Task EnsureCapsAsync()
        {
            if (_capsLoaded) return;
            try
            {
                var list = await _phrases.GetPhrasesForAccountAsync(_tenant.AccountId).ConfigureAwait(false);
                _keepCapsFirstTokens = new HashSet<string>(list.Select(p => FirstToken(p)).Where(t => t.Length > 0 && char.IsUpper(t[0])), StringComparer.OrdinalIgnoreCase);
            }
            catch { _keepCapsFirstTokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase); }
            finally { _capsLoaded = true; }
        }
        private static string FirstToken(string s)
        { if (string.IsNullOrWhiteSpace(s)) return string.Empty; int i=0; while (i<s.Length && !char.IsWhiteSpace(s[i])) i++; return s[..i]; }
        #endregion

        public void InitializeEditor(EditorControl editor)
        {
            editor.MinCharsForSuggest = 1;
            editor.SnippetProvider = new PhraseCompletionProvider(_phrases, _tenant, _cache);
            editor.EnableGhostDebugAnchors(false);
            _ = Task.Run(async () =>
            {
                var all = await _phrases.GetPhrasesForAccountAsync(_tenant.AccountId);
                _cache.Set(_tenant.AccountId, all);
                await EnsureCapsAsync();
            });
        }

        #region Locks & Commands
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
        public ICommand NewStudyCommand { get; }
        public ICommand TestNewStudyProcedureCommand { get; }
        public ICommand AddStudyCommand { get; }
        public ICommand SendReportPreviewCommand { get; }
        public ICommand SendReportCommand { get; }
        public ICommand SelectPreviousStudyCommand { get; }
        public ICommand OpenStudynameMapCommand { get; }
        public ICommand OpenPhraseManagerCommand { get; }
        #endregion

        #region Previous Studies
        public ObservableCollection<PreviousStudyTab> PreviousStudies { get; }
        public sealed class PreviousStudyTab : BaseViewModel
        {
            public Guid Id { get; set; }
            public string Title { get; set; } = string.Empty; // YYYY-MM-DD MOD
            public DateTime StudyDateTime { get; set; }
            public string Modality { get; set; } = string.Empty;
            private string _header = string.Empty; public string Header { get => _header; set => SetProperty(ref _header, value); }
            private string _findings = string.Empty; public string Findings { get => _findings; set => SetProperty(ref _findings, value); }
            private string _conclusion = string.Empty; public string Conclusion { get => _conclusion; set => SetProperty(ref _conclusion, value); }
            public string RawJson { get; set; } = string.Empty;
            public string OriginalHeader { get; set; } = string.Empty;
            public string OriginalFindings { get; set; } = string.Empty;
            public string OriginalConclusion { get; set; } = string.Empty;
            public bool ReportifiedApplied { get; set; }
            private bool _isSelected; public bool IsSelected { get => _isSelected; set => SetProperty(ref _isSelected, value); }
            // Reports available for this study (multiple report rows per study)
            public ObservableCollection<PreviousReportChoice> Reports { get; } = new();
            private PreviousReportChoice? _selectedReport; public PreviousReportChoice? SelectedReport { get => _selectedReport; set { if (SetProperty(ref _selectedReport, value)) ApplyReportSelection(value); } }
            public void ApplyReportSelection(PreviousReportChoice? rep)
            {
                if (rep == null) return;
                OriginalFindings = rep.Findings;
                OriginalConclusion = rep.Conclusion;
                Findings = rep.Findings;
                Conclusion = rep.Conclusion;
            }
            public override string ToString() => Title;
        }

        public sealed class PreviousReportChoice : BaseViewModel
        {
            public DateTime? ReportDateTime { get; set; }
            public string CreatedBy { get; set; } = string.Empty;
            public string Studyname { get; set; } = string.Empty;
            public string Findings { get; set; } = string.Empty;
            public string Conclusion { get; set; } = string.Empty;
            public string Display => $"{Studyname} ({StudyDateTimeFmt}) - {ReportDateTimeFmt} by {CreatedBy}";
            private string StudyDateTimeFmt => _studyDateTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "?";
            private string ReportDateTimeFmt => ReportDateTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "(no report dt)";
            internal DateTime? _studyDateTime; // for formatting (assigned during creation)
            public override string ToString() => Display;
        }

        private PreviousStudyTab? _selectedPreviousStudy; public PreviousStudyTab? SelectedPreviousStudy
        {
            get => _selectedPreviousStudy;
            set
            {
                if (SetProperty(ref _selectedPreviousStudy, value))
                {
                    foreach (var t in PreviousStudies) t.IsSelected = (value != null && t.Id == value.Id);
                    ApplyPreviousReportifiedState();
                }
            }
        }

        private bool _previousReportified; public bool PreviousReportified
        {
            get => _previousReportified;
            set { if (SetProperty(ref _previousReportified, value)) ApplyPreviousReportifiedState(); }
        }
        private void ApplyPreviousReportifiedState()
        {
            var tab = SelectedPreviousStudy; if (tab == null) return;
            Debug.WriteLine($"[PrevToggle] Apply state PrevReportified={PreviousReportified} lenOrigF={tab.OriginalFindings?.Length}");
            if (PreviousReportified)
            {
                tab.Header = tab.OriginalHeader;
                tab.Findings = tab.OriginalFindings;
                tab.Conclusion = tab.OriginalConclusion;
                tab.ReportifiedApplied = true;
            }
            else
            {
                tab.Header = DereportifyPreserveLines(tab.OriginalHeader);
                tab.Findings = DereportifyPreserveLines(tab.OriginalFindings);
                tab.Conclusion = DereportifyPreserveLines(tab.OriginalConclusion);
                tab.ReportifiedApplied = false;
            }
            OnPropertyChanged(nameof(SelectedPreviousStudy));
        }

        private static readonly Regex _rxLineSep = new("(\r\n|\n|\r)", RegexOptions.Compiled);
        private string DereportifyPreserveLines(string? input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            var parts = _rxLineSep.Split(input); // keep separators
            for (int i = 0; i < parts.Length; i++)
            {
                if (i % 2 == 1) continue; // separator
                parts[i] = DereportifySingleLine(parts[i]);
            }
            return string.Concat(parts);
        }
        private string DereportifySingleLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return line.TrimEnd();
            string original = line;
            line = Regex.Replace(line, @"^\s*\d+\.\s+", string.Empty); // numbering
            line = Regex.Replace(line, @"^ {1,4}", string.Empty); // indent
            line = Regex.Replace(line, @"^\s*--?>\s*", "-->");
            // Remove trailing single period if sentence-like
            if (line.EndsWith('.') && !line.EndsWith("..")) line = line[..^1];
            // Decap only if dictionary loaded
            if (_keepCapsFirstTokens != null) line = DecapUnlessDictionary(line);
            return line.TrimEnd();
        }

        private async void OnAddStudy()
        {
            if (_studyRepo == null) return;
            try
            {
                var relatedPatientRaw = await _pacs.GetSelectedIdFromRelatedStudiesAsync();
                string Normalize(string? s) => string.IsNullOrWhiteSpace(s) ? string.Empty : Regex.Replace(s, "[^A-Za-z0-9]", "").ToUpperInvariant();
                var currentNorm = Normalize(PatientNumber);
                var relatedNorm = Normalize(relatedPatientRaw);
                if (string.IsNullOrWhiteSpace(currentNorm) || string.IsNullOrWhiteSpace(relatedNorm) || currentNorm != relatedNorm)
                {
                    Debug.WriteLine($"[AddStudy] Patient mismatch current='{currentNorm}' related='{relatedNorm}' raw='{relatedPatientRaw}' - abort");
                    SetStatus($"Patient mismatch ? cannot add (current {currentNorm} vs related {relatedNorm})", true);
                    return;
                }
                var studyName = await _pacs.GetSelectedStudynameFromRelatedStudiesAsync();
                var dtStr = await _pacs.GetSelectedStudyDateTimeFromRelatedStudiesAsync();
                var radiologist = await _pacs.GetSelectedRadiologistFromRelatedStudiesAsync();
                var reportDateStr = await _pacs.GetSelectedReportDateTimeFromRelatedStudiesAsync();
                if (!DateTime.TryParse(dtStr, out var studyDt)) { SetStatus("Related study datetime invalid", true); return; }
                DateTime? reportDt = DateTime.TryParse(reportDateStr, out var rdt) ? rdt : null;
                var studyId = await _studyRepo.EnsureStudyAsync(PatientNumber, PatientName, PatientSex, null, studyName, studyDt);
                if (studyId == null) { SetStatus("Study save failed", true); return; }
                var f1Task = _pacs.GetCurrentFindingsAsync();
                var f2Task = _pacs.GetCurrentFindings2Async();
                var c1Task = _pacs.GetCurrentConclusionAsync();
                var c2Task = _pacs.GetCurrentConclusion2Async();
                await Task.WhenAll(f1Task, f2Task, c1Task, c2Task);
                string findings = PickLonger(f1Task.Result, f2Task.Result);
                string conclusion = PickLonger(c1Task.Result, c2Task.Result);
                var reportObj = new
                {
                    technique = string.Empty,
                    chief_complaint = string.Empty,
                    history_preview = string.Empty,
                    chief_complaint_proofread = string.Empty,
                    history = string.Empty,
                    history_proofread = string.Empty,
                    header_and_findings = findings,
                    conclusion = conclusion,
                    split_index = 0,
                    comparison = string.Empty,
                    technique_proofread = string.Empty,
                    comparison_proofread = string.Empty,
                    findings_proofread = string.Empty,
                    conclusion_proofread = string.Empty,
                    findings = findings,
                    conclusion_preview = string.Empty
                };
                string json = JsonSerializer.Serialize(reportObj);
                await _studyRepo.UpsertPartialReportAsync(studyId.Value, radiologist, reportDt, json, isMine: false, isCreated: false);
                await LoadPreviousStudiesForPatientAsync(PatientNumber);
                string modality = ExtractModality(studyName);
                var newTab = PreviousStudies.FirstOrDefault(t => t.StudyDateTime == studyDt && string.Equals(t.Modality, modality, StringComparison.OrdinalIgnoreCase));
                if (newTab != null) SelectedPreviousStudy = newTab;
                PreviousReportified = true;
                SetStatus("Previous study added", false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[AddStudy] error: " + ex.Message); SetStatus("Add study failed", true);
            }
        }
        private void OnSendReportPreview() { }
        private void OnSendReport() { PatientLocked = false; }
        #endregion

        #region Reportify Current Study
        private string ReportifyBlock(string input, bool isConclusion)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            input = input.Replace("\r\n", "\n");
            var paragraphs = input.Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
            for (int p = 0; p < paragraphs.Length; p++)
            {
                var lines = paragraphs[p].Split('\n');
                for (int i = 0; i < lines.Length; i++) lines[i] = ReportifySentence(lines[i]);
                paragraphs[p] = string.Join(Environment.NewLine, lines);
            }
            if (isConclusion && paragraphs.Length > 1)
            {
                for (int i = 0; i < paragraphs.Length; i++)
                {
                    var lines = paragraphs[i].Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                    if (lines.Length > 0)
                    {
                        lines[0] = $"{i + 1}. {ReportifyLineExt(lines[0])}";
                        for (int j = 1; j < lines.Length; j++) lines[j] = "   " + lines[j];
                        paragraphs[i] = string.Join(Environment.NewLine, lines);
                    }
                }
            }
            return string.Join(Environment.NewLine + Environment.NewLine, paragraphs);
        }
        private string DereportifyBlock(string input, bool isConclusion)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            input = input.Replace("\r\n", "\n");
            var paragraphs = input.Split(new[] { "\n\n" }, StringSplitOptions.None);
            for (int p = 0; p < paragraphs.Length; p++)
            {
                var lines = paragraphs[p].Split(new[] { '\n' }, StringSplitOptions.None).ToList();
                for (int i = 0; i < lines.Count; i++)
                {
                    var line = lines[i];
                    if (i == 0) line = Regex.Replace(line, @"^\s*\d+\.\s+", string.Empty);
                    line = Regex.Replace(line, @"^ {1,4}", string.Empty);
                    if (line.EndsWith('.') && !line.EndsWith("..")) line = line[..^1];
                    line = Regex.Replace(line, @"^\s*-->\s*", "--> ");
                    line = DecapUnlessDictionary(line);
                    lines[i] = line;
                }
                paragraphs[p] = string.Join(Environment.NewLine, lines);
            }
            return string.Join(Environment.NewLine + Environment.NewLine, paragraphs);
        }
        private string ReportifySentence(string sentence)
        {
            if (string.IsNullOrWhiteSpace(sentence)) return string.Empty;
            string res = PurifySentence(sentence, true);
            res = GetBody(res, true);
            if (res.Length == 0) return string.Empty;
            res = char.ToUpper(res[0]) + (res.Length > 1 ? res[1..] : string.Empty);
            char last = res[^1]; if (char.IsLetterOrDigit(last) || last == ')') res += "."; return res;
        }
        private string ReportifyLineExt(string str)
        { if (string.IsNullOrEmpty(str)) return str; string r = char.ToUpper(str[0]) + (str.Length > 1 ? str[1..] : string.Empty); if (!r.EndsWith('.') && char.IsLetterOrDigit(r[^1])) r += '.'; return r; }
        private string DecapUnlessDictionary(string line)
        {
            if (string.IsNullOrEmpty(line)) return line; int idx = 0; while (idx < line.Length && char.IsWhiteSpace(line[idx])) idx++; if (idx >= line.Length) return line; char c = line[idx]; if (!char.IsUpper(c)) return line; int end = idx; while (end < line.Length && char.IsLetter(line[end])) end++; var token = line.Substring(idx, end - idx); if (_keepCapsFirstTokens != null && _keepCapsFirstTokens.Contains(token)) return line; var lowered = char.ToLower(c) + line[(idx + 1)..]; return idx == 0 ? lowered : line[..idx] + lowered;
        }
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
            => Regex.Replace(input, onlyNumber ? @"^(\d+\. |\d+\.)" : @"^(--> |-->|->|- |-|\*|\d+\. |\d+\. |\d+\))", string.Empty).TrimEnd();
        #endregion

        #region Regex
        private static readonly Regex _rxArrow = new(@"^(--?>)", RegexOptions.Compiled);
        private static readonly Regex _rxBullet = new(@"^-(?!\->)|\*?(?<!-)", RegexOptions.Compiled);
        private static readonly Regex _rxAfterPunct = new(@"([;,:](?<!\d:))(?!\s)", RegexOptions.Compiled);
        private static readonly Regex _rxParensSpace = new(@"(?<=\S)\((?=\S)(?!\s*s\s*\))|(?<=\S)\)(?=[^\.,\s])(?!:)", RegexOptions.Compiled);
        private static readonly Regex _rxNumberUnit = new(@"(\d+(\.\d+)?)(cm|mm|ml)", RegexOptions.Compiled);
        private static readonly Regex _rxDot = new(@"(?<=\D)\.(?=[^\.\)]|$)", RegexOptions.Compiled);
        private static readonly Regex _rxSpaces = new(@"\s+", RegexOptions.Compiled);
        private static readonly Regex _rxLParen = new(@"\(\s+", RegexOptions.Compiled);
        private static readonly Regex _rxRParen = new(@"\s+\)", RegexOptions.Compiled);
        private static readonly Regex _rxModality = new(@"\b(CT|MRI|MR|XR|CR|DX|US|PET[- ]?CT|PETCT|PET|MAMMO|MMG|DXA|NM)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private string ExtractModality(string? studyName)
        {
            if (string.IsNullOrWhiteSpace(studyName)) return "UNK";
            var m = _rxModality.Match(studyName);
            if (!m.Success) return "UNK";
            var v = m.Value.ToUpperInvariant();
            return v switch { "MRI" => "MR", "PET-CT" => "PETCT", "PET CT" => "PETCT", "MMG" => "MAMMO", _ => v };
        }
        #endregion

        #region Helpers / Persistence
        public (string header, string findings, string conclusion) GetDereportifiedSections()
        {
            string h = Reportified ? DereportifyBlock(HeaderText, false) : HeaderText;
            string f = Reportified ? DereportifyBlock(FindingsText, false) : FindingsText;
            string c = Reportified ? DereportifyBlock(ConclusionText, true) : ConclusionText;
            return (h, f, c);
        }
        private async Task LoadPreviousStudiesForPatientAsync(string patientId)
        {
            if (_studyRepo == null) return;
            try
            {
                var rows = await _studyRepo.GetReportsForPatientAsync(patientId);
                PreviousStudies.Clear();
                // Group rows by StudyId/StudyDateTime (a study can have multiple reports)
                var groups = rows.GroupBy(r => new { r.StudyId, r.StudyDateTime, r.Studyname });
                foreach (var g in groups.OrderByDescending(g => g.Key.StudyDateTime))
                {
                    string modality = ExtractModality(StudyName);
                    if (PreviousStudies.Any(t => t.StudyDateTime == g.Key.StudyDateTime && string.Equals(t.Modality, modality, StringComparison.OrdinalIgnoreCase)))
                        continue; // keep uniqueness rule
                    var tab = new PreviousStudyTab
                    {
                        Id = Guid.NewGuid(),
                        StudyDateTime = g.Key.StudyDateTime,
                        Modality = modality,
                        Title = $"{g.Key.StudyDateTime:yyyy-MM-dd} {modality}",
                        OriginalHeader = string.Empty // not currently used for previous header portion
                    };
                    foreach (var row in g.OrderByDescending(r => r.ReportDateTime))
                    {
                        string findings = string.Empty; string conclusion = string.Empty; string headerFind = string.Empty;
                        try
                        {
                            using var doc = JsonDocument.Parse(row.ReportJson);
                            var root = doc.RootElement;
                            if (root.TryGetProperty("header_and_findings", out var hf)) headerFind = hf.GetString() ?? string.Empty;
                            if (root.TryGetProperty("findings", out var ff)) findings = ff.GetString() ?? headerFind; else findings = headerFind;
                            if (root.TryGetProperty("conclusion", out var cc)) conclusion = cc.GetString() ?? string.Empty;
                        }
                        catch (Exception ex) { Debug.WriteLine("[PrevLoad] JSON parse error: " + ex.Message); }
                        var choice = new PreviousReportChoice
                        {
                            ReportDateTime = row.ReportDateTime,
                            CreatedBy = row.CreatedBy ?? string.Empty,
                            Studyname = row.Studyname,
                            Findings = string.IsNullOrWhiteSpace(findings) ? headerFind : findings,
                            Conclusion = conclusion,
                            _studyDateTime = row.StudyDateTime
                        };
                        tab.Reports.Add(choice);
                    }
                    // select most recent report
                    tab.SelectedReport = tab.Reports.FirstOrDefault();
                    if (tab.SelectedReport != null)
                    {
                        tab.OriginalFindings = tab.SelectedReport.Findings;
                        tab.OriginalConclusion = tab.SelectedReport.Conclusion;
                        tab.Findings = tab.SelectedReport.Findings;
                        tab.Conclusion = tab.SelectedReport.Conclusion;
                    }
                    PreviousStudies.Add(tab);
                }
                if (PreviousStudies.Count > 0)
                {
                    SelectedPreviousStudy = PreviousStudies.First();
                    PreviousReportified = true; // default ON after load
                }
            }
            catch (Exception ex) { Debug.WriteLine("[PrevLoad] error: " + ex.Message); }
        }
        private async Task PersistCurrentStudyAsync(string? birthDate = null)
        {
            try
            {
                if (_studyRepo == null) return;
                if (!DateTime.TryParse(StudyDateTime, out var dt)) dt = DateTime.MinValue;
                await _studyRepo.EnsurePatientStudyAsync(PatientNumber, PatientName, PatientSex, birthDate, StudyName, dt == DateTime.MinValue ? null : dt);
            }
            catch { }
        }
        private static string PickLonger(string? a, string? b) => (b?.Length ?? 0) > (a?.Length ?? 0) ? (b ?? string.Empty) : (a ?? string.Empty);
        #endregion

        #region Inner DelegateCommand
        private sealed class DelegateCommand : ICommand
        {
            private readonly Action<object?> _exec; private readonly Predicate<object?>? _can;
            public DelegateCommand(Action<object?> exec, Predicate<object?>? can = null) { _exec = exec; _can = can; }
            public bool CanExecute(object? parameter) => _can?.Invoke(parameter) ?? true;
            public void Execute(object? parameter) => _exec(parameter);
            public event EventHandler? CanExecuteChanged; public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
        #endregion

        private string DereportifyBlockLineByLine(string input, bool isConclusion)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            input = input.Replace("\r\n", "\n");
            var lines = input.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                line = Regex.Replace(line, @"^\s*\d+\.\s+", string.Empty); // numbering
                line = Regex.Replace(line, @"^ {1,4}", string.Empty); // indent
                if (line.EndsWith('.') && !line.EndsWith("..")) line = line[..^1];
                line = Regex.Replace(line, @"^\s*--?>\s*", "-->");
                line = DecapUnlessDictionary(line);
                lines[i] = line;
            }
            return string.Join("\n", lines);
        }

        // Adjust OnNewStudy automation check
        private void OnNewStudy()
        {
            var seqRaw = _localSettings?.AutomationNewStudySequence ?? string.Empty;
            var modules = seqRaw.Split(new[] {',',';'}, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (modules.Length == 0) return;
            foreach (var m in modules)
            {
                if (string.Equals(m, "NewStudy", StringComparison.OrdinalIgnoreCase)) { _ = RunNewStudyProcedureAsync(); }
                else if (string.Equals(m, "LockStudy", StringComparison.OrdinalIgnoreCase) && _lockStudyProc != null) { _ = _lockStudyProc.ExecuteAsync(this); }
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

        internal async Task FetchCurrentStudyAsyncInternal() => await FetchCurrentStudyAsync();
        internal void UpdateCurrentStudyLabelInternal() => UpdateCurrentStudyLabel();
        internal void SetStatusInternal(string msg, bool err=false) => SetStatus(msg, err);

        private async Task RunNewStudyProcedureAsync()
        {
            if (_newStudyProc != null)
                await _newStudyProc.ExecuteAsync(this);
            else
                OnNewStudy();
        }

        private void AutoUnreportifyOnEdit()
        {
            if (_suppressAutoToggle) return;
            if (_reportified)
            {
                _suppressAutoToggle = true;
                Reportified = false;
                _suppressAutoToggle = false;
            }
        }
    }
}