using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Npgsql;
using Wysg.Musm.Radium.Models;

namespace Wysg.Musm.Radium.Services
{
    /// <summary>
    /// Repository for managing tenants in the local database.
    /// Each tenant represents a PACS profile (identified by pacs_key).
    /// </summary>
    public interface ITenantRepository
    {
        Task<TenantModel?> GetTenantByAccountAndPacsAsync(long accountId, string pacsKey);
        Task<IReadOnlyList<TenantModel>> GetTenantsForAccountAsync(long accountId);
        Task<TenantModel> EnsureTenantAsync(long accountId, string pacsKey);
        Task<TenantModel?> GetTenantByIdAsync(long tenantId);
        Task<bool> DeleteTenantAsync(long tenantId);
    }

    public sealed class TenantRepository : ITenantRepository
    {
        private readonly IRadiumLocalSettings _settings;

        public TenantRepository(IRadiumLocalSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        private NpgsqlConnection CreateConnection()
        {
            var cs = _settings.LocalConnectionString;
            if (string.IsNullOrWhiteSpace(cs))
                throw new InvalidOperationException("Local connection string not configured");
            return new NpgsqlConnection(cs);
        }

        public async Task<TenantModel?> GetTenantByAccountAndPacsAsync(long accountId, string pacsKey)
        {
            if (accountId <= 0 || string.IsNullOrWhiteSpace(pacsKey))
                return null;

            const string sql = @"
                SELECT id, account_id, pacs_key, created_at 
                FROM app.tenant 
                WHERE account_id = @accountId AND pacs_key = @pacsKey";

            await using var con = CreateConnection();
            await con.OpenAsync().ConfigureAwait(false);
            await using var cmd = new NpgsqlCommand(sql, con) { CommandTimeout = 10 };
            cmd.Parameters.AddWithValue("accountId", accountId);
            cmd.Parameters.AddWithValue("pacsKey", pacsKey);

            await using var rd = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
            if (await rd.ReadAsync().ConfigureAwait(false))
            {
                return new TenantModel
                {
                    Id = rd.GetInt64(0),
                    AccountId = rd.GetInt64(1),
                    PacsKey = rd.GetString(2),
                    CreatedAt = rd.GetDateTime(3)
                };
            }
            return null;
        }

        public async Task<IReadOnlyList<TenantModel>> GetTenantsForAccountAsync(long accountId)
        {
            if (accountId <= 0)
                return Array.Empty<TenantModel>();

            const string sql = @"
                SELECT id, account_id, pacs_key, created_at 
                FROM app.tenant 
                WHERE account_id = @accountId
                ORDER BY created_at ASC";

            var list = new List<TenantModel>();
            await using var con = CreateConnection();
            await con.OpenAsync().ConfigureAwait(false);
            await using var cmd = new NpgsqlCommand(sql, con) { CommandTimeout = 10 };
            cmd.Parameters.AddWithValue("accountId", accountId);

            await using var rd = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
            while (await rd.ReadAsync().ConfigureAwait(false))
            {
                list.Add(new TenantModel
                {
                    Id = rd.GetInt64(0),
                    AccountId = rd.GetInt64(1),
                    PacsKey = rd.GetString(2),
                    CreatedAt = rd.GetDateTime(3)
                });
            }
            return list;
        }

        public async Task<TenantModel> EnsureTenantAsync(long accountId, string pacsKey)
        {
            if (accountId <= 0)
                throw new ArgumentOutOfRangeException(nameof(accountId));
            if (string.IsNullOrWhiteSpace(pacsKey))
                throw new ArgumentNullException(nameof(pacsKey));

            Debug.WriteLine($"[TenantRepository] EnsureTenantAsync account={accountId} pacs={pacsKey}");

            // Try to find existing tenant
            var existing = await GetTenantByAccountAndPacsAsync(accountId, pacsKey).ConfigureAwait(false);
            if (existing != null)
            {
                Debug.WriteLine($"[TenantRepository] Found existing tenant id={existing.Id}");
                return existing;
            }

            // Insert new tenant
            const string insertSql = @"
                INSERT INTO app.tenant (account_id, pacs_key)
                VALUES (@accountId, @pacsKey)
                ON CONFLICT (account_id, pacs_key) DO UPDATE
                    SET account_id = EXCLUDED.account_id
                RETURNING id, account_id, pacs_key, created_at";

            await using var con = CreateConnection();
            await con.OpenAsync().ConfigureAwait(false);
            await using var cmd = new NpgsqlCommand(insertSql, con) { CommandTimeout = 10 };
            cmd.Parameters.AddWithValue("accountId", accountId);
            cmd.Parameters.AddWithValue("pacsKey", pacsKey);

            await using var rd = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
            if (await rd.ReadAsync().ConfigureAwait(false))
            {
                var newTenant = new TenantModel
                {
                    Id = rd.GetInt64(0),
                    AccountId = rd.GetInt64(1),
                    PacsKey = rd.GetString(2),
                    CreatedAt = rd.GetDateTime(3)
                };
                Debug.WriteLine($"[TenantRepository] Created new tenant id={newTenant.Id}");
                return newTenant;
            }

            throw new InvalidOperationException("Failed to ensure tenant");
        }

        public async Task<TenantModel?> GetTenantByIdAsync(long tenantId)
        {
            if (tenantId <= 0)
                return null;

            const string sql = @"
                SELECT id, account_id, pacs_key, created_at 
                FROM app.tenant 
                WHERE id = @tenantId";

            await using var con = CreateConnection();
            await con.OpenAsync().ConfigureAwait(false);
            await using var cmd = new NpgsqlCommand(sql, con) { CommandTimeout = 10 };
            cmd.Parameters.AddWithValue("tenantId", tenantId);

            await using var rd = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
            if (await rd.ReadAsync().ConfigureAwait(false))
            {
                return new TenantModel
                {
                    Id = rd.GetInt64(0),
                    AccountId = rd.GetInt64(1),
                    PacsKey = rd.GetString(2),
                    CreatedAt = rd.GetDateTime(3)
                };
            }
            return null;
        }

        public async Task<bool> DeleteTenantAsync(long tenantId)
        {
            if (tenantId <= 0)
                return false;

            const string sql = @"DELETE FROM app.tenant WHERE id = @tenantId";

            await using var con = CreateConnection();
            await con.OpenAsync().ConfigureAwait(false);
            await using var cmd = new NpgsqlCommand(sql, con) { CommandTimeout = 10 };
            cmd.Parameters.AddWithValue("tenantId", tenantId);

            var rowsAffected = await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            Debug.WriteLine($"[TenantRepository] DeleteTenantAsync tenant_id={tenantId} rows={rowsAffected}");
            return rowsAffected > 0;
        }
    }
}
