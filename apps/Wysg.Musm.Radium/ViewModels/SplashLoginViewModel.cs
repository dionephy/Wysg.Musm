using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Wysg.Musm.Infrastructure.ViewModels;
using Wysg.Musm.Radium.Services;
using Wysg.Musm.Radium.Services.Diagnostics; // added

namespace Wysg.Musm.Radium.ViewModels
{
    /// <summary>
    /// ViewModel driving the splash + authentication experience.
    /// Responsibilities:
    ///   * Attempt a silent session restoration using a persisted refresh token.
    ///   * On success: ensure (or create) a central account record, load per-account Reportify settings, populate tenant context, fire LoginSuccess.
    ///   * On failure: present interactive login UI (email/password + Google OAuth) and surface validation / error messages.
    ///   * Provide a quick "Test Central" connectivity check before user credentials are entered (helps diagnose network / firewall issues).
    ///
    /// UX flow:
    ///   1. Splash shows loading spinner.
    ///   2. If refresh token present -> RefreshAsync -> EnsureAccountAsync -> load settings -> set ITenantContext -> signal success -> main window.
    ///   3. Else show credential form; user logs in; same ensure + settings load path.
    ///
    /// Design choices:
    ///   * Fire-and-forget background task for last login timestamp so UI remains responsive.
    ///   * Separation of concerns: token persistence abstracted via IAuthStorage; central DB logic kept in ISupabaseService.
    ///   * Minimal retries here (fail fast) ? deeper network retry logic lives inside lower-level services when appropriate.
    /// </summary>
    public partial class SplashLoginViewModel : BaseViewModel
    {
        private readonly IAuthService _auth;
        private readonly AzureSqlCentralService _central; // replaced ISupabaseService
        private readonly ITenantContext _tenantContext;
        private readonly IAuthStorage _storage;

        [ObservableProperty]
        private bool _isLoading = true;  // true while attempting silent restore
        [ObservableProperty]
        private bool _showLogin = false; // switches UI from spinner to login form
        [ObservableProperty]
        private string _email = string.Empty; // bound email entry (pre-filled from storage if available)
        [ObservableProperty]
        private string _errorMessage = string.Empty; // validation or terminal error
        [ObservableProperty]
        private string _statusMessage = "Loading..."; // progressive status messaging
        [ObservableProperty]
        private bool _rememberMe = true; // whether to persist refresh token

        public IRelayCommand LoginCommand { get; }
        public IRelayCommand GoogleLoginCommand { get; }
        public IRelayCommand SignUpCommand { get; }
        public IRelayCommand TestCentralCommand { get; }

        public SplashLoginViewModel(IAuthService auth, AzureSqlCentralService central, ITenantContext tenantContext, IAuthStorage storage)
        {
            _auth = auth;
            _central = central;
            _tenantContext = tenantContext;
            _storage = storage;

            RememberMe = _storage.RememberMe || true; // ensure checkbox starts checked
            if (_storage.Email != null) Email = _storage.Email;

            LoginCommand = new AsyncRelayCommand<object?>(OnEmailLoginAsync);
            GoogleLoginCommand = new AsyncRelayCommand(OnGoogleLoginAsync);
            SignUpCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(OnOpenSignUp);
            TestCentralCommand = new AsyncRelayCommand(OnTestCentralAsync);

            _ = InitializeAsync(); // kick off silent restore; no await in ctor
        }

