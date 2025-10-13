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
            const string sql = "SELECT id FROM med.rad_studyname WHERE studyname=@n LIMIT 1";
            await using var cmd = new NpgsqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@n", studyname);
            var o = await cmd.ExecuteScalarAsync();
            return o is long l ? l : null;
        }
    }
}
