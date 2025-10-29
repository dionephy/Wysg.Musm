using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Wysg.Musm.SnomedTools.Abstractions
{
    /// <summary>
    /// Represents a cached SNOMED candidate synonym waiting for user review.
    /// </summary>
    public sealed record CachedCandidate(
        long Id,
        long ConceptId,
        string ConceptIdStr,
        string ConceptFsn,
        string? ConceptPt,
        string TermText,
        string TermType,
        int WordCount,
        DateTime CachedAt,
        CandidateStatus Status);

    public enum CandidateStatus
    {
        Pending = 0,      // Waiting for review
        Accepted = 1,     // User wants to save (resolve)
        Rejected = 2,     // User wants to ignore
        Saved = 3         // Already saved to database
    }

    /// <summary>
    /// Represents saved fetch progress for resuming.
    /// </summary>
    public sealed record FetchProgress(
        int TargetWordCount,
        string? NextSearchAfter,
        int CurrentPage,
        DateTime SavedAt);

    /// <summary>
    /// Service for managing local cache of SNOMED candidates.
    /// </summary>
    public interface ISnomedCacheService
    {
        /// <summary>
        /// Cache a new candidate synonym. Returns true if cached, false if duplicate.
        /// </summary>
        Task<bool> CacheCandidateAsync(
            long conceptId,
            string conceptIdStr,
            string conceptFsn,
            string? conceptPt,
            string termText,
            string termType,
            int wordCount);

        /// <summary>
        /// Check if a phrase text already exists in Azure SQL phrase table.
        /// </summary>
        Task<bool> CheckPhraseExistsInDatabaseAsync(string phraseText);

        /// <summary>
        /// Get all pending candidates (status = Pending).
        /// </summary>
        Task<IReadOnlyList<CachedCandidate>> GetPendingCandidatesAsync(int limit = 100);

        /// <summary>
        /// Get count of pending candidates.
        /// </summary>
        Task<int> GetPendingCountAsync();

        /// <summary>
        /// Mark a candidate as accepted (ready to save).
        /// </summary>
        Task MarkAcceptedAsync(long candidateId);

        /// <summary>
        /// Mark a candidate as rejected (ignore).
        /// </summary>
        Task MarkRejectedAsync(long candidateId);

        /// <summary>
        /// Mark a candidate as saved (already persisted to database).
        /// </summary>
        Task MarkSavedAsync(long candidateId);

        /// <summary>
        /// Delete old candidates (older than specified days).
        /// </summary>
        Task DeleteOldCandidatesAsync(int daysOld = 30);

        /// <summary>
        /// Get all accepted candidates (ready to be saved to database).
        /// </summary>
        Task<IReadOnlyList<CachedCandidate>> GetAcceptedCandidatesAsync();

        /// <summary>
        /// Clear all candidates (useful for testing or reset).
        /// </summary>
        Task ClearAllCandidatesAsync();

        /// <summary>
        /// Save fetch progress for resuming later.
        /// </summary>
        Task SaveFetchProgressAsync(int targetWordCount, string? nextSearchAfter, int currentPage);

        /// <summary>
        /// Load saved fetch progress.
        /// </summary>
        Task<FetchProgress?> LoadFetchProgressAsync();

        /// <summary>
        /// Clear saved fetch progress.
        /// </summary>
        Task ClearFetchProgressAsync();
    }
}
