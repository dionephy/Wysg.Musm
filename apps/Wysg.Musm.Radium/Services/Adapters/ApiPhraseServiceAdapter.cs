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
        private volatile bool _loaded;
        private readonly System.Threading.SemaphoreSlim _loadLock = new(1,1);
        private List<PhraseInfo> _cachedGlobal = new();

        public ApiPhraseServiceAdapter(RadiumApiClient apiClient)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
            Debug.WriteLine("[ApiPhraseServiceAdapter] Initialized (API mode)");
        }

        public async Task PreloadAsync(long accountId)
        {
            _accountId = accountId;
            Debug.WriteLine($"[ApiPhraseServiceAdapter][Preload] Loading phrases for account {accountId}");
            
            // Skip if no auth token set (would result in 404)
            if (_apiClient == null)
            {
                Debug.WriteLine("[ApiPhraseServiceAdapter][Preload] Skipped: No API client");
                _cachedPhrases = new List<PhraseInfo>();
                _cachedGlobal = new List<PhraseInfo>();
                _loaded = false;
                return;
            }
            
            try
            {
                Debug.WriteLine($"[ApiPhraseServiceAdapter][Preload] Calling GetAllPhrasesAsync for account={accountId}");
                var phrases = await _apiClient.GetAllPhrasesAsync(accountId, activeOnly: false);
                Debug.WriteLine($"[ApiPhraseServiceAdapter][Preload] Received {phrases.Count} account phrases");
                
                Debug.WriteLine($"[ApiPhraseServiceAdapter][Preload] Calling GetGlobalPhrasesAsync");
                var globals = await _apiClient.GetGlobalPhrasesAsync(activeOnly: false);
                Debug.WriteLine($"[ApiPhraseServiceAdapter][Preload] Received {globals.Count} global phrases");
                
                _cachedPhrases = phrases.Select(p => new PhraseInfo(
                    p.Id, p.AccountId, p.Text, p.Active, p.UpdatedAt, p.Rev
                )).ToList();
                _cachedGlobal = globals.Select(p => new PhraseInfo(
                    p.Id, p.AccountId, p.Text, p.Active, p.UpdatedAt, p.Rev
                )).ToList();
                
                Debug.WriteLine($"[ApiPhraseServiceAdapter][Preload] Cached {_cachedPhrases.Count} phrases");
                Debug.WriteLine($"[ApiPhraseServiceAdapter][Preload] Cached {_cachedGlobal.Count} GLOBAL phrases");
                _loaded = true;
                Debug.WriteLine($"[ApiPhraseServiceAdapter][State] loaded={_loaded} accountId={_accountId}");
            }
            catch (System.Net.Http.HttpRequestException ex) when (ex.Message.Contains("404"))
            {
                Debug.WriteLine($"[ApiPhraseServiceAdapter][Preload] 404 ERROR - API endpoint not found!");
                Debug.WriteLine($"[ApiPhraseServiceAdapter][Preload] POSSIBLE CAUSES:");
                Debug.WriteLine($"[ApiPhraseServiceAdapter][Preload] 1. API server not running on http://localhost:5205");
                Debug.WriteLine($"[ApiPhraseServiceAdapter][Preload] 2. GlobalPhrasesController not registered");
                Debug.WriteLine($"[ApiPhraseServiceAdapter][Preload] 3. Missing [Authorize] authentication");
                Debug.WriteLine($"[ApiPhraseServiceAdapter][Preload] Full error: {ex.Message}");
                _cachedPhrases = new List<PhraseInfo>();
                _cachedGlobal = new List<PhraseInfo>();
                _loaded = false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ApiPhraseServiceAdapter][Preload] ERROR: {ex.GetType().Name}: {ex.Message}");
                if (ex.InnerException != null)
                    Debug.WriteLine($"[ApiPhraseServiceAdapter][Preload] INNER: {ex.InnerException.Message}");
                _cachedPhrases = new List<PhraseInfo>();
                _cachedGlobal = new List<PhraseInfo>();
                _loaded = false;
            }
        }

        private async Task EnsureLoadedAsync(long accountId)
        {
            if (_loaded && _accountId == accountId) return;
            Debug.WriteLine($"[ApiPhraseServiceAdapter][EnsureLoaded] START accountId={accountId} loaded={_loaded} cachedCount={_cachedPhrases.Count}");
            await _loadLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_loaded && _accountId == accountId) return; // double-check inside lock
                await PreloadAsync(accountId).ConfigureAwait(false);
            }
            finally { _loadLock.Release(); }
            Debug.WriteLine($"[ApiPhraseServiceAdapter][EnsureLoaded] END accountId={accountId} loaded={_loaded} cachedCount={_cachedPhrases.Count}");
        }

        // Primary methods - account-scoped
        public Task<IReadOnlyList<string>> GetPhrasesForAccountAsync(long accountId)
        {
            return GetPhrasesForAccountInternalAsync(accountId);
        }

        private async Task<IReadOnlyList<string>> GetPhrasesForAccountInternalAsync(long accountId)
        {
            Debug.WriteLine($"[ApiPhraseServiceAdapter][GetPhrasesForAccount] accountId={accountId}");
            if (accountId <= 0) return Array.Empty<string>();
            await EnsureLoadedAsync(accountId).ConfigureAwait(false);
            var texts = _cachedPhrases.Where(p => p.Active && p.AccountId == accountId).Select(p => p.Text).ToList();
            Debug.WriteLine($"[ApiPhraseServiceAdapter][GetPhrasesForAccount] count={texts.Count}");
            return texts;
        }

        public Task<IReadOnlyList<string>> GetPhrasesByPrefixAccountAsync(long accountId, string prefix, int limit = 15)
        {
            return GetPhrasesByPrefixInternalAsync(accountId, prefix, limit);
        }

        private async Task<IReadOnlyList<string>> GetPhrasesByPrefixInternalAsync(long accountId, string prefix, int limit)
        {
            Debug.WriteLine($"[ApiPhraseServiceAdapter][GetByPrefix] accountId={accountId} prefix='{prefix}' limit={limit}");
            if (accountId <= 0 || string.IsNullOrWhiteSpace(prefix)) return Array.Empty<string>();
            await EnsureLoadedAsync(accountId).ConfigureAwait(false);
            var texts = _cachedPhrases
                .Where(p => p.Active && p.AccountId == accountId && p.Text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .OrderBy(p => p.Text.Length).ThenBy(p => p.Text)
                .Take(limit)
                .Select(p => p.Text)
                .ToList();
            Debug.WriteLine($"[ApiPhraseServiceAdapter][GetByPrefix] returned={texts.Count}");
            return texts;
        }

        // Global phrases (not supported in API mode - return empty)
        public Task<IReadOnlyList<string>> GetGlobalPhrasesAsync()
            => Task.FromResult<IReadOnlyList<string>>(_cachedGlobal.Where(p => p.Active).Select(p => p.Text).ToList());

        public Task<IReadOnlyList<string>> GetGlobalPhrasesByPrefixAsync(string prefix, int limit = 15)
        {
            // FIXED: Fetch ALL matching global phrases, sort, then take top 15
            if (string.IsNullOrWhiteSpace(prefix)) 
                return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
            
            var matches = _cachedGlobal
                .Where(p => p.Active && p.Text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .Select(p => p.Text)
                .OrderBy(p => p.Length)
                .ThenBy(p => p, StringComparer.OrdinalIgnoreCase)
                .Take(limit)
                .ToList();
            
            Debug.WriteLine($"[ApiPhraseServiceAdapter][GetGlobalByPrefix] prefix='{prefix}' matched={matches.Count}");
            return Task.FromResult<IReadOnlyList<string>>(matches);
        }

        // Combined phrases (account + global, but global not supported)
        public async Task<IReadOnlyList<string>> GetCombinedPhrasesAsync(long accountId)
        {
            var acct = await GetPhrasesForAccountInternalAsync(accountId);
            var globals = _cachedGlobal.Where(p => p.Active).Select(p => p.Text);
            var combined = acct.Concat(globals).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            Debug.WriteLine($"[ApiPhraseServiceAdapter][GetCombinedPhrasesAsync] Account={acct.Count}, Global={globals.Count()}, Combined={combined.Count}");
            return combined;
        }

        public async Task<IReadOnlyList<string>> GetCombinedPhrasesByPrefixAsync(long accountId, string prefix, int limit = 15)
        {
            Debug.WriteLine($"[ApiPhraseServiceAdapter][GetCombinedByPrefix] prefix='{prefix}', limit={limit}");
            
            // FIXED: Fetch ALL matching phrases (not limited to 15), then sort and take top 15
            // This ensures we get the best matches (shortest first) instead of just the first 15 found
            await EnsureLoadedAsync(accountId).ConfigureAwait(false);
            
            // Get ALL account phrases matching prefix (no limit yet)
            var accountMatches = _cachedPhrases
                .Where(p => p.Active && p.AccountId == accountId && p.Text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .Select(p => p.Text);
            
            // Get ALL global phrases matching prefix (no limit yet)
            var globalMatches = _cachedGlobal
                .Where(p => p.Active && p.Text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .Select(p => p.Text);
            
            // Combine and remove duplicates
            var allMatches = accountMatches.Concat(globalMatches).Distinct(StringComparer.OrdinalIgnoreCase);
            
            // NOW sort by length (shorter first) and alphabetically, THEN take top 15
            var sortedAndLimited = allMatches
                .OrderBy(p => p.Length)
                .ThenBy(p => p, StringComparer.OrdinalIgnoreCase)
                .Take(limit)
                .ToList();
            
            Debug.WriteLine($"[ApiPhraseServiceAdapter][GetCombinedByPrefix] Account={accountMatches.Count()}, Global={globalMatches.Count()}, AllMatches={allMatches.Count()}, Final={sortedAndLimited.Count}");
            return sortedAndLimited;
        }

        // For highlighting - returns COMBINED (account + global) phrases UNFILTERED
        public async Task<IReadOnlyList<string>> GetAllPhrasesForHighlightingAsync(long accountId)
        {
            Debug.WriteLine($"[ApiPhraseServiceAdapter][GetAllForHighlighting] accountId={accountId}");
            if (accountId <= 0) return Array.Empty<string>();
            await EnsureLoadedAsync(accountId).ConfigureAwait(false);
            
            // Combine account + global phrases WITHOUT filtering (syntax highlighting needs ALL phrases)
            var accountTexts = _cachedPhrases.Where(p => p.Active && p.AccountId == accountId).Select(p => p.Text);
            var globalTexts = _cachedGlobal.Where(p => p.Active).Select(p => p.Text);
            
            var combined = new HashSet<string>(accountTexts, StringComparer.OrdinalIgnoreCase);
            foreach (var global in globalTexts)
                combined.Add(global);
            
            Debug.WriteLine($"[ApiPhraseServiceAdapter][GetAllForHighlighting] account={accountTexts.Count()} global={globalTexts.Count()} combined={combined.Count}");
            return combined.OrderBy(t => t).ToList();
        }

        // Deprecated methods (backward compatibility)
        public Task<IReadOnlyList<string>> GetPhrasesForTenantAsync(long tenantId)
            => GetPhrasesForAccountAsync(tenantId);

        public Task<IReadOnlyList<string>> GetPhrasesByPrefixAsync(long tenantId, string prefix, int limit = 15)
            => GetPhrasesByPrefixAccountAsync(tenantId, prefix, limit);

        // Metadata methods
        public Task<IReadOnlyList<PhraseInfo>> GetAllPhraseMetaAsync(long accountId)
        {
            return GetAllPhraseMetaInternalAsync(accountId);
        }

        private async Task<IReadOnlyList<PhraseInfo>> GetAllPhraseMetaInternalAsync(long accountId)
        {
            Debug.WriteLine($"[ApiPhraseServiceAdapter][GetAllMeta] accountId={accountId}");
            if (accountId <= 0) return Array.Empty<PhraseInfo>();
            await EnsureLoadedAsync(accountId).ConfigureAwait(false);
            var phrases = _cachedPhrases.Where(p => p.AccountId == accountId).ToList();
            Debug.WriteLine($"[ApiPhraseServiceAdapter][GetAllMeta] metaCount={phrases.Count}");
            return phrases;
        }

        public Task<IReadOnlyList<PhraseInfo>> GetAllGlobalPhraseMetaAsync()
            => Task.FromResult<IReadOnlyList<PhraseInfo>>(_cachedGlobal.ToList());

        public Task<IReadOnlyList<PhraseInfo>> GetAllNonGlobalPhraseMetaAsync(int take = 500)
        {
            var phrases = _cachedPhrases.Take(take).ToList();
            return Task.FromResult<IReadOnlyList<PhraseInfo>>(phrases);
        }

        // Write operations
        public async Task<PhraseInfo> UpsertPhraseAsync(long? accountId, string text, bool active = true)
        {
            if (!accountId.HasValue)
            {
                Debug.WriteLine($"[ApiPhraseServiceAdapter][UpsertGlobal] text='{text}' active={active}");
                var dtoGlobal = await _apiClient.UpsertGlobalPhraseAsync(text, active);
                var infoGlobal = new PhraseInfo(dtoGlobal.Id, dtoGlobal.AccountId, dtoGlobal.Text, dtoGlobal.Active, dtoGlobal.UpdatedAt, dtoGlobal.Rev);
                var idxG = _cachedGlobal.FindIndex(p => p.Id == dtoGlobal.Id);
                if (idxG >= 0) _cachedGlobal[idxG] = infoGlobal; else _cachedGlobal.Add(infoGlobal);
                return infoGlobal;
            }
            Debug.WriteLine($"[ApiPhraseServiceAdapter][Upsert] accountId={accountId} text='{text}' active={active}");
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
            {
                Debug.WriteLine($"[ApiPhraseServiceAdapter][ToggleGlobal] phraseId={phraseId}");
                await _apiClient.ToggleGlobalPhraseAsync(phraseId);
                var existingG = _cachedGlobal.Find(p => p.Id == phraseId);
                if (existingG != null)
                {
                    var updated = existingG with { Active = !existingG.Active };
                    _cachedGlobal[_cachedGlobal.IndexOf(existingG)] = updated;
                    return updated;
                }
                return null;
            }
            Debug.WriteLine($"[ApiPhraseServiceAdapter][Toggle] accountId={accountId} phraseId={phraseId}");
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

        public async Task<(int converted, int duplicatesRemoved)> ConvertToGlobalPhrasesAsync(long accountId, IEnumerable<long> phraseIds)
        {
            var ids = phraseIds?.Distinct().ToList() ?? new List<long>();
            if (ids.Count == 0)
            {
                return (0, 0);
            }

            Debug.WriteLine($"[ApiPhraseServiceAdapter][ConvertToGlobal] accountId={accountId} ids={ids.Count}");

            // Execute conversion via API
            var response = await _apiClient.ConvertPhrasesToGlobalAsync(accountId, ids);

            // Refresh caches to reflect moved phrases
            _cachedPhrases = _cachedPhrases.Where(p => !ids.Contains(p.Id)).ToList();
            _cachedGlobal = new List<PhraseInfo>();
            _loaded = false;
            await PreloadAsync(accountId).ConfigureAwait(false);

            Debug.WriteLine($"[ApiPhraseServiceAdapter][ConvertToGlobal] converted={response.Converted}, duplicatesRemoved={response.DuplicatesRemoved}");
            return (response.Converted, response.DuplicatesRemoved);
        }

        public async Task RefreshPhrasesAsync(long accountId)
        {
            Debug.WriteLine($"[ApiPhraseServiceAdapter][Refresh] accountId={accountId}");
            _loaded = false; // force reload
            await PreloadAsync(accountId);
        }

        public Task RefreshGlobalPhrasesAsync()
        {
            _cachedGlobal = new List<PhraseInfo>();
            if (_accountId > 0) return PreloadAsync(_accountId);
            return Task.CompletedTask;
        }

        public Task<long?> GetAnyAccountIdAsync()
        {
            return Task.FromResult<long?>(_accountId > 0 ? _accountId : null);
        }
    }
}
