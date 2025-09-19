using Npgsql;
using System;
using System.Threading.Tasks;

namespace Wysg.Musm.Radium.Services
{
    public sealed class SupabaseService : ISupabaseService
    {
        private readonly IRadiumLocalSettings _local;
        public SupabaseService(IRadiumLocalSettings local) { _local = local; }

        private string GetCs()
            => string.IsNullOrWhiteSpace(_local.CentralConnectionString)
                ? "Host=127.0.0.1;Port=5432;Database=wysg_central;Username=postgres;Password=`123qweas"
                : _local.CentralConnectionString!;

        private string BuildConnectionString()
        {
            var raw = GetCs();
            var b = new NpgsqlConnectionStringBuilder(raw)
            {
                IncludeErrorDetail = true,
                Multiplexing = false,
                NoResetOnClose = true
            };
            if (!raw.Contains("sslmode", StringComparison.OrdinalIgnoreCase)) b.SslMode = SslMode.Require;
            if (!raw.Contains("Trust Server Certificate", StringComparison.OrdinalIgnoreCase) &&
                !raw.Contains("TrustServerCertificate", StringComparison.OrdinalIgnoreCase)) b.TrustServerCertificate = true;
            if (b.Timeout < 5) b.Timeout = 5; // connect timeout (sec)
            if (b.CommandTimeout < 30) b.CommandTimeout = 30; // command timeout (sec)
            if (b.KeepAlive < 10) b.KeepAlive = 10; // TCP keepalive (sec)
            return b.ConnectionString;
        }

        public async Task<(bool ok, string message)> TestConnectionAsync()
        {
            try
            {
                await using var con = new NpgsqlConnection(BuildConnectionString());
                await con.OpenAsync();
                await using var cmd = new NpgsqlCommand("select version();", con);
                var ver = (string)(await cmd.ExecuteScalarAsync())!;
                return (true, $"OK: {ver}");
            }
            catch (Exception ex)
            {
                return (false, ex.ToString());
            }
        }

        public async Task<long> EnsureAccountAsync(string uid, string email, string displayName)
        {
            const string upsertByUid = @"INSERT INTO app.account (uid, email, display_name)
VALUES (@uid, @em, @dn)
ON CONFLICT (uid) DO UPDATE SET email = EXCLUDED.email, display_name = EXCLUDED.display_name
RETURNING account_id;";
            await using var con = new NpgsqlConnection(BuildConnectionString());
            await con.OpenAsync();
            try
            {
                await using var cmd = new NpgsqlCommand(upsertByUid, con);
                cmd.Parameters.AddWithValue("uid", uid);
                cmd.Parameters.AddWithValue("em", email);
                cmd.Parameters.AddWithValue("dn", (object)displayName ?? string.Empty);
                var id = await cmd.ExecuteScalarAsync();
                return (long)id!;
            }
            catch (PostgresException pgex) when (pgex.SqlState == PostgresErrorCodes.UniqueViolation && pgex.ConstraintName == "account_email_key")
            {
                // Email already exists under a different uid: adopt that row by updating its uid/display_name
                const string adoptByEmail = @"UPDATE app.account
SET uid = @uid, display_name = @dn
WHERE email = @em
RETURNING account_id;";
                await using var adopt = new NpgsqlCommand(adoptByEmail, con);
                adopt.Parameters.AddWithValue("uid", uid);
                adopt.Parameters.AddWithValue("em", email);
                adopt.Parameters.AddWithValue("dn", (object)displayName ?? string.Empty);
                var id = await adopt.ExecuteScalarAsync();
                if (id is long l) return l;
                // Fallback: select the existing row id by email
                await using var sel = new NpgsqlCommand("SELECT account_id FROM app.account WHERE email = @em;", con);
                sel.Parameters.AddWithValue("em", email);
                var sid = await sel.ExecuteScalarAsync();
                return sid is long l2 ? l2 : throw new InvalidOperationException("Account not found by email after unique violation.");
            }
        }

        public async Task UpdateLastLoginAsync(long accountId)
        {
            await using var con = new NpgsqlConnection(BuildConnectionString());
            await con.OpenAsync();
            await using var cmd = new NpgsqlCommand("UPDATE app.account SET last_login_at = now() WHERE account_id = @id;", con);
            cmd.Parameters.AddWithValue("id", accountId);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
