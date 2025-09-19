using System;
using System.Diagnostics;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using Wysg.Musm.Radium.Models;

namespace Wysg.Musm.Radium.Services
{
    // Google OAuth (PKCE) + Firebase REST (email/password sign-in & sign-up)
    public sealed class GoogleOAuthAuthService : IAuthService
    {
        private readonly string _googleClientId;
        private readonly string _googleClientSecret;
        private readonly string _firebaseApiKey; // Firebase Web API Key
        private readonly HttpClient _http = new HttpClient();

        public GoogleOAuthAuthService()
        {
            _googleClientId = Environment.GetEnvironmentVariable("GOOGLE_OAUTH_CLIENT_ID") ?? string.Empty;
            _googleClientSecret = Environment.GetEnvironmentVariable("GOOGLE_OAUTH_CLIENT_SECRET") ?? string.Empty;
            _firebaseApiKey = Environment.GetEnvironmentVariable("FIREBASE_API_KEY") ?? string.Empty;
        }

        public async Task<AuthResult> SignInWithGoogleAsync()
        {
            if (string.IsNullOrWhiteSpace(_googleClientId))
                return AuthResult.Fail("Google OAuth client not configured (missing GOOGLE_OAUTH_CLIENT_ID).");

            try
            {
                var redirect = "http://127.0.0.1:51789/callback";
                var state = Guid.NewGuid().ToString("N");

                // PKCE
                var verifier = GenerateCodeVerifier();
                var challenge = GenerateCodeChallenge(verifier);

                var url = "https://accounts.google.com/o/oauth2/v2/auth" +
                          "?response_type=code" +
                          "&client_id=" + Uri.EscapeDataString(_googleClientId) +
                          "&redirect_uri=" + Uri.EscapeDataString(redirect) +
                          "&scope=" + Uri.EscapeDataString("openid email profile") +
                          "&state=" + state +
                          "&code_challenge=" + challenge +
                          "&code_challenge_method=S256" +
                          "&access_type=offline" +
                          "&prompt=select_account";

                using var listener = new System.Net.HttpListener();
                listener.Prefixes.Add("http://127.0.0.1:51789/");
                listener.Start();
                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });

                var ctx = await listener.GetContextAsync();
                var query = HttpUtility.ParseQueryString(ctx.Request.Url?.Query ?? "");
                if (query["error"] != null)
                {
                    await Respond(ctx.Response, 200, "Login failed. You can close this window.");
                    return AuthResult.Fail(query["error"]!);
                }
                var code = query["code"]; var recvState = query["state"];            
                if (string.IsNullOrEmpty(code) || recvState != state)
                {
                    await Respond(ctx.Response, 200, "Invalid login response. You can close this window.");
                    return AuthResult.Fail("Invalid code/state");
                }
                await Respond(ctx.Response, 200, "Login success. You can close this window.");
                listener.Stop();

                // Exchange code for tokens
                var pairs = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, string>>
                {
                    new("code", code!),
                    new("client_id", _googleClientId),
                    new("code_verifier", verifier),
                    new("redirect_uri", redirect),
                    new("grant_type", "authorization_code"),
                };
                if (!string.IsNullOrWhiteSpace(_googleClientSecret))
                {
                    pairs.Add(new("client_secret", _googleClientSecret));
                }
                var tokenReq = new HttpRequestMessage(HttpMethod.Post, "https://oauth2.googleapis.com/token")
                {
                    Content = new FormUrlEncodedContent(pairs)
                };
                var tokenRes = await _http.SendAsync(tokenReq);
                var tokenBody = await tokenRes.Content.ReadAsStringAsync();
                if (!tokenRes.IsSuccessStatusCode)
                {
                    return AuthResult.Fail($"Token exchange failed: {(int)tokenRes.StatusCode} {tokenRes.ReasonPhrase} :: {tokenBody}");
                }

                using var doc = JsonDocument.Parse(tokenBody);
                var root = doc.RootElement;
                var googleIdToken = root.GetProperty("id_token").GetString() ?? string.Empty;
                var googleRefreshToken = root.TryGetProperty("refresh_token", out var rt) ? (rt.GetString() ?? string.Empty) : string.Empty;

