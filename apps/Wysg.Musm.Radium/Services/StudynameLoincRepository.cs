using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Npgsql;
using NpgsqlTypes;
using System.Diagnostics;

namespace Wysg.Musm.Radium.Services
{
    public sealed class StudynameLoincRepository : IStudynameLoincRepository
    {
        private readonly IRadiumLocalSettings _settings;
        private string? _mapTableName; // cache resolved table name
        public StudynameLoincRepository(IRadiumLocalSettings settings) => _settings = settings;

        private static string GetFallbackLocalCs()
            => "Host=127.0.0.1;Port=5432;Database=wysg_dev;Username=postgres;Password=`123qweas";

        private NpgsqlConnection Open()
        {
            var raw = _settings.LocalConnectionString
                      ?? GetFallbackLocalCs()
                      ?? _settings.CentralConnectionString;
            var b = new NpgsqlConnectionStringBuilder(raw);
            return new NpgsqlConnection(b.ConnectionString);
        }

        private static async Task<string?> ResolveExistingMapTableAsync(NpgsqlConnection cn)
        {
            await using var cmd = new NpgsqlCommand("select coalesce(to_regclass('med.rad_studyname_loinc_part')::text, to_regclass('med.rad_studyname_loinc')::text)", cn);
            var o = await cmd.ExecuteScalarAsync();
            var s = o as string;
            return string.IsNullOrWhiteSpace(s) ? null : s;
        }

        private async Task<string> GetMapTableAsync(NpgsqlConnection cn)
        {
            if (!string.IsNullOrEmpty(_mapTableName)) return _mapTableName!;
            var tbl = await ResolveExistingMapTableAsync(cn) ?? throw new InvalidOperationException("Mapping table not found. Ensure med.rad_studyname_loinc_part or med.rad_studyname_loinc exists.");
            _mapTableName = tbl;
            return tbl;
        }

        public async Task<IReadOnlyList<StudynameRow>> GetStudynamesAsync()
        {
            var list = new List<StudynameRow>();
            await using var cn = Open();
            await cn.OpenAsync();
            await using var cmd = new NpgsqlCommand("SELECT id, studyname FROM med.rad_studyname ORDER BY studyname", cn);
            await using var rd = await cmd.ExecuteReaderAsync();
            while (await rd.ReadAsync()) list.Add(new StudynameRow(rd.GetInt64(0), rd.GetString(1)));
            return list;
        }

