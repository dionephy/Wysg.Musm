using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Wysg.Musm.SnomedTools.Abstractions
{
    /// <summary>
    /// Phrase metadata information.
    /// </summary>
    public record PhraseInfo(
        long Id,
      long? AccountId,
        string Text,
     bool Active,
        DateTime UpdatedAt,
  long Rev,
        string? Tags = null,
        string? TagsSource = null,
    string? TagsSemanticTag = null);

    /// <summary>
    /// Service for managing medical phrases.
    /// </summary>
    public interface IPhraseService
    {
        /// <summary>
        /// Get all global phrase metadata (phrases with accountId IS NULL).
    /// </summary>
        Task<IReadOnlyList<PhraseInfo>> GetAllGlobalPhraseMetaAsync();

        /// <summary>
        /// Upsert a phrase. If accountId is null, creates/updates a global phrase.
    /// </summary>
   /// <param name="accountId">Account ID or null for global phrase</param>
 /// <param name="text">Phrase text</param>
        /// <param name="active">Whether the phrase is active</param>
        /// <returns>The created/updated phrase info</returns>
        Task<PhraseInfo> UpsertPhraseAsync(long? accountId, string text, bool active = true);

        /// <summary>
        /// Refresh global phrases cache.
        /// </summary>
        Task RefreshGlobalPhrasesAsync();
    }
}
