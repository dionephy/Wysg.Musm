using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics; // added for debug logging
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows.Input; // Added for ICommand
using Wysg.Musm.Infrastructure.ViewModels; // Added for BaseViewModel
using System.Windows.Controls;

namespace Wysg.Musm.Radium.ViewModels
{
    /// <summary>
    /// Partial: Previous studies tab management + JSON view for selected previous report.
    /// Added debug logging to trace why txtPrevJson might not update.
    /// </summary>
    public partial class MainViewModel
    {
        // ---------------- Model Classes ----------------
        public sealed class PreviousStudyTab : BaseViewModel
        {
            public Guid Id { get; set; }
            public string Title { get; set; } = string.Empty;
            public DateTime StudyDateTime { get; set; }
            public string Modality { get; set; } = string.Empty;
            private string _header = string.Empty; public string Header { get => _header; set { if (SetProperty(ref _header, value)) Debug.WriteLine($"[PrevTab] Header changed -> {value}"); } }
            private string _findings = string.Empty; public string Findings { get => _findings; set { if (SetProperty(ref _findings, value)) Debug.WriteLine($"[PrevTab] Findings changed -> len={value?.Length}"); } }
            private string _conclusion = string.Empty; public string Conclusion { get => _conclusion; set { if (SetProperty(ref _conclusion, value)) Debug.WriteLine($"[PrevTab] Conclusion changed -> len={value?.Length}"); } }
            public string RawJson { get; set; } = string.Empty;
            public string OriginalFindings { get; set; } = string.Empty;
            public string OriginalConclusion { get; set; } = string.Empty;
            private bool _isSelected; public bool IsSelected { get => _isSelected; set => SetProperty(ref _isSelected, value); }
            public ObservableCollection<PreviousReportChoice> Reports { get; } = new();
            private PreviousReportChoice? _selectedReport; public PreviousReportChoice? SelectedReport { get => _selectedReport; set { if (SetProperty(ref _selectedReport, value)) { Debug.WriteLine($"[PrevTab] Report selection changed"); ApplyReportSelection(value); } } }
            public void ApplyReportSelection(PreviousReportChoice? rep)
            {
                if (rep == null) return;
                OriginalFindings = rep.Findings; OriginalConclusion = rep.Conclusion;
                Findings = rep.Findings; Conclusion = rep.Conclusion;
            }
            public override string ToString() => Title;

            // New: splitter fields for user-defined ranges
            private int? _hfHeaderFrom; public int? HfHeaderFrom { get => _hfHeaderFrom; set => SetProperty(ref _hfHeaderFrom, value); }
            private int? _hfHeaderTo; public int? HfHeaderTo { get => _hfHeaderTo; set => SetProperty(ref _hfHeaderTo, value); }
            private int? _hfConclusionFrom; public int? HfConclusionFrom { get => _hfConclusionFrom; set => SetProperty(ref _hfConclusionFrom, value); }
            private int? _hfConclusionTo; public int? HfConclusionTo { get => _hfConclusionTo; set => SetProperty(ref _hfConclusionTo, value); }

            private int? _fcHeaderFrom; public int? FcHeaderFrom { get => _fcHeaderFrom; set => SetProperty(ref _fcHeaderFrom, value); }
            private int? _fcHeaderTo; public int? FcHeaderTo { get => _fcHeaderTo; set => SetProperty(ref _fcHeaderTo, value); }
            private int? _fcFindingsFrom; public int? FcFindingsFrom { get => _fcFindingsFrom; set => SetProperty(ref _fcFindingsFrom, value); }
            private int? _fcFindingsTo; public int? FcFindingsTo { get => _fcFindingsTo; set => SetProperty(ref _fcFindingsTo, value); }

            // Additional metadata fields for previous report
            private string _chiefComplaint = string.Empty; public string ChiefComplaint { get => _chiefComplaint; set => SetProperty(ref _chiefComplaint, value ?? string.Empty); }
            private string _patientHistory = string.Empty; public string PatientHistory { get => _patientHistory; set => SetProperty(ref _patientHistory, value ?? string.Empty); }
            private string _studyTechniques = string.Empty; public string StudyTechniques { get => _studyTechniques; set => SetProperty(ref _studyTechniques, value ?? string.Empty); }
            private string _comparison = string.Empty; public string Comparison { get => _comparison; set => SetProperty(ref _comparison, value ?? string.Empty); }
            private string _studyRemark = string.Empty; public string StudyRemark { get => _studyRemark; set => SetProperty(ref _studyRemark, value ?? string.Empty); }
            private string _patientRemark = string.Empty; public string PatientRemark { get => _patientRemark; set => SetProperty(ref _patientRemark, value ?? string.Empty); }

            // Proofread fields (root JSON)
            private string _chiefComplaintProofread = string.Empty; public string ChiefComplaintProofread { get => _chiefComplaintProofread; set => SetProperty(ref _chiefComplaintProofread, value ?? string.Empty); }
            private string _patientHistoryProofread = string.Empty; public string PatientHistoryProofread { get => _patientHistoryProofread; set => SetProperty(ref _patientHistoryProofread, value ?? string.Empty); }
            private string _studyTechniquesProofread = string.Empty; public string StudyTechniquesProofread { get => _studyTechniquesProofread; set => SetProperty(ref _studyTechniquesProofread, value ?? string.Empty); }
            private string _comparisonProofread = string.Empty; public string ComparisonProofread { get => _comparisonProofread; set => SetProperty(ref _comparisonProofread, value ?? string.Empty); }
            private string _findingsProofread = string.Empty; public string FindingsProofread { get => _findingsProofread; set => SetProperty(ref _findingsProofread, value ?? string.Empty); }
            private string _conclusionProofread = string.Empty; public string ConclusionProofread { get => _conclusionProofread; set => SetProperty(ref _conclusionProofread, value ?? string.Empty); }