                // Prefer minting a Firebase session from Google ID token to obtain a Firebase refresh token + UID
                if (!string.IsNullOrWhiteSpace(_firebaseApiKey))
                {
                    var idpUrl = $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithIdp?key={_firebaseApiKey}";
                    var idpBody = new
                    {
                        postBody = $"id_token={googleIdToken}&providerId=google.com",
                        requestUri = "http://localhost",
                        returnSecureToken = true
                    };
                    var idpReq = new HttpRequestMessage(HttpMethod.Post, idpUrl)
                    {
                        Content = new StringContent(JsonSerializer.Serialize(idpBody), Encoding.UTF8, "application/json")
                    };
                    var idpRes = await _http.SendAsync(idpReq);
                    var idpJson = await idpRes.Content.ReadAsStringAsync();
                    if (idpRes.IsSuccessStatusCode)
                    {
                        using var idpDoc = JsonDocument.Parse(idpJson);
                        var idpRoot = idpDoc.RootElement;
                        var firebaseIdToken = idpRoot.GetProperty("idToken").GetString() ?? string.Empty;
                        var firebaseRefreshToken = idpRoot.GetProperty("refreshToken").GetString() ?? string.Empty;
                        var localId = idpRoot.GetProperty("localId").GetString() ?? string.Empty;
                        var email = idpRoot.TryGetProperty("email", out var em) ? em.GetString() ?? string.Empty : string.Empty;
                        var displayName = idpRoot.TryGetProperty("displayName", out var dn) ? dn.GetString() ?? string.Empty : string.Empty;
                        return AuthResult.Ok(localId, email, displayName, firebaseIdToken, firebaseRefreshToken);
                    }
                    // Fallback to Google tokens if Firebase IDP exchange fails
                }

                // Fallback: decode googleIdToken for email/name/sub
                var payloadJson = ParseJwtPayload(googleIdToken);
                using var payload = JsonDocument.Parse(payloadJson);
                var email2 = payload.RootElement.TryGetProperty("email", out var e) ? e.GetString() ?? string.Empty : string.Empty;
                var name2 = payload.RootElement.TryGetProperty("name", out var n) ? n.GetString() ?? string.Empty : string.Empty;
                var sub2 = payload.RootElement.TryGetProperty("sub", out var s) ? s.GetString() ?? string.Empty : string.Empty;

