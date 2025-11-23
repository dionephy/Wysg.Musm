// ============================================================================
// DEPRECATED: PostgreSQL Phrase Service Implementation
// ============================================================================
// 
// This file contains the PostgreSQL implementation of IPhraseService.
// 
// STATUS: DEPRECATED (2025-01-29)
// REASON: Production deployments now use Azure SQL (AzureSqlPhraseService.cs)
// KEEP FOR: Reference, on-premise PostgreSQL scenarios, development environments
// 
// FOR NEW CODE: Use AzureSqlPhraseService instead
// 
// ============================================================================

using System.Collections.Generic;
using System.Threading.Tasks;
using Npgsql;
using System;
using System.Diagnostics;
using System.Linq;
using System.Collections.Concurrent;
using System.Threading;
using System.IO;
using System.Net.Sockets;

namespace Wysg.Musm.Radium.Services
{
    /// <summary>
    /// PostgreSQL implementation of IPhraseService - DEPRECATED.
    /// 
    /// This implementation is deprecated in favor of AzureSqlPhraseService for production deployments.
    /// Retained for reference and potential on-premise/development PostgreSQL scenarios.
    /// 
    /// For new code, use AzureSqlPhraseService instead.
    /// </summary>
    [Obsolete("Use AzureSqlPhraseService for production deployments. This PostgreSQL implementation is retained for on-premise scenarios only.", DiagnosticId = "MUSM001")]
    public class PhraseService : IPhraseService
    {
        private readonly IRadiumLocalSettings _settings;
        private readonly ICentralDataSourceProvider _dsProvider;
        private readonly IPhraseCache _cache;
        private readonly string _fallback = "Host=127.0.0.1;Port=5432;Database=wysg_dev;Username=postgres;Password=`123qweas;Timeout=3";

        // Backend detection (single-flight)
        private volatile bool _backendChecked;
        private volatile bool _radiumAvailable;
        private Task? _detectTask;
        private readonly object _detectLock = new();

        private sealed class PhraseRow
        {
            public long Id { get; init; }
            public long? AccountId { get; init; }  // Nullable for global phrases
            public string Text { get; set; } = string.Empty;
            public bool Active { get; set; }
            public DateTime CreatedAt { get; init; }
            public DateTime UpdatedAt { get; set; }
            public long Rev { get; set; }
            
            // SNOMED support (FR-SNOMED-2025-01-19)
            public string? Tags { get; set; }
            public string? TagsSource { get; set; }
            public string? TagsSemanticTag { get; set; }
        }

        private sealed class AccountPhraseState
        {
            public long? AccountId { get; }  // Nullable: null = global phrases
            public long MaxRev { get; set; }
            public long MaxIdLoaded { get; set; }
            public readonly Dictionary<long, PhraseRow> ById = new();
            public DateTime SnapshotLoadedAtUtc { get; set; }
            public volatile bool Loading;
            public bool Preloaded { get; set; }
            // Per-account lock used for all mutation (FR-261): removed global serialization.
            public readonly SemaphoreSlim UpdateLock = new(1, 1);
            public volatile bool UpdatingSnapshot;
            public AccountPhraseState(long? id) { AccountId = id; }
        }

        // Use -1 as key for global phrases state
        private const long GLOBAL_KEY = -1;
        private readonly ConcurrentDictionary<long, AccountPhraseState> _states = new();
        private readonly ConcurrentDictionary<long, SemaphoreSlim> _locks = new();

        public PhraseService(IRadiumLocalSettings settings, ICentralDataSourceProvider dsProvider, IPhraseCache cache)
        {
            _settings = settings;
            _dsProvider = dsProvider;
            _cache = cache;
            Debug.WriteLine("[PhraseService] Using central radium.phrase delta-sync backend with global phrase support (FR-273).");
        }

        private string BuildConnectionString()
        {
            var raw = _settings.CentralConnectionString;
            try
            {
                var b = new NpgsqlConnectionStringBuilder(raw)
                {
                    IncludeErrorDetail = true,
                    Multiplexing = false,
                    KeepAlive = 30,
                    CommandTimeout = 30,
                    NoResetOnClose = false
                };
                if (!b.ContainsKey("Cancellation Timeout")) b["Cancellation Timeout"] = 8000;
                if (b.Timeout < 8) b.Timeout = 8;
                if (b.CommandTimeout < 30) b.CommandTimeout = 30;
                if (b.MaxPoolSize > 50) b.MaxPoolSize = 50;
                return b.ConnectionString;
            }
            catch { return raw; }
        }

        private NpgsqlConnection CreateConnection()
        {
            var b = new NpgsqlConnectionStringBuilder(BuildConnectionString());
            Debug.WriteLine($"[PhraseService] Open DB Host={b.Host} Db={b.Database} User={b.Username} Multiplexing={b.Multiplexing} NoReset={b.NoResetOnClose}");
            return _dsProvider.Central.CreateConnection();
        }

        private Task EnsureBackendAsync()
        {
            if (_backendChecked) return Task.CompletedTask;
            lock (_detectLock)
            {
                if (_backendChecked) return Task.CompletedTask;
                _detectTask ??= DetectBackendAsync();
                return _detectTask;
            }
        }

        private async Task DetectBackendAsync()
        {
            try
            {
                Debug.WriteLine("[PhraseService] Detecting backend (radium.phrase)...");
                await using var con = _dsProvider.CentralMeta.CreateConnection();
                await PgConnectionHelper.OpenWithLocalSslFallbackAsync(con).ConfigureAwait(false);
                const string sql = "SELECT 1 FROM information_schema.tables WHERE table_schema='radium' AND table_name='phrase'";
                using var cmd = new NpgsqlCommand(sql, con) { CommandTimeout = 8 };
                _radiumAvailable = (await cmd.ExecuteScalarAsync().ConfigureAwait(false)) != null;
                Debug.WriteLine($"[PhraseService] Detection complete radiumAvailable={_radiumAvailable}");
            }
            catch (Exception ex)
            {
                _radiumAvailable = false;
                Debug.WriteLine($"[PhraseService] Detection error {ex.GetType().Name}: {ex.Message}");
            }
            finally { _backendChecked = true; }
        }

