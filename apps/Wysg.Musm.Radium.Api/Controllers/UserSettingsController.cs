using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wysg.Musm.Radium.Api.Extensions;
using Wysg.Musm.Radium.Api.Models.Dtos;
using Wysg.Musm.Radium.Api.Services;

namespace Wysg.Musm.Radium.Api.Controllers
{
    /// <summary>
    /// User Settings management endpoints (formerly reportify settings)
    /// </summary>
    [ApiController]
    [Authorize] // Require Firebase authentication
    [Route("api/accounts/{accountId}/settings")]
    [Produces("application/json")]
    public sealed class UserSettingsController : ControllerBase
    {
        private readonly IUserSettingService _userSettingService;
        private readonly ILogger<UserSettingsController> _logger;

        public UserSettingsController(IUserSettingService userSettingService, ILogger<UserSettingsController> logger)
        {
            _userSettingService = userSettingService ?? throw new ArgumentNullException(nameof(userSettingService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Get user settings for an account
        /// </summary>
        /// <param name="accountId">Account ID</param>
        /// <returns>User settings JSON</returns>
        [HttpGet]
        [ProducesResponseType(typeof(UserSettingDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<UserSettingDto>> Get(long accountId)
        {
            // Validate user has access to this account
            var userAccountId = User.GetAccountId();
            if (userAccountId.HasValue && userAccountId.Value != accountId)
            {
                _logger.LogWarning("User {UserId} attempted to access account {AccountId} but belongs to {UserAccountId}",
                    User.GetFirebaseUid(), accountId, userAccountId);
                return Forbid();
            }

            try
            {
                var settings = await _userSettingService.GetByAccountAsync(accountId);
                if (settings == null)
                {
                    return NotFound(new { error = $"User settings not found for account {accountId}" });
                }

                return Ok(settings);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid account ID: {AccountId}", accountId);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user settings for account {AccountId}", accountId);
                return StatusCode(500, new { error = "An error occurred while retrieving user settings" });
            }
        }

        /// <summary>
        /// Create or update user settings
        /// </summary>
        /// <param name="accountId">Account ID</param>
        /// <param name="request">Settings JSON data</param>
        /// <returns>Updated user settings</returns>
        [HttpPut]
        [ProducesResponseType(typeof(UserSettingDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<UserSettingDto>> Upsert(long accountId, [FromBody] UpdateUserSettingRequest request)
        {
            // Validate user has access to this account
            var userAccountId = User.GetAccountId();
            if (userAccountId.HasValue && userAccountId.Value != accountId)
            {
                return Forbid();
            }

            try
            {
                var settings = await _userSettingService.UpsertAsync(accountId, request);
                return Ok(settings);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid upsert request for account {AccountId}", accountId);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error upserting user settings for account {AccountId}", accountId);
                return StatusCode(500, new { error = "An error occurred while saving user settings" });
            }
        }

        /// <summary>
        /// Delete user settings
        /// </summary>
        /// <param name="accountId">Account ID</param>
        /// <returns>No content on success</returns>
        [HttpDelete]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Delete(long accountId)
        {
            // Validate user has access to this account
            var userAccountId = User.GetAccountId();
            if (userAccountId.HasValue && userAccountId.Value != accountId)
            {
                return Forbid();
            }

            try
            {
                var deleted = await _userSettingService.DeleteAsync(accountId);
                if (!deleted)
                {
                    return NotFound(new { error = $"User settings not found for account {accountId}" });
                }

                return NoContent();
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid account ID: {AccountId}", accountId);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user settings for account {AccountId}", accountId);
                return StatusCode(500, new { error = "An error occurred while deleting user settings" });
            }
        }
    }
}