                return AuthResult.Ok(sub2, email2, name2, googleIdToken, googleRefreshToken);
            }
            catch (Exception ex)
            {
                return AuthResult.Fail(ex.Message);
            }
        }

        public async Task<AuthResult> SignInWithEmailPasswordAsync(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(_firebaseApiKey))
                return AuthResult.Fail("FIREBASE_API_KEY not configured.");

            try
            {
                var url = $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={_firebaseApiKey}";
                var body = new
                {
                    email,
                    password,
                    returnSecureToken = true
                };
                var req = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
                };
                var res = await _http.SendAsync(req);
                var json = await res.Content.ReadAsStringAsync();
                if (!res.IsSuccessStatusCode)
                {
                    return AuthResult.Fail($"Firebase error: {json}");
                }
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                var idToken = root.GetProperty("idToken").GetString() ?? string.Empty;
                var refreshToken = root.GetProperty("refreshToken").GetString() ?? string.Empty;
                var localId = root.GetProperty("localId").GetString() ?? string.Empty;
                var displayName = root.TryGetProperty("displayName", out var dn) ? dn.GetString() ?? string.Empty : string.Empty;
                return AuthResult.Ok(localId, email, displayName, idToken, refreshToken);
            }
            catch (Exception ex)
            {
                return AuthResult.Fail(ex.Message);
            }
        }

        public async Task<AuthResult> RefreshAsync(string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(_firebaseApiKey))
                return AuthResult.Fail("FIREBASE_API_KEY not configured.");
            try
            {
                var url = $"https://securetoken.googleapis.com/v1/token?key={_firebaseApiKey}";
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string,string>("grant_type","refresh_token"),
                    new KeyValuePair<string,string>("refresh_token", refreshToken)
                });
                var res = await _http.PostAsync(url, content);
                var json = await res.Content.ReadAsStringAsync();
                if (!res.IsSuccessStatusCode) return AuthResult.Fail($"Refresh error: {json}");

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                var idToken = root.GetProperty("id_token").GetString() ?? string.Empty;
                var userId = root.GetProperty("user_id").GetString() ?? string.Empty;
                var newRefresh = root.GetProperty("refresh_token").GetString() ?? refreshToken;
                return AuthResult.Ok(userId, string.Empty, string.Empty, idToken, newRefresh);
            }
            catch (Exception ex)
            {
                return AuthResult.Fail(ex.Message);
            }
        }

        public Task SignOutAsync() => Task.CompletedTask;

        private static async Task Respond(System.Net.HttpListenerResponse resp, int code, string message)
        {
            resp.StatusCode = code;
            var buf = Encoding.UTF8.GetBytes(message);
            resp.ContentType = "text/plain; charset=utf-8";
            resp.ContentLength64 = buf.Length;
            await resp.OutputStream.WriteAsync(buf, 0, buf.Length);
            resp.Close();
        }

        private static string ParseJwtPayload(string jwt)
        {
            if (string.IsNullOrEmpty(jwt) || jwt.Split('.').Length < 2) return "{}";
            string payload = jwt.Split('.')[1];
            payload = payload.Replace('-', '+').Replace('_', '/');
            switch (payload.Length % 4)
            {
                case 2: payload += "=="; break;
                case 3: payload += "="; break;
            }
            var bytes = Convert.FromBase64String(payload);
            return Encoding.UTF8.GetString(bytes);
        }

        private static string GenerateCodeVerifier()
        {
            var bytes = new byte[32];
            RandomNumberGenerator.Fill(bytes);
            return Base64UrlNoPadding(bytes);
        }

        private static string GenerateCodeChallenge(string verifier)
        {
            var bytes = Encoding.ASCII.GetBytes(verifier);
            var hash = SHA256.HashData(bytes);
            return Base64UrlNoPadding(hash);
        }

        private static string Base64UrlNoPadding(ReadOnlySpan<byte> buffer)
        {
            string s = Convert.ToBase64String(buffer).Replace('+', '-').Replace('/', '_').TrimEnd('=');
            return s;
        }

        public async Task<AuthResult> SignUpWithEmailPasswordAsync(string email, string password, string? displayName)
        {
            if (string.IsNullOrWhiteSpace(_firebaseApiKey))
                return AuthResult.Fail("FIREBASE_API_KEY not configured.");

            try
            {
                var url = $"https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={_firebaseApiKey}";
                var body = new { email, password, returnSecureToken = true };
                var req = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
                };
                var res = await _http.SendAsync(req);
                var json = await res.Content.ReadAsStringAsync();
                if (!res.IsSuccessStatusCode) return AuthResult.Fail($"Firebase sign-up error: {json}");

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                var idToken = root.GetProperty("idToken").GetString() ?? string.Empty;
                var refreshToken = root.GetProperty("refreshToken").GetString() ?? string.Empty;
                var localId = root.GetProperty("localId").GetString() ?? string.Empty;

                if (!string.IsNullOrWhiteSpace(displayName))
                {
                    var updUrl = $"https://identitytoolkit.googleapis.com/v1/accounts:update?key={_firebaseApiKey}";
                    var updBody = new { idToken, displayName, returnSecureToken = true };
                    var updReq = new HttpRequestMessage(HttpMethod.Post, updUrl)
                    {
                        Content = new StringContent(JsonSerializer.Serialize(updBody), Encoding.UTF8, "application/json")
                    };
                    var updRes = await _http.SendAsync(updReq);
                    var updJson = await updRes.Content.ReadAsStringAsync();
                    if (updRes.IsSuccessStatusCode)
                    {
                        using var updDoc = JsonDocument.Parse(updJson);
                        idToken = updDoc.RootElement.TryGetProperty("idToken", out var it) ? it.GetString() ?? idToken : idToken;
                        refreshToken = updDoc.RootElement.TryGetProperty("refreshToken", out var rt) ? rt.GetString() ?? refreshToken : refreshToken;
                    }
                }

                return AuthResult.Ok(localId, email, displayName ?? string.Empty, idToken, refreshToken);
            }
            catch (Exception ex)
            {
                return AuthResult.Fail(ex.Message);
            }
        }
    }
}
