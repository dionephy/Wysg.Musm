using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Wysg.Musm.Radium.Services;

namespace Wysg.Musm.Radium.Services.Adapters
{
    /// <summary>
    /// Adapter that implements IHotkeyService using RadiumApiClient.
    /// Allows seamless switching between direct DB access and API calls.
    /// </summary>
    public sealed class ApiHotkeyServiceAdapter : IHotkeyService
    {
        private readonly RadiumApiClient _apiClient;

        public ApiHotkeyServiceAdapter(RadiumApiClient apiClient)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        }

        public Task PreloadAsync(long accountId)
        {
            // API doesn't need preloading - it's stateless
            return Task.CompletedTask;
        }

        public async Task<IReadOnlyList<HotkeyInfo>> GetAllHotkeyMetaAsync(long accountId)
        {
            var dtos = await _apiClient.GetHotkeysAsync(accountId);
            return dtos.Select(dto => new HotkeyInfo(
                HotkeyId: dto.HotkeyId,
                AccountId: dto.AccountId,
                TriggerText: dto.TriggerText,
                ExpansionText: dto.ExpansionText,
                Description: dto.Description ?? string.Empty,
                IsActive: dto.IsActive,
                UpdatedAt: dto.UpdatedAt,
                Rev: dto.Rev
            )).ToList();
        }

        public async Task<IReadOnlyDictionary<string, string>> GetActiveHotkeysAsync(long accountId)
        {
            var dtos = await _apiClient.GetHotkeysAsync(accountId);
            return dtos
                .Where(dto => dto.IsActive)
                .ToDictionary(dto => dto.TriggerText, dto => dto.ExpansionText);
        }

        public async Task<HotkeyInfo> UpsertHotkeyAsync(long accountId, string triggerText, string expansionText, bool isActive = true, string? description = null)
        {
            var request = new UpsertHotkeyRequest
            {
                TriggerText = triggerText,
                ExpansionText = expansionText,
                Description = description,
                IsActive = isActive
            };

            var dto = await _apiClient.UpsertHotkeyAsync(accountId, request);
            
            return new HotkeyInfo(
                HotkeyId: dto.HotkeyId,
                AccountId: dto.AccountId,
                TriggerText: dto.TriggerText,
                ExpansionText: dto.ExpansionText,
                Description: dto.Description ?? string.Empty,
                IsActive: dto.IsActive,
                UpdatedAt: dto.UpdatedAt,
                Rev: dto.Rev
            );
        }

        public async Task<HotkeyInfo?> ToggleActiveAsync(long accountId, long hotkeyId)
        {
            var dto = await _apiClient.ToggleHotkeyAsync(accountId, hotkeyId);
            if (dto == null) return null;

            return new HotkeyInfo(
                HotkeyId: dto.HotkeyId,
                AccountId: dto.AccountId,
                TriggerText: dto.TriggerText,
                ExpansionText: dto.ExpansionText,
                Description: dto.Description ?? string.Empty,
                IsActive: dto.IsActive,
                UpdatedAt: dto.UpdatedAt,
                Rev: dto.Rev
            );
        }

        public async Task<bool> DeleteHotkeyAsync(long accountId, long hotkeyId)
        {
            return await _apiClient.DeleteHotkeyAsync(accountId, hotkeyId);
        }

        public Task RefreshHotkeysAsync(long accountId)
        {
            // API is stateless - no refresh needed
            return Task.CompletedTask;
        }
    }
}
