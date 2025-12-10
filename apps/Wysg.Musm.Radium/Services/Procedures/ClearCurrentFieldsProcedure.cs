using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Wysg.Musm.Radium.ViewModels;

namespace Wysg.Musm.Radium.Services.Procedures
{
    /// <summary>
    /// Clears all current report fields (reported, editable, metadata, proofread).
    /// Part of modular NewStudy procedure breakdown.
    /// </summary>
    public sealed class ClearCurrentFieldsProcedure : IClearCurrentFieldsProcedure
    {
        public async Task ExecuteAsync(MainViewModel vm)
        {
            if (vm == null) return;
            
            Debug.WriteLine("[ClearCurrentFieldsProcedure] Emptying all current report JSON fields");
            
            // ========== CURRENT REPORT JSON - EMPTY ALL FIELDS ==========
            // Reported report fields (read-only, from GetReportedReport)
            vm.ReportedHeaderAndFindings = string.Empty;
            vm.ReportedFinalConclusion = string.Empty;
            vm.ReportRadiologist = string.Empty;
            vm.CurrentReportDateTime = null;
            
            // Current editable report fields (bound to Findings/Conclusion editors)
            vm.FindingsText = string.Empty;
            vm.ConclusionText = string.Empty;
            
            // Preorder findings
            vm.FindingsPreorder = string.Empty;
            
            // Metadata fields
            vm.StudyRemark = string.Empty;
            vm.PatientRemark = string.Empty;
            
            // Header component fields (HeaderText is computed from these)
            vm.ChiefComplaint = string.Empty;
            vm.PatientHistory = string.Empty;
            vm.StudyTechniques = string.Empty;
            vm.Comparison = string.Empty;
            
            // Patient and study info fields
            vm.PatientName = string.Empty;
            vm.PatientNumber = string.Empty;
            vm.PatientSex = string.Empty;
            vm.PatientAge = string.Empty;
            vm.StudyName = string.Empty;
            vm.StudyDateTime = string.Empty;
            
            // Update current study label after clearing
            vm.UpdateCurrentStudyLabelInternal();
            
            // Current report proofread fields
            // NOTE: All header component proofread fields removed as per user request
            vm.FindingsProofread = string.Empty;
            vm.ConclusionProofread = string.Empty;
            
            Debug.WriteLine("[ClearCurrentFieldsProcedure] All current report JSON fields emptied");
            
            await Task.CompletedTask;
        }
    }
    
    /// <summary>
    /// Interface for ClearCurrentFieldsProcedure.
    /// </summary>
    public interface IClearCurrentFieldsProcedure
    {
        Task ExecuteAsync(MainViewModel vm);
    }
}
