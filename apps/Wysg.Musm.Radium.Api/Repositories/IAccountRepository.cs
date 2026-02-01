using Wysg.Musm.Radium.Api.Models.Dtos;

namespace Wysg.Musm.Radium.Api.Repositories
{
    /// <summary>
    /// Repository interface for account operations
    /// </summary>
    public interface IAccountRepository
    {
        /// <summary>
        /// Ensures an account exists. Creates if not found, updates if exists.
        /// </summary>
        Task<AccountDto> EnsureAccountAsync(string uid, string email, string displayName);

        /// <summary>
        /// Gets account by UID (Firebase user ID)
        /// </summary>
        Task<AccountDto?> GetAccountByUidAsync(string uid);

        /// <summary>
        /// Gets account by account ID
        /// </summary>
        Task<AccountDto?> GetAccountByIdAsync(long accountId);

        /// <summary>
        /// Updates last login timestamp for an account
        /// </summary>
        Task<bool> UpdateLastLoginAsync(long accountId);

        /// <summary>
        /// Gets reportify settings JSON for an account
        /// </summary>
        Task<string?> GetReportifySettingsAsync(long accountId);

        /// <summary>
        /// Upserts reportify settings JSON for an account
        /// </summary>
        Task<bool> UpsertReportifySettingsAsync(long accountId, string settingsJson);

        /// <summary>
        /// Gets total account count
        /// </summary>
        Task<long> GetAccountCountAsync();
    }
}
