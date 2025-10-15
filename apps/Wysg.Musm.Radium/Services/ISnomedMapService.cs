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
    )
    {
        /// <summary>
        /// Extracts the semantic tag from FSN (text in parentheses at the end).
        /// Examples: "Body structure" from "Heart (body structure)", "Disorder" from "Myocardial infarction (disorder)"
        /// </summary>
        public string? GetSemanticTag()
        {
            if (string.IsNullOrWhiteSpace(Fsn)) return null;
            
            var lastOpenParen = Fsn.LastIndexOf('(');
            var lastCloseParen = Fsn.LastIndexOf(')');
            
            if (lastOpenParen >= 0 && lastCloseParen > lastOpenParen)
            {
                return Fsn.Substring(lastOpenParen + 1, lastCloseParen - lastOpenParen - 1).Trim();
            }
            
            return null;
        }
    }

    public interface ISnomedMapService
    {
        Task<IReadOnlyList<SnomedConcept>> SearchCachedConceptsAsync(string query, int limit = 50);
        Task<PhraseSnomedMapping?> GetMappingAsync(long phraseId);
        Task<bool> MapPhraseAsync(long phraseId, long? accountId, long conceptId, string mappingType = "exact", decimal? confidence = null, string? notes = null, long? mappedBy = null);
        Task CacheConceptAsync(SnomedConcept concept);
    }
}
