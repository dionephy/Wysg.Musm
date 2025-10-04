using System;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Wysg.Musm.Radium.ViewModels
{
    /// <summary>
    /// Partial: Current editor text, reportify toggling and JSON synchronization.
    /// - Maintains raw vs transformed (reportified) text blocks
    /// - Auto-unreportify when user edits transformed content
    /// - Synchronizes a JSON view (CurrentReportJson) with findings + conclusion fields
    /// </summary>
    public partial class MainViewModel
    {
        // Raw (unmodified) copies captured when entering reportified mode
        private string _rawHeader = string.Empty;
        private string _rawFindings = string.Empty;
        private string _rawConclusion = string.Empty;
        private bool _reportified; public bool Reportified { get => _reportified; set => ToggleReportified(value); }

        private bool _suppressAutoToggle; // prevents recursive toggling while programmatically changing text

        private void ToggleReportified(bool value)
        {
            if (!SetProperty(ref _reportified, value)) return;
            if (value)
            {
                CaptureRawIfNeeded();
                _suppressAutoToggle = true;
                HeaderText = SimpleReportifyBlock(_rawHeader);
                FindingsText = SimpleReportifyBlock(_rawFindings);
                ConclusionText = ReportifyConclusion(_rawConclusion);
                _suppressAutoToggle = false;
            }
            else
            {
                _suppressAutoToggle = true;
                HeaderText = _rawHeader;
                FindingsText = _rawFindings;
                ConclusionText = _rawConclusion;
                _suppressAutoToggle = false;
            }
        }
        private void CaptureRawIfNeeded()
        {
            if (!_reportified)
            {
                _rawHeader = HeaderText;
                _rawFindings = ReportFindings; // use alias
                _rawConclusion = ConclusionText;
            }
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

        // Editor-bound properties (override setters to integrate auto-unreportify + JSON update)
        private string _headerText = string.Empty; public string HeaderText { get => _headerText; set { if (value != _headerText) { AutoUnreportifyOnEdit(); if (!_reportified) _rawHeader = value; if (SetProperty(ref _headerText, value)) UpdateCurrentReportJson(); } } }
        private string _findingsText = string.Empty; public string FindingsText { get => _findingsText; set { if (value != _findingsText) { AutoUnreportifyOnEdit(); if (!_reportified) _rawFindings = value; if (SetProperty(ref _findingsText, value)) UpdateCurrentReportJson(); } } }
        private string _conclusionText = string.Empty; public string ConclusionText { get => _conclusionText; set { if (value != _conclusionText) { AutoUnreportifyOnEdit(); if (!_reportified) _rawConclusion = value; if (SetProperty(ref _conclusionText, value)) UpdateCurrentReportJson(); } } }
        // Alias retained for compatibility (some code referenced ReportFindings originally)
        public string ReportFindings { get => _findingsText; set { if (value != _findingsText) { AutoUnreportifyOnEdit(); if (!_reportified) _rawFindings = value; if (SetProperty(ref _findingsText, value)) UpdateCurrentReportJson(); } } }

        // Simple transformation: capitalize first char of each line and ensure trailing period
        private string SimpleReportifyBlock(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            var lines = input.Replace("\r\n", "\n").Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                var l = lines[i].Trim();
                if (l.Length == 0) { lines[i] = string.Empty; continue; }
                l = char.ToUpper(l[0]) + (l.Length > 1 ? l[1..] : string.Empty);
                if (!l.EndsWith('.') && char.IsLetterOrDigit(l[^1])) l += '.';
                lines[i] = l;
            }
            return string.Join("\n", lines);
        }
        private string ReportifyConclusion(string input) => SimpleReportifyBlock(input);

        // JSON synchronization (CurrentReportJson)
        private bool _updatingFromEditors; // guard while pushing to JSON
        private bool _updatingFromJson;    // guard while parsing JSON -> editors
        private string _currentReportJson = "{}"; public string CurrentReportJson { get => _currentReportJson; set { if (SetProperty(ref _currentReportJson, value)) { if (_updatingFromEditors) return; ApplyJsonToEditors(value); } } }

        private void UpdateCurrentReportJson()
        {
            try
            {
                var obj = new
                {
                    findings = _rawFindings == string.Empty ? ReportFindings : _rawFindings,
                    conclusion = _rawConclusion == string.Empty ? ConclusionText : _rawConclusion
                };
                _updatingFromEditors = true;
                CurrentReportJson = JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true });
            }
            catch { }
            finally { _updatingFromEditors = false; }
        }
        private void ApplyJsonToEditors(string json)
        {
            if (_updatingFromJson) return;
            try
            {
                if (string.IsNullOrWhiteSpace(json) || json.Length < 2) return;
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                string newFindings = root.TryGetProperty("findings", out var fEl) ? (fEl.GetString() ?? string.Empty) : string.Empty;
                string newConclusion = root.TryGetProperty("conclusion", out var cEl) ? (cEl.GetString() ?? string.Empty) : string.Empty;
                _updatingFromJson = true;
                _rawFindings = newFindings; _rawConclusion = newConclusion; // keep raw updated
                if (!_reportified)
                {
                    if (FindingsText != newFindings) _findingsText = newFindings; // assign backing fields directly to avoid auto toggle
                    if (ConclusionText != newConclusion) _conclusionText = newConclusion;
                    OnPropertyChanged(nameof(FindingsText));
                    OnPropertyChanged(nameof(ConclusionText));
                }
                else
                {
                    FindingsText = SimpleReportifyBlock(newFindings);
                    ConclusionText = ReportifyConclusion(newConclusion);
                }
            }
            catch { }
            finally { _updatingFromJson = false; }
        }

        // Utility access for exporting raw or transformed sections
        public (string header, string findings, string conclusion) GetDereportifiedSections()
        {
            string h = Reportified ? HeaderText : HeaderText;
            string f = Reportified ? FindingsText : FindingsText;
            string c = Reportified ? ConclusionText : ConclusionText;
            return (h, f, c);
        }
    }
}
