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
    public partial class SplashLoginViewModel : BaseViewModel
    {
        private readonly IAuthService _auth;
        private readonly ISupabaseService _supabase;
        private readonly ITenantContext _tenantContext;
        private readonly IAuthStorage _storage;

        [ObservableProperty]
        private bool _isLoading = true;

        [ObservableProperty]
        private bool _showLogin = false;

        [ObservableProperty]
        private string _email = string.Empty;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private string _statusMessage = "Loading...";

        [ObservableProperty]
        private bool _rememberMe = true;

        public IRelayCommand LoginCommand { get; }
        public IRelayCommand GoogleLoginCommand { get; }
        public IRelayCommand SignUpCommand { get; }
        public IRelayCommand TestCentralCommand { get; }

        public SplashLoginViewModel(IAuthService auth, ISupabaseService supabase, ITenantContext tenantContext, IAuthStorage storage)
        {
            _auth = auth;
            _supabase = supabase;
            _tenantContext = tenantContext;
            _storage = storage;

            RememberMe = _storage.RememberMe || true; // default checked
            if (_storage.Email != null) Email = _storage.Email;

            LoginCommand = new AsyncRelayCommand<object?>(OnEmailLoginAsync);
            GoogleLoginCommand = new AsyncRelayCommand(OnGoogleLoginAsync);
            SignUpCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(OnOpenSignUp);
            TestCentralCommand = new AsyncRelayCommand(OnTestCentralAsync);

            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            try
            {
                Debug.WriteLine("[Splash][Init] Begin T=" + Environment.CurrentManagedThreadId);
                await Task.Delay(300);

                var storedRt = _storage.RefreshToken;
                if (!string.IsNullOrWhiteSpace(storedRt))
                {
                    StatusMessage = "Restoring session...";
                    Debug.WriteLine("[Splash][Init] Attempt silent refresh");
                    var refreshed = await _auth.RefreshAsync(storedRt!);
                    Debug.WriteLine($"[Splash][Init] Refresh result Success={refreshed.Success}");
                    if (refreshed.Success)
                    {
                        try
                        {
                            Debug.WriteLine("[Splash][Init] EnsureAccountAsync start for user=" + refreshed.UserId);
                            long accountId = await _supabase.EnsureAccountAsync(refreshed.UserId, _storage.Email ?? string.Empty, _storage.DisplayName ?? string.Empty);

                            // Load reportify settings (central)
                            try
                            {
                                var rptSvc = ((App)Application.Current).Services.GetService(typeof(IReportifySettingsService)) as IReportifySettingsService;
                                if (rptSvc != null)
                                {
                                    var json = await rptSvc.GetSettingsJsonAsync(accountId);
                                    _tenantContext.ReportifySettingsJson = json;
                                    Debug.WriteLine("[Splash][Init] Loaded reportify settings len=" + (json?.Length ?? 0));
                                }
                            }
                            catch { }

                            // Set tenant + signal success BEFORE last-login update
                            _tenantContext.TenantId = accountId;
                            _tenantContext.TenantCode = refreshed.UserId;
                            Debug.WriteLine("[Splash][Init] Silent login success (tenant set)");
                            LoginSuccess?.Invoke();

                            // Fire & forget last login update (silent) so UI isn't blocked
                            BackgroundTask.Run("LastLoginUpdate", () => _supabase.UpdateLastLoginAsync(accountId, silent: true));
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

                IsLoading = false;
                ShowLogin = true;
                StatusMessage = "Please log in to continue";
                Debug.WriteLine("[Splash][Init] Show login UI");
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

                var accountId = await _supabase.EnsureAccountAsync(auth.UserId, auth.Email, auth.DisplayName);
                await _supabase.UpdateLastLoginAsync(accountId); // interactive login keep errors visible
                _tenantContext.TenantId = accountId;
                _tenantContext.TenantCode = auth.UserId;

                // Load reportify settings after login
                try
                {
                    var rptSvc = ((App)Application.Current).Services.GetService(typeof(IReportifySettingsService)) as IReportifySettingsService;
                    if (rptSvc != null)
                    {
                        var json = await rptSvc.GetSettingsJsonAsync(accountId);
                        _tenantContext.ReportifySettingsJson = json;
                        Debug.WriteLine("[Splash][Login] Loaded reportify settings len=" + (json?.Length ?? 0));
                    }
                }
                catch { }

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

                var accountId = await _supabase.EnsureAccountAsync(auth.UserId, auth.Email, auth.DisplayName);
                await _supabase.UpdateLastLoginAsync(accountId); // interactive login keep errors visible
                _tenantContext.TenantId = accountId;
                _tenantContext.TenantCode = auth.UserId;

                // Load reportify settings after login
                try
                {
                    var rptSvc = ((App)Application.Current).Services.GetService(typeof(IReportifySettingsService)) as IReportifySettingsService;
                    if (rptSvc != null)
                    {
                        var json = await rptSvc.GetSettingsJsonAsync(accountId);
                        _tenantContext.ReportifySettingsJson = json;
                        Debug.WriteLine("[Splash][Login] Loaded reportify settings len=" + (json?.Length ?? 0));
                    }
                }
                catch { }

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
                var (ok, msg) = await _supabase.TestConnectionAsync();
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

        public event Action? LoginSuccess;
    }
}