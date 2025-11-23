using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Wysg.Musm.Radium.Services.ApiClients
{
    /// <summary>
    /// Hotkey DTO (matches API)
    /// </summary>
    public class HotkeyDto
    {
        public long HotkeyId { get; set; }
        public long AccountId { get; set; }
        public string TriggerText { get; set; } = string.Empty;
        public string ExpansionText { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public DateTime UpdatedAt { get; set; }
        public long Rev { get; set; }
    }

    /// <summary>
    /// Upsert hotkey request
    /// </summary>
    public class UpsertHotkeyRequest
    {
        public string TriggerText { get; set; } = string.Empty;
        public string ExpansionText { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// API client for hotkey operations
    /// </summary>
    public interface IHotkeysApiClient
    {
        Task<List<HotkeyDto>> GetAllAsync(long accountId);
        Task<HotkeyDto?> GetByIdAsync(long accountId, long hotkeyId);
        Task<HotkeyDto> UpsertAsync(long accountId, UpsertHotkeyRequest request);
        Task<HotkeyDto> ToggleActiveAsync(long accountId, long hotkeyId);
        Task DeleteAsync(long accountId, long hotkeyId);
        Task<bool> IsAvailableAsync();
    }

    /// <summary>
    /// Implementation of hotkeys API client
    /// </summary>
    public class HotkeysApiClient : ApiClientBase, IHotkeysApiClient
    {
        public HotkeysApiClient(HttpClient httpClient, string baseUrl)
            : base(httpClient, baseUrl)
        {
        }

        public async Task<List<HotkeyDto>> GetAllAsync(long accountId)
        {
            try
            {
                Debug.WriteLine($"[HotkeysApiClient] Getting all hotkeys for account {accountId}");
                return await GetAsync<List<HotkeyDto>>($"/api/accounts/{accountId}/hotkeys") 
                       ?? new List<HotkeyDto>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[HotkeysApiClient] Error getting hotkeys: {ex.Message}");
                throw;
            }
        }

        public async Task<HotkeyDto?> GetByIdAsync(long accountId, long hotkeyId)
        {
            try
            {
                Debug.WriteLine($"[HotkeysApiClient] Getting hotkey {hotkeyId} for account {accountId}");
                return await GetAsync<HotkeyDto>($"/api/accounts/{accountId}/hotkeys/{hotkeyId}");
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                Debug.WriteLine($"[HotkeysApiClient] Hotkey {hotkeyId} not found");
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[HotkeysApiClient] Error getting hotkey: {ex.Message}");
                throw;
            }
        }

        public async Task<HotkeyDto> UpsertAsync(long accountId, UpsertHotkeyRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (string.IsNullOrWhiteSpace(request.TriggerText))
            {
                throw new ArgumentException("Trigger text cannot be empty", nameof(request));
            }

            if (string.IsNullOrWhiteSpace(request.ExpansionText))
            {
                throw new ArgumentException("Expansion text cannot be empty", nameof(request));
            }

            try
            {
                Debug.WriteLine($"[HotkeysApiClient] Upserting hotkey for account {accountId}");
                var result = await PutAsync<HotkeyDto>($"/api/accounts/{accountId}/hotkeys", request);
                
                if (result == null)
                {
                    throw new InvalidOperationException("Upsert returned null result");
                }
                
                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[HotkeysApiClient] Error upserting hotkey: {ex.Message}");
                throw;
            }
        }

        public async Task<HotkeyDto> ToggleActiveAsync(long accountId, long hotkeyId)
        {
            try
            {
                Debug.WriteLine($"[HotkeysApiClient] Toggling hotkey {hotkeyId} for account {accountId}");
                var result = await PostAsync<HotkeyDto>($"/api/accounts/{accountId}/hotkeys/{hotkeyId}/toggle", new { });
                
                if (result == null)
                {
                    throw new InvalidOperationException("Toggle returned null result");
                }
                
                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[HotkeysApiClient] Error toggling hotkey: {ex.Message}");
                throw;
            }
        }

        public async Task DeleteAsync(long accountId, long hotkeyId)
        {
            try
            {
                Debug.WriteLine($"[HotkeysApiClient] Deleting hotkey {hotkeyId} for account {accountId}");
                await DeleteAsync($"/api/accounts/{accountId}/hotkeys/{hotkeyId}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[HotkeysApiClient] Error deleting hotkey: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> IsAvailableAsync()
        {
            return await IsApiAvailableAsync();
        }
    }
}
