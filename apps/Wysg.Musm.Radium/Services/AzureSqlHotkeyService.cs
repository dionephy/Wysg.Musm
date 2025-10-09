using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using System.Threading;

namespace Wysg.Musm.Radium.Services
{
    /// <summary>
    /// Azure SQL implementation of hotkey service with snapshot-based caching.
    /// Follows synchronous pattern: DB update -> snapshot update -> UI reflects snapshot.
    /// Per-account locking prevents concurrent modification races.
    /// </summary>
    public sealed class AzureSqlHotkeyService : IHotkeyService
    {
        private readonly IRadiumLocalSettings _settings;
        
        // Per-account snapshots: accountId -> (hotkeyId -> HotkeyInfo)
        private readonly Dictionary<long, Dictionary<long, HotkeyInfo>> _snapshots = new();
        
        // Per-account locks to prevent concurrent updates
        private readonly Dictionary<long, SemaphoreSlim> _accountLocks = new();
        
        public AzureSqlHotkeyService(IRadiumLocalSettings settings)
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
            try
            {
                await LoadSnapshotInternalAsync(accountId).ConfigureAwait(false);
            }
            finally
            {
                sem.Release();
            }
        }

        public async Task<IReadOnlyList<HotkeyInfo>> GetAllHotkeyMetaAsync(long accountId)
        {
            if (accountId <= 0) return Array.Empty<HotkeyInfo>();
            
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
            finally
            {
                sem.Release();
            }
        }