            // Split outputs (root json: header_temp, findings, conclusion)
            private string _headerTemp = string.Empty; public string HeaderTemp { get => _headerTemp; set => SetProperty(ref _headerTemp, value ?? string.Empty); }
            private string _findingsOut = string.Empty; public string FindingsOut { get => _findingsOut; set => SetProperty(ref _findingsOut, value ?? string.Empty); }
            private string _conclusionOut = string.Empty; public string ConclusionOut { get => _conclusionOut; set => SetProperty(ref _conclusionOut, value ?? string.Empty); }
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
            internal DateTime? _studyDateTime;
            public override string ToString() => Display;
        }

        public ObservableCollection<PreviousStudyTab> PreviousStudies { get; } = new();

        // ---------------- Selection + Reportified Toggle ----------------
        private PreviousStudyTab? _selectedPreviousStudy; public PreviousStudyTab? SelectedPreviousStudy
        { 
            get => _selectedPreviousStudy; 
            set 
            { 
                var old = _selectedPreviousStudy; 
                if (SetProperty(ref _selectedPreviousStudy, value)) 
                { 
                    Debug.WriteLine($"[Prev] SelectedPreviousStudy set -> {(value==null?"<null>":value.Title)}"); 
                    foreach (var t in PreviousStudies) t.IsSelected = (value != null && t.Id == value.Id); 
                    HookPreviousStudy(old, value); 
                    EnsureSplitDefaultsIfNeeded(); 
                    
                    // Notify all wrapper properties that depend on SelectedPreviousStudy
                    OnPropertyChanged(nameof(PreviousHeaderText)); 
                    OnPropertyChanged(nameof(PreviousHeaderAndFindingsText)); 
                    OnPropertyChanged(nameof(PreviousFinalConclusionText)); 
                    
                    // CRITICAL FIX: Notify split-mode properties so editors update when switching tabs in splitted mode
                    OnPropertyChanged(nameof(PreviousHeaderTemp));
                    OnPropertyChanged(nameof(PreviousSplitFindings));
                    OnPropertyChanged(nameof(PreviousSplitConclusion));
                    OnPropertyChanged(nameof(PreviousHeaderSplitView));
                    OnPropertyChanged(nameof(PreviousFindingsSplitView));
                    OnPropertyChanged(nameof(PreviousConclusionSplitView));
                    
                    // NEW: Notify computed editor properties for proofread support
                    OnPropertyChanged(nameof(PreviousFindingsEditorText));
                    OnPropertyChanged(nameof(PreviousConclusionEditorText));
                    
                    UpdatePreviousReportJson(); 
                } 
            } 
        }
        private bool _previousReportSplitted; public bool PreviousReportSplitted
        {
            get => _previousReportSplitted;
            set
            {
                if (SetProperty(ref _previousReportSplitted, value))
                {
                    Debug.WriteLine($"[Prev] PreviousReportSplitted -> {value}");
                    if (value) EnsureSplitDefaultsIfNeeded();
                    OnPropertyChanged(nameof(PreviousHeaderSplitView));
                    OnPropertyChanged(nameof(PreviousFindingsSplitView));
                    OnPropertyChanged(nameof(PreviousConclusionSplitView));
                    
                    // NEW: Notify editor properties when split mode changes (affects fallback)
                    OnPropertyChanged(nameof(PreviousFindingsEditorText));
                    OnPropertyChanged(nameof(PreviousConclusionEditorText));
                }
            }
        }

        private void EnsureSplitDefaultsIfNeeded()
        {
            var tab = SelectedPreviousStudy; if (tab == null) return;
            var hf = tab.Findings ?? string.Empty;
            var fc = tab.Conclusion ?? string.Empty;
            if (tab.HfHeaderFrom == null && tab.HfHeaderTo == null) { tab.HfHeaderFrom = 0; tab.HfHeaderTo = 0; }
            if (tab.HfConclusionFrom == null && tab.HfConclusionTo == null) { tab.HfConclusionFrom = hf.Length; tab.HfConclusionTo = hf.Length; }
            if (tab.FcHeaderFrom == null && tab.FcHeaderTo == null) { tab.FcHeaderFrom = 0; tab.FcHeaderTo = 0; }
            if (tab.FcFindingsFrom == null && tab.FcFindingsTo == null) { tab.FcFindingsFrom = 0; tab.FcFindingsTo = 0; }
        }
        
        // Placeholder properties for split functionality
        private bool _autoSplitHeader; public bool AutoSplitHeader { get => _autoSplitHeader; set => SetProperty(ref _autoSplitHeader, value); }
        private bool _autoSplitConclusion; public bool AutoSplitConclusion { get => _autoSplitConclusion; set => SetProperty(ref _autoSplitConclusion, value); }
        private bool _autoSplit; public bool AutoSplit { get => _autoSplit; set => SetProperty(ref _autoSplit, value); }
        
        // NEW: Computed properties for previous report Findings and Conclusion editors with proofread support
        // These follow the fallback chain: proofread ¡æ splitted (if on) ¡æ original
        public string PreviousFindingsEditorText
        {
            get
            {
                var tab = SelectedPreviousStudy;
                if (tab == null) return _prevHeaderAndFindingsCache ?? string.Empty;
                
                // Proofread mode: use proofread version if available
                if (PreviousProofreadMode && !string.IsNullOrWhiteSpace(tab.FindingsProofread))
                {
                    return tab.FindingsProofread;
                }
                
                // Fallback: splitted mode uses split version, otherwise original
                if (PreviousReportSplitted)
                {
                    return tab.FindingsOut ?? string.Empty;
                }
                else
                {
                    return tab.Findings ?? string.Empty;
                }
            }
        }
        
        public string PreviousConclusionEditorText
        {
            get
            {
                var tab = SelectedPreviousStudy;
                if (tab == null) return _prevFinalConclusionCache ?? string.Empty;
                
                // Proofread mode: use proofread version if available
                if (PreviousProofreadMode && !string.IsNullOrWhiteSpace(tab.ConclusionProofread))
                {
                    return tab.ConclusionProofread;
                }
                
                // Fallback: splitted mode uses split version, otherwise original
                if (PreviousReportSplitted)
                {
                    return tab.ConclusionOut ?? string.Empty;
                }
                else
                {
                    return tab.Conclusion ?? string.Empty;
                }
            }
        }
        
