using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Wysg.Musm.Radium.Services.ApiClients;

namespace Wysg.Musm.Radium.Services.Adapters
{
    /// <summary>
    /// Adapter that implements ISnomedMapService using the API client.
    /// Enables phrase colorizing and SNOMED features to work via REST API instead of direct database access.
    /// </summary>
    public sealed class ApiSnomedMapServiceAdapter : ISnomedMapService
    {
        private readonly ISnomedApiClient _apiClient;

        public ApiSnomedMapServiceAdapter(ISnomedApiClient apiClient)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        }

        public async Task<IReadOnlyList<SnomedConcept>> SearchCachedConceptsAsync(string query, int limit = 50)
        {
            // Note: The API doesn't have a search endpoint yet, so this returns empty for now.
            // If needed, add a search endpoint to the API in the future.
            return Array.Empty<SnomedConcept>();
        }

        public async Task<PhraseSnomedMapping?> GetMappingAsync(long phraseId)
        {
            var dto = await _apiClient.GetMappingAsync(phraseId);
            if (dto == null)
                return null;

            return new PhraseSnomedMapping(
                dto.PhraseId,
                dto.AccountId,
                dto.ConceptId,
                dto.ConceptIdStr,
                dto.Fsn,
                dto.Pt,
                dto.MappingType,
                dto.Confidence,
                dto.Notes,
                dto.Source,
                dto.CreatedAt,
                dto.UpdatedAt
            );
        }

        public async Task<IReadOnlyDictionary<long, PhraseSnomedMapping>> GetMappingsBatchAsync(IEnumerable<long> phraseIds)
        {
            var dtoDict = await _apiClient.GetMappingsBatchAsync(phraseIds);
            var result = new Dictionary<long, PhraseSnomedMapping>();

            foreach (var kvp in dtoDict)
            {
                var dto = kvp.Value;
                result[kvp.Key] = new PhraseSnomedMapping(
                    dto.PhraseId,
                    dto.AccountId,
                    dto.ConceptId,
                    dto.ConceptIdStr,
                    dto.Fsn,
                    dto.Pt,
                    dto.MappingType,
                    dto.Confidence,
                    dto.Notes,
                    dto.Source,
                    dto.CreatedAt,
                    dto.UpdatedAt
                );
            }

            return result;
        }

        public async Task<bool> MapPhraseAsync(long phraseId, long? accountId, long conceptId, string mappingType = "exact", decimal? confidence = null, string? notes = null, long? mappedBy = null)
        {
            try
            {
                var request = new CreateMappingRequest
                {
                    PhraseId = phraseId,
                    AccountId = accountId,
                    ConceptId = conceptId,
                    MappingType = mappingType,
                    Confidence = confidence,
                    Notes = notes,
                    MappedBy = mappedBy
                };

                await _apiClient.CreateMappingAsync(request);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task CacheConceptAsync(SnomedConcept concept)
        {
            var dto = new ApiClients.SnomedConceptDto
            {
                ConceptId = concept.ConceptId,
                ConceptIdStr = concept.ConceptIdStr,
                Fsn = concept.Fsn,
                Pt = concept.Pt,
                Active = concept.Active,
                CachedAt = concept.CachedAt
            };

            await _apiClient.CacheConceptAsync(dto);
        }
    }
}
