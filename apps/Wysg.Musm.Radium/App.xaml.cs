using System;
using System.Configuration;
using System.Data;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Extensions.Hosting;
using Wysg.Musm.Radium.Services;
using Wysg.Musm.Radium.ViewModels;
using Wysg.Musm.Radium.Views;
using System.Threading.Tasks;

namespace Wysg.Musm.Radium
{
    public partial class App : Application
    {
        private readonly IHost _host;
        public IServiceProvider Services => _host.Services;

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
            Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            await _host.StartAsync();
            await ShowSplashLoginAsync();
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            await _host.StopAsync();
            base.OnExit(e);
        }

        private void ConfigureServices(HostBuilderContext context, IServiceCollection services)
        {
            services.AddSingleton<ITenantContext, TenantContext>();
            services.AddSingleton<IPhraseCache, PhraseCache>();
            services.AddSingleton<IRadiumLocalSettings, RadiumLocalSettings>();
            services.AddSingleton<MfcPacsService>();
            services.AddSingleton<IAuthService, GoogleOAuthAuthService>();
            services.AddSingleton<ISupabaseService, SupabaseService>();
            services.AddSingleton<IAuthStorage, DpapiAuthStorage>();

            // keep existing editor services
            services.AddSingleton<IPhraseService, PhraseService>();

            services.AddTransient<SplashLoginViewModel>();
            services.AddTransient<SignUpViewModel>();
            services.AddTransient<MainViewModel>();
        }

        public async Task ShowSplashLoginAsync()
        {
            var splashLoginVM = _host.Services.GetRequiredService<SplashLoginViewModel>();
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
                Current.ShutdownMode = ShutdownMode.OnLastWindowClose;
            }
            else
            {
                Shutdown();
            }
        }
    }
}
