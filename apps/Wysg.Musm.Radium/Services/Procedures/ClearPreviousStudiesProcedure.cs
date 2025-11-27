using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Wysg.Musm.Radium.ViewModels;

namespace Wysg.Musm.Radium.Services.Procedures
{
    /// <summary>
    /// Clears the PreviousStudies collection and resets SelectedPreviousStudy.
    /// Part of modular NewStudy procedure breakdown.
    /// </summary>
    public sealed class ClearPreviousStudiesProcedure : IClearPreviousStudiesProcedure
    {
        public async Task ExecuteAsync(MainViewModel vm)
        {
            if (vm == null) return;
            
            Debug.WriteLine("[ClearPreviousStudiesProcedure] Clearing PreviousStudies collection");
            
            // Clear the PreviousStudies collection
            vm.PreviousStudies.Clear();
            
            // Reset the selected previous study
            vm.SelectedPreviousStudy = null;
            
            Debug.WriteLine("[ClearPreviousStudiesProcedure] PreviousStudies collection cleared");
            
            await Task.CompletedTask;
        }
    }
    
    /// <summary>
    /// Interface for ClearPreviousStudiesProcedure.
    /// </summary>
    public interface IClearPreviousStudiesProcedure
    {
        Task ExecuteAsync(MainViewModel vm);
    }
}
