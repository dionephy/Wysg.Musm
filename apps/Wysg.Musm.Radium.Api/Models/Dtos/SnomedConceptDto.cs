namespace Wysg.Musm.Radium.Api.Models.Dtos;

/// <summary>
/// SNOMED CT concept DTO for caching.
/// </summary>
public sealed class SnomedConceptDto
{
    /// <summary>
    /// SNOMED concept ID (numeric).
    /// </summary>
    public long ConceptId { get; set; }

    /// <summary>
    /// SNOMED concept ID (string representation).
    /// </summary>
    public string ConceptIdStr { get; set; } = string.Empty;

    /// <summary>
    /// Fully Specified Name (FSN) - includes semantic tag in parentheses.
    /// Example: "Heart structure (body structure)"
    /// </summary>
    public string Fsn { get; set; } = string.Empty;

    /// <summary>
    /// Preferred Term (PT) - shorter, user-friendly term.
    /// Example: "Heart structure"
    /// </summary>
    public string? Pt { get; set; }

    /// <summary>
    /// Semantic tag extracted from FSN (computed field).
    /// Example: "body structure", "disorder", "procedure"
    /// </summary>
    public string? SemanticTag { get; set; }

    /// <summary>
    /// SNOMED module ID.
    /// </summary>
    public string? ModuleId { get; set; }

    /// <summary>
    /// Whether the concept is active in SNOMED CT.
    /// </summary>
    public bool Active { get; set; } = true;

    /// <summary>
    /// When this concept was cached in the database.
    /// </summary>
    public DateTime CachedAt { get; set; }

    /// <summary>
    /// Optional expiration time for cache invalidation.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
}
