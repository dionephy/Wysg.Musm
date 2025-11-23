using System;

namespace Wysg.Musm.Radium.Configuration
{
    /// <summary>
    /// API configuration settings
    /// </summary>
    public class ApiSettings
    {
        /// <summary>
        /// Base URL for the Radium API
        /// </summary>
        public string BaseUrl { get; set; } = "http://localhost:5205";

        /// <summary>
        /// HTTP request timeout in seconds
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Enable retry on transient failures
        /// </summary>
        public bool EnableRetry { get; set; } = true;

        /// <summary>
        /// Maximum retry attempts
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>
        /// Enable offline caching
        /// </summary>
        public bool EnableOfflineCache { get; set; } = true;

        /// <summary>
        /// Cache duration in minutes
        /// </summary>
        public int CacheDurationMinutes { get; set; } = 5;
    }
}
