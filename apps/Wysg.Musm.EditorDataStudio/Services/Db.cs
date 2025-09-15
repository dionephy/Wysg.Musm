using System.Collections.Generic;
using System.Threading.Tasks;
using Npgsql;

namespace Wysg.Musm.EditorDataStudio.Services
{
    public interface IDbConfig { string ConnectionString { get; set; } }
    public sealed class DbConfig : IDbConfig
    {
        private readonly ILocalSettings _settings;
        public DbConfig(ILocalSettings settings)
        {
            _settings = settings;
            ConnectionString = settings.ConnectionString ?? "Host=127.0.0.1;Port=5432;Database=wysg_dev;Username=postgres;Password=`123qweas;Timeout=3;Include Error Detail=true";
        }
        public string ConnectionString
        {
            get => _settings.ConnectionString ?? _connStr;
            set { _connStr = value; _settings.ConnectionString = value; }
        }
        private string _connStr = string.Empty;
    }

    public interface IDb
    {
        Task<List<T>> QueryAsync<T>(string sql, object? args = null, System.Func<NpgsqlDataReader, T>? map = null);
        Task<T?> QuerySingleAsync<T>(string sql, object? args = null, System.Func<NpgsqlDataReader, T>? map = null);
        Task<int> ExecuteAsync(string sql, object? args = null);
    }

    public sealed class PgDb : IDb
    {
        private readonly IDbConfig _cfg;
        public PgDb(IDbConfig cfg) { _cfg = cfg; }

        public async Task<List<T>> QueryAsync<T>(string sql, object? args = null, System.Func<NpgsqlDataReader, T>? map = null)
        {
            var list = new List<T>();
            await using var con = new NpgsqlConnection(_cfg.ConnectionString);
            await con.OpenAsync().ConfigureAwait(false);
            await using var cmd = new NpgsqlCommand(sql, con);
            AddParams(cmd, args);
            await using var rd = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
            while (await rd.ReadAsync().ConfigureAwait(false))
                list.Add(map != null ? map(rd) : (T)rd.GetValue(0));
            return list;
        }

        public async Task<T?> QuerySingleAsync<T>(string sql, object? args = null, System.Func<NpgsqlDataReader, T>? map = null)
        {
            await using var con = new NpgsqlConnection(_cfg.ConnectionString);
            await con.OpenAsync().ConfigureAwait(false);
            await using var cmd = new NpgsqlCommand(sql, con);
            AddParams(cmd, args);
            await using var rd = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
            if (await rd.ReadAsync().ConfigureAwait(false))
                return map != null ? map(rd) : (T)rd.GetValue(0);
            return default;
        }

        public async Task<int> ExecuteAsync(string sql, object? args = null)
        {
            await using var con = new NpgsqlConnection(_cfg.ConnectionString);
            await con.OpenAsync().ConfigureAwait(false);
            await using var cmd = new NpgsqlCommand(sql, con);
            AddParams(cmd, args);
            return await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
        }

        private static void AddParams(NpgsqlCommand cmd, object? args)
        {
            if (args == null) return;
            foreach (var p in args.GetType().GetProperties())
                cmd.Parameters.AddWithValue(p.Name, p.GetValue(args) ?? System.DBNull.Value);
        }
    }
}
