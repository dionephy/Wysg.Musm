using System.Security.Claims;

namespace Wysg.Musm.Radium.Api.Extensions
{
    /// <summary>
    /// Extension methods for ClaimsPrincipal to extract Firebase user context
    /// </summary>
    public static class ClaimsPrincipalExtensions
    {
        /// <summary>
        /// Get the Firebase UID from claims
        /// </summary>
        public static string? GetFirebaseUid(this ClaimsPrincipal principal)
        {
            return principal.FindFirst("firebase_uid")?.Value
                ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        /// <summary>
        /// Get the account ID from claims (custom claim set in Firebase)
        /// </summary>
        public static long? GetAccountId(this ClaimsPrincipal principal)
        {
            var accountIdClaim = principal.FindFirst("account_id")?.Value;
            return long.TryParse(accountIdClaim, out var accountId) ? accountId : null;
        }

        /// <summary>
        /// Get the tenant ID from claims (for multi-tenancy)
        /// </summary>
        public static string? GetTenantId(this ClaimsPrincipal principal)
        {
            return principal.FindFirst("tenant_id")?.Value;
        }

        /// <summary>
        /// Get the user's email from claims
        /// </summary>
        public static string? GetEmail(this ClaimsPrincipal principal)
        {
            return principal.FindFirst(ClaimTypes.Email)?.Value;
        }

        /// <summary>
        /// Get the user's role from claims
        /// </summary>
        public static string? GetRole(this ClaimsPrincipal principal)
        {
            return principal.FindFirst(ClaimTypes.Role)?.Value;
        }

        /// <summary>
        /// Check if email is verified
        /// </summary>
        public static bool IsEmailVerified(this ClaimsPrincipal principal)
        {
            var verified = principal.FindFirst("email_verified")?.Value;
            return bool.TryParse(verified, out var result) && result;
        }
    }
}
