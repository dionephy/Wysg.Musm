using System.Windows;
using Wysg.Musm.EditorDataStudio.Services;

namespace Wysg.Musm.EditorDataStudio.Views
{
    public partial class SettingsWindow : Window
    {
        private readonly IDbConfig _cfg;
        private readonly ILocalSettings _local;
        public SettingsWindow(IDbConfig cfg, ILocalSettings local)
        {
            InitializeComponent();
            _cfg = cfg;
            _local = local;
            DataContext = new SettingsViewModel { ConnectionString = _cfg.ConnectionString, UseDarkTheme = _local.UseDarkTheme };
        }

        private void OnSave(object sender, RoutedEventArgs e)
        {
            if (DataContext is SettingsViewModel vm)
            {
                _cfg.ConnectionString = vm.ConnectionString ?? _cfg.ConnectionString;
                _local.UseDarkTheme = vm.UseDarkTheme;

                if (Application.Current is App app)
                    app.ApplyTheme(_local.UseDarkTheme);
            }
            DialogResult = true;
            Close();
        }

        private sealed class SettingsViewModel
        {
            public string? ConnectionString { get; set; }
            public bool UseDarkTheme { get; set; }
        }
    }
}
