using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Wysg.Musm.Radium.Services.ApiClients
{
    /// <summary>
    /// Phrase DTO (matches API)
    /// </summary>
    public class PhraseDto
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
    /// Upsert phrase request
    /// </summary>
    public class UpsertPhraseRequest
    {
        public string Text { get; set; } = string.Empty;
        public bool Active { get; set; } = true;
        public string? Tags { get; set; }
    }

    /// <summary>
    /// API client for phrase operations
    /// </summary>
    public interface IPhrasesApiClient
    {
        Task<List<PhraseDto>> GetAllAsync(long accountId, bool activeOnly = false);
        Task<PhraseDto?> GetByIdAsync(long accountId, long phraseId);
        Task<List<PhraseDto>> SearchAsync(long accountId, string? query, bool activeOnly = true, int maxResults = 100);
        Task<PhraseDto> UpsertAsync(long accountId, string text, bool active = true, string? tags = null);
        Task<List<PhraseDto>> BatchUpsertAsync(long accountId, List<string> phrases, bool active = true);
        Task ToggleActiveAsync(long accountId, long phraseId);
        Task DeleteAsync(long accountId, long phraseId);
        Task<long> GetMaxRevisionAsync(long accountId);
        Task<bool> IsAvailableAsync();
    }

    /// <summary>
    /// Implementation of phrases API client
    /// </summary>
    public class PhrasesApiClient : ApiClientBase, IPhrasesApiClient
    {
        public PhrasesApiClient(HttpClient httpClient, string baseUrl)
            : base(httpClient, baseUrl)
        {
        }

        public async Task<List<PhraseDto>> GetAllAsync(long accountId, bool activeOnly = false)
        {
            try
            {
                Debug.WriteLine($"[PhrasesApiClient] Getting all phrases for account {accountId}, activeOnly={activeOnly}");
                var endpoint = $"/api/accounts/{accountId}/phrases?activeOnly={activeOnly}";
                return await GetAsync<List<PhraseDto>>(endpoint) ?? new List<PhraseDto>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PhrasesApiClient] Error getting phrases: {ex.Message}");
                throw;
            }
        }

        public async Task<PhraseDto?> GetByIdAsync(long accountId, long phraseId)
        {
            try
            {
                Debug.WriteLine($"[PhrasesApiClient] Getting phrase {phraseId} for account {accountId}");
                return await GetAsync<PhraseDto>($"/api/accounts/{accountId}/phrases/{phraseId}");
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                Debug.WriteLine($"[PhrasesApiClient] Phrase {phraseId} not found");
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PhrasesApiClient] Error getting phrase: {ex.Message}");
                throw;
            }
        }

        public async Task<List<PhraseDto>> SearchAsync(long accountId, string? query, bool activeOnly = true, int maxResults = 100)
        {
            try
            {
                Debug.WriteLine($"[PhrasesApiClient] Searching phrases for account {accountId}, query='{query}'");
                var endpoint = $"/api/accounts/{accountId}/phrases/search?query={Uri.EscapeDataString(query ?? "")}&activeOnly={activeOnly}&maxResults={maxResults}";
                return await GetAsync<List<PhraseDto>>(endpoint) ?? new List<PhraseDto>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PhrasesApiClient] Error searching phrases: {ex.Message}");
                throw;
            }
        }

        public async Task<PhraseDto> UpsertAsync(long accountId, string text, bool active = true, string? tags = null)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new ArgumentException("Phrase text cannot be empty", nameof(text));
            }

            try
            {
                Debug.WriteLine($"[PhrasesApiClient] Upserting phrase for account {accountId}");
                var request = new UpsertPhraseRequest 
                { 
                    Text = text.Trim(), 
                    Active = active,
                    Tags = tags
                };
                var result = await PutAsync<PhraseDto>($"/api/accounts/{accountId}/phrases", request);
                
                if (result == null)
                {
                    throw new InvalidOperationException("Upsert returned null result");
                }
                
                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PhrasesApiClient] Error upserting phrase: {ex.Message}");
                throw;
            }
        }

        public async Task<List<PhraseDto>> BatchUpsertAsync(long accountId, List<string> phrases, bool active = true)
        {
            if (phrases == null || phrases.Count == 0)
            {
                return new List<PhraseDto>();
            }

            try
            {
                Debug.WriteLine($"[PhrasesApiClient] Batch upserting {phrases.Count} phrases for account {accountId}");
                var request = new { Phrases = phrases, Active = active };
                return await PutAsync<List<PhraseDto>>($"/api/accounts/{accountId}/phrases/batch", request) 
                       ?? new List<PhraseDto>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PhrasesApiClient] Error batch upserting phrases: {ex.Message}");
                throw;
            }
        }

        public async Task ToggleActiveAsync(long accountId, long phraseId)
        {
            try
            {
                Debug.WriteLine($"[PhrasesApiClient] Toggling phrase {phraseId} for account {accountId}");
                await PostAsync($"/api/accounts/{accountId}/phrases/{phraseId}/toggle", new { });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PhrasesApiClient] Error toggling phrase: {ex.Message}");
                throw;
            }
        }

        public async Task DeleteAsync(long accountId, long phraseId)
        {
            try
            {
                Debug.WriteLine($"[PhrasesApiClient] Deleting phrase {phraseId} for account {accountId}");
                await DeleteAsync($"/api/accounts/{accountId}/phrases/{phraseId}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PhrasesApiClient] Error deleting phrase: {ex.Message}");
                throw;
            }
        }

        public async Task<long> GetMaxRevisionAsync(long accountId)
        {
            try
            {
                Debug.WriteLine($"[PhrasesApiClient] Getting max revision for account {accountId}");
                var result = await GetAsync<Dictionary<string, long>>($"/api/accounts/{accountId}/phrases/revision");
                return result?.FirstOrDefault().Value ?? 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PhrasesApiClient] Error getting max revision: {ex.Message}");
                return 0;
            }
        }

        public async Task<bool> IsAvailableAsync()
        {
            return await IsApiAvailableAsync();
        }
    }
}
