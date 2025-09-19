using System;

namespace Wysg.Musm.Radium.Models
{
    public sealed class AuthResult
    {
        public bool Success { get; init; }
        public string UserId { get; init; } = string.Empty; // Firebase UID
        public string Email { get; init; } = string.Empty;
        public string DisplayName { get; init; } = string.Empty;
        public string IdToken { get; init; } = string.Empty; // Firebase ID token (JWT)
        public string RefreshToken { get; init; } = string.Empty; // Firebase refresh token
        public string Error { get; init; } = string.Empty;

        public static AuthResult Ok(string uid, string email, string name, string idToken, string refreshToken)
            => new AuthResult { Success = true, UserId = uid, Email = email, DisplayName = name, IdToken = idToken, RefreshToken = refreshToken };

        public static AuthResult Fail(string error)
            => new AuthResult { Success = false, Error = error };
    }
}
