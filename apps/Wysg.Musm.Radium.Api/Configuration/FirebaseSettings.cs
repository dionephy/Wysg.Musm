namespace Wysg.Musm.Radium.Api.Configuration
{
    /// <summary>
    /// Firebase authentication settings
    /// </summary>
    public sealed class FirebaseSettings
    {
        /// <summary>
        /// Firebase project ID (e.g., "wysg-musm")
        /// </summary>
        public string ProjectId { get; set; } = string.Empty;

        /// <summary>
        /// Optional: Firebase API key (for admin operations)
        /// </summary>
        public string? ApiKey { get; set; }

        /// <summary>
        /// Optional: Custom audience claim validation
        /// </summary>
        public string? Audience { get; set; }

        /// <summary>
        /// Whether to validate Firebase tokens (set to false for development/testing)
        /// </summary>
        public bool ValidateToken { get; set; } = true;
    }
}
