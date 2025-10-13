using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace Wysg.Musm.Radium.Views
{
    public partial class StudynameTechniqueWindow : Window
    {
        protected override async void OnClosed(System.EventArgs e)
        {
            base.OnClosed(e);
            // After closing (possibly adding new default), refresh current study techniques if studyname matches
            try
            {
                var app = (App)Application.Current;
                var main = app.Services.GetRequiredService<ViewModels.MainViewModel>();
                await main.RefreshStudyTechniqueFromDefaultAsync();
            }
            catch { }
        }
    }
}
