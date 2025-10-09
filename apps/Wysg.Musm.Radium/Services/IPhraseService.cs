using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Wysg.Musm.Radium.Services
{
    public record PhraseInfo(long Id, long? AccountId, string Text, bool Active, DateTime UpdatedAt, long Rev);

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
        
        // Convert account phrases to global (FR-279)
        Task<(int converted, int duplicatesRemoved)> ConvertToGlobalPhrasesAsync(long accountId, IEnumerable<long> phraseIds);
        
        Task RefreshPhrasesAsync(long accountId);
        Task RefreshGlobalPhrasesAsync();
        
        Task<long?> GetAnyAccountIdAsync();
    }
}
