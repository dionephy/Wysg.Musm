using System.Text.Json;
using Wysg.Musm.Radium.Api.Models.Dtos;
using Wysg.Musm.Radium.Api.Repositories;

namespace Wysg.Musm.Radium.Api.Services
{
    public sealed class UserSettingService : IUserSettingService
    {
        private readonly IUserSettingRepository _repository;
        private readonly ILogger<UserSettingService> _logger;

        public UserSettingService(IUserSettingRepository repository, ILogger<UserSettingService> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<UserSettingDto?> GetByAccountAsync(long accountId)
        {
            if (accountId <= 0)
            {
                throw new ArgumentException("Account ID must be positive", nameof(accountId));
            }

            _logger.LogInformation("Getting user settings for account {AccountId}", accountId);

            return await _repository.GetByAccountAsync(accountId);
        }

        public async Task<UserSettingDto> UpsertAsync(long accountId, UpdateUserSettingRequest request)
        {
            if (accountId <= 0)
            {
                throw new ArgumentException("Account ID must be positive", nameof(accountId));
            }

            if (string.IsNullOrWhiteSpace(request.SettingsJson))
            {
                throw new ArgumentException("Settings JSON cannot be empty", nameof(request));
            }

            // Validate JSON
            try
            {
                using var doc = JsonDocument.Parse(request.SettingsJson);
                // JSON is valid if we get here
            }
            catch (JsonException ex)
            {
                throw new ArgumentException($"Invalid JSON format: {ex.Message}", nameof(request), ex);
            }

            _logger.LogInformation("Upserting user settings for account {AccountId}", accountId);

            return await _repository.UpsertAsync(accountId, request);
        }

        public async Task<bool> DeleteAsync(long accountId)
        {
            if (accountId <= 0)
            {
                throw new ArgumentException("Account ID must be positive", nameof(accountId));
            }

            _logger.LogInformation("Deleting user settings for account {AccountId}", accountId);

            return await _repository.DeleteAsync(accountId);
        }
    }
}
