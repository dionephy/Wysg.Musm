using System;
using System.Linq;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Wysg.Musm.EditorDataStudio.Views;
using Wysg.Musm.EditorDataStudio.ViewModels;
using Wysg.Musm.EditorDataStudio.Services;

namespace Wysg.Musm.EditorDataStudio
{
    public partial class App : Application
    {
        private IHost _host = null!;
        public IServiceProvider Services => _host.Services;

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            _host = Host.CreateDefaultBuilder()
                .ConfigureServices(services =>
                {
                    services.AddSingleton<ILocalSettings, LocalSettings>();
                    services.AddSingleton<IDbConfig, DbConfig>();
                    services.AddSingleton<IDb, PgDb>();
                    services.AddSingleton<ITenantLookup, TenantLookup>();
                    services.AddSingleton<IPhraseWriter, PhraseWriter>();

                    services.AddTransient<MainViewModel>();
                })
                .UseSerilog()
                .Build();

            await _host.StartAsync();

            var settings = _host.Services.GetRequiredService<ILocalSettings>();
            ApplyTheme(settings.UseDarkTheme);

            MainViewModel vm;
            try
            {
                vm = _host.Services.GetRequiredService<MainViewModel>();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to initialize MainViewModel.\n{ex.Message}", "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
                return;
            }
            var win = new MainWindow { DataContext = vm };
            win.Show();
        }

        public void ApplyTheme(bool useDark)
        {
            // remove any previously applied theme dictionary from our Themes folder
            for (int i = Resources.MergedDictionaries.Count - 1; i >= 0; i--)
            {
                var md = Resources.MergedDictionaries[i];
                var src = md.Source?.OriginalString ?? string.Empty;
                if (src.Contains("/Themes/") && (src.EndsWith("Light.xaml") || src.EndsWith("Dark.xaml")))
                    Resources.MergedDictionaries.RemoveAt(i);
            }

            var themeName = useDark ? "Dark" : "Light";
            var dict = new ResourceDictionary
            {
                Source = new Uri($"pack://application:,,,/Wysg.Musm.EditorDataStudio;component/Themes/{themeName}.xaml", UriKind.Absolute)
            };
            Resources.MergedDictionaries.Add(dict);
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            await _host.StopAsync();
            base.OnExit(e);
        }
    }
}
