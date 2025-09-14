using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Wysg.Musm.Infrastructure.ViewModels;
using Wysg.Musm.Radium.Models;
using Wysg.Musm.Radium.Services;

namespace Wysg.Musm.Radium.ViewModels
{
    public partial class SplashLoginViewModel : BaseViewModel
    {
        private readonly ITenantService _tenantService;

        [ObservableProperty]
        private bool _isLoading = true;

        [ObservableProperty]
        private bool _showLogin = false;

        [ObservableProperty]
        private string _tenantCode = "dev"; // 기본값을 dev로 설정

        [ObservableProperty]
        private string _userName = string.Empty;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private string _statusMessage = "Loading...";

        public IRelayCommand LoginCommand { get; }

        public SplashLoginViewModel(ITenantService tenantService)
        {
            _tenantService = tenantService;
            LoginCommand = new AsyncRelayCommand(OnLoginAsync);
            
            // 초기화 후 잠시 대기 후 로그인 화면 표시
            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            await Task.Delay(1500); // 스플래시 시간
            
            // dev 테넌트면 자동 로그인
            if (TenantCode == "dev")
            {
                StatusMessage = "Bypassing login for dev tenant...";
                await Task.Delay(500);
                await OnLoginAsync();
                return;
            }

            IsLoading = false;
            ShowLogin = true;
            StatusMessage = "Please login to continue";
        }

        private async Task OnLoginAsync()
        {
            if (string.IsNullOrWhiteSpace(TenantCode))
            {
                ErrorMessage = "Tenant code is required.";
                return;
            }

            IsBusy = true;
            ErrorMessage = string.Empty;

            try
            {
                var request = new LoginRequest
                {
                    TenantCode = TenantCode,
                    UserName = UserName
                };

                bool isValid = await _tenantService.ValidateLoginAsync(request);
                
                if (isValid)
                {
                    OnLoginSuccess();
                }
                else
                {
                    ErrorMessage = "Invalid tenant code or login failed.";
                }
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

        public event Action? LoginSuccess;

        private void OnLoginSuccess()
        {
            LoginSuccess?.Invoke();
        }
    }
}