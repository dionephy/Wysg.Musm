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
using Microsoft.Data.SqlClient;
using System.Drawing; // added
using System.Text; // added for CodePagesEncodingProvider
using System.IO; // added for path ops
using System.Linq; // added

namespace Wysg.Musm.Radium
{
    /// <summary>
    /// WPF Application bootstrapper.
    /// Responsibilities:
    ///   1. Configure logging (Serilog) with optional verbose Npgsql tracing when RAD_TRACE_PG=1.
    ///   2. Build and own a Generic Host (Microsoft.Extensions.Hosting) so the desktop client can leverage the same
    ///      dependency injection + configuration patterns as ASP.NET / services.
    ///   3. Register all application services (data sources, repositories, view models) in <see cref="ConfigureServices"/>.
    ///   4. Drive the startup UI flow: show splash/login window, perform silent refresh, then promote MainWindow when authenticated.
    ///   5. Manage graceful shutdown (stop host on exit so disposables / background tasks flush).
    ///
    /// High‑level startup sequence:
    ///   App() ctor
    ///     -> Build Serilog logger (single instance for entire process)
    ///     -> Construct Generic Host (DI container + config + logging)
    ///   OnStartup()
    ///     -> Start host (activates singleton services)
    ///     -> Initialize low-level diagnostics (PgDebug, FirstChanceDiagnostics)
    ///     -> ShowSplashLoginAsync(): displays splash; attempts silent login; if success -> preloads phrases -> show MainWindow
    ///   OnExit()
    ///     -> Host.StopAsync (structured disposal / flush) then base shutdown.
    ///
    /// Design notes:
    ///   - A single IServiceProvider (Host.Services) is exposed through the App instance so windows can resolve scoped/transient view models.
    ///   - ViewModels are mostly registered as Transient to avoid stale state leaks; stateful context (ITenantContext) is Singleton.
    ///   - Phrase preload is fire-and-forget to keep perceived login fast (preload cancellation or failure is non-fatal).
    ///   - ShutdownMode initially ExplicitShutdown: prevents premature app exit if splash closes without login success.
    ///     Once MainWindow is shown we change to OnMainWindowClose to align with standard desktop semantics.
    ///
    /// Conventions:
    ///   - All DB-access services prefer constructor DI; no ServiceLocator patterns inside those classes.
    ///   - Strict separation: ViewModels contain UI logic only; services contain IO / persistence logic.
    ///
    /// Extension guidance:
    ///   - To add a new feature service: add Singleton / Transient in ConfigureServices, then inject into consuming VMs.
    ///   - For background recurring tasks, prefer hosting IHostedService implementations registered here instead of ad-hoc Task.Run.
    /// </summary>
    public partial class App : Application
    {
        private readonly IHost _host; // Generic Host instance (owns DI container + logging + lifetime)
        public IServiceProvider Services => _host.Services; // Public access point for legacy code paths / windows

        public App()
        {
            // Register code page encodings (EUC-KR/CP949, etc.) for non-UTF8 HTML decoding
            try { Encoding.RegisterProvider(CodePagesEncodingProvider.Instance); } catch { }

            // 1. Logging bootstrap -------------------------------------------------------------
            // Only configure if not already set (avoids duplicate sinks when re-hosted in tests)
            bool tracePg = Environment.GetEnvironmentVariable("RAD_TRACE_PG") == "1";
            if (Log.Logger == Serilog.Core.Logger.None)
            {
                var logCfg = new LoggerConfiguration()
                    .MinimumLevel.Information()
                    .Enrich.FromLogContext()
                    // Custom sink writes to Debug output window (VS / DebugView) for desktop dev convenience
                    .WriteTo.Sink(new DebugSink());

                if (tracePg)
                {
                    // Elevate verbosity for Npgsql & sockets to diagnose slow opens / transient net issues
                    logCfg = logCfg
                        .MinimumLevel.Override("Npgsql", LogEventLevel.Verbose)
                        .MinimumLevel.Override("System.Net.Sockets", LogEventLevel.Debug);
                    Debug.WriteLine("[Diag] Npgsql trace logging enabled (RAD_TRACE_PG=1)");
                }
                else
                {
                    // Keep Npgsql noise down during normal usage
                    logCfg = logCfg.MinimumLevel.Override("Npgsql", LogEventLevel.Warning);
                }
                Log.Logger = logCfg.CreateLogger();
            }

            // 2. Host build (DI + config) -------------------------------------------------------
            _host = Host
                .CreateDefaultBuilder() // loads appsettings.*, environment vars, user secrets (if configured)
                .ConfigureServices(ConfigureServices) // register application services & VMs
                .UseSerilog() // plug Serilog into Generic Host pipeline
                .Build();
            ServicesInitialized = false; // guard to prevent double init
        }

