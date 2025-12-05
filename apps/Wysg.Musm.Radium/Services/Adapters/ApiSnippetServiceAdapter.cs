using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Wysg.Musm.Radium.Services;

namespace Wysg.Musm.Radium.Services.Adapters
{
    /// <summary>
    /// Adapter that implements ISnippetService using RadiumApiClient with in-memory caching.
    /// Allows seamless switching between direct DB access and API calls.
    /// </summary>
    public sealed class ApiSnippetServiceAdapter : ISnippetService
    {
        private readonly RadiumApiClient _apiClient;
        private readonly Dictionary<long, List<SnippetInfo>> _cachedSnippets = new();
        private readonly System.Threading.SemaphoreSlim _cacheLock = new(1, 1);
        private volatile bool _loaded;

        // Diagnostic logging flag - set to true only when debugging caching issues
        private const bool ENABLE_DIAGNOSTIC_LOGGING = false;

        public ApiSnippetServiceAdapter(RadiumApiClient apiClient)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
            if (ENABLE_DIAGNOSTIC_LOGGING)
                Debug.WriteLine("[ApiSnippetServiceAdapter] Initialized with caching");
        }

        public async Task PreloadAsync(long accountId)
        {
            if (accountId <= 0) return;
            await _cacheLock.WaitAsync();
            try
            {
                if (ENABLE_DIAGNOSTIC_LOGGING)
                    Debug.WriteLine($"[ApiSnippetServiceAdapter][Preload] Loading snippets for account {accountId}");
                var dtos = await _apiClient.GetSnippetsAsync(accountId);
                var infos = dtos.Select(dto => new SnippetInfo(
                    SnippetId: dto.SnippetId,
                    AccountId: dto.AccountId,
                    TriggerText: dto.TriggerText,
                    SnippetText: dto.SnippetText,
                    SnippetAst: dto.SnippetAst,
                    Description: dto.Description ?? string.Empty,
                    IsActive: dto.IsActive,
                    UpdatedAt: dto.UpdatedAt,
                    Rev: dto.Rev
                )).ToList();
                
                _cachedSnippets[accountId] = infos;
                _loaded = true;
                if (ENABLE_DIAGNOSTIC_LOGGING)
                    Debug.WriteLine($"[ApiSnippetServiceAdapter][Preload] Cached {infos.Count} snippets for account {accountId}");
            }
            finally
            {
                _cacheLock.Release();
            }
        }

        public async Task<IReadOnlyList<SnippetInfo>> GetAllSnippetMetaAsync(long accountId)
        {
            await _cacheLock.WaitAsync();
            try
            {
                if (!_cachedSnippets.TryGetValue(accountId, out var cached) || cached.Count == 0)
                {
                    if (ENABLE_DIAGNOSTIC_LOGGING)
                        Debug.WriteLine($"[ApiSnippetServiceAdapter][GetAllMeta] Cache miss, loading from API");
                    _cacheLock.Release(); // Release before async call
                    await PreloadAsync(accountId);
                    await _cacheLock.WaitAsync();
                    cached = _cachedSnippets.GetValueOrDefault(accountId) ?? new List<SnippetInfo>();
                }
                
                if (ENABLE_DIAGNOSTIC_LOGGING)
                    Debug.WriteLine($"[ApiSnippetServiceAdapter][GetAllMeta] Returning {cached.Count} from cache");
                return cached;
            }
            finally
            {
                _cacheLock.Release();
            }
        }

        public async Task<IReadOnlyDictionary<string, (string text, string ast, string description)>> GetActiveSnippetsAsync(long accountId)
        {
            await _cacheLock.WaitAsync();
            try
            {
                if (!_cachedSnippets.TryGetValue(accountId, out var cached) || cached.Count == 0)
                {
                    if (ENABLE_DIAGNOSTIC_LOGGING)
                        Debug.WriteLine($"[ApiSnippetServiceAdapter][GetActive] Cache miss, loading from API");
                    _cacheLock.Release(); // Release before async call
                    await PreloadAsync(accountId);
                    await _cacheLock.WaitAsync();
                    cached = _cachedSnippets.GetValueOrDefault(accountId) ?? new List<SnippetInfo>();
                }
                
                var result = cached
                    .Where(s => s.IsActive)
                    .ToDictionary(
                        s => s.TriggerText, 
                        s => (s.SnippetText, s.SnippetAst, s.Description),
                        StringComparer.OrdinalIgnoreCase
                    );
                
                if (ENABLE_DIAGNOSTIC_LOGGING)
                    Debug.WriteLine($"[ApiSnippetServiceAdapter][GetActive] Returning {result.Count} active snippets from cache");
                return result;
            }
            finally
            {
                _cacheLock.Release();
            }
        }

