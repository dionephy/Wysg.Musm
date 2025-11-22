using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Wysg.Musm.Radium.Services;

namespace Wysg.Musm.Radium.Services.Adapters
{
    /// <summary>
    /// Adapter that implements ISnomedMapService using the Radium API.
    /// Provides SNOMED concept caching and phrase-SNOMED mappings via REST API.
    /// </summary>
    public sealed class ApiSnomedMapService : ISnomedMapService
    {
        private readonly RadiumApiClient _apiClient;

        public ApiSnomedMapService(RadiumApiClient apiClient)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        }

        public async Task<IReadOnlyList<SnomedConcept>> SearchCachedConceptsAsync(string query, int limit = 50)
        {
            // NOT IMPLEMENTED: This feature requires direct Snowstorm or database access
            // The API doesn't expose concept search (only cache/retrieve by ID)
            // Clients should continue using SnowstormClient for search
            throw new NotSupportedException("Concept search not available via API. Use SnowstormClient directly.");
        }

        public async Task<PhraseSnomedMapping?> GetMappingAsync(long phraseId)
        {
            var dto = await _apiClient.GetPhraseSnomedMappingAsync(phraseId);
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
            var dtoDict = await _apiClient.GetPhraseSnomedMappingsBatchAsync(phraseIds);
            
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
                var request = new CreateSnomedMappingRequest
                {
                    PhraseId = phraseId,
                    AccountId = accountId,
                    ConceptId = conceptId,
                    MappingType = mappingType,
                    Confidence = confidence,
                    Notes = notes,
                    MappedBy = mappedBy
                };

                await _apiClient.CreatePhraseSnomedMappingAsync(request);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ApiSnomedMapService] Error mapping phrase {phraseId}: {ex.Message}");
                return false;
            }
        }

        public async Task CacheConceptAsync(SnomedConcept concept)
        {
            var dto = new SnomedConceptDto
            {
                ConceptId = concept.ConceptId,
                ConceptIdStr = concept.ConceptIdStr,
                Fsn = concept.Fsn,
                Pt = concept.Pt,
                SemanticTag = ExtractSemanticTag(concept.Fsn),
                Active = concept.Active,
                CachedAt = concept.CachedAt
            };

            await _apiClient.CacheSnomedConceptAsync(dto);
        }

        /// <summary>
        /// Extract semantic tag from FSN (text in parentheses at the end).
        /// Example: "Heart structure (body structure)" ¡æ "body structure"
        /// </summary>
        private static string? ExtractSemanticTag(string? fsn)
        {
            if (string.IsNullOrWhiteSpace(fsn))
                return null;

            var lastOpenParen = fsn.LastIndexOf('(');
            var lastCloseParen = fsn.LastIndexOf(')');

            if (lastOpenParen >= 0 && lastCloseParen > lastOpenParen)
            {
                return fsn.Substring(lastOpenParen + 1, lastCloseParen - lastOpenParen - 1).Trim();
            }

            return null;
        }
    }
}
