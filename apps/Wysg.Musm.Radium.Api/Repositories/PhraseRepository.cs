using Microsoft.Data.SqlClient;
using Wysg.Musm.Radium.Api.Models.Dtos;

namespace Wysg.Musm.Radium.Api.Repositories
{
    /// <summary>
    /// Repository implementation for phrase operations using Azure SQL
    /// </summary>
    public sealed class PhraseRepository : IPhraseRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;
        private readonly ILogger<PhraseRepository> _logger;

        public PhraseRepository(ISqlConnectionFactory connectionFactory, ILogger<PhraseRepository> logger)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<PhraseDto>> GetAllAsync(long accountId, bool activeOnly = false)
        {
            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();

            var sql = activeOnly
                ? "SELECT id, account_id, text, active, created_at, updated_at, rev FROM radium.phrase WHERE account_id = @accountId AND active = 1 ORDER BY text"
                : "SELECT id, account_id, text, active, created_at, updated_at, rev FROM radium.phrase WHERE account_id = @accountId ORDER BY text";

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@accountId", accountId);

            var phrases = new List<PhraseDto>();
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                phrases.Add(MapPhraseDto(reader));
            }

            return phrases;
        }

        public async Task<PhraseDto?> GetByIdAsync(long phraseId, long accountId)
        {
            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();

            const string sql = @"
                SELECT id, account_id, text, active, created_at, updated_at, rev
                FROM radium.phrase
                WHERE id = @phraseId AND account_id = @accountId";

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@phraseId", phraseId);
            command.Parameters.AddWithValue("@accountId", accountId);

            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapPhraseDto(reader);
            }

            return null;
        }

        public async Task<List<PhraseDto>> SearchAsync(long accountId, string? query, bool activeOnly, int maxResults)
        {
            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();

            var sql = @"
                SELECT TOP (@maxResults) id, account_id, text, active, created_at, updated_at, rev
                FROM radium.phrase
                WHERE account_id = @accountId";

            if (!string.IsNullOrWhiteSpace(query))
            {
                sql += " AND text LIKE @query";
            }

            if (activeOnly)
            {
                sql += " AND active = 1";
            }

            sql += " ORDER BY text";

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@accountId", accountId);
            command.Parameters.AddWithValue("@maxResults", maxResults);

            if (!string.IsNullOrWhiteSpace(query))
            {
                command.Parameters.AddWithValue("@query", $"%{query}%");
            }

            var phrases = new List<PhraseDto>();
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                phrases.Add(MapPhraseDto(reader));
            }

            return phrases;
        }

        public async Task<PhraseDto> UpsertAsync(long accountId, string text, bool active)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("Phrase text cannot be empty", nameof(text));

            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();

            // Check if phrase exists
            const string checkSql = @"
                SELECT id, account_id, text, active, created_at, updated_at, rev
                FROM radium.phrase
                WHERE account_id = @accountId AND text = @text";

            await using (var checkCmd = new SqlCommand(checkSql, connection))
            {
                checkCmd.Parameters.AddWithValue("@accountId", accountId);
                checkCmd.Parameters.AddWithValue("@text", text);

                await using var reader = await checkCmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    var existingPhraseId = reader.GetInt64(0);
                    await reader.CloseAsync();

                    // Update existing
                    const string updateSql = @"
                        UPDATE radium.phrase
                        SET active = @active
                        WHERE id = @phraseId";

                    await using var updateCmd = new SqlCommand(updateSql, connection);
                    updateCmd.Parameters.AddWithValue("@active", active);
                    updateCmd.Parameters.AddWithValue("@phraseId", existingPhraseId);
                    await updateCmd.ExecuteNonQueryAsync();

                    _logger.LogInformation("Updated phrase {PhraseId} for account {AccountId}", existingPhraseId, accountId);
                    return await GetByIdAsync(existingPhraseId, accountId) ?? throw new InvalidOperationException("Failed to retrieve updated phrase");
                }
            }

            // Insert new
            const string insertSql = @"
                INSERT INTO radium.phrase (account_id, text, active, created_at, updated_at, rev)
                OUTPUT INSERTED.id
                VALUES (@accountId, @text, @active, SYSUTCDATETIME(), SYSUTCDATETIME(), 1)";

            await using var insertCmd = new SqlCommand(insertSql, connection);
            insertCmd.Parameters.AddWithValue("@accountId", accountId);
            insertCmd.Parameters.AddWithValue("@text", text);
            insertCmd.Parameters.AddWithValue("@active", active);

            var newId = await insertCmd.ExecuteScalarAsync();
            if (newId == null)
                throw new InvalidOperationException("Failed to create phrase");

            var phraseId = Convert.ToInt64(newId);
            _logger.LogInformation("Created phrase {PhraseId} for account {AccountId}", phraseId, accountId);

            return await GetByIdAsync(phraseId, accountId) ?? throw new InvalidOperationException("Failed to retrieve new phrase");
        }

        public async Task<List<PhraseDto>> BatchUpsertAsync(long accountId, List<string> phrases, bool active)
        {
            var results = new List<PhraseDto>();

            foreach (var text in phrases.Where(p => !string.IsNullOrWhiteSpace(p)))
            {
                try
                {
                    var phrase = await UpsertAsync(accountId, text.Trim(), active);
                    results.Add(phrase);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to upsert phrase '{Text}' for account {AccountId}", text, accountId);
                }
            }

            return results;
        }

        public async Task<bool> ToggleActiveAsync(long phraseId, long accountId)
        {
            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();

            const string sql = @"
                UPDATE radium.phrase
                SET active = CASE WHEN active = 1 THEN 0 ELSE 1 END
                WHERE id = @phraseId AND account_id = @accountId";

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@phraseId", phraseId);
            command.Parameters.AddWithValue("@accountId", accountId);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        public async Task<bool> DeleteAsync(long phraseId, long accountId)
        {
            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();

            const string sql = @"
                DELETE FROM radium.phrase
                WHERE id = @phraseId AND account_id = @accountId";

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@phraseId", phraseId);
            command.Parameters.AddWithValue("@accountId", accountId);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        public async Task<long> GetMaxRevisionAsync(long accountId)
        {
            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();

            const string sql = @"
                SELECT ISNULL(MAX(rev), 0)
                FROM radium.phrase
                WHERE account_id = @accountId";

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@accountId", accountId);

            var result = await command.ExecuteScalarAsync();
            return result == DBNull.Value ? 0 : Convert.ToInt64(result);
        }

        private static PhraseDto MapPhraseDto(SqlDataReader reader)
        {
            return new PhraseDto
            {
                Id = reader.GetInt64(0),
                AccountId = reader.GetInt64(1),
                Text = reader.GetString(2),
                Active = reader.GetBoolean(3),
                CreatedAt = reader.GetDateTime(4),
                UpdatedAt = reader.GetDateTime(5),
                Rev = reader.GetInt64(6)
            };
        }
    }
}
