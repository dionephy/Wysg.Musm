namespace Wysg.Musm.Radium.Api.Models.Requests;

/// <summary>
/// Request to create a mapping between a phrase and a SNOMED concept.
/// </summary>
public sealed class CreateMappingRequest
{
    /// <summary>
    /// Phrase ID to map.
    /// </summary>
    public long PhraseId { get; set; }

    /// <summary>
    /// Account ID (null for global phrases).
    /// </summary>
    public long? AccountId { get; set; }

    /// <summary>
    /// SNOMED concept ID to map to.
    /// </summary>
    public long ConceptId { get; set; }

    /// <summary>
    /// Mapping type: "exact", "broader", "narrower", "related".
    /// </summary>
    public string MappingType { get; set; } = "exact";

    /// <summary>
    /// Optional confidence score (0.0 to 1.0).
    /// </summary>
    public decimal? Confidence { get; set; }

    /// <summary>
    /// Optional notes about the mapping.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Who created the mapping (for global mappings only).
    /// </summary>
    public long? MappedBy { get; set; }
}
