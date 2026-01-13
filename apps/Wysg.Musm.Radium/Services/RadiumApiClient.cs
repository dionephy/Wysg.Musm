using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Wysg.Musm.Radium.Services
{
    /// <summary>
    /// HTTP client for calling the Radium API
    /// Uses Firebase ID tokens for authentication
    /// </summary>
    public sealed class RadiumApiClient
    {
        private readonly HttpClient _httpClient;
        private string? _currentToken;

        public RadiumApiClient(string baseUrl)
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(baseUrl),
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        /// <summary>
        /// Set the Firebase ID token for authentication
        /// Call this after successful login with GoogleOAuthAuthService
        /// </summary>
        public void SetAuthToken(string firebaseIdToken)
        {
            _currentToken = firebaseIdToken;
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", firebaseIdToken);
        }

        /// <summary>
        /// Clear the authentication token (on logout)
        /// </summary>
        public void ClearAuthToken()
        {
            _currentToken = null;
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }

        #region Hotkeys

        public async Task<List<HotkeyDto>> GetHotkeysAsync(long accountId)
        {
            var response = await _httpClient.GetAsync($"/api/accounts/{accountId}/hotkeys");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<HotkeyDto>>() ?? new List<HotkeyDto>();
        }

        public async Task<HotkeyDto?> GetHotkeyAsync(long accountId, long hotkeyId)
        {
            var response = await _httpClient.GetAsync($"/api/accounts/{accountId}/hotkeys/{hotkeyId}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<HotkeyDto>();
        }

        public async Task<HotkeyDto> UpsertHotkeyAsync(long accountId, UpsertHotkeyRequest request)
        {
            var response = await _httpClient.PutAsJsonAsync($"/api/accounts/{accountId}/hotkeys", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<HotkeyDto>() 
                ?? throw new InvalidOperationException("Failed to deserialize hotkey");
        }

        public async Task<HotkeyDto?> ToggleHotkeyAsync(long accountId, long hotkeyId)
        {
            var response = await _httpClient.PostAsync($"/api/accounts/{accountId}/hotkeys/{hotkeyId}/toggle", null);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<HotkeyDto>();
        }

        public async Task<bool> DeleteHotkeyAsync(long accountId, long hotkeyId)
        {
            var response = await _httpClient.DeleteAsync($"/api/accounts/{accountId}/hotkeys/{hotkeyId}");
            return response.IsSuccessStatusCode;
        }

        #endregion

        #region Snippets

        public async Task<List<SnippetDto>> GetSnippetsAsync(long accountId)
        {
            var response = await _httpClient.GetAsync($"/api/accounts/{accountId}/snippets");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<SnippetDto>>() ?? new List<SnippetDto>();
        }

        public async Task<SnippetDto?> GetSnippetAsync(long accountId, long snippetId)
        {
            var response = await _httpClient.GetAsync($"/api/accounts/{accountId}/snippets/{snippetId}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<SnippetDto>();
        }

        public async Task<SnippetDto> UpsertSnippetAsync(long accountId, UpsertSnippetRequest request)
        {
            var response = await _httpClient.PutAsJsonAsync($"/api/accounts/{accountId}/snippets", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<SnippetDto>() 
                ?? throw new InvalidOperationException("Failed to deserialize snippet");
        }

        public async Task<SnippetDto?> ToggleSnippetAsync(long accountId, long snippetId)
        {
            var response = await _httpClient.PostAsync($"/api/accounts/{accountId}/snippets/{snippetId}/toggle", null);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<SnippetDto>();
        }

        public async Task<bool> DeleteSnippetAsync(long accountId, long snippetId)
        {
            var response = await _httpClient.DeleteAsync($"/api/accounts/{accountId}/snippets/{snippetId}");
            return response.IsSuccessStatusCode;
        }

        #endregion

        #region Accounts

        public async Task<EnsureAccountResponse> EnsureAccountAsync(EnsureAccountRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("/api/accounts/ensure", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<EnsureAccountResponse>()
                ?? throw new InvalidOperationException("Failed to deserialize ensure account response");
        }

        public async Task UpdateLastLoginAsync(long accountId)
        {
            var response = await _httpClient.PostAsync($"/api/accounts/{accountId}/login", null);
            response.EnsureSuccessStatusCode();
        }

        public async Task<AccountDto?> GetAccountAsync(long accountId)
        {
            var response = await _httpClient.GetAsync($"/api/accounts/{accountId}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<AccountDto>();
        }

        public async Task<string?> GetSettingsAsync(long accountId)
        {
            var response = await _httpClient.GetAsync($"/api/accounts/{accountId}/settings");
            if (response.StatusCode == System.Net.HttpStatusCode.NoContent || 
                response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public async Task UpdateSettingsAsync(long accountId, string settingsJson)
        {
            var response = await _httpClient.PutAsJsonAsync($"/api/accounts/{accountId}/settings", settingsJson);
            response.EnsureSuccessStatusCode();
        }

        #endregion

        #region Phrases

        public async Task<List<PhraseDto>> GetAllPhrasesAsync(long accountId, bool activeOnly = false)
        {
            var url = $"/api/accounts/{accountId}/phrases?activeOnly={activeOnly}";
            var response = await _httpClient.GetAsync(url);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return new List<PhraseDto>();
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<PhraseDto>>() ?? new List<PhraseDto>();
        }

        public async Task<PhraseDto?> GetPhraseByIdAsync(long accountId, long phraseId)
        {
            var response = await _httpClient.GetAsync($"/api/accounts/{accountId}/phrases/{phraseId}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<PhraseDto>();
        }

        public async Task<List<PhraseDto>> SearchPhrasesAsync(long accountId, string? query, bool activeOnly = true, int maxResults = 100)
        {
            var url = $"/api/accounts/{accountId}/phrases/search?activeOnly={activeOnly}&maxResults={maxResults}";
            if (!string.IsNullOrWhiteSpace(query))
                url += $"&query={Uri.EscapeDataString(query)}";
            
            var response = await _httpClient.GetAsync(url);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return new List<PhraseDto>();
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<PhraseDto>>() ?? new List<PhraseDto>();
        }

        public async Task<PhraseDto> UpsertPhraseAsync(long accountId, string text, bool active = true)
        {
            var request = new UpsertPhraseRequest { Text = text, Active = active };
            var response = await _httpClient.PutAsJsonAsync($"/api/accounts/{accountId}/phrases", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<PhraseDto>()
                ?? throw new InvalidOperationException("Failed to deserialize phrase response");
        }

        public async Task<List<PhraseDto>> BatchUpsertPhrasesAsync(long accountId, List<string> phrases, bool active = true)
        {
            var request = new BatchUpsertPhrasesRequest { Phrases = phrases, Active = active };
            var response = await _httpClient.PutAsJsonAsync($"/api/accounts/{accountId}/phrases/batch", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<PhraseDto>>() ?? new List<PhraseDto>();
        }

        public async Task TogglePhraseActiveAsync(long accountId, long phraseId)
        {
            var response = await _httpClient.PostAsync($"/api/accounts/{accountId}/phrases/{phraseId}/toggle", null);
            response.EnsureSuccessStatusCode();
        }

        public async Task DeletePhraseAsync(long accountId, long phraseId)
        {
            var response = await _httpClient.DeleteAsync($"/api/accounts/{accountId}/phrases/{phraseId}");
            response.EnsureSuccessStatusCode();
        }

        public async Task<ConvertToGlobalPhrasesResponse> ConvertPhrasesToGlobalAsync(long accountId, List<long> phraseIds)
        {
            var request = new ConvertToGlobalPhrasesRequest { PhraseIds = phraseIds ?? new List<long>() };
            var response = await _httpClient.PostAsJsonAsync($"/api/accounts/{accountId}/phrases/convert-global", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ConvertToGlobalPhrasesResponse>() ?? new ConvertToGlobalPhrasesResponse();
        }
 
         public async Task<long> GetPhraseMaxRevisionAsync(long accountId)
         {
             var response = await _httpClient.GetAsync($"/api/accounts/{accountId}/phrases/revision");
             response.EnsureSuccessStatusCode();
             return await response.Content.ReadFromJsonAsync<long>();
         }
 
         #endregion

        #region GlobalPhrases
        public async Task<List<PhraseDto>> GetGlobalPhrasesAsync(bool activeOnly = false)
        {
            var url = $"/api/phrases/global?activeOnly={activeOnly}";
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<PhraseDto>>() ?? new List<PhraseDto>();
        }

        public async Task<List<PhraseDto>> SearchGlobalPhrasesAsync(string? query, bool activeOnly = true, int maxResults = 100)
        {
            var url = $"/api/phrases/global/search?activeOnly={activeOnly}&maxResults={maxResults}";
            if (!string.IsNullOrWhiteSpace(query)) url += $"&query={Uri.EscapeDataString(query)}";
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<PhraseDto>>() ?? new List<PhraseDto>();
        }

        public async Task<PhraseDto> UpsertGlobalPhraseAsync(string text, bool active = true)
        {
            var request = new UpsertPhraseRequest { Text = text, Active = active };
            var response = await _httpClient.PutAsJsonAsync("/api/phrases/global", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<PhraseDto>() ?? throw new InvalidOperationException("Failed to deserialize global phrase");
        }

        public async Task ToggleGlobalPhraseAsync(long phraseId)
        {
            var response = await _httpClient.PostAsync($"/api/phrases/global/{phraseId}/toggle", null);
            response.EnsureSuccessStatusCode();
        }

        public async Task DeleteGlobalPhraseAsync(long phraseId)
        {
            var response = await _httpClient.DeleteAsync($"/api/phrases/global/{phraseId}");
            response.EnsureSuccessStatusCode();
        }

        public async Task<long> GetGlobalPhraseMaxRevisionAsync()
        {
            var response = await _httpClient.GetAsync("/api/phrases/global/revision");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<long>();
        }
        #endregion

        #region SNOMED

        /// <summary>
        /// Cache a SNOMED concept in the database.
        /// </summary>
        public async Task CacheSnomedConceptAsync(SnomedConceptDto concept)
        {
            var response = await _httpClient.PostAsJsonAsync("/api/snomed/concepts", concept);
            response.EnsureSuccessStatusCode();
        }

        /// <summary>
        /// Get a cached SNOMED concept by ID.
        /// </summary>
        public async Task<SnomedConceptDto?> GetSnomedConceptAsync(long conceptId)
        {
            var response = await _httpClient.GetAsync($"/api/snomed/concepts/{conceptId}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<SnomedConceptDto>();
        }

        /// <summary>
        /// Create a mapping between a phrase and a SNOMED concept.
        /// </summary>
        public async Task CreatePhraseSnomedMappingAsync(CreateSnomedMappingRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("/api/snomed/mappings", request);
            response.EnsureSuccessStatusCode();
        }

        /// <summary>
        /// Get a single phrase-SNOMED mapping.
        /// </summary>
        public async Task<PhraseSnomedMappingDto?> GetPhraseSnomedMappingAsync(long phraseId)
        {
            var response = await _httpClient.GetAsync($"/api/snomed/mappings/{phraseId}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<PhraseSnomedMappingDto>();
        }

        /// <summary>
        /// Get multiple phrase-SNOMED mappings in batch (for syntax highlighting).
        /// </summary>
        public async Task<Dictionary<long, PhraseSnomedMappingDto>> GetPhraseSnomedMappingsBatchAsync(IEnumerable<long> phraseIds)
        {
            var ids = phraseIds.ToList();
            if (ids.Count == 0)
                return new Dictionary<long, PhraseSnomedMappingDto>();

            // Build query string: ?phraseIds=1&phraseIds=2&phraseIds=3
            var queryParams = string.Join("&", ids.Select(id => $"phraseIds={id}"));
            var url = $"/api/snomed/mappings?{queryParams}";

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Dictionary<long, PhraseSnomedMappingDto>>()
                ?? new Dictionary<long, PhraseSnomedMappingDto>();
        }

        /// <summary>
        /// Delete a phrase-SNOMED mapping.
        /// </summary>
        public async Task DeletePhraseSnomedMappingAsync(long phraseId)
        {
            var response = await _httpClient.DeleteAsync($"/api/snomed/mappings/{phraseId}");
            response.EnsureSuccessStatusCode();
        }

        #endregion
    }

    #region DTOs

    public class HotkeyDto
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

    public class UpsertHotkeyRequest
    {
        public required string TriggerText { get; set; }
        public required string ExpansionText { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class SnippetDto
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

    public class UpsertSnippetRequest
    {
        public required string TriggerText { get; set; }
        public required string SnippetText { get; set; }
        public required string SnippetAst { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
    }

    // Account DTOs
    public class AccountDto
    {
        public long AccountId { get; set; }
        public string Uid { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
    }

    public class EnsureAccountRequest
    {
        public required string Uid { get; set; }
        public required string Email { get; set; }
        public string? DisplayName { get; set; }
    }

    public class EnsureAccountResponse
    {
        public required AccountDto Account { get; set; }
        public string? SettingsJson { get; set; }
    }

    // Phrase DTOs
    public class PhraseDto
    {
        public long Id { get; set; }
        public long? AccountId { get; set; } // null => global phrase
        public string Text { get; set; } = string.Empty;
        public bool Active { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public long Rev { get; set; }
    }

    public class UpsertPhraseRequest
    {
        public required string Text { get; set; }
        public bool Active { get; set; } = true;
    }

    public class BatchUpsertPhrasesRequest
    {
        public required List<string> Phrases { get; set; }
        public bool Active { get; set; } = true;
    }
    public class ConvertToGlobalPhrasesRequest
    {
        public List<long> PhraseIds { get; set; } = new();
    }

    public class ConvertToGlobalPhrasesResponse
    {
        public int Converted { get; set; }
        public int DuplicatesRemoved { get; set; }
    }
 
     // SNOMED DTOs
     public class SnomedConceptDto
     {
         public long ConceptId { get; set; }
         public string ConceptIdStr { get; set; } = string.Empty;
         public string Fsn { get; set; } = string.Empty;
         public string? Pt { get; set; }
         public string? SemanticTag { get; set; }
         public string? ModuleId { get; set; }
         public bool Active { get; set; } = true;
         public DateTime CachedAt { get; set; }
         public DateTime? ExpiresAt { get; set; }
     }

    public class PhraseSnomedMappingDto
    {
        public long PhraseId { get; set; }
        public long? AccountId { get; set; }
        public long ConceptId { get; set; }
        public string ConceptIdStr { get; set; } = string.Empty;
        public string Fsn { get; set; } = string.Empty;
        public string? Pt { get; set; }
        public string? SemanticTag { get; set; }
        public string MappingType { get; set; } = "exact";
        public decimal? Confidence { get; set; }
        public string? Notes { get; set; }
        public string Source { get; set; } = "global";
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CreateSnomedMappingRequest
    {
        public long PhraseId { get; set; }
        public long? AccountId { get; set; }
        public long ConceptId { get; set; }
        public string MappingType { get; set; } = "exact";
        public decimal? Confidence { get; set; }
        public string? Notes { get; set; }
        public long? MappedBy { get; set; }
    }

    #endregion
}
