using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Wysg.Musm.Radium.ViewModels;

namespace Wysg.Musm.Radium.Services.Procedures
{
    /// <summary>
    /// SetCurrentStudyTechniques procedure: Auto-fills study techniques based on studyname.
    /// 
    /// This procedure:
    /// 1. Assumes patient demographics (Name, Number, Age, Sex) and study info (Studyname, Datetime, Remark) 
    ///    have ALREADY been set by other modules (custom procedures or other built-in modules)
    /// 2. Auto-fills study_techniques from default combination (if configured for the studyname)
    /// 3. Sets status message
    /// 
    /// NOTE: This module does NOT fetch from PACS. It only auto-fills techniques based on the 
    /// studyname that was already populated by other modules.
    /// </summary>
    public sealed class SetCurrentStudyTechniquesProcedure : ISetCurrentStudyTechniquesProcedure
    {
        private readonly ITechniqueRepository? _techRepo;
        
        public SetCurrentStudyTechniquesProcedure(ITechniqueRepository? techRepo = null)
        {
            _techRepo = techRepo;
        }
        
        public async Task ExecuteAsync(MainViewModel vm)
        {
            if (vm == null) return;
            
            Debug.WriteLine("[SetCurrentStudyTechniquesProcedure] Starting execution (techniques auto-fill only)");
            
            // DO NOT fetch from PACS - assume patient/study data already set by other modules
            Debug.WriteLine("[SetCurrentStudyTechniquesProcedure] Skipping PACS fetch - using existing patient/study data");
            Debug.WriteLine($"[SetCurrentStudyTechniquesProcedure] Current StudyName: '{vm.StudyName}'");
            Debug.WriteLine($"[SetCurrentStudyTechniquesProcedure] Current PatientNumber: '{vm.PatientNumber}'");

            // Auto-fill study_techniques from default combination (if any)
            try
            {
                if (!string.IsNullOrWhiteSpace(vm.StudyName) && _techRepo != null)
                {
                    Debug.WriteLine($"[SetCurrentStudyTechniquesProcedure] Auto-filling techniques for studyname: {vm.StudyName}");
                    
                    var snId = await _techRepo.GetStudynameIdByNameAsync(vm.StudyName.Trim());
                    if (snId.HasValue)
                    {
                        Debug.WriteLine($"[SetCurrentStudyTechniquesProcedure] Found studyname ID: {snId.Value}");
                        
                        var def = await _techRepo.GetDefaultCombinationForStudynameAsync(snId.Value);
                        if (def.HasValue)
                        {
                            Debug.WriteLine($"[SetCurrentStudyTechniquesProcedure] Found default combination ID: {def.Value.CombinationId}");
                            
                            var items = await _techRepo.GetCombinationItemsAsync(def.Value.CombinationId);
                            var grouped = TechniqueFormatter.BuildGroupedDisplay(
                                items.Select(i => new TechniqueGroupItem(i.Prefix, i.Tech, i.Suffix, i.SequenceOrder)));
                            
                            if (!string.IsNullOrWhiteSpace(grouped))
                            {
                                vm.StudyTechniques = grouped;
                                Debug.WriteLine($"[SetCurrentStudyTechniquesProcedure] Auto-filled study techniques: {grouped}");
                            }
                            else
                            {
                                Debug.WriteLine("[SetCurrentStudyTechniquesProcedure] Grouped techniques result was empty");
                            }
                        }
                        else
                        {
                            Debug.WriteLine("[SetCurrentStudyTechniquesProcedure] No default combination found for studyname");
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"[SetCurrentStudyTechniquesProcedure] Studyname '{vm.StudyName}' not found in database");
                    }
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(vm.StudyName))
                    {
                        Debug.WriteLine("[SetCurrentStudyTechniquesProcedure] StudyName is empty, skipping auto-fill");
                        vm.SetStatusInternal("SetCurrentStudyTechniques: StudyName is empty, cannot auto-fill techniques");
                    }
                    if (_techRepo == null)
                    {
                        Debug.WriteLine("[SetCurrentStudyTechniquesProcedure] TechniqueRepository is null, skipping auto-fill");
                    }
                }
            }
            catch (Exception ex) 
            { 
                Debug.WriteLine($"[SetCurrentStudyTechniquesProcedure] Auto-fill techniques error: {ex.Message}"); 
                vm.SetStatusInternal($"SetCurrentStudyTechniques: Error auto-filling techniques - {ex.Message}");
                // Don't fail the entire procedure if auto-fill fails
                return;
            }

            vm.SetStatusInternal("SetCurrentStudyTechniques: Auto-filled study techniques from default combination");
            Debug.WriteLine("[SetCurrentStudyTechniquesProcedure] Execution completed");
        }
    }
}
