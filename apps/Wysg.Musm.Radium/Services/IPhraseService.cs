using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Wysg.Musm.Radium.Services
{
    // SNOMED-enhanced phrase metadata (FR-SNOMED-2025-01-19)
    public record PhraseInfo(
        long Id, 
        long? AccountId, 
        string Text, 
        bool Active, 
        DateTime UpdatedAt, 
        long Rev,
        string? Tags = null,
        string? TagsSource = null,
        string? TagsSemanticTag = null
    );

    public interface IPhraseService
    {
        // Explicit one-time preload (fills in-memory snapshot; safe to call multiple times)
        Task PreloadAsync(long accountId);

        // Snapshot-backed list (account scope)
        Task<IReadOnlyList<string>> GetPhrasesForAccountAsync(long accountId);
        Task<IReadOnlyList<string>> GetPhrasesByPrefixAccountAsync(long accountId, string prefix, int limit = 50);

        // Global phrases (account_id IS NULL)
        Task<IReadOnlyList<string>> GetGlobalPhrasesAsync();
        Task<IReadOnlyList<string>> GetGlobalPhrasesByPrefixAsync(string prefix, int limit = 50);
        
        // Combined phrases (global + account-specific)
        Task<IReadOnlyList<string>> GetCombinedPhrasesAsync(long accountId);
        Task<IReadOnlyList<string>> GetCombinedPhrasesByPrefixAsync(long accountId, string prefix, int limit = 50);
        
        // Unfiltered combined phrases for syntax highlighting (includes long phrases)
        Task<IReadOnlyList<string>> GetAllPhrasesForHighlightingAsync(long accountId);

        // Backward compatibility wrappers (deprecated)
        [Obsolete("Use GetPhrasesForAccountAsync")] Task<IReadOnlyList<string>> GetPhrasesForTenantAsync(long tenantId);
        [Obsolete("Use GetPhrasesByPrefixAccountAsync")] Task<IReadOnlyList<string>> GetPhrasesByPrefixAsync(long tenantId, string prefix, int limit = 50);

        Task<IReadOnlyList<PhraseInfo>> GetAllPhraseMetaAsync(long accountId);
        Task<IReadOnlyList<PhraseInfo>> GetAllGlobalPhraseMetaAsync();
        
        // NEW (FR-280): List all non-global phrases across all accounts
        Task<IReadOnlyList<PhraseInfo>> GetAllNonGlobalPhraseMetaAsync(int take = 500);
        
        // Upsert with nullable accountId (null = global phrase)
        Task<PhraseInfo> UpsertPhraseAsync(long? accountId, string text, bool active = true);
        Task<PhraseInfo?> ToggleActiveAsync(long? accountId, long phraseId);
        
        // Update phrase text (FR-SNOMED-EDIT-2025-01-19)
        Task<PhraseInfo?> UpdatePhraseTextAsync(long? accountId, long phraseId, string newText);
        
        // Convert account phrases to global (FR-279)
        Task<(int converted, int duplicatesRemoved)> ConvertToGlobalPhrasesAsync(long accountId, IEnumerable<long> phraseIds);
        
        Task RefreshPhrasesAsync(long accountId);
        Task RefreshGlobalPhrasesAsync();
        
        Task<long?> GetAnyAccountIdAsync();
    }
}
