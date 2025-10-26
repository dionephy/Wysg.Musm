using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Diagnostics;
using Wysg.Musm.Radium.Services;

namespace Wysg.Musm.Radium.ViewModels
{
    public partial class MainViewModel
    {
        private void SubscribeStudynameTechniqueAutoRefresh()
        {
            // Subscribe to StudynameTechniqueViewModel default-set events via a weak event or DI messenger in future.
            // For now, expose a method the window/viewmodel can call explicitly after default change.
        }

        // Called when default technique combination for the current studyname is updated
        public async Task RefreshStudyTechniqueFromDefaultAsync()
        {
            try
            {
                Debug.WriteLine("[MainViewModel.Techniques] RefreshStudyTechniqueFromDefaultAsync - START");
                
                if (string.IsNullOrWhiteSpace(StudyName))
                {
                    Debug.WriteLine("[MainViewModel.Techniques] StudyName is empty - returning");
                    return;
                }
                
                Debug.WriteLine($"[MainViewModel.Techniques] Current StudyName: '{StudyName}'");
                
                var repo = ((App)System.Windows.Application.Current).Services.GetService(typeof(ITechniqueRepository)) as ITechniqueRepository;
                if (repo == null)
                {
                    Debug.WriteLine("[MainViewModel.Techniques] ITechniqueRepository is null - returning");
                    return;
                }
                
                Debug.WriteLine("[MainViewModel.Techniques] Getting studyname ID from repository...");
                var snId = await repo.GetStudynameIdByNameAsync(StudyName.Trim());
                if (!snId.HasValue)
                {
                    Debug.WriteLine($"[MainViewModel.Techniques] No studyname ID found for '{StudyName}' - returning");
                    return;
                }
                
                Debug.WriteLine($"[MainViewModel.Techniques] Studyname ID: {snId.Value}");
                Debug.WriteLine("[MainViewModel.Techniques] Getting default combination...");
                
                var def = await repo.GetDefaultCombinationForStudynameAsync(snId.Value);
                if (!def.HasValue)
                {
                    Debug.WriteLine($"[MainViewModel.Techniques] No default combination found for studyname ID {snId.Value} - returning");
                    return;
                }
                
                Debug.WriteLine($"[MainViewModel.Techniques] Default combination ID: {def.Value.CombinationId}, Display: '{def.Value.Display}'");
                Debug.WriteLine("[MainViewModel.Techniques] Getting combination items...");
                
                var items = await repo.GetCombinationItemsAsync(def.Value.CombinationId);
                Debug.WriteLine($"[MainViewModel.Techniques] Retrieved {items.Count} combination items");
                
                foreach (var item in items)
                {
                    Debug.WriteLine($"[MainViewModel.Techniques]   Item: Prefix='{item.Prefix}', Tech='{item.Tech}', Suffix='{item.Suffix}', Seq={item.SequenceOrder}");
                }
                
                var grouped = TechniqueFormatter.BuildGroupedDisplay(items.Select(i => new TechniqueGroupItem(i.Prefix, i.Tech, i.Suffix, i.SequenceOrder)));
                Debug.WriteLine($"[MainViewModel.Techniques] Grouped display: '{grouped}'");
                
                if (!string.IsNullOrWhiteSpace(grouped))
                {
                    var oldValue = StudyTechniques;
                    StudyTechniques = grouped;
                    Debug.WriteLine($"[MainViewModel.Techniques] StudyTechniques updated: '{oldValue}' -> '{grouped}'");
                }
                else
                {
                    Debug.WriteLine("[MainViewModel.Techniques] Grouped display is empty - not updating StudyTechniques");
                }
                
                Debug.WriteLine("[MainViewModel.Techniques] RefreshStudyTechniqueFromDefaultAsync - COMPLETED");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MainViewModel.Techniques] EXCEPTION in RefreshStudyTechniqueFromDefaultAsync:");
                Debug.WriteLine($"[MainViewModel.Techniques]   Type: {ex.GetType().Name}");
                Debug.WriteLine($"[MainViewModel.Techniques]   Message: {ex.Message}");
                Debug.WriteLine($"[MainViewModel.Techniques]   StackTrace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Debug.WriteLine($"[MainViewModel.Techniques]   InnerException: {ex.InnerException.GetType().Name} - {ex.InnerException.Message}");
                }
            }
        }
        
        public static async Task RefreshStudyTechniqueAfterEditAsync(MainViewModel vm)
        {
            try
            {
                Debug.WriteLine("[MainViewModel.Techniques] RefreshStudyTechniqueAfterEditAsync - START");
                await vm.RefreshStudyTechniqueFromDefaultAsync();
                Debug.WriteLine("[MainViewModel.Techniques] RefreshStudyTechniqueAfterEditAsync - COMPLETED");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MainViewModel.Techniques] EXCEPTION in RefreshStudyTechniqueAfterEditAsync: {ex.Message}");
            }
        }
    }
}
