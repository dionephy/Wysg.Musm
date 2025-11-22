using Microsoft.Data.SqlClient;
using System.Data;
using Wysg.Musm.Radium.Api.Models.Dtos;

namespace Wysg.Musm.Radium.Api.Repositories
{
    /// <summary>
    /// Repository implementation for account operations using Azure SQL
    /// </summary>
    public sealed class AccountRepository : IAccountRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;
        private readonly ILogger<AccountRepository> _logger;

        public AccountRepository(ISqlConnectionFactory connectionFactory, ILogger<AccountRepository> logger)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<AccountDto> EnsureAccountAsync(string uid, string email, string displayName)
        {
            if (string.IsNullOrWhiteSpace(uid))
                throw new ArgumentException("UID cannot be empty", nameof(uid));
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email cannot be empty", nameof(email));

            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();

            // First, try to find existing account by UID
            const string selectSql = @"
                SELECT account_id, uid, email, display_name, is_active, created_at, last_login_at
                FROM app.account
                WHERE uid = @uid";

            await using (var selectCmd = new SqlCommand(selectSql, connection))
            {
                selectCmd.Parameters.AddWithValue("@uid", uid);
                await using var reader = await selectCmd.ExecuteReaderAsync();
                
                if (await reader.ReadAsync())
                {
                    var accountId = reader.GetInt64(0);
                    await reader.CloseAsync();

                    // Update existing account
                    const string updateSql = @"
                        UPDATE app.account
                        SET email = @email, display_name = @displayName
                        WHERE account_id = @accountId";

                    await using var updateCmd = new SqlCommand(updateSql, connection);
                    updateCmd.Parameters.AddWithValue("@email", email);
                    updateCmd.Parameters.AddWithValue("@displayName", (object?)displayName ?? DBNull.Value);
                    updateCmd.Parameters.AddWithValue("@accountId", accountId);
                    await updateCmd.ExecuteNonQueryAsync();

                    _logger.LogInformation("Updated existing account {AccountId} for UID {Uid}", accountId, uid);
                    return await GetAccountByIdAsync(accountId) ?? throw new InvalidOperationException("Failed to retrieve updated account");
                }
            }

            // Account doesn't exist - create new one
            const string insertSql = @"
                INSERT INTO app.account (uid, email, display_name, is_active, created_at)
                OUTPUT INSERTED.account_id
                VALUES (@uid, @email, @displayName, 1, SYSUTCDATETIME())";

            try
            {
                await using var insertCmd = new SqlCommand(insertSql, connection);
                insertCmd.Parameters.AddWithValue("@uid", uid);
                insertCmd.Parameters.AddWithValue("@email", email);
                insertCmd.Parameters.AddWithValue("@displayName", (object?)displayName ?? DBNull.Value);

                var newAccountId = await insertCmd.ExecuteScalarAsync();
                if (newAccountId == null)
                    throw new InvalidOperationException("Failed to create account");

                var accountId = Convert.ToInt64(newAccountId);
                _logger.LogInformation("Created new account {AccountId} for UID {Uid}", accountId, uid);

                return await GetAccountByIdAsync(accountId) ?? throw new InvalidOperationException("Failed to retrieve new account");
            }
            catch (SqlException ex) when (ex.Number == 2627) // Unique constraint violation
            {
                // Race condition: account was created by another request
                // Try to fetch it by email
                _logger.LogWarning("Account creation race condition for UID {Uid}, attempting to fetch by email", uid);

                const string selectByEmailSql = @"
                    SELECT account_id
                    FROM app.account
                    WHERE email = @email";

                await using var selectByEmailCmd = new SqlCommand(selectByEmailSql, connection);
                selectByEmailCmd.Parameters.AddWithValue("@email", email);
                var existingId = await selectByEmailCmd.ExecuteScalarAsync();

                if (existingId != null)
                {
                    var accountId = Convert.ToInt64(existingId);
                    
                    // Adopt this UID
                    const string adoptSql = @"
                        UPDATE app.account
                        SET uid = @uid, display_name = @displayName
                        WHERE account_id = @accountId";

                    await using var adoptCmd = new SqlCommand(adoptSql, connection);
                    adoptCmd.Parameters.AddWithValue("@uid", uid);
                    adoptCmd.Parameters.AddWithValue("@displayName", (object?)displayName ?? DBNull.Value);
                    adoptCmd.Parameters.AddWithValue("@accountId", accountId);
                    await adoptCmd.ExecuteNonQueryAsync();

                    return await GetAccountByIdAsync(accountId) ?? throw new InvalidOperationException("Failed to retrieve adopted account");
                }

                throw;
            }
        }

        public async Task<AccountDto?> GetAccountByUidAsync(string uid)
        {
            if (string.IsNullOrWhiteSpace(uid))
                return null;

            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();

            const string sql = @"
                SELECT account_id, uid, email, display_name, is_active, created_at, last_login_at
                FROM app.account
                WHERE uid = @uid";

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@uid", uid);

            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapAccountDto(reader);
            }

            return null;
        }

        public async Task<AccountDto?> GetAccountByIdAsync(long accountId)
        {
            if (accountId <= 0)
                return null;

            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();

            const string sql = @"
                SELECT account_id, uid, email, display_name, is_active, created_at, last_login_at
                FROM app.account
                WHERE account_id = @accountId";

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@accountId", accountId);

            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapAccountDto(reader);
            }

            return null;
        }

        public async Task<bool> UpdateLastLoginAsync(long accountId)
        {
            if (accountId <= 0)
                return false;

            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();

            const string sql = @"
                UPDATE app.account
                SET last_login_at = SYSUTCDATETIME()
                WHERE account_id = @accountId";

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@accountId", accountId);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        public async Task<string?> GetReportifySettingsAsync(long accountId)
        {
            if (accountId <= 0)
                return null;

            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();

            const string sql = @"
                SELECT settings_json
                FROM radium.reportify_setting
                WHERE account_id = @accountId";

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@accountId", accountId);

            var result = await command.ExecuteScalarAsync();
            return result as string;
        }

        public async Task<bool> UpsertReportifySettingsAsync(long accountId, string settingsJson)
        {
            if (accountId <= 0)
                throw new ArgumentException("Account ID must be positive", nameof(accountId));
            if (string.IsNullOrWhiteSpace(settingsJson))
                throw new ArgumentException("Settings JSON cannot be empty", nameof(settingsJson));

            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();

            const string sql = @"
                MERGE radium.reportify_setting AS target
                USING (SELECT @accountId AS account_id, @settingsJson AS settings_json) AS source
                ON (target.account_id = source.account_id)
                WHEN MATCHED THEN
                    UPDATE SET 
                        settings_json = source.settings_json,
                        updated_at = SYSUTCDATETIME(),
                        rev = target.rev + 1
                WHEN NOT MATCHED THEN
                    INSERT (account_id, settings_json, updated_at, rev)
                    VALUES (source.account_id, source.settings_json, SYSUTCDATETIME(), 1);";

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@accountId", accountId);
            command.Parameters.AddWithValue("@settingsJson", settingsJson);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        private static AccountDto MapAccountDto(SqlDataReader reader)
        {
            return new AccountDto
            {
                AccountId = reader.GetInt64(0),
                Uid = reader.GetString(1),
                Email = reader.GetString(2),
                DisplayName = reader.IsDBNull(3) ? null : reader.GetString(3),
                IsActive = reader.GetBoolean(4),
                CreatedAt = reader.GetDateTime(5),
                LastLoginAt = reader.IsDBNull(6) ? null : reader.GetDateTime(6)
            };
        }
    }
}
