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
using System.Diagnostics;
using Wysg.Musm.Radium.Services.Diagnostics; // added
using Serilog.Events;

namespace Wysg.Musm.Radium
{
    public partial class App : Application
    {
        private readonly IHost _host;
        public IServiceProvider Services => _host.Services;

        public App()
        {
            // Network trace (option 3): enable when RAD_TRACE_PG=1
            bool tracePg = Environment.GetEnvironmentVariable("RAD_TRACE_PG") == "1";
            if (Log.Logger == Serilog.Core.Logger.None)
            {
                var logCfg = new LoggerConfiguration()
                    .MinimumLevel.Information()
                    .Enrich.FromLogContext();

                // Fallback: write Serilog events into Debug output via custom sink
                logCfg = logCfg.WriteTo.Sink(new DebugSink());

                if (tracePg)
                {
                    logCfg = logCfg
                        .MinimumLevel.Override("Npgsql", LogEventLevel.Verbose)
                        .MinimumLevel.Override("System.Net.Sockets", LogEventLevel.Debug);
                    Debug.WriteLine("[Diag] Npgsql trace logging enabled (RAD_TRACE_PG=1)");
                }
                else
                {
                    logCfg = logCfg.MinimumLevel.Override("Npgsql", LogEventLevel.Warning);
                }
                Log.Logger = logCfg.CreateLogger();
            }

            _host = Host
                .CreateDefaultBuilder()
                .ConfigureServices(ConfigureServices)
                .UseSerilog()
                .Build();
            ServicesInitialized = false; // flag
        }

        private bool ServicesInitialized { get; set; }
                
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            await _host.StartAsync();
            if (!ServicesInitialized)
            {
                try { Services.GetRequiredService<IRadiumLocalSettings>(); } catch { }
                PgDebug.Initialize();
                FirstChanceDiagnostics.Initialize(); // new
                ServicesInitialized = true;
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
            services.AddSingleton<ITenantContext, TenantContext>();
            services.AddSingleton<IPhraseCache, PhraseCache>();
            services.AddSingleton<IRadiumLocalSettings, RadiumLocalSettings>();
            services.AddSingleton<PacsService>();
            services.AddSingleton<IAuthService, GoogleOAuthAuthService>();
            services.AddSingleton<ISupabaseService, SupabaseService>();
            services.AddSingleton<IAuthStorage, DpapiAuthStorage>();

            services.AddSingleton<ICentralDataSourceProvider, CentralDataSourceProvider>();
            services.AddSingleton<IPhraseService, PhraseService>();
            services.AddSingleton<IStudynameLoincRepository, StudynameLoincRepository>();

            services.AddTransient<SplashLoginViewModel>();
            services.AddTransient<SignUpViewModel>();
            services.AddTransient<MainViewModel>();
            services.AddTransient<StudynameLoincViewModel>();
            services.AddTransient<PhrasesViewModel>();
        }

        public async Task ShowSplashLoginAsync()
        {
            var splashLoginVM = _host.Services.GetRequiredService<SplashLoginViewModel>();
            var splashLoginWindow = new SplashLoginWindow { DataContext = splashLoginVM };

            bool loginSuccess = false;
            splashLoginVM.LoginSuccess += () =>
            {
                loginSuccess = true;
                try
                {
                    BackgroundTask.Run("PhrasePreload", async () =>
                    {
                        if (Environment.GetEnvironmentVariable("RAD_DISABLE_PHRASE_PRELOAD") == "1")
                        {
                            Debug.WriteLine("[App][Preload] Skipped (RAD_DISABLE_PHRASE_PRELOAD=1)");
                            return;
                        }
                        var tenant = _host.Services.GetRequiredService<ITenantContext>();
                        if (tenant.TenantId > 0)
                        {
                            Debug.WriteLine($"[App][Preload] Start tenant={tenant.TenantId}");
                            var phraseSvc = _host.Services.GetRequiredService<IPhraseService>();
                            await phraseSvc.PreloadAsync(tenant.TenantId);
                            Debug.WriteLine("[App][Preload] Done");
                        }
                    });
                }
                catch { }
                splashLoginWindow.Close();
            };

            splashLoginWindow.ShowDialog();

            if (loginSuccess)
            {
                var mainVM = _host.Services.GetRequiredService<MainViewModel>();
                var mainWindow = new MainWindow { DataContext = mainVM };
                Current.MainWindow = mainWindow;
                mainWindow.Show();
                // Ensure application exits when main window closes regardless of auxiliary tool windows
                Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
            }
            else
            {
                Shutdown();
            }
        }
    }

    internal sealed class DebugSink : Serilog.Core.ILogEventSink
    {
        public void Emit(Serilog.Events.LogEvent logEvent)
        {
            try
            {
                var msg = logEvent.RenderMessage();
                System.Diagnostics.Debug.WriteLine($"[Log][{logEvent.Level}] {msg}");
                if (logEvent.Exception != null)
                    System.Diagnostics.Debug.WriteLine(logEvent.Exception);
            }
            catch { }
        }
    }
}
