using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Wysg.Musm.Radium.Services.Adapters
{
    /// <summary>
    /// Adapter that implements IPhraseService using the Radium API instead of direct database access.
    /// Provides phrase management (create, read, update, delete) via REST API calls.
    /// </summary>
    public sealed class ApiPhraseServiceAdapter : IPhraseService
    {
        private readonly RadiumApiClient _apiClient;
        private long _accountId;
        private List<PhraseInfo> _cachedPhrases = new();

        public ApiPhraseServiceAdapter(RadiumApiClient apiClient)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
            Debug.WriteLine("[ApiPhraseServiceAdapter] Initialized (API mode)");
        }

        public async Task PreloadAsync(long accountId)
        {
            _accountId = accountId;
            Debug.WriteLine($"[ApiPhraseServiceAdapter][Preload] Loading phrases for account {accountId}");
            
            try
            {
                var phrases = await _apiClient.GetAllPhrasesAsync(accountId, activeOnly: false);
                _cachedPhrases = phrases.Select(p => new PhraseInfo(
                    p.Id, p.AccountId, p.Text, p.Active, p.UpdatedAt, p.Rev
                )).ToList();
                
                Debug.WriteLine($"[ApiPhraseServiceAdapter][Preload] Cached {_cachedPhrases.Count} phrases");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ApiPhraseServiceAdapter][Preload] Error: {ex.Message}");
                _cachedPhrases = new List<PhraseInfo>();
            }
        }

        // Primary methods - account-scoped
        public Task<IReadOnlyList<string>> GetPhrasesForAccountAsync(long accountId)
        {
            var texts = _cachedPhrases
                .Where(p => p.Active && p.AccountId == accountId)
                .Select(p => p.Text)
                .ToList();
            return Task.FromResult<IReadOnlyList<string>>(texts);
        }

        public Task<IReadOnlyList<string>> GetPhrasesByPrefixAccountAsync(long accountId, string prefix, int limit = 50)
        {
            var texts = _cachedPhrases
                .Where(p => p.Active && p.AccountId == accountId && p.Text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .Take(limit)
                .Select(p => p.Text)
                .ToList();
            return Task.FromResult<IReadOnlyList<string>>(texts);
        }

        // Global phrases (not supported in API mode - return empty)
        public Task<IReadOnlyList<string>> GetGlobalPhrasesAsync()
            => Task.FromResult<IReadOnlyList<string>>(new List<string>());

        public Task<IReadOnlyList<string>> GetGlobalPhrasesByPrefixAsync(string prefix, int limit = 50)
            => Task.FromResult<IReadOnlyList<string>>(new List<string>());

        // Combined phrases (account + global, but global not supported)
        public Task<IReadOnlyList<string>> GetCombinedPhrasesAsync(long accountId)
            => GetPhrasesForAccountAsync(accountId);

        public Task<IReadOnlyList<string>> GetCombinedPhrasesByPrefixAsync(long accountId, string prefix, int limit = 50)
            => GetPhrasesByPrefixAccountAsync(accountId, prefix, limit);

        // For highlighting
        public Task<IReadOnlyList<string>> GetAllPhrasesForHighlightingAsync(long accountId)
            => GetPhrasesForAccountAsync(accountId);

        // Deprecated methods (backward compatibility)
        public Task<IReadOnlyList<string>> GetPhrasesForTenantAsync(long tenantId)
            => GetPhrasesForAccountAsync(tenantId);

        public Task<IReadOnlyList<string>> GetPhrasesByPrefixAsync(long tenantId, string prefix, int limit = 50)
            => GetPhrasesByPrefixAccountAsync(tenantId, prefix, limit);

        // Metadata methods
        public Task<IReadOnlyList<PhraseInfo>> GetAllPhraseMetaAsync(long accountId)
        {
            var phrases = _cachedPhrases.Where(p => p.AccountId == accountId).ToList();
            return Task.FromResult<IReadOnlyList<PhraseInfo>>(phrases);
        }

        public Task<IReadOnlyList<PhraseInfo>> GetAllGlobalPhraseMetaAsync()
            => Task.FromResult<IReadOnlyList<PhraseInfo>>(new List<PhraseInfo>());

        public Task<IReadOnlyList<PhraseInfo>> GetAllNonGlobalPhraseMetaAsync(int take = 500)
        {
            var phrases = _cachedPhrases.Take(take).ToList();
            return Task.FromResult<IReadOnlyList<PhraseInfo>>(phrases);
        }

        // Write operations
        public async Task<PhraseInfo> UpsertPhraseAsync(long? accountId, string text, bool active = true)
        {
            if (!accountId.HasValue)
                throw new NotSupportedException("Global phrases not supported in API mode");

            try
            {
                var dto = await _apiClient.UpsertPhraseAsync(accountId.Value, text, active);
                var info = new PhraseInfo(dto.Id, dto.AccountId, dto.Text, dto.Active, dto.UpdatedAt, dto.Rev);
                
                // Update cache
                var existing = _cachedPhrases.FindIndex(p => p.Id == dto.Id);
                if (existing >= 0)
                    _cachedPhrases[existing] = info;
                else
                    _cachedPhrases.Add(info);
                
                Debug.WriteLine($"[ApiPhraseServiceAdapter][Upsert] Success: {text}");
                return info;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ApiPhraseServiceAdapter][Upsert] Error: {ex.Message}");
                throw;
            }
        }

        public async Task<PhraseInfo?> ToggleActiveAsync(long? accountId, long phraseId)
        {
            if (!accountId.HasValue)
                throw new NotSupportedException("Global phrases not supported in API mode");

            try
            {
                await _apiClient.TogglePhraseActiveAsync(accountId.Value, phraseId);
                
                // Update cache
                var existing = _cachedPhrases.Find(p => p.Id == phraseId);
                if (existing != null)
                {
                    var updated = existing with { Active = !existing.Active };
                    _cachedPhrases[_cachedPhrases.IndexOf(existing)] = updated;
                    return updated;
                }
                
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ApiPhraseServiceAdapter][Toggle] Error: {ex.Message}");
                return null;
            }
        }

        public Task<PhraseInfo?> UpdatePhraseTextAsync(long? accountId, long phraseId, string newText)
        {
            // Not directly supported - would need to delete and recreate
            throw new NotSupportedException("UpdatePhraseTextAsync not supported in API mode - use delete + upsert");
        }

        public Task<(int converted, int duplicatesRemoved)> ConvertToGlobalPhrasesAsync(long accountId, IEnumerable<long> phraseIds)
        {
            throw new NotSupportedException("Converting to global phrases not supported in API mode");
        }

        public async Task RefreshPhrasesAsync(long accountId)
        {
            await PreloadAsync(accountId);
        }

        public Task RefreshGlobalPhrasesAsync()
        {
            // Global phrases not supported
            return Task.CompletedTask;
        }

        public Task<long?> GetAnyAccountIdAsync()
        {
            return Task.FromResult<long?>(_accountId > 0 ? _accountId : null);
        }
    }
}
