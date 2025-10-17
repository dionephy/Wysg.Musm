using System;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Diagnostics;

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

        // New: PACS remarks captured via automation modules
        private string _studyRemark = string.Empty; public string StudyRemark { get => _studyRemark; set { if (SetProperty(ref _studyRemark, value ?? string.Empty)) UpdateCurrentReportJson(); } }
        private string _patientRemark = string.Empty; public string PatientRemark { get => _patientRemark; set { if (SetProperty(ref _patientRemark, value ?? string.Empty)) UpdateCurrentReportJson(); } }

        // Header fields for real-time formatting
        private string _chiefComplaint = string.Empty; 
        public string ChiefComplaint 
        { 
            get => _chiefComplaint; 
            set 
            { 
                Debug.WriteLine($"[Editor] ChiefComplaint setter called with value: {value?.Length ?? 0} chars");
                if (SetProperty(ref _chiefComplaint, value ?? string.Empty)) 
                { 
                    Debug.WriteLine("[Editor] ChiefComplaint property changed, calling updates...");
                    SafeUpdateJson(); 
                    SafeUpdateHeader(); 
                } 
            } 
        }

        // Proofread fields for current report (root JSON)
        private string _chiefComplaintProofread = string.Empty; public string ChiefComplaintProofread { get => _chiefComplaintProofread; set { if (SetProperty(ref _chiefComplaintProofread, value ?? string.Empty)) UpdateCurrentReportJson(); } }
        private string _patientHistoryProofread = string.Empty; public string PatientHistoryProofread { get => _patientHistoryProofread; set { if (SetProperty(ref _patientHistoryProofread, value ?? string.Empty)) UpdateCurrentReportJson(); } }
        private string _studyTechniquesProofread = string.Empty; public string StudyTechniquesProofread { get => _studyTechniquesProofread; set { if (SetProperty(ref _studyTechniquesProofread, value ?? string.Empty)) UpdateCurrentReportJson(); } }
        private string _comparisonProofread = string.Empty; public string ComparisonProofread { get => _comparisonProofread; set { if (SetProperty(ref _comparisonProofread, value ?? string.Empty)) UpdateCurrentReportJson(); } }
        private string _findingsProofread = string.Empty; public string FindingsProofread { get => _findingsProofread; set { if (SetProperty(ref _findingsProofread, value ?? string.Empty)) UpdateCurrentReportJson(); } }
        private string _conclusionProofread = string.Empty; public string ConclusionProofread { get => _conclusionProofread; set { if (SetProperty(ref _conclusionProofread, value ?? string.Empty)) UpdateCurrentReportJson(); } }
        
        private string _patientHistory = string.Empty; 
        public string PatientHistory 
        { 
            get => _patientHistory; 
            set 
            { 
                Debug.WriteLine($"[Editor] PatientHistory setter called with value: {value?.Length ?? 0} chars");
                if (SetProperty(ref _patientHistory, value ?? string.Empty)) 
                { 
                    Debug.WriteLine("[Editor] PatientHistory property changed, calling updates...");
                    SafeUpdateJson(); 
                    SafeUpdateHeader(); 
                } 
            } 
        }
        
        private string _studyTechniques = string.Empty; 
        public string StudyTechniques 
        { 
            get => _studyTechniques; 
            set 
            { 
                Debug.WriteLine($"[Editor] StudyTechniques setter called with value: {value?.Length ?? 0} chars");
                if (SetProperty(ref _studyTechniques, value ?? string.Empty)) 
                { 
                    Debug.WriteLine("[Editor] StudyTechniques property changed, calling updates...");
                    SafeUpdateJson(); 
                    SafeUpdateHeader(); 
                } 
            } 
        }
        
        private string _comparison = string.Empty; 
        public string Comparison 
        { 
            get => _comparison; 
            set 
            { 
                Debug.WriteLine($"[Editor] Comparison setter called with value: {value?.Length ?? 0} chars");
                if (SetProperty(ref _comparison, value ?? string.Empty)) 
                { 
                    Debug.WriteLine("[Editor] Comparison property changed, calling updates...");
                    SafeUpdateJson(); 
                    SafeUpdateHeader(); 
                } 
            } 
        }

        private bool _suppressAutoToggle; // prevents recursive toggling while programmatically changing text
        private bool _isInitialized; // prevents updates during initialization

        private void ToggleReportified(bool value)
        {
            if (!SetProperty(ref _reportified, value)) return;
            if (value)
            {
                CaptureRawIfNeeded();
                _suppressAutoToggle = true;
                HeaderText = ApplyReportifyBlock(_rawHeader, false);
                FindingsText = ApplyReportifyBlock(_rawFindings, false);
                ConclusionText = ApplyReportifyConclusion(_rawConclusion);
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
                try
                {
                    // Set reportified to false first
                    _reportified = false;
                    OnPropertyChanged(nameof(Reportified));
                    
                    // Then update the text properties to dereportified values
                    HeaderText = _rawHeader;
                    FindingsText = _rawFindings;
                    ConclusionText = _rawConclusion;
                }
                finally
                {
                    _suppressAutoToggle = false;
                }
            }
        }

        // Editor-bound properties (override setters to integrate auto-unreportify + JSON update)
        // NEW BEHAVIOR: When reportified, cancel the text change and only dereportify
        // NOTE: HeaderText is now auto-generated from component fields (chief_complaint, patient_history, study_techniques, comparison)
        private string _headerText = string.Empty; 
        public string HeaderText 
        { 
            get => _headerText; 
            private set // Changed to private - header is computed
            { 
                if (value != _headerText) 
                { 
                    if (!_reportified) _rawHeader = value; 
                    SetProperty(ref _headerText, value);
                } 
            } 
        }
        
        private string _findingsText = string.Empty; 
        public string FindingsText 
        { 
            get => _findingsText; 
            set 
            { 
                if (value != _findingsText) 
                { 
                    // NEW: If reportified and not suppressing, cancel change and dereportify
                    if (_reportified && !_suppressAutoToggle)
                    {
                        AutoUnreportifyOnEdit();
                        return; // Cancel the text change
                    }
                    if (!_reportified) _rawFindings = value; 
                    if (SetProperty(ref _findingsText, value)) 
                    {
                        UpdateCurrentReportJson();
                    }
                } 
            } 
        }
        
        private string _conclusionText = string.Empty; 
        public string ConclusionText 
        { 
            get => _conclusionText; 
            set 
            { 
                if (value != _conclusionText) 
                { 
                    // NEW: If reportified and not suppressing, cancel change and dereportify
                    if (_reportified && !_suppressAutoToggle)
                    {
                        AutoUnreportifyOnEdit();
                        return; // Cancel the text change
                    }
                    if (!_reportified) _rawConclusion = value; 
                    if (SetProperty(ref _conclusionText, value)) UpdateCurrentReportJson(); 
                } 
            } 
        }
        
        // Alias retained for compatibility (some code referenced ReportFindings originally)
        public string ReportFindings 
        { 
            get => _findingsText; 
            set 
            { 
                if (value != _findingsText) 
                { 
                    // NEW: If reportified and not suppressing, cancel change and dereportify
                    if (_reportified && !_suppressAutoToggle)
                    {
                        AutoUnreportifyOnEdit();
                        return; // Cancel the text change
                    }
                    if (!_reportified) _rawFindings = value; 
                    if (SetProperty(ref _findingsText, value)) UpdateCurrentReportJson(); 
                } 
            } 
        }

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
                    conclusion = _rawConclusion == string.Empty ? ConclusionText : _rawConclusion,
                    study_remark = _studyRemark,
                    patient_remark = _patientRemark,
                    chief_complaint = _chiefComplaint,
                    patient_history = _patientHistory,
                    study_techniques = _studyTechniques,
                    comparison = _comparison,
                    chief_complaint_proofread = _chiefComplaintProofread,
                    patient_history_proofread = _patientHistoryProofread,
                    study_techniques_proofread = _studyTechniquesProofread,
                    comparison_proofread = _comparisonProofread,
                    findings_proofread = _findingsProofread,
                    conclusion_proofread = _conclusionProofread
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
                _updatingFromJson = true;
                _rawFindings = newFindings; _rawConclusion = newConclusion; // keep raw updated
                StudyRemark = newStudyRemark; // round-trippable
                PatientRemark = newPatientRemark; // now round-trippable as well
                // Update header component fields
                _chiefComplaint = newChiefComplaint; OnPropertyChanged(nameof(ChiefComplaint));
                _patientHistory = newPatientHistory; OnPropertyChanged(nameof(PatientHistory));
                _studyTechniques = newStudyTechniques; OnPropertyChanged(nameof(StudyTechniques));
                _comparison = newComparison; OnPropertyChanged(nameof(Comparison));
                // Proofread fields
                _chiefComplaintProofread = newChiefComplaintPf; OnPropertyChanged(nameof(ChiefComplaintProofread));
                _patientHistoryProofread = newPatientHistoryPf; OnPropertyChanged(nameof(PatientHistoryProofread));
                _studyTechniquesProofread = newStudyTechniquesPf; OnPropertyChanged(nameof(StudyTechniquesProofread));
                _comparisonProofread = newComparisonPf; OnPropertyChanged(nameof(ComparisonProofread));
                _findingsProofread = newFindingsPf; OnPropertyChanged(nameof(FindingsProofread));
                _conclusionProofread = newConclusionPf; OnPropertyChanged(nameof(ConclusionProofread));
                // Recompute HeaderText from component fields even during JSON updates
                UpdateFormattedHeader();
                // Do not assign PatientRemark here; it is updated by automation AcquirePatientRemarkAsync only
                if (!_reportified)
                {
                    if (FindingsText != newFindings) _findingsText = newFindings; // assign backing fields directly to avoid auto toggle
                    if (ConclusionText != newConclusion) _conclusionText = newConclusion;
                    OnPropertyChanged(nameof(FindingsText));
                    OnPropertyChanged(nameof(ConclusionText));
                }
                else
                {
                    FindingsText = ApplyReportifyBlock(newFindings, false);
                    ConclusionText = ApplyReportifyConclusion(newConclusion);
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

        // Safe wrappers that check initialization state
        private void SafeUpdateJson()
        {
            if (!_isInitialized)
            {
                Debug.WriteLine("[Editor] SafeUpdateJson: Skipped (not initialized)");
                return;
            }
            try 
            { 
                Debug.WriteLine("[Editor] SafeUpdateJson: Executing UpdateCurrentReportJson");
                UpdateCurrentReportJson(); 
            } 
            catch (Exception ex)
            {
                Debug.WriteLine($"[Editor] SafeUpdateJson EXCEPTION: {ex.GetType().Name} - {ex.Message}");
            }
        }

        private void SafeUpdateHeader()
        {
            if (!_isInitialized)
            {
                Debug.WriteLine("[Editor] SafeUpdateHeader: Skipped (not initialized)");
                return;
            }
            try 
            { 
                Debug.WriteLine("[Editor] SafeUpdateHeader: Executing UpdateFormattedHeader");
                UpdateFormattedHeader(); 
            } 
            catch (Exception ex)
            {
                Debug.WriteLine($"[Editor] SafeUpdateHeader EXCEPTION: {ex.GetType().Name} - {ex.Message}");
            }
        }

        // Real-time header formatting based on component fields
        private void UpdateFormattedHeader()
        {
            var lines = new System.Collections.Generic.List<string>();
            
            bool hasClinicalInfo = !string.IsNullOrWhiteSpace(_chiefComplaint) || !string.IsNullOrWhiteSpace(_patientHistory);
            bool hasTechniques = !string.IsNullOrWhiteSpace(_studyTechniques);
            bool hasAnyHeaderContent = hasClinicalInfo || hasTechniques;
            
            // Clinical information logic
            if (hasClinicalInfo)
            {
                if (string.IsNullOrWhiteSpace(_chiefComplaint) && !string.IsNullOrWhiteSpace(_patientHistory))
                {
                    // Chief complaint is empty but history exists -> show "Clinical information: NA"
                    lines.Add("Clinical information: NA");
                }
                else if (!string.IsNullOrWhiteSpace(_chiefComplaint))
                {
                    // Chief complaint exists -> show it as first line
                    lines.Add($"Clinical information: {_chiefComplaint}");
                }
                
                // Add patient history lines (each line prefixed with "- ")
                if (!string.IsNullOrWhiteSpace(_patientHistory))
                {
                    var historyLines = _patientHistory.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in historyLines)
                    {
                        var trimmed = line.Trim();
                        if (!string.IsNullOrWhiteSpace(trimmed))
                        {
                            lines.Add($"- {trimmed}");
                        }
                    }
                }
            }
            
            // Techniques logic
            if (hasTechniques)
            {
                lines.Add($"Techniques: {_studyTechniques}");
            }
            
            // Comparison logic
            if (!string.IsNullOrWhiteSpace(_comparison))
            {
                lines.Add($"Comparison: {_comparison}");
            }
            else if (hasAnyHeaderContent)
            {
                // Comparison is empty but we have other header content -> show "Comparison: NA"
                lines.Add("Comparison: NA");
            }
            // else: if comparison is empty AND no other header content, don't show comparison line at all
            
            // Join and trim the result
            var formatted = string.Join("\n", lines).Trim();
            HeaderText = formatted;
        }
    }
}
