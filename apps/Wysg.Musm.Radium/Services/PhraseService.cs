using System.Collections.Generic;
using System.Threading.Tasks;
using Npgsql;

namespace Wysg.Musm.Radium.Services
{
    public class PhraseService : IPhraseService
    {
        // Add a small timeout to avoid long hangs if DB is unreachable
        private readonly string _cs = "Host=127.0.0.1;Port=5432;Database=wysg_dev;Username=postgres;Password=`123qweas;Timeout=3";

        public async Task<IReadOnlyList<string>> GetPhrasesForTenantAsync(long tenantId)
        {
            const string sql = @"SELECT text FROM content.phrase WHERE tenant_id = @tid AND active = TRUE ORDER BY text LIMIT 500";
            var list = new List<string>();
            await using var con = new NpgsqlConnection(_cs);
            await con.OpenAsync().ConfigureAwait(false);
            await using var cmd = new NpgsqlCommand(sql, con);
            cmd.Parameters.AddWithValue("tid", tenantId);
            await using var rd = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
            while (await rd.ReadAsync().ConfigureAwait(false))
            {
                list.Add(rd.GetString(0));
            }
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
            await using var con = new NpgsqlConnection(_cs);
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
