using Google.Apis.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace Wysg.Musm.Radium.Api.Authentication
{
    /// <summary>
    /// Firebase JWT Bearer authentication handler
    /// Validates Firebase ID tokens using Google's library
    /// </summary>
    public sealed class FirebaseAuthenticationHandler : AuthenticationHandler<JwtBearerOptions>
    {
        private readonly Configuration.FirebaseSettings _firebaseSettings;

        public FirebaseAuthenticationHandler(
            IOptionsMonitor<JwtBearerOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            IOptions<Configuration.FirebaseSettings> firebaseSettings)
            : base(options, logger, encoder)
        {
            _firebaseSettings = firebaseSettings.Value;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            // Skip authentication if configured
            if (!_firebaseSettings.ValidateToken)
            {
                Logger.LogWarning("Firebase token validation is DISABLED - using development mode");
                
                // Extract UID from Authorization header or use default
                // This allows testing with real Firebase UIDs without validation
                string uid = "dev-user-123"; // Default fallback
                string email = "dev@example.com";
                string name = "Dev User";
                
                if (Request.Headers.TryGetValue("Authorization", out var devAuthHeader))
                {
                    var devToken = devAuthHeader.ToString().Replace("Bearer ", "").Trim();
                    if (!string.IsNullOrEmpty(devToken) && devToken != "Bearer")
                    {
                        // Try to decode the JWT to get real claims (even though we don't validate it)
                        try
                        {
                            var parts = devToken.Split('.');
                            if (parts.Length == 3)
                            {
                                var payload = parts[1];
                                // Base64 URL decode
                                payload = payload.Replace('-', '+').Replace('_', '/');
                                switch (payload.Length % 4)
                                {
                                    case 2: payload += "=="; break;
                                    case 3: payload += "="; break;
                                }
                                
                                var payloadJson = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(payload));
                                var jsonDoc = System.Text.Json.JsonDocument.Parse(payloadJson);
                                
                                if (jsonDoc.RootElement.TryGetProperty("user_id", out var uidProp))
                                    uid = uidProp.GetString() ?? uid;
                                if (jsonDoc.RootElement.TryGetProperty("email", out var emailProp))
                                    email = emailProp.GetString() ?? email;
                                if (jsonDoc.RootElement.TryGetProperty("name", out var nameProp))
                                    name = nameProp.GetString() ?? name;
                                
                                Logger.LogInformation("Development mode: Extracted UID={Uid}, Email={Email} from JWT payload", uid, email);
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.LogWarning(ex, "Failed to decode JWT in development mode, using defaults");
                        }
                    }
                }
                
                var claims = new[]
                {
                    new Claim("firebase_uid", uid),
                    new Claim(ClaimTypes.NameIdentifier, uid),
                    new Claim(ClaimTypes.Name, name),
                    new Claim(ClaimTypes.Email, email)
                };

                var identity = new ClaimsIdentity(claims, Scheme.Name);
                var principal = new ClaimsPrincipal(identity);
                var ticket = new AuthenticationTicket(principal, Scheme.Name);

                return AuthenticateResult.Success(ticket);
            }

            // Extract token from Authorization header
            if (!Request.Headers.TryGetValue("Authorization", out var authHeader))
            {
                return AuthenticateResult.NoResult();
            }

            var token = authHeader.ToString();
            if (string.IsNullOrEmpty(token) || !token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return AuthenticateResult.NoResult();
            }

            token = token["Bearer ".Length..].Trim();

            try
            {
                // Validate Firebase ID token
                var payload = await GoogleJsonWebSignature.ValidateAsync(token, new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = string.IsNullOrEmpty(_firebaseSettings.Audience)
                        ? new[] { _firebaseSettings.ProjectId }
                        : new[] { _firebaseSettings.Audience }
                });

                // Create claims from Firebase token
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, payload.Subject), // Firebase UID
                    new Claim(ClaimTypes.Email, payload.Email ?? string.Empty),
                    new Claim("firebase_uid", payload.Subject),
                    new Claim("email_verified", payload.EmailVerified.ToString())
                };

                // Extract custom claims from the raw JWT if present
                // Firebase custom claims are stored in a separate dictionary
                var customClaims = GetCustomClaims(token);
                
                if (customClaims.TryGetValue("tenant_id", out var tenantId))
                {
                    claims.Add(new Claim("tenant_id", tenantId));
                }

                if (customClaims.TryGetValue("account_id", out var accountId))
                {
                    claims.Add(new Claim("account_id", accountId));
                }

                if (customClaims.TryGetValue("role", out var role))
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }

                var identity = new ClaimsIdentity(claims, Scheme.Name);
                var principal = new ClaimsPrincipal(identity);
                var ticket = new AuthenticationTicket(principal, Scheme.Name);

                Logger.LogInformation("Successfully authenticated user {UserId} ({Email})", 
                    payload.Subject, payload.Email);

                return AuthenticateResult.Success(ticket);
            }
            catch (InvalidJwtException ex)
            {
                Logger.LogWarning(ex, "Invalid Firebase JWT token");
                return AuthenticateResult.Fail("Invalid Firebase token");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error validating Firebase token");
                return AuthenticateResult.Fail("Error validating token");
            }
        }

        /// <summary>
        /// Create a fake authentication result for development/testing
        /// </summary>
        private AuthenticateResult CreateDevelopmentAuthResult()
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "dev-user-123"),
                new Claim(ClaimTypes.Email, "dev@example.com"),
                new Claim("firebase_uid", "dev-user-123"),
                new Claim("account_id", "1"), // Default to account 1 for testing
                new Claim("tenant_id", "dev-tenant"),
                new Claim(ClaimTypes.Role, "admin")
            };

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return AuthenticateResult.Success(ticket);
        }

        /// <summary>
        /// Extract custom claims from Firebase JWT token
        /// Firebase custom claims are in the JWT payload but not in the Payload object
        /// </summary>
        private static Dictionary<string, string> GetCustomClaims(string token)
        {
            var customClaims = new Dictionary<string, string>();

            try
            {
                // Decode JWT payload (middle part between two dots)
                var parts = token.Split('.');
                if (parts.Length != 3) return customClaims;

                var payload = parts[1];
                // Add padding if needed
                switch (payload.Length % 4)
                {
                    case 2: payload += "=="; break;
                    case 3: payload += "="; break;
                }

                var json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(payload));
                var doc = System.Text.Json.JsonDocument.Parse(json);

                // Extract custom claims
                if (doc.RootElement.TryGetProperty("account_id", out var accountId))
                {
                    customClaims["account_id"] = accountId.ToString();
                }

                if (doc.RootElement.TryGetProperty("tenant_id", out var tenantId))
                {
                    customClaims["tenant_id"] = tenantId.GetString() ?? string.Empty;
                }

                if (doc.RootElement.TryGetProperty("role", out var role))
                {
                    customClaims["role"] = role.GetString() ?? "user";
                }
            }
            catch
            {
                // Ignore errors in custom claims extraction
            }

            return customClaims;
        }
    }
}
