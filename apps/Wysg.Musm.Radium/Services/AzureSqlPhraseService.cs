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
    /// </summary>
    public sealed class AzureSqlPhraseService : IPhraseService
    {
        private readonly IRadiumLocalSettings _settings;
        private readonly IPhraseCache _cache;
        private readonly ConcurrentDictionary<long, AccountPhraseState> _states = new();

        private sealed class PhraseRow
        {
            public long Id { get; init; }
            public long AccountId { get; init; }
            public string Text { get; set; } = string.Empty;
            public bool Active { get; set; }
            public DateTime UpdatedAt { get; set; }
            public long Rev { get; set; }
        }
        private sealed class AccountPhraseState
        {
            public long AccountId { get; }
            public readonly SemaphoreSlim UpdateLock = new(1,1);
            public readonly Dictionary<long, PhraseRow> ById = new();
            public AccountPhraseState(long id) { AccountId = id; }
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

        public async Task<PhraseInfo> UpsertPhraseAsync(long accountId, string text, bool active = true)
        {
            if (accountId <= 0) throw new ArgumentOutOfRangeException(nameof(accountId));
            var state = _states.GetOrAdd(accountId, id => new AccountPhraseState(id));
            await state.UpdateLock.WaitAsync().ConfigureAwait(false);
            try
            {
                // Update-first then insert pattern + final SELECT
                const string updateSql = @"UPDATE radium.phrase SET active=@active WHERE account_id=@aid AND [text]=@text";
                const string insertSql = @"INSERT INTO radium.phrase(account_id,[text],active) VALUES(@aid,@text,@active)";
                const string selectSql = @"SELECT TOP (1) id, account_id, [text], active, updated_at, rev FROM radium.phrase WHERE account_id=@aid AND [text]=@text";
                await using var con = CreateConnection();
                await con.OpenAsync().ConfigureAwait(false);
                await using (var upd = new SqlCommand(updateSql, con))
                {
                    upd.Parameters.AddWithValue("@active", active);
                    upd.Parameters.AddWithValue("@aid", accountId);
                    upd.Parameters.AddWithValue("@text", text);
                    var rows = await upd.ExecuteNonQueryAsync().ConfigureAwait(false);
                    if (rows == 0)
                    {
                        await using var ins = new SqlCommand(insertSql, con);
                        ins.Parameters.AddWithValue("@aid", accountId);
                        ins.Parameters.AddWithValue("@text", text);
                        ins.Parameters.AddWithValue("@active", active);
                        try { await ins.ExecuteNonQueryAsync().ConfigureAwait(false); }
                        catch (SqlException sx) when (sx.Number == 2627) // unique violation -> concurrent insert, fallthrough to select
                        { }
                    }
                }
                await using (var sel = new SqlCommand(selectSql, con))
                {
                    sel.Parameters.AddWithValue("@aid", accountId);
                    sel.Parameters.AddWithValue("@text", text);
                    await using var rd = await sel.ExecuteReaderAsync().ConfigureAwait(false);
                    if (!await rd.ReadAsync().ConfigureAwait(false)) throw new InvalidOperationException("Phrase select failed");
                    var info = new PhraseInfo(rd.GetInt64(0), rd.GetInt64(1), rd.GetString(2), rd.GetBoolean(3), rd.GetDateTime(4), rd.GetInt64(5));
                    UpdateSnapshot(state, info);
                    _cache.Clear(accountId);
                    return info;
                }
            }
            finally { state.UpdateLock.Release(); }
        }

        public async Task<PhraseInfo?> ToggleActiveAsync(long accountId, long phraseId)
        {
            if (accountId <= 0) return null;
            var state = _states.GetOrAdd(accountId, id => new AccountPhraseState(id));
            await state.UpdateLock.WaitAsync().ConfigureAwait(false);
            try
            {
                const string toggleSql = @"UPDATE radium.phrase SET active = CASE active WHEN 1 THEN 0 ELSE 1 END WHERE account_id=@aid AND id=@pid;";
                const string sel = @"SELECT id, account_id, [text], active, updated_at, rev FROM radium.phrase WHERE id=@pid AND account_id=@aid";
                await using var con = CreateConnection();
                await con.OpenAsync().ConfigureAwait(false);
                await using (var cmd = new SqlCommand(toggleSql, con))
                {
                    cmd.Parameters.AddWithValue("@aid", accountId);
                    cmd.Parameters.AddWithValue("@pid", phraseId);
                    var rows = await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                    if (rows == 0) return null;
                }
                await using (var q = new SqlCommand(sel, con))
                {
                    q.Parameters.AddWithValue("@aid", accountId);
                    q.Parameters.AddWithValue("@pid", phraseId);
                    await using var rd = await q.ExecuteReaderAsync().ConfigureAwait(false);
                    if (!await rd.ReadAsync().ConfigureAwait(false)) return null;
                    var info = new PhraseInfo(rd.GetInt64(0), rd.GetInt64(1), rd.GetString(2), rd.GetBoolean(3), rd.GetDateTime(4), rd.GetInt64(5));
                    UpdateSnapshot(state, info);
                    _cache.Clear(accountId);
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

        public Task<long?> GetAnyAccountIdAsync() => Task.FromResult<long?>(null); // not required for Azure migration path
    }
}
