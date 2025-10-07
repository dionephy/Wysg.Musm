using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using System.Windows.Controls;
using Wysg.Musm.Radium.Services;

namespace Wysg.Musm.Radium.ViewModels
{
    public partial class SignUpViewModel : ObservableObject
    {
        private readonly IAuthService _auth;
        private readonly AzureSqlCentralService _central;

        private string _email = string.Empty;
        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        private string _displayName = string.Empty;
        public string DisplayName
        {
            get => _displayName;
            set => SetProperty(ref _displayName, value);
        }

        private string _errorMessage = string.Empty;
        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public IRelayCommand SignUpCommand { get; }

        public SignUpViewModel(IAuthService auth, AzureSqlCentralService central)
        {
            _auth = auth;
            _central = central;
            SignUpCommand = new AsyncRelayCommand<object?>(OnSignUpAsync);
        }

        private async Task OnSignUpAsync(object? parameter)
        {
            if (parameter is not PasswordBox pw)
            {
                ErrorMessage = "Unexpected password input.";
                return;
            }
            var password = pw.Password ?? string.Empty;
            if (string.IsNullOrWhiteSpace(Email)) { ErrorMessage = "E-mail is required."; return; }
            if (string.IsNullOrWhiteSpace(password)) { ErrorMessage = "Password is required."; return; }

            ErrorMessage = string.Empty;
            var result = await _auth.SignUpWithEmailPasswordAsync(Email.Trim(), password, string.IsNullOrWhiteSpace(DisplayName) ? null : DisplayName.Trim());
            if (!result.Success)
            {
                ErrorMessage = result.Error;
                return;
            }

            // Create account row centrally
            await _central.EnsureAccountAsync(result.UserId, result.Email, result.DisplayName);

            // Close the window on success
            System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
            {
                foreach (var win in System.Windows.Application.Current!.Windows)
                {
                    if (win is System.Windows.Window w && w.DataContext == this) { w.DialogResult = true; w.Close(); break; }
                }
            });
        }
    }
}
