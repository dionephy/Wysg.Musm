using System.Collections.Generic;
using System.Threading.Tasks;
using Npgsql;

namespace Wysg.Musm.Radium.Services
{
    public sealed partial class TechniqueRepository : ITechniqueRepository
    {
        private readonly IRadiumLocalSettings _settings;
        private readonly ITenantContext? _tenant;
        public TechniqueRepository(IRadiumLocalSettings settings, ITenantContext? tenant = null) { _settings = settings; _tenant = tenant; }
        private static string FallbackLocal() => "Host=127.0.0.1;Port=5432;Database=wysg_dev;Username=postgres;Password=`123qweas";
        private NpgsqlConnection Open() => new(_settings.LocalConnectionString ?? FallbackLocal());
        private long Aid => _tenant?.AccountId ?? 0L;

        public async Task<IReadOnlyList<SimpleTextRow>> GetPrefixesAsync()
        {
            var list = new List<SimpleTextRow>();
            await using var cn = Open(); await PgConnectionHelper.OpenWithLocalSslFallbackAsync(cn);
            const string sql = "SELECT id, prefix_text, display_order FROM med.rad_technique_prefix WHERE account_id=@aid ORDER BY display_order, prefix_text";
            await using var cmd = new NpgsqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@aid", Aid);
            await using var rd = await cmd.ExecuteReaderAsync();
            while (await rd.ReadAsync()) list.Add(new SimpleTextRow(rd.GetInt64(0), rd.IsDBNull(1) ? string.Empty : rd.GetString(1)));
            return list;
        }

        public async Task<long> AddPrefixAsync(string text)
        {
            await using var cn = Open(); await PgConnectionHelper.OpenWithLocalSslFallbackAsync(cn);
            const string sql = @"INSERT INTO med.rad_technique_prefix(account_id, prefix_text) VALUES (@aid, @t)
ON CONFLICT (account_id, prefix_text) DO UPDATE SET prefix_text = EXCLUDED.prefix_text
RETURNING id";
            await using var cmd = new NpgsqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@aid", Aid);
            cmd.Parameters.AddWithValue("@t", text);
            var o = await cmd.ExecuteScalarAsync();
            return o is long l ? l : 0L;
        }

        public async Task<IReadOnlyList<SimpleTextRow>> GetTechsAsync()
        {
            var list = new List<SimpleTextRow>();
            await using var cn = Open(); await PgConnectionHelper.OpenWithLocalSslFallbackAsync(cn);
            const string sql = "SELECT id, tech_text, display_order FROM med.rad_technique_tech WHERE account_id=@aid ORDER BY display_order, tech_text";
            await using var cmd = new NpgsqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@aid", Aid);
            await using var rd = await cmd.ExecuteReaderAsync();
            while (await rd.ReadAsync()) list.Add(new SimpleTextRow(rd.GetInt64(0), rd.IsDBNull(1) ? string.Empty : rd.GetString(1)));
            return list;
        }

        public async Task<long> AddTechAsync(string text)
        {
            await using var cn = Open(); await PgConnectionHelper.OpenWithLocalSslFallbackAsync(cn);
            const string sql = @"INSERT INTO med.rad_technique_tech(account_id, tech_text) VALUES (@aid, @t)
ON CONFLICT (account_id, tech_text) DO UPDATE SET tech_text = EXCLUDED.tech_text
RETURNING id";
            await using var cmd = new NpgsqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@aid", Aid);
            cmd.Parameters.AddWithValue("@t", text);
            var o = await cmd.ExecuteScalarAsync();
            return o is long l ? l : 0L;
        }

        public async Task<IReadOnlyList<SimpleTextRow>> GetSuffixesAsync()
        {
            var list = new List<SimpleTextRow>();
            await using var cn = Open(); await PgConnectionHelper.OpenWithLocalSslFallbackAsync(cn);
            const string sql = "SELECT id, suffix_text, display_order FROM med.rad_technique_suffix WHERE account_id=@aid ORDER BY display_order, suffix_text";
            await using var cmd = new NpgsqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@aid", Aid);
            await using var rd = await cmd.ExecuteReaderAsync();
            while (await rd.ReadAsync()) list.Add(new SimpleTextRow(rd.GetInt64(0), rd.IsDBNull(1) ? string.Empty : rd.GetString(1)));
            return list;
        }

