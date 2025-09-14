using Npgsql;
using Wysg.Musm.Radium.Models;

namespace Wysg.Musm.Radium.Services
{
    public class TenantService : ITenantService
    {
        private readonly string _connectionString = 
            "Host=127.0.0.1;Port=5432;Database=wysg_dev;Username=postgres;Password=`123qweas";

        public async Task<TenantModel?> GetTenantByCodeAsync(string tenantCode)
        {
            const string sql = "SELECT id, code, name, created_at FROM app.tenant WHERE code = @code";
            
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("code", tenantCode);
            
            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                int idOrd = reader.GetOrdinal("id");
                int codeOrd = reader.GetOrdinal("code");
                int nameOrd = reader.GetOrdinal("name");
                int createdOrd = reader.GetOrdinal("created_at");

                return new TenantModel
                {
                    Id = reader.GetFieldValue<long>(idOrd),
                    Code = reader.GetFieldValue<string>(codeOrd),
                    Name = reader.GetFieldValue<string>(nameOrd),
                    CreatedAt = reader.GetFieldValue<DateTime>(createdOrd)
                };
            }
            
            return null;
        }

        public async Task<bool> ValidateLoginAsync(LoginRequest request)
        {
            // dev Å×³ÍÆ®´Â ÀÚµ¿ ½ÂÀÎ
            if (request.TenantCode == "dev")
                return true;

            var tenant = await GetTenantByCodeAsync(request.TenantCode);
            return tenant != null;
        }
    }
}