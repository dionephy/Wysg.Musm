using Microsoft.Data.SqlClient;

namespace Wysg.Musm.Radium.Api.Repositories
{
    public interface ISqlConnectionFactory
    {
        SqlConnection CreateConnection();
    }

    public sealed class SqlConnectionFactory : ISqlConnectionFactory
    {
        private readonly string _connectionString;

        public SqlConnectionFactory(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("DefaultConnection string not configured");
        }

        public SqlConnection CreateConnection()
        {
            return new SqlConnection(_connectionString);
        }
    }
}
