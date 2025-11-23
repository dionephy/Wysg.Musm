using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Wysg.Musm.Radium.Services;

namespace Wysg.Musm.Radium.ViewModels
{
    /// <summary>
    /// Example login ViewModel showing how to integrate API token management
    /// Use this as a reference for updating your actual SplashLoginViewModel
    /// </summary>
    public partial class LoginViewModelExample : ObservableObject
    {
        private readonly IAuthService _authService;
        private readonly ITenantContext _tenantContext;
        private readonly ApiTokenManager _apiTokenManager;
        private readonly ApiTestService _apiTestService;
        private readonly IServiceProvider _serviceProvider;

        [ObservableProperty]
        private string _email = string.Empty;

        [ObservableProperty]
        private string _statusMessage = "Ready to login";

        [ObservableProperty]
        private bool _isBusy = false;

        public IRelayCommand LoginCommand { get; }
        public IRelayCommand TestApiCommand { get; }

        public LoginViewModelExample(
            IAuthService authService,
            ITenantContext tenantContext,
            ApiTokenManager apiTokenManager,
            ApiTestService apiTestService,
            IServiceProvider serviceProvider)
        {
            _authService = authService;
            _tenantContext = tenantContext;
            _apiTokenManager = apiTokenManager;
            _apiTestService = apiTestService;
            _serviceProvider = serviceProvider;

            LoginCommand = new AsyncRelayCommand<string>(OnLoginAsync);
            TestApiCommand = new AsyncRelayCommand(OnTestApiAsync);
        }

        /// <summary>
        /// Email/Password login with API token management
        /// </summary>
        private async Task OnLoginAsync(string? password)
        {
            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(password))
            {
                StatusMessage = "Email and password required";
                return;
            }

            IsBusy = true;
            StatusMessage = "Signing in...";

            try
            {
                Debug.WriteLine($"[Login] Attempting login for {Email}");

                // Step 1: Authenticate with Firebase
                var authResult = await _authService.SignInWithEmailPasswordAsync(Email, password);

                if (!authResult.Success)
                {
                    StatusMessage = $"Login failed: {authResult.Error}";
                    Debug.WriteLine($"[Login] ? Authentication failed: {authResult.Error}");
                    return;
                }

                Debug.WriteLine($"[Login] ? Firebase authentication successful");
                Debug.WriteLine($"[Login] UID: {authResult.UserId}");
                Debug.WriteLine($"[Login] Token length: {authResult.IdToken?.Length ?? 0}");

                // Step 2: Set Firebase token on all API clients
                StatusMessage = "Setting up API connection...";
                _apiTokenManager.SetAuthToken(authResult.IdToken);

                // Step 3: Verify API is reachable
                StatusMessage = "Verifying API connection...";
                var apiAvailable = await _apiTokenManager.VerifyApiConnectionAsync();

                if (!apiAvailable)
                {
                    Debug.WriteLine("[Login] ?? API not available, but continuing with login");
                    // Don't fail login if API is temporarily unavailable
                }

                // Step 4: Set tenant context (this might trigger data loading)
                StatusMessage = "Loading user data...";
                _tenantContext.AccountId = 1; // Replace with actual account ID from your logic
                _tenantContext.TenantCode = authResult.UserId;

                // Step 5: (Optional) Run comprehensive API tests
                if (Debugger.IsAttached)
                {
                    Debug.WriteLine("[Login] Running API tests (debug mode)...");
                    await _apiTestService.RunAllTestsAsync();
                }

                // Step 6: Success!
                StatusMessage = "Login successful!";
                Debug.WriteLine("[Login] ? Login completed successfully");

                // Raise event or navigate to main window
                OnLoginSuccess();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
                Debug.WriteLine($"[Login] ? Unexpected error: {ex}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Test API connection (for debugging)
        /// </summary>
        private async Task OnTestApiAsync()
        {
            IsBusy = true;
            StatusMessage = "Testing API...";

            try
            {
                Debug.WriteLine("[Test] Starting API connectivity test...");

                var success = await _apiTestService.RunAllTestsAsync();

                if (success)
                {
                    StatusMessage = "? All API tests passed!";
                    MessageBox.Show("API connection successful!\n\nCheck Debug output for details.", 
                                  "Test Successful", 
                                  MessageBoxButton.OK, 
                                  MessageBoxImage.Information);
                }
                else
                {
                    StatusMessage = "? Some tests failed";
                    MessageBox.Show("Some API tests failed.\n\nCheck Debug output for details.", 
                                  "Test Failed", 
                                  MessageBoxButton.OK, 
                                  MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Test error: {ex.Message}";
                Debug.WriteLine($"[Test] ? Error: {ex}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Called when login is successful
        /// Override or replace with your navigation logic
        /// </summary>
        protected virtual void OnLoginSuccess()
        {
            Debug.WriteLine("[Login] OnLoginSuccess called");
            // Example: Close login window, open main window
            // Application.Current.MainWindow.Show();
        }

        /// <summary>
        /// Logout and clear API tokens
        /// </summary>
        public async Task LogoutAsync()
        {
            try
            {
                Debug.WriteLine("[Login] Logging out...");

                // Clear API tokens
                _apiTokenManager.ClearAuthTokens();

                // Clear tenant context
                _tenantContext.AccountId = 0;
                _tenantContext.TenantCode = string.Empty;

                // Sign out from Firebase
                await _authService.SignOutAsync();

                StatusMessage = "Logged out";
                Debug.WriteLine("[Login] ? Logout complete");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Login] Logout error: {ex.Message}");
            }
        }
    }
}
