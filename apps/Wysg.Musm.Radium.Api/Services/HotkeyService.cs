using Wysg.Musm.Radium.Api.Models.Dtos;
using Wysg.Musm.Radium.Api.Repositories;

namespace Wysg.Musm.Radium.Api.Services
{
    public sealed class HotkeyService : IHotkeyService
    {
        private readonly IHotkeyRepository _repository;
        private readonly ILogger<HotkeyService> _logger;

        public HotkeyService(IHotkeyRepository repository, ILogger<HotkeyService> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IReadOnlyList<HotkeyDto>> GetAllByAccountAsync(long accountId)
        {
            if (accountId <= 0)
            {
                throw new ArgumentException("Account ID must be positive", nameof(accountId));
            }

            _logger.LogInformation("Getting all hotkeys for account {AccountId}", accountId);

            return await _repository.GetAllByAccountAsync(accountId);
        }

        public async Task<HotkeyDto?> GetByIdAsync(long accountId, long hotkeyId)
        {
            if (accountId <= 0)
            {
                throw new ArgumentException("Account ID must be positive", nameof(accountId));
            }

            if (hotkeyId <= 0)
            {
                throw new ArgumentException("Hotkey ID must be positive", nameof(hotkeyId));
            }

            return await _repository.GetByIdAsync(accountId, hotkeyId);
        }

        public async Task<HotkeyDto> UpsertAsync(long accountId, UpsertHotkeyRequest request)
        {
            if (accountId <= 0)
            {
                throw new ArgumentException("Account ID must be positive", nameof(accountId));
            }

            if (string.IsNullOrWhiteSpace(request.TriggerText))
            {
                throw new ArgumentException("Trigger text cannot be empty", nameof(request));
            }

            if (string.IsNullOrWhiteSpace(request.ExpansionText))
            {
                throw new ArgumentException("Expansion text cannot be empty", nameof(request));
            }

            _logger.LogInformation("Upserting hotkey for account {AccountId}, trigger '{TriggerText}'", 
                accountId, request.TriggerText);

            return await _repository.UpsertAsync(accountId, request);
        }

        public async Task<HotkeyDto?> ToggleActiveAsync(long accountId, long hotkeyId)
        {
            if (accountId <= 0)
            {
                throw new ArgumentException("Account ID must be positive", nameof(accountId));
            }

            if (hotkeyId <= 0)
            {
                throw new ArgumentException("Hotkey ID must be positive", nameof(hotkeyId));
            }

            _logger.LogInformation("Toggling hotkey {HotkeyId} for account {AccountId}", hotkeyId, accountId);

            return await _repository.ToggleActiveAsync(accountId, hotkeyId);
        }

        public async Task<bool> DeleteAsync(long accountId, long hotkeyId)
        {
            if (accountId <= 0)
            {
                throw new ArgumentException("Account ID must be positive", nameof(accountId));
            }

            if (hotkeyId <= 0)
            {
                throw new ArgumentException("Hotkey ID must be positive", nameof(hotkeyId));
            }

            _logger.LogInformation("Deleting hotkey {HotkeyId} for account {AccountId}", hotkeyId, accountId);

            return await _repository.DeleteAsync(accountId, hotkeyId);
        }
    }
}
