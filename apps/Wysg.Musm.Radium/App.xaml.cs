using System;
using System.Configuration;
using System.Data;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Http; // HttpClient factory
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
using Wysg.Musm.Radium.Services.ApiClients; // API clients
using Wysg.Musm.Radium.Configuration; // ApiSettings

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

            // Pipe Debug.WriteLine output to console so it appears in dotnet run (Option 2)
            try
            {
                if (Trace.Listeners.OfType<TextWriterTraceListener>().All(l => l.Writer != Console.Out))
                {
                    Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));
                    Trace.AutoFlush = true;
                    Debug.WriteLine("[App][DebugListener] Console listener (Trace) attached.");
                }
            }
            catch { }

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
            // ✨ NEW: API Settings Configuration (base URL now sourced from local settings only)
            var apiSettings = context.Configuration.GetSection("ApiSettings").Get<ApiSettings>() 
                           ?? new ApiSettings();
            services.AddSingleton(apiSettings);

            // ✨ NEW: Register API Clients (6 clients)
            RegisterApiClients(services);

            // ✨ NEW: Register API Helper Services
            services.AddSingleton<ApiTokenManager>();
            services.AddSingleton<ApiTestService>();

            // Core context & configuration ------------------------------------------------------
            services.AddSingleton<ITenantContext, TenantContext>();          // Holds current account/session; raises AccountIdChanged
            services.AddSingleton<IRadiumLocalSettings, RadiumLocalSettings>(); // Encrypted on-disk local settings
            services.AddSingleton<IPhraseCache, PhraseCache>();              // Completion / search cache

            // Auth / external service integration ----------------------------------------------
            services.AddSingleton<IAuthService, GoogleOAuthAuthService>();   // Google OAuth flow + token refresh
            services.AddSingleton<IAuthStorage, DpapiAuthStorage>();         // DPAPI-protected refresh token storage

            // API client (for calling backend API instead of direct DB access) -----------------
            services.AddSingleton<RadiumApiClient>(sp =>
            {
                var apiUrl = ResolveApiBaseUrl(sp);
                Debug.WriteLine($"[DI] Registering RadiumApiClient with base URL (settings): {apiUrl}");
                return new RadiumApiClient(apiUrl);
            });

            // All central data access is now exclusively via API
            Debug.WriteLine("[DI] API Mode: ENABLED (all central data via API)");

            // Data access (local only) ---------------------------------------------------------
            services.AddSingleton<ITenantRepository, TenantRepository>();    // Local tenant management (PACS profiles)
            
            // Phrase service - now exclusively via API adapter
            services.AddSingleton<IPhraseService>(sp =>
            {
                Debug.WriteLine("[DI] Using ApiPhraseServiceAdapter (API mode)");
                return new Wysg.Musm.Radium.Services.Adapters.ApiPhraseServiceAdapter(
                    sp.GetRequiredService<RadiumApiClient>());
            });

            // Hotkey service - now exclusively via API adapter
            services.AddSingleton<IHotkeyService>(sp =>
            {
                Debug.WriteLine("[DI] Using ApiHotkeyServiceAdapter (API mode)");
                return new Wysg.Musm.Radium.Services.Adapters.ApiHotkeyServiceAdapter(
                    sp.GetRequiredService<RadiumApiClient>());
            });

            // Snippet service - now exclusively via API adapter
            services.AddSingleton<ISnippetService>(sp =>
            {
                Debug.WriteLine("[DI] Using ApiSnippetServiceAdapter (API mode)");
                return new Wysg.Musm.Radium.Services.Adapters.ApiSnippetServiceAdapter(
                    sp.GetRequiredService<RadiumApiClient>());
            });

            // Reportify settings - now exclusively via API
            services.AddSingleton<IReportifySettingsService>(sp =>
            {
                Debug.WriteLine("[DI] Using ApiReportifySettingsService (API mode)");
                return new ApiReportifySettingsService(
                    sp.GetRequiredService<IUserSettingsApiClient>(),
                    sp.GetRequiredService<ITenantContext>());
            });

            services.AddSingleton<IStudynameLoincRepository, StudynameLoincRepository>();    // Mapping study names to LOINC codes
            services.AddSingleton<IRadStudyRepository, RadStudyRepository>();                // Local study + report persistence
            services.AddSingleton<ITechniqueRepository, TechniqueRepository>();              // Technique feature repository (Postgres)
            services.AddSingleton<PacsService>();                                             // PACS interaction abstraction

            // SNOMED mapping - now exclusively via API adapter
            services.AddSingleton<ISnomedMapService>(sp =>
            {
                Debug.WriteLine("[DI] Using ApiSnomedMapServiceAdapter (API mode) - SNOMED via REST API");
                return new Wysg.Musm.Radium.Services.Adapters.ApiSnomedMapServiceAdapter(
                    sp.GetRequiredService<ISnomedApiClient>());
            });


            // Procedures (automation) ----------------------------------------------------------
            services.AddSingleton<Wysg.Musm.Radium.Services.Procedures.INewStudyProcedure>(sp => new Wysg.Musm.Radium.Services.Procedures.NewStudyProcedure(
                sp.GetService<Wysg.Musm.Radium.Services.Procedures.IClearCurrentFieldsProcedure>(),
                sp.GetService<Wysg.Musm.Radium.Services.Procedures.IClearPreviousFieldsProcedure>(),
                sp.GetService<Wysg.Musm.Radium.Services.Procedures.IClearPreviousStudiesProcedure>(),
                sp.GetService<Wysg.Musm.Radium.Services.Procedures.ISetCurrentStudyTechniquesProcedure>()
            ));
            services.AddSingleton<Wysg.Musm.Radium.Services.Procedures.ISetStudyLockedProcedure, Wysg.Musm.Radium.Services.Procedures.SetStudyLockedProcedure>();
            services.AddSingleton<Wysg.Musm.Radium.Services.Procedures.ISetStudyOpenedProcedure, Wysg.Musm.Radium.Services.Procedures.SetStudyOpenedProcedure>();
            services.AddSingleton<Wysg.Musm.Radium.Services.Procedures.IClearCurrentFieldsProcedure, Wysg.Musm.Radium.Services.Procedures.ClearCurrentFieldsProcedure>();
            services.AddSingleton<Wysg.Musm.Radium.Services.Procedures.IClearPreviousFieldsProcedure, Wysg.Musm.Radium.Services.Procedures.ClearPreviousFieldsProcedure>();
            services.AddSingleton<Wysg.Musm.Radium.Services.Procedures.IClearPreviousStudiesProcedure, Wysg.Musm.Radium.Services.Procedures.ClearPreviousStudiesProcedure>();
            services.AddSingleton<Wysg.Musm.Radium.Services.Procedures.ISetCurrentStudyTechniquesProcedure>(sp => new Wysg.Musm.Radium.Services.Procedures.SetCurrentStudyTechniquesProcedure(
                sp.GetService<ITechniqueRepository>()
            ));
            services.AddSingleton<Wysg.Musm.Radium.Services.Procedures.IInsertPreviousStudyProcedure>(sp => new Wysg.Musm.Radium.Services.Procedures.InsertPreviousStudyProcedure(
                sp.GetService<IRadStudyRepository>()
            ));
            services.AddSingleton<Wysg.Musm.Radium.Services.Procedures.IInsertCurrentStudyProcedure>(sp => new Wysg.Musm.Radium.Services.Procedures.InsertCurrentStudyProcedure(
                sp.GetService<IRadStudyRepository>()
            ));
            services.AddSingleton<Wysg.Musm.Radium.Services.Procedures.IInsertCurrentStudyReportProcedure>(sp => new Wysg.Musm.Radium.Services.Procedures.InsertCurrentStudyReportProcedure(
                sp.GetService<IRadStudyRepository>(),
                sp.GetService<IExportedReportsApiClient>(),
                sp.GetService<ITenantContext>()
            ));

            // ViewModels (transient) -----------------------------------------------------------
            services.AddTransient<SplashLoginViewModel>();
            services.AddTransient<SignUpViewModel>();
            // MainViewModel
            services.AddSingleton(sp => new MainViewModel(
                sp.GetRequiredService<IPhraseService>(),
                sp.GetRequiredService<ITenantContext>(),
                sp.GetRequiredService<IPhraseCache>(),
                sp.GetRequiredService<IHotkeyService>(),
                sp.GetRequiredService<ISnippetService>(),
                sp.GetService<IRadStudyRepository>(),
                sp.GetService<Wysg.Musm.Radium.Services.Procedures.INewStudyProcedure>(),
                sp.GetService<IRadiumLocalSettings>(),
                sp.GetService<Wysg.Musm.Radium.Services.Procedures.ISetStudyLockedProcedure>(),
                sp.GetService<Wysg.Musm.Radium.Services.Procedures.ISetStudyOpenedProcedure>(),
                sp.GetService<Wysg.Musm.Radium.Services.Procedures.IClearCurrentFieldsProcedure>(),
                sp.GetService<Wysg.Musm.Radium.Services.Procedures.IClearPreviousFieldsProcedure>(),
                sp.GetService<Wysg.Musm.Radium.Services.Procedures.IClearPreviousStudiesProcedure>(),
                sp.GetService<Wysg.Musm.Radium.Services.Procedures.ISetCurrentStudyTechniquesProcedure>(),
                sp.GetService<Wysg.Musm.Radium.Services.Procedures.IInsertPreviousStudyProcedure>(),
                sp.GetService<Wysg.Musm.Radium.Services.Procedures.IInsertCurrentStudyProcedure>(),
                sp.GetService<Wysg.Musm.Radium.Services.Procedures.IInsertCurrentStudyReportProcedure>(),
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

            // Snowstorm client (used by phrase extraction / SNOMED browsing)
            services.AddSingleton<ISnowstormClient, SnowstormClient>();
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
                    // Initialize AccountStoragePaths with tenant context for dynamic per-account path resolution
                    var tenant = _host.Services.GetRequiredService<ITenantContext>();
                    AccountStoragePaths.Initialize(tenant);
                    AccountStoragePaths.ConfigurePathOverrides();
                    Debug.WriteLine($"[App][LoginSuccess] AccountStoragePaths configured for AccountId={tenant.AccountId}, PacsKey={tenant.CurrentPacsKey}");

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
                catch (Exception ex) 
                { 
                    Debug.WriteLine($"[App][LoginSuccess] Error configuring paths: {ex.Message}");
                    /* Non-fatal preload error suppressed */ 
                }
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

        /// <summary>
        /// ✨ NEW: Register all 6 API clients for backend communication
        /// These replace direct database access for account-specific operations
        /// </summary>
        private void RegisterApiClients(IServiceCollection services)
        {
            // User Settings API Client
            services.AddScoped<IUserSettingsApiClient>(sp =>
            {
                var baseUrl = ResolveApiBaseUrl(sp);
                var httpClient = new System.Net.Http.HttpClient
                {
                    BaseAddress = new Uri(baseUrl),
                    Timeout = TimeSpan.FromSeconds(30)
                };
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Wysg.Musm.Radium/1.0");
                return new UserSettingsApiClient(httpClient, baseUrl);
            });

            // Phrases API Client
            services.AddScoped<IPhrasesApiClient>(sp =>
            {
                var baseUrl = ResolveApiBaseUrl(sp);
                var httpClient = new System.Net.Http.HttpClient
                {
                    BaseAddress = new Uri(baseUrl),
                    Timeout = TimeSpan.FromSeconds(30)
                };
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Wysg.Musm.Radium/1.0");
                return new PhrasesApiClient(httpClient, baseUrl);
            });

            // Hotkeys API Client
            services.AddScoped<IHotkeysApiClient>(sp =>
            {
                var baseUrl = ResolveApiBaseUrl(sp);
                var httpClient = new System.Net.Http.HttpClient
                {
                    BaseAddress = new Uri(baseUrl),
                    Timeout = TimeSpan.FromSeconds(30)
                };
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Wysg.Musm.Radium/1.0");
                return new HotkeysApiClient(httpClient, baseUrl);
            });

            // Snippets API Client
            services.AddScoped<ISnippetsApiClient>(sp =>
            {
                var baseUrl = ResolveApiBaseUrl(sp);
                var httpClient = new System.Net.Http.HttpClient
                {
                    BaseAddress = new Uri(baseUrl),
                    Timeout = TimeSpan.FromSeconds(30)
                };
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Wysg.Musm.Radium/1.0");
                return new SnippetsApiClient(httpClient, baseUrl);
            });

            // SNOMED API Client - Singleton to match ISnomedMapService lifecycle
            services.AddSingleton<ISnomedApiClient>(sp =>
            {
                var baseUrl = ResolveApiBaseUrl(sp);
                var httpClient = new System.Net.Http.HttpClient
                {
                    BaseAddress = new Uri(baseUrl),
                    Timeout = TimeSpan.FromSeconds(30)
                };
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Wysg.Musm.Radium/1.0");
                return new SnomedApiClient(httpClient, baseUrl);
            });

            // Exported Reports API Client
            services.AddScoped<IExportedReportsApiClient>(sp =>
            {
                var baseUrl = ResolveApiBaseUrl(sp);
                var httpClient = new System.Net.Http.HttpClient
                {
                    BaseAddress = new Uri(baseUrl),
                    Timeout = TimeSpan.FromSeconds(30)
                };
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Wysg.Musm.Radium/1.0");
                return new ExportedReportsApiClient(httpClient, baseUrl);
            });
        }

        private static string ResolveApiBaseUrl(IServiceProvider sp)
        {
            var settings = sp.GetRequiredService<IRadiumLocalSettings>();
            return ResolveApiBaseUrl(settings);
        }

        private static string ResolveApiBaseUrl(IRadiumLocalSettings settings)
        {
            var raw = settings.ApiBaseUrl;
            if (string.IsNullOrWhiteSpace(raw)) raw = "http://127.0.0.1:5205/"; // UI default
            return raw.TrimEnd('/') + "/";
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
