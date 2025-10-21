using System.Collections.Generic;
using System.Threading.Tasks;

namespace Wysg.Musm.Radium.Services
{
    public interface ISnowstormClient
    {
        Task<IReadOnlyList<SnomedConcept>> SearchConceptsAsync(string query, int limit = 50);
        
        /// <summary>
        /// Browse SNOMED concepts by semantic tag (domain) with pagination support.
        /// Returns concepts with ALL their terms/descriptions (FSN, PT, synonyms).
        /// </summary>
        /// <param name="semanticTag">Semantic tag filter (e.g., "body structure", "finding", "disorder")</param>
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
