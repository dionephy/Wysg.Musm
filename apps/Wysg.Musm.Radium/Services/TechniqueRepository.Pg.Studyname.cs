using System.Collections.Generic;
using System.Threading.Tasks;
using Npgsql;

namespace Wysg.Musm.Radium.Services
{
    public partial interface ITechniqueRepository
    {
        Task<long?> GetStudynameIdByNameAsync(string studyname);
    }

    public sealed partial class TechniqueRepository : ITechniqueRepository
    {
        public async Task<long?> GetStudynameIdByNameAsync(string studyname)
        {
            await using var cn = Open(); await PgConnectionHelper.OpenWithLocalSslFallbackAsync(cn);
            var tid = _tenant?.TenantId ?? 0L;
            if (tid > 0)
            {
                const string sql = "SELECT id FROM med.rad_studyname WHERE tenant_id=@tid AND studyname=@n LIMIT 1";
                await using var cmd = new NpgsqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@tid", tid);
                cmd.Parameters.AddWithValue("@n", studyname);
                var o = await cmd.ExecuteScalarAsync();
                return o is long l ? l : null;
            }
            else
            {
                const string sql2 = "SELECT id FROM med.rad_studyname WHERE studyname=@n LIMIT 1";
                await using var cmd2 = new NpgsqlCommand(sql2, cn);
                cmd2.Parameters.AddWithValue("@n", studyname);
                var o2 = await cmd2.ExecuteScalarAsync();
                return o2 is long l2 ? l2 : null;
            }
        }
    }
}