        // NEW: Computed properties for previous report that switch between raw and proofread versions based on PreviousProofreadMode
        // These properties will be bound to the previous report editor DocumentText
        public string PreviousChiefComplaintDisplay 
        {
            get
            {
                var tab = SelectedPreviousStudy;
                if (tab == null) return string.Empty;
                var text = PreviousProofreadMode && !string.IsNullOrWhiteSpace(tab.ChiefComplaintProofread) 
                    ? tab.ChiefComplaintProofread 
                    : tab.ChiefComplaint;
                return PreviousProofreadMode ? ApplyProofreadPlaceholders(text) : text;
            }
        }

        public string PreviousPatientHistoryDisplay 
        {
            get
            {
                var tab = SelectedPreviousStudy;
                if (tab == null) return string.Empty;
                var text = PreviousProofreadMode && !string.IsNullOrWhiteSpace(tab.PatientHistoryProofread) 
                    ? tab.PatientHistoryProofread 
                    : tab.PatientHistory;
                return PreviousProofreadMode ? ApplyProofreadPlaceholders(text) : text;
            }
        }

        public string PreviousStudyTechniquesDisplay 
        {
            get
            {
                var tab = SelectedPreviousStudy;
                if (tab == null) return string.Empty;
                var text = PreviousProofreadMode && !string.IsNullOrWhiteSpace(tab.StudyTechniquesProofread) 
                    ? tab.StudyTechniquesProofread 
                    : tab.StudyTechniques;
                return PreviousProofreadMode ? ApplyProofreadPlaceholders(text) : text;
            }
        }

        public string PreviousComparisonDisplay 
        {
            get
            {
                var tab = SelectedPreviousStudy;
                if (tab == null) return string.Empty;
                var text = PreviousProofreadMode && !string.IsNullOrWhiteSpace(tab.ComparisonProofread) 
                    ? tab.ComparisonProofread 
                    : tab.Comparison;
                return PreviousProofreadMode ? ApplyProofreadPlaceholders(text) : text;
            }
        }

        public string PreviousFindingsDisplay 
        {
            get
            {
                var tab = SelectedPreviousStudy;
                if (tab == null) return string.Empty;
                var text = PreviousProofreadMode && !string.IsNullOrWhiteSpace(tab.FindingsProofread) 
                    ? tab.FindingsProofread 
                    : tab.FindingsOut;
                return PreviousProofreadMode ? ApplyProofreadPlaceholders(text) : text;
            }
        }

        public string PreviousConclusionDisplay 
        {
            get
            {
                var tab = SelectedPreviousStudy;
                if (tab == null) return string.Empty;
                var text = PreviousProofreadMode && !string.IsNullOrWhiteSpace(tab.ConclusionProofread) 
                    ? tab.ConclusionProofread 
                    : tab.ConclusionOut;
                return PreviousProofreadMode ? ApplyProofreadPlaceholders(text) : text;
            }
        }
        
        // Placeholder commands for split functionality
        public ICommand? SplitHeaderCommand { get; set; }
        public ICommand? SplitConclusionCommand { get; set; }
        public ICommand? SplitHeaderTopCommand { get; set; }
        public ICommand? SplitHeaderBottomCommand { get; set; }
        public ICommand? SplitFindingsCommand { get; set; }

        private void InitializePreviousSplitCommands()
        {
            SplitHeaderTopCommand = new SimpleCommand(p => OnSplitHeaderTop(p));
            SplitConclusionCommand = new SimpleCommand(p => OnSplitConclusionTop(p));
            SplitHeaderBottomCommand = new SimpleCommand(p => OnSplitHeaderBottom(p));
            SplitFindingsCommand = new SimpleCommand(p => OnSplitFindingsBottom(p));
        }

        private static (int from, int to) GetOffsetsFromTextBox(object? param)
        {
            if (param is TextBox tb)
            {
                if (tb.SelectionLength > 0)
                {
                    int from = tb.SelectionStart;
                    int to = tb.SelectionStart + tb.SelectionLength;
                    return (from, to);
                }
                else
                {
                    int pos = tb.CaretIndex;
                    return (pos, pos);
                }
            }
            return (0, 0);
        }

        private void OnSplitHeaderTop(object? param)
        {
            var tab = SelectedPreviousStudy; if (tab == null) { SetStatus("Select a previous report first", true); return; }
            var (from, to) = GetOffsetsFromTextBox(param);
            tab.HfHeaderFrom = from;
            tab.HfHeaderTo = to;
            // Adjust conclusion start/end to not precede header end
            var hf = tab.Findings ?? string.Empty;
            int headerTo = Clamp(tab.HfHeaderTo ?? 0, 0, hf.Length);
            if ((tab.HfConclusionFrom ?? 0) < headerTo) tab.HfConclusionFrom = headerTo;
            if ((tab.HfConclusionTo ?? 0) < headerTo) tab.HfConclusionTo = headerTo;
            UpdatePreviousReportJson();
        }

        private void OnSplitConclusionTop(object? param)
        {
            var tab = SelectedPreviousStudy; if (tab == null) { SetStatus("Select a previous report first", true); return; }
            var (from, to) = GetOffsetsFromTextBox(param);
            tab.HfConclusionFrom = from;
            tab.HfConclusionTo = to;
            // Adjust header range to not exceed conclusion start
            var hf = tab.Findings ?? string.Empty;
            int conclFrom = Clamp(tab.HfConclusionFrom ?? 0, 0, hf.Length);
            if ((tab.HfHeaderFrom ?? 0) > conclFrom) tab.HfHeaderFrom = conclFrom;
            if ((tab.HfHeaderTo ?? 0) > conclFrom) tab.HfHeaderTo = conclFrom;
            UpdatePreviousReportJson();
        }

        private void OnSplitHeaderBottom(object? param)
        {
            var tab = SelectedPreviousStudy; if (tab == null) { SetStatus("Select a previous report first", true); return; }
            var (from, to) = GetOffsetsFromTextBox(param);
            tab.FcHeaderFrom = from;
            tab.FcHeaderTo = to;
            // Adjust findings split to not precede header end
            var fc = tab.Conclusion ?? string.Empty;
            int headerTo = Clamp(tab.FcHeaderTo ?? 0, 0, fc.Length);
            if ((tab.FcFindingsFrom ?? 0) < headerTo) tab.FcFindingsFrom = headerTo;
            if ((tab.FcFindingsTo ?? 0) < headerTo) tab.FcFindingsTo = headerTo;
            UpdatePreviousReportJson();
        }

