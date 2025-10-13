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

            vm.Reportified = false; // will trigger property logic
            vm.PreviousReportified = true;
            vm.SetStatusInternal("New study initialized (unlocked)");
        }
    }
}