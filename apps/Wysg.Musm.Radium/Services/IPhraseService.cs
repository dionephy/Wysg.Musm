using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Wysg.Musm.Radium.Services
{
    public record PhraseInfo(long Id, long AccountId, string Text, bool Active, DateTime UpdatedAt, long Rev);

    public interface IPhraseService
    {
        // Snapshot-backed list (account scope)
        Task<IReadOnlyList<string>> GetPhrasesForAccountAsync(long accountId);
        Task<IReadOnlyList<string>> GetPhrasesByPrefixAccountAsync(long accountId, string prefix, int limit = 50);

        // Backward compatibility wrappers (deprecated)
        [Obsolete("Use GetPhrasesForAccountAsync")] Task<IReadOnlyList<string>> GetPhrasesForTenantAsync(long tenantId);
        [Obsolete("Use GetPhrasesByPrefixAccountAsync")] Task<IReadOnlyList<string>> GetPhrasesByPrefixAsync(long tenantId, string prefix, int limit = 50);

        Task<IReadOnlyList<PhraseInfo>> GetAllPhraseMetaAsync(long accountId);
        Task<PhraseInfo> UpsertPhraseAsync(long accountId, string text, bool active = true);
        Task<PhraseInfo?> ToggleActiveAsync(long accountId, long phraseId);
        Task<long?> GetAnyAccountIdAsync();
    }
}