        private void OnSplitFindingsBottom(object? param)
        {
            var tab = SelectedPreviousStudy; if (tab == null) { SetStatus("Select a previous report first", true); return; }
            var (from, to) = GetOffsetsFromTextBox(param);
            tab.FcFindingsFrom = from;
            tab.FcFindingsTo = to;
            // Adjust header split to not exceed findings start
            var fc = tab.Conclusion ?? string.Empty;
            int findingsFrom = Clamp(tab.FcFindingsFrom ?? 0, 0, fc.Length);
            if ((tab.FcHeaderFrom ?? 0) > findingsFrom) tab.FcHeaderFrom = findingsFrom;
            if ((tab.FcHeaderTo ?? 0) > findingsFrom) tab.FcHeaderTo = findingsFrom;
            UpdatePreviousReportJson();
        }

        // ---------------- Previous Editors (wrappers reusing underlying tab values) ----------------
        private string _prevHeaderCache = string.Empty;
        private string _prevHeaderAndFindingsCache = string.Empty;
        private string _prevFinalConclusionCache = string.Empty;
        private string _prevStudyRemarkCache = string.Empty;
        private string _prevPatientRemarkCache = string.Empty;
        private string _prevChiefComplaintCache = string.Empty;
        private string _prevPatientHistoryCache = string.Empty;
        private string _prevStudyTechniquesCache = string.Empty;
        private string _prevComparisonCache = string.Empty;
        private string _prevHeaderTempCache = string.Empty;
        private string _prevFindingsOutCache = string.Empty;
        private string _prevConclusionOutCache = string.Empty;
        private string _prevChiefComplaintProofreadCache = string.Empty;
        private string _prevPatientHistoryProofreadCache = string.Empty;
        private string _prevStudyTechniquesProofreadCache = string.Empty;
        private string _prevComparisonProofreadCache = string.Empty;
        private string _prevFindingsProofreadCache = string.Empty;
        private string _prevConclusionProofreadCache = string.Empty;
        public string PreviousHeaderText
        {
            get => SelectedPreviousStudy?.Header ?? _prevHeaderCache;
            set
            {
                if (SelectedPreviousStudy == null)
                { if (value == _prevHeaderCache) return; _prevHeaderCache = value; OnPropertyChanged(); Debug.WriteLine($"[PrevEdit] Cache Header -> {value}"); }
                else if (SelectedPreviousStudy.Header != value) { SelectedPreviousStudy.Header = value; Debug.WriteLine($"[PrevEdit] Tab Header -> {value}"); }
                UpdatePreviousReportJson();
                OnPropertyChanged(nameof(PreviousReportJson));
            }
        }
        public string PreviousHeaderAndFindingsText
        {
            get => SelectedPreviousStudy?.Findings ?? _prevHeaderAndFindingsCache;
            set
            {
                if (SelectedPreviousStudy == null)
                { if (value == _prevHeaderAndFindingsCache) return; _prevHeaderAndFindingsCache = value; OnPropertyChanged(); Debug.WriteLine($"[PrevEdit] Cache HeaderAndFindings len={value?.Length}"); }
                else if (SelectedPreviousStudy.Findings != value) { SelectedPreviousStudy.Findings = value; Debug.WriteLine($"[PrevEdit] Tab HeaderAndFindings len={value?.Length}"); }
                UpdatePreviousReportJson();
                OnPropertyChanged(nameof(PreviousReportJson));
            }
        }
        public string PreviousFinalConclusionText
        {
            get => SelectedPreviousStudy?.Conclusion ?? _prevFinalConclusionCache;
            set
            {
                if (SelectedPreviousStudy == null)
                { if (value == _prevFinalConclusionCache) return; _prevFinalConclusionCache = value; OnPropertyChanged(); Debug.WriteLine($"[PrevEdit] Cache FinalConclusion len={value?.Length}"); }
                else if (SelectedPreviousStudy.Conclusion != value) { SelectedPreviousStudy.Conclusion = value; Debug.WriteLine($"[PrevEdit] Tab FinalConclusion len={value?.Length}"); }
                UpdatePreviousReportJson();
                OnPropertyChanged(nameof(PreviousReportJson));
                OnPropertyChanged(nameof(PreviousHeaderSplitView));
                OnPropertyChanged(nameof(PreviousFindingsSplitView));
                OnPropertyChanged(nameof(PreviousConclusionSplitView));
            }
        }
        
        // Aliases for backward compatibility (if needed elsewhere)
        public string PreviousFindingsText
        {
            get => PreviousHeaderAndFindingsText;
            set => PreviousHeaderAndFindingsText = value;
        }
        public string PreviousConclusionText
        {
            get => PreviousFinalConclusionText;
            set => PreviousFinalConclusionText = value;
        }

        // New root fields bound editors
        public string PreviousHeaderTemp
        {
            get => SelectedPreviousStudy?.HeaderTemp ?? _prevHeaderTempCache;
            set
            {
                if (SelectedPreviousStudy == null)
                { if (value == _prevHeaderTempCache) return; _prevHeaderTempCache = value; OnPropertyChanged(); }
                else if (SelectedPreviousStudy.HeaderTemp != value) { SelectedPreviousStudy.HeaderTemp = value; }
                UpdatePreviousReportJson();
                OnPropertyChanged(nameof(PreviousReportJson));
            }
        }
        public string PreviousSplitFindings
        {
            get => SelectedPreviousStudy?.FindingsOut ?? _prevFindingsOutCache;
            set
            {
                if (SelectedPreviousStudy == null)
                { if (value == _prevFindingsOutCache) return; _prevFindingsOutCache = value; OnPropertyChanged(); }
                else if (SelectedPreviousStudy.FindingsOut != value) { SelectedPreviousStudy.FindingsOut = value; }
                UpdatePreviousReportJson();
                OnPropertyChanged(nameof(PreviousReportJson));
            }
        }
        public string PreviousSplitConclusion
        {
            get => SelectedPreviousStudy?.ConclusionOut ?? _prevConclusionOutCache;
            set
            {
                if (SelectedPreviousStudy == null)
                { if (value == _prevConclusionOutCache) return; _prevConclusionOutCache = value; OnPropertyChanged(); }
                else if (SelectedPreviousStudy.ConclusionOut != value) { SelectedPreviousStudy.ConclusionOut = value; }
                UpdatePreviousReportJson();
                OnPropertyChanged(nameof(PreviousReportJson));
            }
        }

