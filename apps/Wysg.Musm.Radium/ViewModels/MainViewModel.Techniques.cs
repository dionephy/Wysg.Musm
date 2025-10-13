using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
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
                if (string.IsNullOrWhiteSpace(StudyName)) return;
                var repo = ((App)System.Windows.Application.Current).Services.GetService(typeof(ITechniqueRepository)) as ITechniqueRepository;
                if (repo == null) return;
                var snId = await repo.GetStudynameIdByNameAsync(StudyName.Trim());
                if (!snId.HasValue) return;
                var def = await repo.GetDefaultCombinationForStudynameAsync(snId.Value);
                if (!def.HasValue) return;
                var items = await repo.GetCombinationItemsAsync(def.Value.CombinationId);
                var grouped = TechniqueFormatter.BuildGroupedDisplay(items.Select(i => new TechniqueGroupItem(i.Prefix, i.Tech, i.Suffix, i.SequenceOrder)));
                if (!string.IsNullOrWhiteSpace(grouped)) StudyTechniques = grouped;
            }
            catch { }
        }
    }
}
