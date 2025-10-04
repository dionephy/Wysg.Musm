using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics; // added for debug logging
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using Wysg.Musm.Infrastructure.ViewModels; // Added for BaseViewModel

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
            public string OriginalHeader { get; set; } = string.Empty;
            public string OriginalFindings { get; set; } = string.Empty;
            public string OriginalConclusion { get; set; } = string.Empty;
            public bool ReportifiedApplied { get; set; }
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
        { get => _selectedPreviousStudy; set { var old = _selectedPreviousStudy; if (SetProperty(ref _selectedPreviousStudy, value)) { Debug.WriteLine($"[Prev] SelectedPreviousStudy set -> {(value==null?"<null>":value.Title)}"); foreach (var t in PreviousStudies) t.IsSelected = (value != null && t.Id == value.Id); HookPreviousStudy(old, value); ApplyPreviousReportifiedState(); OnPropertyChanged(nameof(PreviousHeaderText)); OnPropertyChanged(nameof(PreviousFindingsText)); OnPropertyChanged(nameof(PreviousConclusionText)); UpdatePreviousReportJson(); } } }
        private bool _previousReportified; public bool PreviousReportified { get => _previousReportified; set { if (SetProperty(ref _previousReportified, value)) { Debug.WriteLine($"[Prev] PreviousReportified -> {value}"); ApplyPreviousReportifiedState(); } } }

        private void ApplyPreviousReportifiedState()
        {
            var tab = SelectedPreviousStudy; if (tab == null) return;
            Debug.WriteLine($"[Prev] ApplyPreviousReportifiedState reportified={PreviousReportified}");
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
            UpdatePreviousReportJson();
        }

        // ---------------- Previous Editors (wrappers reusing underlying tab values) ----------------
        private string _prevHeaderCache = string.Empty;
        private string _prevFindingsCache = string.Empty;
        private string _prevConclusionCache = string.Empty;
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
        public string PreviousFindingsText
        {
            get => SelectedPreviousStudy?.Findings ?? _prevFindingsCache;
            set
            {
                if (SelectedPreviousStudy == null)
                { if (value == _prevFindingsCache) return; _prevFindingsCache = value; OnPropertyChanged(); Debug.WriteLine($"[PrevEdit] Cache Findings len={value?.Length}"); }
                else if (SelectedPreviousStudy.Findings != value) { SelectedPreviousStudy.Findings = value; Debug.WriteLine($"[PrevEdit] Tab Findings len={value?.Length}"); }
                UpdatePreviousReportJson();
                OnPropertyChanged(nameof(PreviousReportJson));
            }
        }
        public string PreviousConclusionText
        {
            get => SelectedPreviousStudy?.Conclusion ?? _prevConclusionCache;
            set
            {
                if (SelectedPreviousStudy == null)
                { if (value == _prevConclusionCache) return; _prevConclusionCache = value; OnPropertyChanged(); Debug.WriteLine($"[PrevEdit] Cache Conclusion len={value?.Length}"); }
                else if (SelectedPreviousStudy.Conclusion != value) { SelectedPreviousStudy.Conclusion = value; Debug.WriteLine($"[PrevEdit] Tab Conclusion len={value?.Length}"); }
                UpdatePreviousReportJson();
                OnPropertyChanged(nameof(PreviousReportJson));
            }
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
                    obj = new { header = _prevHeaderCache, findings = _prevFindingsCache, conclusion = _prevConclusionCache };
                    Debug.WriteLine($"[PrevJson] Update (cache only) hLen={_prevHeaderCache?.Length} fLen={_prevFindingsCache?.Length} cLen={_prevConclusionCache?.Length}");
                }
                else
                {
                    obj = new { header = tab.Header ?? string.Empty, findings = tab.Findings ?? string.Empty, conclusion = tab.Conclusion ?? string.Empty };
                    Debug.WriteLine($"[PrevJson] Update (tab) hLen={tab.Header?.Length} fLen={tab.Findings?.Length} cLen={tab.Conclusion?.Length}");
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
                string newHeader = root.TryGetProperty("header", out var hEl) ? (hEl.GetString() ?? string.Empty) : string.Empty;
                string newFindings = root.TryGetProperty("findings", out var fEl) ? (fEl.GetString() ?? string.Empty) : string.Empty;
                string newConclusion = root.TryGetProperty("conclusion", out var cEl) ? (cEl.GetString() ?? string.Empty) : string.Empty;
                _updatingPrevFromJson = true;
                if (tab == null)
                {
                    // Apply to caches when no tab selected yet
                    bool changed = false;
                    if (_prevHeaderCache != newHeader) { _prevHeaderCache = newHeader; OnPropertyChanged(nameof(PreviousHeaderText)); changed = true; }
                    if (_prevFindingsCache != newFindings) { _prevFindingsCache = newFindings; OnPropertyChanged(nameof(PreviousFindingsText)); changed = true; }
                    if (_prevConclusionCache != newConclusion) { _prevConclusionCache = newConclusion; OnPropertyChanged(nameof(PreviousConclusionText)); changed = true; }
                    if (changed) UpdatePreviousReportJson(); else OnPropertyChanged(nameof(PreviousReportJson));
                }
                else
                {
                    bool changed = false;
                    if (tab.Header != newHeader) { tab.Header = newHeader; changed = true; }
                    if (tab.Findings != newFindings) { tab.Findings = newFindings; changed = true; }
                    if (tab.Conclusion != newConclusion) { tab.Conclusion = newConclusion; changed = true; }
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
            if (e.PropertyName == nameof(PreviousStudyTab.Findings) || e.PropertyName == nameof(PreviousStudyTab.Conclusion) || e.PropertyName == nameof(PreviousStudyTab.Header))
            {
                Debug.WriteLine($"[PrevTab] PropertyChanged -> {e.PropertyName}");
                OnPropertyChanged(nameof(PreviousHeaderText));
                OnPropertyChanged(nameof(PreviousFindingsText));
                OnPropertyChanged(nameof(PreviousConclusionText));
                UpdatePreviousReportJson();
            }
        }
    }
}
