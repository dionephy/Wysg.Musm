namespace Wysg.Musm.Radium.Services
{
    public interface IAuthService
    {
        Task<Wysg.Musm.Radium.Models.AuthResult> SignInWithGoogleAsync();
        Task<Wysg.Musm.Radium.Models.AuthResult> SignInWithEmailPasswordAsync(string email, string password);
        Task<Wysg.Musm.Radium.Models.AuthResult> SignUpWithEmailPasswordAsync(string email, string password, string? displayName);
        Task<Wysg.Musm.Radium.Models.AuthResult> RefreshAsync(string refreshToken);
        Task SignOutAsync();
    }
}