        /// <summary>
        /// Silent initialization path invoked on splash load. Attempts token refresh -> account ensure -> settings load.
        /// Falls back to interactive login UI on any failure (user still able to proceed manually).
        /// </summary>
        private async Task InitializeAsync()
        {
            var swAll = Stopwatch.StartNew();
            try
            {
                Debug.WriteLine("[Splash][Init] Begin T=" + Environment.CurrentManagedThreadId);
                StatusMessage = "Initializing...";
                await Task.Delay(120); // smaller visual delay just to paint UI

                var storedRt = _storage.RefreshToken;
                if (!string.IsNullOrWhiteSpace(storedRt))
                {
                    var swStage = Stopwatch.StartNew();
                    StatusMessage = "Restoring session (refresh token)...";
                    Debug.WriteLine("[Splash][Init][Stage] Refresh start");
                    var refreshed = await _auth.RefreshAsync(storedRt!);
                    swStage.Stop();
                    Debug.WriteLine($"[Splash][Init][Stage] Refresh done success={refreshed.Success} ms={swStage.ElapsedMilliseconds}");
                    if (refreshed.Success)
                    {
                        try
                        {
                            // Ensure account
                            swStage.Restart();
                            StatusMessage = "Ensuring account...";
                            Debug.WriteLine("[Splash][Init][Stage] EnsureAccount start user=" + refreshed.UserId);
                            var combined = await _central.EnsureAccountAndGetSettingsAsync(refreshed.UserId, _storage.Email ?? string.Empty, _storage.DisplayName ?? string.Empty);
                            var accountId = combined.accountId;
                            swStage.Stop();
                            Debug.WriteLine($"[Splash][Init][Stage] Ensure+Settings account={accountId} ms={swStage.ElapsedMilliseconds} settingsLen={(combined.settingsJson?.Length ?? 0)}");

                            // Apply settings immediately
                            _tenantContext.ReportifySettingsJson = combined.settingsJson;

                            // Set tenant context
                            StatusMessage = "Loading phrases...";
                            _tenantContext.TenantId = accountId;
                            _tenantContext.TenantCode = refreshed.UserId;

                            // Preload phrases BEFORE signaling success as requested
                            try
                            {
                                var phraseSvc = ((App)Application.Current).Services.GetService(typeof(IPhraseService)) as IPhraseService;
                                if (phraseSvc != null)
                                {
                                    var swP = Stopwatch.StartNew();
                                    await phraseSvc.PreloadAsync(accountId);
                                    swP.Stop();
                                    Debug.WriteLine($"[Splash][Init][Stage] Phrase preload ms={swP.ElapsedMilliseconds}");
                                }
                            }
                            catch (Exception pex) { Debug.WriteLine("[Splash][Init][Stage][PhrasePreload][WARN] " + pex.Message); }

                            StatusMessage = "Finalizing session...";
                            Debug.WriteLine("[Splash][Init] Silent login success (tenant set) with phrases");
                            LoginSuccess?.Invoke();

                            // Background last login update
                            BackgroundTask.Run("LastLoginUpdate", () => _central.UpdateLastLoginAsync(accountId, silent: true));
                            return;
                        }
                        catch (OperationCanceledException oce)
                        {
                            Debug.WriteLine("[Splash][Init][CANCEL] " + oce);
                            ErrorMessage = "Session restore canceled.";
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("[Splash][Init][EX] " + ex);
                            ErrorMessage = $"Session restore failed: {ex.Message}";
                        }
                    }
                }

                // Fallback to interactive login
                IsLoading = false;
                ShowLogin = true;
                StatusMessage = string.IsNullOrEmpty(ErrorMessage) ? "Please log in to continue" : ErrorMessage;
                Debug.WriteLine("[Splash][Init] Show login UI (silent restore path ended) totalMs=" + swAll.ElapsedMilliseconds);
            }
            catch (OperationCanceledException oce)
            {
                Debug.WriteLine("[Splash][Init][CANCEL-OUTER] " + oce);
                ErrorMessage = "Initialization canceled.";
                IsLoading = false; ShowLogin = true; StatusMessage = "Retry login";
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[Splash][Init][EX-OUTER] " + ex);
                ErrorMessage = ex.Message;
                IsLoading = false; ShowLogin = true; StatusMessage = "Error - login manually";
            }
        }

