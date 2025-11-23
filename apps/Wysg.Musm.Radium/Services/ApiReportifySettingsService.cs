using System;
using System.Threading.Tasks;
using System.Diagnostics;
using Wysg.Musm.Radium.Services.ApiClients;

namespace Wysg.Musm.Radium.Services
{
    /// <summary>
    /// MIGRATED: API-based implementation of IReportifySettingsService
    /// 
    /// This replaces the old direct database access with secure API calls.
    /// </summary>
    public class ApiReportifySettingsService : IReportifySettingsService
    {
        private readonly IUserSettingsApiClient _apiClient;
        private readonly ITenantContext _tenantContext;

        public ApiReportifySettingsService(
            IUserSettingsApiClient apiClient, 
            ITenantContext tenantContext)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
            _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        }

        public async Task<string?> GetSettingsJsonAsync(long accountId)
        {
            try
            {
                Debug.WriteLine($"[ApiReportifySettingsService] Getting settings for account {accountId}");
                
                var settings = await _apiClient.GetAsync(accountId);
                return settings?.SettingsJson;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ApiReportifySettingsService] Error getting settings: {ex.Message}");
                // Return null to indicate settings don't exist yet
                return null;
            }
        }

        public async Task<(string settingsJson, long rev)> UpsertAsync(long accountId, string settingsJson)
        {
            if (string.IsNullOrWhiteSpace(settingsJson))
            {
                throw new ArgumentException("Settings JSON cannot be empty", nameof(settingsJson));
            }

            try
            {
                Debug.WriteLine($"[ApiReportifySettingsService] Upserting settings for account {accountId}");
                
                var result = await _apiClient.UpsertAsync(accountId, settingsJson);
                
                Debug.WriteLine($"[ApiReportifySettingsService] Settings upserted successfully, rev={result.Rev}");
                
                return (result.SettingsJson, result.Rev);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ApiReportifySettingsService] Error upserting settings: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> DeleteAsync(long accountId)
        {
            try
            {
                Debug.WriteLine($"[ApiReportifySettingsService] Deleting settings for account {accountId}");
                
                await _apiClient.DeleteAsync(accountId);
                
                Debug.WriteLine($"[ApiReportifySettingsService] Settings deleted successfully");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ApiReportifySettingsService] Error deleting settings: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get settings for current tenant
        /// </summary>
        public async Task<string?> GetCurrentTenantSettingsAsync()
        {
            var accountId = _tenantContext.AccountId;
            if (accountId <= 0)
            {
                Debug.WriteLine("[ApiReportifySettingsService] No current account ID");
                return null;
            }

            return await GetSettingsJsonAsync(accountId);
        }

        /// <summary>
        /// Save settings for current tenant
        /// </summary>
        public async Task<(string settingsJson, long rev)> SaveCurrentTenantSettingsAsync(string json)
        {
            var accountId = _tenantContext.AccountId;
            if (accountId <= 0)
            {
                throw new InvalidOperationException("No current account ID");
            }

            return await UpsertAsync(accountId, json);
        }

        /// <summary>
        /// Check if API is available
        /// </summary>
        public async Task<bool> IsApiAvailableAsync()
        {
            try
            {
                return await _apiClient.IsAvailableAsync();
            }
            catch
            {
                return false;
            }
        }
    }
}
