using CommunityToolkit.Mvvm.ComponentModel;

namespace Wysg.Musm.Radium.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private string? _headerFormatTemplate;
        public string? HeaderFormatTemplate
        {
            get => _headerFormatTemplate;
            set
            {
                if (SetProperty(ref _headerFormatTemplate, value))
                {
                    UpdateReportifyJson();
                }
            }
        }
    }
}
