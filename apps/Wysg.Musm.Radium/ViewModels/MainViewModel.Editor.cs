using System;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Linq;

namespace Wysg.Musm.Radium.ViewModels
{
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
                // CRITICAL FIX: Always raise PropertyChanged event first to ensure UI synchronization
                // This is essential for automation modules that set Reportified=true
                bool actualChanged = (_reportified != value);
                
                // Update backing field
                _reportified = value;
                
                // Always notify, even if value didn't change (ensures UI syncs after automation)
                OnPropertyChanged(nameof(Reportified));
                
                // Notify display properties since reportified state affects them
                OnPropertyChanged(nameof(FindingsDisplay));
                OnPropertyChanged(nameof(ConclusionDisplay));
                OnPropertyChanged(nameof(HeaderDisplay)); // NEW: Also notify header display
                
                // Only apply transformations if value actually changed
                if (actualChanged)
                {
                    ToggleReportified(value);
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
                if (SetProperty(ref _studyRemark, value ?? string.Empty)) 
                {
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
                if (SetProperty(ref _patientRemark, value ?? string.Empty)) 
                {
                    UpdateCurrentReportJson(); 
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
                if (SetProperty(ref _chiefComplaint, value ?? string.Empty)) 
                { 
                    SafeUpdateJson(); 
                    SafeUpdateHeader();
                    // NEW: Notify computed display property
                    OnPropertyChanged(nameof(HeaderDisplay));
                } 
            } 
        }

        // Proofread fields for current report (root JSON)
        // NOTE: All header component proofread fields removed as per user request
        // Only Findings and Conclusion proofread fields remain
        
        private string _findingsProofread = string.Empty; 
        public string FindingsProofread 
        { 
            get => NormalizeForWpf(_findingsProofread); 
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
            get => NormalizeForWpf(_conclusionProofread); 
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
                if (SetProperty(ref _patientHistory, value ?? string.Empty)) 
                { 
                    SafeUpdateJson(); 
                    SafeUpdateHeader();
                    // NEW: Notify computed display property
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
                if (SetProperty(ref _studyTechniques, value ?? string.Empty)) 
                { 
                    SafeUpdateJson(); 
                    SafeUpdateHeader();
                    // NEW: Notify computed display property
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
                if (SetProperty(ref _comparison, value ?? string.Empty)) 
                { 
                    SafeUpdateJson(); 
                    SafeUpdateHeader();
                    // NEW: Notify computed display property
                    OnPropertyChanged(nameof(HeaderDisplay));
                } 
            } 
        }

        // NEW: Computed HeaderDisplay property that formats header from raw component fields
        // This mirrors the logic in UpdateFormattedHeader but uses raw fields directly
        public string HeaderDisplay
        {
            get
            {
                var lines = new System.Collections.Generic.List<string>();
                
                // Use raw fields for all header components (no proofread versions)
                var chiefComplaintDisplay = _chiefComplaint;
                var patientHistoryDisplay = _patientHistory;
                var studyTechniquesDisplay = _studyTechniques;
                var comparisonDisplay = _comparison;
                
                bool hasClinicalInfo = !string.IsNullOrWhiteSpace(chiefComplaintDisplay) || !string.IsNullOrWhiteSpace(patientHistoryDisplay);
                bool hasTechniques = !string.IsNullOrWhiteSpace(studyTechniquesDisplay);
                bool hasAnyHeaderContent = hasClinicalInfo || hasTechniques;
                
                // Clinical information logic
                if (hasClinicalInfo)
                {
                    if (string.IsNullOrWhiteSpace(chiefComplaintDisplay) && !string.IsNullOrWhiteSpace(patientHistoryDisplay))
                    {
                        // Chief complaint is empty but history exists -> show "Clinical information: NA"
                        lines.Add("Clinical information: N/A");
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
                    // Comparison is empty but we have other header content -> show "Comparison: N/A"
                    lines.Add("Comparison: N/A");
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
                }
                // PRIORITY 2: Only Reportified is ON
                // Show reportified version of RAW text
                else if (_reportified)
                {
                    result = _findingsText; // This already contains the reportified version of raw text
                }
                // PRIORITY 3: Only ProofreadMode is ON
                // Show proofread text with placeholders as-is (not reportified)
                else if (ProofreadMode && !string.IsNullOrWhiteSpace(_findingsProofread))
                {
                    result = ApplyProofreadPlaceholders(_findingsProofread);
                }
                // PRIORITY 4: Both OFF
                // Show raw text
                else
                {
                    result = _findingsText;
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
                }
                // PRIORITY 2: Only Reportified is ON
                // Show reportified version of RAW text
                else if (_reportified)
                {
                    result = _conclusionText; // This already contains the reportified version of raw text
                }
                // PRIORITY 3: Only ProofreadMode is ON
                // Show proofread text with placeholders as-is (not reportified)
                else if (ProofreadMode && !string.IsNullOrWhiteSpace(_conclusionProofread))
                {
                    result = ApplyProofreadPlaceholders(_conclusionProofread);
                }
                // PRIORITY 4: Both OFF
                // Show raw text
                else
                {
                    result = _conclusionText;
                }
                return result;
            }
        }
        
        private bool _suppressAutoToggle; // prevents recursive toggling while programmatically changing text
        private bool _isInitialized; // prevents updates during initialization

        private void ToggleReportified(bool value)
        {
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
                    // CRITICAL FIX: Always allow clearing (empty string) regardless of reportified state
                    // This ensures ClearCurrentFields can clear the editor even when reportified is ON
                    bool isClearing = string.IsNullOrEmpty(value);
                    
                    // NEW: If reportified and not suppressing and NOT clearing, cancel change and dereportify
                    if (_reportified && !_suppressAutoToggle && !isClearing)
                    {
                        AutoUnreportifyOnEdit();
                        return; // Cancel the text change
                    }
                    
                    // CRITICAL FIX: Always update _rawFindings when not reportified OR when clearing
                    // When clearing, update both raw and display values
                    if (!_reportified || isClearing)
                    {
                        _rawFindings = value;
                    }
                    
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
                    // CRITICAL FIX: Always allow clearing (empty string) regardless of reportified state
                    // This ensures ClearCurrentFields can clear the editor even when reportified is ON
                    bool isClearing = string.IsNullOrEmpty(value);
                    
                    // NEW: If reportified and not suppressing and NOT clearing, cancel change and dereportify
                    if (_reportified && !_suppressAutoToggle && !isClearing)
                    {
                        AutoUnreportifyOnEdit();
                        return; // Cancel the text change
                    }
                    
                    // CRITICAL FIX: Always update _rawConclusion when not reportified OR when clearing
                    // When clearing, update both raw and display values
                    if (!_reportified || isClearing)
                    {
                        _rawConclusion = value;
                    }
                    
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
                    // NOTE: All header component proofread fields removed (chief_complaint_proofread, patient_history_proofread, study_techniques_proofread, comparison_proofread)
                    // Only Findings and Conclusion proofread fields remain
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
                // NOTE: All header component proofread fields removed as per user request
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
                // Proofread fields (all header component proofread fields removed)
                _findingsProofread = newFindingsPf; OnPropertyChanged(nameof(FindingsProofread));
                _conclusionProofread = newConclusionPf; OnPropertyChanged(nameof(ConclusionProofread));
                
                // NEW: Notify HeaderDisplay when JSON changes
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

        // NEW: Get sections with proofread fallback for phrase extraction
        // ALWAYS returns proofread versions when available, regardless of ProofreadMode toggle
        public (string header, string findings, string conclusion) GetProofreadOrRawSections()
        {
            string header, findings, conclusion;
            
            // Header: No proofread versions for any header components
            // Always use raw values from GetDereportifiedSections
            var (h, _, _) = GetDereportifiedSections();
            header = h;
            
            // Findings: ALWAYS prefer proofread if available
            if (!string.IsNullOrWhiteSpace(_findingsProofread))
            {
                findings = ApplyProofreadPlaceholders(_findingsProofread);
            }
            else
            {
                var (_, f, _) = GetDereportifiedSections();
                findings = f;
            }
            
            // Conclusion: ALWAYS prefer proofread if available
            if (!string.IsNullOrWhiteSpace(_conclusionProofread))
            {
                conclusion = ApplyProofreadPlaceholders(_conclusionProofread);
            }
            else
            {
                var (_, _, c) = GetDereportifiedSections();
                conclusion = c;
            }
            
            return (header, findings, conclusion);
        }
        
        // NEW: Public accessors for raw (unreportified) values - use these for database saves and PACS sends
        public string RawFindingsText => _reportified ? _rawFindings : (_findingsText ?? string.Empty);
        public string RawConclusionText => _reportified ? _rawConclusion : (_conclusionText ?? string.Empty);
        
        // NEW: Writable properties for top grid textboxes (not affected by Reportify toggle)
        // These always read/write the raw values, regardless of Reportified state
        public string RawFindingsTextEditable
        {
            get => NormalizeForWpf(_reportified ? _rawFindings : (_findingsText ?? string.Empty));
            set
            {
                if (_reportified)
                {
                    // When reportified, update the raw backing value
                    if (_rawFindings != value)
                    {
                        _rawFindings = value;
                        OnPropertyChanged(nameof(RawFindingsTextEditable));
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
            get => NormalizeForWpf(_reportified ? _rawConclusion : (_conclusionText ?? string.Empty));
            set
            {
                if (_reportified)
                {
                    if (_rawConclusion != value)
                    {
                        _rawConclusion = value;
                        OnPropertyChanged(nameof(RawConclusionTextEditable));
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
                    ConclusionText = value;
                }
            }
        }
        
        // Normalize newlines for WPF TextBox display (use CRLF) without mutating stored values
        // NOTE: TextBox in WPF renders best with CRLF (\r\n). Proofread/raw fields can contain LF-only or CRLF
        // depending on source (API, copy-paste). To avoid visual concatenation in some flows and keep consistent
        // caret behavior, getters that are bound to TextBoxes return a normalized CRLF view while setters keep
        // storing values unmodified. This is a UI-only normalization.
        private static string NormalizeForWpf(string? s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            // Unify to LF then map to CRLF
            var t = s.Replace("\r\n", "\n");
            t = t.Replace("\r", "\n");
            t = t.Replace("\n", "\r\n");
            return t;
        }
        
        // Safe wrappers that check initialization state (removed excessive logging)
        private void SafeUpdateJson()
        {
            if (!_isInitialized) return;
            try { UpdateCurrentReportJson(); } 
            catch { }
        }

        private void SafeUpdateHeader()
        {
            if (!_isInitialized) return;
            
            // NEW: Check if header updates should be skipped for XR modality
            if (ShouldSkipHeaderUpdateForXR())
            {
                Debug.WriteLine("[Editor] SafeUpdateHeader: Skipped for XR modality (setting enabled)");
                return;
            }
            
            try { UpdateFormattedHeader(); } 
            catch { }
        }

        // Real-time header formatting based on component fields
        private void UpdateFormattedHeader()
        {
            try
            {
                // If XR modality excluded, keep old behavior
                if (ShouldSkipHeaderUpdateForXR())
                {
                    var lines = new System.Collections.Generic.List<string>();
                    bool hasClinicalInfo = !string.IsNullOrWhiteSpace(_chiefComplaint) || !string.IsNullOrWhiteSpace(_patientHistory);
                    bool hasTechniques = !string.IsNullOrWhiteSpace(_studyTechniques);
                    bool hasAnyHeaderContent = hasClinicalInfo || hasTechniques;
                    if (hasClinicalInfo)
                    {
                        if (string.IsNullOrWhiteSpace(_chiefComplaint) && !string.IsNullOrWhiteSpace(_patientHistory))
                            lines.Add("Clinical information: N/A");
                        else if (!string.IsNullOrWhiteSpace(_chiefComplaint))
                            lines.Add($"Clinical information: {_chiefComplaint}");
                        if (!string.IsNullOrWhiteSpace(_patientHistory))
                        {
                            var historyLines = _patientHistory.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n', StringSplitOptions.RemoveEmptyEntries);
                            foreach (var line in historyLines)
                            {
                                var trimmed = line.Trim();
                                if (!string.IsNullOrWhiteSpace(trimmed)) lines.Add($"- {trimmed}");
                            }
                        }
                    }
                    if (hasTechniques) lines.Add($"Techniques: {_studyTechniques}");
                    if (!string.IsNullOrWhiteSpace(_comparison)) lines.Add($"Comparison: {_comparison}");
                    else if (hasAnyHeaderContent) lines.Add("Comparison: N/A");
                    var formattedLegacy = string.Join("\n", lines).Trim();
                    HeaderText = formattedLegacy;
                    return;
                }

                // Use user-configured header template if available
                var template = _localSettings?.HeaderFormatTemplate;
                if (string.IsNullOrWhiteSpace(template))
                {
                    template = "Clinical information: {Chief Complaint}\n- {Patient History Lines}\nTechniques: {Techniques}\nComparison: {Comparison}";
                }
                var formatted = BuildHeaderFromTemplate(template);
                HeaderText = formatted;
            }
            catch
            {
                // Fall back silently to legacy behavior if template application fails
                var lines = new System.Collections.Generic.List<string>();
                bool hasClinicalInfo = !string.IsNullOrWhiteSpace(_chiefComplaint) || !string.IsNullOrWhiteSpace(_patientHistory);
                bool hasTechniques = !string.IsNullOrWhiteSpace(_studyTechniques);
                bool hasAnyHeaderContent = hasClinicalInfo || hasTechniques;
                if (hasClinicalInfo)
                {
                    if (string.IsNullOrWhiteSpace(_chiefComplaint) && !string.IsNullOrWhiteSpace(_patientHistory))
                        lines.Add("Clinical information: N/A");
                    else if (!string.IsNullOrWhiteSpace(_chiefComplaint))
                        lines.Add($"Clinical information: {_chiefComplaint}");
                    if (!string.IsNullOrWhiteSpace(_patientHistory))
                    {
                        var historyLines = _patientHistory.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n', StringSplitOptions.RemoveEmptyEntries);
                        foreach (var line in historyLines)
                        {
                            var trimmed = line.Trim();
                            if (!string.IsNullOrWhiteSpace(trimmed)) lines.Add($"- {trimmed}");
                        }
                    }
                }
                if (hasTechniques) lines.Add($"Techniques: {_studyTechniques}");
                if (!string.IsNullOrWhiteSpace(_comparison)) lines.Add($"Comparison: {_comparison}");
                else if (hasAnyHeaderContent) lines.Add("Comparison: N/A");
                HeaderText = string.Join("\n", lines).Trim();
            }
        }
        
        /// <summary>
        /// Returns true if current study modality is in the ModalitiesNoHeaderUpdate exclusion list.
        /// </summary>
        private bool ShouldSkipHeaderUpdateForXR()
        {
            try
            {
                // Get the comma-separated list of excluded modalities
                var modalitiesNoHeaderUpdate = _localSettings?.ModalitiesNoHeaderUpdate ?? string.Empty;
                
                if (string.IsNullOrWhiteSpace(modalitiesNoHeaderUpdate))
                {
                    return false; // No exclusions, allow header updates
                }
                
                // Parse comma-separated list
                var excludedModalities = modalitiesNoHeaderUpdate
                    .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(m => m.Trim().ToUpperInvariant())
                    .Where(m => !string.IsNullOrEmpty(m))
                    .ToList();
                
                if (excludedModalities.Count == 0)
                {
                    return false; // No valid exclusions, allow header updates
                }
                
                // Check if current study modality is in the exclusion list
                var modality = ExtractModalityFromStudyName(StudyName);
                
                if (string.IsNullOrWhiteSpace(modality))
                {
                    return false; // Cannot determine modality, allow header updates
                }
                
                bool isExcluded = excludedModalities.Contains(modality.ToUpperInvariant());
                
                if (isExcluded)
                {
                    Debug.WriteLine($"[Editor] Modality '{modality}' is in exclusion list, skipping header update");
                }
                
                return isExcluded;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Editor] Error checking modality exclusion: {ex.Message}");
                return false; // On error, allow header updates (safe fallback)
            }
        }
        
        private static string ExtractModalityFromStudyName(string? studyName)
        {
            if (string.IsNullOrWhiteSpace(studyName))
                return string.Empty;
            
            var parts = studyName.Trim().Split(new[] { ' ', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
            return parts.Length > 0 ? parts[0].Trim() : string.Empty;
        }
        
        /// <summary>
        /// Called when Header format template is changed and settings window is saved.
        /// If header has content, rebuild it using the new template and current component fields.
        /// </summary>
        public void OnHeaderFormatTemplateChanged()
        {
            try
            {
                // Read template from local settings; use default if empty
                var template = _localSettings?.HeaderFormatTemplate ?? string.Empty;
                if (string.IsNullOrWhiteSpace(template))
                {
                    template = "Clinical information: {Chief Complaint}\n- {Patient History Lines}\nTechniques: {Techniques}\nComparison: {Comparison}";
                }

                // Build header with placeholders
                string formatted = BuildHeaderFromTemplate(template);

                // If existing header has content, refresh it to match changed format
                if (!string.IsNullOrWhiteSpace(_headerText))
                {
                    _suppressAutoToggle = true;
                    try
                    {
                        HeaderText = formatted;
                    }
                    finally { _suppressAutoToggle = false; }
                }
                else
                {
                    // Even if empty, ensure computed HeaderDisplay reflects new template indirectly
                    HeaderText = formatted;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[HeaderFormat] Error applying template: {ex.Message}");
            }
        }

        private string BuildHeaderFromTemplate(string template)
        {
            var chief = _chiefComplaint ?? string.Empty;
            var history = (_patientHistory ?? string.Empty).Replace("\r\n", "\n").Replace("\r", "\n");
            string historyLines = string.IsNullOrWhiteSpace(history)
                ? string.Empty
                : string.Join("\n", history.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim()));
            var techniques = _studyTechniques ?? string.Empty;
            var comparison = _comparison ?? string.Empty;
            var chiefDisplay = chief;

            var lines = template.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
            var outputLines = new System.Collections.Generic.List<string>(lines.Length);
            
            foreach (var rawLine in lines)
            {
                // If line is completely empty (no text at all), preserve it
                if (string.IsNullOrWhiteSpace(rawLine) && rawLine.Length == 0)
                {
                    outputLines.Add(string.Empty);
                    continue;
                }
                
                var line = rawLine;
                var matches = Regex.Matches(line, @"\{([^}]+)\}");
                var distinct = matches.Cast<Match>().Select(m => m.Groups[1].Value).Distinct().ToList();
                
                string GetValue(string name) => name switch
                {
                    "Chief Complaint" => chiefDisplay,
                    "Patient History Lines" => historyLines,
                    "Techniques" => techniques,
                    "Comparison" => comparison,
                    _ => string.Empty
                };
                
                // If the line contains exactly one distinct placeholder and its mapped value is empty or whitespace, skip the line
                if (distinct.Count == 1)
                {
                    var onlyVal = GetValue(distinct[0]);
                    if (string.IsNullOrWhiteSpace(onlyVal)) continue;
                }
                
                // Perform replacements
                foreach (var name in distinct)
                {
                    var val = GetValue(name);
                    line = line.Replace("{" + name + "}", val ?? string.Empty);
                }
                
                // After replacement, if line becomes empty due to only having placeholders, skip it
                // But if it had other text, keep it even if trimmed result is empty
                var trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed) && distinct.Count > 0)
                {
                    continue;
                }
                
                outputLines.Add(trimmed);
            }
            
            return string.Join("\n", outputLines).Trim();
        }
    }
}
