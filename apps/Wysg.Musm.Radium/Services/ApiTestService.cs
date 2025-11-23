using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Wysg.Musm.Radium.Services.ApiClients;

namespace Wysg.Musm.Radium.Services
{
    /// <summary>
    /// Service for testing API connectivity and functionality
    /// Use this during development/debugging to verify API integration
    /// </summary>
    public sealed class ApiTestService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ITenantContext _tenantContext;

        public ApiTestService(IServiceProvider serviceProvider, ITenantContext tenantContext)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        }

        /// <summary>
        /// Run comprehensive API tests
        /// Returns true if all tests pass
        /// </summary>
        public async Task<bool> RunAllTestsAsync()
        {
            Debug.WriteLine("???????????????????????????????????????");
            Debug.WriteLine("[ApiTest] Starting comprehensive API tests");
            Debug.WriteLine("???????????????????????????????????????");

            var allPassed = true;

            // Test 1: Health Check
            if (!await TestHealthCheckAsync())
            {
                allPassed = false;
            }

            // Test 2: User Settings API
            if (!await TestUserSettingsApiAsync())
            {
                allPassed = false;
            }

            // Test 3: Phrases API
            if (!await TestPhrasesApiAsync())
            {
                allPassed = false;
            }

            // Test 4: Hotkeys API
            if (!await TestHotkeysApiAsync())
            {
                allPassed = false;
            }

            // Test 5: Snippets API
            if (!await TestSnippetsApiAsync())
            {
                allPassed = false;
            }

            Debug.WriteLine("???????????????????????????????????????");
            Debug.WriteLine($"[ApiTest] Tests completed: {(allPassed ? "? ALL PASSED" : "? SOME FAILED")}");
            Debug.WriteLine("???????????????????????????????????????");

            return allPassed;
        }

        /// <summary>
        /// Test 1: Health Check - Verify API is reachable
        /// </summary>
        private async Task<bool> TestHealthCheckAsync()
        {
            Debug.WriteLine("\n[Test 1] Health Check");
            Debug.WriteLine("式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式");

            try
            {
                using var client = new HttpClient { BaseAddress = new Uri("http://localhost:5205") };
                client.Timeout = TimeSpan.FromSeconds(5);

                var response = await client.GetAsync("/health");
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"? API is healthy: {content}");
                    return true;
                }
                else
                {
                    Debug.WriteLine($"? Health check failed: {response.StatusCode}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"? Cannot reach API: {ex.Message}");
                Debug.WriteLine("   Make sure API is running on http://localhost:5205");
                return false;
            }
        }

        /// <summary>
        /// Test 2: User Settings API
        /// </summary>
        private async Task<bool> TestUserSettingsApiAsync()
        {
            Debug.WriteLine("\n[Test 2] User Settings API");
            Debug.WriteLine("式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式");

            try
            {
                var accountId = _tenantContext.AccountId;
                if (accountId <= 0)
                {
                    Debug.WriteLine("?? No account ID available, skipping test");
                    return true; // Not a failure, just can't test
                }

                var client = _serviceProvider.GetService<IUserSettingsApiClient>();
                if (client == null)
                {
                    Debug.WriteLine("? UserSettingsApiClient not registered");
                    return false;
                }

                Debug.WriteLine($"   Account ID: {accountId}");
                var settings = await client.GetAsync(accountId);

                if (settings != null)
                {
                    Debug.WriteLine($"? Settings retrieved: {settings.SettingsJson?.Length ?? 0} characters");
                    Debug.WriteLine($"   Rev: {settings.Rev}, Updated: {settings.UpdatedAt}");
                }
                else
                {
                    Debug.WriteLine("??  No settings found (new account?)");
                }

                return true;
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                Debug.WriteLine("? 401 Unauthorized - Token not set or expired");
                Debug.WriteLine("   Call ApiTokenManager.SetAuthToken() after login");
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"? Test failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Test 3: Phrases API
        /// </summary>
        private async Task<bool> TestPhrasesApiAsync()
        {
            Debug.WriteLine("\n[Test 3] Phrases API");
            Debug.WriteLine("式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式");

            try
            {
                var accountId = _tenantContext.AccountId;
                if (accountId <= 0)
                {
                    Debug.WriteLine("?? No account ID available, skipping test");
                    return true;
                }

                var client = _serviceProvider.GetService<IPhrasesApiClient>();
                if (client == null)
                {
                    Debug.WriteLine("? PhrasesApiClient not registered");
                    return false;
                }

                Debug.WriteLine($"   Account ID: {accountId}");
                var phrases = await client.GetAllAsync(accountId, activeOnly: true);

                Debug.WriteLine($"? Phrases retrieved: {phrases.Count} phrases");
                if (phrases.Count > 0)
                {
                    Debug.WriteLine($"   Sample: '{phrases[0].Text}' (ID: {phrases[0].Id})");
                }

                return true;
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                Debug.WriteLine("? 401 Unauthorized - Token not set or expired");
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"? Test failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Test 4: Hotkeys API
        /// </summary>
        private async Task<bool> TestHotkeysApiAsync()
        {
            Debug.WriteLine("\n[Test 4] Hotkeys API");
            Debug.WriteLine("式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式");

            try
            {
                var accountId = _tenantContext.AccountId;
                if (accountId <= 0)
                {
                    Debug.WriteLine("?? No account ID available, skipping test");
                    return true;
                }

                var client = _serviceProvider.GetService<IHotkeysApiClient>();
                if (client == null)
                {
                    Debug.WriteLine("? HotkeysApiClient not registered");
                    return false;
                }

                Debug.WriteLine($"   Account ID: {accountId}");
                var hotkeys = await client.GetAllAsync(accountId);

                Debug.WriteLine($"? Hotkeys retrieved: {hotkeys.Count} hotkeys");
                if (hotkeys.Count > 0)
                {
                    Debug.WriteLine($"   Sample: '{hotkeys[0].TriggerText}' ⊥ '{hotkeys[0].ExpansionText.Substring(0, Math.Min(20, hotkeys[0].ExpansionText.Length))}...'");
                }

                return true;
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                Debug.WriteLine("? 401 Unauthorized - Token not set or expired");
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"? Test failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Test 5: Snippets API
        /// </summary>
        private async Task<bool> TestSnippetsApiAsync()
        {
            Debug.WriteLine("\n[Test 5] Snippets API");
            Debug.WriteLine("式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式");

            try
            {
                var accountId = _tenantContext.AccountId;
                if (accountId <= 0)
                {
                    Debug.WriteLine("?? No account ID available, skipping test");
                    return true;
                }

                var client = _serviceProvider.GetService<ISnippetsApiClient>();
                if (client == null)
                {
                    Debug.WriteLine("? SnippetsApiClient not registered");
                    return false;
                }

                Debug.WriteLine($"   Account ID: {accountId}");
                var snippets = await client.GetAllAsync(accountId);

                Debug.WriteLine($"? Snippets retrieved: {snippets.Count} snippets");
                if (snippets.Count > 0)
                {
                    Debug.WriteLine($"   Sample: '{snippets[0].TriggerText}' ⊥ '{snippets[0].SnippetText.Substring(0, Math.Min(30, snippets[0].SnippetText.Length))}...'");
                }

                return true;
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                Debug.WriteLine("? 401 Unauthorized - Token not set or expired");
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"? Test failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Quick test - just verify API is reachable
        /// </summary>
        public async Task<bool> QuickTestAsync()
        {
            Debug.WriteLine("[ApiTest] Running quick health check...");
            
            var result = await TestHealthCheckAsync();
            
            if (result)
            {
                Debug.WriteLine("[ApiTest] ? Quick test passed");
            }
            else
            {
                Debug.WriteLine("[ApiTest] ? Quick test failed");
            }
            
            return result;
        }
    }
}
