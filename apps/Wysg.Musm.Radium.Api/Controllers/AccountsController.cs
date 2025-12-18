using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wysg.Musm.Radium.Api.Extensions;
using Wysg.Musm.Radium.Api.Models.Dtos;
using Wysg.Musm.Radium.Api.Repositories;

namespace Wysg.Musm.Radium.Api.Controllers
{
    /// <summary>
    /// Controller for account management operations.
    /// NOTE: User settings endpoints are handled by UserSettingsController to avoid route conflicts.
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class AccountsController : ControllerBase
    {
        private readonly IAccountRepository _accountRepository;
        private readonly ILogger<AccountsController> _logger;

        public AccountsController(
            IAccountRepository accountRepository,
            ILogger<AccountsController> logger)
        {
            _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Ensures account exists and returns account info with settings.
        /// Creates account if not found, updates if exists.
        /// </summary>
        /// <param name="request">Account information from Firebase</param>
        /// <returns>Account information and settings</returns>
        /// <response code="200">Account ensured successfully</response>
        /// <response code="400">Invalid request</response>
        /// <response code="401">Unauthorized - invalid or missing Firebase token</response>
        /// <response code="403">Forbidden - UID mismatch</response>
        [HttpPost("ensure")]
        [ProducesResponseType(typeof(EnsureAccountResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<EnsureAccountResponse>> EnsureAccount(
            [FromBody] EnsureAccountRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Get UID from Firebase JWT token
            var tokenUid = User.GetFirebaseUid();
            if (string.IsNullOrEmpty(tokenUid))
            {
                _logger.LogWarning("JWT token does not contain UID claim");
                return Unauthorized("Invalid token: missing UID");
            }

            // SECURITY: Ensure JWT UID matches the requested UID
            if (tokenUid != request.Uid)
            {
                _logger.LogWarning(
                    "UID mismatch: Token UID {TokenUid} does not match request UID {RequestUid}",
                    tokenUid, request.Uid);
                return StatusCode(403, new { error = "Cannot create/update account for different user" });
            }

            try
            {
                // Ensure account exists
                var account = await _accountRepository.EnsureAccountAsync(
                    request.Uid,
                    request.Email,
                    request.DisplayName ?? string.Empty);

                // Update last login
                await _accountRepository.UpdateLastLoginAsync(account.AccountId);

                // Get settings
                var settingsJson = await _accountRepository.GetReportifySettingsAsync(account.AccountId);

                _logger.LogInformation(
                    "Account ensured: AccountId={AccountId}, Uid={Uid}, Email={Email}",
                    account.AccountId, account.Uid, account.Email);

                return Ok(new EnsureAccountResponse
                {
                    Account = account,
                    SettingsJson = settingsJson
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ensuring account for UID {Uid}", request.Uid);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Updates last login timestamp for an account.
        /// </summary>
        /// <param name="accountId">Account ID</param>
        /// <returns>Success status</returns>
        /// <response code="204">Last login updated successfully</response>
        /// <response code="401">Unauthorized - invalid or missing Firebase token</response>
        /// <response code="403">Forbidden - not your account</response>
        /// <response code="404">Account not found</response>
        [HttpPost("{accountId}/login")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateLastLogin(long accountId)
        {
            // Get UID from Firebase JWT token
            var tokenUid = User.GetFirebaseUid();
            if (string.IsNullOrEmpty(tokenUid))
            {
                _logger.LogWarning("JWT token does not contain UID claim");
                return Unauthorized("Invalid token: missing UID");
            }

            try
            {
                // SECURITY: Verify this account belongs to the authenticated user
                var account = await _accountRepository.GetAccountByIdAsync(accountId);
                if (account == null)
                {
                    _logger.LogWarning("Account {AccountId} not found", accountId);
                    return NotFound($"Account {accountId} not found");
                }

                if (account.Uid != tokenUid)
                {
                    _logger.LogWarning(
                        "UID mismatch: Token UID {TokenUid} does not match account UID {AccountUid}",
                        tokenUid, account.Uid);
                    return StatusCode(403, new { error = "Cannot update login for different user" });
                }

                // Update last login
                await _accountRepository.UpdateLastLoginAsync(accountId);

                _logger.LogInformation("Last login updated for account {AccountId}", accountId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating last login for account {AccountId}", accountId);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Gets account information by account ID.
        /// </summary>
        /// <param name="accountId">Account ID</param>
        /// <returns>Account information</returns>
        /// <response code="200">Account found</response>
        /// <response code="401">Unauthorized - invalid or missing Firebase token</response>
        /// <response code="403">Forbidden - not your account</response>
        /// <response code="404">Account not found</response>
        [HttpGet("{accountId}")]
        [ProducesResponseType(typeof(AccountDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AccountDto>> GetAccount(long accountId)
        {
            // Get UID from Firebase JWT token
            var tokenUid = User.GetFirebaseUid();
            if (string.IsNullOrEmpty(tokenUid))
            {
                _logger.LogWarning("JWT token does not contain UID claim");
                return Unauthorized("Invalid token: missing UID");
            }

            try
            {
                var account = await _accountRepository.GetAccountByIdAsync(accountId);
                if (account == null)
                {
                    _logger.LogWarning("Account {AccountId} not found", accountId);
                    return NotFound($"Account {accountId} not found");
                }

                // SECURITY: Only allow access to own account
                if (account.Uid != tokenUid)
                {
                    _logger.LogWarning(
                        "UID mismatch: Token UID {TokenUid} does not match account UID {AccountUid}",
                        tokenUid, account.Uid);
                    return StatusCode(403, new { error = "Cannot access different user's account" });
                }

                return Ok(account);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving account {AccountId}", accountId);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
