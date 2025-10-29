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
        private bool _reportified; 
        public bool Reportified 
        { 
            get => _reportified; 
            set 
            { 
                Debug.WriteLine($"[Editor.Reportified] Setter called: old={_reportified}, new={value}"); 
                
                // CRITICAL FIX: Always raise PropertyChanged event first to ensure UI synchronization
                // This is essential for automation modules that set Reportified=true
                bool actualChanged = (_reportified != value);
                
                // Update backing field
                _reportified = value;
                
                // Always notify, even if value didn't change (ensures UI syncs after automation)
                OnPropertyChanged(nameof(Reportified));
                Debug.WriteLine($"[Editor.Reportified] PropertyChanged raised, _reportified is now={_reportified}");
                
                // Notify display properties since reportified state affects them
                OnPropertyChanged(nameof(FindingsDisplay));
                OnPropertyChanged(nameof(ConclusionDisplay));
                OnPropertyChanged(nameof(HeaderDisplay)); // NEW: Also notify header display
                
                // Only apply transformations if value actually changed
                if (actualChanged)
                {
                    ToggleReportified(value);
                }
                else
                {
                    Debug.WriteLine($"[Editor.Reportified] Value didn't change, skipping transformation");
                }
            } 
        }

        // New: PACS remarks captured via automation modules
        private string _studyRemark = string.Empty; 
        public string StudyRemark 
        { 
            get => _studyRemark; 
            set 
            { 
                Debug.WriteLine($"[Editor] StudyRemark setter called: old length={_studyRemark?.Length ?? 0}, new length={value?.Length ?? 0}");
                if (SetProperty(ref _studyRemark, value ?? string.Empty)) 
                {
                    Debug.WriteLine($"[Editor] StudyRemark property changed, value='{_studyRemark}'");
                    UpdateCurrentReportJson(); 
                }
            } 
        }
        
        private string _patientRemark = string.Empty; 
        public string PatientRemark 
        { 
            get => _patientRemark; 
            set 
            { 
                Debug.WriteLine($"[Editor] PatientRemark setter called: old length={_patientRemark?.Length ?? 0}, new length={value?.Length ?? 0}");
                Debug.WriteLine($"[Editor] PatientRemark new value first 100 chars: '{(value?.Length > 100 ? value.Substring(0, 100) : value ?? "(null)")}'");
                
                if (SetProperty(ref _patientRemark, value ?? string.Empty)) 
                {
                    Debug.WriteLine($"[Editor] PatientRemark property changed successfully!");
                    Debug.WriteLine($"[Editor] PatientRemark backing field now has length={_patientRemark.Length}");
                    Debug.WriteLine($"[Editor] PatientRemark first line: '{(_patientRemark.Contains('\n') ? _patientRemark.Substring(0, _patientRemark.IndexOf('\n')) : _patientRemark)}'");
                    UpdateCurrentReportJson(); 
                }
                else
                {
                    Debug.WriteLine($"[Editor] PatientRemark SetProperty returned false (value unchanged)");
                }
            } 
        }
        
        // NEW: Reported report fields (populated only by GetReportedReport module)
        private string _reportedHeaderAndFindings = string.Empty; 
        public string ReportedHeaderAndFindings 
        { 
            get => _reportedHeaderAndFindings; 
            set 
            { 
                if (SetProperty(ref _reportedHeaderAndFindings, value ?? string.Empty)) 
                {
                    UpdateCurrentReportJson(); 
                }
            } 
        }
        
        private string _reportedFinalConclusion = string.Empty; 
        public string ReportedFinalConclusion 
        { 
            get => _reportedFinalConclusion; 
            set 
            { 
                if (SetProperty(ref _reportedFinalConclusion, value ?? string.Empty)) 
                {
                    UpdateCurrentReportJson(); 
                }
            } 
        }

        // NEW: Radiologist name captured via GetReportedReport module
        private string _reportRadiologist = string.Empty; public string ReportRadiologist { get => _reportRadiologist; set { if (SetProperty(ref _reportRadiologist, value ?? string.Empty)) UpdateCurrentReportJson(); } }

        // NEW: Findings preorder (saved via Save Preorder button)
        private string _findingsPreorder = string.Empty; 
        public string FindingsPreorder 
        { 
            get => _findingsPreorder; 
            set 
            { 
                if (SetProperty(ref _findingsPreorder, value ?? string.Empty))
                {
                    Debug.WriteLine($"[Editor] FindingsPreorder updated: length={value?.Length ?? 0}");
                    UpdateCurrentReportJson(); 
                }
            } 
        }

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
                    // NEW: Notify computed display property
                    OnPropertyChanged(nameof(ChiefComplaintDisplay));
                    OnPropertyChanged(nameof(HeaderDisplay));
                } 
            } 
        }

        // Proofread fields for current report (root JSON)
        private string _chiefComplaintProofread = string.Empty; 
        public string ChiefComplaintProofread 
        { 
            get => _chiefComplaintProofread; 
            set 
            { 
                if (SetProperty(ref _chiefComplaintProofread, value ?? string.Empty)) 
                {
                    UpdateCurrentReportJson();
                    // NEW: Notify computed display properties
                    OnPropertyChanged(nameof(ChiefComplaintDisplay));
                    OnPropertyChanged(nameof(HeaderDisplay));
                }
            } 
        }
        
        private string _patientHistoryProofread = string.Empty; 
        public string PatientHistoryProofread 
        { 
            get => _patientHistoryProofread; 
            set 
            { 
                if (SetProperty(ref _patientHistoryProofread, value ?? string.Empty)) 
                {
                    UpdateCurrentReportJson();
                    // NEW: Notify computed display properties
                    OnPropertyChanged(nameof(PatientHistoryDisplay));
                    OnPropertyChanged(nameof(HeaderDisplay));
                }
            } 
        }
        
        private string _studyTechniquesProofread = string.Empty; 
        public string StudyTechniquesProofread 
        { 
            get => _studyTechniquesProofread; 
            set 
            { 
                if (SetProperty(ref _studyTechniquesProofread, value ?? string.Empty)) 
                {
                    UpdateCurrentReportJson();
                    // NEW: Notify computed display properties
                    OnPropertyChanged(nameof(StudyTechniquesDisplay));
                    OnPropertyChanged(nameof(HeaderDisplay));
                }
            } 
        }
        
        private string _comparisonProofread = string.Empty; 
        public string ComparisonProofread 
        { 
            get => _comparisonProofread; 
            set 
            { 
                if (SetProperty(ref _comparisonProofread, value ?? string.Empty)) 
                {
                    UpdateCurrentReportJson();
                    // NEW: Notify computed display properties
                    OnPropertyChanged(nameof(ComparisonDisplay));
                    OnPropertyChanged(nameof(HeaderDisplay));
                }
            } 
        }
        
        private string _findingsProofread = string.Empty; 
        public string FindingsProofread 
        { 
            get => _findingsProofread; 
            set 
            { 
                if (SetProperty(ref _findingsProofread, value ?? string.Empty)) 
                {
                    UpdateCurrentReportJson(); 
                    // Notify computed properties
                    OnPropertyChanged(nameof(FindingsDisplay));
                }
            } 
        }
        
        private string _conclusionProofread = string.Empty; 
        public string ConclusionProofread 
        { 
            get => _conclusionProofread; 
            set 
            { 
                if (SetProperty(ref _conclusionProofread, value ?? string.Empty)) 
                {
                    UpdateCurrentReportJson(); 
                    // Notify computed properties
                    OnPropertyChanged(nameof(ConclusionDisplay));
                }
            } 
        }
        
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
                    // NEW: Notify computed display property
                    OnPropertyChanged(nameof(PatientHistoryDisplay));
                    OnPropertyChanged(nameof(HeaderDisplay));
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
                    // NEW: Notify computed display property
                    OnPropertyChanged(nameof(StudyTechniquesDisplay));
                    OnPropertyChanged(nameof(HeaderDisplay));
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
                    // NEW: Notify computed display property
                    OnPropertyChanged(nameof(ComparisonDisplay));
                    OnPropertyChanged(nameof(HeaderDisplay));
                } 
            } 
        }

        // NEW: Computed display properties for header components with proofread support
        // These switch between raw and proofread versions based on ProofreadMode toggle
        public string ChiefComplaintDisplay
        {
            get
            {
                if (ProofreadMode && !string.IsNullOrWhiteSpace(_chiefComplaintProofread))
                {
                    return ApplyProofreadPlaceholders(_chiefComplaintProofread);
                }
                return _chiefComplaint;
            }
        }

        public string PatientHistoryDisplay
        {
            get
            {
                if (ProofreadMode && !string.IsNullOrWhiteSpace(_patientHistoryProofread))
                {
                    return ApplyProofreadPlaceholders(_patientHistoryProofread);
                }
                return _patientHistory;
            }
        }

        public string StudyTechniquesDisplay
        {
            get
            {
                if (ProofreadMode && !string.IsNullOrWhiteSpace(_studyTechniquesProofread))
                {
                    return ApplyProofreadPlaceholders(_studyTechniquesProofread);
                }
                return _studyTechniques;
            }
        }

        public string ComparisonDisplay
        {
            get
            {
                if (ProofreadMode && !string.IsNullOrWhiteSpace(_comparisonProofread))
                {
                    return ApplyProofreadPlaceholders(_comparisonProofread);
                }
                return _comparison;
            }
        }

        // NEW: Computed HeaderDisplay property that formats header from component display properties
        // This mirrors the logic in UpdateFormattedHeader but uses Display properties instead of raw fields
        public string HeaderDisplay
        {
            get
            {
                var lines = new System.Collections.Generic.List<string>();
                
                // Use display properties (which consider ProofreadMode) instead of raw fields
                var chiefComplaintDisplay = ChiefComplaintDisplay;
                var patientHistoryDisplay = PatientHistoryDisplay;
                var studyTechniquesDisplay = StudyTechniquesDisplay;
                var comparisonDisplay = ComparisonDisplay;
                
                bool hasClinicalInfo = !string.IsNullOrWhiteSpace(chiefComplaintDisplay) || !string.IsNullOrWhiteSpace(patientHistoryDisplay);
                bool hasTechniques = !string.IsNullOrWhiteSpace(studyTechniquesDisplay);
                bool hasAnyHeaderContent = hasClinicalInfo || hasTechniques;
                
                // Clinical information logic
                if (hasClinicalInfo)
                {
                    if (string.IsNullOrWhiteSpace(chiefComplaintDisplay) && !string.IsNullOrWhiteSpace(patientHistoryDisplay))
                    {
                        // Chief complaint is empty but history exists -> show "Clinical information: NA"
                        lines.Add("Clinical information: NA");
                    }
                    else if (!string.IsNullOrWhiteSpace(chiefComplaintDisplay))
                    {
                        // Chief complaint exists -> show it as first line
                        lines.Add($"Clinical information: {chiefComplaintDisplay}");
                    }
                    
                    // Add patient history lines (each line prefixed with "- ")
                    if (!string.IsNullOrWhiteSpace(patientHistoryDisplay))
                    {
                        var historyLines = patientHistoryDisplay.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n', StringSplitOptions.RemoveEmptyEntries);
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
                    lines.Add($"Techniques: {studyTechniquesDisplay}");
                }
                
                // Comparison logic
                if (!string.IsNullOrWhiteSpace(comparisonDisplay))
                {
                    lines.Add($"Comparison: {comparisonDisplay}");
                }
                else if (hasAnyHeaderContent)
                {
                    // Comparison is empty but we have other header content -> show "Comparison: NA"
                    lines.Add("Comparison: NA");
                }
                // else: if comparison is empty AND no other header content, don't show comparison line at all
                
                // Join and trim the result
                return string.Join("\n", lines).Trim();
            }
        }

        // NEW: Computed properties that consider BOTH Reportified and ProofreadMode
        // Priority: When BOTH are ON, show reportified version of proofread text
        // This allows showing reportified text regardless of proofread state
        public string FindingsDisplay
        {
            get
            {
                string result;
                
                // PRIORITY 1: Both Reportified AND ProofreadMode are ON
                // Show reportified version of PROOFREAD text
                if (_reportified && ProofreadMode && !string.IsNullOrWhiteSpace(_findingsProofread))
                {
                    var proofreadWithPlaceholders = ApplyProofreadPlaceholders(_findingsProofread);
                    result = ApplyReportifyBlock(proofreadWithPlaceholders, false);
                    Debug.WriteLine($"[FindingsDisplay] BOTH ON, returning reportified(proofread+placeholders) length={result?.Length ?? 0}");
                }
                // PRIORITY 2: Only Reportified is ON
                // Show reportified version of RAW text
                else if (_reportified)
                {
                    result = _findingsText; // This already contains the reportified version of raw text
                    Debug.WriteLine($"[FindingsDisplay] Reportified=true only, returning _findingsText length={result?.Length ?? 0}");
                }
                // PRIORITY 3: Only ProofreadMode is ON
                // Show proofread text with placeholders as-is (not reportified)
                else if (ProofreadMode && !string.IsNullOrWhiteSpace(_findingsProofread))
                {
                    result = ApplyProofreadPlaceholders(_findingsProofread);
                    Debug.WriteLine($"[FindingsDisplay] ProofreadMode=true only, returning _findingsProofread+placeholders length={result?.Length ?? 0}");
                }
                // PRIORITY 4: Both OFF
                // Show raw text
                else
                {
                    result = _findingsText;
                    Debug.WriteLine($"[FindingsDisplay] Both OFF, returning _findingsText length={result?.Length ?? 0}");
                }
                return result;
            }
        }

        public string ConclusionDisplay
        {
            get
            {
                string result;
                
                // PRIORITY 1: Both Reportified AND ProofreadMode are ON
                // Show reportified version of PROOFREAD text
                if (_reportified && ProofreadMode && !string.IsNullOrWhiteSpace(_conclusionProofread))
                {
                    var proofreadWithPlaceholders = ApplyProofreadPlaceholders(_conclusionProofread);
                    result = ApplyReportifyConclusion(proofreadWithPlaceholders);
                    Debug.WriteLine($"[ConclusionDisplay] BOTH ON, returning reportified(proofread+placeholders) length={result?.Length ?? 0}");
                }
                // PRIORITY 2: Only Reportified is ON
                // Show reportified version of RAW text
                else if (_reportified)
                {
                    result = _conclusionText; // This already contains the reportified version of raw text
                    Debug.WriteLine($"[ConclusionDisplay] Reportified=true only, returning _conclusionText length={result?.Length ?? 0}");
                }
                // PRIORITY 3: Only ProofreadMode is ON
                // Show proofread text with placeholders as-is (not reportified)
                else if (ProofreadMode && !string.IsNullOrWhiteSpace(_conclusionProofread))
                {
                    result = ApplyProofreadPlaceholders(_conclusionProofread);
                    Debug.WriteLine($"[ConclusionDisplay] ProofreadMode=true only, returning _conclusionProofread+placeholders length={result?.Length ?? 0}");
                }
                // PRIORITY 4: Both OFF
                // Show raw text
                else
                {
                    result = _conclusionText;
                    Debug.WriteLine($"[ConclusionDisplay] Both OFF, returning _conclusionText length={result?.Length ?? 0}");
                }
                return result;
            }
        }
        
        private bool _suppressAutoToggle; // prevents recursive toggling while programmatically changing text
        private bool _isInitialized; // prevents updates during initialization

        private void ToggleReportified(bool value)
        {
            Debug.WriteLine($"[Editor.ToggleReportified] START: applying transformations for reportified={value}");
            
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
            
            // CRITICAL FIX: Notify the raw editable properties so top grid textboxes update
            OnPropertyChanged(nameof(RawFindingsTextEditable));
            OnPropertyChanged(nameof(RawConclusionTextEditable));
            
            Debug.WriteLine($"[Editor.ToggleReportified] END: transformations applied");
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
                    
                    // CRITICAL FIX: Always update _rawFindings when not reportified
                    // This ensures RawFindingsTextEditable always returns current value
                    if (!_reportified) _rawFindings = value;
                    
                    if (SetProperty(ref _findingsText, value)) 
                    {
                        UpdateCurrentReportJson();
                        // Notify display property since it depends on this value
                        OnPropertyChanged(nameof(FindingsDisplay));
                        // CRITICAL FIX: Also notify the raw editable property
                        OnPropertyChanged(nameof(RawFindingsTextEditable));
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
                    
                    // CRITICAL FIX: Always update _rawConclusion when not reportified
                    // This ensures RawConclusionTextEditable always returns current value
                    if (!_reportified) _rawConclusion = value;
                    
                    if (SetProperty(ref _conclusionText, value))
                    {
                        UpdateCurrentReportJson();
                        // Notify display property since it depends on this value
                        OnPropertyChanged(nameof(ConclusionDisplay));
                        // CRITICAL FIX: Also notify the raw editable property
                        OnPropertyChanged(nameof(RawConclusionTextEditable));
                    }
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
                    // REPORTED REPORT: Populated ONLY by GetReportedReport module (read-only)
                    header_and_findings = _reportedHeaderAndFindings,
                    final_conclusion = _reportedFinalConclusion,
                    
                    // CURRENT EDITABLE REPORT: Bound to Findings/Conclusion editors (two-way)
                    // CRITICAL FIX: Always save RAW (unreportified) values
                    findings = _reportified ? _rawFindings : (FindingsText ?? string.Empty),
                    conclusion = _reportified ? _rawConclusion : (ConclusionText ?? string.Empty),
                    
                    // Preorder findings (saved via Save Preorder button)
                    findings_preorder = _findingsPreorder,
                    
                    // Metadata and other fields
                    study_remark = _studyRemark,
                    patient_remark = _patientRemark,
                    report_radiologist = _reportRadiologist,
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
                
                // Read reported report fields (only from JSON, not bound to editors)
                string newHeaderAndFindings = root.TryGetProperty("header_and_findings", out var hfEl) && hfEl.ValueKind == JsonValueKind.String ? hfEl.GetString() ?? string.Empty : string.Empty;
                string newFinalConclusion = root.TryGetProperty("final_conclusion", out var fcEl) && fcEl.ValueKind == JsonValueKind.String ? fcEl.GetString() ?? string.Empty : string.Empty;
                
                // Read current editable report fields (bound to Findings/Conclusion editors)
                string newFindings = root.TryGetProperty("findings", out var fEl) && fEl.ValueKind == JsonValueKind.String ? fEl.GetString() ?? string.Empty : string.Empty;
                string newConclusion = root.TryGetProperty("conclusion", out var cEl) && cEl.ValueKind == JsonValueKind.String ? cEl.GetString() ?? string.Empty : string.Empty;
                
                string newStudyRemark = root.TryGetProperty("study_remark", out var sEl) ? (sEl.GetString() ?? string.Empty) : string.Empty;
                string newChiefComplaint = root.TryGetProperty("chief_complaint", out var ccEl) ? (ccEl.GetString() ?? string.Empty) : string.Empty;
                string newPatientHistory = root.TryGetProperty("patient_history", out var phEl) ? (phEl.GetString() ?? string.Empty) : string.Empty;
                string newStudyTechniques = root.TryGetProperty("study_techniques", out var stEl) ? (stEl.GetString() ?? string.Empty) : string.Empty;
                string newComparison = root.TryGetProperty("comparison", out var compEl) ? (compEl.GetString() ?? string.Empty) : string.Empty;
                string newPatientRemark = root.TryGetProperty("patient_remark", out var pEl) ? (pEl.GetString() ?? string.Empty) : string.Empty;
                string newReportRadiologist = root.TryGetProperty("report_radiologist", out var rrEl) ? (rrEl.GetString() ?? string.Empty) : string.Empty;
                string newFindingsPreorder = root.TryGetProperty("findings_preorder", out var fpoEl) ? (fpoEl.GetString() ?? string.Empty) : string.Empty;
                string newChiefComplaintPf = root.TryGetProperty("chief_complaint_proofread", out var ccpEl) ? (ccpEl.GetString() ?? string.Empty) : string.Empty;
                string newPatientHistoryPf = root.TryGetProperty("patient_history_proofread", out var phpEl) ? (phpEl.GetString() ?? string.Empty) : string.Empty;
                string newStudyTechniquesPf = root.TryGetProperty("study_techniques_proofread", out var stpEl) ? (stpEl.GetString() ?? string.Empty) : string.Empty;
                string newComparisonPf = root.TryGetProperty("comparison_proofread", out var cpEl) ? (cpEl.GetString() ?? string.Empty) : string.Empty;
                string newFindingsPf = root.TryGetProperty("findings_proofread", out var fpEl) ? (fpEl.GetString() ?? string.Empty) : string.Empty;
                string newConclusionPf = root.TryGetProperty("conclusion_proofread", out var clpEl) ? (clpEl.GetString() ?? string.Empty) : string.Empty;
                _updatingFromJson = true;
                
                // Update reported report fields (not bound to editors)
                _reportedHeaderAndFindings = newHeaderAndFindings; OnPropertyChanged(nameof(ReportedHeaderAndFindings));
                _reportedFinalConclusion = newFinalConclusion; OnPropertyChanged(nameof(ReportedFinalConclusion));
                
                // Update current editable report fields (keep raw updated for reportify toggling)
                _rawFindings = newFindings; 
                _rawConclusion = newConclusion;
                
                StudyRemark = newStudyRemark; // round-trippable
                PatientRemark = newPatientRemark; // now round-trippable as well
                ReportRadiologist = newReportRadiologist; // round-trippable
                _findingsPreorder = newFindingsPreorder; OnPropertyChanged(nameof(FindingsPreorder)); // round-trippable
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
                
                // NEW: Notify all computed display properties when JSON changes
                OnPropertyChanged(nameof(ChiefComplaintDisplay));
                OnPropertyChanged(nameof(PatientHistoryDisplay));
                OnPropertyChanged(nameof(StudyTechniquesDisplay));
                OnPropertyChanged(nameof(ComparisonDisplay));
                OnPropertyChanged(nameof(HeaderDisplay));
                
                // Recompute HeaderText from component fields even during JSON updates
                UpdateFormattedHeader();
                
                // Update Findings/Conclusion editors from JSON
                if (!_reportified)
                {
                    if (FindingsText != newFindings) _findingsText = newFindings; // assign backing fields directly to avoid auto toggle
                    if (ConclusionText != newConclusion) _conclusionText = newConclusion;
                    OnPropertyChanged(nameof(FindingsText));
                    OnPropertyChanged(nameof(ConclusionText));
                    // CRITICAL FIX: Also notify raw editable properties for top grid textboxes
                    OnPropertyChanged(nameof(RawFindingsTextEditable));
                    OnPropertyChanged(nameof(RawConclusionTextEditable));
                }
                else
                {
                    // If reportified, apply transformation to the loaded values
                    FindingsText = ApplyReportifyBlock(newFindings, false);
                    ConclusionText = ApplyReportifyConclusion(newConclusion);
                    // Raw editable properties are already notified via FindingsText/ConclusionText setters
                }
            }
            catch { }
            finally { _updatingFromJson = false; }
        }

        /// <summary>
        /// Applies placeholder replacements for reportify default values in proofread text.
        /// Replaces {DDx} with DefaultDifferentialDiagnosis, {arrow} with DefaultArrow, {bullet} with DefaultDetailingPrefix.
        /// </summary>
        private string ApplyProofreadPlaceholders(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;
            
            // Get defaults from reportify settings JSON
            string ddx = "DDx:";  // default fallback
            string arrow = "-->";  // default fallback
            string bullet = "-";  // default fallback
            
            try
            {
                if (!string.IsNullOrWhiteSpace(_tenant?.ReportifySettingsJson))
                {
                    using var doc = JsonDocument.Parse(_tenant.ReportifySettingsJson);
                    var root = doc.RootElement;
                    
                    if (root.TryGetProperty("defaults", out var defaults))
                    {
                        if (defaults.TryGetProperty("differential_diagnosis", out var ddxEl) && ddxEl.ValueKind == JsonValueKind.String)
                            ddx = ddxEl.GetString() ?? ddx;
                        
                        if (defaults.TryGetProperty("arrow", out var arrowEl) && arrowEl.ValueKind == JsonValueKind.String)
                            arrow = arrowEl.GetString() ?? arrow;
                        
                        if (defaults.TryGetProperty("detailing_prefix", out var bulletEl) && bulletEl.ValueKind == JsonValueKind.String)
                            bullet = bulletEl.GetString() ?? bullet;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ProofreadPlaceholders] JSON parse error: {ex.Message}");
                // Fall through to use defaults
            }
            
            // Apply replacements (case-insensitive)
            text = Regex.Replace(text, @"\{DDx\}", ddx, RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"\{arrow\}", arrow, RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"\{bullet\}", bullet, RegexOptions.IgnoreCase);
            
            return text;
        }

        // Utility access for exporting raw or transformed sections
        public (string header, string findings, string conclusion) GetDereportifiedSections()
        {
            string h = Reportified ? _rawHeader : HeaderText;
            string f = Reportified ? _rawFindings : FindingsText;
            string c = Reportified ? _rawConclusion : ConclusionText;
            return (h, f, c);
        }
        
        // NEW: Public accessors for raw (unreportified) values - use these for database saves and PACS sends
        public string RawFindingsText => _reportified ? _rawFindings : (_findingsText ?? string.Empty);
        public string RawConclusionText => _reportified ? _rawConclusion : (_conclusionText ?? string.Empty);
        
        // NEW: Writable properties for top grid textboxes (not affected by Reportify toggle)
        // These always read/write the raw values, regardless of Reportified state
        public string RawFindingsTextEditable
        {
            get => _reportified ? _rawFindings : (_findingsText ?? string.Empty);
            set
            {
                if (_reportified)
                {
                    // When reportified, update the raw backing value
                    if (_rawFindings != value)
                    {
                        _rawFindings = value;
                        OnPropertyChanged(nameof(RawFindingsTextEditable));
                        
                        // CRITICAL FIX: Also update the center editor with the reportified version
                        _suppressAutoToggle = true;
                        try
                        {
                            _findingsText = ApplyReportifyBlock(value, false);
                            OnPropertyChanged(nameof(FindingsText));
                            OnPropertyChanged(nameof(FindingsDisplay));
                        }
                        finally
                        {
                            _suppressAutoToggle = false;
                        }
                        
                        UpdateCurrentReportJson();
                    }
                }
                else
                {
                    // When not reportified, update FindingsText normally
                    FindingsText = value;
                }
            }
        }

        public string RawConclusionTextEditable
        {
            get => _reportified ? _rawConclusion : (_conclusionText ?? string.Empty);
            set
            {
                if (_reportified)
                {
                    // When reportified, update the raw backing value
                    if (_rawConclusion != value)
                    {
                        _rawConclusion = value;
                        OnPropertyChanged(nameof(RawConclusionTextEditable));
                        
                        // CRITICAL FIX: Also update the center editor with the reportified version
                        _suppressAutoToggle = true;
                        try
                        {
                            _conclusionText = ApplyReportifyConclusion(value);
                            OnPropertyChanged(nameof(ConclusionText));
                            OnPropertyChanged(nameof(ConclusionDisplay));
                        }
                        finally
                        {
                            _suppressAutoToggle = false;
                        }
                        
                        UpdateCurrentReportJson();
                    }
                }
                else
                {
                    // When not reportified, update ConclusionText normally
                    ConclusionText = value;
                }
            }
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
