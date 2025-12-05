using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace Wysg.Musm.Radium.ViewModels
{
    /// <summary>
    /// Partial: JSON synchronization logic for Previous Studies.
    /// </summary>
    public partial class MainViewModel
    {
        private string _previousReportJson = "{}";
        
        public string PreviousReportJson
        {
            get => _previousReportJson;
            set
            {
                var changed = SetProperty(ref _previousReportJson, value);
                Debug.WriteLine($"[PrevJson] Set PreviousReportJson changed={changed} len={value?.Length}");
                if (!changed) OnPropertyChanged(nameof(PreviousReportJson));
                if (_updatingPrevFromEditors) return;
                ApplyJsonToPrevious(value);
            }
        }
        
        private bool _updatingPrevFromEditors;
        private bool _updatingPrevFromJson;
        
        private void UpdatePreviousReportJson()
        {
            var tab = SelectedPreviousStudy;
            try
            {
                if (tab == null)
                {
                    // No selected tab yet: use cache fields so txtPrevJson mirrors user typing
                    var obj = new
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
                        // NOTE: All header component proofread fields removed as per user request
                        findings_proofread = _prevFindingsProofreadCache,
                        conclusion_proofread = _prevConclusionProofreadCache
                    };
                    var json = JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true });
                    _updatingPrevFromEditors = true;
                    PreviousReportJson = json;
                    OnPropertyChanged(nameof(PreviousReportJson));
                    Debug.WriteLine($"[PrevJson] Update (cache only) htLen={_prevHeaderTempCache?.Length} hfLen={_prevHeaderAndFindingsCache?.Length} fcLen={_prevFinalConclusionCache?.Length}");
                }
                else
                {
                    // Use raw JSON from database as base
                    string baseJson = tab.RawJson;
                    
                    // If no raw JSON available, create minimal structure
                    if (string.IsNullOrWhiteSpace(baseJson) || baseJson == "{}")
                    {
                        baseJson = JsonSerializer.Serialize(new
                        {
                            header_and_findings = tab.Findings ?? string.Empty,
                            final_conclusion = tab.Conclusion ?? string.Empty
                        }, new JsonSerializerOptions { WriteIndented = true });
                    }
                    
                    // Parse the raw JSON
                    using var doc = JsonDocument.Parse(baseJson);
                    using var stream = new MemoryStream();
                    using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });
                    
                    writer.WriteStartObject();
                    
                    // CRITICAL FIX: Fields to exclude from copy (will be rewritten with current values from tab properties)
                    var excludedFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                    {
                        "PrevReport",     // Will rebuild this
                        "header_temp",    // Will rewrite with computed split value
                        "findings",       // Will rewrite with computed split value
                        "conclusion",     // Will rewrite with computed split value
                        // CRITICAL FIX: Exclude proofread fields so they're written from tab properties, not stale RawJson
                        // NOTE: All header component proofread fields removed as per user request
                        "findings_proofread",
                        "conclusion_proofread"
                    };
                    
                    // Copy all existing properties from raw JSON (except excluded fields)
                    foreach (var prop in doc.RootElement.EnumerateObject())
                    {
                        if (excludedFields.Contains(prop.Name)) continue;
                        
                        // Write the property as-is
                        prop.WriteTo(writer);
                    }
                    
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
                    
                    // Update tab split outputs
                    if (tab.HeaderTemp != splitHeader) tab.HeaderTemp = splitHeader;
                    if (tab.FindingsOut != splitFindings) tab.FindingsOut = splitFindings;
                    if (tab.ConclusionOut != splitConclusion) tab.ConclusionOut = splitConclusion;
                    
                    // Write the computed split output fields (these replace any existing values from DB)
                    writer.WriteString("header_temp", tab.HeaderTemp ?? string.Empty);
                    writer.WriteString("findings", tab.FindingsOut ?? string.Empty);
                    writer.WriteString("conclusion", tab.ConclusionOut ?? string.Empty);
                    
                    // CRITICAL FIX: Write proofread fields from tab properties (current values), not from stale RawJson
                    // NOTE: All header component proofread fields removed as per user request
                    writer.WriteString("findings_proofread", tab.FindingsProofread ?? string.Empty);
                    writer.WriteString("conclusion_proofread", tab.ConclusionProofread ?? string.Empty);
            
            // Add PrevReport section with split ranges
            writer.WritePropertyName("PrevReport");
            writer.WriteStartObject();
            writer.WriteNumber("header_and_findings_header_splitter_from", tab.HfHeaderFrom ?? 0);
            writer.WriteNumber("header_and_findings_header_splitter_to", tab.HfHeaderTo ?? 0);
            writer.WriteNumber("header_and_findings_conclusion_splitter_from", tab.HfConclusionFrom ?? hf.Length);
            writer.WriteNumber("header_and_findings_conclusion_splitter_to", tab.HfConclusionTo ?? hf.Length);
            writer.WriteNumber("final_conclusion_header_splitter_from", tab.FcHeaderFrom ?? 0);
            writer.WriteNumber("final_conclusion_header_splitter_to", tab.FcHeaderTo ?? 0);
            writer.WriteNumber("final_conclusion_findings_splitter_from", tab.FcFindingsFrom ?? 0);
            writer.WriteNumber("final_conclusion_findings_splitter_to", tab.FcFindingsTo ?? 0);
            writer.WriteEndObject();
            
            writer.WriteEndObject();
            writer.Flush();
            
            var jsonBytes = stream.ToArray();
            var json = System.Text.Encoding.UTF8.GetString(jsonBytes);
            
            _updatingPrevFromEditors = true;
            PreviousReportJson = json;
            OnPropertyChanged(nameof(PreviousReportJson));
            Debug.WriteLine($"[PrevJson] Update (tab, from raw DB JSON) htLen={tab.HeaderTemp?.Length} hfLen={tab.Findings?.Length} fcLen={tab.Conclusion?.Length}");
        }
    }
    catch (Exception ex)
    {
        Debug.WriteLine("[PrevJson] Update error: " + ex.Message);
    }
    finally
    {
        _updatingPrevFromEditors = false;
    }
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
                
                if (tab == null)
                {
                    // Apply to caches when no tab selected yet
                    ApplyJsonToCaches(root);
                }
                else
                {
                    // Apply to selected tab
                    ApplyPreviousJsonFields(tab, root);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[PrevJson] Apply error: " + ex.Message);
            }
            finally
            {
                _updatingPrevFromJson = false;
            }
        }
        
        private void ApplyJsonToCaches(JsonElement root)
        {
            _updatingPrevFromJson = true;
            
            string newHeaderTemp = root.TryGetProperty("header_temp", out var htEl) ? (htEl.GetString() ?? string.Empty) : string.Empty;
            string newHeaderAndFindings = root.TryGetProperty("header_and_findings", out var hfEl) ? (hfEl.GetString() ?? string.Empty) : string.Empty;
            string newFinalConclusion = root.TryGetProperty("final_conclusion", out var fcEl) ? (fcEl.GetString() ?? string.Empty) : string.Empty;
            string newFindingsOut = root.TryGetProperty("findings", out var fEl2) ? (fEl2.GetString() ?? string.Empty) : string.Empty;
            string newConclusionOut = root.TryGetProperty("conclusion", out var cEl2) ? (cEl2.GetString() ?? string.Empty) : string.Empty;
            string newStudyRemark = root.TryGetProperty("study_remark", out var sEl) ? (sEl.GetString() ?? string.Empty) : string.Empty;
            string newPatientRemark = root.TryGetProperty("patient_remark", out var pEl) ? (pEl.GetString() ?? string.Empty) : string.Empty;
            string newChiefComplaint = root.TryGetProperty("chief_complaint", out var ccEl) ? (ccEl.GetString() ?? string.Empty) : string.Empty;
            string newPatientHistory = root.TryGetProperty("patient_history", out var phEl) ? (phEl.GetString() ?? string.Empty) : string.Empty;
            string newStudyTechniques = root.TryGetProperty("study_techniques", out var stEl) ? (stEl.GetString() ?? string.Empty) : string.Empty;
            string newComparison = root.TryGetProperty("comparison", out var compEl) ? (compEl.GetString() ?? string.Empty) : string.Empty;
            // NOTE: All header component proofread fields removed as per user request
            string newFindingsPf = root.TryGetProperty("findings_proofread", out var fpEl) ? (fpEl.GetString() ?? string.Empty) : string.Empty;
            string newConclusionPf = root.TryGetProperty("conclusion_proofread", out var clpEl) ? (clpEl.GetString() ?? string.Empty) : string.Empty;
            
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
            // NOTE: All header component proofread cache fields removed as per user request
            _prevFindingsProofreadCache = newFindingsPf;
            _prevConclusionProofreadCache = newConclusionPf;

            if (changed) UpdatePreviousReportJson();
            else OnPropertyChanged(nameof(PreviousReportJson));
        }
        
        private void ApplyPreviousJsonFields(PreviousStudyTab tab, JsonElement root)
        {
            _updatingPrevFromJson = true;
            
            bool changed = false;
            bool proofreadFieldsChanged = false; // Track if proofread fields changed for real-time editor updates
            
            // Update core report fields
            if (root.TryGetProperty("header_temp", out var htEl) && htEl.ValueKind == JsonValueKind.String)
            {
                string value = htEl.GetString() ?? string.Empty;
                if (tab.HeaderTemp != value) { tab.HeaderTemp = value; changed = true; }
            }
            
            if (root.TryGetProperty("header_and_findings", out var hfEl) && hfEl.ValueKind == JsonValueKind.String)
            {
                string value = hfEl.GetString() ?? string.Empty;
                if (tab.Findings != value) { tab.Findings = value; changed = true; }
            }
            
            if (root.TryGetProperty("final_conclusion", out var fcEl) && fcEl.ValueKind == JsonValueKind.String)
            {
                string value = fcEl.GetString() ?? string.Empty;
                if (tab.Conclusion != value) { tab.Conclusion = value; changed = true; }
            }
            
            if (root.TryGetProperty("findings", out var fEl) && fEl.ValueKind == JsonValueKind.String)
            {
                string value = fEl.GetString() ?? string.Empty;
                if (tab.FindingsOut != value) { tab.FindingsOut = value; changed = true; }
            }
            
            if (root.TryGetProperty("conclusion", out var cEl) && cEl.ValueKind == JsonValueKind.String)
            {
                string value = cEl.GetString() ?? string.Empty;
                if (tab.ConclusionOut != value) { tab.ConclusionOut = value; changed = true; }
            }
            
            // Update metadata fields
            if (root.TryGetProperty("study_remark", out var srEl) && srEl.ValueKind == JsonValueKind.String)
            {
                string value = srEl.GetString() ?? string.Empty;
                if (tab.StudyRemark != value) { tab.StudyRemark = value; changed = true; }
            }
            
            if (root.TryGetProperty("patient_remark", out var prEl) && prEl.ValueKind == JsonValueKind.String)
            {
                string value = prEl.GetString() ?? string.Empty;
                if (tab.PatientRemark != value) { tab.PatientRemark = value; changed = true; }
            }
            
            if (root.TryGetProperty("chief_complaint", out var ccEl) && ccEl.ValueKind == JsonValueKind.String)
            {
                string value = ccEl.GetString() ?? string.Empty;
                if (tab.ChiefComplaint != value) { tab.ChiefComplaint = value; changed = true; }
            }
            
            if (root.TryGetProperty("patient_history", out var phEl) && phEl.ValueKind == JsonValueKind.String)
            {
                string value = phEl.GetString() ?? string.Empty;
                if (tab.PatientHistory != value) { tab.PatientHistory = value; changed = true; }
            }
            
            if (root.TryGetProperty("study_techniques", out var stEl) && stEl.ValueKind == JsonValueKind.String)
            {
                string value = stEl.GetString() ?? string.Empty;
                if (tab.StudyTechniques != value) { tab.StudyTechniques = value; changed = true; }
            }
            
            if (root.TryGetProperty("comparison", out var compEl) && compEl.ValueKind == JsonValueKind.String)
            {
                string value = compEl.GetString() ?? string.Empty;
                if (tab.Comparison != value) { tab.Comparison = value; changed = true; }
            }
            
            // Update proofread fields - CRITICAL: Track changes for real-time editor updates
            // NOTE: All header component proofread fields removed as per user request
            // CRITICAL: Findings proofread - notify editor property for real-time updates
            if (root.TryGetProperty("findings_proofread", out var fpEl) && fpEl.ValueKind == JsonValueKind.String)
            {
                string value = fpEl.GetString() ?? string.Empty;
                if (tab.FindingsProofread != value) 
                { 
                    tab.FindingsProofread = value; 
                    changed = true; 
                    proofreadFieldsChanged = true;
                    Debug.WriteLine($"[PrevJson] findings_proofread updated, length={value.Length}");
                }
            }
            
            // CRITICAL: Conclusion proofread - notify editor property for real-time updates
            if (root.TryGetProperty("conclusion_proofread", out var clpEl) && clpEl.ValueKind == JsonValueKind.String)
            {
                string value = clpEl.GetString() ?? string.Empty;
                if (tab.ConclusionProofread != value) 
                { 
                    tab.ConclusionProofread = value; 
                    changed = true; 
                    proofreadFieldsChanged = true;
                    Debug.WriteLine($"[PrevJson] conclusion_proofread updated, length={value.Length}");
                }
            }
            
            // Update splitter ranges if present in PrevReport section
            if (root.TryGetProperty("PrevReport", out var prObj) && prObj.ValueKind == JsonValueKind.Object)
            {
                int? GetInt(string name)
                {
                    if (prObj.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out var i))
                        return i;
                    return null;
                }
                
                var hfHeaderFrom = GetInt("header_and_findings_header_splitter_from");
                var hfHeaderTo = GetInt("header_and_findings_header_splitter_to");
                var hfConclusionFrom = GetInt("header_and_findings_conclusion_splitter_from");
                var hfConclusionTo = GetInt("header_and_findings_conclusion_splitter_to");
                var fcHeaderFrom = GetInt("final_conclusion_header_splitter_from");
                var fcHeaderTo = GetInt("final_conclusion_header_splitter_to");
                var fcFindingsFrom = GetInt("final_conclusion_findings_splitter_from");
                var fcFindingsTo = GetInt("final_conclusion_findings_splitter_to");
                
                if (hfHeaderFrom.HasValue && tab.HfHeaderFrom != hfHeaderFrom) { tab.HfHeaderFrom = hfHeaderFrom; changed = true; }
                if (hfHeaderTo.HasValue && tab.HfHeaderTo != hfHeaderTo) { tab.HfHeaderTo = hfHeaderTo; changed = true; }
                if (hfConclusionFrom.HasValue && tab.HfConclusionFrom != hfConclusionFrom) { tab.HfConclusionFrom = hfConclusionFrom; changed = true; }
                if (hfConclusionTo.HasValue && tab.HfConclusionTo != hfConclusionTo) { tab.HfConclusionTo = hfConclusionTo; changed = true; }
                if (fcHeaderFrom.HasValue && tab.FcHeaderFrom != fcHeaderFrom) { tab.FcHeaderFrom = fcHeaderFrom; changed = true; }
                if (fcHeaderTo.HasValue && tab.FcHeaderTo != fcHeaderTo) { tab.FcHeaderTo = fcHeaderTo; changed = true; }
                if (fcFindingsFrom.HasValue && tab.FcFindingsFrom != fcFindingsFrom) { tab.FcFindingsFrom = fcFindingsFrom; changed = true; }
                if (fcFindingsTo.HasValue && tab.FcFindingsTo != fcFindingsTo) { tab.FcFindingsTo = fcFindingsTo; changed = true; }
            }
            
            // CRITICAL FIX: Immediately notify editor properties when proofread fields change
            // This ensures real-time updates when findings_proofread/conclusion_proofread are populated via JSON
            if (proofreadFieldsChanged)
            {
                Debug.WriteLine("[PrevJson] Proofread fields changed - notifying editor properties for real-time updates");
                OnPropertyChanged(nameof(PreviousFindingsEditorText));
                OnPropertyChanged(nameof(PreviousConclusionEditorText));
            }
            
            if (changed) UpdatePreviousReportJson();
            else OnPropertyChanged(nameof(PreviousReportJson));
        }
        
        /// <summary>
        /// Applies JSON content directly to a specific tab's properties without using the shared _previousReportJson field.
        /// This is used when saving a tab's JSON before switching away, to avoid corruption from WPF binding timing issues.
        /// CRITICAL: This method prevents the bug where Tab A's JSON gets overwritten with Tab B's JSON during tab switches.
        /// </summary>
        private void ApplyJsonToTabDirectly(PreviousStudyTab tab, string json)
        {
            if (tab == null || string.IsNullOrWhiteSpace(json) || json == "{}") return;
            
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                
                ApplyPreviousJsonFields(tab, root);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PrevJson] ApplyJsonToTabDirectly error: {ex.Message}");
            }
        }
        
        private void HookPreviousStudy(PreviousStudyTab? oldTab, PreviousStudyTab? newTab)
        {
            if (oldTab != null) oldTab.PropertyChanged -= OnSelectedPrevStudyPropertyChanged;
            if (newTab != null)
            {
                newTab.PropertyChanged += OnSelectedPrevStudyPropertyChanged;
                UpdatePreviousReportJson();
            }
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
                // NOTE: All header component proofread property names removed as per user request
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
                OnPropertyChanged(nameof(PreviousHeaderEditorText));  // FIX: Add notification for header editor
                OnPropertyChanged(nameof(PreviousFindingsEditorText));
                OnPropertyChanged(nameof(PreviousConclusionEditorText));
                
                UpdatePreviousReportJson();
            }
        }
    }
}
