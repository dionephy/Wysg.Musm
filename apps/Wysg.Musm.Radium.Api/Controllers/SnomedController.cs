using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wysg.Musm.Radium.Api.Models.Dtos;
using Wysg.Musm.Radium.Api.Models.Requests;
using Wysg.Musm.Radium.Api.Repositories;

namespace Wysg.Musm.Radium.Api.Controllers;

/// <summary>
/// SNOMED CT concept caching and phrase-SNOMED mapping endpoints.
/// Used for semantic tagging and syntax highlighting in the editor.
/// </summary>
[ApiController]
[Route("api/snomed")]
[Authorize]
public class SnomedController : ControllerBase
{
    private readonly ISnomedRepository _repository;
    private readonly ILogger<SnomedController> _logger;

    public SnomedController(ISnomedRepository repository, ILogger<SnomedController> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Cache a SNOMED CT concept (upsert).
    /// </summary>
    /// <remarks>
    /// Stores a SNOMED concept in the local cache for mapping to phrases.
    /// Called before creating phrase-SNOMED mappings.
    /// </remarks>
    [HttpPost("concepts")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CacheConcept([FromBody] SnomedConceptDto dto)
    {
        if (dto == null)
            return BadRequest("Concept data is required");

        if (dto.ConceptId <= 0)
            return BadRequest("Invalid concept ID");

        if (string.IsNullOrWhiteSpace(dto.ConceptIdStr))
            return BadRequest("Concept ID string is required");

        if (string.IsNullOrWhiteSpace(dto.Fsn))
            return BadRequest("FSN is required");

        try
        {
            await _repository.CacheConceptAsync(dto);
            _logger.LogInformation("Cached SNOMED concept {ConceptId}", dto.ConceptId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error caching SNOMED concept {ConceptId}", dto.ConceptId);
            return StatusCode(500, "Failed to cache concept");
        }
    }

    /// <summary>
    /// Get a cached SNOMED concept by ID.
    /// </summary>
    [HttpGet("concepts/{conceptId}")]
    [ProducesResponseType(typeof(SnomedConceptDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SnomedConceptDto>> GetConcept(long conceptId)
    {
        if (conceptId <= 0)
            return BadRequest("Invalid concept ID");

        try
        {
            var concept = await _repository.GetConceptAsync(conceptId);
            if (concept == null)
                return NotFound();

            return Ok(concept);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving SNOMED concept {ConceptId}", conceptId);
            return StatusCode(500, "Failed to retrieve concept");
        }
    }

    /// <summary>
    /// Create a mapping between a phrase and a SNOMED concept.
    /// </summary>
    /// <remarks>
    /// Maps a phrase to a SNOMED CT concept for semantic tagging.
    /// The concept must already be cached (use POST /api/snomed/concepts first).
    /// </remarks>
    [HttpPost("mappings")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateMapping([FromBody] CreateMappingRequest request)
    {
        if (request == null)
            return BadRequest("Mapping request is required");

        if (request.PhraseId <= 0)
            return BadRequest("Invalid phrase ID");

        if (request.ConceptId <= 0)
            return BadRequest("Invalid concept ID");

        var validMappingTypes = new[] { "exact", "broader", "narrower", "related" };
        if (!validMappingTypes.Contains(request.MappingType?.ToLowerInvariant()))
            return BadRequest("Invalid mapping type. Must be: exact, broader, narrower, or related");

        if (request.Confidence.HasValue && (request.Confidence < 0 || request.Confidence > 1))
            return BadRequest("Confidence must be between 0.0 and 1.0");

        try
        {
            await _repository.CreateMappingAsync(
                request.PhraseId,
                request.AccountId,
                request.ConceptId,
                request.MappingType ?? "exact", // Provide default if null
                request.Confidence,
                request.Notes,
                request.MappedBy
            );

            _logger.LogInformation(
                "Created mapping: Phrase {PhraseId} ¡æ Concept {ConceptId} (type: {MappingType})",
                request.PhraseId, request.ConceptId, request.MappingType);

            return NoContent();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found in cache"))
        {
            _logger.LogWarning(ex, "Concept not cached: {ConceptId}", request.ConceptId);
            return NotFound("SNOMED concept not found in cache. Cache the concept first.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating mapping for phrase {PhraseId}", request.PhraseId);
            return StatusCode(500, "Failed to create mapping");
        }
    }

    /// <summary>
    /// Get a single phrase-SNOMED mapping.
    /// </summary>
    [HttpGet("mappings/{phraseId}")]
    [ProducesResponseType(typeof(PhraseSnomedMappingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PhraseSnomedMappingDto>> GetMapping(long phraseId)
    {
        if (phraseId <= 0)
            return BadRequest("Invalid phrase ID");

        try
        {
            var mapping = await _repository.GetMappingAsync(phraseId);
            if (mapping == null)
                return NotFound();

            return Ok(mapping);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving mapping for phrase {PhraseId}", phraseId);
            return StatusCode(500, "Failed to retrieve mapping");
        }
    }

    /// <summary>
    /// Get multiple phrase-SNOMED mappings in batch (for syntax highlighting).
    /// </summary>
    /// <remarks>
    /// Efficiently retrieves mappings for multiple phrases in a single query.
    /// Used by the editor to load semantic tags for syntax highlighting.
    /// 
    /// Example: GET /api/snomed/mappings?phraseIds=1&amp;phraseIds=2&amp;phraseIds=3
    /// </remarks>
    [HttpGet("mappings")]
    [ProducesResponseType(typeof(Dictionary<long, PhraseSnomedMappingDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Dictionary<long, PhraseSnomedMappingDto>>> GetMappingsBatch([FromQuery] long[] phraseIds)
    {
        if (phraseIds == null || phraseIds.Length == 0)
            return Ok(new Dictionary<long, PhraseSnomedMappingDto>());

        try
        {
            var mappings = await _repository.GetMappingsBatchAsync(phraseIds);
            _logger.LogInformation("Retrieved {Count} mappings for {Total} phrases", mappings.Count, phraseIds.Length);
            return Ok(mappings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving batch mappings for {Count} phrases", phraseIds.Length);
            return StatusCode(500, "Failed to retrieve mappings");
        }
    }

    /// <summary>
    /// Delete a phrase-SNOMED mapping.
    /// </summary>
    [HttpDelete("mappings/{phraseId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteMapping(long phraseId)
    {
        if (phraseId <= 0)
            return BadRequest("Invalid phrase ID");

        try
        {
            await _repository.DeleteMappingAsync(phraseId);
            _logger.LogInformation("Deleted mapping for phrase {PhraseId}", phraseId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting mapping for phrase {PhraseId}", phraseId);
            return StatusCode(500, "Failed to delete mapping");
        }
    }
}
