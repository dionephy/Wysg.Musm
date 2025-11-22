namespace Wysg.Musm.Radium.Api.Models.Dtos
{
    /// <summary>
    /// Account DTO for API responses
    /// </summary>
    public sealed class AccountDto
    {
        public long AccountId { get; set; }
        public string Uid { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
    }

    /// <summary>
    /// Create/Update account request
    /// </summary>
    public sealed class EnsureAccountRequest
    {
        public required string Uid { get; set; }
        public required string Email { get; set; }
        public string? DisplayName { get; set; }
    }

    /// <summary>
    /// Response containing account info and settings
    /// </summary>
    public sealed class EnsureAccountResponse
    {
        public required AccountDto Account { get; set; }
        public string? SettingsJson { get; set; }
    }
}
