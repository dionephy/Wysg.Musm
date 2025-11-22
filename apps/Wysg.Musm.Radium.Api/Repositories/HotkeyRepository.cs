using Microsoft.Data.SqlClient;
using Wysg.Musm.Radium.Api.Models.Dtos;

namespace Wysg.Musm.Radium.Api.Repositories
{
    public sealed class HotkeyRepository : IHotkeyRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        public HotkeyRepository(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        }

        public async Task<IReadOnlyList<HotkeyDto>> GetAllByAccountAsync(long accountId)
        {
            const string sql = @"
                SELECT hotkey_id, account_id, trigger_text, expansion_text, 
                       ISNULL(description, N'') as description, is_active, updated_at, rev
                FROM radium.hotkey
                WHERE account_id = @accountId
                ORDER BY updated_at DESC";

            var results = new List<HotkeyDto>();

            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection) { CommandTimeout = 30 };
            command.Parameters.AddWithValue("@accountId", accountId);

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(MapToDto(reader));
            }

            return results;
        }

        public async Task<HotkeyDto?> GetByIdAsync(long accountId, long hotkeyId)
        {
            const string sql = @"
                SELECT hotkey_id, account_id, trigger_text, expansion_text, 
                       ISNULL(description, N'') as description, is_active, updated_at, rev
                FROM radium.hotkey
                WHERE hotkey_id = @hotkeyId AND account_id = @accountId";

            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection) { CommandTimeout = 30 };
            command.Parameters.AddWithValue("@hotkeyId", hotkeyId);
            command.Parameters.AddWithValue("@accountId", accountId);

            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapToDto(reader);
            }

            return null;
        }

        public async Task<HotkeyDto> UpsertAsync(long accountId, UpsertHotkeyRequest request)
        {
            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();

            // Try UPDATE first
            const string updateSql = @"
                UPDATE radium.hotkey
                SET expansion_text = @expansionText,
                    is_active = @isActive,
                    description = @description
                WHERE account_id = @accountId AND trigger_text = @triggerText";

            await using (var updateCmd = new SqlCommand(updateSql, connection) { CommandTimeout = 30 })
            {
                updateCmd.Parameters.AddWithValue("@expansionText", request.ExpansionText);
                updateCmd.Parameters.AddWithValue("@isActive", request.IsActive);
                updateCmd.Parameters.AddWithValue("@description", (object?)request.Description ?? DBNull.Value);
                updateCmd.Parameters.AddWithValue("@accountId", accountId);
                updateCmd.Parameters.AddWithValue("@triggerText", request.TriggerText);

                var rowsAffected = await updateCmd.ExecuteNonQueryAsync();

                if (rowsAffected == 0)
                {
                    // INSERT if not exists
                    const string insertSql = @"
                        INSERT INTO radium.hotkey(account_id, trigger_text, expansion_text, description, is_active)
                        VALUES (@accountId, @triggerText, @expansionText, @description, @isActive)";

                    await using var insertCmd = new SqlCommand(insertSql, connection) { CommandTimeout = 30 };
                    insertCmd.Parameters.AddWithValue("@accountId", accountId);
                    insertCmd.Parameters.AddWithValue("@triggerText", request.TriggerText);
                    insertCmd.Parameters.AddWithValue("@expansionText", request.ExpansionText);
                    insertCmd.Parameters.AddWithValue("@description", (object?)request.Description ?? DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@isActive", request.IsActive);

                    await insertCmd.ExecuteNonQueryAsync();
                }
            }

            // SELECT the result
            const string selectSql = @"
                SELECT hotkey_id, account_id, trigger_text, expansion_text, 
                       ISNULL(description, N'') as description, is_active, updated_at, rev
                FROM radium.hotkey
                WHERE account_id = @accountId AND trigger_text = @triggerText";

            await using var selectCmd = new SqlCommand(selectSql, connection) { CommandTimeout = 30 };
            selectCmd.Parameters.AddWithValue("@accountId", accountId);
            selectCmd.Parameters.AddWithValue("@triggerText", request.TriggerText);

            await using var reader = await selectCmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapToDto(reader);
            }

            throw new InvalidOperationException("Upsert failed - unable to retrieve inserted/updated hotkey");
        }

        public async Task<HotkeyDto?> ToggleActiveAsync(long accountId, long hotkeyId)
        {
            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();

            const string updateSql = @"
                UPDATE radium.hotkey
                SET is_active = CASE WHEN is_active = 1 THEN 0 ELSE 1 END
                WHERE hotkey_id = @hotkeyId AND account_id = @accountId";

            await using (var command = new SqlCommand(updateSql, connection) { CommandTimeout = 30 })
            {
                command.Parameters.AddWithValue("@hotkeyId", hotkeyId);
                command.Parameters.AddWithValue("@accountId", accountId);

                var rowsAffected = await command.ExecuteNonQueryAsync();
                if (rowsAffected == 0)
                {
                    return null;
                }
            }

            // Return updated entity
            return await GetByIdAsync(accountId, hotkeyId);
        }

        public async Task<bool> DeleteAsync(long accountId, long hotkeyId)
        {
            const string sql = @"
                DELETE FROM radium.hotkey
                WHERE hotkey_id = @hotkeyId AND account_id = @accountId";

            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection) { CommandTimeout = 30 };
            command.Parameters.AddWithValue("@hotkeyId", hotkeyId);
            command.Parameters.AddWithValue("@accountId", accountId);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        private static HotkeyDto MapToDto(SqlDataReader reader)
        {
            return new HotkeyDto
            {
                HotkeyId = reader.GetInt64(0),
                AccountId = reader.GetInt64(1),
                TriggerText = reader.GetString(2),
                ExpansionText = reader.GetString(3),
                Description = reader.GetString(4),
                IsActive = reader.GetBoolean(5),
                UpdatedAt = reader.GetDateTime(6),
                Rev = reader.GetInt64(7)
            };
        }
    }
}
