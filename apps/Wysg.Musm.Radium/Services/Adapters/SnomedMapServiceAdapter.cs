using System.Threading.Tasks;
using Wysg.Musm.SnomedTools.Abstractions;

namespace Wysg.Musm.Radium.Services.Adapters
{
    /// <summary>
    /// Adapter that bridges Radium's ISnomedMapService to SnomedTools' ISnomedMapService interface.
    /// </summary>
    public sealed class SnomedMapServiceAdapter : Wysg.Musm.SnomedTools.Abstractions.ISnomedMapService
    {
        private readonly Wysg.Musm.Radium.Services.ISnomedMapService _radiumService;

        public SnomedMapServiceAdapter(Wysg.Musm.Radium.Services.ISnomedMapService radiumService)
        {
            _radiumService = radiumService;
        }

        public async Task CacheConceptAsync(Wysg.Musm.SnomedTools.Abstractions.SnomedConcept concept)
        {
            // Convert from SnomedTools type to Radium type
            var radiumConcept = new Wysg.Musm.Radium.Services.SnomedConcept(
                concept.ConceptId,
                concept.ConceptIdStr,
                concept.Fsn,
                concept.Pt,
                concept.Active,
                concept.CachedAt
            );

            await _radiumService.CacheConceptAsync(radiumConcept);
        }

        public Task<bool> MapPhraseAsync(
            long phraseId,
            long? accountId,
            long conceptId,
            string mappingType = "exact",
            decimal? confidence = null,
            string? notes = null,
            long? mappedBy = null)
        {
            return _radiumService.MapPhraseAsync(phraseId, accountId, conceptId, mappingType, confidence, notes, mappedBy);
        }
    }
}
