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
        private readonly ICentralDataSourceProvider _dsProvider;
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
            // When true we already performed an eager snapshot and should not auto-trigger background paging anymore.
            public bool Preloaded { get; set; }
            public AccountPhraseState(long id) { AccountId = id; }
        }

        private readonly ConcurrentDictionary<long, AccountPhraseState> _states = new();
        private readonly ConcurrentDictionary<long, SemaphoreSlim> _locks = new();
        private volatile bool _indexEnsured;
        private DateTime _lastIndexCheckUtc = DateTime.MinValue;
        private int _indexCreateInFlight = 0;

        public PhraseService(IRadiumLocalSettings settings, ICentralDataSourceProvider dsProvider)
        {
            _settings = settings;
            _dsProvider = dsProvider;
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
                    CommandTimeout = 60,
                    NoResetOnClose = false // align with central data source policy
                };
                if (!b.ContainsKey("Cancellation Timeout")) b["Cancellation Timeout"] = 4000; // ms for faster cancel cleanup
                if (b.Timeout < 8) b.Timeout = 8;
                if (b.CommandTimeout < 30) b.CommandTimeout = 30;
                return b.ConnectionString;
            }
            catch { return raw; }
        }

        private NpgsqlConnection CreateConnection()
        {
            // All central opens go through shared data source
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
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(12)))
                {
                    var openTask = PgConnectionHelper.OpenWithLocalSslFallbackAsync(con);
                    var completed = await Task.WhenAny(openTask, Task.Delay(Timeout.Infinite, cts.Token));
                    if (completed != openTask)
                    {
                        Debug.WriteLine("[PhraseService][Detect][OPEN-CANCEL] open timeout");
                        _radiumAvailable = false; _backendChecked = true; return;
                    }
                    await openTask.ConfigureAwait(false);
                }

                const string sql = "SELECT 1 FROM information_schema.tables WHERE table_schema='radium' AND table_name='phrase'";
                object? existsObj = null;
                using var cmd = new NpgsqlCommand(sql, con) { CommandTimeout = 8 };
                using (var execCts = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
                {
                    var execTask = cmd.ExecuteScalarAsync();
                    var completed = await Task.WhenAny(execTask, Task.Delay(Timeout.Infinite, execCts.Token));
                    if (completed != execTask)
                    {
                        Debug.WriteLine("[PhraseService][Detect][CMD-CANCEL] metadata timeout");
                        _radiumAvailable = false; _backendChecked = true; return;
                    }
                    existsObj = await execTask.ConfigureAwait(false);
                }
                _radiumAvailable = existsObj != null;
                Debug.WriteLine($"[PhraseService] Detection complete radiumAvailable={_radiumAvailable}");
                if (_radiumAvailable) _ = Task.Run(EnsureIndexAsync);
            }
            catch (OperationCanceledException oce) { Debug.WriteLine("[PhraseService][Detect][CANCEL-GEN] " + oce.Message); _radiumAvailable = false; }
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
                    try { await PgConnectionHelper.OpenWithLocalSslFallbackAsync(con); }
                    catch (OperationCanceledException oce) { Debug.WriteLine("[PhraseService][INDEX][CANCEL-Open] " + oce.Message); _indexEnsured = true; return; }

                    const string existsSql = "SELECT 1 FROM pg_indexes WHERE schemaname='radium' AND indexname='ix_phrase_account_id'";
                    try
                    {
                        await using (var existsCmd = new NpgsqlCommand(existsSql, con) { CommandTimeout = 10 })
                        {
                            var exists = await existsCmd.ExecuteScalarAsync() != null;
                            if (exists) { _indexEnsured = true; return; }
                        }
                    }
                    catch (OperationCanceledException oce)
                    {
                        Debug.WriteLine("[PhraseService][INDEX][CANCEL-Exists] " + oce.Message);
                        _indexEnsured = true; return;
                    }

                    const string createSql = "CREATE INDEX IF NOT EXISTS ix_phrase_account_id ON radium.phrase (account_id, id)";
                    try
                    {
                        await using (var st = new NpgsqlCommand("SET LOCAL statement_timeout TO 30000", con))
                            await st.ExecuteNonQueryAsync();
                        await using var cmd = new NpgsqlCommand(createSql, con) { CommandTimeout = 40 };
                        await cmd.ExecuteNonQueryAsync();
                        Debug.WriteLine("[PhraseService][INDEX] Created / verified index.");
                        _indexEnsured = true;
                    }
                    catch (OperationCanceledException oce)
                    {
                        Debug.WriteLine("[PhraseService][INDEX][CANCEL-Build] " + oce.Message);
                        _indexEnsured = true;
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
                    _indexEnsured = true;
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
                if (state.Preloaded) return; // eager snapshot already loaded
                if (state.ById.Count == 0 && !state.Loading)
                {
                    state.Loading = true;
                    _ = Task.Run(() => LoadSnapshotPagedAsync(state));
                    return;
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
                if (state.Preloaded) return; // already done
                if (state.Loading)
                {
                    // Another preload / background load running, wait briefly until it completes.
                    lck.Release();
                    for (int i = 0; i < 40; i++)
                    {
                        await Task.Delay(50).ConfigureAwait(false);
                        if (!state.Loading) return;
                    }
                    return;
                }
                state.Loading = true;
                try
                {
                    await EagerLoadAllAsync(state).ConfigureAwait(false);
                    state.Preloaded = true;
                }
                catch (OperationCanceledException oce) { Debug.WriteLine("[PhraseService][Preload][CANCEL] " + oce.Message); }
                finally
                {
                    state.Loading = false;
                }
            }
            finally
            {
                if (lck.CurrentCount == 0) lck.Release();
            }
        }

        private async Task EagerLoadAllAsync(AccountPhraseState state)
        {
            try
            {
                Debug.WriteLine($"[PhraseService][PreloadFull] BEGIN account={state.AccountId} at={DateTime.UtcNow:O}");
                const int Batch = 2000; // large batches ok for single pass
                long lastId = 0;
                while (true)
                {
                    var rows = await LoadPageAsync(state.AccountId, lastId, Batch).ConfigureAwait(false);
                    if (rows.Count == 0) break;
                    foreach (var r in rows)
                    {
                        state.ById[r.Id] = r;
                        if (r.Rev > state.MaxRev) state.MaxRev = r.Rev;
                        if (r.Id > lastId) lastId = r.Id;
                    }
                    state.MaxIdLoaded = lastId;
                    if (rows.Count < Batch) break;
                }
                state.SnapshotLoadedAtUtc = DateTime.UtcNow;
                Debug.WriteLine($"[PhraseService][PreloadFull] END account={state.AccountId} rows={state.ById.Count} at={DateTime.UtcNow:O}");
            }
            catch (OperationCanceledException oce) { Debug.WriteLine("[PhraseService][PreloadFull][CANCEL] " + oce.Message); }
            catch (Exception ex) { Debug.WriteLine($"[PhraseService] Preload error account={state.AccountId}: {ex.Message}"); }
        }

        private async Task LoadSnapshotPagedAsync(AccountPhraseState state)
        {
            try
            {
                Debug.WriteLine($"[PhraseService][Snapshot] BEGIN account={state.AccountId} at={DateTime.UtcNow:O}");
                int pageSize = 300;
                const int HardCap = 15000;
                long lastId = state.MaxIdLoaded;
                int attempt = 0;
                while (state.ById.Count < HardCap)
                {
                    attempt++; var sw = Stopwatch.StartNew();
                    List<PhraseRow> rows;
                    try { rows = await LoadPageAsync(state.AccountId, lastId, pageSize).ConfigureAwait(false); }
                    catch (OperationCanceledException oce) { Debug.WriteLine("[PhraseService][Page][CANCEL] " + oce.Message); break; }
                    catch (TimeoutException tex) { Debug.WriteLine($"[PhraseService][WARN] Page timeout size={pageSize} afterId={lastId}: {tex.Message}"); pageSize = Math.Max(50, pageSize / 2); continue; }
                    catch (NpgsqlException nex) when (nex.InnerException is TimeoutException) { pageSize = Math.Max(50, pageSize / 2); Debug.WriteLine($"[PhraseService][WARN] Npgsql timeout size->{pageSize}"); continue; }
                    sw.Stop();
                    if (rows.Count == 0) break;
                    foreach (var r in rows)
                    {
                        state.ById[r.Id] = r;
                        if (r.Rev > state.MaxRev) state.MaxRev = r.Rev;
                        if (r.Id > lastId) lastId = r.Id;
                    }
                    state.MaxIdLoaded = lastId;
                    Debug.WriteLine($"[PhraseService] Loaded page rows={rows.Count} total={state.ById.Count} size={pageSize} ms={sw.ElapsedMilliseconds}");
                    if (rows.Count == pageSize && sw.ElapsedMilliseconds < 250 && pageSize < 2000)
                        pageSize = Math.Min(2000, pageSize * 2);
                    if (rows.Count < pageSize) break;
                }
                state.SnapshotLoadedAtUtc = DateTime.UtcNow;
                Debug.WriteLine($"[PhraseService][Snapshot] END account={state.AccountId} rows={state.ById.Count} rev={state.MaxRev} at={DateTime.UtcNow:O}");
            }
            catch (OperationCanceledException oce) { Debug.WriteLine("[PhraseService][Snapshot][CANCEL] " + oce.Message); }
            catch (Exception ex) { Debug.WriteLine($"[PhraseService] Snapshot load error account={state.AccountId}: {ex.GetType().Name} {ex.Message}"); }
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
            try { await PgConnectionHelper.OpenWithLocalSslFallbackAsync(con).ConfigureAwait(false); }
            catch (OperationCanceledException oce) { Debug.WriteLine("[PhraseService][PageOpen][CANCEL] " + oce.Message); return list; }
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
            catch (OperationCanceledException oce) { Debug.WriteLine("[PhraseService][PageExec][CANCEL] " + oce.Message); }
            catch (NpgsqlException ex) when (ex.InnerException is TimeoutException tex)
            {
                Debug.WriteLine($"[PhraseService][TIMEOUT] LoadPage after={afterId} take={take}: {tex.Message}");
                throw tex; // convert to TimeoutException for outer logic
            }
            return list;
        }

        public async Task<IReadOnlyList<string>> GetPhrasesForAccountAsync(long accountId)
        {
            try { await EnsureUpToDateAsync(accountId).ConfigureAwait(false); }
            catch (OperationCanceledException oce) { Debug.WriteLine("[PhraseService][GetPhrases][CANCEL] " + oce.Message); }
            if (_states.TryGetValue(accountId, out var state))
                return state.ById.Values.Where(r => r.Active).Select(r => r.Text).OrderBy(t => t).Take(500).ToList();
            return Array.Empty<string>();
        }

        public async Task<IReadOnlyList<string>> GetPhrasesByPrefixAccountAsync(long accountId, string prefix, int limit = 50)
        {
            if (string.IsNullOrWhiteSpace(prefix)) return Array.Empty<string>();
            try { await EnsureUpToDateAsync(accountId).ConfigureAwait(false); }
            catch (OperationCanceledException oce) { Debug.WriteLine("[PhraseService][GetPrefix][CANCEL] " + oce.Message); }
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
            try { await EnsureUpToDateAsync(accountId).ConfigureAwait(false); }
            catch (OperationCanceledException oce) { Debug.WriteLine("[PhraseService][GetMeta][CANCEL] " + oce.Message); }
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

        public async Task<PhraseInfo> UpsertPhraseAsync(long accountId, string text, bool active = true)
        {
            // NOTE (Rev Stabilization):
            // Originally the UPSERT logic always executed an UPDATE branch (ON CONFLICT) that set updated_at=now() and rev=nextval(...)
            // even when neither 'text' nor 'active' changed. In the database a BEFORE UPDATE trigger (radium.touch_phrase)
            // ALSO bumped updated_at and rev unconditionally. Result: every window open (ensure / refresh) caused silent rev churn.
            // Fix path:
            //   1. Application: perform a pre-select; if row exists and Active already matches, return early (NO UPDATE sent).
            //   2. Database: trigger changed to bump rev only when NEW.active IS DISTINCT FROM OLD.active OR NEW.text IS DISTINCT FROM OLD.text.
            //   3. Application UPDATE statements no longer assign updated_at / rev explicitly (let trigger decide).
            //   4. UI (PhrasesViewModel) suppresses ToggleActive during initial binding to avoid accidental UPDATE.
            // This method therefore only produces rev increments for real state transitions.
            await EnsureBackendAsync().ConfigureAwait(false);
            if (!_radiumAvailable) throw new InvalidOperationException("radium.phrase not available");

            const string selectExistingSql = @"SELECT id, account_id, text, active, created_at, updated_at, rev
                                                FROM radium.phrase
                                                WHERE account_id=@aid AND text=@text";
            const string insertSql = @"INSERT INTO radium.phrase(account_id,text,active) VALUES(@aid,@text,@active)
                                      RETURNING id, account_id, text, active, created_at, updated_at, rev";
            const string updateSql = @"UPDATE radium.phrase SET active=@active
                                      WHERE account_id=@aid AND text=@text
                                      RETURNING id, account_id, text, active, created_at, updated_at, rev";

            int attempts = 0;
            while (true)
            {
                attempts++;
                NpgsqlConnection? con = null;
                try
                {
                    con = CreateConnection();
                    try { await PgConnectionHelper.OpenWithLocalSslFallbackAsync(con).ConfigureAwait(false); }
                    catch (OperationCanceledException oce) { Debug.WriteLine("[PhraseService][Upsert][CANCEL-Open] " + oce.Message); if (attempts >= 4) throw; else continue; }
                    await using (var stCmd = new NpgsqlCommand("SET LOCAL statement_timeout TO 10000", con))
                        await stCmd.ExecuteNonQueryAsync().ConfigureAwait(false);

                    // 1. Pre-check existing to avoid unnecessary UPDATE (rev bump)
                    PhraseInfo? existing = null;
                    await using (var sel = new NpgsqlCommand(selectExistingSql, con) { CommandTimeout = 20 })
                    {
                        sel.Parameters.AddWithValue("aid", accountId);
                        sel.Parameters.AddWithValue("text", text);
                        using var ctsSel = new CancellationTokenSource(TimeSpan.FromSeconds(8));
                        await using var rdSel = await sel.ExecuteReaderAsync(ctsSel.Token).ConfigureAwait(false);
                        if (await rdSel.ReadAsync(ctsSel.Token).ConfigureAwait(false))
                        {
                            existing = new PhraseInfo(rdSel.GetInt64(0), rdSel.GetInt64(1), rdSel.GetString(2), rdSel.GetBoolean(3), rdSel.GetDateTime(5), rdSel.GetInt64(6));
                        }
                    }

                    if (existing != null)
                    {
                        // If no logical change, return as-is (no rev / updated_at change)
                        if (existing.Active == active)
                        {
                            Debug.WriteLine($"[PhraseService][Upsert] SKIP(no-change) text='{text}' rev={existing.Rev}");
                            CacheAfterUpsert(existing);
                            return existing;
                        }
                        // Perform targeted update only when active differs
                        PhraseInfo? updated = null;
                        await using (var upd = new NpgsqlCommand(updateSql, con) { CommandTimeout = 30 })
                        {
                            upd.Parameters.AddWithValue("aid", accountId);
                            upd.Parameters.AddWithValue("text", text);
                            upd.Parameters.AddWithValue("active", active);
                            using var ctsUpd = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                            await using var rdUpd = await upd.ExecuteReaderAsync(ctsUpd.Token).ConfigureAwait(false);
                            if (await rdUpd.ReadAsync(ctsUpd.Token).ConfigureAwait(false))
                            {
                                updated = new PhraseInfo(rdUpd.GetInt64(0), rdUpd.GetInt64(1), rdUpd.GetString(2), rdUpd.GetBoolean(3), rdUpd.GetDateTime(5), rdUpd.GetInt64(6));
                            }
                        }
                        if (updated == null) throw new InvalidOperationException("Upsert update failed");
                        Debug.WriteLine($"[PhraseService][Upsert] UPDATE text='{text}' oldRev={existing.Rev} newRev={updated.Rev}");
                        CacheAfterUpsert(updated);
                        return updated;
                    }
                    else
                    {
                        // Insert new
                        PhraseInfo? inserted = null;
                        await using (var ins = new NpgsqlCommand(insertSql, con) { CommandTimeout = 30 })
                        {
                            ins.Parameters.AddWithValue("aid", accountId);
                            ins.Parameters.AddWithValue("text", text);
                            ins.Parameters.AddWithValue("active", active);
                            using var ctsIns = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                            await using var rdIns = await ins.ExecuteReaderAsync(ctsIns.Token).ConfigureAwait(false);
                            if (await rdIns.ReadAsync(ctsIns.Token).ConfigureAwait(false))
                            {
                                inserted = new PhraseInfo(rdIns.GetInt64(0), rdIns.GetInt64(1), rdIns.GetString(2), rdIns.GetBoolean(3), rdIns.GetDateTime(5), rdIns.GetInt64(6));
                            }
                        }
                        if (inserted == null) throw new InvalidOperationException("Upsert insert failed");
                        Debug.WriteLine($"[PhraseService][Upsert] INSERT text='{text}' rev={inserted.Rev}");
                        CacheAfterUpsert(inserted);
                        return inserted;
                    }
                }
                catch (OperationCanceledException oce)
                {
                    Debug.WriteLine($"[PhraseService][Upsert][CANCEL] attempt={attempts} text='{text}' {oce.Message}");
                    if (attempts < 4) { await Task.Delay(150 * attempts).ConfigureAwait(false); continue; }
                    throw;
                }
                catch (Exception ex) when (IsTransient(ex) && attempts < 4)
                {
                    Debug.WriteLine($"[PhraseService][RETRY] Upsert attempt={attempts} text='{text}' transient: {ex.Message}");
                    if (con != null) try { NpgsqlConnection.ClearPool(con); } catch { }
                    await Task.Delay(150 * attempts).ConfigureAwait(false); continue;
                }
            }
        }

        private void CacheAfterUpsert(PhraseInfo info)
        {
            // Cache policy: we mirror only the active flag / rev / updated_at returned.
            // Since we intentionally skip no-op updates, a returned row implies either initial insert
            // or a genuine state change validated by server trigger logic.
            var st = _states.GetOrAdd(info.AccountId, id => new AccountPhraseState(id));
            if (!st.ById.TryGetValue(info.Id, out var row))
            {
                row = new PhraseRow
                {
                    Id = info.Id,
                    AccountId = info.AccountId,
                    Text = info.Text,
                    Active = info.Active,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = info.UpdatedAt,
                    Rev = info.Rev
                };
                st.ById[info.Id] = row;
            }
            else
            {
                row.Active = info.Active;
                row.UpdatedAt = info.UpdatedAt;
                row.Rev = info.Rev;
            }
            if (info.Rev > st.MaxRev) st.MaxRev = info.Rev;
            if (info.Id > st.MaxIdLoaded) st.MaxIdLoaded = info.Id;
        }

        public async Task<PhraseInfo?> ToggleActiveAsync(long accountId, long phraseId)
        {
            // ToggleActive deliberately sends a minimal UPDATE (no manual updated_at/rev) letting the trigger bump only when Active flips.
            // If UI race causes mismatch (user double-click), we re-check and optionally re-toggle (see PhrasesViewModel).
            await EnsureBackendAsync().ConfigureAwait(false);
            if (!_radiumAvailable) return null;
            const string sql = @"UPDATE radium.phrase SET active = NOT active
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
                    try { await PgConnectionHelper.OpenWithLocalSslFallbackAsync(con).ConfigureAwait(false); }
                    catch (OperationCanceledException oce) { Debug.WriteLine("[PhraseService][Toggle][CANCEL-Open] " + oce.Message); if (attempts < 4) { await Task.Delay(150 * attempts); continue; } else return null; }
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
                            row.Active = info.Active; row.UpdatedAt = info.UpdatedAt; row.Rev = info.Rev; if (info.Rev > st.MaxRev) st.MaxRev = info.Rev;
                        }
                        return info;
                    }
                    return null;
                }
                catch (OperationCanceledException oce)
                {
                    Debug.WriteLine($"[PhraseService][Toggle][CANCEL] attempt={attempts} id={phraseId} {oce.Message}");
                    if (attempts < 4) { await Task.Delay(150 * attempts); continue; }
                    return null;
                }
                catch (Exception ex) when (IsTransient(ex) && attempts < 4)
                {
                    Debug.WriteLine($"[PhraseService][RETRY] Toggle attempt={attempts} id={phraseId} transient: {ex.Message}");
                    if (con != null) try { NpgsqlConnection.ClearPool(con); } catch { }
                    await Task.Delay(150 * attempts).ConfigureAwait(false); continue;
                }
            }
        }

        public async Task<long?> GetAnyAccountIdAsync()
        {
            return await GetAnyAccountIdInternalAsync(null).ConfigureAwait(false);
        }

        private async Task<long?> GetAnyAccountIdInternalAsync(long? cachedAccount)
        {
            await EnsureBackendAsync().ConfigureAwait(false);
            if (!_radiumAvailable) return cachedAccount; // fallback to provided cached value
            const string sql = "SELECT account_id FROM radium.phrase ORDER BY account_id LIMIT 1";
            await using var con = CreateConnection();
            try { await PgConnectionHelper.OpenWithLocalSslFallbackAsync(con).ConfigureAwait(false); }
            catch (OperationCanceledException oce) { Debug.WriteLine("[PhraseService][AnyAccount][CANCEL-Open] " + oce.Message); return cachedAccount; }
            await using var cmd = new NpgsqlCommand(sql, con);
            try
            {
                var result = await cmd.ExecuteScalarAsync().ConfigureAwait(false);
                return result == null ? cachedAccount : (long?)(result is long l ? l : Convert.ToInt64(result));
            }
            catch (OperationCanceledException oce) { Debug.WriteLine("[PhraseService][AnyAccount][CANCEL-Exec] " + oce.Message); return cachedAccount; }
        }

        private static bool IsTransient(Exception ex)
        {
            if (ex is TimeoutException) return true;
            if (ex is IOException) return true;
            if (ex is SocketException) return true;
            if (ex is OperationCanceledException) return true; // treat as transient (UI should not break)
            if (ex is NpgsqlException npgEx)
            {
                if (npgEx.InnerException is TimeoutException) return true;
                if (npgEx.InnerException is SocketException) return true;
                if (npgEx.InnerException is OperationCanceledException) return true;
                if (npgEx.Message.IndexOf("reading from stream", StringComparison.OrdinalIgnoreCase) >= 0) return true;
                if (npgEx.Message.IndexOf("Timeout", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            }
            return false;
        }
    }
}
