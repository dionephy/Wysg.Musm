namespace Wysg.Musm.Radium.Api.Models.Dtos
{
    /// <summary>
    /// Snippet DTO for API responses
    /// </summary>
    public sealed class SnippetDto
    {
        public long SnippetId { get; set; }
        public long AccountId { get; set; }
        public string TriggerText { get; set; } = string.Empty;
        public string SnippetText { get; set; } = string.Empty;
        public string SnippetAst { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public DateTime UpdatedAt { get; set; }
        public long Rev { get; set; }
    }

    /// <summary>
    /// Create/Update snippet request
    /// </summary>
    public sealed class UpsertSnippetRequest
    {
        public required string TriggerText { get; set; }
        public required string SnippetText { get; set; }
        public required string SnippetAst { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
