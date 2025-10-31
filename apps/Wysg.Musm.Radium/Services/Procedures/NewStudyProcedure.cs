using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Wysg.Musm.Radium.ViewModels;

namespace Wysg.Musm.Radium.Services.Procedures
{
    public sealed partial class NewStudyProcedure : INewStudyProcedure
    {
        public async Task ExecuteAsync(MainViewModel vm)
        {
            if (vm == null) return;
            
            // FIRST: Empty ALL JSON fields for current and previous reports
            Debug.WriteLine("[NewStudyProcedure] Emptying ALL JSON fields (current and previous reports)");
            
            // ========== CURRENT REPORT JSON - EMPTY ALL FIELDS ==========
            // Reported report fields (read-only, from GetReportedReport)
            vm.ReportedHeaderAndFindings = string.Empty;
            vm.ReportedFinalConclusion = string.Empty;
            vm.ReportRadiologist = string.Empty;
            
            // Current editable report fields (bound to Findings/Conclusion editors)
            // Note: FindingsText and ConclusionText will be cleared later by the existing code
            
            // Preorder findings
            vm.FindingsPreorder = string.Empty;
            
            // Metadata fields
            vm.StudyRemark = string.Empty;
            vm.PatientRemark = string.Empty;
            
            // Header component fields (will be cleared by existing code below)
            // ChiefComplaint, PatientHistory, StudyTechniques, Comparison
            
            // Current report proofread fields
            vm.ChiefComplaintProofread = string.Empty;
            vm.PatientHistoryProofread = string.Empty;
            vm.StudyTechniquesProofread = string.Empty;
            vm.ComparisonProofread = string.Empty;
            vm.FindingsProofread = string.Empty;
            vm.ConclusionProofread = string.Empty;
            
            // ========== PREVIOUS REPORT JSON - EMPTY ALL FIELDS ==========
            // Clear for EVERY previous study tab (not just selected one)
            foreach (var prevTab in vm.PreviousStudies)
            {
                // Original report text fields
                prevTab.Header = string.Empty;
                prevTab.Findings = string.Empty;
                prevTab.Conclusion = string.Empty;
                prevTab.OriginalFindings = string.Empty;
                prevTab.OriginalConclusion = string.Empty;
                
                // Split output fields (root JSON)
                prevTab.HeaderTemp = string.Empty;
                prevTab.FindingsOut = string.Empty;
                prevTab.ConclusionOut = string.Empty;
                
                // Metadata fields
                prevTab.ChiefComplaint = string.Empty;
                prevTab.PatientHistory = string.Empty;
                prevTab.StudyTechniques = string.Empty;
                prevTab.Comparison = string.Empty;
                prevTab.StudyRemark = string.Empty;
                prevTab.PatientRemark = string.Empty;
                
                // Proofread fields
                prevTab.ChiefComplaintProofread = string.Empty;
                prevTab.PatientHistoryProofread = string.Empty;
                prevTab.StudyTechniquesProofread = string.Empty;
                prevTab.ComparisonProofread = string.Empty;
                prevTab.FindingsProofread = string.Empty;
                prevTab.ConclusionProofread = string.Empty;
                
                // Split range fields (nullable int)
                prevTab.HfHeaderFrom = null;
                prevTab.HfHeaderTo = null;
                prevTab.HfConclusionFrom = null;
                prevTab.HfConclusionTo = null;
                prevTab.FcHeaderFrom = null;
                prevTab.FcHeaderTo = null;
                prevTab.FcFindingsFrom = null;
                prevTab.FcFindingsTo = null;
            }
            
            // Toggle off Proofread (current only - previous proofread toggle is NOT touched)
            vm.ProofreadMode = false;
            
            // Toggle off Reportified (current only - previous doesn't have this toggle anymore)
            vm.Reportified = false;
            
            Debug.WriteLine("[NewStudyProcedure] Emptied all JSON fields and toggled off Proofread/Reportified (current only)");
            
            // Continue with existing NewStudy logic
            vm.PreviousStudies.Clear();
            vm.SelectedPreviousStudy = null;
            // Clear header component fields (HeaderText is computed from these)
            vm.ChiefComplaint = vm.PatientHistory = vm.StudyTechniques = vm.Comparison = string.Empty;
            vm.FindingsText = vm.ConclusionText = string.Empty;
            vm.PatientName = vm.PatientNumber = vm.PatientSex = vm.PatientAge = vm.StudyName = vm.StudyDateTime = string.Empty;
            vm.UpdateCurrentStudyLabelInternal();
            try { await vm.FetchCurrentStudyAsyncInternal(); } catch (Exception ex) { Debug.WriteLine("[NewStudyProcedure] fetch error: " + ex.Message); }

            // After studyname is loaded, auto-fill study_techniques from default combination (if any)
            try
            {
                if (!string.IsNullOrWhiteSpace(vm.StudyName) && _techRepo != null)
                {
                    var snId = await _techRepo.GetStudynameIdByNameAsync(vm.StudyName.Trim());
                    if (snId.HasValue)
                    {
                        var def = await _techRepo.GetDefaultCombinationForStudynameAsync(snId.Value);
                        if (def.HasValue)
                        {
                            var items = await _techRepo.GetCombinationItemsAsync(def.Value.CombinationId);
                            var grouped = TechniqueFormatter.BuildGroupedDisplay(items.Select(i => new TechniqueGroupItem(i.Prefix, i.Tech, i.Suffix, i.SequenceOrder)));
                            if (!string.IsNullOrWhiteSpace(grouped)) vm.StudyTechniques = grouped;
                        }
                    }
                }
            }
            catch (Exception ex) { Debug.WriteLine("[NewStudyProcedure] autofill techniques error: " + ex.Message); }

            vm.SetStatusInternal("New study initialized (unlocked, current toggles off, all JSON emptied)");
        }
    }
}