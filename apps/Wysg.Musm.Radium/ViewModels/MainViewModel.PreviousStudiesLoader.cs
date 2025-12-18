using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Wysg.Musm.Radium.ViewModels
{
    /// <summary>
    /// Partial: Data loading of previous studies from repository (isolated from presentation logic).
    /// </summary>
    public partial class MainViewModel
    {
        private async Task LoadPreviousStudiesForPatientAsync(string patientId)
        {
            if (_studyRepo == null) return;
            try
            {
                var rows = await _studyRepo.GetReportsForPatientAsync(patientId);
                
                // CRITICAL FIX: Clear selection and cache BEFORE clearing collection
                // This prevents stale cache data from being shown in the UI
                _selectedPreviousStudy = null;
                _prevHeaderTempCache = string.Empty;
                _prevHeaderAndFindingsCache = string.Empty;
                _prevFinalConclusionCache = string.Empty;
                _prevFindingsOutCache = string.Empty;
                _prevConclusionOutCache = string.Empty;
                _prevStudyRemarkCache = string.Empty;
                _prevPatientRemarkCache = string.Empty;
                _prevChiefComplaintCache = string.Empty;
                _prevPatientHistoryCache = string.Empty;
                _prevStudyTechniquesCache = string.Empty;
                _prevComparisonCache = string.Empty;
                _prevFindingsProofreadCache = string.Empty;
                _prevConclusionProofreadCache = string.Empty;
                
                PreviousStudies.Clear();
                var groups = rows.GroupBy(r => new { r.StudyId, r.StudyDateTime, r.Studyname });
                foreach (var g in groups.OrderByDescending(g => g.Key.StudyDateTime))
                {
                    string modality = await ExtractModalityAsync(g.Key.Studyname);
                    if (PreviousStudies.Any(t => t.StudyDateTime == g.Key.StudyDateTime && string.Equals(t.Modality, modality, StringComparison.OrdinalIgnoreCase)))
                        continue;
                    var tab = new PreviousStudyTab
                    {
                        Id = Guid.NewGuid(),
                        StudyDateTime = g.Key.StudyDateTime,
                        Modality = modality,
                        Title = $"{modality} {g.Key.StudyDateTime:yyyy-MM-dd}"
                    };
                    foreach (var row in g.OrderByDescending(r => r.ReportDateTime))
                    {
                        // Variables to hold database fields
                        string headerFind = string.Empty;  // Original header_and_findings from PACS
                        string finalConclusion = string.Empty;  // Original final_conclusion from PACS
                        string createdBy = string.Empty;
                        
                        // Extended fields from JSON
                        string studyRemark = string.Empty;
                        string patientRemark = string.Empty;
                        string chiefComplaint = string.Empty;
                        string patientHistory = string.Empty;
                        string studyTechniques = string.Empty;
                        string comparison = string.Empty;
                        
                        // Proofread fields from JSON
                        // NOTE: All header component proofread fields removed as per user request
                        string findingsProofread = string.Empty;
                        string conclusionProofread = string.Empty;
                        
                        // CRITICAL FIX: Variables to hold split range properties from PrevReport section
                        int? hfHeaderFrom = null;
                        int? hfHeaderTo = null;
                        int? hfConclusionFrom = null;
                        int? hfConclusionTo = null;
                        int? fcHeaderFrom = null;
                        int? fcHeaderTo = null;
                        int? fcFindingsFrom = null;
                        int? fcFindingsTo = null;
                        
                        try
                        {
                            using var doc = JsonDocument.Parse(row.ReportJson);
                            var root = doc.RootElement;
                            
                            // Read ORIGINAL fields from database (not computed split outputs)
                            if (root.TryGetProperty("header_and_findings", out var hf)) 
                                headerFind = hf.GetString() ?? string.Empty;

                            // Prefer new mapping: root.final_conclusion
                            if (root.TryGetProperty("final_conclusion", out var fc))
                            {
                                finalConclusion = fc.GetString() ?? string.Empty;
                            }
                            else if (root.TryGetProperty("conclusion", out var cc))
                            {
                                // Legacy: try root.conclusion
                                finalConclusion = cc.GetString() ?? string.Empty;
                            }
                            else if (root.TryGetProperty("PrevReport", out var pr) && pr.ValueKind == JsonValueKind.Object)
                            {
                                // Back-compat: nested PrevReport fields
                                if (pr.TryGetProperty("final_conclusion", out var pfc)) finalConclusion = pfc.GetString() ?? string.Empty;
                                else if (pr.TryGetProperty("conclusion", out var pcc)) finalConclusion = pcc.GetString() ?? string.Empty;
                            }
                            
                            // Read radiologist from JSON
                            if (root.TryGetProperty("report_radiologist", out var rr)) createdBy = rr.GetString() ?? string.Empty;
                            
                            // Read extended metadata fields
                            if (root.TryGetProperty("study_remark", out var sr)) studyRemark = sr.GetString() ?? string.Empty;
                            if (root.TryGetProperty("patient_remark", out var pr2)) patientRemark = pr2.GetString() ?? string.Empty;
                            if (root.TryGetProperty("chief_complaint", out var ccmp)) chiefComplaint = ccmp.GetString() ?? string.Empty;
                            if (root.TryGetProperty("patient_history", out var ph)) patientHistory = ph.GetString() ?? string.Empty;
                            if (root.TryGetProperty("study_techniques", out var st)) studyTechniques = st.GetString() ?? string.Empty;
                            if (root.TryGetProperty("comparison", out var cmp)) comparison = cmp.GetString() ?? string.Empty;
                            
                            // Read proofread fields
                            // NOTE: All header component proofread fields removed as per user request
                            if (root.TryGetProperty("findings_proofread", out var fpr)) findingsProofread = fpr.GetString() ?? string.Empty;
                            if (root.TryGetProperty("conclusion_proofread", out var cnpr)) conclusionProofread = cnpr.GetString() ?? string.Empty;
                    
                            // CRITICAL FIX: Read split ranges from PrevReport section
                            if (root.TryGetProperty("PrevReport", out var prevReport) && prevReport.ValueKind == JsonValueKind.Object)
                            {
                                int? GetInt(string name)
                                {
                                    if (prevReport.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out var i))
                                        return i;
                                    return null;
                                }
                        
                                hfHeaderFrom = GetInt("header_and_findings_header_splitter_from");
                                hfHeaderTo = GetInt("header_and_findings_header_splitter_to");
                                hfConclusionFrom = GetInt("header_and_findings_conclusion_splitter_from");
                                hfConclusionTo = GetInt("header_and_findings_conclusion_splitter_to");
                                fcHeaderFrom = GetInt("final_conclusion_header_splitter_from");
                                fcHeaderTo = GetInt("final_conclusion_header_splitter_to");
                                fcFindingsFrom = GetInt("final_conclusion_findings_splitter_from");
                                fcFindingsTo = GetInt("final_conclusion_findings_splitter_to");
                        
                                Debug.WriteLine($"[PrevLoad] Loaded split ranges from DB: HfHeaderFrom={hfHeaderFrom}, HfHeaderTo={hfHeaderTo}");
                            }
                        }
                        catch (Exception ex) { Debug.WriteLine("[PrevLoad] JSON parse error: " + ex.Message); }
                        
                        var choice = new PreviousReportChoice
                        {
                            ReportDateTime = row.ReportDateTime,
                            CreatedBy = createdBy,
                            Studyname = row.Studyname,
                            Findings = headerFind,  // Use original header_and_findings
                            Conclusion = finalConclusion,  // Use original final_conclusion
                            _studyDateTime = row.StudyDateTime,
                            ReportJson = row.ReportJson  // CRITICAL FIX: Store the raw JSON for this report
                        };
                        tab.Reports.Add(choice);
                        
                        // If this is the first (most recent) report, populate tab fields from it
                        if (tab.Reports.Count == 1)
                        {
                            // Populate extended fields in the tab
                            tab.StudyRemark = studyRemark;
                            tab.PatientRemark = patientRemark;
                            tab.ChiefComplaint = chiefComplaint;
                            tab.PatientHistory = patientHistory;
                            tab.StudyTechniques = studyTechniques;
                            tab.Comparison = comparison;
                            
                            // Populate proofread fields in the tab
                            // NOTE: All header component proofread fields removed as per user request
                            tab.FindingsProofread = findingsProofread;
                            tab.ConclusionProofread = conclusionProofread;
                            
                            // CRITICAL FIX: Populate split range properties in the tab
                            tab.HfHeaderFrom = hfHeaderFrom;
                            tab.HfHeaderTo = hfHeaderTo;
                            tab.HfConclusionFrom = hfConclusionFrom;
                            tab.HfConclusionTo = hfConclusionTo;
                            tab.FcHeaderFrom = fcHeaderFrom;
                            tab.FcHeaderTo = fcHeaderTo;
                            tab.FcFindingsFrom = fcFindingsFrom;
                            tab.FcFindingsTo = fcFindingsTo;
                            
                            // Store the raw JSON for later use
                            tab.RawJson = row.ReportJson;
                        }
                    }
                    tab.SelectedReport = tab.Reports.FirstOrDefault();
                    if (tab.SelectedReport != null)
                    {
                        tab.OriginalFindings = tab.SelectedReport.Findings;
                        tab.OriginalConclusion = tab.SelectedReport.Conclusion;
                        tab.Findings = tab.SelectedReport.Findings;  // This is now header_and_findings (original)
                        tab.Conclusion = tab.SelectedReport.Conclusion;  // This is now final_conclusion (original)
                    }
                    PreviousStudies.Add(tab);
                }
                if (PreviousStudies.Count > 0)
                {
                    SelectedPreviousStudy = PreviousStudies.First();
                }
            }
            catch (Exception ex) { Debug.WriteLine("[PrevLoad] error: " + ex.Message); }
        }
        
        public async Task RefreshPreviousStudyModalitiesAsync()
        {
            if (_studynameLoincRepo == null || PreviousStudies.Count == 0)
                return;

            foreach (var tab in PreviousStudies)
            {
                var studyname = tab.SelectedReport?.Studyname
                               ?? tab.Reports.FirstOrDefault()?.Studyname
                               ?? string.Empty;

                if (string.IsNullOrWhiteSpace(studyname))
                    continue;

                var newModality = await ExtractModalityAsync(studyname);
                if (!string.Equals(tab.Modality, newModality, StringComparison.OrdinalIgnoreCase))
                {
                    tab.Modality = newModality;
                    tab.Title = $"{newModality} {tab.StudyDateTime:yyyy-MM-dd}";
                }
            }
        }
 
         /// <summary>
         /// Extracts modality from studyname using LOINC mapping.
         /// First tries to get modality from LOINC part mapping (Rad.Modality.Modality Type).
         /// Falls back to simple prefix extraction from studyname if no LOINC mapping exists.
         /// </summary>
        private async Task<string> ExtractModalityAsync(string studyname)
        {
            if (string.IsNullOrWhiteSpace(studyname))
                return "UNKNOWN";

            try
            {
                // Try to get modality from LOINC mapping
                if (_studynameLoincRepo != null)
                {
                    var studynameRows = await _studynameLoincRepo.GetStudynamesAsync();
                    var studynameRow = studynameRows.FirstOrDefault(s => string.Equals(s.Studyname, studyname, StringComparison.OrdinalIgnoreCase));
                    
                    if (studynameRow != null)
                    {
                        var mappings = await _studynameLoincRepo.GetMappingsAsync(studynameRow.Id);
                        var parts = await _studynameLoincRepo.GetPartsAsync();
                        
                        // Find the Rad.Modality.Modality Type part
                        var modalityPart = mappings
                            .Join(parts, m => m.PartNumber, p => p.PartNumber, (m, p) => p)
                            .FirstOrDefault(p => string.Equals(p.PartTypeName, "Rad.Modality.Modality Type", StringComparison.Ordinal));
                        
                        if (modalityPart != null && !string.IsNullOrWhiteSpace(modalityPart.PartName))
                        {
                            Debug.WriteLine($"[ExtractModality] Found LOINC modality for '{studyname}': {modalityPart.PartName}");
                            return modalityPart.PartName;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ExtractModality] Error getting LOINC modality: {ex.Message}");
            }
            
            // Fallback: Extract modality from studyname prefix
            return ExtractModalityFallback(studyname);
        }

        /// <summary>
        /// Fallback method to extract modality from studyname when LOINC mapping is not available.
        /// Returns "OT" (Other) for unmapped studynames as per specification.
        /// </summary>
        private static string ExtractModalityFallback(string studyname)
        {
            if (string.IsNullOrWhiteSpace(studyname))
                return "OT";

            // For unmapped studies, return "OT" (Other) as per specification
            // This indicates the study needs LOINC mapping to get proper modality
            Debug.WriteLine($"[ExtractModality] No LOINC mapping for '{studyname}' - returning 'OT'");
            return "OT";
        }
    }
}
