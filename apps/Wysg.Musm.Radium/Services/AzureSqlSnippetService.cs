using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace Wysg.Musm.Radium.Services
{
    /// <summary>
    /// Azure SQL implementation of snippet service with snapshot-based caching.
    /// Synchronous flow: DB update -> snapshot update -> UI displays snapshot.
    /// </summary>
    public sealed class AzureSqlSnippetService : ISnippetService
    {
        private readonly IRadiumLocalSettings _settings;

        // Per-account snapshots: accountId -> (snippetId -> SnippetInfo)
        private readonly Dictionary<long, Dictionary<long, SnippetInfo>> _snapshots = new();

        // Per-account locks to prevent concurrent modifications
        private readonly Dictionary<long, SemaphoreSlim> _accountLocks = new();

        public AzureSqlSnippetService(IRadiumLocalSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        private string ConnectionString => _settings.CentralConnectionString ?? throw new InvalidOperationException("Central connection string not configured");
        private SqlConnection CreateConnection() => new SqlConnection(ConnectionString);

        private SemaphoreSlim GetAccountLock(long accountId)
        {
            lock (_accountLocks)
            {
                if (!_accountLocks.TryGetValue(accountId, out var sem))
                {
                    sem = new SemaphoreSlim(1, 1);
                    _accountLocks[accountId] = sem;
                }
                return sem;
            }
        }

        public async Task PreloadAsync(long accountId)
        {
            if (accountId <= 0) return;
            var sem = GetAccountLock(accountId);
            await sem.WaitAsync().ConfigureAwait(false);
            try { await LoadSnapshotInternalAsync(accountId).ConfigureAwait(false); }
            finally { sem.Release(); }
        }

        public async Task<IReadOnlyList<SnippetInfo>> GetAllSnippetMetaAsync(long accountId)
        {
            if (accountId <= 0) return Array.Empty<SnippetInfo>();
            var sem = GetAccountLock(accountId);
            await sem.WaitAsync().ConfigureAwait(false);
            try
            {
                if (!_snapshots.TryGetValue(accountId, out var snap))
                {
                    await LoadSnapshotInternalAsync(accountId).ConfigureAwait(false);
                    snap = _snapshots[accountId];
                }
                return snap.Values.OrderByDescending(h => h.UpdatedAt).ToList();
            }
            finally { sem.Release(); }
        }

        public async Task<IReadOnlyDictionary<string, (string text, string ast, string description)>> GetActiveSnippetsAsync(long accountId)
        {
            if (accountId <= 0) return new Dictionary<string, (string, string, string)>();
            var sem = GetAccountLock(accountId);
            await sem.WaitAsync().ConfigureAwait(false);
            try
            {
                if (!_snapshots.TryGetValue(accountId, out var snap))
                {
                    await LoadSnapshotInternalAsync(accountId).ConfigureAwait(false);
                    snap = _snapshots[accountId];
                }
                return snap.Values
                    .Where(s => s.IsActive)
                    .ToDictionary(s => s.TriggerText, s => (s.SnippetText, s.SnippetAst, s.Description ?? string.Empty), StringComparer.OrdinalIgnoreCase);
            }
            finally { sem.Release(); }
        }

        public async Task<SnippetInfo> UpsertSnippetAsync(long accountId, string triggerText, string snippetText, string snippetAst, bool isActive = true, string? description = null)
        {
            if (accountId <= 0) throw new ArgumentException("Account ID must be positive", nameof(accountId));
            if (string.IsNullOrWhiteSpace(triggerText)) throw new ArgumentException("Trigger text cannot be blank", nameof(triggerText));
            if (string.IsNullOrWhiteSpace(snippetText)) throw new ArgumentException("Snippet text cannot be blank", nameof(snippetText));
            if (string.IsNullOrWhiteSpace(snippetAst)) throw new ArgumentException("Snippet AST cannot be blank", nameof(snippetAst));
            triggerText = triggerText.Trim(); snippetText = snippetText.Trim(); snippetAst = snippetAst.Trim();
            description = (description ?? string.Empty).Trim();

            var sem = GetAccountLock(accountId);
            await sem.WaitAsync().ConfigureAwait(false);
            try
            {
                using var conn = CreateConnection();
                await conn.OpenAsync().ConfigureAwait(false);

                var updateSql = @"UPDATE radium.snippet
                                   SET snippet_text = @snippetText,
                                       snippet_ast = @snippetAst,
                                       is_active = @isActive,
                                       description = @description
                                   WHERE account_id = @accountId AND trigger_text = @triggerText;";
                using (var upd = new SqlCommand(updateSql, conn) { CommandTimeout = 30 })
                {
                    upd.Parameters.AddWithValue("@snippetText", snippetText);
                    upd.Parameters.AddWithValue("@snippetAst", snippetAst);
                    upd.Parameters.AddWithValue("@isActive", isActive);
                    upd.Parameters.AddWithValue("@description", (object)description ?? DBNull.Value);
                    upd.Parameters.AddWithValue("@accountId", accountId);
                    upd.Parameters.AddWithValue("@triggerText", triggerText);
                    var rows = await upd.ExecuteNonQueryAsync().ConfigureAwait(false);
                    if (rows == 0)
                    {
                        var insertSql = @"INSERT INTO radium.snippet(account_id, trigger_text, snippet_text, snippet_ast, description, is_active)
                                           VALUES (@accountId, @triggerText, @snippetText, @snippetAst, @description, @isActive);";
                        try
                        {
                            using var ins = new SqlCommand(insertSql, conn) { CommandTimeout = 30 };
                            ins.Parameters.AddWithValue("@accountId", accountId);
                            ins.Parameters.AddWithValue("@triggerText", triggerText);
                            ins.Parameters.AddWithValue("@snippetText", snippetText);
                            ins.Parameters.AddWithValue("@snippetAst", snippetAst);
                            ins.Parameters.AddWithValue("@description", (object)description ?? DBNull.Value);
                            ins.Parameters.AddWithValue("@isActive", isActive);
                            await ins.ExecuteNonQueryAsync().ConfigureAwait(false);
                        }
                        catch (SqlException sx) when (sx.Number == 2627)
                        {
                            using var upd2 = new SqlCommand(updateSql, conn) { CommandTimeout = 30 };
                            upd2.Parameters.AddWithValue("@snippetText", snippetText);
                            upd2.Parameters.AddWithValue("@snippetAst", snippetAst);
                            upd2.Parameters.AddWithValue("@isActive", isActive);
                            upd2.Parameters.AddWithValue("@description", (object)description ?? DBNull.Value);
                            upd2.Parameters.AddWithValue("@accountId", accountId);
                            upd2.Parameters.AddWithValue("@triggerText", triggerText);
                            _ = await upd2.ExecuteNonQueryAsync().ConfigureAwait(false);
                        }
                    }
                }

                var selectSql = @"SELECT snippet_id, account_id, trigger_text, snippet_text, snippet_ast, ISNULL(description, N'') as description, is_active, updated_at, rev
                                   FROM radium.snippet
                                   WHERE account_id = @accountId AND trigger_text = @triggerText;";
                using var sel = new SqlCommand(selectSql, conn) { CommandTimeout = 30 };
                sel.Parameters.AddWithValue("@accountId", accountId);
                sel.Parameters.AddWithValue("@triggerText", triggerText);

                SnippetInfo? result = null;
                using var rd = await sel.ExecuteReaderAsync().ConfigureAwait(false);
                if (await rd.ReadAsync().ConfigureAwait(false))
                {
                    result = new SnippetInfo(
                        SnippetId: rd.GetInt64(0),
                        AccountId: rd.GetInt64(1),
                        TriggerText: rd.GetString(2),
                        SnippetText: rd.GetString(3),
                        SnippetAst: rd.GetString(4),
                        Description: rd.GetString(5),
                        IsActive: rd.GetBoolean(6),
                        UpdatedAt: rd.GetDateTime(7),
                        Rev: rd.GetInt64(8)
                    );
                }
                if (result == null) throw new InvalidOperationException("Upsert returned no data after SELECT");

                if (!_snapshots.TryGetValue(accountId, out var snap)) { snap = new Dictionary<long, SnippetInfo>(); _snapshots[accountId] = snap; }
                snap[result.SnippetId] = result;
                return result;
            }
            finally { sem.Release(); }
        }

        public async Task<SnippetInfo?> ToggleActiveAsync(long accountId, long snippetId)
        {
            if (accountId <= 0) return null;
            if (snippetId <= 0) return null;

            var sem = GetAccountLock(accountId);
            await sem.WaitAsync().ConfigureAwait(false);
            try
            {
                using var conn = CreateConnection();
                await conn.OpenAsync().ConfigureAwait(false);

                var sql = @"UPDATE radium.snippet
                             SET is_active = CASE WHEN is_active = 1 THEN 0 ELSE 1 END
                             WHERE snippet_id = @snippetId AND account_id = @accountId;";
                using (var cmd = new SqlCommand(sql, conn) { CommandTimeout = 30 })
                {
                    cmd.Parameters.AddWithValue("@snippetId", snippetId);
                    cmd.Parameters.AddWithValue("@accountId", accountId);
                    var rows = await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                    if (rows == 0) return null;
                }

                var selectSql = @"SELECT snippet_id, account_id, trigger_text, snippet_text, snippet_ast, ISNULL(description, N'') as description, is_active, updated_at, rev
                                   FROM radium.snippet
                                   WHERE snippet_id = @snippetId AND account_id = @accountId;";
                using var sel = new SqlCommand(selectSql, conn) { CommandTimeout = 30 };
                sel.Parameters.AddWithValue("@snippetId", snippetId);
                sel.Parameters.AddWithValue("@accountId", accountId);

                SnippetInfo? result = null;
                using var reader = await sel.ExecuteReaderAsync().ConfigureAwait(false);
                if (await reader.ReadAsync().ConfigureAwait(false))
                {
                    result = new SnippetInfo(
                        SnippetId: reader.GetInt64(0),
                        AccountId: reader.GetInt64(1),
                        TriggerText: reader.GetString(2),
                        SnippetText: reader.GetString(3),
                        SnippetAst: reader.GetString(4),
                        Description: reader.GetString(5),
                        IsActive: reader.GetBoolean(6),
                        UpdatedAt: reader.GetDateTime(7),
                        Rev: reader.GetInt64(8)
                    );
                }
                if (result == null) return null;

                if (!_snapshots.TryGetValue(accountId, out var snap)) { snap = new Dictionary<long, SnippetInfo>(); _snapshots[accountId] = snap; }
                snap[result.SnippetId] = result;
                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SnippetService] Toggle exception: {ex.Message}");
                return null;
            }
            finally { sem.Release(); }
        }

        public async Task<bool> DeleteSnippetAsync(long accountId, long snippetId)
        {
            if (accountId <= 0) return false;
            if (snippetId <= 0) return false;

            var sem = GetAccountLock(accountId);
            await sem.WaitAsync().ConfigureAwait(false);
            try
            {
                using var conn = CreateConnection();
                await conn.OpenAsync().ConfigureAwait(false);

                var sql = "DELETE FROM radium.snippet WHERE snippet_id = @snippetId AND account_id = @accountId;";
                using var cmd = new SqlCommand(sql, conn) { CommandTimeout = 30 };
                cmd.Parameters.AddWithValue("@snippetId", snippetId);
                cmd.Parameters.AddWithValue("@accountId", accountId);
                var rows = await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);

                if (rows > 0)
                {
                    if (_snapshots.TryGetValue(accountId, out var snap)) snap.Remove(snippetId);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SnippetService] Delete exception: {ex.Message}");
                return false;
            }
            finally { sem.Release(); }
        }

        public async Task RefreshSnippetsAsync(long accountId)
        {
            if (accountId <= 0) return;
            var sem = GetAccountLock(accountId);
            await sem.WaitAsync().ConfigureAwait(false);
            try { await LoadSnapshotInternalAsync(accountId).ConfigureAwait(false); }
            finally { sem.Release(); }
        }

        private async Task LoadSnapshotInternalAsync(long accountId)
        {
            using var conn = CreateConnection();
            await conn.OpenAsync().ConfigureAwait(false);

            var sql = @"SELECT snippet_id, account_id, trigger_text, snippet_text, snippet_ast, ISNULL(description, N'') as description, is_active, updated_at, rev
                        FROM radium.snippet
                        WHERE account_id = @accountId
                        ORDER BY updated_at DESC;";
            using var cmd = new SqlCommand(sql, conn) { CommandTimeout = 30 };
            cmd.Parameters.AddWithValue("@accountId", accountId);

            var snap = new Dictionary<long, SnippetInfo>();
            using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                var info = new SnippetInfo(
                    SnippetId: reader.GetInt64(0),
                    AccountId: reader.GetInt64(1),
                    TriggerText: reader.GetString(2),
                    SnippetText: reader.GetString(3),
                    SnippetAst: reader.GetString(4),
                    Description: reader.GetString(5),
                    IsActive: reader.GetBoolean(6),
                    UpdatedAt: reader.GetDateTime(7),
                    Rev: reader.GetInt64(8)
                );
                snap[info.SnippetId] = info;
            }
            _snapshots[accountId] = snap;
        }
    }
}