        // Computed display strings when PreviousReportSplitted is ON
        public string PreviousHeaderSplitView
        {
            get
            {
                if (!PreviousReportSplitted) return PreviousHeaderText;
                var tab = SelectedPreviousStudy;
                string hf = tab?.Findings ?? _prevHeaderAndFindingsCache ?? string.Empty;
                string fc = tab?.Conclusion ?? _prevFinalConclusionCache ?? string.Empty;
                int hfFrom = Clamp(tab?.HfHeaderFrom ?? 0, 0, hf.Length);
                int fcFrom = Clamp(tab?.FcHeaderFrom ?? 0, 0, fc.Length);
                var part1 = Sub(hf, 0, hfFrom).Trim();
                var part2 = Sub(fc, 0, fcFrom).Trim();
                return (part1 + Environment.NewLine + part2).Trim();
            }
        }
        public string PreviousFindingsSplitView
        {
            get
            {
                if (!PreviousReportSplitted) return PreviousHeaderAndFindingsText;
                var tab = SelectedPreviousStudy;
                string hf = tab?.Findings ?? _prevHeaderAndFindingsCache ?? string.Empty;
                string fc = tab?.Conclusion ?? _prevFinalConclusionCache ?? string.Empty;
                int hfTo = Clamp(tab?.HfHeaderTo ?? 0, 0, hf.Length);
                int hfFrom2 = Clamp(tab?.HfConclusionFrom ?? hf.Length, 0, hf.Length);
                int fcTo = Clamp(tab?.FcHeaderTo ?? 0, 0, fc.Length);
                int fcFrom2 = Clamp(tab?.FcFindingsFrom ?? 0, 0, fc.Length);
                var part1 = Sub(hf, hfTo, hfFrom2 - hfTo).Trim();
                var part2 = Sub(fc, fcTo, fcFrom2 - fcTo).Trim();
                return (part1 + Environment.NewLine + part2).Trim();
            }
        }
        public string PreviousConclusionSplitView
        {
            get
            {
                if (!PreviousReportSplitted) return PreviousFinalConclusionText;
                var tab = SelectedPreviousStudy;
                string hf = tab?.Findings ?? _prevHeaderAndFindingsCache ?? string.Empty;
                string fc = tab?.Conclusion ?? _prevFinalConclusionCache ?? string.Empty;
                int hfTo2 = Clamp(tab?.HfConclusionTo ?? hf.Length, 0, hf.Length);
                int fcTo2 = Clamp(tab?.FcFindingsTo ?? 0, 0, fc.Length);
                var part1 = Sub(hf, hfTo2, hf.Length - hfTo2).Trim();
                var part2 = Sub(fc, fcTo2, fc.Length - fcTo2).Trim();
                return (part1 + Environment.NewLine + part2).Trim();
            }
        }

        private static int Clamp(int value, int min, int max) => value < min ? min : (value > max ? max : value);
        private static string Sub(string s, int start, int length)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            if (start < 0) start = 0; if (start > s.Length) start = s.Length;
            if (length < 0) length = 0; if (start + length > s.Length) length = s.Length - start;
            return length <= 0 ? string.Empty : s.Substring(start, length);
        }

