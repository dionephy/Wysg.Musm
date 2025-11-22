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

        /// <summary>
        /// Gets a specific phrase by ID
        /// </summary>
        Task<PhraseDto?> GetByIdAsync(long phraseId, long accountId);

        /// <summary>
        /// Searches phrases by text
        /// </summary>
        Task<List<PhraseDto>> SearchAsync(long accountId, string? query, bool activeOnly, int maxResults);

        /// <summary>
        /// Creates or updates a phrase (upsert by text)
        /// </summary>
        Task<PhraseDto> UpsertAsync(long accountId, string text, bool active);

        /// <summary>
        /// Batch upsert phrases
        /// </summary>
        Task<List<PhraseDto>> BatchUpsertAsync(long accountId, List<string> phrases, bool active);

        /// <summary>
        /// Toggles active status for a phrase
        /// </summary>
        Task<bool> ToggleActiveAsync(long phraseId, long accountId);

        /// <summary>
        /// Deletes a phrase
        /// </summary>
        Task<bool> DeleteAsync(long phraseId, long accountId);

        /// <summary>
        /// Gets revision number for sync
        /// </summary>
        Task<long> GetMaxRevisionAsync(long accountId);
    }
}