        public async Task<SnippetInfo> UpsertSnippetAsync(long accountId, string triggerText, string snippetText, string snippetAst, bool isActive = true, string? description = null)
        {
            var request = new UpsertSnippetRequest
            {
                TriggerText = triggerText,
                SnippetText = snippetText,
                SnippetAst = snippetAst,
                Description = description,
                IsActive = isActive
            };

            var dto = await _apiClient.UpsertSnippetAsync(accountId, request);
            
            var info = new SnippetInfo(
                SnippetId: dto.SnippetId,
                AccountId: dto.AccountId,
                TriggerText: dto.TriggerText,
                SnippetText: dto.SnippetText,
                SnippetAst: dto.SnippetAst,
                Description: dto.Description ?? string.Empty,
                IsActive: dto.IsActive,
                UpdatedAt: dto.UpdatedAt,
                Rev: dto.Rev
            );
            
            // Update cache
            await _cacheLock.WaitAsync();
            try
            {
                if (_cachedSnippets.TryGetValue(accountId, out var cached))
                {
                    var existing = cached.FindIndex(s => s.SnippetId == info.SnippetId);
                    if (existing >= 0)
                        cached[existing] = info;
                    else
                        cached.Add(info);
                    
                    if (ENABLE_DIAGNOSTIC_LOGGING)
                        Debug.WriteLine($"[ApiSnippetServiceAdapter][Upsert] Updated cache for snippet {info.SnippetId}");
                }
            }
            finally
            {
                _cacheLock.Release();
            }
            
            return info;
        }

        public async Task<SnippetInfo?> ToggleActiveAsync(long accountId, long snippetId)
        {
            var dto = await _apiClient.ToggleSnippetAsync(accountId, snippetId);
            if (dto == null) return null;

            var info = new SnippetInfo(
                SnippetId: dto.SnippetId,
                AccountId: dto.AccountId,
                TriggerText: dto.TriggerText,
                SnippetText: dto.SnippetText,
                SnippetAst: dto.SnippetAst,
                Description: dto.Description ?? string.Empty,
                IsActive: dto.IsActive,
                UpdatedAt: dto.UpdatedAt,
                Rev: dto.Rev
            );
            
            // Update cache
            await _cacheLock.WaitAsync();
            try
            {
                if (_cachedSnippets.TryGetValue(accountId, out var cached))
                {
                    var existing = cached.FindIndex(s => s.SnippetId == info.SnippetId);
                    if (existing >= 0)
                        cached[existing] = info;
                    
                    if (ENABLE_DIAGNOSTIC_LOGGING)
                        Debug.WriteLine($"[ApiSnippetServiceAdapter][Toggle] Updated cache for snippet {info.SnippetId}");
                }
            }
            finally
            {
                _cacheLock.Release();
            }
            
            return info;
        }

        public async Task<bool> DeleteSnippetAsync(long accountId, long snippetId)
        {
            var result = await _apiClient.DeleteSnippetAsync(accountId, snippetId);
            
            if (result)
            {
                // Update cache
                await _cacheLock.WaitAsync();
                try
                {
                    if (_cachedSnippets.TryGetValue(accountId, out var cached))
                    {
                        cached.RemoveAll(s => s.SnippetId == snippetId);
                        if (ENABLE_DIAGNOSTIC_LOGGING)
                            Debug.WriteLine($"[ApiSnippetServiceAdapter][Delete] Removed snippet {snippetId} from cache");
                    }
                }
                finally
                {
                    _cacheLock.Release();
                }
            }
            
            return result;
        }

        public async Task RefreshSnippetsAsync(long accountId)
        {
            if (ENABLE_DIAGNOSTIC_LOGGING)
                Debug.WriteLine($"[ApiSnippetServiceAdapter][Refresh] Reloading from API for account {accountId}");
            _loaded = false;
            await PreloadAsync(accountId);
        }
    }
}
