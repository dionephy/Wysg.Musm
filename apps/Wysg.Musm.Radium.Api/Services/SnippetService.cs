using Wysg.Musm.Radium.Api.Models.Dtos;
using Wysg.Musm.Radium.Api.Repositories;

namespace Wysg.Musm.Radium.Api.Services
{
    public sealed class SnippetService : ISnippetService
    {
        private readonly ISnippetRepository _repository;
        private readonly ILogger<SnippetService> _logger;

        public SnippetService(ISnippetRepository repository, ILogger<SnippetService> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IReadOnlyList<SnippetDto>> GetAllByAccountAsync(long accountId)
        {
            if (accountId <= 0)
            {
                throw new ArgumentException("Account ID must be positive", nameof(accountId));
            }

            _logger.LogInformation("Getting all snippets for account {AccountId}", accountId);

            return await _repository.GetAllByAccountAsync(accountId);
        }

        public async Task<SnippetDto?> GetByIdAsync(long accountId, long snippetId)
        {
            if (accountId <= 0)
            {
                throw new ArgumentException("Account ID must be positive", nameof(accountId));
            }

            if (snippetId <= 0)
            {
                throw new ArgumentException("Snippet ID must be positive", nameof(snippetId));
            }

            return await _repository.GetByIdAsync(accountId, snippetId);
        }

        public async Task<SnippetDto> UpsertAsync(long accountId, UpsertSnippetRequest request)
        {
            if (accountId <= 0)
            {
                throw new ArgumentException("Account ID must be positive", nameof(accountId));
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

            _logger.LogInformation("Upserting snippet for account {AccountId}, trigger '{TriggerText}'", 
                accountId, request.TriggerText);

            return await _repository.UpsertAsync(accountId, request);
        }

        public async Task<SnippetDto?> ToggleActiveAsync(long accountId, long snippetId)
        {
            if (accountId <= 0)
            {
                throw new ArgumentException("Account ID must be positive", nameof(accountId));
            }

            if (snippetId <= 0)
            {
                throw new ArgumentException("Snippet ID must be positive", nameof(snippetId));
            }

            _logger.LogInformation("Toggling snippet {SnippetId} for account {AccountId}", snippetId, accountId);

            return await _repository.ToggleActiveAsync(accountId, snippetId);
        }

        public async Task<bool> DeleteAsync(long accountId, long snippetId)
        {
            if (accountId <= 0)
            {
                throw new ArgumentException("Account ID must be positive", nameof(accountId));
            }

            if (snippetId <= 0)
            {
                throw new ArgumentException("Snippet ID must be positive", nameof(snippetId));
            }

            _logger.LogInformation("Deleting snippet {SnippetId} for account {AccountId}", snippetId, accountId);

            return await _repository.DeleteAsync(accountId, snippetId);
        }
    }
}
