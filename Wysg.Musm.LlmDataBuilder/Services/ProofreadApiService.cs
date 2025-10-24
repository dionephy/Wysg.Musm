using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Wysg.Musm.LlmDataBuilder.Services
{
    /// <summary>
    /// Service for calling the proofreading API
    /// </summary>
    public class ProofreadApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiUrl;
        private readonly string _authToken;

        public ProofreadApiService(string apiUrl = "http://192.168.111.79:8081", string authToken = "change-me")
        {
            _httpClient = new HttpClient();
            _apiUrl = apiUrl;
            _authToken = authToken;
        }

        /// <summary>
        /// Calls the proofreading API with the given prompt and candidate text
        /// </summary>
        /// <param name="prompt">The prompt (e.g., "Proofread")</param>
        /// <param name="candidateText">The text to proofread</param>
        /// <param name="language">The language code (default: "en")</param>
        /// <param name="strictness">The strictness level (1-5, default: 4)</param>
        /// <returns>The API response containing the proofread text and issues</returns>
        public async Task<ProofreadResponse?> GetProofreadResultAsync(
            string prompt,
            string candidateText,
            string language = "en",
            int strictness = 4)
        {
            try
            {
                var request = new ProofreadRequest
                {
                    Prompt = prompt,
                    CandidateText = candidateText,
                    Language = language,
                    Strictness = strictness
                };

                var jsonContent = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });

                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_authToken}");

                var response = await _httpClient.PostAsync($"{_apiUrl}/v1/evaluations", content);
                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ProofreadResponse>(responseJson, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true
                });

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"API call failed: {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// Request model for the proofreading API
    /// </summary>
    public class ProofreadRequest
    {
        [JsonPropertyName("prompt")]
        public string Prompt { get; set; } = string.Empty;

        [JsonPropertyName("candidate_text")]
        public string CandidateText { get; set; } = string.Empty;

        [JsonPropertyName("language")]
        public string Language { get; set; } = "en";

        [JsonPropertyName("strictness")]
        public int Strictness { get; set; } = 4;
    }

    /// <summary>
    /// Response model from the proofreading API
    /// </summary>
    public class ProofreadResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("proofread_text")]
        public string ProofreadText { get; set; } = string.Empty;

        [JsonPropertyName("issues")]
        public List<ProofreadIssue> Issues { get; set; } = new List<ProofreadIssue>();

        [JsonPropertyName("model_name")]
        public string ModelName { get; set; } = string.Empty;

        [JsonPropertyName("model_version")]
        public string ModelVersion { get; set; } = string.Empty;

        [JsonPropertyName("latency_ms")]
        public int LatencyMs { get; set; }

        [JsonPropertyName("summary_metrics")]
        public SummaryMetrics? SummaryMetrics { get; set; }

        [JsonPropertyName("failure_reason")]
        public string? FailureReason { get; set; }

        [JsonPropertyName("submitted_at")]
        public DateTime SubmittedAt { get; set; }

        [JsonPropertyName("completed_at")]
        public DateTime CompletedAt { get; set; }
    }

    /// <summary>
    /// Represents an issue found during proofreading
    /// </summary>
    public class ProofreadIssue
    {
        [JsonPropertyName("category")]
        public string Category { get; set; } = string.Empty;

        [JsonPropertyName("span_start")]
        public int SpanStart { get; set; }

        [JsonPropertyName("span_end")]
        public int SpanEnd { get; set; }

        [JsonPropertyName("suggestion")]
        public string Suggestion { get; set; } = string.Empty;

        [JsonPropertyName("severity")]
        public string Severity { get; set; } = string.Empty;

        [JsonPropertyName("confidence")]
        public double Confidence { get; set; }
    }

    /// <summary>
    /// Summary metrics from the API
    /// </summary>
    public class SummaryMetrics
    {
        [JsonPropertyName("tokens")]
        public int Tokens { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }
    }
}
