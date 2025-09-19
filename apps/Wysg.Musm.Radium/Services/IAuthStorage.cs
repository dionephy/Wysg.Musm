namespace Wysg.Musm.Radium.Services
{
    public interface IAuthStorage
    {
        string? RefreshToken { get; set; }
        string? Email { get; set; }
        string? DisplayName { get; set; }
        bool RememberMe { get; set; }
    }
}
