using Microsoft.Data.SqlClient;
using Wysg.Musm.Radium.Api.Models.Dtos;

namespace Wysg.Musm.Radium.Api.Repositories
{
    public sealed class UserSettingRepository : IUserSettingRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        public UserSettingRepository(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        }

        public async Task<UserSettingDto?> GetByAccountAsync(long accountId)
        {
            const string sql = @"
                SELECT account_id, settings_json, updated_at, rev
                FROM radium.user_setting
                WHERE account_id = @accountId";

            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection) { CommandTimeout = 30 };
            command.Parameters.AddWithValue("@accountId", accountId);

            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapToDto(reader);
            }

            return null;
        }

        public async Task<UserSettingDto> UpsertAsync(long accountId, UpdateUserSettingRequest request)
        {
            const string sql = @"
                MERGE radium.user_setting AS target
                USING (SELECT @accountId AS account_id) AS source
                ON target.account_id = source.account_id
                WHEN MATCHED THEN
                    UPDATE SET 
                        settings_json = @settingsJson,
                        updated_at = SYSUTCDATETIME(),
                        rev = target.rev + 1
                WHEN NOT MATCHED THEN
                    INSERT (account_id, settings_json, updated_at, rev)
                    VALUES (@accountId, @settingsJson, SYSUTCDATETIME(), 1)
                OUTPUT INSERTED.account_id, INSERTED.settings_json, INSERTED.updated_at, INSERTED.rev;";

            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection) { CommandTimeout = 30 };
            command.Parameters.AddWithValue("@accountId", accountId);
            command.Parameters.AddWithValue("@settingsJson", request.SettingsJson ?? "{}");

            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapToDto(reader);
            }

            throw new InvalidOperationException("Upsert failed - unable to retrieve inserted/updated user setting");
        }

        public async Task<bool> DeleteAsync(long accountId)
        {
            const string sql = @"
                DELETE FROM radium.user_setting
                WHERE account_id = @accountId";

            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection) { CommandTimeout = 30 };
            command.Parameters.AddWithValue("@accountId", accountId);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        private static UserSettingDto MapToDto(SqlDataReader reader)
        {
            return new UserSettingDto
            {
                AccountId = reader.GetInt64(0),
                SettingsJson = reader.GetString(1),
                UpdatedAt = reader.GetDateTime(2),
                Rev = reader.GetInt64(3)
            };
        }
    }
}
