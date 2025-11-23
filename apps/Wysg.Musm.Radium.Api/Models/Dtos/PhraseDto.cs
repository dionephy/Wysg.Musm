namespace Wysg.Musm.Radium.Api.Models.Dtos
{
    /// <summary>
    /// Phrase DTO for API responses (account-specific or global)
    /// </summary>
    public sealed class PhraseDto
    {
        public long Id { get; set; }
        public long? AccountId { get; set; } // NULL for global phrases
        public string Text { get; set; } = string.Empty;
        public bool Active { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public long Rev { get; set; }
        public string? Tags { get; set; }
        public string? TagsSource { get; set; }
        public string? TagsSemanticTag { get; set; }
    }

    /// <summary>
    /// Create/Update phrase request
    /// </summary>
    public sealed class UpsertPhraseRequest
    {
        public required string Text { get; set; }
        public bool Active { get; set; } = true;
        public string? Tags { get; set; } // Optional JSON tags
    }

    /// <summary>
    /// Batch upsert phrases request
    /// </summary>
    public sealed class BatchUpsertPhrasesRequest
    {
        public required List<string> Phrases { get; set; }
        public bool Active { get; set; } = true;
        public string? Tags { get; set; } // Optional JSON tags (applied to all)
    }

    /// <summary>
    /// Search phrases request
    /// </summary>
    public sealed class SearchPhrasesRequest
    {
        public string? Query { get; set; }
        public bool? ActiveOnly { get; set; }
        public string? SemanticTag { get; set; } // Filter by semantic tag
        public int MaxResults { get; set; } = 100;
    }

    /// <summary>
    /// Paginated response for phrases
    /// </summary>
    public sealed class PaginatedPhrasesResponse
    {
        public IReadOnlyList<PhraseDto> Data { get; set; } = Array.Empty<PhraseDto>();
        public PaginationInfo Pagination { get; set; } = new();
    }

    /// <summary>
    /// Pagination metadata
    /// </summary>
    public sealed class PaginationInfo
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
    }

    /// <summary>
    /// Sync response with changes since a revision
    /// </summary>
    public sealed class PhraseSyncResponse
    {
        public IReadOnlyList<PhraseDto> Changes { get; set; } = Array.Empty<PhraseDto>();
        public long LatestRev { get; set; }
        public int Count { get; set; }
    }
}

