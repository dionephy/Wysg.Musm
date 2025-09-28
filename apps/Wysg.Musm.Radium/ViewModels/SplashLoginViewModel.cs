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
            await Task.Delay(300);

            // Try silent login using refresh token if present
            var storedRt = _storage.RefreshToken;
            if (!string.IsNullOrWhiteSpace(storedRt))
            {
                StatusMessage = "Restoring session...";
                var refreshed = await _auth.RefreshAsync(storedRt!);
                if (refreshed.Success)
                {
                    try
                    {
                        long accountId = await _supabase.EnsureAccountAsync(refreshed.UserId, _storage.Email ?? string.Empty, _storage.DisplayName ?? string.Empty);
                        await _supabase.UpdateLastLoginAsync(accountId);
                        _tenantContext.TenantId = accountId;
                        _tenantContext.TenantCode = refreshed.UserId;
                        LoginSuccess?.Invoke();
                        return;
                    }
                    catch (Exception ex)
                    {
                        ErrorMessage = $"Session restore failed: {ex.Message}";
                    }
                }
            }

            IsLoading = false;
            ShowLogin = true;
            StatusMessage = "Please log in to continue";
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
                var auth = await _auth.SignInWithEmailPasswordAsync(Email.Trim(), password);
                if (!auth.Success) { ErrorMessage = auth.Error; return; }

                var accountId = await _supabase.EnsureAccountAsync(auth.UserId, auth.Email, auth.DisplayName);
                await _supabase.UpdateLastLoginAsync(accountId);
                _tenantContext.TenantId = accountId;
                _tenantContext.TenantCode = auth.UserId;

                PersistAuth(auth);
                LoginSuccess?.Invoke();
            }
            catch (Exception ex)
            {
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
                var auth = await _auth.SignInWithGoogleAsync();
                if (!auth.Success) { ErrorMessage = auth.Error; return; }

                var accountId = await _supabase.EnsureAccountAsync(auth.UserId, auth.Email, auth.DisplayName);
                await _supabase.UpdateLastLoginAsync(accountId);
                _tenantContext.TenantId = accountId;
                _tenantContext.TenantCode = auth.UserId;

                PersistAuth(auth);
                LoginSuccess?.Invoke();
            }
            catch (Exception ex)
            {
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
                var (ok, msg) = await _supabase.TestConnectionAsync();
                StatusMessage = ok ? msg : $"Central DB error: {msg}";
            }
            finally { IsBusy = false; }
        }

        public event Action? LoginSuccess;
    }
}