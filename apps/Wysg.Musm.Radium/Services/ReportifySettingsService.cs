using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Npgsql;
using System.Net.Sockets;

namespace Wysg.Musm.Radium.Services
{
    public sealed class ReportifySettingsService : IReportifySettingsService
    {
        private readonly ICentralDataSourceProvider _ds;
        private readonly IRadiumLocalSettings _local;
        public ReportifySettingsService(ICentralDataSourceProvider ds, IRadiumLocalSettings local)
        { _ds = ds; _local = local; }

        private NpgsqlConnection CreateConnection() => _ds.Central.CreateConnection();

        private static bool IsTransient(Exception ex)
        {
            if (ex is TimeoutException) return true;
            if (ex is SocketException) return true;
            if (ex is OperationCanceledException) return true;
            if (ex is NpgsqlException npg)
            {
                if (npg.InnerException != null && IsTransient(npg.InnerException)) return true;
                if (npg.Message.IndexOf("Timeout", StringComparison.OrdinalIgnoreCase) >= 0) return true;
                if (npg.Message.IndexOf("reading from stream", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            }
            return false;
        }

        public async Task<string?> GetSettingsJsonAsync(long accountId)
        {
            const string sql = "SELECT settings_json::text FROM radium.reportify_setting WHERE account_id=@aid";
            int attempts = 0;
            while (true)
            {
                attempts++;
                await using var con = CreateConnection();
                try
                {
                    await PgConnectionHelper.OpenWithLocalSslFallbackAsync(con);
                }
                catch (Exception ex) when (IsTransient(ex) && attempts < 2)
                {
                    Debug.WriteLine($"[ReportifySettings][Get][RETRY-OPEN {attempts}] transient: {ex.Message}");
                    try { NpgsqlConnection.ClearPool(con); } catch { }
                    await Task.Delay(120 * attempts);
                    continue;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("[ReportifySettings][Get][OPEN-FAIL] " + ex.Message);
                    return null;
                }

                await using var cmd = new NpgsqlCommand(sql, con) { CommandTimeout = 12 };
                cmd.Parameters.AddWithValue("aid", accountId);
                try
                {
                    using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(8));
                    var obj = await cmd.ExecuteScalarAsync(cts.Token);
                    return obj as string ?? obj?.ToString();
                }
                catch (OperationCanceledException oce) when (attempts < 2)
                {
                    Debug.WriteLine($"[ReportifySettings][Get][CANCEL-RETRY {attempts}] {oce.Message}");
                    try { NpgsqlConnection.ClearPool(con); } catch { }
                    await Task.Delay(120 * attempts);
                    continue;
                }
                catch (Exception ex) when (IsTransient(ex) && attempts < 2)
                {
                    Debug.WriteLine($"[ReportifySettings][Get][RETRY {attempts}] transient exec: {ex.Message}");
                    try { NpgsqlConnection.ClearPool(con); } catch { }
                    await Task.Delay(120 * attempts);
                    continue;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("[ReportifySettings] Get error: " + ex.Message);
                    return null;
                }
            }
        }

        public async Task<(string settingsJson,long rev)> UpsertAsync(long accountId, string settingsJson)
        {
            const string sql = @"INSERT INTO radium.reportify_setting(account_id, settings_json)
VALUES(@aid, CAST(@json AS jsonb))
ON CONFLICT (account_id) DO UPDATE SET settings_json = EXCLUDED.settings_json, updated_at = now(), rev = radium.reportify_setting.rev + 1
RETURNING settings_json::text, rev";
            int attempts = 0;
            while (true)
            {
                attempts++;
                await using var con = CreateConnection();
                try
                {
                    await PgConnectionHelper.OpenWithLocalSslFallbackAsync(con);
                }
                catch (Exception ex) when (IsTransient(ex) && attempts < 3)
                {
                    Debug.WriteLine($"[ReportifySettings][Upsert][RETRY-OPEN {attempts}] transient: {ex.Message}");
                    try { NpgsqlConnection.ClearPool(con); } catch { }
                    await Task.Delay(150 * attempts);
                    continue;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("[ReportifySettings][Upsert][OPEN-FAIL] " + ex.Message);
                    return (settingsJson, 0);
                }

                await using var cmd = new NpgsqlCommand(sql, con) { CommandTimeout = 20 };
                cmd.Parameters.AddWithValue("aid", accountId);
                cmd.Parameters.AddWithValue("json", settingsJson ?? "{}");
                try
                {
                    using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(10));
                    await using var rd = await cmd.ExecuteReaderAsync(cts.Token);
                    if (await rd.ReadAsync(cts.Token))
                    {
                        var json = rd.GetString(0); var rev = rd.GetInt64(1);
                        return (json, rev);
                    }
                    return (settingsJson, 0);
                }
                catch (OperationCanceledException oce) when (attempts < 3)
                {
                    Debug.WriteLine($"[ReportifySettings][Upsert][CANCEL-RETRY {attempts}] {oce.Message}");
                    try { NpgsqlConnection.ClearPool(con); } catch { }
                    await Task.Delay(150 * attempts);
                    continue;
                }
                catch (Exception ex) when (IsTransient(ex) && attempts < 3)
                {
                    Debug.WriteLine($"[ReportifySettings][Upsert][RETRY {attempts}] transient: {ex.Message}");
                    try { NpgsqlConnection.ClearPool(con); } catch { }
                    await Task.Delay(150 * attempts);
                    continue;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("[ReportifySettings] Upsert error: " + ex.Message);
                    return (settingsJson, 0);
                }
            }
        }

        public async Task<bool> DeleteAsync(long accountId)
        {
            const string sql = "DELETE FROM radium.reportify_setting WHERE account_id=@aid";
            await using var con = CreateConnection();
            try { await PgConnectionHelper.OpenWithLocalSslFallbackAsync(con); } catch { return false; }
            await using var cmd = new NpgsqlCommand(sql, con) { CommandTimeout = 10 };
            cmd.Parameters.AddWithValue("aid", accountId);
            try { return await cmd.ExecuteNonQueryAsync() > 0; }
            catch (Exception ex) { Debug.WriteLine("[ReportifySettings] Delete error: " + ex.Message); return false; }
        }
    }
}
