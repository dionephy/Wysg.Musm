using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Wysg.Musm.SnomedTools.Abstractions;

namespace Wysg.Musm.Radium.Services.Adapters
{
    /// <summary>
    /// Adapter that bridges Radium's ISnowstormClient to SnomedTools' ISnowstormClient interface.
    /// </summary>
    public sealed class SnowstormClientAdapter : Wysg.Musm.SnomedTools.Abstractions.ISnowstormClient
    {
        private readonly Wysg.Musm.Radium.Services.ISnowstormClient _radiumClient;

        public SnowstormClientAdapter(Wysg.Musm.Radium.Services.ISnowstormClient radiumClient)
        {
            _radiumClient = radiumClient;
        }

        public async Task<(IReadOnlyList<Wysg.Musm.SnomedTools.Abstractions.SnomedConceptWithTerms> concepts, string? nextSearchAfter)> BrowseBySemanticTagAsync(
            string semanticTag,
            int offset = 0,
            int limit = 10,
            string? searchAfterToken = null)
        {
            // Call Radium's implementation
            var (concepts, token) = await _radiumClient.BrowseBySemanticTagAsync(semanticTag, offset, limit, searchAfterToken);

            // Convert from Radium types to SnomedTools types
            var convertedConcepts = concepts.Select(concept =>
                new Wysg.Musm.SnomedTools.Abstractions.SnomedConceptWithTerms(
                    concept.ConceptId,
                    concept.ConceptIdStr,
                    concept.Fsn,
                    concept.Pt,
                    concept.Active,
                    concept.CachedAt,
                    concept.AllTerms.Select(term => new Wysg.Musm.SnomedTools.Abstractions.SnomedTerm(term.Term, term.Type)).ToList()
                )).ToList();

            return (convertedConcepts, token);
        }
    }
}