        public async Task<long> EnsureStudynameAsync(string studyname)
        {
            await using var cn = Open();
            await cn.OpenAsync();
            await using var cmd = new NpgsqlCommand(@"INSERT INTO med.rad_studyname(studyname)
VALUES (@n) ON CONFLICT (studyname) DO UPDATE SET studyname = EXCLUDED.studyname RETURNING id;", cn);
            cmd.Parameters.AddWithValue("@n", studyname);
            return (long)await cmd.ExecuteScalarAsync();
        }

        public async Task<IReadOnlyList<PartRow>> GetPartsAsync()
        {
            var list = new List<PartRow>();
            await using var cn = Open();
            await cn.OpenAsync();
            await using var cmd = new NpgsqlCommand("SELECT part_number, part_type_name, part_name FROM loinc.part ORDER BY part_type_name, part_name", cn);
            await using var rd = await cmd.ExecuteReaderAsync();
            while (await rd.ReadAsync())
                list.Add(new PartRow(rd.GetString(0), rd.IsDBNull(1) ? string.Empty : rd.GetString(1), rd.IsDBNull(2) ? string.Empty : rd.GetString(2)));
            return list;
        }

        public async Task<IReadOnlyList<CommonPartRow>> GetCommonPartsAsync(int limit = 50)
        {
            var list = new List<CommonPartRow>();
            await using var cn = Open();
            await cn.OpenAsync();
            var tbl = await GetMapTableAsync(cn);
            var sql = $@"SELECT p.part_number, p.part_type_name, p.part_name, COUNT(*) AS usage
                         FROM {tbl} m
                         JOIN loinc.part p ON p.part_number = m.part_number
                         GROUP BY p.part_number, p.part_type_name, p.part_name
                         ORDER BY usage DESC, p.part_name
                         LIMIT @lim";
            await using var cmd = new NpgsqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@lim", limit);
            await using var rd = await cmd.ExecuteReaderAsync();
            while (await rd.ReadAsync())
                list.Add(new CommonPartRow(rd.GetString(0), rd.IsDBNull(1) ? string.Empty : rd.GetString(1), rd.IsDBNull(2) ? string.Empty : rd.GetString(2), rd.GetInt64(3)));
            return list;
        }

        public async Task<IReadOnlyList<MappingRow>> GetMappingsAsync(long studynameId)
        {
            var list = new List<MappingRow>();
            await using var cn = Open();
            await cn.OpenAsync();
            var tbl = await GetMapTableAsync(cn);
            await using var cmd = new NpgsqlCommand($"SELECT part_number, part_sequence_order FROM {tbl} WHERE studyname_id=@id ORDER BY part_number", cn);
            cmd.Parameters.AddWithValue("@id", studynameId);
            await using var rd = await cmd.ExecuteReaderAsync();
            while (await rd.ReadAsync()) list.Add(new MappingRow(rd.GetString(0), rd.GetString(1)));
            return list;
        }

        public async Task SaveMappingsAsync(long studynameId, IEnumerable<MappingRow> items)
        {
            await using var cn = Open();
            await cn.OpenAsync();
            var tbl = await GetMapTableAsync(cn);
            await using var tx = await cn.BeginTransactionAsync();
            await using (var del = new NpgsqlCommand($"DELETE FROM {tbl} WHERE studyname_id=@id", cn, (NpgsqlTransaction)tx))
            {
                del.Parameters.AddWithValue("@id", studynameId);
                await del.ExecuteNonQueryAsync();
            }
            await using (var ins = new NpgsqlCommand($"INSERT INTO {tbl}(studyname_id, part_number, part_sequence_order) VALUES (@id, @p, @o)", cn, (NpgsqlTransaction)tx))
            {
                var pId = ins.Parameters.Add("@id", NpgsqlDbType.Bigint);
                var pNum = ins.Parameters.Add("@p", NpgsqlDbType.Text);
                var pOrd = ins.Parameters.Add("@o", NpgsqlDbType.Text);
                foreach (var it in items)
                {
                    pId.Value = studynameId;
                    pNum.Value = it.PartNumber;
                    pOrd.Value = it.PartSequenceOrder ?? "A";
                    await ins.ExecuteNonQueryAsync();
                }
            }
            await tx.CommitAsync();
        }

        public async Task<IReadOnlyList<PlaybookMatchRow>> GetPlaybookMatchesAsync(IEnumerable<string> partNumbers)
        {
            var list = new List<PlaybookMatchRow>();
            var numbers = partNumbers?.Distinct().ToArray() ?? Array.Empty<string>();
            if (numbers.Length < 3) return list;
            await using var cn = Open();
            await cn.OpenAsync();
            // Group by loinc_number because each loinc entry spans multiple part rows
            var sql = @"SELECT rb.loinc_number, max(rb.long_common_name) AS long_common_name
                        FROM loinc.rplaybook rb
                        WHERE rb.part_number = ANY(@nums)
                        GROUP BY rb.loinc_number
                        HAVING COUNT(DISTINCT rb.part_number) = @n
                        ORDER BY max(rb.long_common_name)";
            await using var cmd = new NpgsqlCommand(sql, cn);
            var pNums = cmd.Parameters.Add("@nums", NpgsqlDbType.Array | NpgsqlDbType.Text);
            pNums.Value = numbers;
            cmd.Parameters.Add("@n", NpgsqlDbType.Integer).Value = numbers.Length;
            await using var rd = await cmd.ExecuteReaderAsync();
            while (await rd.ReadAsync()) list.Add(new PlaybookMatchRow(rd.GetString(0), rd.IsDBNull(1) ? string.Empty : rd.GetString(1)));
            return list;
        }

        public async Task<IReadOnlyList<PlaybookPartDetailRow>> GetPlaybookPartsAsync(string loincNumber)
        {
            var list = new List<PlaybookPartDetailRow>();
            await using var cn = Open();
            await cn.OpenAsync();
            var sql = @"SELECT part_number, coalesce(part_name,''), coalesce(part_sequence_order,'A')
                        FROM loinc.rplaybook
                        WHERE loinc_number=@id
                        ORDER BY part_sequence_order, part_type_name, part_name";
            await using var cmd = new NpgsqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@id", loincNumber);
            await using var rd = await cmd.ExecuteReaderAsync();
            while (await rd.ReadAsync()) list.Add(new PlaybookPartDetailRow(rd.GetString(0), rd.GetString(1), rd.GetString(2)));
            return list;
        }
    }
}