        public async Task<IReadOnlyDictionary<string, string>> GetActiveHotkeysAsync(long accountId)
        {
            if (accountId <= 0) return new Dictionary<string, string>();
            
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
                    .Where(h => h.IsActive)
                    .ToDictionary(h => h.TriggerText, h => h.ExpansionText, StringComparer.OrdinalIgnoreCase);
            }
            finally
            {
                sem.Release();
            }
        }

        public async Task<HotkeyInfo> UpsertHotkeyAsync(long accountId, string triggerText, string expansionText, bool isActive = true)
        {
            if (accountId <= 0) throw new ArgumentException("Account ID must be positive", nameof(accountId));
            if (string.IsNullOrWhiteSpace(triggerText)) throw new ArgumentException("Trigger text cannot be blank", nameof(triggerText));
            if (string.IsNullOrWhiteSpace(expansionText)) throw new ArgumentException("Expansion text cannot be blank", nameof(expansionText));
            
            triggerText = triggerText.Trim();
            expansionText = expansionText.Trim();
            
            var sem = GetAccountLock(accountId);
            await sem.WaitAsync().ConfigureAwait(false);
            try
            {
                Debug.WriteLine($"[HotkeyService] Upserting hotkey for account={accountId}, trigger='{triggerText}'");
                
                using var conn = CreateConnection();
                await conn.OpenAsync().ConfigureAwait(false);

                // First try UPDATE
                var updateSql = @"UPDATE radium.hotkey
                                   SET expansion_text = @expansionText,
                                       is_active = @isActive
                                   WHERE account_id = @accountId AND trigger_text = @triggerText;";
                using (var upd = new SqlCommand(updateSql, conn) { CommandTimeout = 30 })
                {
                    upd.Parameters.AddWithValue("@expansionText", expansionText);
                    upd.Parameters.AddWithValue("@isActive", isActive);
                    upd.Parameters.AddWithValue("@accountId", accountId);
                    upd.Parameters.AddWithValue("@triggerText", triggerText);
                    var rows = await upd.ExecuteNonQueryAsync().ConfigureAwait(false);
                    if (rows == 0)
                    {
                        // No row -> try INSERT (handle unique violation race)
                        var insertSql = @"INSERT INTO radium.hotkey(account_id, trigger_text, expansion_text, is_active)
                                           VALUES (@accountId, @triggerText, @expansionText, @isActive);";
                        try
                        {
                            using var ins = new SqlCommand(insertSql, conn) { CommandTimeout = 30 };
                            ins.Parameters.AddWithValue("@accountId", accountId);
                            ins.Parameters.AddWithValue("@triggerText", triggerText);
                            ins.Parameters.AddWithValue("@expansionText", expansionText);
                            ins.Parameters.AddWithValue("@isActive", isActive);
                            await ins.ExecuteNonQueryAsync().ConfigureAwait(false);
                        }
                        catch (SqlException sx) when (sx.Number == 2627)
                        {
                            // Unique constraint hit -> another writer inserted; fall back to update
                            using var upd2 = new SqlCommand(updateSql, conn) { CommandTimeout = 30 };
                            upd2.Parameters.AddWithValue("@expansionText", expansionText);
                            upd2.Parameters.AddWithValue("@isActive", isActive);
                            upd2.Parameters.AddWithValue("@accountId", accountId);
                            upd2.Parameters.AddWithValue("@triggerText", triggerText);
                            _ = await upd2.ExecuteNonQueryAsync().ConfigureAwait(false);
                        }
                    }
                }

                // Final: SELECT row (let trigger update updated_at & rev)
                var selectSql = @"SELECT hotkey_id, account_id, trigger_text, expansion_text, is_active, updated_at, rev
                                   FROM radium.hotkey
                                   WHERE account_id = @accountId AND trigger_text = @triggerText;";
                using var sel = new SqlCommand(selectSql, conn) { CommandTimeout = 30 };
                sel.Parameters.AddWithValue("@accountId", accountId);
                sel.Parameters.AddWithValue("@triggerText", triggerText);

                HotkeyInfo? result = null;
                using var rd = await sel.ExecuteReaderAsync().ConfigureAwait(false);
                if (await rd.ReadAsync().ConfigureAwait(false))
                {
                    result = new HotkeyInfo(
                        HotkeyId: rd.GetInt64(0),
                        AccountId: rd.GetInt64(1),
                        TriggerText: rd.GetString(2),
                        ExpansionText: rd.GetString(3),
                        IsActive: rd.GetBoolean(4),
                        UpdatedAt: rd.GetDateTime(5),
                        Rev: rd.GetInt64(6)
                    );
                }

                if (result == null) throw new InvalidOperationException("Upsert returned no data after SELECT");
                
                // Update snapshot
                if (!_snapshots.TryGetValue(accountId, out var snap))
                {
                    snap = new Dictionary<long, HotkeyInfo>();
                    _snapshots[accountId] = snap;
                }
                snap[result.HotkeyId] = result;
                
                Debug.WriteLine($"[HotkeyService] Upsert complete: hotkeyId={result.HotkeyId}, rev={result.Rev}");
                return result;
            }
            finally
            {
                sem.Release();
            }
        }

        public async Task<HotkeyInfo?> ToggleActiveAsync(long accountId, long hotkeyId)
        {
            if (accountId <= 0) return null;
            if (hotkeyId <= 0) return null;
            
            var sem = GetAccountLock(accountId);
            await sem.WaitAsync().ConfigureAwait(false);
            try
            {
                Debug.WriteLine($"[HotkeyService] Toggling hotkey id={hotkeyId} for account={accountId}");
                
                using var conn = CreateConnection();
                await conn.OpenAsync().ConfigureAwait(false);
                
                // Update (let trigger adjust updated_at & rev)
                var sql = @"UPDATE radium.hotkey 
                             SET is_active = CASE WHEN is_active = 1 THEN 0 ELSE 1 END
                             WHERE hotkey_id = @hotkeyId AND account_id = @accountId;";
                
                using (var cmd = new SqlCommand(sql, conn) { CommandTimeout = 30 })
                {
                    cmd.Parameters.AddWithValue("@hotkeyId", hotkeyId);
                    cmd.Parameters.AddWithValue("@accountId", accountId);
                    var rows = await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                    if (rows == 0) return null;
                }

                // Select updated row
                var selectSql = @"SELECT hotkey_id, account_id, trigger_text, expansion_text, is_active, updated_at, rev
                                   FROM radium.hotkey
                                   WHERE hotkey_id = @hotkeyId AND account_id = @accountId;";
                using var sel = new SqlCommand(selectSql, conn) { CommandTimeout = 30 };
                sel.Parameters.AddWithValue("@hotkeyId", hotkeyId);
                sel.Parameters.AddWithValue("@accountId", accountId);

                HotkeyInfo? result = null;
                using var reader = await sel.ExecuteReaderAsync().ConfigureAwait(false);
                if (await reader.ReadAsync().ConfigureAwait(false))
                {
                    result = new HotkeyInfo(
                        HotkeyId: reader.GetInt64(0),
                        AccountId: reader.GetInt64(1),
                        TriggerText: reader.GetString(2),
                        ExpansionText: reader.GetString(3),
                        IsActive: reader.GetBoolean(4),
                        UpdatedAt: reader.GetDateTime(5),
                        Rev: reader.GetInt64(6)
                    );
                }
                
                if (result == null)
                {
                    Debug.WriteLine($"[HotkeyService] Toggle failed post-select: hotkey not found id={hotkeyId}");
                    return null;
                }
                
                // Update snapshot
                if (!_snapshots.TryGetValue(accountId, out var snap))
                {
                    snap = new Dictionary<long, HotkeyInfo>();
                    _snapshots[accountId] = snap;
                }
                snap[result.HotkeyId] = result;
                
                Debug.WriteLine($"[HotkeyService] Toggle complete: hotkeyId={result.HotkeyId}, active={result.IsActive}, rev={result.Rev}");
                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[HotkeyService] Toggle exception: {ex.Message}");
                return null;
            }
            finally
            {
                sem.Release();
            }
        }

        public async Task<bool> DeleteHotkeyAsync(long accountId, long hotkeyId)
        {
            if (accountId <= 0) return false;
            if (hotkeyId <= 0) return false;
            
            var sem = GetAccountLock(accountId);
            await sem.WaitAsync().ConfigureAwait(false);
            try
            {
                Debug.WriteLine($"[HotkeyService] Deleting hotkey id={hotkeyId} for account={accountId}");
                
                using var conn = CreateConnection();
                await conn.OpenAsync().ConfigureAwait(false);
                
                var sql = "DELETE FROM radium.hotkey WHERE hotkey_id = @hotkeyId AND account_id = @accountId;";
                using var cmd = new SqlCommand(sql, conn) { CommandTimeout = 30 };
                cmd.Parameters.AddWithValue("@hotkeyId", hotkeyId);
                cmd.Parameters.AddWithValue("@accountId", accountId);
                
                var rows = await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                
                if (rows > 0)
                {
                    Debug.WriteLine($"[HotkeyService] Delete complete: hotkeyId={hotkeyId}");
                    
                    // Update snapshot
                    if (_snapshots.TryGetValue(accountId, out var snap))
                    {
                        snap.Remove(hotkeyId);
                        Debug.WriteLine($"[HotkeyService] Snapshot updated for account={accountId}");
                    }
                    
                    return true;
                }
                
                Debug.WriteLine($"[HotkeyService] Delete failed: hotkey not found id={hotkeyId}");
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[HotkeyService] Delete exception: {ex.Message}");
                return false;
            }
            finally
            {
                sem.Release();
            }
        }

        public async Task RefreshHotkeysAsync(long accountId)
        {
            if (accountId <= 0) return;
            
            var sem = GetAccountLock(accountId);
            await sem.WaitAsync().ConfigureAwait(false);
            try
            {
                await LoadSnapshotInternalAsync(accountId).ConfigureAwait(false);
            }
            finally
            {
                sem.Release();
            }
        }

        private async Task LoadSnapshotInternalAsync(long accountId)
        {
            Debug.WriteLine($"[HotkeyService] Loading snapshot for account={accountId}");
            
            using var conn = CreateConnection();
            await conn.OpenAsync().ConfigureAwait(false);
            
            var sql = @"SELECT hotkey_id, account_id, trigger_text, expansion_text, is_active, updated_at, rev
                        FROM radium.hotkey
                        WHERE account_id = @accountId
                        ORDER BY updated_at DESC;";
            
            using var cmd = new SqlCommand(sql, conn) { CommandTimeout = 30 };
            cmd.Parameters.AddWithValue("@accountId", accountId);
            
            var snap = new Dictionary<long, HotkeyInfo>();
            using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                var info = new HotkeyInfo(
                    HotkeyId: reader.GetInt64(0),
                    AccountId: reader.GetInt64(1),
                    TriggerText: reader.GetString(2),
                    ExpansionText: reader.GetString(3),
                    IsActive: reader.GetBoolean(4),
                    UpdatedAt: reader.GetDateTime(5),
                    Rev: reader.GetInt64(6)
                );
                snap[info.HotkeyId] = info;
            }
            
            _snapshots[accountId] = snap;
            Debug.WriteLine($"[HotkeyService] Loaded {snap.Count} hotkeys for account={accountId}");
        }
    }
}
