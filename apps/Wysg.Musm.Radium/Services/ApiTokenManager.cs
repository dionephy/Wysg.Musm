using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Wysg.Musm.Radium.Services.ApiClients;

namespace Wysg.Musm.Radium.Services
{
    /// <summary>
    /// Helper class to manage authentication tokens across all API clients
    /// Simplifies setting/clearing tokens after login/logout
    /// </summary>
    public sealed class ApiTokenManager
    {
        private readonly IServiceProvider _serviceProvider;

        public ApiTokenManager(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        /// <summary>
        /// Set Firebase ID token on all registered API clients
        /// Call this immediately after successful login
        /// </summary>
        public void SetAuthToken(string firebaseIdToken)
        {
            if (string.IsNullOrWhiteSpace(firebaseIdToken))
            {
                throw new ArgumentException("Firebase ID token cannot be empty", nameof(firebaseIdToken));
            }

            var clients = GetAllApiClients();
            int successCount = 0;

            foreach (var client in clients)
            {
                try
                {
                    client.SetAuthToken(firebaseIdToken);
                    successCount++;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ApiTokenManager] Failed to set token on {client.GetType().Name}: {ex.Message}");
                }
            }

            Debug.WriteLine($"[ApiTokenManager] ? Token set on {successCount}/{clients.Count} API clients");
        }

        /// <summary>
        /// Clear authentication tokens from all API clients
        /// Call this on logout
        /// </summary>
        public void ClearAuthTokens()
        {
            var clients = GetAllApiClients();
            int successCount = 0;

            foreach (var client in clients)
            {
                try
                {
                    client.ClearAuthToken();
                    successCount++;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ApiTokenManager] Failed to clear token on {client.GetType().Name}: {ex.Message}");
                }
            }

            Debug.WriteLine($"[ApiTokenManager] ? Tokens cleared on {successCount}/{clients.Count} API clients");
        }

        /// <summary>
        /// Verify all API clients are available and token is set
        /// </summary>
        public async Task<bool> VerifyApiConnectionAsync()
        {
            try
            {
                Debug.WriteLine("[ApiTokenManager] Verifying API connection...");

                // Try to get one API client and check if it can reach the API
                var userSettingsClient = _serviceProvider.GetService<IUserSettingsApiClient>();
                if (userSettingsClient == null)
                {
                    Debug.WriteLine("[ApiTokenManager] ? UserSettingsApiClient not registered");
                    return false;
                }

                var isAvailable = await userSettingsClient.IsAvailableAsync();
                
                if (isAvailable)
                {
                    Debug.WriteLine("[ApiTokenManager] ? API connection verified");
                }
                else
                {
                    Debug.WriteLine("[ApiTokenManager] ? API not reachable");
                }

                return isAvailable;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ApiTokenManager] ? API verification failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get all registered API client instances
        /// </summary>
        private System.Collections.Generic.List<ApiClientBase> GetAllApiClients()
        {
            var clients = new[]
            {
                _serviceProvider.GetService<IUserSettingsApiClient>() as ApiClientBase,
                _serviceProvider.GetService<IPhrasesApiClient>() as ApiClientBase,
                _serviceProvider.GetService<IHotkeysApiClient>() as ApiClientBase,
                _serviceProvider.GetService<ISnippetsApiClient>() as ApiClientBase,
                _serviceProvider.GetService<ISnomedApiClient>() as ApiClientBase,
                _serviceProvider.GetService<IExportedReportsApiClient>() as ApiClientBase,
                _serviceProvider.GetService<IAccountsApiClient>() as ApiClientBase
            };

            return clients.Where(c => c != null).Cast<ApiClientBase>().ToList();
        }
    }
}
