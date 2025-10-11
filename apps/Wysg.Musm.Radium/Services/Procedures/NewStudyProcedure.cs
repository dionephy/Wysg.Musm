using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Wysg.Musm.Radium.ViewModels;

namespace Wysg.Musm.Radium.Services.Procedures
{
    public sealed class NewStudyProcedure : INewStudyProcedure
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
            vm.Reportified = false; // will trigger property logic
            vm.PreviousReportified = true;
            vm.SetStatusInternal("New study initialized (unlocked)");
        }
    }
}