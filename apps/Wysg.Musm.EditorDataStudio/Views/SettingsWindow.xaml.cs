using System.Windows;
using Wysg.Musm.EditorDataStudio.Services;

namespace Wysg.Musm.EditorDataStudio.Views
{
    public partial class SettingsWindow : Window
    {
        private readonly IDbConfig _cfg;
        public SettingsWindow(IDbConfig cfg)
        {
            InitializeComponent();
            _cfg = cfg;
            DataContext = new SettingsViewModel { ConnectionString = _cfg.ConnectionString };
        }

        private void OnSave(object sender, RoutedEventArgs e)
        {
            if (DataContext is SettingsViewModel vm)
            {
                _cfg.ConnectionString = vm.ConnectionString ?? _cfg.ConnectionString;
            }
            DialogResult = true;
            Close();
        }

        private sealed class SettingsViewModel
        {
            public string? ConnectionString { get; set; }
        }
    }
}
