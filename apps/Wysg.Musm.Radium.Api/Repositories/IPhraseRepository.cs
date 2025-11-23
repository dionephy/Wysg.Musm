using Wysg.Musm.Radium.Api.Models.Dtos;

namespace Wysg.Musm.Radium.Api.Repositories
{
    /// <summary>
    /// Repository interface for phrase operations
    /// </summary>
    public interface IPhraseRepository
    {
        /// <summary>
        /// Gets all phrases for an account
        /// </summary>
        Task<List<PhraseDto>> GetAllAsync(long accountId, bool activeOnly = false);
        /// <summary>Gets all GLOBAL phrases (account_id IS NULL)</summary>
        Task<List<PhraseDto>> GetAllGlobalAsync(bool activeOnly = false);

        /// <summary>
        /// Gets a specific phrase by ID
        /// </summary>
        Task<PhraseDto?> GetByIdAsync(long phraseId, long accountId);
        /// <summary>Gets a specific GLOBAL phrase by ID</summary>
        Task<PhraseDto?> GetGlobalByIdAsync(long phraseId);

        /// <summary>
        /// Searches phrases by text
        /// </summary>
        Task<List<PhraseDto>> SearchAsync(long accountId, string? query, bool activeOnly, int maxResults);
        /// <summary>Search GLOBAL phrases</summary>
        Task<List<PhraseDto>> SearchGlobalAsync(string? query, bool activeOnly, int maxResults);

        /// <summary>
        /// Creates or updates a phrase (upsert by text)
        /// </summary>
        Task<PhraseDto> UpsertAsync(long accountId, string text, bool active);
        /// <summary>Upsert GLOBAL phrase (account_id=NULL)</summary>
        Task<PhraseDto> UpsertGlobalAsync(string text, bool active);

        /// <summary>
        /// Batch upsert phrases
        /// </summary>
        Task<List<PhraseDto>> BatchUpsertAsync(long accountId, List<string> phrases, bool active);
        /// <summary>Batch upsert GLOBAL phrases</summary>
        Task<List<PhraseDto>> BatchUpsertGlobalAsync(List<string> phrases, bool active);

        /// <summary>
        /// Toggles active status for a phrase
        /// </summary>
        Task<bool> ToggleActiveAsync(long phraseId, long accountId);
        /// <summary>Toggle GLOBAL phrase active flag</summary>
        Task<bool> ToggleGlobalActiveAsync(long phraseId);

        /// <summary>
        /// Deletes a phrase
        /// </summary>
        Task<bool> DeleteAsync(long phraseId, long accountId);
        /// <summary>Delete GLOBAL phrase</summary>
        Task<bool> DeleteGlobalAsync(long phraseId);

        /// <summary>
        /// Gets revision number for sync
        /// </summary>
        Task<long> GetMaxRevisionAsync(long accountId);
        /// <summary>Get max revision for GLOBAL phrases</summary>
        Task<long> GetGlobalMaxRevisionAsync();
    }
}
