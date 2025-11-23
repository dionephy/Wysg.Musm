using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Wysg.Musm.Radium.Services.ApiClients
{
    /// <summary>
    /// SNOMED Concept DTO (matches API)
    /// </summary>
    public class SnomedConceptDto
    {
        public long ConceptId { get; set; }
        public string ConceptIdStr { get; set; } = string.Empty;
        public string Fsn { get; set; } = string.Empty;
        public string? Pt { get; set; }
        public string? SemanticTag { get; set; }
        public string? ModuleId { get; set; }
        public bool Active { get; set; }
        public DateTime CachedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }

    /// <summary>
    /// Phrase-SNOMED mapping DTO (matches API)
    /// </summary>
    public class PhraseSnomedMappingDto
    {
        public long PhraseId { get; set; }
        public long? AccountId { get; set; }
        public long ConceptId { get; set; }
        public string ConceptIdStr { get; set; } = string.Empty;
        public string Fsn { get; set; } = string.Empty;
        public string? Pt { get; set; }
        public string? SemanticTag { get; set; }
        public string MappingType { get; set; } = "exact";
        public decimal? Confidence { get; set; }
        public string? Notes { get; set; }
        public string Source { get; set; } = "account";
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Create mapping request
    /// </summary>
    public class CreateMappingRequest
    {
        public long PhraseId { get; set; }
        public long? AccountId { get; set; }
        public long ConceptId { get; set; }
        public string MappingType { get; set; } = "exact";
        public decimal? Confidence { get; set; }
        public string? Notes { get; set; }
        public long? MappedBy { get; set; }
    }

    /// <summary>
    /// API client for SNOMED operations
    /// </summary>
    public interface ISnomedApiClient
    {
        Task CacheConceptAsync(SnomedConceptDto concept);
        Task<SnomedConceptDto?> GetConceptAsync(long conceptId);
        Task CreateMappingAsync(CreateMappingRequest request);
        Task<PhraseSnomedMappingDto?> GetMappingAsync(long phraseId);
        Task<Dictionary<long, PhraseSnomedMappingDto>> GetMappingsBatchAsync(IEnumerable<long> phraseIds);
        Task DeleteMappingAsync(long phraseId);
        Task<bool> IsAvailableAsync();
    }

    /// <summary>
    /// Implementation of SNOMED API client
    /// </summary>
    public class SnomedApiClient : ApiClientBase, ISnomedApiClient
    {
        public SnomedApiClient(HttpClient httpClient, string baseUrl)
            : base(httpClient, baseUrl)
        {
        }

        public async Task CacheConceptAsync(SnomedConceptDto concept)
        {
            if (concept == null)
            {
                throw new ArgumentNullException(nameof(concept));
            }

            try
            {
                Debug.WriteLine($"[SnomedApiClient] Caching concept {concept.ConceptId}");
                await PostAsync("/api/snomed/concepts", concept);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SnomedApiClient] Error caching concept: {ex.Message}");
                throw;
            }
        }

        public async Task<SnomedConceptDto?> GetConceptAsync(long conceptId)
        {
            try
            {
                Debug.WriteLine($"[SnomedApiClient] Getting concept {conceptId}");
                return await GetAsync<SnomedConceptDto>($"/api/snomed/concepts/{conceptId}");
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                Debug.WriteLine($"[SnomedApiClient] Concept {conceptId} not found");
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SnomedApiClient] Error getting concept: {ex.Message}");
                throw;
            }
        }

        public async Task CreateMappingAsync(CreateMappingRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            try
            {
                Debug.WriteLine($"[SnomedApiClient] Creating mapping for phrase {request.PhraseId}");
                await PostAsync("/api/snomed/mappings", request);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SnomedApiClient] Error creating mapping: {ex.Message}");
                throw;
            }
        }

        public async Task<PhraseSnomedMappingDto?> GetMappingAsync(long phraseId)
        {
            try
            {
                Debug.WriteLine($"[SnomedApiClient] Getting mapping for phrase {phraseId}");
                return await GetAsync<PhraseSnomedMappingDto>($"/api/snomed/mappings/{phraseId}");
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                Debug.WriteLine($"[SnomedApiClient] Mapping for phrase {phraseId} not found");
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SnomedApiClient] Error getting mapping: {ex.Message}");
                throw;
            }
        }

        public async Task<Dictionary<long, PhraseSnomedMappingDto>> GetMappingsBatchAsync(IEnumerable<long> phraseIds)
        {
            if (phraseIds == null)
            {
                throw new ArgumentNullException(nameof(phraseIds));
            }

            try
            {
                var idsArray = phraseIds as long[] ?? phraseIds.ToArray();
                if (idsArray.Length == 0)
                {
                    return new Dictionary<long, PhraseSnomedMappingDto>();
                }

                Debug.WriteLine($"[SnomedApiClient] Getting mappings for {idsArray.Length} phrases via POST");
                
                // Use POST to avoid URI too long error with large batches
                var request = new { phraseIds = idsArray };
                return await PostAsync<Dictionary<long, PhraseSnomedMappingDto>>("/api/snomed/mappings/batch", request) 
                       ?? new Dictionary<long, PhraseSnomedMappingDto>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SnomedApiClient] Error getting batch mappings: {ex.Message}");
                throw;
            }
        }

        public async Task DeleteMappingAsync(long phraseId)
        {
            try
            {
                Debug.WriteLine($"[SnomedApiClient] Deleting mapping for phrase {phraseId}");
                await DeleteAsync($"/api/snomed/mappings/{phraseId}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SnomedApiClient] Error deleting mapping: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> IsAvailableAsync()
        {
            return await IsApiAvailableAsync();
        }
    }
}
