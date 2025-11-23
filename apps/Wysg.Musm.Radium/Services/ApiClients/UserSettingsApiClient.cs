using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Wysg.Musm.Radium.Services.ApiClients
{
    /// <summary>
    /// User Settings DTO (matches API)
    /// </summary>
    public class UserSettingDto
    {
        public long AccountId { get; set; }
        public string SettingsJson { get; set; } = "{}";
        public DateTime UpdatedAt { get; set; }
        public long Rev { get; set; }
    }

    /// <summary>
    /// Update user settings request
    /// </summary>
    public class UpdateUserSettingRequest
    {
        public string SettingsJson { get; set; } = "{}";
    }

    /// <summary>
    /// API client for user settings operations
    /// </summary>
    public interface IUserSettingsApiClient
    {
        Task<UserSettingDto?> GetAsync(long accountId);
        Task<UserSettingDto> UpsertAsync(long accountId, string settingsJson);
        Task DeleteAsync(long accountId);
        Task<bool> IsAvailableAsync();
    }

    /// <summary>
    /// Implementation of user settings API client
    /// </summary>
    public class UserSettingsApiClient : ApiClientBase, IUserSettingsApiClient
    {
        public UserSettingsApiClient(HttpClient httpClient, string baseUrl)
            : base(httpClient, baseUrl)
        {
        }

        public async Task<UserSettingDto?> GetAsync(long accountId)
        {
            try
            {
                Debug.WriteLine($"[UserSettingsApiClient] Getting settings for account {accountId}");
                return await GetAsync<UserSettingDto>($"/api/accounts/{accountId}/settings");
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                Debug.WriteLine($"[UserSettingsApiClient] Settings not found for account {accountId}");
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[UserSettingsApiClient] Error getting settings: {ex.Message}");
                throw;
            }
        }

        public async Task<UserSettingDto> UpsertAsync(long accountId, string settingsJson)
        {
            if (string.IsNullOrWhiteSpace(settingsJson))
            {
                throw new ArgumentException("Settings JSON cannot be empty", nameof(settingsJson));
            }

            try
            {
                Debug.WriteLine($"[UserSettingsApiClient] Upserting settings for account {accountId}");
                var request = new UpdateUserSettingRequest { SettingsJson = settingsJson };
                var result = await PutAsync<UserSettingDto>($"/api/accounts/{accountId}/settings", request);
                
                if (result == null)
                {
                    throw new InvalidOperationException("Upsert returned null result");
                }
                
                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[UserSettingsApiClient] Error upserting settings: {ex.Message}");
                throw;
            }
        }

        public async Task DeleteAsync(long accountId)
        {
            try
            {
                Debug.WriteLine($"[UserSettingsApiClient] Deleting settings for account {accountId}");
                await DeleteAsync($"/api/accounts/{accountId}/settings");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[UserSettingsApiClient] Error deleting settings: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> IsAvailableAsync()
        {
            return await IsApiAvailableAsync();
        }
    }
}
