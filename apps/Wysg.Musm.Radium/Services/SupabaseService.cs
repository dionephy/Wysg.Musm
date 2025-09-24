using Npgsql;
using System;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Wysg.Musm.Radium.Services
{
    public sealed class SupabaseService : ISupabaseService
    {
        private readonly IRadiumLocalSettings _local;
        private static int _openCounter = 0;
        private static int _callCounter = 0;
        public SupabaseService(IRadiumLocalSettings local) { _local = local; Debug.WriteLine("[Central][Init] SupabaseService constructed"); }

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

        private NpgsqlConnection CreateConnection()
        {
            var cs = BuildConnectionString();
            var b = new NpgsqlConnectionStringBuilder(cs);
            var redacted = $"Host={b.Host};Port={b.Port};Db={b.Database};User={b.Username};SSLMode={b.SslMode};Pooling={b.Pooling}";
            var id = System.Threading.Interlocked.Increment(ref _openCounter);
            Debug.WriteLine($"[Central][Open#{id}] Creating connection {redacted}");
            return new NpgsqlConnection(cs);
        }

        public async Task<(bool ok, string message)> TestConnectionAsync()
        {
            var call = System.Threading.Interlocked.Increment(ref _callCounter);
            Debug.WriteLine($"[Central][Call#{call}] TestConnectionAsync START");
            try
            {
                await using var con = CreateConnection();
                await con.OpenAsync();
                Debug.WriteLine($"[Central][Call#{call}] Opened State={con.FullState}");
                await using var cmd = new NpgsqlCommand("select version();", con);
                var ver = (string)(await cmd.ExecuteScalarAsync())!;
                Debug.WriteLine($"[Central][Call#{call}] OK {ver}");
                return (true, $"OK: {ver}");
            }
            catch (PostgresException pex)
            {
                Debug.WriteLine($"[Central][Call#{call}][PGX] SqlState={pex.SqlState} Msg={pex.MessageText} Detail={pex.Detail} Hint={pex.Hint} Where={pex.Where}");
                return (false, $"PG:{pex.SqlState}:{pex.MessageText}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Central][Call#{call}][EX] {ex.GetType().Name} {ex.Message}\n{ex.StackTrace}");
                return (false, ex.ToString());
            }
        }

        public async Task<long> EnsureAccountAsync(string uid, string email, string displayName)
        {
            var call = System.Threading.Interlocked.Increment(ref _callCounter);
            Debug.WriteLine($"[Central][Call#{call}] EnsureAccountAsync uid={uid} email={email}");
            const string upsertByUid = @"INSERT INTO app.account (uid, email, display_name)
VALUES (@uid, @em, @dn)
ON CONFLICT (uid) DO UPDATE SET email = EXCLUDED.email, display_name = EXCLUDED.display_name
RETURNING account_id;";
            await using var con = CreateConnection();
            await con.OpenAsync();
            try
            {
                await using var cmd = new NpgsqlCommand(upsertByUid, con);
                cmd.Parameters.AddWithValue("uid", uid);
                cmd.Parameters.AddWithValue("em", email);
                cmd.Parameters.AddWithValue("dn", (object)displayName ?? string.Empty);
                var id = await cmd.ExecuteScalarAsync();
                Debug.WriteLine($"[Central][Call#{call}] EnsureAccountAsync OK id={id}");
                return (long)id!;
            }
            catch (PostgresException pgex) when (pgex.SqlState == PostgresErrorCodes.UniqueViolation && pgex.ConstraintName == "account_email_key")
            {
                Debug.WriteLine($"[Central][Call#{call}] UniqueViolation adopt by email");
                const string adoptByEmail = @"UPDATE app.account
SET uid = @uid, display_name = @dn
WHERE email = @em
RETURNING account_id;";
                await using var adopt = new NpgsqlCommand(adoptByEmail, con);
                adopt.Parameters.AddWithValue("uid", uid);
                adopt.Parameters.AddWithValue("em", email);
                adopt.Parameters.AddWithValue("dn", (object)displayName ?? string.Empty);
                var id = await adopt.ExecuteScalarAsync();
                if (id is long l) { Debug.WriteLine($"[Central][Call#{call}] Adopt OK id={l}"); return l; }
                await using var sel = new NpgsqlCommand("SELECT account_id FROM app.account WHERE email = @em;", con);
                sel.Parameters.AddWithValue("em", email);
                var sid = await sel.ExecuteScalarAsync();
                Debug.WriteLine($"[Central][Call#{call}] Adopt fallback id={sid}");
                return sid is long l2 ? l2 : throw new InvalidOperationException("Account not found by email after unique violation.");
            }
            catch (PostgresException pex)
            {
                Debug.WriteLine($"[Central][Call#{call}][PGX] SqlState={pex.SqlState} Msg={pex.MessageText}");
                throw;
            }
        }

        public async Task UpdateLastLoginAsync(long accountId)
        {
            var call = System.Threading.Interlocked.Increment(ref _callCounter);
            Debug.WriteLine($"[Central][Call#{call}] UpdateLastLoginAsync id={accountId}");
            await using var con = CreateConnection();
            await con.OpenAsync();
            await using var cmd = new NpgsqlCommand("UPDATE app.account SET last_login_at = now() WHERE account_id = @id;", con);
            cmd.Parameters.AddWithValue("id", accountId);
            try
            {
                var n = await cmd.ExecuteNonQueryAsync();
                Debug.WriteLine($"[Central][Call#{call}] UpdateLastLogin rows={n}");
            }
            catch (PostgresException pex)
            {
                Debug.WriteLine($"[Central][Call#{call}][PGX] SqlState={pex.SqlState} Msg={pex.MessageText}");
                throw;
            }
        }

        // Central diagnostic (single lightweight ping capturing server/network info)
        public async Task<CentralDbDiagnostics> GetDiagnosticsAsync()
        {
            var call = System.Threading.Interlocked.Increment(ref _callCounter);
            Debug.WriteLine($"[Central][Call#{call}] GetDiagnosticsAsync START");
            await using var con = CreateConnection();
            try
            {
                var start = Stopwatch.StartNew();
                await con.OpenAsync();
                var openMs = start.ElapsedMilliseconds;
                await using var cmd = new NpgsqlCommand("select current_database(), current_user, inet_server_addr()::text, inet_server_port(), version();", con);
                await using var rd = await cmd.ExecuteReaderAsync();
                await rd.ReadAsync();
                var db = rd.GetString(0);
                var user = rd.GetString(1);
                var host = rd.IsDBNull(2) ? "?" : rd.GetString(2);
                var port = rd.GetInt32(3);
                var ver = rd.GetString(4);
                Debug.WriteLine($"[Central][Call#{call}] GetDiagnosticsAsync OK openMs={openMs} db={db} user={user} host={host} port={port}");
                return new CentralDbDiagnostics(db, user, host, port, ver, null, DateTime.UtcNow);
            }
            catch (PostgresException pex)
            {
                Debug.WriteLine($"[Central][Call#{call}][PGX] SqlState={pex.SqlState} Msg={pex.MessageText} Detail={pex.Detail} Hint={pex.Hint}");
                return new CentralDbDiagnostics("", "", "", 0, "", $"PG:{pex.SqlState}:{pex.MessageText}", DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Central][Call#{call}][EX] {ex.GetType().Name} {ex.Message}");
                return new CentralDbDiagnostics("", "", "", 0, "", ex.Message, DateTime.UtcNow);
            }
        }
    }

    public sealed record CentralDbDiagnostics(string Database, string User, string Host, int Port, string Version, string? Error, DateTime RetrievedAtUtc);
}