        public async Task<long> AddSuffixAsync(string text)
        {
            await using var cn = Open(); await PgConnectionHelper.OpenWithLocalSslFallbackAsync(cn);
            const string sql = @"INSERT INTO med.rad_technique_suffix(account_id, suffix_text) VALUES (@aid, @t)
ON CONFLICT (account_id, suffix_text) DO UPDATE SET suffix_text = EXCLUDED.suffix_text
RETURNING id";
            await using var cmd = new NpgsqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@aid", Aid);
            cmd.Parameters.AddWithValue("@t", text);
            var o = await cmd.ExecuteScalarAsync();
            return o is long l ? l : 0L;
        }

        public async Task<IReadOnlyList<StudynameCombinationRow>> GetCombinationsForStudynameAsync(long studynameId)
        {
            var list = new List<StudynameCombinationRow>();
            await using var cn = Open(); await PgConnectionHelper.OpenWithLocalSslFallbackAsync(cn);
            const string sql = @"SELECT m.combination_id,
       COALESCE(v.combination_name, v.combination_display) AS disp,
       m.is_default
FROM med.rad_studyname_technique_combination m
LEFT JOIN med.v_technique_combination_display v ON v.id = m.combination_id
WHERE m.studyname_id = @id
ORDER BY m.is_default DESC, disp";
            await using var cmd = new NpgsqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@id", studynameId);
            await using var rd = await cmd.ExecuteReaderAsync();
            while (await rd.ReadAsync())
            {
                long combId = rd.GetInt64(0);
                string display = rd.IsDBNull(1) ? string.Empty : rd.GetString(1);
                bool isDefault = !rd.IsDBNull(2) && rd.GetBoolean(2);
                list.Add(new StudynameCombinationRow(combId, display, isDefault));
            }
            return list;
        }

        public async Task SetDefaultForStudynameAsync(long studynameId, long combinationId)
        {
            await using var cn = Open(); await PgConnectionHelper.OpenWithLocalSslFallbackAsync(cn);
            await using var tx = await cn.BeginTransactionAsync();
            try
            {
                await using (var clear = new NpgsqlCommand("UPDATE med.rad_studyname_technique_combination SET is_default=false WHERE studyname_id=@sid AND is_default=true", cn, (NpgsqlTransaction)tx))
                { clear.Parameters.AddWithValue("@sid", studynameId); await clear.ExecuteNonQueryAsync(); }

                const string up = @"INSERT INTO med.rad_studyname_technique_combination(studyname_id, combination_id, is_default)
VALUES (@sid, @cid, true)
ON CONFLICT (studyname_id, combination_id) DO UPDATE SET is_default = EXCLUDED.is_default";
                await using (var upsert = new NpgsqlCommand(up, cn, (NpgsqlTransaction)tx))
                {
                    upsert.Parameters.AddWithValue("@sid", studynameId);
                    upsert.Parameters.AddWithValue("@cid", combinationId);
                    await upsert.ExecuteNonQueryAsync();
                }
                await tx.CommitAsync();
            }
            catch
            {
                try { await tx.RollbackAsync(); } catch { }
                throw;
            }
        }

        public async Task<long> EnsureTechniqueAsync(long? prefixId, long techId, long? suffixId)
        {
            await using var cn = Open(); await PgConnectionHelper.OpenWithLocalSslFallbackAsync(cn);
            const string sql = @"INSERT INTO med.rad_technique(account_id, prefix_id, tech_id, suffix_id)
VALUES (@aid, @p, @t, @s)
ON CONFLICT (account_id, prefix_id, tech_id, suffix_id) DO UPDATE SET tech_id = EXCLUDED.tech_id
RETURNING id";
            await using var cmd = new NpgsqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@aid", Aid);
            cmd.Parameters.AddWithValue("@p", prefixId.HasValue ? prefixId.Value : (object)System.DBNull.Value);
            cmd.Parameters.AddWithValue("@t", techId);
            cmd.Parameters.AddWithValue("@s", suffixId.HasValue ? suffixId.Value : (object)System.DBNull.Value);
            var o = await cmd.ExecuteScalarAsync();
            return o is long l ? l : 0L;
        }

