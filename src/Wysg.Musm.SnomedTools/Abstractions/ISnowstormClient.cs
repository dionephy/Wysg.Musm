using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Wysg.Musm.SnomedTools.Abstractions
{
    /// <summary>
    /// Represents a SNOMED-CT concept with basic information.
    /// </summary>
    public sealed record SnomedConcept(
 long ConceptId,
   string ConceptIdStr,
   string Fsn,
        string? Pt,
        bool Active,
        DateTime CachedAt);

    /// <summary>
    /// Represents a SNOMED-CT concept with all its terms/descriptions.
    /// </summary>
    public sealed record SnomedConceptWithTerms(
        long ConceptId,
  string ConceptIdStr,
        string Fsn,
    string? Pt,
        bool Active,
        DateTime CachedAt,
IReadOnlyList<SnomedTerm> AllTerms);

    /// <summary>
    /// Represents a single term/description for a SNOMED concept.
    /// </summary>
    public sealed record SnomedTerm(string Term, string Type);

    /// <summary>
    /// Service for interacting with SNOMED-CT terminology server (e.g., Snowstorm).
    /// </summary>
    public interface ISnowstormClient
    {
        /// <summary>
        /// Browse SNOMED concepts by semantic tag (domain) with pagination support.
      /// Returns concepts with ALL their terms/descriptions (FSN, PT, synonyms).
        /// </summary>
        /// <param name="semanticTag">Semantic tag filter (e.g., "body structure", "finding", "disorder", "all")</param>
        /// <param name="offset">Number of results to skip (for pagination)</param>
        /// <param name="limit">Maximum number of results to return</param>
        /// <param name="searchAfterToken">Optional searchAfter token from previous page for efficient pagination</param>
        /// <returns>Tuple containing list of SNOMED concepts with all terms, and the next searchAfter token</returns>
        Task<(IReadOnlyList<SnomedConceptWithTerms> concepts, string? nextSearchAfter)> BrowseBySemanticTagAsync(
       string semanticTag,
     int offset = 0,
            int limit = 10,
string? searchAfterToken = null);
    }
}
