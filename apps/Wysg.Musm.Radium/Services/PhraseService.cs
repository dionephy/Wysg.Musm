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
    public class PhraseService : IPhraseService
    {
        private readonly IRadiumLocalSettings _settings;
        private readonly string _fallback = "Host=127.0.0.1;Port=5432;Database=wysg_dev;Username=postgres;Password=`123qweas;Timeout=3"; // legacy fallback (dev only)

        // Backend detection (single-flight)
        private volatile bool _backendChecked;
        private volatile bool _radiumAvailable;
        private Task? _detectTask;
        private readonly object _detectLock = new();

        private sealed class PhraseRow
        {
            public long Id { get; init; }
            public long AccountId { get; init; }
            public string Text { get; set; } = string.Empty;
            public bool Active { get; set; }
            public DateTime CreatedAt { get; init; }
            public DateTime UpdatedAt { get; set; }
            public long Rev { get; set; }
        }

        private sealed class AccountPhraseState
        {
            public long AccountId { get; }
            public long MaxRev { get; set; }
            public long MaxIdLoaded { get; set; }
            public readonly Dictionary<long, PhraseRow> ById = new();
            public DateTime SnapshotLoadedAtUtc { get; set; }
            public volatile bool Loading;
            public AccountPhraseState(long id) { AccountId = id; }
        }

        private readonly ConcurrentDictionary<long, AccountPhraseState> _states = new();
        private readonly ConcurrentDictionary<long, SemaphoreSlim> _locks = new();
        private volatile bool _indexEnsured;
        private DateTime _lastIndexCheckUtc = DateTime.MinValue;
        private int _indexCreateInFlight = 0;

        public PhraseService(IRadiumLocalSettings settings)
        {
            _settings = settings;
            Debug.WriteLine("[PhraseService] Using central radium.phrase delta-sync backend.");
        }

        private string BuildConnectionString()
        {
            var raw = _settings.CentralConnectionString ?? _settings.LocalConnectionString ?? _fallback;
            try
            {
                var b = new NpgsqlConnectionStringBuilder(raw)
                {
                    IncludeErrorDetail = true,
                    Multiplexing = false,
                    KeepAlive = 15,
                    // Increase default command timeout; page reads are short but allow cushion
                    CommandTimeout = 60
                };
                if (b.Timeout < 5) b.Timeout = 5;
                if (b.CommandTimeout < 30) b.CommandTimeout = 30;
                return b.ConnectionString;
            }
            catch { return raw; }
        }

        private NpgsqlConnection CreateConnection()
        {
            var cs = BuildConnectionString();
            var b = new NpgsqlConnectionStringBuilder(cs);
            Debug.WriteLine($"[PhraseService] Open DB Host={b.Host} Db={b.Database} User={b.Username} Multiplexing={b.Multiplexing}");
            return new NpgsqlConnection(cs);
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
                await using var con = CreateConnection();
                await con.OpenAsync().ConfigureAwait(false);
                await using var cmd = new NpgsqlCommand("SELECT 1 FROM information_schema.tables WHERE table_schema='radium' AND table_name='phrase'", con);
                var existsObj = await cmd.ExecuteScalarAsync().ConfigureAwait(false);
                _radiumAvailable = existsObj != null;
                Debug.WriteLine($"[PhraseService] Detection complete radiumAvailable={_radiumAvailable}");
                if (_radiumAvailable) _ = Task.Run(EnsureIndexAsync); // fire & forget index ensure
            }
            catch (Exception ex)
            {
                _radiumAvailable = false;
                Debug.WriteLine($"[PhraseService] Detection error {ex.GetType().Name}: {ex.Message}");
            }
            finally { _backendChecked = true; }
        }

        private async Task EnsureIndexAsync()
        {
            if (_indexEnsured) return;
            if (Environment.GetEnvironmentVariable("RAD_SKIP_PHRASE_INDEX") == "1") { _indexEnsured = true; return; }
            var now = DateTime.UtcNow;
            if ((now - _lastIndexCheckUtc) < TimeSpan.FromHours(1) && _lastIndexCheckUtc != DateTime.MinValue) return;
            _lastIndexCheckUtc = now;

            if (Interlocked.CompareExchange(ref _indexCreateInFlight, 1, 0) != 1)
            {
                try
                {
                    await using var con = CreateConnection();
                    using var openCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                    await con.OpenAsync(openCts.Token).ConfigureAwait(false);

                    const string existsSql = "SELECT 1 FROM pg_indexes WHERE schemaname='radium' AND indexname='ix_phrase_account_id'";
                    using var existsCts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                    try
                    {
                        await using (var existsCmd = new NpgsqlCommand(existsSql, con) { CommandTimeout = 10 })
                        {
                            var exists = await existsCmd.ExecuteScalarAsync(existsCts.Token).ConfigureAwait(false) != null;
                            if (exists) { _indexEnsured = true; return; }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // Existence check too slow on free tier ? skip index creation to avoid recurring timeouts
                        Debug.WriteLine("[PhraseService][INDEX] Existence check timeout; skipping index ensure (mark ensured)");
                        _indexEnsured = true; return;
                    }

                    // Build index only if existence check was fast (already returned); guard with longer but bounded timeout
                    using var buildCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                    const string createSql = "CREATE INDEX IF NOT EXISTS ix_phrase_account_id ON radium.phrase (account_id, id)"; // no CONCURRENTLY to reduce duration on small table
                    try
                    {
                        await using (var st = new NpgsqlCommand("SET LOCAL statement_timeout TO 30000", con))
                            await st.ExecuteNonQueryAsync(buildCts.Token).ConfigureAwait(false);
                        await using var cmd = new NpgsqlCommand(createSql, con) { CommandTimeout = 40 };
                        await cmd.ExecuteNonQueryAsync(buildCts.Token).ConfigureAwait(false);
                        Debug.WriteLine("[PhraseService][INDEX] Created / verified index.");
                        _indexEnsured = true;
                    }
                    catch (OperationCanceledException)
                    {
                        Debug.WriteLine("[PhraseService][INDEX] Build timeout; will not retry for 1h.");
                        _indexEnsured = true; // prevent repeated stalls
                    }
                    catch (Exception ex) when (IsTransient(ex))
                    {
                        Debug.WriteLine($"[PhraseService][INDEX] Transient during build: {ex.Message} (skip)");
                        _indexEnsured = true;
                    }
                }
                catch (Exception ex) when (IsTransient(ex))
                {
                    Debug.WriteLine($"[PhraseService][INDEX] Transient open: {ex.Message} (skip this round)");
                    _indexEnsured = true; // skip further attempts
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[PhraseService][INDEX] Unrecoverable: {ex.Message} (skip)");
                    _indexEnsured = true;
                }
                finally
                {
                    Interlocked.Exchange(ref _indexCreateInFlight, 0);
                }
            }
        }

        private SemaphoreSlim GetLock(long accountId) => _locks.GetOrAdd(accountId, _ => new SemaphoreSlim(1, 1));

        private async Task EnsureUpToDateAsync(long accountId)
        {
            await EnsureBackendAsync().ConfigureAwait(false);
            if (!_radiumAvailable) return;
            var lck = GetLock(accountId);
            await lck.WaitAsync().ConfigureAwait(false);
            try
            {
                var state = _states.GetOrAdd(accountId, id => new AccountPhraseState(id));
                if (state.ById.Count == 0 && !state.Loading)
                {
                    _ = Task.Run(() => LoadSnapshotPagedAsync(state));
                    return;
                }
            }
            finally { lck.Release(); }
        }

        private async Task LoadSnapshotPagedAsync(AccountPhraseState state)
        {
            if (state.Loading) return;
            state.Loading = true;
            try
            {
                int pageSize = 300; // smaller initial page to avoid timeouts
                const int HardCap = 15000;
                long lastId = state.MaxIdLoaded;
                int attempt = 0;
                while (state.ById.Count < HardCap)
                {
                    attempt++;
                    var sw = Stopwatch.StartNew();
                    List<PhraseRow> rows;
                    try
                    {
                        rows = await LoadPageAsync(state.AccountId, lastId, pageSize).ConfigureAwait(false);
                    }
                    catch (TimeoutException tex)
                    {
                        Debug.WriteLine($"[PhraseService][WARN] Page timeout pageSize={pageSize} attempt={attempt} afterId={lastId}: {tex.Message}");
                        // shrink page size and retry limited times
                        pageSize = Math.Max(50, pageSize / 2);
                        if (pageSize <= 50) continue; // retry with smaller size
                        continue;
                    }
                    catch (NpgsqlException nex) when (nex.InnerException is TimeoutException)
                    {
                        pageSize = Math.Max(50, pageSize / 2);
                        Debug.WriteLine($"[PhraseService][WARN] Npgsql timeout, reducing pageSize -> {pageSize}");
                        continue;
                    }
                    sw.Stop();
                    if (rows.Count == 0) break;
                    foreach (var r in rows)
                    {
                        state.ById[r.Id] = r;
                        if (r.Rev > state.MaxRev) state.MaxRev = r.Rev;
                        if (r.Id > lastId) lastId = r.Id;
                    }
                    state.MaxIdLoaded = lastId;
                    Debug.WriteLine($"[PhraseService] Loaded page rows={rows.Count} total={state.ById.Count} pageSize={pageSize} elapsed={sw.ElapsedMilliseconds}ms");
                    // adaptively grow if fast
                    if (rows.Count == pageSize && sw.ElapsedMilliseconds < 250 && pageSize < 2000)
                        pageSize = Math.Min(2000, pageSize * 2);
                    if (rows.Count < pageSize) break;
                }
                state.SnapshotLoadedAtUtc = DateTime.UtcNow;
                Debug.WriteLine($"[PhraseService] Snapshot complete account={state.AccountId} rows={state.ById.Count} rev={state.MaxRev}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PhraseService] Snapshot load error account={state.AccountId}: {ex.GetType().Name} {ex.Message}");
            }
            finally { state.Loading = false; }
        }

        private async Task<List<PhraseRow>> LoadPageAsync(long accountId, long afterId, int take)
        {
            var list = new List<PhraseRow>(take);
            const string sql = @"SELECT id, account_id, text, active, created_at, updated_at, rev
                                   FROM radium.phrase
                                   WHERE account_id=@aid AND id > @after
                                   ORDER BY id
                                   LIMIT @lim";
            await using var con = CreateConnection();
            await con.OpenAsync().ConfigureAwait(false);
            await using var cmd = new NpgsqlCommand(sql, con) { CommandTimeout = 45 };
            cmd.Parameters.AddWithValue("aid", accountId);
            cmd.Parameters.AddWithValue("after", afterId);
            cmd.Parameters.AddWithValue("lim", take);
            try
            {
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
                        Rev = rd.GetInt64(6)
                    });
                }
            }
            catch (NpgsqlException ex) when (ex.InnerException is TimeoutException tex)
            {
                Debug.WriteLine($"[PhraseService][TIMEOUT] LoadPage after={afterId} take={take}: {tex.Message}");
                throw tex; // convert to TimeoutException for outer logic
            }
            return list;
        }

        public async Task<IReadOnlyList<string>> GetPhrasesForAccountAsync(long accountId)
        {
            await EnsureUpToDateAsync(accountId).ConfigureAwait(false);
            if (_states.TryGetValue(accountId, out var state))
                return state.ById.Values.Where(r => r.Active).Select(r => r.Text).OrderBy(t => t).Take(500).ToList();
            return Array.Empty<string>();
        }

        public async Task<IReadOnlyList<string>> GetPhrasesByPrefixAccountAsync(long accountId, string prefix, int limit = 50)
        {
            if (string.IsNullOrWhiteSpace(prefix)) return Array.Empty<string>();
            await EnsureUpToDateAsync(accountId).ConfigureAwait(false);
            if (_states.TryGetValue(accountId, out var state))
            {
                return state.ById.Values.Where(r => r.Active && r.Text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(r => r.Text.Length).ThenBy(r => r.Text)
                    .Take(limit).Select(r => r.Text).ToList();
            }
            return Array.Empty<string>();
        }

        // Deprecated wrappers
        public Task<IReadOnlyList<string>> GetPhrasesForTenantAsync(long tenantId) => GetPhrasesForAccountAsync(tenantId);
        public Task<IReadOnlyList<string>> GetPhrasesByPrefixAsync(long tenantId, string prefix, int limit = 50) => GetPhrasesByPrefixAccountAsync(tenantId, prefix, limit);

        public async Task<IReadOnlyList<PhraseInfo>> GetAllPhraseMetaAsync(long accountId)
        {
            // Prefer snapshot if already loaded / loading
            await EnsureUpToDateAsync(accountId).ConfigureAwait(false);
            if (_states.TryGetValue(accountId, out var state) && state.ById.Count > 0)
            {
                return state.ById.Values
                    .OrderByDescending(r => r.UpdatedAt)
                    .Take(1000)
                    .Select(r => new PhraseInfo(r.Id, r.AccountId, r.Text, r.Active, r.UpdatedAt, r.Rev))
                    .ToList();
            }
            // If still empty (first call, background load running) return empty; UI can retry.
            return Array.Empty<PhraseInfo>();
        }

        private static bool IsTransient(Exception ex)
        {
            if (ex is TimeoutException) return true;
            if (ex is IOException) return true;
            if (ex is SocketException) return true;
            if (ex is NpgsqlException npgEx)
            {
                if (npgEx.InnerException is TimeoutException) return true;
                if (npgEx.InnerException is SocketException) return true;
                if (npgEx.Message.IndexOf("reading from stream", StringComparison.OrdinalIgnoreCase) >= 0) return true;
                if (npgEx.Message.IndexOf("Timeout", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            }
            return false;
        }

        public async Task<PhraseInfo> UpsertPhraseAsync(long accountId, string text, bool active = true)
        {
            await EnsureBackendAsync().ConfigureAwait(false);
            if (!_radiumAvailable) throw new InvalidOperationException("radium.phrase not available");
            const string sql = @"INSERT INTO radium.phrase(account_id, text, active) VALUES(@aid,@text,@active)
                                 ON CONFLICT (account_id,text) DO UPDATE SET active=EXCLUDED.active, updated_at=now(), rev=nextval('radium.phrase_rev_seq')
                                 RETURNING id, account_id, text, active, created_at, updated_at, rev";
            int attempts = 0;
            while (true)
            {
                attempts++;
                NpgsqlConnection? con = null;
                try
                {
                    con = CreateConnection();
                    await con.OpenAsync().ConfigureAwait(false);
                    // defensive: per-connection statement timeout (ms)
                    await using (var stCmd = new NpgsqlCommand("SET LOCAL statement_timeout TO 10000", con))
                        await stCmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                    await using var cmd = new NpgsqlCommand(sql, con) { CommandTimeout = 45 };
                    cmd.Parameters.AddWithValue("aid", accountId);
                    cmd.Parameters.AddWithValue("text", text);
                    cmd.Parameters.AddWithValue("active", active);
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(12));
                    await using var rd = await cmd.ExecuteReaderAsync(cts.Token).ConfigureAwait(false);
                    if (await rd.ReadAsync(cts.Token).ConfigureAwait(false))
                    {
                        var info = new PhraseInfo(rd.GetInt64(0), rd.GetInt64(1), rd.GetString(2), rd.GetBoolean(3), rd.GetDateTime(5), rd.GetInt64(6));
                        var st = _states.GetOrAdd(accountId, id => new AccountPhraseState(id));
                        st.ById[info.Id] = new PhraseRow
                        {
                            Id = info.Id,
                            AccountId = info.AccountId,
                            Text = info.Text,
                            Active = info.Active,
                            CreatedAt = rd.GetDateTime(4),
                            UpdatedAt = info.UpdatedAt,
                            Rev = info.Rev
                        };
                        if (info.Rev > st.MaxRev) st.MaxRev = info.Rev;
                        if (info.Id > st.MaxIdLoaded) st.MaxIdLoaded = info.Id;
                        return info;
                    }
                    throw new InvalidOperationException("Upsert failed");
                }
                catch (Exception ex) when (IsTransient(ex) && attempts < 4)
                {
                    Debug.WriteLine($"[PhraseService][RETRY] Upsert attempt={attempts} text='{text}' transient: {ex.Message}");
                    if (con != null) try { NpgsqlConnection.ClearPool(con); } catch { }
                    await Task.Delay(150 * attempts).ConfigureAwait(false);
                    continue;
                }
                catch (OperationCanceledException oce)
                {
                    // treat as transient timeout
                    if (attempts < 4)
                    {
                        Debug.WriteLine($"[PhraseService][RETRY] Upsert canceled/timeout attempt={attempts} text='{text}': {oce.Message}");
                        if (con != null) try { NpgsqlConnection.ClearPool(con); } catch { }
                        await Task.Delay(150 * attempts).ConfigureAwait(false);
                        continue;
                    }
                    throw;
                }
            }
        }

        public async Task<PhraseInfo?> ToggleActiveAsync(long accountId, long phraseId)
        {
            await EnsureBackendAsync().ConfigureAwait(false);
            if (!_radiumAvailable) return null;
            const string sql = @"UPDATE radium.phrase SET active = NOT active, updated_at=now(), rev=nextval('radium.phrase_rev_seq')
                                 WHERE account_id=@aid AND id=@pid
                                 RETURNING id, account_id, text, active, created_at, updated_at, rev";
            int attempts = 0;
            while (true)
            {
                attempts++;
                NpgsqlConnection? con = null;
                try
                {
                    con = CreateConnection();
                    await con.OpenAsync().ConfigureAwait(false);
                    await using (var stCmd = new NpgsqlCommand("SET LOCAL statement_timeout TO 8000", con))
                        await stCmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                    await using var cmd = new NpgsqlCommand(sql, con) { CommandTimeout = 30 };
                    cmd.Parameters.AddWithValue("aid", accountId);
                    cmd.Parameters.AddWithValue("pid", phraseId);
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                    await using var rd = await cmd.ExecuteReaderAsync(cts.Token).ConfigureAwait(false);
                    if (await rd.ReadAsync(cts.Token).ConfigureAwait(false))
                    {
                        var info = new PhraseInfo(rd.GetInt64(0), rd.GetInt64(1), rd.GetString(2), rd.GetBoolean(3), rd.GetDateTime(5), rd.GetInt64(6));
                        if (_states.TryGetValue(accountId, out var st) && st.ById.TryGetValue(info.Id, out var row))
                        {
                            row.Active = info.Active;
                            row.UpdatedAt = info.UpdatedAt;
                            row.Rev = info.Rev;
                            if (info.Rev > st.MaxRev) st.MaxRev = info.Rev;
                        }
                        return info;
                    }
                    return null;
                }
                catch (Exception ex) when (IsTransient(ex) && attempts < 4)
                {
                    Debug.WriteLine($"[PhraseService][RETRY] Toggle attempt={attempts} id={phraseId} transient: {ex.Message}");
                    if (con != null) try { NpgsqlConnection.ClearPool(con); } catch { }
                    await Task.Delay(150 * attempts).ConfigureAwait(false);
                    continue;
                }
                catch (OperationCanceledException oce)
                {
                    if (attempts < 4)
                    {
                        Debug.WriteLine($"[PhraseService][RETRY] Toggle canceled/timeout attempt={attempts} id={phraseId}: {oce.Message}");
                        if (con != null) try { NpgsqlConnection.ClearPool(con); } catch { }
                        await Task.Delay(150 * attempts).ConfigureAwait(false);
                        continue;
                    }
                    throw;
                }
            }
        }

        public async Task<long?> GetAnyAccountIdAsync()
        {
            await EnsureBackendAsync().ConfigureAwait(false);
            if (!_radiumAvailable) return null;
            const string sql = "SELECT account_id FROM radium.phrase ORDER BY account_id LIMIT 1";
            await using var con = CreateConnection();
            await con.OpenAsync().ConfigureAwait(false);
            await using var cmd = new NpgsqlCommand(sql, con);
            var result = await cmd.ExecuteScalarAsync().ConfigureAwait(false);
            return result == null ? null : (long?)(result is long l ? l : Convert.ToInt64(result));
        }
    }
}
