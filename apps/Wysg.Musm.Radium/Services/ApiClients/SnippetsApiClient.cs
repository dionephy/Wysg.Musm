using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Wysg.Musm.Radium.Services.ApiClients
{
    /// <summary>
    /// Snippet DTO (matches API)
    /// </summary>
    public class SnippetDto
    {
        public long SnippetId { get; set; }
        public long AccountId { get; set; }
        public string TriggerText { get; set; } = string.Empty;
        public string SnippetText { get; set; } = string.Empty;
        public string SnippetAst { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public long Rev { get; set; }
    }

    /// <summary>
    /// Upsert snippet request
    /// </summary>
    public class UpsertSnippetRequest
    {
        public string TriggerText { get; set; } = string.Empty;
        public string SnippetText { get; set; } = string.Empty;
        public string SnippetAst { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// API client for snippet operations
    /// </summary>
    public interface ISnippetsApiClient
    {
        Task<List<SnippetDto>> GetAllAsync(long accountId);
        Task<SnippetDto?> GetByIdAsync(long accountId, long snippetId);
        Task<SnippetDto> UpsertAsync(long accountId, UpsertSnippetRequest request);
        Task<SnippetDto> ToggleActiveAsync(long accountId, long snippetId);
        Task DeleteAsync(long accountId, long snippetId);
        Task<bool> IsAvailableAsync();
    }

    /// <summary>
    /// Implementation of snippets API client
    /// </summary>
    public class SnippetsApiClient : ApiClientBase, ISnippetsApiClient
    {
        public SnippetsApiClient(HttpClient httpClient, string baseUrl)
            : base(httpClient, baseUrl)
        {
        }

        public async Task<List<SnippetDto>> GetAllAsync(long accountId)
        {
            try
            {
                Debug.WriteLine($"[SnippetsApiClient] Getting all snippets for account {accountId}");
                return await GetAsync<List<SnippetDto>>($"/api/accounts/{accountId}/snippets") 
                       ?? new List<SnippetDto>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SnippetsApiClient] Error getting snippets: {ex.Message}");
                throw;
            }
        }

        public async Task<SnippetDto?> GetByIdAsync(long accountId, long snippetId)
        {
            try
            {
                Debug.WriteLine($"[SnippetsApiClient] Getting snippet {snippetId} for account {accountId}");
                return await GetAsync<SnippetDto>($"/api/accounts/{accountId}/snippets/{snippetId}");
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                Debug.WriteLine($"[SnippetsApiClient] Snippet {snippetId} not found");
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SnippetsApiClient] Error getting snippet: {ex.Message}");
                throw;
            }
        }

        public async Task<SnippetDto> UpsertAsync(long accountId, UpsertSnippetRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (string.IsNullOrWhiteSpace(request.TriggerText))
            {
                throw new ArgumentException("Trigger text cannot be empty", nameof(request));
            }

            if (string.IsNullOrWhiteSpace(request.SnippetText))
            {
                throw new ArgumentException("Snippet text cannot be empty", nameof(request));
            }

            if (string.IsNullOrWhiteSpace(request.SnippetAst))
            {
                throw new ArgumentException("Snippet AST cannot be empty", nameof(request));
            }

            try
            {
                Debug.WriteLine($"[SnippetsApiClient] Upserting snippet for account {accountId}");
                var result = await PutAsync<SnippetDto>($"/api/accounts/{accountId}/snippets", request);
                
                if (result == null)
                {
                    throw new InvalidOperationException("Upsert returned null result");
                }
                
                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SnippetsApiClient] Error upserting snippet: {ex.Message}");
                throw;
            }
        }

        public async Task<SnippetDto> ToggleActiveAsync(long accountId, long snippetId)
        {
            try
            {
                Debug.WriteLine($"[SnippetsApiClient] Toggling snippet {snippetId} for account {accountId}");
                var result = await PostAsync<SnippetDto>($"/api/accounts/{accountId}/snippets/{snippetId}/toggle", new { });
                
                if (result == null)
                {
                    throw new InvalidOperationException("Toggle returned null result");
                }
                
                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SnippetsApiClient] Error toggling snippet: {ex.Message}");
                throw;
            }
        }

        public async Task DeleteAsync(long accountId, long snippetId)
        {
            try
            {
                Debug.WriteLine($"[SnippetsApiClient] Deleting snippet {snippetId} for account {accountId}");
                await DeleteAsync($"/api/accounts/{accountId}/snippets/{snippetId}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SnippetsApiClient] Error deleting snippet: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> IsAvailableAsync()
        {
            return await IsApiAvailableAsync();
        }
    }
}
