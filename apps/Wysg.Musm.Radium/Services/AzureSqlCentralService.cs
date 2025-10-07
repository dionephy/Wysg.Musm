using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace Wysg.Musm.Radium.Services
{
    /// <summary>
    /// Central account/settings service (Azure SQL only).
    /// Postgres (legacy Supabase) support removed.
    /// </summary>
    public sealed class AzureSqlCentralService
    {
        private readonly IRadiumLocalSettings _settings;
        public AzureSqlCentralService(IRadiumLocalSettings settings) { _settings = settings; }
        private string Cs => _settings.CentralConnectionString ?? throw new InvalidOperationException("Central connection string missing");
        private static bool LooksAzure(string cs) => cs.IndexOf("database.windows.net", StringComparison.OrdinalIgnoreCase) >= 0 || cs.IndexOf("Initial Catalog=", StringComparison.OrdinalIgnoreCase) >= 0;
        private void EnsureAzure() { if (!LooksAzure(Cs)) throw new InvalidOperationException("Central connection string is not an Azure SQL format (Postgres path removed)"); }

        // ---------- Public API (driver-agnostic) ----------
        public async Task<(bool ok, string message)> TestConnectionAsync()
        {
            try
            {
                EnsureAzure();
                await using var con = new SqlConnection(Cs);
                await con.OpenAsync();
                await using var cmd = new SqlCommand("SELECT @@VERSION", con);
                var ver = (string)(await cmd.ExecuteScalarAsync())!;
                return (true, ver);
            }
            catch (Exception ex) { return (false, ex.Message); }
        }

        public async Task<long> EnsureAccountAsync(string uid, string email, string displayName)
        {
            EnsureAzure();
            await using var con = new SqlConnection(Cs); await con.OpenAsync();
            const string selectUid = "SELECT account_id FROM app.account WHERE uid=@uid";
            await using (var sel = new SqlConnection(Cs)) { }
            await using (var sel = new SqlCommand(selectUid, con))
            {
                sel.Parameters.AddWithValue("@uid", uid);
                var existing = await sel.ExecuteScalarAsync();
                if (existing is long id)
                {
                    const string upd = "UPDATE app.account SET email=@em, display_name=@dn WHERE account_id=@id";
                    await using var up = new SqlCommand(upd, con);
                    up.Parameters.AddWithValue("@em", email);
                    up.Parameters.AddWithValue("@dn", (object)displayName ?? string.Empty);
                    up.Parameters.AddWithValue("@id", id);
                    await up.ExecuteNonQueryAsync();
                    return id;
                }
            }
            const string ins = "INSERT INTO app.account(uid,email,display_name,is_active,created_at) VALUES(@uid,@em,@dn,1,SYSUTCDATETIME());SELECT SCOPE_IDENTITY();";
            try
            {
                await using var cmd = new SqlCommand(ins, con);
                cmd.Parameters.AddWithValue("@uid", uid);
                cmd.Parameters.AddWithValue("@em", email);
                cmd.Parameters.AddWithValue("@dn", (object)displayName ?? string.Empty);
                var idObj = await cmd.ExecuteScalarAsync();
                return Convert.ToInt64(idObj);
            }
            catch (SqlException sx) when (sx.Number == 2627)
            {
                const string selByEmail = "SELECT account_id FROM app.account WHERE email=@em";
                await using var sel = new SqlCommand(selByEmail, con);
                sel.Parameters.AddWithValue("@em", email);
                var idObj = await sel.ExecuteScalarAsync();
                if (idObj is null) throw;
                long id = Convert.ToInt64(idObj);
                const string adopt = "UPDATE app.account SET uid=@uid, display_name=@dn WHERE account_id=@id";
                await using var up = new SqlCommand(adopt, con);
                up.Parameters.AddWithValue("@uid", uid);
                up.Parameters.AddWithValue("@dn", (object)displayName ?? string.Empty);
                up.Parameters.AddWithValue("@id", id);
                await up.ExecuteNonQueryAsync();
                return id;
            }
        }

        public Task UpdateLastLoginAsync(long accountId) => UpdateLastLoginAsync(accountId, silent: false);
        public async Task UpdateLastLoginAsync(long accountId, bool silent)
        {
            try
            {
                EnsureAzure();
                await using var con = new SqlConnection(Cs); await con.OpenAsync();
                const string sql = "UPDATE app.account SET last_login_at = SYSUTCDATETIME() WHERE account_id=@id";
                await using var cmd = new SqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@id", accountId);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex) { if (!silent) Debug.WriteLine("[Central] UpdateLastLogin error: " + ex.Message); }
        }

        public async Task<(long accountId, string? settingsJson)> EnsureAccountAndGetSettingsAsync(string uid, string email, string displayName)
        {
            EnsureAzure();
            var id = await EnsureAccountAsync(uid, email, displayName).ConfigureAwait(false);
            string? json = null;
            try
            {
                await using var con = new SqlConnection(Cs); await con.OpenAsync();
                const string sql = "SELECT settings_json, rev FROM radium.reportify_setting WHERE account_id=@id";
                await using var cmd = new SqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@id", id);
                await using var rd = await cmd.ExecuteReaderAsync();
                if (await rd.ReadAsync()) json = rd.GetString(0);
            }
            catch (Exception ex) { Debug.WriteLine("[Central] Fetch settings error: " + ex.Message); }
            return (id, json);
        }
    }
}