        private static bool IsTransientTimeout(Exception ex)
        {
            if (ex is TimeoutException) return true;
            if (ex is NpgsqlException npg)
            {
                if (npg.InnerException is TimeoutException) return true;
                if (ContainsSocketTimeout(npg)) return true;
                if (npg.Message.IndexOf("Timeout during reading attempt", StringComparison.OrdinalIgnoreCase) >= 0) return true;
                if (npg.Message.IndexOf("Exception while reading from stream", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            }
            if (ContainsSocketTimeout(ex)) return true;
            if (ex is IOException io && io.InnerException is TimeoutException) return true;
            return false;
            static bool ContainsSocketTimeout(Exception e)
            {
                for (var cur = e; cur != null; cur = cur.InnerException)
                    if (cur is SocketException se && se.SocketErrorCode == SocketError.TimedOut) return true;
                return false;
            }
        }

        // Basic transient checker for network/DB hiccups (kept conservative; deprecated service)
        private static bool IsTransient(Exception ex)
        {
            // Treat common network and database transport failures as transient
            if (ex is TimeoutException) return true;
            if (ex is IOException) return true;
            if (ex is SocketException) return true;
            if (ex is NpgsqlException) return true;
            if (ex is PostgresException pex)
            {
                // Serialization failure or deadlock
                if (string.Equals(pex.SqlState, "40001", StringComparison.Ordinal)) return true; // serialization_failure
                if (string.Equals(pex.SqlState, "40P01", StringComparison.Ordinal)) return true; // deadlock_detected
            }
            return false;
        }

        private SemaphoreSlim GetLock(long key) => _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

        private async Task EnsureUpToDateAsync(long accountId)
        {
            await EnsureBackendAsync().ConfigureAwait(false);
            if (!_radiumAvailable) return;
            var lck = GetLock(accountId);
            await lck.WaitAsync().ConfigureAwait(false);
            try
            {
                var state = _states.GetOrAdd(accountId, id => new AccountPhraseState(id));
                if (state.Preloaded) return;
                if (state.ById.Count == 0 && !state.Loading)
                {
                    state.Loading = true;
                    _ = Task.Run(() => EagerLoadAllAsync(state));
                }
            }
            finally { lck.Release(); }
        }

        // Public explicit preload: fetch entire snapshot once (used at app load) then disable auto background paging.
        public async Task PreloadAsync(long accountId)
        {
            await EnsureBackendAsync().ConfigureAwait(false);
            if (!_radiumAvailable) return;
            var lck = GetLock(accountId);
            await lck.WaitAsync().ConfigureAwait(false);
            try
            {
                var state = _states.GetOrAdd(accountId, id => new AccountPhraseState(id));
                if (state.Preloaded || state.Loading) return;
                state.Loading = true;
                try
                {
                    await EagerLoadAllAsync(state).ConfigureAwait(false);
                    state.Preloaded = true;
                }
                finally { state.Loading = false; }
            }
            finally { if (lck.CurrentCount == 0) lck.Release(); }
        }

        private async Task EagerLoadAllAsync(AccountPhraseState state)
        {
            try
            {
                Debug.WriteLine($"[PhraseService][PreloadFull] BEGIN account={state.AccountId}");
                // Intentionally minimal (on-demand loads). No full scan for free tier stability.
                state.SnapshotLoadedAtUtc = DateTime.UtcNow;
                Debug.WriteLine($"[PhraseService][PreloadFull] END account={state.AccountId} rows=0");
            }
            catch (Exception ex) { Debug.WriteLine($"[PhraseService] Preload error account={state.AccountId}: {ex.Message}"); }
        }

        // Reused single-connection page loader (no per-page open / SET LOCAL)
        private static async Task<List<PhraseRow>> LoadPageOnConnectionAsync(NpgsqlConnection con, long accountId, long afterId, int take)
        {
            var list = new List<PhraseRow>(take);
            const string sql = @"SELECT id, account_id, text, active, created_at, updated_at, rev, tags, tags_source, tags_semantic_tag
                                   FROM radium.phrase
                                   WHERE account_id=@aid AND id > @after
                                   ORDER BY id
                                   LIMIT @lim";
            await using var cmd = new NpgsqlCommand(sql, con) { CommandTimeout = 12 };
            cmd.Parameters.AddWithValue("aid", accountId);
            cmd.Parameters.AddWithValue("after", afterId);
            cmd.Parameters.AddWithValue("lim", take);
            await using var rd = await cmd.ExecuteReaderAsync(System.Data.CommandBehavior.SequentialAccess).ConfigureAwait(false);
            while (await rd.ReadAsync().ConfigureAwait(false))
            {
                list.Add(new PhraseRow
                {
                    Id = rd.GetInt64(0),
                    AccountId = rd.GetInt64(1),
                    Text = rd.GetString(2),
                    Active = rd.GetBoolean(3),
                    CreatedAt = rd.GetDateTime(4),
                    UpdatedAt = rd.GetDateTime(5),
                    Rev = rd.GetInt64(6),
                    Tags = rd.IsDBNull(7) ? null : rd.GetString(7),
                    TagsSource = rd.IsDBNull(8) ? null : rd.GetString(8),
                    TagsSemanticTag = rd.IsDBNull(9) ? null : rd.GetString(9)
                });
            }
            return list;
        }

        private async Task<List<PhraseRow>> LoadPageAsync(long accountId, long afterId, int take)
        {
            var list = new List<PhraseRow>(take);
            const string sql = @"SELECT id, account_id, text, active, created_at, updated_at, rev, tags, tags_source, tags_semantic_tag
                                   FROM radium.phrase
                                   WHERE account_id=@aid AND id > @after
                                   ORDER BY id
                                   LIMIT @lim";
            await using var con = CreateConnection();
            await PgConnectionHelper.OpenWithLocalSslFallbackAsync(con).ConfigureAwait(false);
            await using var cmd = new NpgsqlCommand(sql, con) { CommandTimeout = 12 };
            cmd.Parameters.AddWithValue("aid", accountId);
            cmd.Parameters.AddWithValue("after", afterId);
            cmd.Parameters.AddWithValue("lim", take);
            await using var rd = await cmd.ExecuteReaderAsync(System.Data.CommandBehavior.SequentialAccess).ConfigureAwait(false);
            while (await rd.ReadAsync().ConfigureAwait(false))
            {
                list.Add(new PhraseRow
                {
                    Id = rd.GetInt64(0),
                    AccountId = rd.GetInt64(1),
                    Text = rd.GetString(2),
                    Active = rd.GetBoolean(3),
                    CreatedAt = rd.GetDateTime(4),
                    UpdatedAt = rd.GetDateTime(5),
                    Rev = rd.GetInt64(6),
                    Tags = rd.IsDBNull(7) ? null : rd.GetString(7),
                    TagsSource = rd.IsDBNull(8) ? null : rd.GetString(8),
                    TagsSemanticTag = rd.IsDBNull(9) ? null : rd.GetString(9)
                });
            }
            return list;
        }

        public async Task<IReadOnlyList<string>> GetPhrasesForAccountAsync(long accountId)
        {
            if (accountId <= 0) return Array.Empty<string>();
            if (!_states.TryGetValue(accountId, out var state))
            {
                state = _states.GetOrAdd(accountId, id => new AccountPhraseState(id));
                try { await LoadSmallSetAsync(state).ConfigureAwait(false); } catch { return Array.Empty<string>(); }
            }
            return state.ById.Values.Where(r => r.Active).Select(r => r.Text).OrderBy(t => t).Take(500).ToList();
        }

        public async Task<IReadOnlyList<string>> GetPhrasesByPrefixAccountAsync(long accountId, string prefix, int limit = 15)
        {
            if (accountId <= 0 || string.IsNullOrWhiteSpace(prefix)) return Array.Empty<string>();
            if (!_states.TryGetValue(accountId, out var state))
            {
                state = _states.GetOrAdd(accountId, id => new AccountPhraseState(id));
                try { await LoadSmallSetAsync(state).ConfigureAwait(false); } catch { return Array.Empty<string>(); }
            }
            return state.ById.Values.Where(r => r.Active && r.Text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .OrderBy(r => r.Text.Length).ThenBy(r => r.Text)
                .Take(limit).Select(r => r.Text).ToList();
        }

        private async Task LoadSmallSetAsync(AccountPhraseState state)
        {
            Debug.WriteLine($"[PhraseService][LoadSmall] Loading phrases for account={state.AccountId}");
            await using var con = CreateConnection();
            await PgConnectionHelper.OpenWithLocalSslFallbackAsync(con).ConfigureAwait(false);
            const string sql = @"SELECT id, account_id, text, active, created_at, updated_at, rev, tags, tags_source, tags_semantic_tag
                                   FROM radium.phrase
                                   WHERE account_id = @aid
                                   ORDER BY updated_at DESC
                                   LIMIT 100";
            await using var cmd = new NpgsqlCommand(sql, con) { CommandTimeout = 12 };
            cmd.Parameters.AddWithValue("aid", state.AccountId);
            await using var rd = await cmd.ExecuteReaderAsync(System.Data.CommandBehavior.SequentialAccess).ConfigureAwait(false);
            state.ById.Clear();
            state.MaxRev = 0;
            int count = 0;
            while (await rd.ReadAsync().ConfigureAwait(false))
            {
                var row = new PhraseRow
                {
                    Id = rd.GetInt64(0),
                    AccountId = rd.GetInt64(1),
                    Text = rd.GetString(2),
                    Active = rd.GetBoolean(3),
                    CreatedAt = rd.GetDateTime(4),
                    UpdatedAt = rd.GetDateTime(5),
                    Rev = rd.GetInt64(6),
                    Tags = rd.IsDBNull(7) ? null : rd.GetString(7),
                    TagsSource = rd.IsDBNull(8) ? null : rd.GetString(8),
                    TagsSemanticTag = rd.IsDBNull(9) ? null : rd.GetString(9)
                };
                state.ById[row.Id] = row;
                if (row.Rev > state.MaxRev) state.MaxRev = row.Rev;
                count++;
            }
            state.SnapshotLoadedAtUtc = DateTime.UtcNow;
            Debug.WriteLine($"[PhraseService][LoadSmall] Loaded {count} phrases for account={state.AccountId}");
        }

        public Task<IReadOnlyList<string>> GetPhrasesForTenantAsync(long tenantId) => GetPhrasesForAccountAsync(tenantId);
        public Task<IReadOnlyList<string>> GetPhrasesByPrefixAsync(long tenantId, string prefix, int limit = 15) => GetPhrasesByPrefixAccountAsync(tenantId, prefix, limit);

        public async Task<IReadOnlyList<PhraseInfo>> GetAllPhraseMetaAsync(long accountId)
        {
            if (accountId <= 0) return Array.Empty<PhraseInfo>();
            if (!_states.TryGetValue(accountId, out var state) || state.ById.Count == 0)
            {
                state = _states.GetOrAdd(accountId, id => new AccountPhraseState(id));
                try { await LoadSmallSetAsync(state).ConfigureAwait(false); } catch { return Array.Empty<PhraseInfo>(); }
            }
            // Return ALL account phrases (removed Take(1000) limit for comprehensive access)
            return state.ById.Values.OrderByDescending(r => r.UpdatedAt)
                .Select(r => new PhraseInfo(r.Id, r.AccountId, r.Text, r.Active, r.UpdatedAt, r.Rev, r.Tags, r.TagsSource, r.TagsSemanticTag)).ToList();
        }

        // NEW: Non-global phrase metadata across all accounts (FR-280)
        public async Task<IReadOnlyList<PhraseInfo>> GetAllNonGlobalPhraseMetaAsync(int take = 500)
        {
            await EnsureBackendAsync().ConfigureAwait(false);
            if (!_radiumAvailable) return Array.Empty<PhraseInfo>();
            const string sql = @"SELECT id, account_id, text, active, created_at, updated_at, rev, tags, tags_source, tags_semantic_tag
                                   FROM radium.phrase
                                   WHERE account_id IS NOT NULL
                                   ORDER BY updated_at DESC
                                   LIMIT @lim";
            var list = new List<PhraseInfo>(take);
            await using var con = CreateConnection();
            await PgConnectionHelper.OpenWithLocalSslFallbackAsync(con).ConfigureAwait(false);
            await using var cmd = new NpgsqlCommand(sql, con) { CommandTimeout = 12 };
            cmd.Parameters.AddWithValue("lim", take);
            await using var rd = await cmd.ExecuteReaderAsync(System.Data.CommandBehavior.SequentialAccess).ConfigureAwait(false);
            while (await rd.ReadAsync().ConfigureAwait(false))
            {
                list.Add(new PhraseInfo(
                    rd.GetInt64(0),
                    rd.GetInt64(1),
                    rd.GetString(2),
                    rd.GetBoolean(3),
                    rd.GetDateTime(5),
                    rd.GetInt64(6),
                    rd.IsDBNull(7) ? null : rd.GetString(7),
                    rd.IsDBNull(8) ? null : rd.GetString(8),
                    rd.IsDBNull(9) ? null : rd.GetString(9)));
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
                    state.UpdatingSnapshot = true;
                    state.ById.Clear();
                    state.MaxRev = 0;
                    state.MaxIdLoaded = 0;
                    try { await LoadSmallSetAsync(state).ConfigureAwait(false); }
                    finally { state.UpdatingSnapshot = false; }
                }
                finally { state.UpdateLock.Release(); }
            }
        }

        // Global phrases support
        public async Task<IReadOnlyList<string>> GetGlobalPhrasesAsync()
        {
            await EnsureBackendAsync().ConfigureAwait(false);
            if (!_radiumAvailable) return Array.Empty<string>();
            
            var state = _states.GetOrAdd(GLOBAL_KEY, _ => new AccountPhraseState(null));
            if (state.ById.Count == 0 && !state.Loading)
            {
                try { await LoadGlobalPhrasesAsync(state).ConfigureAwait(false); } 
                catch { return Array.Empty<string>(); }
            }
            // Filter global phrases to 4 words or less for completion (FR-completion-filter-2025-01-29 - increased from 3 to 4)
            var allActive = state.ById.Values.Where(r => r.Active).ToList();
            Debug.WriteLine($"[PhraseService][GetGlobalPhrasesAsync] Total active global phrases: {allActive.Count}");
            
            var filtered = allActive.Where(r => CountWords(r.Text) <= 4).ToList();
            Debug.WriteLine($"[PhraseService][GetGlobalPhrasesAsync] After 4-word filter: {filtered.Count}");
            
            if (allActive.Count > 0 && allActive.Count - filtered.Count > 0)
            {
                Debug.WriteLine($"[PhraseService][GetGlobalPhrasesAsync] Filtered out {allActive.Count - filtered.Count} long phrases");
                // Show first 3 examples of filtered phrases
                var examples = allActive.Where(r => CountWords(r.Text) > 4).Take(3).ToList();
                foreach (var ex in examples)
                {
                    Debug.WriteLine($"  FILTERED: \"{ex.Text}\" ({CountWords(ex.Text)} words)");
                }
            }
            
            return filtered.Select(r => r.Text).OrderBy(t => t).ToList();
        }

        public async Task<IReadOnlyList<string>> GetGlobalPhrasesByPrefixAsync(string prefix, int limit = 15)
        {
            if (string.IsNullOrWhiteSpace(prefix)) return Array.Empty<string>();
            await EnsureBackendAsync().ConfigureAwait(false);
            if (!_radiumAvailable) return Array.Empty<string>();
            
            var state = _states.GetOrAdd(GLOBAL_KEY, _ => new AccountPhraseState(null));
            if (state.ById.Count == 0 && !state.Loading)
            {
                try { await LoadGlobalPhrasesAsync(state).ConfigureAwait(false); } 
                catch { return Array.Empty<string>(); }
            }
            
            Debug.WriteLine($"[PhraseService][GetGlobalPhrasesByPrefixAsync] prefix='{prefix}', limit={limit}");
            
            var matching = state.ById.Values.Where(r => r.Active && r.Text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList();
            Debug.WriteLine($"[PhraseService][GetGlobalPhrasesByPrefixAsync] Found {matching.Count} matches for prefix '{prefix}'");
            
            // Filter global phrases to 4 words or less for completion window (FR-completion-filter-2025-01-29 - increased from 3 to 4)
            var filtered = matching.Where(r => CountWords(r.Text) <= 4).ToList();
            Debug.WriteLine($"[PhraseService][GetGlobalPhrasesByPrefixAsync] After 4-word filter: {filtered.Count}");
            
            if (matching.Count > filtered.Count)
            {
                Debug.WriteLine($"[PhraseService][GetGlobalPhrasesByPrefixAsync] Filtered out {matching.Count - filtered.Count} long phrases");
                var examples = matching.Where(r => CountWords(r.Text) > 4).Take(3).ToList();
                foreach (var ex in examples)
                {
                    Debug.WriteLine($"  FILTERED: \"{ex.Text}\" ({CountWords(ex.Text)} words)");
                }
            }
            
            return filtered.OrderBy(r => r.Text.Length).ThenBy(r => r.Text)
                .Take(limit).Select(r => r.Text).ToList();
        }

        // Combined phrases (global + account-specific)
        public async Task<IReadOnlyList<string>> GetCombinedPhrasesAsync(long accountId)
        {
            var globalPhrases = await GetGlobalPhrasesAsync().ConfigureAwait(false);
            var accountPhrases = await GetPhrasesForAccountAsync(accountId).ConfigureAwait(false);
            
            Debug.WriteLine($"[PhraseService][GetCombinedPhrasesAsync] accountId={accountId}, global={globalPhrases.Count}, account={accountPhrases.Count}");
            
            var combined = new HashSet<string>(accountPhrases, StringComparer.OrdinalIgnoreCase);
            foreach (var global in globalPhrases)
                combined.Add(global);
            
            Debug.WriteLine($"[PhraseService][GetCombinedPhrasesAsync] Combined total: {combined.Count}");
            
            return combined.OrderBy(t => t).ToList();
        }

        public async Task<IReadOnlyList<string>> GetCombinedPhrasesByPrefixAsync(long accountId, string prefix, int limit = 15)
        {
            if (string.IsNullOrWhiteSpace(prefix)) return Array.Empty<string>();
            
            Debug.WriteLine($"[PhraseService][GetCombinedPhrasesByPrefixAsync] accountId={accountId}, prefix='{prefix}', limit={limit}");
            
            // Global phrases are already filtered to 4 words or less in GetGlobalPhrasesByPrefixAsync
            var globalPhrases = await GetGlobalPhrasesByPrefixAsync(prefix, limit).ConfigureAwait(false);
            // Account-specific phrases are NOT filtered (no word limit)
            var accountPhrases = await GetPhrasesByPrefixAccountAsync(accountId, prefix, limit).ConfigureAwait(false);
            
            Debug.WriteLine($"[PhraseService][GetCombinedPhrasesByPrefixAsync] global={globalPhrases.Count}, account={accountPhrases.Count}");
            
            var combined = new HashSet<string>(accountPhrases, StringComparer.OrdinalIgnoreCase);
            foreach (var global in globalPhrases)
                combined.Add(global);
            
            Debug.WriteLine($"[PhraseService][GetCombinedPhrasesByPrefixAsync] Combined total: {combined.Count}");
            
            return combined.OrderBy(t => t.Length).ThenBy(t => t).Take(limit).ToList();
        }

        // Unfiltered combined phrases for syntax highlighting (includes ALL phrases regardless of word count)
        public async Task<IReadOnlyList<string>> GetAllPhrasesForHighlightingAsync(long accountId)
        {
            await EnsureBackendAsync().ConfigureAwait(false);
            if (!_radiumAvailable) return Array.Empty<string>();
            
            // Get ALL global phrases from _states (unfiltered)
            var globalState = _states.GetOrAdd(GLOBAL_KEY, _ => new AccountPhraseState(null));
            if (globalState.ById.Count == 0 && !globalState.Loading)
            {
                try { await LoadGlobalPhrasesAsync(globalState).ConfigureAwait(false); } 
                catch { return Array.Empty<string>(); }
            }
            
            // Get ALL account phrases
            var accountState = _states.GetOrAdd(accountId, id => new AccountPhraseState(id));
            if (accountState.ById.Count == 0 && !accountState.Loading)
            {
                try { await LoadSmallSetAsync(accountState).ConfigureAwait(false); } 
                catch { return Array.Empty<string>(); }
            }
            
            // Combine WITHOUT filtering - syntax highlighting needs all phrases
            var globalPhrases = globalState.ById.Values.Where(r => r.Active).Select(r => r.Text);
            var accountPhrases = accountState.ById.Values.Where(r => r.Active).Select(r => r.Text);
            
            var combined = new HashSet<string>(accountPhrases, StringComparer.OrdinalIgnoreCase);
            foreach (var global in globalPhrases)
                combined.Add(global);
            
            Debug.WriteLine($"[PhraseService][GetAllPhrasesForHighlightingAsync] accountId={accountId}, total={combined.Count} (unfiltered for highlighting)");
            
            return combined.OrderBy(t => t).ToList();
        }

        private async Task LoadGlobalPhrasesAsync(AccountPhraseState state)
        {
            Debug.WriteLine("[PhraseService][LoadGlobal] Loading global phrases");
            await using var con = CreateConnection();
            await PgConnectionHelper.OpenWithLocalSslFallbackAsync(con).ConfigureAwait(false);
            // Load ALL global phrases (no limit) for accurate existence checks
            const string sql = @"SELECT id, account_id, text, active, created_at, updated_at, rev, tags, tags_source, tags_semantic_tag
                                   FROM radium.phrase
                                   WHERE account_id IS NULL
                                   ORDER BY updated_at DESC";
            await using var cmd = new NpgsqlCommand(sql, con) { CommandTimeout = 12 };
            await using var rd = await cmd.ExecuteReaderAsync(System.Data.CommandBehavior.SequentialAccess).ConfigureAwait(false);
            state.ById.Clear();
            state.MaxRev = 0;
            int count = 0;
            while (await rd.ReadAsync().ConfigureAwait(false))
            {
                var row = new PhraseRow
                {
                    Id = rd.GetInt64(0),
                    AccountId = rd.IsDBNull(1) ? null : rd.GetInt64(1),
                    Text = rd.GetString(2),
                    Active = rd.GetBoolean(3),
                    CreatedAt = rd.GetDateTime(4),
                    UpdatedAt = rd.GetDateTime(5),
                    Rev = rd.GetInt64(6),
                    Tags = rd.IsDBNull(7) ? null : rd.GetString(7),
                    TagsSource = rd.IsDBNull(8) ? null : rd.GetString(8),
                    TagsSemanticTag = rd.IsDBNull(9) ? null : rd.GetString(9)
                };
                state.ById[row.Id] = row;
                if (row.Rev > state.MaxRev) state.MaxRev = row.Rev;
                count++;
            }
            state.SnapshotLoadedAtUtc = DateTime.UtcNow;
            Debug.WriteLine($"[PhraseService][LoadGlobal] Loaded {count} global phrases");
        }

        public async Task<IReadOnlyList<PhraseInfo>> GetAllGlobalPhraseMetaAsync()
        {
            await EnsureBackendAsync().ConfigureAwait(false);
            if (!_radiumAvailable) return Array.Empty<PhraseInfo>();
            
            var state = _states.GetOrAdd(GLOBAL_KEY, _ => new AccountPhraseState(null));
            if (state.ById.Count == 0)
            {
                try { await LoadGlobalPhrasesAsync(state).ConfigureAwait(false); } 
                catch { return Array.Empty<PhraseInfo>(); }
            }
            // Return ALL global phrases (removed Take(1000) limit for accurate SNOMED browser existence checks)
            return state.ById.Values.OrderByDescending(r => r.UpdatedAt)
                .Select(r => new PhraseInfo(r.Id, r.AccountId, r.Text, r.Active, r.UpdatedAt, r.Rev, r.Tags, r.TagsSource, r.TagsSemanticTag)).ToList();
        }

        public async Task RefreshGlobalPhrasesAsync()
        {
            if (_states.TryGetValue(GLOBAL_KEY, out var state))
            {
                await state.UpdateLock.WaitAsync().ConfigureAwait(false);
                try
                {
                    state.UpdatingSnapshot = true;
                    state.ById.Clear();
                    state.MaxRev = 0;
                    state.MaxIdLoaded = 0;
                    try { await LoadGlobalPhrasesAsync(state).ConfigureAwait(false); }
                    finally { state.UpdatingSnapshot = false; }
                }
                finally { state.UpdateLock.Release(); }
            }
            // Invalidate all completion caches because global phrases affect every account's combined list
            _cache.ClearAll();
        }

        public async Task<PhraseInfo> UpsertPhraseAsync(long? accountId, string text, bool active = true)
        {
            if (accountId.HasValue && accountId.Value <= 0) 
                throw new ArgumentOutOfRangeException(nameof(accountId));
            await EnsureBackendAsync().ConfigureAwait(false);
            if (!_radiumAvailable) throw new InvalidOperationException("radium.phrase not available");
            
            var key = accountId ?? GLOBAL_KEY;
            var state = _states.GetOrAdd(key, _ => new AccountPhraseState(accountId));
            await state.UpdateLock.WaitAsync().ConfigureAwait(false);
            try
            {
                state.UpdatingSnapshot = true;
                var result = await UpsertPhraseInternalAsync(accountId, text, active).ConfigureAwait(false);

                // Failsafe: if we upserted a GLOBAL phrase, delete all non-global duplicates across ALL accounts (FR-281)
                if (!accountId.HasValue)
                {
                    const string delSql = @"DELETE FROM radium.phrase WHERE account_id IS NOT NULL AND text=@text";
                    await using var con = CreateConnection();
                    await PgConnectionHelper.OpenWithLocalSslFallbackAsync(con).ConfigureAwait(false);
                    await using var del = new NpgsqlCommand(delSql, con) { CommandTimeout = 12 };
                    del.Parameters.AddWithValue("text", text);
                    _ = await del.ExecuteNonQueryAsync().ConfigureAwait(false);
                }

                UpdateSnapshotAfterUpsert(state, result);

                if (!accountId.HasValue)
                {
                    // Global phrases changed: clear ALL caches so combined completion repopulates including globals
                    _cache.ClearAll();
                }
                else
                {
                    _cache.Clear(accountId ?? GLOBAL_KEY);
                }
                return result;
            }
            finally
            {
                state.UpdatingSnapshot = false;
                state.UpdateLock.Release();
            }
        }

        public async Task<PhraseInfo?> ToggleActiveAsync(long? accountId, long phraseId)
        {
            if (accountId.HasValue && accountId.Value <= 0) return null;
            await EnsureBackendAsync().ConfigureAwait(false);
            if (!_radiumAvailable) return null;
            
            var key = accountId ?? GLOBAL_KEY;
            var state = _states.GetOrAdd(key, _ => new AccountPhraseState(accountId));
            await state.UpdateLock.WaitAsync().ConfigureAwait(false);
            try
            {
                state.UpdatingSnapshot = true;
                var info = await ToggleActiveInternalAsync(accountId, phraseId).ConfigureAwait(false);
                if (info != null)
                {
                    UpdateSnapshotAfterToggle(state, info);
                    if (!accountId.HasValue)
                        _cache.ClearAll();
                    else
                        _cache.Clear(accountId ?? GLOBAL_KEY);
                }
                return info;
            }
            finally
            {
                state.UpdatingSnapshot = false;
                state.UpdateLock.Release();
            }
        }

        public async Task<PhraseInfo?> UpdatePhraseTextAsync(long? accountId, long phraseId, string newText)
        {
            if (accountId.HasValue && accountId.Value <= 0) return null;
            if (string.IsNullOrWhiteSpace(newText)) return null;
            
            await EnsureBackendAsync().ConfigureAwait(false);
            if (!_radiumAvailable) return null;
            
            var key = accountId ?? GLOBAL_KEY;
            var state = _states.GetOrAdd(key, _ => new AccountPhraseState(accountId));
            await state.UpdateLock.WaitAsync().ConfigureAwait(false);
            try
            {
                state.UpdatingSnapshot = true;
                var info = await UpdatePhraseTextInternalAsync(accountId, phraseId, newText).ConfigureAwait(false);
                if (info != null)
                {
                    UpdateSnapshotAfterToggle(state, info);
                    if (!accountId.HasValue)
                        _cache.ClearAll();
                    else
                        _cache.Clear(accountId ?? GLOBAL_KEY);
                }
                return info;
            }
            finally
            {
                state.UpdatingSnapshot = false;
                state.UpdateLock.Release();
            }
        }

        private async Task<PhraseInfo> UpsertPhraseInternalAsync(long? accountId, string text, bool active)
        {
            int attempts = 0;
            const int maxAttempts = 3;
            while (attempts < maxAttempts)
            {
                attempts++;
                try
                {
                    await using var con = CreateConnection();
                    await PgConnectionHelper.OpenWithLocalSslFallbackAsync(con).ConfigureAwait(false);
                    
                    string selectSql = accountId.HasValue
                        ? @"SELECT id, account_id, text, active, created_at, updated_at, rev, tags, tags_source, tags_semantic_tag
                            FROM radium.phrase
                            WHERE account_id=@aid AND text=@text"
                        : @"SELECT id, account_id, text, active, created_at, updated_at, rev, tags, tags_source, tags_semantic_tag
                            FROM radium.phrase
                            WHERE account_id IS NULL AND text=@text";
                            
                    PhraseInfo? existing = null;
                    await using (var selCmd = new NpgsqlCommand(selectSql, con) { CommandTimeout = 12 })
                    {
                        if (accountId.HasValue)
                            selCmd.Parameters.AddWithValue("aid", accountId.Value);
                        selCmd.Parameters.AddWithValue("text", text);
                        await using var rd = await selCmd.ExecuteReaderAsync().ConfigureAwait(false);
                        if (await rd.ReadAsync().ConfigureAwait(false))
                        {
                            existing = new PhraseInfo(
                                rd.GetInt64(0), 
                                rd.IsDBNull(1) ? null : rd.GetInt64(1), 
                                rd.GetString(2), 
                                rd.GetBoolean(3), 
                                rd.GetDateTime(5), 
                                rd.GetInt64(6),
                                rd.IsDBNull(7) ? null : rd.GetString(7),
                                rd.IsDBNull(8) ? null : rd.GetString(8),
                                rd.IsDBNull(9) ? null : rd.GetString(9));
                        }
                    }
                    
                    if (existing != null && existing.Active == active) return existing; // no-op
                    
                    PhraseInfo result;
                    if (existing == null)
                    {
                        const string insertSql = @"INSERT INTO radium.phrase(account_id,text,active) VALUES(@aid,@text,@active)
                                                  RETURNING id, account_id, text, active, created_at, updated_at, rev, tags, tags_source, tags_semantic_tag";
                        await using var ins = new NpgsqlCommand(insertSql, con) { CommandTimeout = 12 };
                        if (accountId.HasValue)
                            ins.Parameters.AddWithValue("aid", accountId.Value);
                        else
                            ins.Parameters.AddWithValue("aid", DBNull.Value);
                        ins.Parameters.AddWithValue("text", text);
                        ins.Parameters.AddWithValue("active", active);
                        await using var rd = await ins.ExecuteReaderAsync().ConfigureAwait(false);
                        if (!await rd.ReadAsync().ConfigureAwait(false)) throw new InvalidOperationException("Insert failed");
                        result = new PhraseInfo(
                            rd.GetInt64(0), 
                            rd.IsDBNull(1) ? null : rd.GetInt64(1), 
                            rd.GetString(2), 
                            rd.GetBoolean(3), 
                            rd.GetDateTime(5), 
                            rd.GetInt64(6),
                            rd.IsDBNull(7) ? null : rd.GetString(7),
                            rd.IsDBNull(8) ? null : rd.GetString(8),
                            rd.IsDBNull(9) ? null : rd.GetString(9));
                    }
                    else
                    {
                        string updateSql = accountId.HasValue
                            ? @"UPDATE radium.phrase SET active=@active
                                WHERE account_id=@aid AND text=@text
                                RETURNING id, account_id, text, active, created_at, updated_at, rev, tags, tags_source, tags_semantic_tag"
                            : @"UPDATE radium.phrase SET active=@active
                                WHERE account_id IS NULL AND text=@text
                                RETURNING id, account_id, text, active, created_at, updated_at, rev, tags, tags_source, tags_semantic_tag";
                        await using var upd = new NpgsqlCommand(updateSql, con) { CommandTimeout = 12 };
                        if (accountId.HasValue)
                            upd.Parameters.AddWithValue("aid", accountId.Value);
                        upd.Parameters.AddWithValue("text", text);
                        upd.Parameters.AddWithValue("active", active);
                        await using var rd = await upd.ExecuteReaderAsync().ConfigureAwait(false);
                        if (!await rd.ReadAsync().ConfigureAwait(false)) throw new InvalidOperationException("Update failed");
                        result = new PhraseInfo(
                            rd.GetInt64(0), 
                            rd.IsDBNull(1) ? null : rd.GetInt64(1), 
                            rd.GetString(2), 
                            rd.GetBoolean(3), 
                            rd.GetDateTime(5), 
                            rd.GetInt64(6),
                            rd.IsDBNull(7) ? null : rd.GetString(7),
                            rd.IsDBNull(8) ? null : rd.GetString(8),
                            rd.IsDBNull(9) ? null : rd.GetString(9));
                    }
                    return result;
                }
                catch (Exception ex) when ((IsTransient(ex) || IsTransientTimeout(ex)) && attempts < maxAttempts)
                {
                    Debug.WriteLine($"[PhraseService][UpsertInternal][Transient] attempt={attempts} {ex.GetType().Name}: {ex.Message}");
                    await Task.Delay(150 * attempts).ConfigureAwait(false);
                }
            }
            throw new InvalidOperationException("Upsert failed after retries");
        }

        private async Task<PhraseInfo?> ToggleActiveInternalAsync(long? accountId, long phraseId)
        {
            string sql = accountId.HasValue
                ? @"UPDATE radium.phrase SET active = NOT active
                    WHERE account_id=@aid AND id=@pid
                    RETURNING id, account_id, text, active, created_at, updated_at, rev, tags, tags_source, tags_semantic_tag"
                : @"UPDATE radium.phrase SET active = NOT active
                    WHERE account_id IS NULL AND id=@pid
                    RETURNING id, account_id, text, active, created_at, updatedAt, rev, tags, tags_source, tags_semantic_tag";
                    
            int attempts = 0;
            const int maxAttempts = 3;
            while (attempts < maxAttempts)
            {
                attempts++;
                try
                {
                    await using var con = CreateConnection();
                    await PgConnectionHelper.OpenWithLocalSslFallbackAsync(con).ConfigureAwait(false);
                    await using var cmd = new NpgsqlCommand(sql, con) { CommandTimeout = 12 };
                    if (accountId.HasValue)
                        cmd.Parameters.AddWithValue("aid", accountId.Value);
                    cmd.Parameters.AddWithValue("pid", phraseId);
                    await using var rd = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
                    if (await rd.ReadAsync().ConfigureAwait(false))
                    {
                        return new PhraseInfo(
                            rd.GetInt64(0), 
                            rd.IsDBNull(1) ? null : rd.GetInt64(1), 
                            rd.GetString(2), 
                            rd.GetBoolean(3), 
                            rd.GetDateTime(5), 
                            rd.GetInt64(6),
                            rd.IsDBNull(7) ? null : rd.GetString(7),
                            rd.IsDBNull(8) ? null : rd.GetString(8),
                            rd.IsDBNull(9) ? null : rd.GetString(9));
                    }
                    return null;
                }
                catch (Exception ex) when ((IsTransient(ex) || IsTransientTimeout(ex)) && attempts < maxAttempts)
                {
                    Debug.WriteLine($"[PhraseService][ToggleInternal][Transient] attempt={attempts} {ex.GetType().Name}: {ex.Message}");
                    await Task.Delay(150 * attempts).ConfigureAwait(false);
                }
            }
            return null;
        }

        private async Task<PhraseInfo?> UpdatePhraseTextInternalAsync(long? accountId, long phraseId, string newText)
        {
            string sql = accountId.HasValue
                ? @"UPDATE radium.phrase SET text=@text
                    WHERE account_id=@aid AND id=@pid
                    RETURNING id, account_id, text, active, created_at, updatedAt, rev, tags, tags_source, tags_semantic_tag"
                : @"UPDATE radium.phrase SET text=@text
                    WHERE account_id IS NULL AND id=@pid
                    RETURNING id, account_id, text, active, created_at, updatedAt, rev, tags, tags_source, tags_semantic_tag";
                    
            int attempts = 0;
            const int maxAttempts = 3;
            while (attempts < maxAttempts)
            {
                attempts++;
                try
                {
                    await using var con = CreateConnection();
                    await PgConnectionHelper.OpenWithLocalSslFallbackAsync(con).ConfigureAwait(false);
                    await using var cmd = new NpgsqlCommand(sql, con) { CommandTimeout = 12 };
                    if (accountId.HasValue)
                        cmd.Parameters.AddWithValue("aid", accountId.Value);
                    cmd.Parameters.AddWithValue("pid", phraseId);
                    cmd.Parameters.AddWithValue("text", newText.Trim());
                    await using var rd = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
                    if (await rd.ReadAsync().ConfigureAwait(false))
                    {
                        return new PhraseInfo(
                            rd.GetInt64(0), 
                            rd.IsDBNull(1) ? null : rd.GetInt64(1), 
                            rd.GetString(2), 
                            rd.GetBoolean(3), 
                            rd.GetDateTime(5), 
                            rd.GetInt64(6),
                            rd.IsDBNull(7) ? null : rd.GetString(7),
                            rd.IsDBNull(8) ? null : rd.GetString(8),
                            rd.IsDBNull(9) ? null : rd.GetString(9));
                    }
                    return null;
                }
                catch (Exception ex) when ((IsTransient(ex) || IsTransientTimeout(ex)) && attempts < maxAttempts)
                {
                    Debug.WriteLine($"[PhraseService][UpdateTextInternal][Transient] attempt={attempts} {ex.GetType().Name}: {ex.Message}");
                    await Task.Delay(150 * attempts).ConfigureAwait(false);
                }
            }
            return null;
        }

        private void UpdateSnapshotAfterUpsert(AccountPhraseState state, PhraseInfo info)
        {
            if (!state.ById.TryGetValue(info.Id, out var row))
            {
                row = new PhraseRow
                {
                    Id = info.Id,
                    AccountId = info.AccountId,
                    Text = info.Text,
                    Active = info.Active,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = info.UpdatedAt,
                    Rev = info.Rev,
                    Tags = info.Tags,
                    TagsSource = info.TagsSource,
                    TagsSemanticTag = info.TagsSemanticTag
                };
                state.ById[info.Id] = row;
            }
            else
            {
                row.Text = info.Text;
                row.Active = info.Active;
                row.UpdatedAt = info.UpdatedAt;
                row.Rev = info.Rev;
                row.Tags = info.Tags;
                row.TagsSource = info.TagsSource;
                row.TagsSemanticTag = info.TagsSemanticTag;
            }
            if (info.Rev > state.MaxRev) state.MaxRev = info.Rev;
        }

        private void UpdateSnapshotAfterToggle(AccountPhraseState state, PhraseInfo info)
        {
            if (state.ById.TryGetValue(info.Id, out var row))
            {
                row.Active = info.Active;
                row.UpdatedAt = info.UpdatedAt;
                row.Rev = info.Rev;
            }
            if (info.Rev > state.MaxRev) state.MaxRev = info.Rev;
        }

        private static int CountWords(string text)
        {
            // Simple word count: split by spaces, ignore empty
            return text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        }

        // Convert account phrases to global (FR-279)
        public async Task<(int converted, int duplicatesRemoved)> ConvertToGlobalPhrasesAsync(long accountId, IEnumerable<long> phraseIds)
        {
            if (accountId <= 0) return (0, 0);
            if (phraseIds == null) return (0, 0);
            var ids = phraseIds.Distinct().Where(id => id > 0).ToList();
            if (ids.Count == 0) return (0, 0);

            await EnsureBackendAsync().ConfigureAwait(false);
            if (!_radiumAvailable) return (0, 0);

            int converted = 0;
            int removed = 0;

            await using var con = CreateConnection();
            await PgConnectionHelper.OpenWithLocalSslFallbackAsync(con).ConfigureAwait(false);
            await using var tx = await con.BeginTransactionAsync().ConfigureAwait(false);
            try
            {
                foreach (var id in ids)
                {
                    // Load source phrase
                    const string selSql = @"SELECT text, active FROM radium.phrase WHERE account_id=@aid AND id=@pid";
                    await using (var sel = new NpgsqlCommand(selSql, con, tx) { CommandTimeout = 12 })
                    {
                        sel.Parameters.AddWithValue("aid", accountId);
                        sel.Parameters.AddWithValue("pid", id);
                        await using var rd = await sel.ExecuteReaderAsync().ConfigureAwait(false);
                        if (!await rd.ReadAsync().ConfigureAwait(false))
                        {
                            // Not found for this account; skip
                            continue;
                        }
                        var text = rd.GetString(0);
                        var active = rd.GetBoolean(1);
                        await rd.DisposeAsync().ConfigureAwait(false);

                        // Ensure a global row exists
                        bool existsGlobal;
                        const string existsSql = @"SELECT 1 FROM radium.phrase WHERE account_id IS NULL AND text=@text LIMIT 1";
                        await using (var exCmd = new NpgsqlCommand(existsSql, con, tx) { CommandTimeout = 12 })
                        {
                            exCmd.Parameters.AddWithValue("text", text);
                            existsGlobal = (await exCmd.ExecuteScalarAsync().ConfigureAwait(false)) != null;
                        }
                        if (!existsGlobal)
                        {
                            const string insSql = @"INSERT INTO radium.phrase(account_id, text, active) VALUES(NULL, @text, @active)";
                            await using var ins = new NpgsqlCommand(insSql, con, tx) { CommandTimeout = 12 };
                            ins.Parameters.AddWithValue("text", text);
                            ins.Parameters.AddWithValue("active", active);
                            converted += await ins.ExecuteNonQueryAsync().ConfigureAwait(false);
                        }

                        // Delete all non-global duplicates of this text
                        const string delSql = @"DELETE FROM radium.phrase WHERE account_id IS NOT NULL AND text=@text";
                        await using var del = new NpgsqlCommand(delSql, con, tx) { CommandTimeout = 12 };
                        del.Parameters.AddWithValue("text", text);
                        removed += await del.ExecuteNonQueryAsync().ConfigureAwait(false);
                    }
                }

                await tx.CommitAsync().ConfigureAwait(false);
            }
            catch (Exception ex) when (IsTransient(ex) || IsTransientTimeout(ex))
            {
                await tx.RollbackAsync().ConfigureAwait(false);
                Debug.WriteLine($"[PhraseService][ConvertToGlobal] Transient error: {ex.GetType().Name} {ex.Message}");
                throw;
            }

            // Global changes affect all accounts
            _cache.ClearAll();
            return (converted, removed);
        }

        // Try to fetch any account id that has phrases available (best-effort helper)
        public async Task<long?> GetAnyAccountIdAsync()
        {
            await EnsureBackendAsync().ConfigureAwait(false);
            if (!_radiumAvailable) return null;
            try
            {
                await using var con = _dsProvider.CentralMeta.CreateConnection();
                await PgConnectionHelper.OpenWithLocalSslFallbackAsync(con).ConfigureAwait(false);
                const string sql = @"SELECT account_id FROM radium.phrase WHERE account_id IS NOT NULL ORDER BY updated_at DESC LIMIT 1";
                await using var cmd = new NpgsqlCommand(sql, con) { CommandTimeout = 8 };
                var obj = await cmd.ExecuteScalarAsync().ConfigureAwait(false);
                if (obj is long l) return l;
                if (obj is int i) return (long)i;
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PhraseService][GetAnyAccountIdAsync] Error: {ex.Message}");
                return null;
            }
        }
    }
}
