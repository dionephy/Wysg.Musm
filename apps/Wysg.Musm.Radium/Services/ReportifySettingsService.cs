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

        private NpgsqlConnection CreateConnection(bool meta = false) => meta ? _ds.CentralMeta.CreateConnection() : _ds.Central.CreateConnection();

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

        private static bool IsReadTimeout(Exception ex)
        {
            if (ex is TimeoutException) return true;
            if (ex is NpgsqlException npg && (npg.Message.IndexOf("Timeout", StringComparison.OrdinalIgnoreCase) >= 0 || npg.Message.IndexOf("reading from stream", StringComparison.OrdinalIgnoreCase) >= 0)) return true;
            return false;
        }

        public async Task<string?> GetSettingsJsonAsync(long accountId)
        {
            // Strategy v3:
            //  1) Ultra-fast meta attempt (CentralMeta pool) with 3s command timeout + server-side statement_timeout (SET LOCAL 3000).
            //     If it times out while reading, we assume cold backend; skip waiting longer and retry quickly.
            //  2) Single primary attempt (12s command timeout) with server-side limit (SET LOCAL 8000) but soft local stopwatch cap (fail fast >9s).
            //  No further retries. Total worst-case wait ~ (3s + 2s backoff + 9s) ~= 14s; typical success < 300ms once warm.
            const string sql = "SELECT settings_json::text FROM radium.reportify_setting WHERE account_id=@aid";

            async Task<string?> AttemptAsync(bool meta, int cmdTimeoutSeconds, int serverStmtMs, int attempt)
            {
                await using var con = CreateConnection(meta);
                var sw = Stopwatch.StartNew();
                await PgConnectionHelper.OpenWithLocalSslFallbackAsync(con);
                var openMs = sw.ElapsedMilliseconds;
                // Apply server-side statement timeout to ensure backend cancels if stuck
                try
                {
                    await using var st = new NpgsqlCommand($"SET LOCAL statement_timeout TO {serverStmtMs}", con) { CommandTimeout = 2 };
                    await st.ExecuteNonQueryAsync();
                }
                catch (Exception ex) { Debug.WriteLine($"[ReportifySettings][Get][Attempt{attempt}] SET LOCAL warn: {ex.Message}"); }
                await using var cmd = new NpgsqlCommand(sql, con) { CommandTimeout = cmdTimeoutSeconds };
                cmd.Parameters.AddWithValue("aid", accountId);
                sw.Restart();
                var obj = await cmd.ExecuteScalarAsync();
                var execMs = sw.ElapsedMilliseconds;
                Debug.WriteLine($"[ReportifySettings][Get] attempt={attempt} meta={meta} openMs={openMs} execMs={execMs} totalMs={openMs + execMs}");
                return obj as string ?? obj?.ToString();
            }

            // Attempt 1 (meta)
            try
            {
                return await AttemptAsync(meta: true, cmdTimeoutSeconds: 3, serverStmtMs: 3000, attempt: 1);
            }
            catch (Exception ex) when (IsReadTimeout(ex) || IsTransient(ex))
            {
                Debug.WriteLine($"[ReportifySettings][Get][Attempt1-FAIL] {ex.GetType().Name}: {ex.Message} -> quick retry on primary");
                await Task.Delay(500); // short backoff instead of long stall
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ReportifySettings][Get][Attempt1-ABORT] {ex.GetType().Name}: {ex.Message}");
                return null;
            }

            // Attempt 2 (primary). Soft cap 9s even though cmd timeout is 12s to avoid long splash block.
            var softCap = TimeSpan.FromSeconds(9);
            var swAll = Stopwatch.StartNew();
            try
            {
                var result = await AttemptAsync(meta: false, cmdTimeoutSeconds: 12, serverStmtMs: 8000, attempt: 2);
                return result;
            }
            catch (Exception ex) when (IsReadTimeout(ex) || IsTransient(ex))
            {
                Debug.WriteLine($"[ReportifySettings][Get][Attempt2-FAIL] {ex.GetType().Name}: {ex.Message} softElapsed={swAll.ElapsedMilliseconds}ms");
                return null; // give up fast for startup; user can manually refresh later
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ReportifySettings][Get][Attempt2-ABORT] {ex.GetType().Name}: {ex.Message}");
                return null;
            }
            finally
            {
                if (swAll.Elapsed > softCap)
                    Debug.WriteLine($"[ReportifySettings][Get][WARN] Attempt2 exceeded soft cap {softCap.TotalMilliseconds}ms (elapsed={swAll.ElapsedMilliseconds})");
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
