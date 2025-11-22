namespace Wysg.Musm.Radium.Api.Models.Dtos
{
    /// <summary>
    /// Hotkey DTO for API responses
    /// </summary>
    public sealed class HotkeyDto
    {
        public long HotkeyId { get; set; }
        public long AccountId { get; set; }
        public string TriggerText { get; set; } = string.Empty;
        public string ExpansionText { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public DateTime UpdatedAt { get; set; }
        public long Rev { get; set; }
    }

    /// <summary>
    /// Create/Update hotkey request
    /// </summary>
    public sealed class UpsertHotkeyRequest
    {
        public required string TriggerText { get; set; }
        public required string ExpansionText { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
