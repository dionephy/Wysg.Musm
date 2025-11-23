namespace Wysg.Musm.Radium.Api.Models.Dtos
{
    /// <summary>
    /// User Settings DTO for API responses
    /// </summary>
    public sealed class UserSettingDto
    {
        public long AccountId { get; set; }
        public string SettingsJson { get; set; } = "{}";
        public DateTime UpdatedAt { get; set; }
        public long Rev { get; set; }
    }

    /// <summary>
    /// Update user settings request
    /// </summary>
    public sealed class UpdateUserSettingRequest
    {
        public required string SettingsJson { get; set; }
    }
}
