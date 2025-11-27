using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Wysg.Musm.Radium.ViewModels;

namespace Wysg.Musm.Radium.Services.Procedures
{
    /// <summary>
    /// Clears all previous study report fields for ALL previous study tabs.
    /// Part of modular NewStudy procedure breakdown.
    /// </summary>
    public sealed class ClearPreviousFieldsProcedure : IClearPreviousFieldsProcedure
    {
        public async Task ExecuteAsync(MainViewModel vm)
        {
            if (vm == null) return;
            
            Debug.WriteLine("[ClearPreviousFieldsProcedure] Emptying all previous report JSON fields");
            
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
                // NOTE: All header component proofread fields removed as per user request
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
            
            Debug.WriteLine("[ClearPreviousFieldsProcedure] All previous report JSON fields emptied");
            
            await Task.CompletedTask;
        }
    }
    
    /// <summary>
    /// Interface for ClearPreviousFieldsProcedure.
    /// </summary>
    public interface IClearPreviousFieldsProcedure
    {
        Task ExecuteAsync(MainViewModel vm);
    }
}
