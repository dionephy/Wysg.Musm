using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wysg.Musm.Radium.Api.Extensions;
using Wysg.Musm.Radium.Api.Models.Dtos;
using Wysg.Musm.Radium.Api.Services;

namespace Wysg.Musm.Radium.Api.Controllers
{
    /// <summary>
    /// Hotkey management endpoints
    /// </summary>
    [ApiController]
    [Authorize] // Require Firebase authentication
    [Route("api/accounts/{accountId}/[controller]")]
    [Produces("application/json")]
    public sealed class HotkeysController : ControllerBase
    {
        private readonly IHotkeyService _hotkeyService;
        private readonly ILogger<HotkeysController> _logger;

        public HotkeysController(IHotkeyService hotkeyService, ILogger<HotkeysController> logger)
        {
            _hotkeyService = hotkeyService ?? throw new ArgumentNullException(nameof(hotkeyService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Get all hotkeys for an account
        /// </summary>
        /// <param name="accountId">Account ID</param>
        /// <returns>List of hotkeys</returns>
        [HttpGet]
        [ProducesResponseType(typeof(IReadOnlyList<HotkeyDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IReadOnlyList<HotkeyDto>>> GetAll(long accountId)
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
                var hotkeys = await _hotkeyService.GetAllByAccountAsync(accountId);
                return Ok(hotkeys);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid account ID: {AccountId}", accountId);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting hotkeys for account {AccountId}", accountId);
                return StatusCode(500, new { error = "An error occurred while retrieving hotkeys" });
            }
        }

        /// <summary>
        /// Get a specific hotkey by ID
        /// </summary>
        /// <param name="accountId">Account ID</param>
        /// <param name="hotkeyId">Hotkey ID</param>
        /// <returns>Hotkey details</returns>
        [HttpGet("{hotkeyId}")]
        [ProducesResponseType(typeof(HotkeyDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<HotkeyDto>> GetById(long accountId, long hotkeyId)
        {
            // Validate user has access to this account
            var userAccountId = User.GetAccountId();
            if (userAccountId.HasValue && userAccountId.Value != accountId)
            {
                return Forbid();
            }

            try
            {
                var hotkey = await _hotkeyService.GetByIdAsync(accountId, hotkeyId);
                if (hotkey == null)
                {
                    return NotFound(new { error = $"Hotkey {hotkeyId} not found for account {accountId}" });
                }

                return Ok(hotkey);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid parameters: accountId={AccountId}, hotkeyId={HotkeyId}", 
                    accountId, hotkeyId);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting hotkey {HotkeyId} for account {AccountId}", 
                    hotkeyId, accountId);
                return StatusCode(500, new { error = "An error occurred while retrieving the hotkey" });
            }
        }

        /// <summary>
        /// Create or update a hotkey
        /// </summary>
        /// <param name="accountId">Account ID</param>
        /// <param name="request">Hotkey data</param>
        /// <returns>Created or updated hotkey</returns>
        [HttpPut]
        [ProducesResponseType(typeof(HotkeyDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<HotkeyDto>> Upsert(long accountId, [FromBody] UpsertHotkeyRequest request)
        {
            // Validate user has access to this account
            var userAccountId = User.GetAccountId();
            if (userAccountId.HasValue && userAccountId.Value != accountId)
            {
                return Forbid();
            }

            try
            {
                var hotkey = await _hotkeyService.UpsertAsync(accountId, request);
                return Ok(hotkey);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid upsert request for account {AccountId}", accountId);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error upserting hotkey for account {AccountId}", accountId);
                return StatusCode(500, new { error = "An error occurred while saving the hotkey" });
            }
        }

        /// <summary>
        /// Toggle hotkey active status
        /// </summary>
        /// <param name="accountId">Account ID</param>
        /// <param name="hotkeyId">Hotkey ID</param>
        /// <returns>Updated hotkey</returns>
        [HttpPost("{hotkeyId}/toggle")]
        [ProducesResponseType(typeof(HotkeyDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<HotkeyDto>> ToggleActive(long accountId, long hotkeyId)
        {
            // Validate user has access to this account
            var userAccountId = User.GetAccountId();
            if (userAccountId.HasValue && userAccountId.Value != accountId)
            {
                return Forbid();
            }

            try
            {
                var hotkey = await _hotkeyService.ToggleActiveAsync(accountId, hotkeyId);
                if (hotkey == null)
                {
                    return NotFound(new { error = $"Hotkey {hotkeyId} not found for account {accountId}" });
                }

                return Ok(hotkey);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid parameters: accountId={AccountId}, hotkeyId={HotkeyId}", 
                    accountId, hotkeyId);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling hotkey {HotkeyId} for account {AccountId}", 
                    hotkeyId, accountId);
                return StatusCode(500, new { error = "An error occurred while toggling the hotkey" });
            }
        }

        /// <summary>
        /// Delete a hotkey
        /// </summary>
        /// <param name="accountId">Account ID</param>
        /// <param name="hotkeyId">Hotkey ID</param>
        /// <returns>No content on success</returns>
        [HttpDelete("{hotkeyId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Delete(long accountId, long hotkeyId)
        {
            // Validate user has access to this account
            var userAccountId = User.GetAccountId();
            if (userAccountId.HasValue && userAccountId.Value != accountId)
            {
                return Forbid();
            }

            try
            {
                var deleted = await _hotkeyService.DeleteAsync(accountId, hotkeyId);
                if (!deleted)
                {
                    return NotFound(new { error = $"Hotkey {hotkeyId} not found for account {accountId}" });
                }

                return NoContent();
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid parameters: accountId={AccountId}, hotkeyId={HotkeyId}", 
                    accountId, hotkeyId);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting hotkey {HotkeyId} for account {AccountId}", 
                    hotkeyId, accountId);
                return StatusCode(500, new { error = "An error occurred while deleting the hotkey" });
            }
        }
    }
}
