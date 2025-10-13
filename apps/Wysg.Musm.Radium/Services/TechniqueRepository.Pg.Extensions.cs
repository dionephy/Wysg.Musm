using System.Collections.Generic;
using System.Threading.Tasks;
using Npgsql;

namespace Wysg.Musm.Radium.Services
{
    public partial interface ITechniqueRepository
    {
        Task<(long CombinationId, string Display, bool IsDefault)?> GetDefaultCombinationForStudynameAsync(long studynameId);
        Task<IReadOnlyList<(string? Prefix, string Tech, string? Suffix, int SequenceOrder)>> GetCombinationItemsAsync(long combinationId);
    }

    public sealed partial class TechniqueRepository : ITechniqueRepository
    {
        public async Task<(long CombinationId, string Display, bool IsDefault)?> GetDefaultCombinationForStudynameAsync(long studynameId)
        {
            await using var cn = Open(); await PgConnectionHelper.OpenWithLocalSslFallbackAsync(cn);
            const string sql = @"SELECT m.combination_id,
       COALESCE(v.combination_name, v.combination_display) AS disp,
       m.is_default
FROM med.rad_studyname_technique_combination m
LEFT JOIN med.v_technique_combination_display v ON v.id = m.combination_id
WHERE m.studyname_id = @id AND m.is_default = true
LIMIT 1";
            await using var cmd = new NpgsqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@id", studynameId);
            await using var rd = await cmd.ExecuteReaderAsync();
            if (await rd.ReadAsync())
            {
                long combId = rd.GetInt64(0); string disp = rd.IsDBNull(1) ? string.Empty : rd.GetString(1); bool def = !rd.IsDBNull(2) && rd.GetBoolean(2);
                return (combId, disp, def);
            }
            return null;
        }

        public async Task<IReadOnlyList<(string? Prefix, string Tech, string? Suffix, int SequenceOrder)>> GetCombinationItemsAsync(long combinationId)
        {
            var list = new List<(string? Prefix, string Tech, string? Suffix, int SequenceOrder)>();
            await using var cn = Open(); await PgConnectionHelper.OpenWithLocalSslFallbackAsync(cn);
            const string sql = @"SELECT COALESCE(tp.prefix_text,''), tt.tech_text, COALESCE(ts.suffix_text,''), i.sequence_order
FROM med.technique_combination_item i
JOIN med.technique t ON t.id = i.technique_id
LEFT JOIN med.technique_prefix tp ON tp.id = t.prefix_id
JOIN med.technique_tech tt ON tt.id = t.tech_id
LEFT JOIN med.technique_suffix ts ON ts.id = t.suffix_id
WHERE i.combination_id = @cid
ORDER BY i.sequence_order";
            await using var cmd = new NpgsqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@cid", combinationId);
            await using var rd = await cmd.ExecuteReaderAsync();
            while (await rd.ReadAsync())
            {
                string? prefix = rd.IsDBNull(0) ? null : rd.GetString(0);
                string tech = rd.IsDBNull(1) ? string.Empty : rd.GetString(1);
                string? suffix = rd.IsDBNull(2) ? null : rd.GetString(2);
                int seq = rd.IsDBNull(3) ? 0 : rd.GetInt32(3);
                list.Add((prefix, tech, suffix, seq));
            }
            return list;
        }
    }
}
