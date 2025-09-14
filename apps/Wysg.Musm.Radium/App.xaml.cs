using System.Configuration;
using System.Data;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Extensions.Hosting;
using Wysg.Musm.Infrastructure.ViewModels;
using Wysg.Musm.Radium.Services;
using Wysg.Musm.Radium.ViewModels;
using Wysg.Musm.Radium.Views;
using System.Threading.Tasks;

namespace Wysg.Musm.Radium
{
    public partial class App : Application
    {
        private readonly IHost _host;

        public App()
        {
            _host = Host
                .CreateDefaultBuilder()
                .ConfigureServices(ConfigureServices)
                .UseSerilog()
                .Build();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            await _host.StartAsync();

            // resolve dev tenant once (robust against varying ids)
            var tenantService = _host.Services.GetRequiredService<ITenantService>();
            var ctx = _host.Services.GetRequiredService<ITenantContext>();
            var dev = await tenantService.GetTenantByCodeAsync("dev");
            if (dev != null)
            {
                ctx.TenantId = dev.Id;
                ctx.TenantCode = dev.Code;
            }

            await ShowSplashLoginAsync();
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            await _host.StopAsync();
            base.OnExit(e);
        }

        private void ConfigureServices(HostBuilderContext context, IServiceCollection services)
        {
            services.AddSingleton<ITenantService, TenantService>();
            services.AddSingleton<IPhraseService, PhraseService>();
            services.AddSingleton<ITenantContext, TenantContext>();
            services.AddSingleton<IPhraseCache, PhraseCache>();
            services.AddTransient<SplashLoginViewModel>();
            services.AddTransient<MainViewModel>();
        }

        private async Task ShowSplashLoginAsync()
        {
            var oldMode = Current.ShutdownMode;
            Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            var tenantService = _host.Services.GetRequiredService<ITenantService>();
            var splashLoginVM = new SplashLoginViewModel(tenantService);
            var splashLoginWindow = new SplashLoginWindow { DataContext = splashLoginVM };

            bool loginSuccess = false;
            splashLoginVM.LoginSuccess += () =>
            {
                loginSuccess = true;
                splashLoginWindow.Close();
            };

            splashLoginWindow.ShowDialog();

            if (loginSuccess)
            {
                var mainVM = _host.Services.GetRequiredService<MainViewModel>();
                var mainWindow = new MainWindow { DataContext = mainVM };
                Current.MainWindow = mainWindow;
                mainWindow.Show();
                Current.ShutdownMode = oldMode;
            }
            else
            {
                Shutdown();
            }
        }
    }
}
