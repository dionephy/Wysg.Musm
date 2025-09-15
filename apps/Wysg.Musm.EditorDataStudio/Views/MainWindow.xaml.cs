using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Wysg.Musm.EditorDataStudio.Services;

namespace Wysg.Musm.EditorDataStudio.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void OnOpenSettings(object sender, RoutedEventArgs e)
        {
            // Resolve IDbConfig from DI to bind into settings window
            if (Application.Current is App app)
            {
                var cfg = app.Services.GetRequiredService<IDbConfig>();
                var win = new SettingsWindow(cfg) { Owner = this };
                _ = win.ShowDialog();
            }
        }
    }
}
