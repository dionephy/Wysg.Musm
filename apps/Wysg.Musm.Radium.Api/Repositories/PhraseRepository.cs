using Microsoft.Data.SqlClient;
using Wysg.Musm.Radium.Api.Models.Dtos;
using System.Linq;

namespace Wysg.Musm.Radium.Api.Repositories
{
    /// <summary>Repository implementation for phrase operations (Azure SQL) including GLOBAL (account_id IS NULL).</summary>
    public sealed class PhraseRepository : IPhraseRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;
        private readonly ILogger<PhraseRepository> _logger;
        public PhraseRepository(ISqlConnectionFactory connectionFactory, ILogger<PhraseRepository> logger)
        { _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory)); _logger = logger ?? throw new ArgumentNullException(nameof(logger)); }

        public async Task<List<PhraseDto>> GetAllAsync(long accountId, bool activeOnly = false)
        {
            await using var con = _connectionFactory.CreateConnection();
            await con.OpenAsync();
            var sql = activeOnly ?
                "SELECT id, account_id, text, active, created_at, updated_at, rev FROM radium.phrase WHERE account_id=@accountId AND active=1 ORDER BY text" :
                "SELECT id, account_id, text, active, created_at, updated_at, rev FROM radium.phrase WHERE account_id=@accountId ORDER BY text";
            await using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@accountId", accountId);
            var list = new List<PhraseDto>();
            await using var rd = await cmd.ExecuteReaderAsync();
            while (await rd.ReadAsync()) list.Add(Map(rd));
            return list;
        }

        public async Task<List<PhraseDto>> GetAllGlobalAsync(bool activeOnly = false)
        {
            await using var con = _connectionFactory.CreateConnection();
            await con.OpenAsync();
            var sql = activeOnly ?
                "SELECT id, account_id, text, active, created_at, updated_at, rev FROM radium.phrase WHERE account_id IS NULL AND active=1 ORDER BY text" :
                "SELECT id, account_id, text, active, created_at, updated_at, rev FROM radium.phrase WHERE account_id IS NULL ORDER BY text";
            await using var cmd = new SqlCommand(sql, con);
            var list = new List<PhraseDto>();
            await using var rd = await cmd.ExecuteReaderAsync();
            while (await rd.ReadAsync()) list.Add(Map(rd));
            return list;
        }

        public async Task<PhraseDto?> GetByIdAsync(long phraseId, long accountId)
        {
            await using var con = _connectionFactory.CreateConnection();
            await con.OpenAsync();
            const string sql = "SELECT id, account_id, text, active, created_at, updated_at, rev FROM radium.phrase WHERE id=@id AND account_id=@accountId";
            await using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@id", phraseId);
            cmd.Parameters.AddWithValue("@accountId", accountId);
            await using var rd = await cmd.ExecuteReaderAsync();
            return await rd.ReadAsync() ? Map(rd) : null;
        }

        public async Task<PhraseDto?> GetGlobalByIdAsync(long phraseId)
        {
            await using var con = _connectionFactory.CreateConnection();
            await con.OpenAsync();
            const string sql = "SELECT id, account_id, text, active, created_at, updated_at, rev FROM radium.phrase WHERE id=@id AND account_id IS NULL";
            await using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@id", phraseId);
            await using var rd = await cmd.ExecuteReaderAsync();
            return await rd.ReadAsync() ? Map(rd) : null;
        }

        public async Task<List<PhraseDto>> SearchAsync(long accountId, string? query, bool activeOnly, int maxResults)
        {
            await using var con = _connectionFactory.CreateConnection();
            await con.OpenAsync();
            var sql = "SELECT TOP (@max) id, account_id, text, active, created_at, updated_at, rev FROM radium.phrase WHERE account_id=@accountId";
            if (!string.IsNullOrWhiteSpace(query)) sql += " AND text LIKE @q";
            if (activeOnly) sql += " AND active=1";
            sql += " ORDER BY text";
            await using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@accountId", accountId);
            cmd.Parameters.AddWithValue("@max", maxResults);
            if (!string.IsNullOrWhiteSpace(query)) cmd.Parameters.AddWithValue("@q", $"%{query}%");
            var list = new List<PhraseDto>();
            await using var rd = await cmd.ExecuteReaderAsync();
            while (await rd.ReadAsync()) list.Add(Map(rd));
            return list;
        }

        public async Task<List<PhraseDto>> SearchGlobalAsync(string? query, bool activeOnly, int maxResults)
        {
            await using var con = _connectionFactory.CreateConnection();
            await con.OpenAsync();
            var sql = "SELECT TOP (@max) id, account_id, text, active, created_at, updated_at, rev FROM radium.phrase WHERE account_id IS NULL";
            if (!string.IsNullOrWhiteSpace(query)) sql += " AND text LIKE @q";
            if (activeOnly) sql += " AND active=1";
            sql += " ORDER BY text";
            await using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@max", maxResults);
            if (!string.IsNullOrWhiteSpace(query)) cmd.Parameters.AddWithValue("@q", $"%{query}%");
            var list = new List<PhraseDto>();
            await using var rd = await cmd.ExecuteReaderAsync();
            while (await rd.ReadAsync()) list.Add(Map(rd));
            return list;
        }

        public async Task<PhraseDto> UpsertAsync(long accountId, string text, bool active)
        {
            if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Phrase text cannot be empty", nameof(text));
            await using var con = _connectionFactory.CreateConnection();
            await con.OpenAsync();
            const string checkSql = "SELECT id FROM radium.phrase WHERE account_id=@accountId AND text=@text";
            await using (var check = new SqlCommand(checkSql, con))
            {
                check.Parameters.AddWithValue("@accountId", accountId);
                check.Parameters.AddWithValue("@text", text);
                var existing = await check.ExecuteScalarAsync();
                if (existing != null)
                {
                    const string updSql = "UPDATE radium.phrase SET active=@active WHERE id=@id";
                    await using var upd = new SqlCommand(updSql, con);
                    upd.Parameters.AddWithValue("@active", active);
                    upd.Parameters.AddWithValue("@id", Convert.ToInt64(existing));
                    await upd.ExecuteNonQueryAsync();
                    _logger.LogInformation("Updated phrase {Id} account {AccountId}", existing, accountId);
                    return await GetByIdAsync(Convert.ToInt64(existing), accountId) ?? throw new InvalidOperationException("Failed to load updated phrase");
                }
            }
            const string insSql = "INSERT INTO radium.phrase (account_id,text,active,created_at,updated_at,rev) OUTPUT INSERTED.id VALUES (@accountId,@text,@active,SYSUTCDATETIME(),SYSUTCDATETIME(),1)";
            await using var ins = new SqlCommand(insSql, con);
            ins.Parameters.AddWithValue("@accountId", accountId);
            ins.Parameters.AddWithValue("@text", text);
            ins.Parameters.AddWithValue("@active", active);
            var newId = await ins.ExecuteScalarAsync();
            _logger.LogInformation("Inserted phrase {Id} account {AccountId}", newId, accountId);
            return await GetByIdAsync(Convert.ToInt64(newId!), accountId) ?? throw new InvalidOperationException("Failed to load new phrase");
        }

        public async Task<PhraseDto> UpsertGlobalAsync(string text, bool active)
        {
            if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Phrase text cannot be empty", nameof(text));
            await using var con = _connectionFactory.CreateConnection();
            await con.OpenAsync();
            const string checkSql = "SELECT id FROM radium.phrase WHERE account_id IS NULL AND text=@text";
            await using (var check = new SqlCommand(checkSql, con))
            {
                check.Parameters.AddWithValue("@text", text);
                var existing = await check.ExecuteScalarAsync();
                if (existing != null)
                {
                    const string updSql = "UPDATE radium.phrase SET active=@active WHERE id=@id";
                    await using var upd = new SqlCommand(updSql, con);
                    upd.Parameters.AddWithValue("@active", active);
                    upd.Parameters.AddWithValue("@id", Convert.ToInt64(existing));
                    await upd.ExecuteNonQueryAsync();
                    _logger.LogInformation("Updated GLOBAL phrase {Id}", existing);
                    return await GetGlobalByIdAsync(Convert.ToInt64(existing)) ?? throw new InvalidOperationException("Failed to load updated global phrase");
                }
            }
            const string insSql = "INSERT INTO radium.phrase (account_id,text,active,created_at,updated_at,rev) OUTPUT INSERTED.id VALUES (NULL,@text,@active,SYSUTCDATETIME(),SYSUTCDATETIME(),1)";
            await using var ins = new SqlCommand(insSql, con);
            ins.Parameters.AddWithValue("@text", text);
            ins.Parameters.AddWithValue("@active", active);
            var newId = await ins.ExecuteScalarAsync();
            _logger.LogInformation("Inserted GLOBAL phrase {Id}", newId);
            return await GetGlobalByIdAsync(Convert.ToInt64(newId!)) ?? throw new InvalidOperationException("Failed to load new global phrase");
        }

        public async Task<List<PhraseDto>> BatchUpsertAsync(long accountId, List<string> phrases, bool active)
        {
            var result = new List<PhraseDto>();
            foreach (var p in phrases.Where(p => !string.IsNullOrWhiteSpace(p)))
            {
                try { result.Add(await UpsertAsync(accountId, p.Trim(), active)); }
                catch (Exception ex) { _logger.LogWarning(ex, "Failed upsert '{Text}' acct={AccountId}", p, accountId); }
            }
            return result;
        }

        public async Task<List<PhraseDto>> BatchUpsertGlobalAsync(List<string> phrases, bool active)
        {
            var result = new List<PhraseDto>();
            foreach (var p in phrases.Where(p => !string.IsNullOrWhiteSpace(p)))
            {
                try { result.Add(await UpsertGlobalAsync(p.Trim(), active)); }
                catch (Exception ex) { _logger.LogWarning(ex, "Failed global upsert '{Text}'", p); }
            }
            return result;
        }

        public async Task<bool> ToggleActiveAsync(long phraseId, long accountId)
        {
            await using var con = _connectionFactory.CreateConnection();
            await con.OpenAsync();
            const string sql = "UPDATE radium.phrase SET active = CASE WHEN active=1 THEN 0 ELSE 1 END WHERE id=@id AND account_id=@acct";
            await using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@id", phraseId);
            cmd.Parameters.AddWithValue("@acct", accountId);
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> ToggleGlobalActiveAsync(long phraseId)
        {
            await using var con = _connectionFactory.CreateConnection();
            await con.OpenAsync();
            const string sql = "UPDATE radium.phrase SET active = CASE WHEN active=1 THEN 0 ELSE 1 END WHERE id=@id AND account_id IS NULL";
            await using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@id", phraseId);
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> DeleteAsync(long phraseId, long accountId)
        {
            await using var con = _connectionFactory.CreateConnection();
            await con.OpenAsync();
            const string sql = "DELETE FROM radium.phrase WHERE id=@id AND account_id=@acct";
            await using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@id", phraseId);
            cmd.Parameters.AddWithValue("@acct", accountId);
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> DeleteGlobalAsync(long phraseId)
        {
            await using var con = _connectionFactory.CreateConnection();
            await con.OpenAsync();
            const string sql = "DELETE FROM radium.phrase WHERE id=@id AND account_id IS NULL";
            await using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@id", phraseId);
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public async Task<long> GetMaxRevisionAsync(long accountId)
        {
            await using var con = _connectionFactory.CreateConnection();
            await con.OpenAsync();
            const string sql = "SELECT ISNULL(MAX(rev),0) FROM radium.phrase WHERE account_id=@acct";
            await using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@acct", accountId);
            var result = await cmd.ExecuteScalarAsync();
            return result == DBNull.Value ? 0 : Convert.ToInt64(result);
        }

        public async Task<long> GetGlobalMaxRevisionAsync()
        {
            await using var con = _connectionFactory.CreateConnection();
            await con.OpenAsync();
            const string sql = "SELECT ISNULL(MAX(rev),0) FROM radium.phrase WHERE account_id IS NULL";
            await using var cmd = new SqlCommand(sql, con);
            var result = await cmd.ExecuteScalarAsync();
            return result == DBNull.Value ? 0 : Convert.ToInt64(result);
        }

        private static PhraseDto Map(SqlDataReader rd) => new()
        {
            Id = rd.GetInt64(0),
            AccountId = rd.IsDBNull(1) ? null : rd.GetInt64(1),
            Text = rd.GetString(2),
            Active = rd.GetBoolean(3),
            CreatedAt = rd.GetDateTime(4),
            UpdatedAt = rd.GetDateTime(5),
            Rev = rd.GetInt64(6)
        };
    }
}
