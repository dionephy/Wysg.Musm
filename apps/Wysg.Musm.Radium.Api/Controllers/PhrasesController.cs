using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wysg.Musm.Radium.Api.Extensions;
using Wysg.Musm.Radium.Api.Models.Dtos;
using Wysg.Musm.Radium.Api.Repositories;

namespace Wysg.Musm.Radium.Api.Controllers
{
    /// <summary>
    /// Controller for phrase management operations
    /// Phrases are text completions/auto-suggestions for radiological reporting
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("api/accounts/{accountId}/[controller]")]
    public class PhrasesController : ControllerBase
    {
        private readonly IPhraseRepository _phraseRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly ILogger<PhrasesController> _logger;

        public PhrasesController(
            IPhraseRepository phraseRepository,
            IAccountRepository accountRepository,
            ILogger<PhrasesController> logger)
        {
            _phraseRepository = phraseRepository ?? throw new ArgumentNullException(nameof(phraseRepository));
            _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets all phrases for an account
        /// </summary>
        /// <param name="accountId">Account ID</param>
        /// <param name="activeOnly">Filter to active phrases only</param>
        /// <returns>List of phrases</returns>
        [HttpGet]
        [ProducesResponseType(typeof(List<PhraseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<List<PhraseDto>>> GetAll(long accountId, [FromQuery] bool activeOnly = false)
        {
            if (!await VerifyAccountOwnershipAsync(accountId))
                return StatusCode(403, new { error = "Cannot access different user's phrases" });

            try
            {
                var phrases = await _phraseRepository.GetAllAsync(accountId, activeOnly);
                _logger.LogInformation("Retrieved {Count} phrases for account {AccountId}", phrases.Count, accountId);
                return Ok(phrases);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving phrases for account {AccountId}", accountId);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Gets a specific phrase by ID
        /// </summary>
        /// <param name="accountId">Account ID</param>
        /// <param name="phraseId">Phrase ID</param>
        /// <returns>Phrase details</returns>
        [HttpGet("{phraseId}")]
        [ProducesResponseType(typeof(PhraseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PhraseDto>> GetById(long accountId, long phraseId)
        {
            if (!await VerifyAccountOwnershipAsync(accountId))
                return StatusCode(403, new { error = "Cannot access different user's phrases" });

            try
            {
                var phrase = await _phraseRepository.GetByIdAsync(phraseId, accountId);
                if (phrase == null)
                {
                    _logger.LogWarning("Phrase {PhraseId} not found for account {AccountId}", phraseId, accountId);
                    return NotFound($"Phrase {phraseId} not found");
                }

                return Ok(phrase);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving phrase {PhraseId} for account {AccountId}", phraseId, accountId);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Searches phrases by text query
        /// </summary>
        /// <param name="accountId">Account ID</param>
        /// <param name="query">Search query (partial text match)</param>
        /// <param name="activeOnly">Filter to active phrases only</param>
        /// <param name="maxResults">Maximum number of results (default 100)</param>
        /// <returns>List of matching phrases</returns>
        [HttpGet("search")]
        [ProducesResponseType(typeof(List<PhraseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<List<PhraseDto>>> Search(
            long accountId,
            [FromQuery] string? query,
            [FromQuery] bool activeOnly = true,
            [FromQuery] int maxResults = 100)
        {
            if (!await VerifyAccountOwnershipAsync(accountId))
                return StatusCode(403, new { error = "Cannot access different user's phrases" });

            try
            {
                var phrases = await _phraseRepository.SearchAsync(accountId, query, activeOnly, maxResults);
                _logger.LogInformation("Search found {Count} phrases for account {AccountId} with query '{Query}'",
                    phrases.Count, accountId, query);
                return Ok(phrases);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching phrases for account {AccountId}", accountId);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Creates or updates a phrase (upsert by text)
        /// </summary>
        /// <param name="accountId">Account ID</param>
        /// <param name="request">Phrase data</param>
        /// <returns>Created or updated phrase</returns>
        [HttpPut]
        [ProducesResponseType(typeof(PhraseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<PhraseDto>> Upsert(long accountId, [FromBody] UpsertPhraseRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!await VerifyAccountOwnershipAsync(accountId))
                return StatusCode(403, new { error = "Cannot modify different user's phrases" });

            if (string.IsNullOrWhiteSpace(request.Text))
                return BadRequest("Phrase text cannot be empty");

            try
            {
                var phrase = await _phraseRepository.UpsertAsync(accountId, request.Text.Trim(), request.Active);
                _logger.LogInformation("Upserted phrase {PhraseId} for account {AccountId}", phrase.Id, accountId);
                return Ok(phrase);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error upserting phrase for account {AccountId}", accountId);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Batch upsert multiple phrases
        /// </summary>
        /// <param name="accountId">Account ID</param>
        /// <param name="request">List of phrases to upsert</param>
        /// <returns>List of created/updated phrases</returns>
        [HttpPut("batch")]
        [ProducesResponseType(typeof(List<PhraseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<List<PhraseDto>>> BatchUpsert(long accountId, [FromBody] BatchUpsertPhrasesRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!await VerifyAccountOwnershipAsync(accountId))
                return StatusCode(403, new { error = "Cannot modify different user's phrases" });

            if (request.Phrases == null || request.Phrases.Count == 0)
                return BadRequest("Phrases list cannot be empty");

            try
            {
                var phrases = await _phraseRepository.BatchUpsertAsync(accountId, request.Phrases, request.Active);
                _logger.LogInformation("Batch upserted {Count} phrases for account {AccountId}", phrases.Count, accountId);
                return Ok(phrases);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error batch upserting phrases for account {AccountId}", accountId);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Converts the specified account phrases into global phrases (account_id NULL).
        /// </summary>
        [HttpPost("convert-global")]
        [ProducesResponseType(typeof(ConvertToGlobalPhrasesResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ConvertToGlobalPhrasesResponse>> ConvertToGlobal(long accountId, [FromBody] ConvertToGlobalPhrasesRequest request)
        {
            if (!await VerifyAccountOwnershipAsync(accountId))
                return StatusCode(403, new { error = "Cannot modify different user's phrases" });

            if (request?.PhraseIds == null || request.PhraseIds.Count == 0)
                return BadRequest("PhraseIds cannot be empty");

            try
            {
                var (converted, duplicatesRemoved) = await _phraseRepository.ConvertToGlobalAsync(accountId, request.PhraseIds);
                var response = new ConvertToGlobalPhrasesResponse
                {
                    Converted = converted,
                    DuplicatesRemoved = duplicatesRemoved
                };
                _logger.LogInformation("Converted {Converted} phrases to global for account {AccountId} (duplicates skipped: {Duplicates})", converted, accountId, duplicatesRemoved);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting phrases to global for account {AccountId}", accountId);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Toggles the active status of a phrase
        /// </summary>
        /// <param name="accountId">Account ID</param>
        /// <param name="phraseId">Phrase ID</param>
        /// <returns>Success status</returns>
        [HttpPost("{phraseId}/toggle")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Toggle(long accountId, long phraseId)
        {
            if (!await VerifyAccountOwnershipAsync(accountId))
                return StatusCode(403, new { error = "Cannot modify different user's phrases" });

            try
            {
                var success = await _phraseRepository.ToggleActiveAsync(phraseId, accountId);
                if (!success)
                {
                    _logger.LogWarning("Phrase {PhraseId} not found for account {AccountId}", phraseId, accountId);
                    return NotFound($"Phrase {phraseId} not found");
                }

                _logger.LogInformation("Toggled phrase {PhraseId} for account {AccountId}", phraseId, accountId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling phrase {PhraseId} for account {AccountId}", phraseId, accountId);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Deletes a phrase by ID
        /// </summary>
        /// <param name="accountId">Account ID</param>
        /// <param name="phraseId">Phrase ID</param>
        /// <returns>Success status</returns>
        [HttpDelete("{phraseId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(long accountId, long phraseId)
        {
            if (!await VerifyAccountOwnershipAsync(accountId))
                return StatusCode(403, new { error = "Cannot delete different user's phrases" });

            try
            {
                var success = await _phraseRepository.DeleteAsync(phraseId, accountId);
                if (!success)
                {
                    _logger.LogWarning("Phrase {PhraseId} not found for account {AccountId}", phraseId, accountId);
                    return NotFound($"Phrase {phraseId} not found");
                }

                _logger.LogInformation("Deleted phrase {PhraseId} for account {AccountId}", phraseId, accountId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting phrase {PhraseId} for account {AccountId}", phraseId, accountId);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Gets the maximum revision number for an account
        /// </summary>
        /// <param name="accountId">Account ID</param>
        /// <returns>Maximum revision number</returns>
        [HttpGet("revision")]
        [ProducesResponseType(typeof(long), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<long>> GetMaxRevision(long accountId)
        {
            if (!await VerifyAccountOwnershipAsync(accountId))
                return StatusCode(403, new { error = "Cannot access different user's phrases" });

            try
            {
                var maxRev = await _phraseRepository.GetMaxRevisionAsync(accountId);
                return Ok(maxRev);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting max revision for account {AccountId}", accountId);
                return StatusCode(500, "Internal server error");
            }
        }

        private async Task<bool> VerifyAccountOwnershipAsync(long accountId)
        {
            var tokenUid = User.GetFirebaseUid();
            if (string.IsNullOrEmpty(tokenUid))
            {
                _logger.LogWarning("JWT token does not contain UID claim");
                return false;
            }

            var account = await _accountRepository.GetAccountByIdAsync(accountId);
            if (account == null)
            {
                _logger.LogWarning("Account {AccountId} not found", accountId);
                return false;
            }

            if (account.Uid != tokenUid)
            {
                _logger.LogWarning("UID mismatch: Token UID {TokenUid} does not match account UID {AccountUid}", tokenUid, account.Uid);
                return false;
            }

            return true;
        }
    }
}
