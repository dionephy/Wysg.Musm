using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Wysg.Musm.Radium.Services;

namespace Wysg.Musm.Radium.Services.Adapters
{
    /// <summary>
    /// Adapter that implements IHotkeyService using RadiumApiClient with in-memory caching.
    /// Allows seamless switching between direct DB access and API calls.
    /// </summary>
    public sealed class ApiHotkeyServiceAdapter : IHotkeyService
    {
        private readonly RadiumApiClient _apiClient;
        private readonly Dictionary<long, List<HotkeyInfo>> _cachedHotkeys = new();
        private readonly System.Threading.SemaphoreSlim _cacheLock = new(1, 1);
        private volatile bool _loaded;

        // Diagnostic logging flag - set to true only when debugging caching issues
        private const bool ENABLE_DIAGNOSTIC_LOGGING = false;

        public ApiHotkeyServiceAdapter(RadiumApiClient apiClient)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
            if (ENABLE_DIAGNOSTIC_LOGGING)
                Debug.WriteLine("[ApiHotkeyServiceAdapter] Initialized with caching");
        }

        public async Task PreloadAsync(long accountId)
        {
            if (accountId <= 0) return;
            await _cacheLock.WaitAsync();
            try
            {
                if (ENABLE_DIAGNOSTIC_LOGGING)
                    Debug.WriteLine($"[ApiHotkeyServiceAdapter][Preload] Loading hotkeys for account {accountId}");
                var dtos = await _apiClient.GetHotkeysAsync(accountId);
                var infos = dtos.Select(dto => new HotkeyInfo(
                    HotkeyId: dto.HotkeyId,
                    AccountId: dto.AccountId,
                    TriggerText: dto.TriggerText,
                    ExpansionText: dto.ExpansionText,
                    Description: dto.Description ?? string.Empty,
                    IsActive: dto.IsActive,
                    UpdatedAt: dto.UpdatedAt,
                    Rev: dto.Rev
                )).ToList();
                
                _cachedHotkeys[accountId] = infos;
                _loaded = true;
                if (ENABLE_DIAGNOSTIC_LOGGING)
                    Debug.WriteLine($"[ApiHotkeyServiceAdapter][Preload] Cached {infos.Count} hotkeys for account {accountId}");
            }
            finally
            {
                _cacheLock.Release();
            }
        }

        public async Task<IReadOnlyList<HotkeyInfo>> GetAllHotkeyMetaAsync(long accountId)
        {
            await _cacheLock.WaitAsync();
            try
            {
                if (!_cachedHotkeys.TryGetValue(accountId, out var cached) || cached.Count == 0)
                {
                    if (ENABLE_DIAGNOSTIC_LOGGING)
                        Debug.WriteLine($"[ApiHotkeyServiceAdapter][GetAllMeta] Cache miss, loading from API");
                    _cacheLock.Release();
                    await PreloadAsync(accountId);
                    await _cacheLock.WaitAsync();
                    cached = _cachedHotkeys.GetValueOrDefault(accountId) ?? new List<HotkeyInfo>();
                }
                
                if (ENABLE_DIAGNOSTIC_LOGGING)
                    Debug.WriteLine($"[ApiHotkeyServiceAdapter][GetAllMeta] Returning {cached.Count} from cache");
                return cached;
            }
            finally
            {
                _cacheLock.Release();
            }
        }

