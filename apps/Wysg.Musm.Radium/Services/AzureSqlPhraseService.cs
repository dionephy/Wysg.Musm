using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace Wysg.Musm.Radium.Services
{
    /// <summary>
    /// Azure SQL implementation of <see cref="IPhraseService"/> replacing Postgres specific features (RETURNING, ON CONFLICT).
    /// Revision bump handled by trigger (rev increments only on logical change of text/active) ? we simply SELECT after mutation.
    /// Flow kept synchronous (FR-258..260) with per-account update lock (same semantics as Postgres variant).
    /// Supports global phrases (FR-273..278) with account_id nullable.
    /// </summary>
    public sealed class AzureSqlPhraseService : IPhraseService
    {
        private readonly IRadiumLocalSettings _settings;
        private readonly IPhraseCache _cache;
        private readonly ConcurrentDictionary<long, AccountPhraseState> _states = new();
        private const long GLOBAL_KEY = -1; // Key for global phrases state

        private sealed class PhraseRow
        {
            public long Id { get; init; }
            public long? AccountId { get; init; }  // Nullable for global phrases
            public string Text { get; set; } = string.Empty;
            public bool Active { get; set; }
            public DateTime UpdatedAt { get; set; }
            public long Rev { get; set; }
        }
        private sealed class AccountPhraseState
        {
            public long? AccountId { get; }  // Nullable: null = global phrases
            public readonly SemaphoreSlim UpdateLock = new(1,1);
            public readonly Dictionary<long, PhraseRow> ById = new();
            public AccountPhraseState(long? id) { AccountId = id; }
        }

        public AzureSqlPhraseService(IRadiumLocalSettings settings, IPhraseCache cache)
        { _settings = settings; _cache = cache; }

        private string ConnectionString => _settings.CentralConnectionString ?? throw new InvalidOperationException("Central connection string not configured");
        private SqlConnection CreateConnection() => new SqlConnection(ConnectionString);

        public Task PreloadAsync(long accountId) => Task.CompletedTask; // no-op (lazy first access load)

        public async Task<IReadOnlyList<string>> GetPhrasesForAccountAsync(long accountId)
        {
            if (accountId <= 0) return Array.Empty<string>();
            var state = _states.GetOrAdd(accountId, id => new AccountPhraseState(id));
            if (state.ById.Count == 0) await LoadSnapshotAsync(state).ConfigureAwait(false);
            return state.ById.Values.Where(r => r.Active).Select(r => r.Text).OrderBy(t => t).Take(500).ToList();
        }

        public async Task<IReadOnlyList<string>> GetPhrasesByPrefixAccountAsync(long accountId, string prefix, int limit = 50)
        {
            if (accountId <= 0 || string.IsNullOrWhiteSpace(prefix)) return Array.Empty<string>();
            var state = _states.GetOrAdd(accountId, id => new AccountPhraseState(id));
            if (state.ById.Count == 0) await LoadSnapshotAsync(state).ConfigureAwait(false);
            return state.ById.Values.Where(r => r.Active && r.Text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .OrderBy(r => r.Text.Length).ThenBy(r => r.Text).Take(limit).Select(r => r.Text).ToList();
        }

        // ============================================================================
        // Global Phrases Support (FR-273..FR-278)
        // ============================================================================
        
        /// <summary>
        /// Get all active global phrases (account_id IS NULL) with 3-word filtering for completion.
        /// 
        /// FR-completion-filter-2025-01-20: Global phrases are filtered to ¡Â3 words to reduce
        /// clutter in completion popup. Account-specific phrases are NOT filtered.
        /// 
        /// Filtering rationale:
        /// - Global phrases contain long medical terms (e.g., "ligament of distal interphalangeal joint...")
        /// - These long phrases crowd the completion window
        /// - Short phrases (¡Â3 words) remain useful for quick completion
        /// - Users can still access long phrases via syntax highlighting, phrase manager, etc.
        /// </summary>
        public async Task<IReadOnlyList<string>> GetGlobalPhrasesAsync()
        {
            var state = _states.GetOrAdd(GLOBAL_KEY, _ => new AccountPhraseState(null));
            if (state.ById.Count == 0) 
                await LoadGlobalSnapshotAsync(state).ConfigureAwait(false);
            
            // Apply 3-word filter for completion window (FR-completion-filter-2025-01-20)
            var allActive = state.ById.Values.Where(r => r.Active).ToList();
            Debug.WriteLine($"[AzureSqlPhraseService][GetGlobalPhrasesAsync] Total active global phrases: {allActive.Count}");
            
            var filtered = allActive.Where(r => CountWords(r.Text) <= 3).ToList();
            Debug.WriteLine($"[AzureSqlPhraseService][GetGlobalPhrasesAsync] After 3-word filter: {filtered.Count}");
            
            // Log filtering statistics and examples for debugging
            if (allActive.Count > 0 && allActive.Count - filtered.Count > 0)
            {
                Debug.WriteLine($"[AzureSqlPhraseService][GetGlobalPhrasesAsync] Filtered out {allActive.Count - filtered.Count} long phrases");
                
                // Show first 3 examples of filtered phrases for debugging
                var examples = allActive.Where(r => CountWords(r.Text) > 3).Take(3).ToList();
                foreach (var ex in examples)
                {
                    Debug.WriteLine($"  FILTERED: \"{ex.Text}\" ({CountWords(ex.Text)} words)");
                }
            }
            
            return filtered.Select(r => r.Text).OrderBy(t => t).Take(500).ToList();
        }

        /// <summary>
        /// Get global phrases matching a prefix with 3-word filtering for completion.
        /// 
        /// This is the primary method used by completion popup when typing.
        /// Filtering logic is identical to GetGlobalPhrasesAsync().
        /// </summary>
        /// <param name="prefix">Case-insensitive prefix to match (e.g., "li" matches "ligament", "liver")</param>
        /// <param name="limit">Maximum number of results to return (default 50)</param>
        public async Task<IReadOnlyList<string>> GetGlobalPhrasesByPrefixAsync(string prefix, int limit = 50)
        {
            if (string.IsNullOrWhiteSpace(prefix)) return Array.Empty<string>();
            
            var state = _states.GetOrAdd(GLOBAL_KEY, _ => new AccountPhraseState(null));
            if (state.ById.Count == 0) 
                await LoadGlobalSnapshotAsync(state).ConfigureAwait(false);
            
            Debug.WriteLine($"[AzureSqlPhraseService][GetGlobalPhrasesByPrefixAsync] prefix='{prefix}', limit={limit}");
            
            // Find all phrases starting with prefix (case-insensitive)
            var matching = state.ById.Values
                .Where(r => r.Active && r.Text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .ToList();
            Debug.WriteLine($"[AzureSqlPhraseService][GetGlobalPhrasesByPrefixAsync] Found {matching.Count} matches for prefix '{prefix}'");
            
            // Apply 3-word filter for completion window (FR-completion-filter-2025-01-20)
            var filtered = matching.Where(r => CountWords(r.Text) <= 3).ToList();
            Debug.WriteLine($"[AzureSqlPhraseService][GetGlobalPhrasesByPrefixAsync] After 3-word filter: {filtered.Count}");
            
            // Log filtering statistics for debugging
            if (matching.Count > filtered.Count)
            {
                Debug.WriteLine($"[AzureSqlPhraseService][GetGlobalPhrasesByPrefixAsync] Filtered out {matching.Count - filtered.Count} long phrases");
                
                var examples = matching.Where(r => CountWords(r.Text) > 3).Take(3).ToList();
                foreach (var ex in examples)
                {
                    Debug.WriteLine($"  FILTERED: \"{ex.Text}\" ({CountWords(ex.Text)} words)");
                }
            }
            
            // Sort by length first (shorter = more likely to be what user wants), then alphabetically
            return filtered
                .OrderBy(r => r.Text.Length)
                .ThenBy(r => r.Text)
                .Take(limit)
                .Select(r => r.Text)
                .ToList();
        }
        
        /// <summary>
        /// Count words in a phrase by splitting on whitespace.
        /// 
        /// FR-completion-filter-2025-01-20: Used to filter global phrases to ¡Â3 words for completion window.
        /// 
        /// Word counting rules:
        /// - Split on space, tab, CR, LF
        /// - Remove empty entries (multiple spaces count as one separator)
        /// - "no evidence" = 2 words
        /// - "ligament of distal interphalangeal joint" = 5 words (filtered out)
        /// </summary>
        /// <param name="text">Phrase text to count words in</param>
        /// <returns>Number of words (0 if null/whitespace)</returns>
        private static int CountWords(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0;
            
            var count = text.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length;
            
            // Debug logging for long phrases containing "ligament" (sample debugging - can be removed in production)
            if (count > 3 && text.Contains("ligament", StringComparison.OrdinalIgnoreCase))
            {
                Debug.WriteLine($"[AzureSqlPhraseService][CountWords] \"{text}\" = {count} words");
            }
            
            return count;
        }

        // ============================================================================
        // Combined Phrases (Global + Account-Specific) - FR-274, T385
        // ============================================================================
        
        /// <summary>
        /// Get combined phrases for completion: filtered global phrases + all account-specific phrases.
        /// 
        /// Filtering policy (FR-completion-filter-2025-01-20):
        /// - Global phrases: Filtered to ¡Â3 words (reduces clutter)
        /// - Account-specific phrases: NOT filtered (user's custom phrases, always show all)
        /// 
        /// This method is used by PhraseCompletionProvider to populate the completion cache.
        /// </summary>
        public async Task<IReadOnlyList<string>> GetCombinedPhrasesAsync(long accountId)
        {
            // Global phrases are pre-filtered to ¡Â3 words
            var globalPhrases = await GetGlobalPhrasesAsync().ConfigureAwait(false);
            // Account phrases are NOT filtered
            var accountPhrases = await GetPhrasesForAccountAsync(accountId).ConfigureAwait(false);
            
            // Combine using HashSet to eliminate duplicates (case-insensitive)
            var combined = new HashSet<string>(accountPhrases, StringComparer.OrdinalIgnoreCase);
            foreach (var global in globalPhrases)
                combined.Add(global);
                
            return combined.OrderBy(t => t).ToList();
        }

        /// <summary>
        /// Get combined phrases matching a prefix: filtered global + all account-specific.
        /// Used by completion popup for prefix-based filtering.
        /// </summary>
        public async Task<IReadOnlyList<string>> GetCombinedPhrasesByPrefixAsync(long accountId, string prefix, int limit = 50)
        {
            if (string.IsNullOrWhiteSpace(prefix)) return Array.Empty<string>();
            
            // Global phrases are pre-filtered to ¡Â3 words in GetGlobalPhrasesByPrefixAsync
            var globalPhrases = await GetGlobalPhrasesByPrefixAsync(prefix, limit).ConfigureAwait(false);
            // Account-specific phrases are NOT filtered (no word limit)
            var accountPhrases = await GetPhrasesByPrefixAccountAsync(accountId, prefix, limit).ConfigureAwait(false);
            
            var combined = new HashSet<string>(accountPhrases, StringComparer.OrdinalIgnoreCase);
            foreach (var global in globalPhrases)
                combined.Add(global);
                
            return combined.OrderBy(t => t.Length).ThenBy(t => t).Take(limit).ToList();
        }

        // ============================================================================
        // Unfiltered Phrases for Syntax Highlighting
        // ============================================================================
        
        /// <summary>
        /// Get ALL phrases (global + account) WITHOUT filtering for syntax highlighting.
        /// 
        /// IMPORTANT: This method does NOT apply the 3-word filter!
        /// Syntax highlighting needs ALL phrases including long ones (e.g., 
        /// "ligament of distal interphalangeal joint...") to properly highlight medical terminology.
        /// 
        /// Dual storage strategy:
        /// - _states: Stores ALL phrases (used by this method)
        /// - Completion cache: Stores filtered phrases (populated by GetCombinedPhrasesAsync)
        /// </summary>
        public async Task<IReadOnlyList<string>> GetAllPhrasesForHighlightingAsync(long accountId)
        {
            // Get ALL global phrases from state (unfiltered)
            var globalState = _states.GetOrAdd(GLOBAL_KEY, _ => new AccountPhraseState(null));
            if (globalState.ById.Count == 0) 
                await LoadGlobalSnapshotAsync(globalState).ConfigureAwait(false);
            
            // Get ALL account phrases
            var accountState = _states.GetOrAdd(accountId, id => new AccountPhraseState(id));
            if (accountState.ById.Count == 0) 
                await LoadSnapshotAsync(accountState).ConfigureAwait(false);
            
            // Combine WITHOUT filtering - syntax highlighting needs all phrases
            var globalPhrases = globalState.ById.Values.Where(r => r.Active).Select(r => r.Text);
            var accountPhrases = accountState.ById.Values.Where(r => r.Active).Select(r => r.Text);
            
            var combined = new HashSet<string>(accountPhrases, StringComparer.OrdinalIgnoreCase);
            foreach (var global in globalPhrases)
                combined.Add(global);
            
            Debug.WriteLine($"[AzureSqlPhraseService][GetAllPhrasesForHighlightingAsync] accountId={accountId}, total={combined.Count} (unfiltered for highlighting)");
            
            return combined.OrderBy(t => t).ToList();
        }

        [Obsolete] public Task<IReadOnlyList<string>> GetPhrasesForTenantAsync(long tenantId) => GetPhrasesForAccountAsync(tenantId);
        [Obsolete] public Task<IReadOnlyList<string>> GetPhrasesByPrefixAsync(long tenantId, string prefix, int limit = 50) => GetPhrasesByPrefixAccountAsync(tenantId, prefix, limit);

        public async Task<IReadOnlyList<PhraseInfo>> GetAllPhraseMetaAsync(long accountId)
        {
            if (accountId <= 0) return Array.Empty<PhraseInfo>();
            var state = _states.GetOrAdd(accountId, id => new AccountPhraseState(id));
            if (state.ById.Count == 0) await LoadSnapshotAsync(state).ConfigureAwait(false);
            return state.ById.Values.OrderByDescending(r => r.UpdatedAt).Take(1000)
                .Select(r => new PhraseInfo(r.Id, r.AccountId, r.Text, r.Active, r.UpdatedAt, r.Rev)).ToList();
        }

        // NEW: Global phrase metadata
        public async Task<IReadOnlyList<PhraseInfo>> GetAllGlobalPhraseMetaAsync()
        {
            var state = _states.GetOrAdd(GLOBAL_KEY, _ => new AccountPhraseState(null));
            if (state.ById.Count == 0) await LoadGlobalSnapshotAsync(state).ConfigureAwait(false);
            return state.ById.Values.OrderByDescending(r => r.UpdatedAt).Take(1000)
                .Select(r => new PhraseInfo(r.Id, r.AccountId, r.Text, r.Active, r.UpdatedAt, r.Rev)).ToList();
        }

        // NEW: Non-global phrase metadata across all accounts (FR-280)
        public async Task<IReadOnlyList<PhraseInfo>> GetAllNonGlobalPhraseMetaAsync(int take = 500)
        {
            const string sql = @"SELECT TOP (@lim) id, account_id, [text], active, updated_at, rev
                                  FROM radium.phrase WHERE account_id IS NOT NULL
                                  ORDER BY updated_at DESC";
            var list = new List<PhraseInfo>(take);
            await using var con = CreateConnection();
            await con.OpenAsync().ConfigureAwait(false);
            await using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@lim", take);
            await using var rd = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
            while (await rd.ReadAsync().ConfigureAwait(false))
            {
                list.Add(new PhraseInfo(
                    rd.GetInt64(0),
                    rd.GetInt64(1),
                    rd.GetString(2),
                    rd.GetBoolean(3),
                    rd.GetDateTime(4),
                    rd.GetInt64(5)
                ));
            }
            return list;
        }

        public async Task RefreshPhrasesAsync(long accountId)
        {
            if (accountId <= 0) return;
            if (_states.TryGetValue(accountId, out var state))
            {
                await state.UpdateLock.WaitAsync().ConfigureAwait(false);
                try
                {
                    state.ById.Clear();
                    await LoadSnapshotAsync(state).ConfigureAwait(false);
                }
                finally { state.UpdateLock.Release(); }
            }
        }

        // NEW: Refresh global phrases
        public async Task RefreshGlobalPhrasesAsync()
        {
            if (_states.TryGetValue(GLOBAL_KEY, out var state))
            {
                await state.UpdateLock.WaitAsync().ConfigureAwait(false);
                try
                {
                    state.ById.Clear();
                    await LoadGlobalSnapshotAsync(state).ConfigureAwait(false);
                }
                finally { state.UpdateLock.Release(); }
            }
        }

        private async Task LoadSnapshotAsync(AccountPhraseState state)
        {
            const string sql = @"SELECT TOP (100) id, account_id, text, active, updated_at, rev
                                  FROM radium.phrase WHERE account_id=@aid
                                  ORDER BY updated_at DESC";
            await using var con = CreateConnection();
            await con.OpenAsync().ConfigureAwait(false);
            await using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@aid", state.AccountId);
            await using var rd = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
            while (await rd.ReadAsync().ConfigureAwait(false))
            {
                var row = new PhraseRow
                {
                    Id = rd.GetInt64(0),
                    AccountId = rd.GetInt64(1),
                    Text = rd.GetString(2),
                    Active = rd.GetBoolean(3),
                    UpdatedAt = rd.GetDateTime(4),
                    Rev = rd.GetInt64(5)
                };
                state.ById[row.Id] = row;
            }
        }

        private async Task LoadGlobalSnapshotAsync(AccountPhraseState state)
        {
            // Load ALL global phrases (no limit) for accurate existence checks
            const string sql = @"SELECT id, account_id, text, active, updated_at, rev
                                  FROM radium.phrase WHERE account_id IS NULL
                                  ORDER BY updated_at DESC";
            await using var con = CreateConnection();
            await con.OpenAsync().ConfigureAwait(false);
            await using var cmd = new SqlCommand(sql, con);
            await using var rd = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
            while (await rd.ReadAsync().ConfigureAwait(false))
            {
                var row = new PhraseRow
                {
                    Id = rd.GetInt64(0),
                    AccountId = rd.IsDBNull(1) ? null : rd.GetInt64(1),
                    Text = rd.GetString(2),
                    Active = rd.GetBoolean(3),
                    UpdatedAt = rd.GetDateTime(4),
                    Rev = rd.GetInt64(5)
                };
                state.ById[row.Id] = row;
            }
        }

        // MODIFIED: Now accepts nullable accountId
        public async Task<PhraseInfo> UpsertPhraseAsync(long? accountId, string text, bool active = true)
        {
            if (accountId.HasValue && accountId.Value <= 0) 
                throw new ArgumentOutOfRangeException(nameof(accountId));
            
            var key = accountId ?? GLOBAL_KEY;
            var state = _states.GetOrAdd(key, _ => new AccountPhraseState(accountId));
            await state.UpdateLock.WaitAsync().ConfigureAwait(false);
            try
            {
                // Update-first then insert pattern + final SELECT
                string updateSql = accountId.HasValue
                    ? @"UPDATE radium.phrase SET active=@active WHERE account_id=@aid AND [text]=@text"
                    : @"UPDATE radium.phrase SET active=@active WHERE account_id IS NULL AND [text]=@text";
                    
                string insertSql = @"INSERT INTO radium.phrase(account_id,[text],active) VALUES(@aid,@text,@active)";
                
                string selectSql = accountId.HasValue
                    ? @"SELECT TOP (1) id, account_id, [text], active, updated_at, rev FROM radium.phrase WHERE account_id=@aid AND [text]=@text"
                    : @"SELECT TOP (1) id, account_id, [text], active, updated_at, rev FROM radium.phrase WHERE account_id IS NULL AND [text]=@text";
                    
                await using var con = CreateConnection();
                await con.OpenAsync().ConfigureAwait(false);
                await using (var upd = new SqlCommand(updateSql, con))
                {
                    upd.Parameters.AddWithValue("@active", active);
                    if (accountId.HasValue)
                        upd.Parameters.AddWithValue("@aid", accountId.Value);
                    upd.Parameters.AddWithValue("@text", text);
                    var rows = await upd.ExecuteNonQueryAsync().ConfigureAwait(false);
                    if (rows == 0)
                    {
                        await using var ins = new SqlCommand(insertSql, con);
                        if (accountId.HasValue)
                            ins.Parameters.AddWithValue("@aid", accountId.Value);
                        else
                            ins.Parameters.AddWithValue("@aid", DBNull.Value);
                        ins.Parameters.AddWithValue("@text", text);
                        ins.Parameters.AddWithValue("@active", active);
                        try { await ins.ExecuteNonQueryAsync().ConfigureAwait(false); }
                        catch (SqlException sx) when (sx.Number == 2627) // unique violation -> concurrent insert, fallthrough to select
                        { }
                    }
                }
                PhraseInfo info;
                await using (var sel = new SqlCommand(selectSql, con))
                {
                    if (accountId.HasValue)
                        sel.Parameters.AddWithValue("@aid", accountId.Value);
                    sel.Parameters.AddWithValue("@text", text);
                    await using var rd = await sel.ExecuteReaderAsync().ConfigureAwait(false);
                    if (!await rd.ReadAsync().ConfigureAwait(false)) throw new InvalidOperationException("Phrase select failed");
                    info = new PhraseInfo(
                        rd.GetInt64(0), 
                        rd.IsDBNull(1) ? null : rd.GetInt64(1), 
                        rd.GetString(2), 
                        rd.GetBoolean(3), 
                        rd.GetDateTime(4), 
                        rd.GetInt64(5));
                }
                // Failsafe: if we created/updated a GLOBAL phrase, delete all non-global duplicates (FR-281)
                if (!accountId.HasValue)
                {
                    const string delDup = @"DELETE FROM radium.phrase WHERE account_id IS NOT NULL AND [text]=@text";
                    await using var del = new SqlCommand(delDup, con);
                    del.Parameters.AddWithValue("@text", text);
                    _ = await del.ExecuteNonQueryAsync().ConfigureAwait(false);
                }
                // Update snapshots and caches
                UpdateSnapshot(state, info);
                _cache.Clear(accountId ?? GLOBAL_KEY);
                return info;
            }
            finally { state.UpdateLock.Release(); }
        }

        // MODIFIED: Now accepts nullable accountId
        public async Task<PhraseInfo?> ToggleActiveAsync(long? accountId, long phraseId)
        {
            if (accountId.HasValue && accountId.Value <= 0) return null;
            
            var key = accountId ?? GLOBAL_KEY;
            var state = _states.GetOrAdd(key, _ => new AccountPhraseState(accountId));
            await state.UpdateLock.WaitAsync().ConfigureAwait(false);
            try
            {
                string toggleSql = accountId.HasValue
                    ? @"UPDATE radium.phrase SET active = CASE active WHEN 1 THEN 0 ELSE 1 END WHERE account_id=@aid AND id=@pid;"
                    : @"UPDATE radium.phrase SET active = CASE active WHEN 1 THEN 0 ELSE 1 END WHERE account_id IS NULL AND id=@pid;";
                    
                string selectSql = accountId.HasValue
                    ? @"SELECT id, account_id, [text], active, updated_at, rev FROM radium.phrase WHERE id=@pid AND account_id=@aid"
                    : @"SELECT id, account_id, [text], active, updated_at, rev FROM radium.phrase WHERE id=@pid AND account_id IS NULL";
                    
                await using var con = CreateConnection();
                await con.OpenAsync().ConfigureAwait(false);
                await using (var cmd = new SqlCommand(toggleSql, con))
                {
                    if (accountId.HasValue)
                        cmd.Parameters.AddWithValue("@aid", accountId.Value);
                    cmd.Parameters.AddWithValue("@pid", phraseId);
                    var rows = await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                    if (rows == 0) return null;
                }
                await using (var q = new SqlCommand(selectSql, con))
                {
                    if (accountId.HasValue)
                        q.Parameters.AddWithValue("@aid", accountId.Value);
                    q.Parameters.AddWithValue("@pid", phraseId);
                    await using var rd = await q.ExecuteReaderAsync().ConfigureAwait(false);
                    if (!await rd.ReadAsync().ConfigureAwait(false)) return null;
                    var info = new PhraseInfo(
                        rd.GetInt64(0), 
                        rd.IsDBNull(1) ? null : rd.GetInt64(1), 
                        rd.GetString(2), 
                        rd.GetBoolean(3), 
                        rd.GetDateTime(4), 
                        rd.GetInt64(5));
                    UpdateSnapshot(state, info);
                    _cache.Clear(accountId ?? GLOBAL_KEY);
                    return info;
                }
            }
            finally { state.UpdateLock.Release(); }
        }

        // NEW: Update phrase text (FR-SNOMED-EDIT-2025-01-19)
        public async Task<PhraseInfo?> UpdatePhraseTextAsync(long? accountId, long phraseId, string newText)
        {
            if (accountId.HasValue && accountId.Value <= 0) return null;
            if (string.IsNullOrWhiteSpace(newText)) return null;
            
            var key = accountId ?? GLOBAL_KEY;
            var state = _states.GetOrAdd(key, _ => new AccountPhraseState(accountId));
            await state.UpdateLock.WaitAsync().ConfigureAwait(false);
            try
            {
                string updateSql = accountId.HasValue
                    ? @"UPDATE radium.phrase SET [text]=@text WHERE account_id=@aid AND id=@pid;"
                    : @"UPDATE radium.phrase SET [text]=@text WHERE account_id IS NULL AND id=@pid;";
                    
                string selectSql = accountId.HasValue
                    ? @"SELECT id, account_id, [text], active, updated_at, rev FROM radium.phrase WHERE id=@pid AND account_id=@aid"
                    : @"SELECT id, account_id, [text], active, updated_at, rev FROM radium.phrase WHERE id=@pid AND account_id IS NULL";
                    
                await using var con = CreateConnection();
                await con.OpenAsync().ConfigureAwait(false);
                await using (var cmd = new SqlCommand(updateSql, con))
                {
                    if (accountId.HasValue)
                        cmd.Parameters.AddWithValue("@aid", accountId.Value);
                    cmd.Parameters.AddWithValue("@pid", phraseId);
                    cmd.Parameters.AddWithValue("@text", newText.Trim());
                    var rows = await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                    if (rows == 0) return null;
                }
                await using (var q = new SqlCommand(selectSql, con))
                {
                    if (accountId.HasValue)
                        q.Parameters.AddWithValue("@aid", accountId.Value);
                    q.Parameters.AddWithValue("@pid", phraseId);
                    await using var rd = await q.ExecuteReaderAsync().ConfigureAwait(false);
                    if (!await rd.ReadAsync().ConfigureAwait(false)) return null;
                    var info = new PhraseInfo(
                        rd.GetInt64(0), 
                        rd.IsDBNull(1) ? null : rd.GetInt64(1), 
                        rd.GetString(2), 
                        rd.GetBoolean(3), 
                        rd.GetDateTime(4), 
                        rd.GetInt64(5));
                    UpdateSnapshot(state, info);
                    _cache.Clear(accountId ?? GLOBAL_KEY);
                    return info;
                }
            }
            finally { state.UpdateLock.Release(); }
        }

        private static void UpdateSnapshot(AccountPhraseState state, PhraseInfo info)
        {
            if (!state.ById.TryGetValue(info.Id, out var row))
            {
                row = new PhraseRow { Id = info.Id, AccountId = info.AccountId, Text = info.Text, Active = info.Active, UpdatedAt = info.UpdatedAt, Rev = info.Rev };
                state.ById[info.Id] = row;
            }
            else
            {
                row.Text = info.Text; row.Active = info.Active; row.UpdatedAt = info.UpdatedAt; row.Rev = info.Rev;
            }
        }

        // NEW: Convert account phrases to global (FR-279)
        public async Task<(int converted, int duplicatesRemoved)> ConvertToGlobalPhrasesAsync(long accountId, IEnumerable<long> phraseIds)
        {
            if (accountId <= 0) return (0, 0);
            
            var ids = phraseIds.ToList();
            if (ids.Count == 0) return (0, 0);
            
            int converted = 0;
            int duplicatesRemoved = 0;
            
            var accountKey = accountId;
            var accountState = _states.GetOrAdd(accountKey, _ => new AccountPhraseState(accountId));
            var globalState = _states.GetOrAdd(GLOBAL_KEY, _ => new AccountPhraseState(null));
            
            await accountState.UpdateLock.WaitAsync().ConfigureAwait(false);
            await globalState.UpdateLock.WaitAsync().ConfigureAwait(false);
            try
            {
                await using var con = CreateConnection();
                await con.OpenAsync().ConfigureAwait(false);
                
                // Group phrases by text to detect duplicates across accounts
                var phrasesToConvert = new Dictionary<string, List<long>>(StringComparer.OrdinalIgnoreCase);
                
                // Load all phrases to convert
                foreach (var phraseId in ids)
                {
                    const string selectSql = @"SELECT id, [text] FROM radium.phrase WHERE id=@pid AND account_id=@aid";
                    await using var selCmd = new SqlCommand(selectSql, con);
                    selCmd.Parameters.AddWithValue("@pid", phraseId);
                    selCmd.Parameters.AddWithValue("@aid", accountId);
                    
                    await using var rd = await selCmd.ExecuteReaderAsync().ConfigureAwait(false);
                    if (await rd.ReadAsync().ConfigureAwait(false))
                    {
                        var text = rd.GetString(1);
                        if (!phrasesToConvert.ContainsKey(text))
                            phrasesToConvert[text] = new List<long>();
                        phrasesToConvert[text].Add(phraseId);
                    }
                }
                
                // Process each unique phrase text
                foreach (var kvp in phrasesToConvert)
                {
                    var text = kvp.Key;
                    var phIds = kvp.Value;
                    
                    // Check if a global phrase with this text already exists
                    const string checkGlobalSql = @"SELECT TOP (1) id FROM radium.phrase WHERE account_id IS NULL AND [text]=@text";
                    await using var checkCmd = new SqlCommand(checkGlobalSql, con);
                    checkCmd.Parameters.AddWithValue("@text", text);
                    var existingGlobalId = await checkCmd.ExecuteScalarAsync().ConfigureAwait(false);
                    
                    if (existingGlobalId != null)
                    {
                        // Global phrase exists - delete all non-global rows with same text (all accounts)
                        const string deleteAllDupSql = @"DELETE FROM radium.phrase WHERE account_id IS NOT NULL AND [text]=@text";
                        await using var delAllCmd = new SqlCommand(deleteAllDupSql, con);
                        delAllCmd.Parameters.AddWithValue("@text", text);
                        var removed = await delAllCmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                        duplicatesRemoved += removed;

                        foreach (var id in phIds) accountState.ById.Remove(id);
                    }
                    else
                    {
                        // No global phrase exists - convert first occurrence to global
                        var firstId = phIds[0];
                        
                        const string updateSql = @"UPDATE radium.phrase SET account_id = NULL WHERE id=@pid AND account_id=@aid";
                        await using var updCmd = new SqlCommand(updateSql, con);
                        updCmd.Parameters.AddWithValue("@pid", firstId);
                        updCmd.Parameters.AddWithValue("@aid", accountId);
                        var rows = await updCmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                        
                        if (rows > 0)
                        {
                            // Get updated phrase info
                            const string selectSql = @"SELECT id, account_id, [text], active, updated_at, rev 
                                                       FROM radium.phrase WHERE id=@pid AND account_id IS NULL";
                            await using var selCmd = new SqlCommand(selectSql, con);
                            selCmd.Parameters.AddWithValue("@pid", firstId);
                            
                            await using var updRd = await selCmd.ExecuteReaderAsync().ConfigureAwait(false);
                            if (await updRd.ReadAsync().ConfigureAwait(false))
                            {
                                // Remove from account snapshot
                                accountState.ById.Remove(firstId);
                                
                                // Add to global snapshot
                                var globalRow = new PhraseRow
                                {
                                    Id = updRd.GetInt64(0),
                                    AccountId = updRd.IsDBNull(1) ? (long?)null : updRd.GetInt64(1),
                                    Text = updRd.GetString(2),
                                    Active = updRd.GetBoolean(3),
                                    UpdatedAt = updRd.GetDateTime(4),
                                    Rev = updRd.GetInt64(5)
                                };
                                globalState.ById[firstId] = globalRow;
                                converted++;
                            }
                        }
                        
                        // Delete all remaining non-global duplicates across ALL accounts for this text
                        const string deleteAllDupSql2 = @"DELETE FROM radium.phrase WHERE account_id IS NOT NULL AND [text]=@text";
                        await using var delDupCmd2 = new SqlCommand(deleteAllDupSql2, con);
                        delDupCmd2.Parameters.AddWithValue("@text", text);
                        var removed2 = await delDupCmd2.ExecuteNonQueryAsync().ConfigureAwait(false);
                        duplicatesRemoved += removed2;
                    }
                }
            }
            finally
            {
                globalState.UpdateLock.Release();
                accountState.UpdateLock.Release();
            }

            // Invalidate caches and refresh snapshots OUTSIDE of locks to avoid deadlocks
            _cache.Clear(accountId);
            _cache.Clear(GLOBAL_KEY);
            await RefreshPhrasesAsync(accountId).ConfigureAwait(false);
            await RefreshGlobalPhrasesAsync().ConfigureAwait(false);
            
            return (converted, duplicatesRemoved);
        }

        public Task<long?> GetAnyAccountIdAsync() => Task.FromResult<long?>(null); // not required for Azure migration path
    }
}
