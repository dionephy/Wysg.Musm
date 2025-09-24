using System.Collections.Generic;
using System.Threading.Tasks;
using Npgsql;
using System;
using System.Diagnostics;

namespace Wysg.Musm.Radium.Services
{
    public class PhraseService : IPhraseService
    {
        private readonly IRadiumLocalSettings _settings;
        private readonly string _fallback = "Host=127.0.0.1;Port=5432;Database=wysg_dev;Username=postgres;Password=`123qweas;Timeout=3"; // legacy fallback

        // NOTE: Currently uses LOCAL DB (editor phrases). Planned: migrate to central DB (use CentralConnectionString) after consolidation.
        public PhraseService(IRadiumLocalSettings settings)
        {
            _settings = settings;
            Debug.WriteLine("[PhraseService] Constructed. Using LocalConnectionString (fallback if null). Future: switch to central.");
        }

        private string BuildConnectionString()
        {
            var raw = _settings.LocalConnectionString ?? _fallback;
            try
            {
                var b = new NpgsqlConnectionStringBuilder(raw);
                if (b.Timeout < 3) b.Timeout = 3;
                if (b.CommandTimeout < 30) b.CommandTimeout = 30;
                b.IncludeErrorDetail = true;
                // (Future) if switching to central prefer SSL
                return b.ConnectionString;
            }
            catch
            {
                return raw; // worst-case use raw
            }
        }

        private NpgsqlConnection CreateConnection()
        {
            var cs = BuildConnectionString();
            var b = new NpgsqlConnectionStringBuilder(cs);
            Debug.WriteLine($"[PhraseService] Opening phrases DB Host={b.Host} Db={b.Database} User={b.Username}");
            return new NpgsqlConnection(cs);
        }

        public async Task<IReadOnlyList<string>> GetPhrasesForTenantAsync(long tenantId)
        {
            const string sql = @"SELECT text FROM content.phrase WHERE tenant_id = @tid AND active = TRUE ORDER BY text LIMIT 500";
            var list = new List<string>();
            await using var con = CreateConnection();
            await con.OpenAsync().ConfigureAwait(false);
            await using var cmd = new NpgsqlCommand(sql, con);
            cmd.Parameters.AddWithValue("tid", tenantId);
            await using var rd = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
            while (await rd.ReadAsync().ConfigureAwait(false)) list.Add(rd.GetString(0));
            return list;
        }

        public async Task<IReadOnlyList<string>> GetPhrasesByPrefixAsync(long tenantId, string prefix, int limit = 50)
        {
            const string sql = @"SELECT text FROM content.phrase WHERE tenant_id = @tid AND active = TRUE AND (
                                    (case_sensitive = FALSE AND lower(text) LIKE lower(@p) || '%') OR
                                    (case_sensitive = TRUE  AND text LIKE @p || '%')
                                 )
                                 ORDER BY length(text), text
                                 LIMIT @limit";
            var list = new List<string>();
            await using var con = CreateConnection();
            await con.OpenAsync().ConfigureAwait(false);
            await using var cmd = new NpgsqlCommand(sql, con);
            cmd.Parameters.AddWithValue("tid", tenantId);
            cmd.Parameters.AddWithValue("p", prefix);
            cmd.Parameters.AddWithValue("limit", limit);
            await using var rd = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
            while (await rd.ReadAsync().ConfigureAwait(false)) list.Add(rd.GetString(0));
            return list;
        }
    }
}
