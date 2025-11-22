using Wysg.Musm.Radium.Api.Models.Dtos;

namespace Wysg.Musm.Radium.Api.Repositories;

/// <summary>
/// Repository for SNOMED CT concept caching and phrase-SNOMED mappings.
/// </summary>
public interface ISnomedRepository
{
    /// <summary>
    /// Cache a SNOMED concept (upsert).
    /// </summary>
    Task CacheConceptAsync(SnomedConceptDto concept);

    /// <summary>
    /// Get a cached SNOMED concept by ID.
    /// </summary>
    Task<SnomedConceptDto?> GetConceptAsync(long conceptId);

    /// <summary>
    /// Create a mapping between a phrase and a SNOMED concept.
    /// </summary>
    /// <param name="phraseId">Phrase ID</param>
    /// <param name="accountId">Account ID (null for global phrases)</param>
    /// <param name="conceptId">SNOMED concept ID</param>
    /// <param name="mappingType">Mapping type (exact, broader, narrower, related)</param>
    /// <param name="confidence">Confidence score (0.0 to 1.0)</param>
    /// <param name="notes">Optional notes</param>
    /// <param name="mappedBy">Who created the mapping (for global mappings)</param>
    Task CreateMappingAsync(
        long phraseId,
        long? accountId,
        long conceptId,
        string mappingType = "exact",
        decimal? confidence = null,
        string? notes = null,
        long? mappedBy = null);

    /// <summary>
    /// Get a single phrase-SNOMED mapping.
    /// </summary>
    Task<PhraseSnomedMappingDto?> GetMappingAsync(long phraseId);

    /// <summary>
    /// Get multiple phrase-SNOMED mappings in a single query (batch operation).
    /// Used for loading semantic tags for syntax highlighting.
    /// </summary>
    Task<Dictionary<long, PhraseSnomedMappingDto>> GetMappingsBatchAsync(IEnumerable<long> phraseIds);

    /// <summary>
    /// Delete a phrase-SNOMED mapping.
    /// </summary>
    Task DeleteMappingAsync(long phraseId);
}
