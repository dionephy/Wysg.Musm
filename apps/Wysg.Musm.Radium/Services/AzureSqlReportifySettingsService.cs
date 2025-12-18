using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace Wysg.Musm.Radium.Services
{
    /// <summary>
    /// Azure SQL implementation of reportify settings persistence.
    /// </summary>
    public sealed class AzureSqlReportifySettingsService : IReportifySettingsService
    {
        private readonly IRadiumLocalSettings _settings;
        public AzureSqlReportifySettingsService(IRadiumLocalSettings settings) { _settings = settings; }
        private string Cs => _settings.CentralConnectionString ?? throw new InvalidOperationException("Central connection string missing");
        private SqlConnection CreateConnection() => new SqlConnection(Cs);

        public async Task<string?> GetSettingsJsonAsync(long accountId)
        {
            if (accountId <= 0) return null;
            const string sql = "SELECT settings_json FROM radium.user_setting WHERE account_id=@id";
            await using var con = CreateConnection();
            try
            {
                await con.OpenAsync();
                await using var cmd = new SqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@id", accountId);
                var obj = await cmd.ExecuteScalarAsync();
                return obj as string;
            }
            catch (Exception ex) { Debug.WriteLine("[AzureSqlReportify] Get error: " + ex.Message); return null; }
        }

        public async Task<(string settingsJson,long rev)> UpsertAsync(long accountId, string settingsJson)
        {
            if (accountId <= 0) return (settingsJson, 0);
            const string up = @"UPDATE radium.user_setting
SET settings_json=@json, updated_at=SYSUTCDATETIME(), rev = CASE WHEN settings_json=@json THEN rev ELSE rev+1 END
WHERE account_id=@id";
            const string ins = @"INSERT INTO radium.user_setting(account_id, settings_json, updated_at, rev)
VALUES(@id,@json,SYSUTCDATETIME(),1)";
            const string sel = @"SELECT settings_json, rev FROM radium.user_setting WHERE account_id=@id";
            await using var con = CreateConnection();
            await con.OpenAsync();
            await using (var cmd = new SqlCommand(up, con))
            {
                cmd.Parameters.AddWithValue("@json", settingsJson ?? "{}");
                cmd.Parameters.AddWithValue("@id", accountId);
                var rows = await cmd.ExecuteNonQueryAsync();
                if (rows == 0)
                {
                    try
                    {
                        await using var icmd = new SqlCommand(ins, con);
                        icmd.Parameters.AddWithValue("@id", accountId);
                        icmd.Parameters.AddWithValue("@json", settingsJson ?? "{}");
                        await icmd.ExecuteNonQueryAsync();
                    }
                    catch (SqlException sx) when (sx.Number == 2627) { /* concurrent insert */ }
                }
            }
            await using (var s = new SqlCommand(sel, con))
            {
                s.Parameters.AddWithValue("@id", accountId);
                await using var rd = await s.ExecuteReaderAsync();
                if (await rd.ReadAsync()) return (rd.GetString(0), rd.GetInt64(1));
            }
            return (settingsJson, 1);
        }

        public async Task<bool> DeleteAsync(long accountId)
        {
            if (accountId <= 0) return false;
            const string sql = "DELETE FROM radium.user_setting WHERE account_id=@id";
            await using var con = CreateConnection();
            try
            {
                await con.OpenAsync();
                await using var cmd = new SqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@id", accountId);
                return await cmd.ExecuteNonQueryAsync() > 0;
            }
            catch (Exception ex) { Debug.WriteLine("[AzureSqlReportify] Delete error: " + ex.Message); return false; }
        }
    }
}
