using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Wysg.Musm.Radium.Services;
using Wysg.Musm.Radium.ViewModels;

namespace Wysg.Musm.Radium.Services.Procedures
{
    public sealed partial class NewStudyProcedure : INewStudyProcedure
    {
        public static async Task TryFillTechniquesFromDefaultAsync(MainViewModel vm)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(vm.StudyName)) return;
                var app = (App)System.Windows.Application.Current;
                var repo = app.Services.GetRequiredService<ITechniqueRepository>();
                var snId = await repo.GetStudynameIdByNameAsync(vm.StudyName.Trim());
                if (!snId.HasValue) return;
                var def = await repo.GetDefaultCombinationForStudynameAsync(snId.Value);
                if (!def.HasValue) return;
                var items = await repo.GetCombinationItemsAsync(def.Value.CombinationId);
                var grouped = TechniqueFormatter.BuildGroupedDisplay(items.Select(i => new TechniqueGroupItem(i.Prefix, i.Tech, i.Suffix, i.SequenceOrder)));
                if (!string.IsNullOrWhiteSpace(grouped)) vm.StudyTechniques = grouped;
            }
            catch (Exception ex) { Debug.WriteLine("[NewStudyProcedure] TryFillTechniquesFromDefaultAsync error: " + ex.Message); }
        }
    }
}