        // ---------------- JSON Sync for Previous Tabs ----------------
        private string _previousReportJson = "{}"; public string PreviousReportJson { get => _previousReportJson; set { var changed = SetProperty(ref _previousReportJson, value); Debug.WriteLine($"[PrevJson] Set PreviousReportJson changed={changed} len={value?.Length}"); if (!changed) OnPropertyChanged(nameof(PreviousReportJson)); if (_updatingPrevFromEditors) return; ApplyJsonToPrevious(value); } }
        private bool _updatingPrevFromEditors; private bool _updatingPrevFromJson;
        private void UpdatePreviousReportJson()
        {
            var tab = SelectedPreviousStudy;
            try
            {
                object obj;
                if (tab == null)
                {
                    // No selected tab yet: use cache fields so txtPrevJson mirrors user typing
                    obj = new
                    {
                        header_temp = _prevHeaderTempCache,
                        header_and_findings = _prevHeaderAndFindingsCache,
                        final_conclusion = _prevFinalConclusionCache,
                        findings = _prevFindingsOutCache,
                        conclusion = _prevConclusionOutCache,
                        study_remark = _prevStudyRemarkCache,
                        patient_remark = _prevPatientRemarkCache,
                        chief_complaint = _prevChiefComplaintCache,
                        patient_history = _prevPatientHistoryCache,
                        study_techniques = _prevStudyTechniquesCache,
                        comparison = _prevComparisonCache,
                        chief_complaint_proofread = _prevChiefComplaintProofreadCache,
                        patient_history_proofread = _prevPatientHistoryProofreadCache,
                        study_techniques_proofread = _prevStudyTechniquesProofreadCache,
                        comparison_proofread = _prevComparisonProofreadCache,
                        findings_proofread = _prevFindingsProofreadCache,
                        conclusion_proofread = _prevConclusionProofreadCache
                    };
                    Debug.WriteLine($"[PrevJson] Update (cache only) htLen={_prevHeaderTempCache?.Length} hfLen={_prevHeaderAndFindingsCache?.Length} fcLen={_prevFinalConclusionCache?.Length}");
                }
                else
                {
                    // Ensure defaults and compute split outputs
                    EnsureSplitDefaultsIfNeeded();
                    string hf = tab.Findings ?? string.Empty;
                    string fc = tab.Conclusion ?? string.Empty;
                    int hfFrom = Clamp(tab.HfHeaderFrom ?? 0, 0, hf.Length);
                    int hfTo = Clamp(tab.HfHeaderTo ?? 0, 0, hf.Length);
                    int hfCFrom = Clamp(tab.HfConclusionFrom ?? hf.Length, 0, hf.Length);
                    int hfCTo = Clamp(tab.HfConclusionTo ?? hf.Length, 0, hf.Length);
                    int fcFrom = Clamp(tab.FcHeaderFrom ?? 0, 0, fc.Length);
                    int fcTo = Clamp(tab.FcHeaderTo ?? 0, 0, fc.Length);
                    int fcFFrom = Clamp(tab.FcFindingsFrom ?? 0, 0, fc.Length);
                    int fcFTo = Clamp(tab.FcFindingsTo ?? 0, 0, fc.Length);
                    string splitHeader = (Sub(hf, 0, hfFrom).Trim() + Environment.NewLine + Sub(fc, 0, fcFrom).Trim()).Trim();
                    string splitFindings = (Sub(hf, hfTo, hfCFrom - hfTo).Trim() + Environment.NewLine + Sub(fc, fcTo, fcFFrom - fcTo).Trim()).Trim();
                    string splitConclusion = (Sub(hf, hfCTo, hf.Length - hfCTo).Trim() + Environment.NewLine + Sub(fc, fcFTo, fc.Length - fcFTo).Trim()).Trim();
                    if (tab.HeaderTemp != splitHeader) tab.HeaderTemp = splitHeader;
                    if (tab.FindingsOut != splitFindings) tab.FindingsOut = splitFindings;
                    if (tab.ConclusionOut != splitConclusion) tab.ConclusionOut = splitConclusion;

                    obj = new
                    {
                        header_temp = tab.HeaderTemp ?? string.Empty,
                        header_and_findings = tab.Findings ?? string.Empty,
                        final_conclusion = tab.Conclusion ?? string.Empty,
                        findings = tab.FindingsOut ?? string.Empty,
                        conclusion = tab.ConclusionOut ?? string.Empty,
                        study_remark = tab.StudyRemark,
                        patient_remark = tab.PatientRemark,
                        chief_complaint = tab.ChiefComplaint,
                        patient_history = tab.PatientHistory,
                        study_techniques = tab.StudyTechniques,
                        comparison = tab.Comparison,
                        chief_complaint_proofread = tab.ChiefComplaintProofread,
                        patient_history_proofread = tab.PatientHistoryProofread,
                        study_techniques_proofread = tab.StudyTechniquesProofread,
                        comparison_proofread = tab.ComparisonProofread,
                        findings_proofread = tab.FindingsProofread,
                        conclusion_proofread = tab.ConclusionProofread,
                        PrevReport = new
                        {
                            header_and_findings_header_splitter_from = tab.HfHeaderFrom,
                            header_and_findings_header_splitter_to = tab.HfHeaderTo,
                            header_and_findings_conclusion_splitter_from = tab.HfConclusionFrom,
                            header_and_findings_conclusion_splitter_to = tab.HfConclusionTo,
                            final_conclusion_header_splitter_from = tab.FcHeaderFrom,
                            final_conclusion_header_splitter_to = tab.FcHeaderTo,
                            final_conclusion_findings_splitter_from = tab.FcFindingsFrom,
                            final_conclusion_findings_splitter_to = tab.FcFindingsTo
                        }
                    };
                    Debug.WriteLine($"[PrevJson] Update (tab) htLen={tab.HeaderTemp?.Length} hfLen={tab.Findings?.Length} fcLen={tab.Conclusion?.Length}");
                }
                var json = JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true });
                _updatingPrevFromEditors = true;
                PreviousReportJson = json;
                OnPropertyChanged(nameof(PreviousReportJson));
            }
            catch (Exception ex) { Debug.WriteLine("[PrevJson] Update error: " + ex.Message); }
            finally { _updatingPrevFromEditors = false; }
        }
        private void ApplyJsonToPrevious(string json)
        {
            if (_updatingPrevFromJson) return;
            var tab = SelectedPreviousStudy;
            try
            {
                if (string.IsNullOrWhiteSpace(json) || json.Length < 2) return;
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                string newHeaderTemp = root.TryGetProperty("header_temp", out var htEl) ? (htEl.GetString() ?? string.Empty) : string.Empty;
                string newHeaderAndFindings = root.TryGetProperty("header_and_findings", out var hfEl) ? (hfEl.GetString() ?? string.Empty) : string.Empty;
                string newFinalConclusion = root.TryGetProperty("final_conclusion", out var fcEl) ? (fcEl.GetString() ?? string.Empty) : string.Empty;
                string newFindingsOut = root.TryGetProperty("findings", out var fEl2) ? (fEl2.GetString() ?? string.Empty) : string.Empty;
                string newConclusionOut = root.TryGetProperty("conclusion", out var cEl2) ? (cEl2.GetString() ?? string.Empty) : string.Empty;
                string newStudyRemark = root.TryGetProperty("study_remark", out var sEl) ? (sEl.GetString() ?? string.Empty) : string.Empty;
                string newChiefComplaint = root.TryGetProperty("chief_complaint", out var ccEl) ? (ccEl.GetString() ?? string.Empty) : string.Empty;
                string newPatientHistory = root.TryGetProperty("patient_history", out var phEl) ? (phEl.GetString() ?? string.Empty) : string.Empty;
                string newStudyTechniques = root.TryGetProperty("study_techniques", out var stEl) ? (stEl.GetString() ?? string.Empty) : string.Empty;
                string newComparison = root.TryGetProperty("comparison", out var compEl) ? (compEl.GetString() ?? string.Empty) : string.Empty;
                string newPatientRemark = root.TryGetProperty("patient_remark", out var pEl) ? (pEl.GetString() ?? string.Empty) : string.Empty;
                string newChiefComplaintPf = root.TryGetProperty("chief_complaint_proofread", out var ccpEl) ? (ccpEl.GetString() ?? string.Empty) : string.Empty;
                string newPatientHistoryPf = root.TryGetProperty("patient_history_proofread", out var phpEl) ? (phpEl.GetString() ?? string.Empty) : string.Empty;
                string newStudyTechniquesPf = root.TryGetProperty("study_techniques_proofread", out var stpEl) ? (stpEl.GetString() ?? string.Empty) : string.Empty;
                string newComparisonPf = root.TryGetProperty("comparison_proofread", out var cpEl) ? (cpEl.GetString() ?? string.Empty) : string.Empty;
                string newFindingsPf = root.TryGetProperty("findings_proofread", out var fpEl) ? (fpEl.GetString() ?? string.Empty) : string.Empty;
                string newConclusionPf = root.TryGetProperty("conclusion_proofread", out var clpEl) ? (clpEl.GetString() ?? string.Empty) : string.Empty;
                // Optional nested PrevReport splitter fields
                int? ReadInt(JsonElement obj, string name)
                {
                    if (obj.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out var i)) return i; return null;
                }
                int? hfHeaderFrom = null, hfHeaderTo = null, hfConclusionFrom = null, hfConclusionTo = null;
                int? fcHeaderFrom = null, fcHeaderTo = null, fcFindingsFrom = null, fcFindingsTo = null;
                string? prStudyRemark = null, prPatientRemark = null, prChiefComplaint = null, prPatientHistory = null, prStudyTechniques = null, prComparison = null;
                if (root.TryGetProperty("PrevReport", out var prEl) && prEl.ValueKind == JsonValueKind.Object)
                {
                    hfHeaderFrom = ReadInt(prEl, "header_and_findings_header_splitter_from");
                    hfHeaderTo = ReadInt(prEl, "header_and_findings_header_splitter_to");
                    hfConclusionFrom = ReadInt(prEl, "header_and_findings_conclusion_splitter_from");
                    hfConclusionTo = ReadInt(prEl, "header_and_findings_conclusion_splitter_to");
                    fcHeaderFrom = ReadInt(prEl, "final_conclusion_header_splitter_from");
                    fcHeaderTo = ReadInt(prEl, "final_conclusion_header_splitter_to");
                    fcFindingsFrom = ReadInt(prEl, "final_conclusion_findings_splitter_from");
                    fcFindingsTo = ReadInt(prEl, "final_conclusion_findings_splitter_to");
                    prStudyRemark = prEl.TryGetProperty("study_remark", out var srm) ? (srm.GetString() ?? string.Empty) : null;
                    prPatientRemark = prEl.TryGetProperty("patient_remark", out var prm) ? (prm.GetString() ?? string.Empty) : null;
                    prChiefComplaint = prEl.TryGetProperty("chief_complaint", out var ccm) ? (ccm.GetString() ?? string.Empty) : null;
                    prPatientHistory = prEl.TryGetProperty("patient_history", out var phm) ? (phm.GetString() ?? string.Empty) : null;
                    prStudyTechniques = prEl.TryGetProperty("study_techniques", out var stm) ? (stm.GetString() ?? string.Empty) : null;
                    prComparison = prEl.TryGetProperty("comparison", out var cpm) ? (cpm.GetString() ?? string.Empty) : null;
                }
                _updatingPrevFromJson = true;
                if (tab == null)
                {
                    // Apply to caches when no tab selected yet
                    bool changed = false;
                    if (_prevHeaderTempCache != newHeaderTemp) { _prevHeaderTempCache = newHeaderTemp; OnPropertyChanged(nameof(PreviousHeaderTemp)); changed = true; }
                    if (_prevHeaderAndFindingsCache != newHeaderAndFindings) { _prevHeaderAndFindingsCache = newHeaderAndFindings; OnPropertyChanged(nameof(PreviousHeaderAndFindingsText)); changed = true; }
                    if (_prevFinalConclusionCache != newFinalConclusion) { _prevFinalConclusionCache = newFinalConclusion; OnPropertyChanged(nameof(PreviousFinalConclusionText)); changed = true; }
                    if (_prevFindingsOutCache != newFindingsOut) { _prevFindingsOutCache = newFindingsOut; OnPropertyChanged(nameof(PreviousSplitFindings)); changed = true; }
                    if (_prevConclusionOutCache != newConclusionOut) { _prevConclusionOutCache = newConclusionOut; OnPropertyChanged(nameof(PreviousSplitConclusion)); changed = true; }
                    _prevStudyRemarkCache = newStudyRemark;
                    _prevPatientRemarkCache = newPatientRemark;
                    _prevChiefComplaintCache = newChiefComplaint;
                    _prevPatientHistoryCache = newPatientHistory;
                    _prevStudyTechniquesCache = newStudyTechniques;
                    _prevComparisonCache = newComparison;
                    _prevChiefComplaintProofreadCache = newChiefComplaintPf;
                    _prevPatientHistoryProofreadCache = newPatientHistoryPf;
                    _prevStudyTechniquesProofreadCache = newStudyTechniquesPf;
                    _prevComparisonProofreadCache = newComparisonPf;
                    _prevFindingsProofreadCache = newFindingsPf;
                    _prevConclusionProofreadCache = newConclusionPf;
                    if (changed) UpdatePreviousReportJson(); else OnPropertyChanged(nameof(PreviousReportJson));
                }
                else
                {
                    bool changed = false;
                    if (tab.HeaderTemp != newHeaderTemp) { tab.HeaderTemp = newHeaderTemp; changed = true; }
                    if (tab.Findings != newHeaderAndFindings) { tab.Findings = newHeaderAndFindings; changed = true; }
                    if (tab.Conclusion != newFinalConclusion) { tab.Conclusion = newFinalConclusion; changed = true; }
                    if (tab.FindingsOut != newFindingsOut) { tab.FindingsOut = newFindingsOut; changed = true; }
                    if (tab.ConclusionOut != newConclusionOut) { tab.ConclusionOut = newConclusionOut; changed = true; }
                    if (newStudyRemark != tab.StudyRemark) { tab.StudyRemark = newStudyRemark; changed = true; }
                    if (newPatientRemark != tab.PatientRemark) { tab.PatientRemark = newPatientRemark; changed = true; }
                    if (newChiefComplaint != tab.ChiefComplaint) { tab.ChiefComplaint = newChiefComplaint; changed = true; }
                    if (newPatientHistory != tab.PatientHistory) { tab.PatientHistory = newPatientHistory; changed = true; }
                    if (newStudyTechniques != tab.StudyTechniques) { tab.StudyTechniques = newStudyTechniques; changed = true; }
                    if (newComparison != tab.Comparison) { tab.Comparison = newComparison; changed = true; }
                    if (newChiefComplaintPf != tab.ChiefComplaintProofread) { tab.ChiefComplaintProofread = newChiefComplaintPf; changed = true; }
                    if (newPatientHistoryPf != tab.PatientHistoryProofread) { tab.PatientHistoryProofread = newPatientHistoryPf; changed = true; }
                    if (newStudyTechniquesPf != tab.StudyTechniquesProofread) { tab.StudyTechniquesProofread = newStudyTechniquesPf; changed = true; }
                    if (newComparisonPf != tab.ComparisonProofread) { tab.ComparisonProofread = newComparisonPf; changed = true; }
                    if (newFindingsPf != tab.FindingsProofread) { tab.FindingsProofread = newFindingsPf; changed = true; }
                    if (newConclusionPf != tab.ConclusionProofread) { tab.ConclusionProofread = newConclusionPf; changed = true; }
                    // Update splitter fields if present
                    if (hfHeaderFrom != tab.HfHeaderFrom) { tab.HfHeaderFrom = hfHeaderFrom; changed = true; }
                    if (hfHeaderTo != tab.HfHeaderTo) { tab.HfHeaderTo = hfHeaderTo; changed = true; }
                    if (hfConclusionFrom != tab.HfConclusionFrom) { tab.HfConclusionFrom = hfConclusionFrom; changed = true; }
                    if (hfConclusionTo != tab.HfConclusionTo) { tab.HfConclusionTo = hfConclusionTo; changed = true; }
                    if (fcHeaderFrom != tab.FcHeaderFrom) { tab.FcHeaderFrom = fcHeaderFrom; changed = true; }
                    if (fcHeaderTo != tab.FcHeaderTo) { tab.FcHeaderTo = fcHeaderTo; changed = true; }
                    if (fcFindingsFrom != tab.FcFindingsFrom) { tab.FcFindingsFrom = fcFindingsFrom; changed = true; }
                    if (fcFindingsTo != tab.FcFindingsTo) { tab.FcFindingsTo = fcFindingsTo; changed = true; }
                    // metadata now at root; ignore values in PrevReport
                    if (changed) UpdatePreviousReportJson(); else OnPropertyChanged(nameof(PreviousReportJson));
                }
            }
            catch (Exception ex) { Debug.WriteLine("[PrevJson] Apply error: " + ex.Message); }
            finally { _updatingPrevFromJson = false; }
        }
        private void HookPreviousStudy(PreviousStudyTab? oldTab, PreviousStudyTab? newTab)
        {
            if (oldTab != null) oldTab.PropertyChanged -= OnSelectedPrevStudyPropertyChanged;
            if (newTab != null) { newTab.PropertyChanged += OnSelectedPrevStudyPropertyChanged; UpdatePreviousReportJson(); }
        }
        private void OnSelectedPrevStudyPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (_updatingPrevFromJson) return;
            if (e.PropertyName == nameof(PreviousStudyTab.Findings)
                || e.PropertyName == nameof(PreviousStudyTab.Conclusion)
                || e.PropertyName == nameof(PreviousStudyTab.Header)
                || e.PropertyName == nameof(PreviousStudyTab.HfHeaderFrom)
                || e.PropertyName == nameof(PreviousStudyTab.HfHeaderTo)
                || e.PropertyName == nameof(PreviousStudyTab.HfConclusionFrom)
                || e.PropertyName == nameof(PreviousStudyTab.HfConclusionTo)
                || e.PropertyName == nameof(PreviousStudyTab.FcHeaderFrom)
                || e.PropertyName == nameof(PreviousStudyTab.FcHeaderTo)
                || e.PropertyName == nameof(PreviousStudyTab.FcFindingsFrom)
                || e.PropertyName == nameof(PreviousStudyTab.FcFindingsTo)
                || e.PropertyName == nameof(PreviousStudyTab.ChiefComplaint)
                || e.PropertyName == nameof(PreviousStudyTab.PatientHistory)
                || e.PropertyName == nameof(PreviousStudyTab.StudyTechniques)
                || e.PropertyName == nameof(PreviousStudyTab.Comparison)
                || e.PropertyName == nameof(PreviousStudyTab.StudyRemark)
                || e.PropertyName == nameof(PreviousStudyTab.PatientRemark)
                || e.PropertyName == nameof(PreviousStudyTab.HeaderTemp)
                || e.PropertyName == nameof(PreviousStudyTab.FindingsOut)
                || e.PropertyName == nameof(PreviousStudyTab.ConclusionOut)
                || e.PropertyName == nameof(PreviousStudyTab.ChiefComplaintProofread)
                || e.PropertyName == nameof(PreviousStudyTab.PatientHistoryProofread)
                || e.PropertyName == nameof(PreviousStudyTab.StudyTechniquesProofread)
                || e.PropertyName == nameof(PreviousStudyTab.ComparisonProofread)
                || e.PropertyName == nameof(PreviousStudyTab.FindingsProofread)
                || e.PropertyName == nameof(PreviousStudyTab.ConclusionProofread))
            {
                Debug.WriteLine($"[PrevTab] PropertyChanged -> {e.PropertyName}");
                OnPropertyChanged(nameof(PreviousHeaderText));
                OnPropertyChanged(nameof(PreviousHeaderAndFindingsText));
                OnPropertyChanged(nameof(PreviousFinalConclusionText));
                OnPropertyChanged(nameof(PreviousHeaderTemp));
                OnPropertyChanged(nameof(PreviousSplitFindings));
                OnPropertyChanged(nameof(PreviousSplitConclusion));
                OnPropertyChanged(nameof(PreviousHeaderSplitView));
                OnPropertyChanged(nameof(PreviousFindingsSplitView));
                OnPropertyChanged(nameof(PreviousConclusionSplitView));
                
                // NEW: Notify editor properties when underlying data changes (especially proofread fields)
                OnPropertyChanged(nameof(PreviousFindingsEditorText));
                OnPropertyChanged(nameof(PreviousConclusionEditorText));
                
                UpdatePreviousReportJson();
            }
        }
    }
}
