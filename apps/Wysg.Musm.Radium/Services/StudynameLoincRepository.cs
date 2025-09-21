using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Npgsql;

namespace Wysg.Musm.Radium.Services
{
    public sealed class StudynameLoincRepository : IStudynameLoincRepository
    {
        private readonly IRadiumLocalSettings _settings;
        public StudynameLoincRepository(IRadiumLocalSettings settings) => _settings = settings;

        private NpgsqlConnection Open() => new NpgsqlConnection(_settings.LocalConnectionString ?? _settings.CentralConnectionString ?? throw new InvalidOperationException("No connection string configured"));

        public async Task<IReadOnlyList<StudynameRow>> GetStudynamesAsync()
        {
            var list = new List<StudynameRow>();
            await using var cn = Open();
            await cn.OpenAsync();
            await using var cmd = new NpgsqlCommand("SELECT id, studyname FROM med.rad_studyname ORDER BY studyname", cn);
            await using var rd = await cmd.ExecuteReaderAsync();
            while (await rd.ReadAsync())
                list.Add(new StudynameRow(rd.GetInt64(0), rd.GetString(1)));
            return list;
        }

        public async Task<long> EnsureStudynameAsync(string studyname)
        {
            await using var cn = Open();
            await cn.OpenAsync();
            await using var cmd = new NpgsqlCommand(@"INSERT INTO med.rad_studyname(studyname)
VALUES (@n) ON CONFLICT (studyname) DO UPDATE SET studyname = EXCLUDED.studyname RETURNING id;", cn);
            cmd.Parameters.AddWithValue("@n", studyname);
            var id = (long)await cmd.ExecuteScalarAsync();
            return id;
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

        public async Task<IReadOnlyList<MappingRow>> GetMappingsAsync(long studynameId)
        {
            var list = new List<MappingRow>();
            await using var cn = Open();
            await cn.OpenAsync();
            await using var cmd = new NpgsqlCommand("SELECT part_number, part_sequence_order FROM med.rad_studyname_loinc_part WHERE studyname_id=@id ORDER BY part_number", cn);
            cmd.Parameters.AddWithValue("@id", studynameId);
            await using var rd = await cmd.ExecuteReaderAsync();
            while (await rd.ReadAsync())
                list.Add(new MappingRow(rd.GetString(0), rd.GetString(1)));
            return list;
        }

        public async Task SaveMappingsAsync(long studynameId, IEnumerable<MappingRow> items)
        {
            await using var cn = Open();
            await cn.OpenAsync();
            await using var tx = await cn.BeginTransactionAsync();
            await using (var del = new NpgsqlCommand("DELETE FROM med.rad_studyname_loinc_part WHERE studyname_id=@id", cn, (NpgsqlTransaction)tx))
            {
                del.Parameters.AddWithValue("@id", studynameId);
                await del.ExecuteNonQueryAsync();
            }
            await using (var ins = new NpgsqlCommand("INSERT INTO med.rad_studyname_loinc_part(studyname_id, part_number, part_sequence_order) VALUES (@id, @p, @o)", cn, (NpgsqlTransaction)tx))
            {
                var pId = ins.Parameters.Add("@id", NpgsqlTypes.NpgsqlDbType.Bigint);
                var pNum = ins.Parameters.Add("@p", NpgsqlTypes.NpgsqlDbType.Text);
                var pOrd = ins.Parameters.Add("@o", NpgsqlTypes.NpgsqlDbType.Text);
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
    }
}