        /// <summary>
        /// Handles email/password authentication path. Ensures account, updates last login (interactive error surfaced),
        /// loads reportify settings, persists auth context (if RememberMe).  Raises LoginSuccess on completion.
        /// </summary>
        private async Task OnEmailLoginAsync(object? parameter)
        {
            if (parameter is not PasswordBox pw) { ErrorMessage = "Unexpected password input."; return; }
            var password = pw.Password ?? string.Empty;
            if (string.IsNullOrWhiteSpace(Email)) { ErrorMessage = "E-mail is required."; return; }
            if (string.IsNullOrWhiteSpace(password)) { ErrorMessage = "Password is required."; return; }

            IsBusy = true;
            ErrorMessage = string.Empty;
            try
            {
                StatusMessage = "Signing in...";
                Debug.WriteLine("[Splash][Login] Email sign-in start email=" + Email);
                var auth = await _auth.SignInWithEmailPasswordAsync(Email.Trim(), password);
                Debug.WriteLine("[Splash][Login] Auth success=" + auth.Success);
                if (!auth.Success) { ErrorMessage = auth.Error; return; }

                var accountId = await _central.EnsureAccountAsync(auth.UserId, auth.Email, auth.DisplayName);
                await _central.UpdateLastLoginAsync(accountId); // interactive login keep errors visible
                _tenantContext.TenantId = accountId;
                _tenantContext.TenantCode = auth.UserId;

                // Load reportify settings after login
                try
                {
                    var combined = await _central.EnsureAccountAndGetSettingsAsync(auth.UserId, auth.Email, auth.DisplayName);
                    _tenantContext.ReportifySettingsJson = combined.settingsJson;
                    Debug.WriteLine("[Splash][Login] Loaded reportify settings len=" + (combined.settingsJson?.Length ?? 0));
                }
                catch { }

                // Preload phrases before closing splash (requirement)
                try
                {
                    StatusMessage = "Loading phrases...";
                    var phraseSvc = ((App)Application.Current).Services.GetService(typeof(IPhraseService)) as IPhraseService;
                    if (phraseSvc != null)
                        await phraseSvc.PreloadAsync(accountId);
                }
                catch (Exception px) { Debug.WriteLine("[Splash][Login][PhrasePreload][WARN] " + px.Message); }

                PersistAuth(auth);
                Debug.WriteLine("[Splash][Login] Success account=" + accountId);
                LoginSuccess?.Invoke();
            }
            catch (OperationCanceledException oce)
            {
                Debug.WriteLine("[Splash][Login][CANCEL] " + oce);
                ErrorMessage = "Login canceled.";
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[Splash][Login][EX] " + ex);
                ErrorMessage = $"Login error: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Handles Google OAuth sign-in path. Mirrors email/password flow with different credential source.
        /// </summary>
        private async Task OnGoogleLoginAsync()
        {
            IsBusy = true;
            ErrorMessage = string.Empty;
            try
            {
                StatusMessage = "Opening Google sign-in...";
                Debug.WriteLine("[Splash][Google] Start");
                var auth = await _auth.SignInWithGoogleAsync();
                Debug.WriteLine("[Splash][Google] Auth success=" + auth.Success);
                if (!auth.Success) { ErrorMessage = auth.Error; return; }

                var accountId = await _central.EnsureAccountAsync(auth.UserId, auth.Email, auth.DisplayName);
                await _central.UpdateLastLoginAsync(accountId); // interactive login keep errors visible
                _tenantContext.TenantId = accountId;
                _tenantContext.TenantCode = auth.UserId;

                try
                {
                    var combined = await _central.EnsureAccountAndGetSettingsAsync(auth.UserId, auth.Email, auth.DisplayName);
                    _tenantContext.ReportifySettingsJson = combined.settingsJson;
                    Debug.WriteLine("[Splash][Login] Loaded reportify settings len=" + (combined.settingsJson?.Length ?? 0));
                }
                catch { }

                try
                {
                    StatusMessage = "Loading phrases...";
                    var phraseSvc = ((App)Application.Current).Services.GetService(typeof(IPhraseService)) as IPhraseService;
                    if (phraseSvc != null)
                        await phraseSvc.PreloadAsync(accountId);
                }
                catch (Exception px) { Debug.WriteLine("[Splash][Google][PhrasePreload][WARN] " + px.Message); }

                PersistAuth(auth);
                Debug.WriteLine("[Splash][Google] Success account=" + accountId);
                LoginSuccess?.Invoke();
            }
            catch (OperationCanceledException oce)
            {
                Debug.WriteLine("[Splash][Google][CANCEL] " + oce);
                ErrorMessage = "Google login canceled.";
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[Splash][Google][EX] " + ex);
                ErrorMessage = $"Google login error: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>Persists authentication artifacts (refresh token, email, display name) based on RememberMe choice.</summary>
        private void PersistAuth(Wysg.Musm.Radium.Models.AuthResult auth)
        {
            _storage.RememberMe = RememberMe;
            if (RememberMe)
            {
                _storage.RefreshToken = string.IsNullOrWhiteSpace(auth.RefreshToken) ? _storage.RefreshToken : auth.RefreshToken;
                _storage.Email = string.IsNullOrWhiteSpace(auth.Email) ? Email : auth.Email;
                _storage.DisplayName = string.IsNullOrWhiteSpace(auth.DisplayName) ? _storage.DisplayName : auth.DisplayName;
            }
            else
            {
                _storage.RefreshToken = null;
            }
        }

        private void OnOpenSignUp()
        {
            var provider = ((App)Application.Current).Services;
            var vm = provider.GetService<Wysg.Musm.Radium.ViewModels.SignUpViewModel>()!;
            var owner = Application.Current.MainWindow ?? (Application.Current.Windows.Count > 0 ? Application.Current.Windows[0] : null);
            var win = new Wysg.Musm.Radium.Views.SignUpWindow(vm) { Owner = owner };
            win.ShowDialog();
        }

        private async Task OnTestCentralAsync()
        {
            IsBusy = true; ErrorMessage = string.Empty; StatusMessage = "Testing central DB...";
            try
            {
                Debug.WriteLine("[Splash][TestCentral] Start");
                var (ok, msg) = await _central.TestConnectionAsync();
                StatusMessage = ok ? msg : $"Central DB error: {msg}";
                Debug.WriteLine($"[Splash][TestCentral] Done ok={ok} msg={msg}");
            }
            catch (OperationCanceledException oce)
            {
                Debug.WriteLine("[Splash][TestCentral][CANCEL] " + oce);
                StatusMessage = "Central test canceled";
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[Splash][TestCentral][EX] " + ex);
                StatusMessage = "Central test error: " + ex.Message;
            }
            finally { IsBusy = false; }
        }

        public event Action? LoginSuccess; // Raised after tenant context set & settings loaded
    }
}