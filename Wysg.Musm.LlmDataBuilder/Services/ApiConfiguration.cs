using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Wysg.Musm.LlmDataBuilder.Services
{
    /// <summary>
    /// Configuration for API settings
    /// </summary>
    public class ApiConfiguration
    {
        private const string ConfigFileName = "api_config.json";

        [JsonPropertyName("apiUrl")]
        public string ApiUrl { get; set; } = "http://192.168.111.79:8081";

        [JsonPropertyName("authToken")]
        public string AuthToken { get; set; } = "change-me";

        [JsonPropertyName("defaultPrompt")]
        public string DefaultPrompt { get; set; } = "Proofread";

        [JsonPropertyName("language")]
        public string Language { get; set; } = "en";

        [JsonPropertyName("strictness")]
        public int Strictness { get; set; } = 4;

        /// <summary>
        /// Loads the API configuration from file or returns default values
        /// </summary>
        public static ApiConfiguration Load(string workingDirectory)
        {
            try
            {
                string configPath = Path.Combine(workingDirectory, ConfigFileName);
                if (File.Exists(configPath))
                {
                    string json = File.ReadAllText(configPath);
                    var config = JsonSerializer.Deserialize<ApiConfiguration>(json);
                    return config ?? new ApiConfiguration();
                }
            }
            catch
            {
                // If loading fails, return default configuration
            }

            return new ApiConfiguration();
        }

        /// <summary>
        /// Saves the API configuration to file
        /// </summary>
        public void Save(string workingDirectory)
        {
            try
            {
                string configPath = Path.Combine(workingDirectory, ConfigFileName);
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                string json = JsonSerializer.Serialize(this, options);
                File.WriteAllText(configPath, json);
            }
            catch
            {
                // Silently fail if save doesn't work
            }
        }
    }
}
