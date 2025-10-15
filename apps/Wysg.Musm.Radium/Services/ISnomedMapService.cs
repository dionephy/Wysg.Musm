using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Wysg.Musm.Radium.Services
{
    public sealed record SnomedConcept(long ConceptId, string ConceptIdStr, string Fsn, string? Pt, bool Active, DateTime CachedAt);

    public sealed record PhraseSnomedMapping(
        long PhraseId,
        long? AccountId,
        long ConceptId,
        string ConceptIdStr,
        string Fsn,
        string? Pt,
        string MappingType,
        decimal? Confidence,
        string? Notes,
        string Source, // "global" or "account"
        DateTime CreatedAt,
        DateTime UpdatedAt
    );

    public interface ISnomedMapService
    {
        Task<IReadOnlyList<SnomedConcept>> SearchCachedConceptsAsync(string query, int limit = 50);
        Task<PhraseSnomedMapping?> GetMappingAsync(long phraseId);
        Task<bool> MapPhraseAsync(long phraseId, long? accountId, long conceptId, string mappingType = "exact", decimal? confidence = null, string? notes = null, long? mappedBy = null);
    }
}
