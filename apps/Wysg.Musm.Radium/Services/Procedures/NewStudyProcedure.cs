using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Wysg.Musm.Radium.ViewModels;

namespace Wysg.Musm.Radium.Services.Procedures
{
    public sealed partial class NewStudyProcedure : INewStudyProcedure
    {
        private readonly IClearCurrentFieldsProcedure? _clearCurrentFieldsProc;
        private readonly IClearPreviousFieldsProcedure? _clearPreviousFieldsProc;
        private readonly IClearPreviousStudiesProcedure? _clearPreviousStudiesProc;
        private readonly ISetCurrentStudyTechniquesProcedure? _setCurrentStudyTechniquesProc;
        
        public NewStudyProcedure(
            IClearCurrentFieldsProcedure? clearCurrentFields = null,
            IClearPreviousFieldsProcedure? clearPreviousFields = null,
            IClearPreviousStudiesProcedure? clearPreviousStudies = null,
            ISetCurrentStudyTechniquesProcedure? setCurrentStudyTechniques = null)
        {
            _clearCurrentFieldsProc = clearCurrentFields;
            _clearPreviousFieldsProc = clearPreviousFields;
            _clearPreviousStudiesProc = clearPreviousStudies;
            _setCurrentStudyTechniquesProc = setCurrentStudyTechniques;
        }
        
        public async Task ExecuteAsync(MainViewModel vm)
        {
            if (vm == null) return;
            
            Debug.WriteLine("[NewStudyProcedure] Starting execution");
            
            // Call modular procedures
            if (_clearCurrentFieldsProc != null)
            {
                await _clearCurrentFieldsProc.ExecuteAsync(vm);
            }
            
            if (_clearPreviousFieldsProc != null)
            {
                await _clearPreviousFieldsProc.ExecuteAsync(vm);
            }
            
            if (_clearPreviousStudiesProc != null)
            {
                await _clearPreviousStudiesProc.ExecuteAsync(vm);
            }
            
            // Toggle off Proofread and Reportified (current only)
            vm.ProofreadMode = false;
            vm.Reportified = false;
            
            Debug.WriteLine("[NewStudyProcedure] Toggled off Proofread/Reportified (current only)");
            
            // Auto-fill current study techniques (assumes studyname already set)
            if (_setCurrentStudyTechniquesProc != null)
            {
                await _setCurrentStudyTechniquesProc.ExecuteAsync(vm);
            }

            vm.SetStatusInternal("New study initialized (unlocked, current toggles off, all JSON emptied)");
        }
    }
}