        private bool ServicesInitialized { get; set; }

        /// <summary>
        /// WPF entry point after XAML initialization. Starts the host then shows splash/login.
        /// </summary>
        protected override async void OnStartup(StartupEventArgs e)
        {   
            base.OnStartup(e);
            // Global exception diagnostics: capture unhandled exceptions early
            try
            {
                AppDomain.CurrentDomain.UnhandledException += (s, ev) =>
                {
                    try
                    {
                        var ex = ev.ExceptionObject as Exception;
                        Debug.WriteLine($"[App][UnhandledException] {(ex?.GetType().Name ?? "<null>")}: {ex?.Message}\n{ex?.StackTrace}");
                        Serilog.Log.Error(ex, "[Unhandled] AppDomain.CurrentDomain.UnhandledException");
                    }
                    catch { }
                };
                DispatcherUnhandledException += (s, ev2) =>
                {
                    try
                    {
                        Debug.WriteLine($"[App][DispatcherUnhandled] {ev2.Exception.GetType().Name}: {ev2.Exception.Message}\n{ev2.Exception.StackTrace}");
                        Serilog.Log.Error(ev2.Exception, "[Unhandled] DispatcherUnhandledException");
                    }
                    catch { }
                    // Do not mark handled here to keep default behavior; change to true to avoid process crash temporarily
                    // ev2.Handled = true;
                };
                System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (s, ev3) =>
                {
                    try
                    {
                        Debug.WriteLine($"[App][UnobservedTask] {ev3.Exception.GetType().Name}: {ev3.Exception.Message}\n{ev3.Exception.StackTrace}");
                        Serilog.Log.Error(ev3.Exception, "[Unhandled] UnobservedTaskException");
                        ev3.SetObserved();
                    }
                    catch { }
                };
            }
            catch { }
            Current.ShutdownMode = ShutdownMode.OnExplicitShutdown; // do not exit when splash closes
            await _host.StartAsync(); // activates Singleton services
            if (!ServicesInitialized)
            {
                try { Services.GetRequiredService<IRadiumLocalSettings>(); } catch { /* Local settings lazy load failure is non-fatal */ }
                PgDebug.Initialize(); // lightweight connection / diagnostic hooks
                FirstChanceDiagnostics.Initialize(); // register first-chance exception listeners
                ServicesInitialized = true;
            }
            await ShowSplashLoginAsync();
        }

        /// <summary>
        /// Ensures all managed resources / hosted services are stopped cleanly.
        /// </summary>
        protected override async void OnExit(ExitEventArgs e)
        {
            await _host.StopAsync(); // graceful shutdown (flush logs, dispose singletons)
            base.OnExit(e);
        }

