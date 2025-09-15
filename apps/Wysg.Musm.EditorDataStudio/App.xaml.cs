using System;
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

            var vm = _host.Services.GetRequiredService<MainViewModel>();
            var win = new MainWindow { DataContext = vm };
            win.Show();
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            await _host.StopAsync();
            base.OnExit(e);
        }
    }
}
