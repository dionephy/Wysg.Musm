using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Wysg.Musm.Radium.Services.ApiClients
{
    /// <summary>
    /// Base class for all API clients providing common HTTP operations
    /// </summary>
    public abstract class ApiClientBase
    {
        protected readonly HttpClient HttpClient;
        protected readonly string BaseUrl;

        protected ApiClientBase(HttpClient httpClient, string baseUrl)
        {
            HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            BaseUrl = baseUrl?.TrimEnd('/') ?? throw new ArgumentNullException(nameof(baseUrl));
        }

        /// <summary>
        /// Get HttpClient with Firebase authentication header
        /// Note: The token should be set externally (e.g., from login) rather than retrieved here
        /// This design allows the API clients to be simple and not depend on auth refresh logic
        /// </summary>
        protected Task<HttpClient> GetAuthenticatedClientAsync()
        {
            // Token is expected to be set on HttpClient.DefaultRequestHeaders.Authorization
            // by the caller (e.g., after login via GoogleOAuthAuthService)
            // If not set, the API will return 401 and the app should handle re-authentication
            
            if (HttpClient.DefaultRequestHeaders.Authorization == null)
            {
                Debug.WriteLine("[ApiClientBase] WARNING: No authorization header set. API calls may fail with 401.");
            }
            
            return Task.FromResult(HttpClient);
        }
        
        /// <summary>
        /// Set the Firebase ID token for authentication
        /// Call this after successful login
        /// </summary>
        public void SetAuthToken(string firebaseIdToken)
        {
            if (string.IsNullOrWhiteSpace(firebaseIdToken))
            {
                throw new ArgumentException("Firebase ID token cannot be empty", nameof(firebaseIdToken));
            }

            HttpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", firebaseIdToken);
            
            Debug.WriteLine("[ApiClientBase] Authorization token set successfully");
        }
        
        /// <summary>
        /// Clear authentication token (for logout)
        /// </summary>
        public void ClearAuthToken()
        {
            HttpClient.DefaultRequestHeaders.Authorization = null;
            Debug.WriteLine("[ApiClientBase] Authorization token cleared");
        }

        /// <summary>
        /// Execute GET request
        /// </summary>
        protected async Task<T?> GetAsync<T>(string endpoint) where T : class
        {
            try
            {
                var client = await GetAuthenticatedClientAsync();
                var url = $"{BaseUrl}{endpoint}";
                
                Debug.WriteLine($"[ApiClientBase] GET {url}");
                
                var response = await client.GetAsync(url);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"[ApiClientBase] GET failed: {response.StatusCode} - {errorContent}");
                }
                
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<T>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ApiClientBase] GET error for {endpoint}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Execute POST request
        /// </summary>
        protected async Task<T?> PostAsync<T>(string endpoint, object data) where T : class
        {
            try
            {
                var client = await GetAuthenticatedClientAsync();
                var url = $"{BaseUrl}{endpoint}";
                
                Debug.WriteLine($"[ApiClientBase] POST {url}");
                
                var response = await client.PostAsJsonAsync(url, data);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"[ApiClientBase] POST failed: {response.StatusCode} - {errorContent}");
                }
                
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<T>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ApiClientBase] POST error for {endpoint}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Execute POST request without expecting a response body
        /// </summary>
        protected async Task PostAsync(string endpoint, object data)
        {
            try
            {
                var client = await GetAuthenticatedClientAsync();
                var url = $"{BaseUrl}{endpoint}";
                
                Debug.WriteLine($"[ApiClientBase] POST {url}");
                
                var response = await client.PostAsJsonAsync(url, data);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"[ApiClientBase] POST failed: {response.StatusCode} - {errorContent}");
                }
                
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ApiClientBase] POST error for {endpoint}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Execute PUT request
        /// </summary>
        protected async Task<T?> PutAsync<T>(string endpoint, object data) where T : class
        {
            try
            {
                var client = await GetAuthenticatedClientAsync();
                var url = $"{BaseUrl}{endpoint}";
                
                Debug.WriteLine($"[ApiClientBase] PUT {url}");
                
                var response = await client.PutAsJsonAsync(url, data);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"[ApiClientBase] PUT failed: {response.StatusCode} - {errorContent}");
                }
                
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<T>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ApiClientBase] PUT error for {endpoint}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Execute DELETE request
        /// </summary>
        protected async Task DeleteAsync(string endpoint)
        {
            try
            {
                var client = await GetAuthenticatedClientAsync();
                var url = $"{BaseUrl}{endpoint}";
                
                Debug.WriteLine($"[ApiClientBase] DELETE {url}");
                
                var response = await client.DeleteAsync(url);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"[ApiClientBase] DELETE failed: {response.StatusCode} - {errorContent}");
                }
                
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ApiClientBase] DELETE error for {endpoint}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Check if API is reachable
        /// </summary>
        public async Task<bool> IsApiAvailableAsync()
        {
            try
            {
                var response = await HttpClient.GetAsync($"{BaseUrl}/health");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}
