namespace Wysg.Musm.Radium.Api.Models.Dtos;

/// <summary>
/// Represents a mapping between a phrase and a SNOMED CT concept.
/// Used for semantic tagging and phrase coloring in the editor.
/// </summary>
public sealed class PhraseSnomedMappingDto
{
    /// <summary>
    /// Phrase ID.
    /// </summary>
    public long PhraseId { get; set; }

    /// <summary>
    /// Account ID (null for global phrases).
    /// </summary>
    public long? AccountId { get; set; }

    /// <summary>
    /// SNOMED concept ID.
    /// </summary>
    public long ConceptId { get; set; }

    /// <summary>
    /// SNOMED concept ID (string).
    /// </summary>
    public string ConceptIdStr { get; set; } = string.Empty;

    /// <summary>
    /// Fully Specified Name from concept cache.
    /// Example: "Heart structure (body structure)"
    /// </summary>
    public string Fsn { get; set; } = string.Empty;

    /// <summary>
    /// Preferred Term from concept cache.
    /// </summary>
    public string? Pt { get; set; }

    /// <summary>
    /// Semantic tag extracted from FSN.
    /// Example: "body structure", "disorder", "finding"
    /// Used for syntax highlighting color coding.
    /// </summary>
    public string? SemanticTag { get; set; }

    /// <summary>
    /// Mapping type: "exact", "broader", "narrower", "related".
    /// </summary>
    public string MappingType { get; set; } = "exact";

    /// <summary>
    /// Confidence score (0.0 to 1.0).
    /// </summary>
    public decimal? Confidence { get; set; }

    /// <summary>
    /// Optional notes about the mapping.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Source of mapping: "global" or "account".
    /// </summary>
    public string Source { get; set; } = "global";

    /// <summary>
    /// When the mapping was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the mapping was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}