        public async Task<long> CreateCombinationAsync(string? name)
        {
            await using var cn = Open(); await PgConnectionHelper.OpenWithLocalSslFallbackAsync(cn);
            const string sql = "INSERT INTO med.rad_technique_combination(account_id, combination_name) VALUES (@aid, @n) RETURNING id";
            await using var cmd = new NpgsqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@aid", Aid);
            cmd.Parameters.AddWithValue("@n", (object?)name ?? System.DBNull.Value);
            var o = await cmd.ExecuteScalarAsync();
            return o is long l ? l : 0L;
        }

        public async Task AddCombinationItemsAsync(long combinationId, IEnumerable<(long techniqueId, int sequenceOrder)> items)
        {
            await using var cn = Open(); await PgConnectionHelper.OpenWithLocalSslFallbackAsync(cn);
            await using var tx = await cn.BeginTransactionAsync();
            try
            {
                const string sql = @"INSERT INTO med.rad_technique_combination_item(combination_id, technique_id, sequence_order)
VALUES (@c, @t, @o)
ON CONFLICT (combination_id, technique_id, sequence_order) DO NOTHING";
                await using var cmd = new NpgsqlCommand(sql, cn, (NpgsqlTransaction)tx);
                var pC = cmd.Parameters.Add("@c", NpgsqlTypes.NpgsqlDbType.Bigint);
                var pT = cmd.Parameters.Add("@t", NpgsqlTypes.NpgsqlDbType.Bigint);
                var pO = cmd.Parameters.Add("@o", NpgsqlTypes.NpgsqlDbType.Integer);
                foreach (var (techId, seq) in items)
                {
                    pC.Value = combinationId; pT.Value = techId; pO.Value = seq;
                    await cmd.ExecuteNonQueryAsync();
                }
                await tx.CommitAsync();
            }
            catch
            {
                try { await tx.RollbackAsync(); } catch { }
                throw;
            }
        }

        public async Task LinkStudynameCombinationAsync(long studynameId, long combinationId, bool isDefault = false)
        {
            await using var cn = Open(); await PgConnectionHelper.OpenWithLocalSslFallbackAsync(cn);
            const string sql = @"INSERT INTO med.rad_studyname_technique_combination(studyname_id, combination_id, is_default)
VALUES (@sid, @cid, @def)
ON CONFLICT (studyname_id, combination_id) DO UPDATE SET is_default = EXCLUDED.is_default";
            await using var cmd = new NpgsqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@sid", studynameId);
            cmd.Parameters.AddWithValue("@cid", combinationId);
            cmd.Parameters.AddWithValue("@def", isDefault);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task DeleteStudynameCombinationLinkAsync(long studynameId, long combinationId)
        {
            await using var cn = Open(); await PgConnectionHelper.OpenWithLocalSslFallbackAsync(cn);
            const string sql = @"DELETE FROM med.rad_studyname_technique_combination 
WHERE studyname_id = @sid AND combination_id = @cid";
            await using var cmd = new NpgsqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@sid", studynameId);
            cmd.Parameters.AddWithValue("@cid", combinationId);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<long?> GetStudyTechniqueCombinationAsync(long studyId)
        {
            await using var cn = Open(); await PgConnectionHelper.OpenWithLocalSslFallbackAsync(cn);
            const string sql = @"SELECT combination_id FROM med.rad_study_technique_combination WHERE study_id = @sid LIMIT 1";
            await using var cmd = new NpgsqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@sid", studyId);
            var result = await cmd.ExecuteScalarAsync();
            return result is long l ? l : null;
        }
        
        public async Task LinkStudyTechniqueCombinationAsync(long studyId, long combinationId)
        {
            await using var cn = Open(); await PgConnectionHelper.OpenWithLocalSslFallbackAsync(cn);
            const string sql = @"INSERT INTO med.rad_study_technique_combination(study_id, combination_id)
VALUES (@sid, @cid)
ON CONFLICT (study_id) DO UPDATE SET combination_id = EXCLUDED.combination_id";
            await using var cmd = new NpgsqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@sid", studyId);
            cmd.Parameters.AddWithValue("@cid", combinationId);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