        public async Task<IReadOnlyDictionary<string, string>> GetActiveHotkeysAsync(long accountId)
        {
            await _cacheLock.WaitAsync();
            try
            {
                if (!_cachedHotkeys.TryGetValue(accountId, out var cached) || cached.Count == 0)
                {
                    if (ENABLE_DIAGNOSTIC_LOGGING)
                        Debug.WriteLine($"[ApiHotkeyServiceAdapter][GetActive] Cache miss, loading from API");
                    _cacheLock.Release();
                    await PreloadAsync(accountId);
                    await _cacheLock.WaitAsync();
                    cached = _cachedHotkeys.GetValueOrDefault(accountId) ?? new List<HotkeyInfo>();
                }
                
                var result = cached
                    .Where(h => h.IsActive)
                    .ToDictionary(h => h.TriggerText, h => h.ExpansionText, StringComparer.OrdinalIgnoreCase);
                
                if (ENABLE_DIAGNOSTIC_LOGGING)
                    Debug.WriteLine($"[ApiHotkeyServiceAdapter][GetActive] Returning {result.Count} active hotkeys from cache");
                return result;
            }
            finally
            {
                _cacheLock.Release();
            }
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
            
            var info = new HotkeyInfo(
                HotkeyId: dto.HotkeyId,
                AccountId: dto.AccountId,
                TriggerText: dto.TriggerText,
                ExpansionText: dto.ExpansionText,
                Description: dto.Description ?? string.Empty,
                IsActive: dto.IsActive,
                UpdatedAt: dto.UpdatedAt,
                Rev: dto.Rev
            );
            
            // Update cache
            await _cacheLock.WaitAsync();
            try
            {
                if (_cachedHotkeys.TryGetValue(accountId, out var cached))
                {
                    var existing = cached.FindIndex(h => h.HotkeyId == info.HotkeyId);
                    if (existing >= 0)
                        cached[existing] = info;
                    else
                        cached.Add(info);
                    
                    if (ENABLE_DIAGNOSTIC_LOGGING)
                        Debug.WriteLine($"[ApiHotkeyServiceAdapter][Upsert] Updated cache for hotkey {info.HotkeyId}");
                }
            }
            finally
            {
                _cacheLock.Release();
            }
            
            return info;
        }

        public async Task<HotkeyInfo?> ToggleActiveAsync(long accountId, long hotkeyId)
        {
            var dto = await _apiClient.ToggleHotkeyAsync(accountId, hotkeyId);
            if (dto == null) return null;

            var info = new HotkeyInfo(
                HotkeyId: dto.HotkeyId,
                AccountId: dto.AccountId,
                TriggerText: dto.TriggerText,
                ExpansionText: dto.ExpansionText,
                Description: dto.Description ?? string.Empty,
                IsActive: dto.IsActive,
                UpdatedAt: dto.UpdatedAt,
                Rev: dto.Rev
            );
            
            // Update cache
            await _cacheLock.WaitAsync();
            try
            {
                if (_cachedHotkeys.TryGetValue(accountId, out var cached))
                {
                    var existing = cached.FindIndex(h => h.HotkeyId == info.HotkeyId);
                    if (existing >= 0)
                        cached[existing] = info;
                    
                    if (ENABLE_DIAGNOSTIC_LOGGING)
                        Debug.WriteLine($"[ApiHotkeyServiceAdapter][Toggle] Updated cache for hotkey {info.HotkeyId}");
                }
            }
            finally
            {
                _cacheLock.Release();
            }
            
            return info;
        }

        public async Task<bool> DeleteHotkeyAsync(long accountId, long hotkeyId)
        {
            var result = await _apiClient.DeleteHotkeyAsync(accountId, hotkeyId);
            
            if (result)
            {
                // Update cache
                await _cacheLock.WaitAsync();
                try
                {
                    if (_cachedHotkeys.TryGetValue(accountId, out var cached))
                    {
                        cached.RemoveAll(h => h.HotkeyId == hotkeyId);
                        if (ENABLE_DIAGNOSTIC_LOGGING)
                            Debug.WriteLine($"[ApiHotkeyServiceAdapter][Delete] Removed hotkey {hotkeyId} from cache");
                    }
                }
                finally
                {
                    _cacheLock.Release();
                }
            }
            
            return result;
        }

        public async Task RefreshHotkeysAsync(long accountId)
        {
            if (ENABLE_DIAGNOSTIC_LOGGING)
                Debug.WriteLine($"[ApiHotkeyServiceAdapter][Refresh] Reloading from API for account {accountId}");
            _loaded = false;
            await PreloadAsync(accountId);
        }
    }
}
