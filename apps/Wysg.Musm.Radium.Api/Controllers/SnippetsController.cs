using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wysg.Musm.Radium.Api.Extensions;
using Wysg.Musm.Radium.Api.Models.Dtos;
using Wysg.Musm.Radium.Api.Services;

namespace Wysg.Musm.Radium.Api.Controllers
{
    /// <summary>
    /// Snippet management endpoints
    /// </summary>
    [ApiController]
    [Authorize] // Require Firebase authentication
    [Route("api/accounts/{accountId}/[controller]")]
    [Produces("application/json")]
    public sealed class SnippetsController : ControllerBase
    {
        private readonly ISnippetService _snippetService;
        private readonly ILogger<SnippetsController> _logger;

        public SnippetsController(ISnippetService snippetService, ILogger<SnippetsController> logger)
        {
            _snippetService = snippetService ?? throw new ArgumentNullException(nameof(snippetService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Get all snippets for an account
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<SnippetDto>>> GetAll(long accountId)
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
                var snippets = await _snippetService.GetAllByAccountAsync(accountId);
                return Ok(snippets);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid account ID: {AccountId}", accountId);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting snippets for account {AccountId}", accountId);
                return StatusCode(500, new { error = "An error occurred while retrieving snippets" });
            }
        }

        /// <summary>
        /// Get a specific snippet by ID
        /// </summary>
        [HttpGet("{snippetId}")]
        public async Task<ActionResult<SnippetDto>> GetById(long accountId, long snippetId)
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
                var snippet = await _snippetService.GetByIdAsync(accountId, snippetId);
                if (snippet == null)
                {
                    return NotFound(new { error = $"Snippet {snippetId} not found for account {accountId}" });
                }

                return Ok(snippet);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid parameters: accountId={AccountId}, snippetId={SnippetId}", 
                    accountId, snippetId);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting snippet {SnippetId} for account {AccountId}", 
                    snippetId, accountId);
                return StatusCode(500, new { error = "An error occurred while retrieving the snippet" });
            }
        }

        /// <summary>
        /// Create or update a snippet
        /// </summary>
        [HttpPut]
        public async Task<ActionResult<SnippetDto>> Upsert(long accountId, [FromBody] UpsertSnippetRequest request)
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
                var snippet = await _snippetService.UpsertAsync(accountId, request);
                return Ok(snippet);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid upsert request for account {AccountId}", accountId);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error upserting snippet for account {AccountId}", accountId);
                return StatusCode(500, new { error = "An error occurred while saving the snippet" });
            }
        }

        /// <summary>
        /// Toggle snippet active status
        /// </summary>
        [HttpPost("{snippetId}/toggle")]
        public async Task<ActionResult<SnippetDto>> ToggleActive(long accountId, long snippetId)
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
                var snippet = await _snippetService.ToggleActiveAsync(accountId, snippetId);
                if (snippet == null)
                {
                    return NotFound(new { error = $"Snippet {snippetId} not found for account {accountId}" });
                }

                return Ok(snippet);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid parameters: accountId={AccountId}, snippetId={SnippetId}", 
                    accountId, snippetId);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling snippet {SnippetId} for account {AccountId}", 
                    snippetId, accountId);
                return StatusCode(500, new { error = "An error occurred while toggling the snippet" });
            }
        }

        /// <summary>
        /// Delete a snippet
        /// </summary>
        [HttpDelete("{snippetId}")]
        public async Task<IActionResult> Delete(long accountId, long snippetId)
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
                var deleted = await _snippetService.DeleteAsync(accountId, snippetId);
                if (!deleted)
                {
                    return NotFound(new { error = $"Snippet {snippetId} not found for account {accountId}" });
                }

                return NoContent();
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid parameters: accountId={AccountId}, snippetId={SnippetId}", 
                    accountId, snippetId);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting snippet {SnippetId} for account {AccountId}", 
                    snippetId, accountId);
                return StatusCode(500, new { error = "An error occurred while deleting the snippet" });
            }
        }
    }
}
