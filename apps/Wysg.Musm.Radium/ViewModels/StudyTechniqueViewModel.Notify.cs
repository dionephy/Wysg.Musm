using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Wysg.Musm.Radium.ViewModels
{
    public partial class StudyTechniqueViewModel
    {
        private async System.Threading.Tasks.Task NotifyDefaultChangedAsync()
        {
            try
            {
                var app = (App)System.Windows.Application.Current;
                var main = app.Services.GetRequiredService<MainViewModel>();
                await main.RefreshStudyTechniqueFromDefaultAsync();
            }
            catch { }
        }
    }
}