        /// <summary>
        /// DI registration hub.  Categorized by responsibility for readability.
        /// Scope choices:
        ///   - Singleton: shared state / caches / connection providers / context.
        ///   - Transient: view models (fresh state per window open) & short-lived orchestration components.
        /// </summary>
        private void ConfigureServices(HostBuilderContext context, IServiceCollection services)
        {
            // Core context & configuration ------------------------------------------------------
            services.AddSingleton<ITenantContext, TenantContext>();          // Holds current account/session; raises AccountIdChanged
            services.AddSingleton<IRadiumLocalSettings, RadiumLocalSettings>(); // Encrypted on-disk local & central connection strings
            services.AddSingleton<IPhraseCache, PhraseCache>();              // Completion / search cache

            // Auth / external service integration ----------------------------------------------
            services.AddSingleton<IAuthService, GoogleOAuthAuthService>();   // Google OAuth flow + token refresh
            services.AddSingleton<IAuthStorage, DpapiAuthStorage>();         // DPAPI-protected refresh token storage

            // API client (for calling backend API instead of direct DB access) -----------------
            services.AddSingleton<RadiumApiClient>(sp =>
            {
                // Use environment variable or appsettings for API URL
                var apiUrl = Environment.GetEnvironmentVariable("RADIUM_API_URL") ?? "http://localhost:5205";
                Debug.WriteLine($"[DI] Registering RadiumApiClient with base URL: {apiUrl}");
                return new RadiumApiClient(apiUrl);
            });

            // Feature flag: USE_API=1 to use API, otherwise use direct DB (default)
            var useApi = Environment.GetEnvironmentVariable("USE_API") == "1";
            Debug.WriteLine($"[DI] API Mode: {(useApi ? "ENABLED (via API)" : "DISABLED (direct DB)")}");

            // Data access (central + local) ----------------------------------------------------
            services.AddSingleton<ICentralDataSourceProvider, CentralDataSourceProvider>(); // Shared NpgsqlDataSources (central)
            services.AddSingleton<ITenantRepository, TenantRepository>();    // Local tenant management (PACS profiles)
            services.AddSingleton<IPhraseService>(sp =>
            {
                // Feature flag: USE_API=1 to use API for phrases
                var useApiForPhrases = Environment.GetEnvironmentVariable("USE_API") == "1";
                
                if (useApiForPhrases)
                {
                    Debug.WriteLine("[DI] Using ApiPhraseServiceAdapter (API mode)");
                    return new Wysg.Musm.Radium.Services.Adapters.ApiPhraseServiceAdapter(
                        sp.GetRequiredService<RadiumApiClient>());
                }
                
                // Otherwise use direct database access (current default)
                var settings = sp.GetRequiredService<IRadiumLocalSettings>();
                var cache = sp.GetRequiredService<IPhraseCache>();
                var cs = settings.CentralConnectionString ?? string.Empty;
                
                if (cs.IndexOf("database.windows.net", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    Debug.WriteLine("[DI] Using AzureSqlPhraseService (detected Azure SQL connection string)");
                    return new AzureSqlPhraseService(settings, cache);
                }
                
                Debug.WriteLine("[DI] Using Postgres PhraseService");
                return new PhraseService(settings, sp.GetRequiredService<ICentralDataSourceProvider>(), cache);
            });

            // Hotkey service - switchable between API and direct DB
            services.AddSingleton<IHotkeyService>(sp =>
            {
                if (useApi)
                {
                    Debug.WriteLine("[DI] Using ApiHotkeyServiceAdapter (API mode)");
                    return new Wysg.Musm.Radium.Services.Adapters.ApiHotkeyServiceAdapter(
                        sp.GetRequiredService<RadiumApiClient>());
                }
                else
                {
                    Debug.WriteLine("[DI] Using AzureSqlHotkeyService (direct DB mode)");
                    var settings = sp.GetRequiredService<IRadiumLocalSettings>();
                    return new AzureSqlHotkeyService(settings);
                }
            });

            // Snippet service - switchable between API and direct DB
            services.AddSingleton<ISnippetService>(sp =>
            {
                if (useApi)
                {
                    Debug.WriteLine("[DI] Using ApiSnippetServiceAdapter (API mode)");
                    return new Wysg.Musm.Radium.Services.Adapters.ApiSnippetServiceAdapter(
                        sp.GetRequiredService<RadiumApiClient>());
                }
                else
                {
                    Debug.WriteLine("[DI] Using AzureSqlSnippetService (direct DB mode)");
                    var settings = sp.GetRequiredService<IRadiumLocalSettings>();
                    return new AzureSqlSnippetService(settings);
                }
            });


            services.AddSingleton<IReportifySettingsService>(sp =>
            {
                var settings = sp.GetRequiredService<IRadiumLocalSettings>();
                var cs = settings.CentralConnectionString ?? string.Empty;
                if (cs.IndexOf("database.windows.net", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    Debug.WriteLine("[DI] Using AzureSqlReportifySettingsService");
                    return new AzureSqlReportifySettingsService(settings);
                }
                return new ReportifySettingsService(sp.GetRequiredService<ICentralDataSourceProvider>(), settings);
            });
            services.AddSingleton<IStudynameLoincRepository, StudynameLoincRepository>();    // Mapping study names to LOINC codes
            services.AddSingleton<IRadStudyRepository, RadStudyRepository>();                // Local study + report persistence
            services.AddSingleton<ITechniqueRepository, TechniqueRepository>();              // Technique feature repository (Postgres)
            services.AddSingleton<PacsService>();                                             // PACS interaction abstraction

            // SNOMED mapping - switchable between API and direct DB
            services.AddSingleton<ISnomedMapService>(sp =>
            {
                if (useApi)
                {
                    Debug.WriteLine("[DI] Using ApiSnomedMapService (API mode) - SNOMED via REST API");
                    return new Wysg.Musm.Radium.Services.Adapters.ApiSnomedMapService(
                        sp.GetRequiredService<RadiumApiClient>());
                }
                else
                {
                    Debug.WriteLine("[DI] Using AzureSqlSnomedMapService (direct DB mode)");
                    return new AzureSqlSnomedMapService(sp.GetRequiredService<IRadiumLocalSettings>());
                }
            });


            // Procedures (automation) ----------------------------------------------------------
            services.AddSingleton<Wysg.Musm.Radium.Services.Procedures.INewStudyProcedure, Wysg.Musm.Radium.Services.Procedures.NewStudyProcedure>();
            services.AddSingleton<Wysg.Musm.Radium.Services.Procedures.ILockStudyProcedure, Wysg.Musm.Radium.Services.Procedures.LockStudyProcedure>();

            // ViewModels (transient) -----------------------------------------------------------
            services.AddTransient<SplashLoginViewModel>();
            services.AddTransient<SignUpViewModel>();
            services.AddTransient<MainViewModel>(sp => new MainViewModel(
                sp.GetRequiredService<IPhraseService>(),
                sp.GetRequiredService<ITenantContext>(),
                sp.GetRequiredService<IPhraseCache>(),
                sp.GetRequiredService<IHotkeyService>(),
                sp.GetRequiredService<ISnippetService>(),
                sp.GetService<IRadStudyRepository>(),
                sp.GetService<Wysg.Musm.Radium.Services.Procedures.INewStudyProcedure>(),
                sp.GetService<IRadiumLocalSettings>(),
                sp.GetService<Wysg.Musm.Radium.Services.Procedures.ILockStudyProcedure>(),
                sp.GetService<IAuthStorage>(),
                sp.GetService<ISnomedMapService>(),
                sp.GetService<IStudynameLoincRepository>()
            ));
            services.AddTransient<StudynameLoincViewModel>();
            services.AddTransient<StudynameTechniqueViewModel>();
            services.AddTransient<StudyTechniqueViewModel>();
            services.AddTransient<PhrasesViewModel>();
            services.AddTransient<PhraseExtractionViewModel>();
            services.AddTransient<GlobalPhrasesViewModel>(); // Global phrases admin UI (account_id=1 only)
            services.AddTransient<HotkeysViewModel>(); // Hotkey management UI
            services.AddTransient<SnippetsViewModel>(); // Snippet management UI
            services.AddTransient<SettingsViewModel>(sp => new SettingsViewModel(
                sp.GetRequiredService<IRadiumLocalSettings>(),
                sp.GetService<IReportifySettingsService>(),
                sp.GetService<ITenantContext>(),
                sp.GetService<PhrasesViewModel>(), // compose phrases tab inside settings
                sp.GetRequiredService<ITenantRepository>() // pass tenant repo so SettingsVM can load PACS from DB
            ));
        }

        /// <summary>
        /// Displays SplashLoginWindow (modal).  Silent refresh (if present) occurs inside SplashLoginViewModel.
        /// On successful authentication:
        ///   - Schedules phrase preload (background) for current tenant.
        ///   - Opens MainWindow and switches ShutdownMode to OnMainWindowClose.
        /// On failure / cancel: application exits.
        /// </summary>
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
                    // After login, set per-PACS spy settings path override for ProcedureExecutor
                    var tenant = _host.Services.GetRequiredService<ITenantContext>();
                    string pacsKey = string.IsNullOrWhiteSpace(tenant.CurrentPacsKey) ? "default_pacs" : tenant.CurrentPacsKey;
                    string baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Wysg.Musm", "Radium", "Pacs", SanitizeFileName(pacsKey));
                    Directory.CreateDirectory(baseDir);
                    string spyPath = Path.Combine(baseDir, "ui-procedures.json");
                    ProcedureExecutor.SetProcPathOverride(() => spyPath);
                    string bookmarksPath = Path.Combine(baseDir, "bookmarks.json");
                    UiBookmarks.GetStorePathOverride = () => bookmarksPath;

                    BackgroundTask.Run("PhrasePreload", async () =>
                    {
                        if (Environment.GetEnvironmentVariable("RAD_DISABLE_PHRASE_PRELOAD") == "1")
                        {
                            Debug.WriteLine("[App][Preload] Skipped (RAD_DISABLE_PHRASE_PRELOAD=1)");
                            return;
                        }
                        if (tenant.TenantId > 0)
                        {
                            Debug.WriteLine($"[App][Preload] Start tenant={tenant.TenantId}");
                            var phraseSvc = _host.Services.GetRequiredService<IPhraseService>();
                            await phraseSvc.PreloadAsync(tenant.TenantId);
                            Debug.WriteLine("[App][Preload] Done");
                        }
                    });
                }
                catch { /* Non-fatal preload error suppressed */ }
                splashLoginWindow.Close();
            };

            splashLoginWindow.ShowDialog();

            if (loginSuccess)
            {
                var mainVM = _host.Services.GetRequiredService<MainViewModel>();
                var mainWindow = new MainWindow { DataContext = mainVM };
                Current.MainWindow = mainWindow;
                mainWindow.Show();
                Current.ShutdownMode = ShutdownMode.OnMainWindowClose; // normal desktop lifetime from now on
            }
            else
            {
                Shutdown(); // abort application (no authenticated session)
            }
        }

        private static string SanitizeFileName(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            return string.Join("_", name.Split(invalid, StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.');
        }
    }

    /// <summary>
    /// Minimal Serilog sink forwarding structured events to Debug output window to simplify development
    /// without requiring external log files.  Safe on background threads.
    /// </summary>
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
            catch { /* swallow logging exceptions */ }
        }
    }
}
