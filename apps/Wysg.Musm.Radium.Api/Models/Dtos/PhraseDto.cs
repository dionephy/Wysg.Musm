namespace Wysg.Musm.Radium.Api.Models.Dtos
{
    /// <summary>
    /// Phrase DTO for API responses
    /// </summary>
    public sealed class PhraseDto
    {
        public long Id { get; set; }
        public long AccountId { get; set; }
        public string Text { get; set; } = string.Empty;
        public bool Active { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public long Rev { get; set; }
    }

    /// <summary>
    /// Create/Update phrase request
    /// </summary>
    public sealed class UpsertPhraseRequest
    {
        public required string Text { get; set; }
        public bool Active { get; set; } = true;
    }

    /// <summary>
    /// Batch upsert phrases request
    /// </summary>
    public sealed class BatchUpsertPhrasesRequest
    {
        public required List<string> Phrases { get; set; }
        public bool Active { get; set; } = true;
    }

    /// <summary>
    /// Search phrases request
    /// </summary>
    public sealed class SearchPhrasesRequest
    {
        public string? Query { get; set; }
        public bool? ActiveOnly { get; set; }
        public int MaxResults { get; set; } = 100;
    }
}
