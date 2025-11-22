using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Wysg.Musm.Radium.Services;

namespace Wysg.Musm.Radium.Services.Adapters
{
    /// <summary>
    /// Adapter that implements ISnippetService using RadiumApiClient.
    /// Allows seamless switching between direct DB access and API calls.
    /// </summary>
    public sealed class ApiSnippetServiceAdapter : ISnippetService
    {
        private readonly RadiumApiClient _apiClient;

        public ApiSnippetServiceAdapter(RadiumApiClient apiClient)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        }

        public Task PreloadAsync(long accountId)
        {
            // API doesn't need preloading - it's stateless
            return Task.CompletedTask;
        }

        public async Task<IReadOnlyList<SnippetInfo>> GetAllSnippetMetaAsync(long accountId)
        {
            var dtos = await _apiClient.GetSnippetsAsync(accountId);
            return dtos.Select(dto => new SnippetInfo(
                SnippetId: dto.SnippetId,
                AccountId: dto.AccountId,
                TriggerText: dto.TriggerText,
                SnippetText: dto.SnippetText,
                SnippetAst: dto.SnippetAst,
                Description: dto.Description ?? string.Empty,
                IsActive: dto.IsActive,
                UpdatedAt: dto.UpdatedAt,
                Rev: dto.Rev
            )).ToList();
        }

        public async Task<IReadOnlyDictionary<string, (string text, string ast, string description)>> GetActiveSnippetsAsync(long accountId)
        {
            var dtos = await _apiClient.GetSnippetsAsync(accountId);
            return dtos
                .Where(dto => dto.IsActive)
                .ToDictionary(
                    dto => dto.TriggerText, 
                    dto => (dto.SnippetText, dto.SnippetAst, dto.Description ?? string.Empty)
                );
        }

        public async Task<SnippetInfo> UpsertSnippetAsync(long accountId, string triggerText, string snippetText, string snippetAst, bool isActive = true, string? description = null)
        {
            var request = new UpsertSnippetRequest
            {
                TriggerText = triggerText,
                SnippetText = snippetText,
                SnippetAst = snippetAst,
                Description = description,
                IsActive = isActive
            };

            var dto = await _apiClient.UpsertSnippetAsync(accountId, request);
            
            return new SnippetInfo(
                SnippetId: dto.SnippetId,
                AccountId: dto.AccountId,
                TriggerText: dto.TriggerText,
                SnippetText: dto.SnippetText,
                SnippetAst: dto.SnippetAst,
                Description: dto.Description ?? string.Empty,
                IsActive: dto.IsActive,
                UpdatedAt: dto.UpdatedAt,
                Rev: dto.Rev
            );
        }

        public async Task<SnippetInfo?> ToggleActiveAsync(long accountId, long snippetId)
        {
            var dto = await _apiClient.ToggleSnippetAsync(accountId, snippetId);
            if (dto == null) return null;

            return new SnippetInfo(
                SnippetId: dto.SnippetId,
                AccountId: dto.AccountId,
                TriggerText: dto.TriggerText,
                SnippetText: dto.SnippetText,
                SnippetAst: dto.SnippetAst,
                Description: dto.Description ?? string.Empty,
                IsActive: dto.IsActive,
                UpdatedAt: dto.UpdatedAt,
                Rev: dto.Rev
            );
        }

        public async Task<bool> DeleteSnippetAsync(long accountId, long snippetId)
        {
            return await _apiClient.DeleteSnippetAsync(accountId, snippetId);
        }

        public Task RefreshSnippetsAsync(long accountId)
        {
            // API is stateless - no refresh needed
            return Task.CompletedTask;
        }
    }
}
