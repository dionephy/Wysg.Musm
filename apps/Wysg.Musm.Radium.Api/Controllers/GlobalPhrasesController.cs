using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wysg.Musm.Radium.Api.Models.Dtos;
using Wysg.Musm.Radium.Api.Repositories;

namespace Wysg.Musm.Radium.Api.Controllers
{
    /// <summary>
    /// Global Phrases API - manages phrases shared across all accounts (account_id IS NULL).
    /// These are predefined medical terms/templates available to all users.
    /// Requires authentication but no account-specific authorization.
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("api/phrases/global")]
    public class GlobalPhrasesController : ControllerBase
    {
        private readonly IPhraseRepository _repo;
        private readonly ILogger<GlobalPhrasesController> _logger;

        public GlobalPhrasesController(IPhraseRepository repo, ILogger<GlobalPhrasesController> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        /// <summary>
        /// Get all global phrases (account_id IS NULL).
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<PhraseDto>>> GetAll([FromQuery] bool activeOnly = false)
        {
            _logger.LogInformation("GetAll global phrases called. activeOnly={ActiveOnly}", activeOnly);
            var list = await _repo.GetAllGlobalAsync(activeOnly);
            _logger.LogInformation("Returning {Count} global phrases", list.Count);
            return Ok(list);
        }

        /// <summary>
        /// Search global phrases by query string.
        /// </summary>
        [HttpGet("search")]
        public async Task<ActionResult<List<PhraseDto>>> Search(
            [FromQuery] string? query,
            [FromQuery] bool activeOnly = true,
            [FromQuery] int maxResults = 100)
        {
            _logger.LogInformation("Search global phrases. query={Query}, activeOnly={ActiveOnly}, maxResults={MaxResults}",
                query, activeOnly, maxResults);
            var list = await _repo.SearchGlobalAsync(query, activeOnly, maxResults);
            _logger.LogInformation("Search returned {Count} global phrases", list.Count);
            return Ok(list);
        }

        /// <summary>
        /// Create or update a global phrase (idempotent upsert by text).
        /// </summary>
        [HttpPut]
        public async Task<ActionResult<PhraseDto>> Upsert([FromBody] UpsertPhraseRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Text))
            {
                _logger.LogWarning("Upsert called with empty text");
                return BadRequest("Phrase text cannot be empty");
            }

            _logger.LogInformation("Upserting global phrase: {Text}", request.Text);
            var dto = await _repo.UpsertGlobalAsync(request.Text.Trim(), request.Active);
            _logger.LogInformation("Upserted global phrase id={Id}", dto.Id);
            return Ok(dto);
        }

        /// <summary>
        /// Toggle active status of a global phrase.
        /// </summary>
        [HttpPost("{phraseId}/toggle")]
        public async Task<IActionResult> Toggle(long phraseId)
        {
            _logger.LogInformation("Toggle global phrase id={PhraseId}", phraseId);
            var success = await _repo.ToggleGlobalActiveAsync(phraseId);
            if (!success)
            {
                _logger.LogWarning("Global phrase not found: id={PhraseId}", phraseId);
                return NotFound();
            }
            _logger.LogInformation("Toggled global phrase id={PhraseId}", phraseId);
            return NoContent();
        }

        /// <summary>
        /// Delete a global phrase (soft delete - sets active=false).
        /// </summary>
        [HttpDelete("{phraseId}")]
        public async Task<IActionResult> Delete(long phraseId)
        {
            _logger.LogInformation("Delete global phrase id={PhraseId}", phraseId);
            var success = await _repo.DeleteGlobalAsync(phraseId);
            if (!success)
            {
                _logger.LogWarning("Global phrase not found: id={PhraseId}", phraseId);
                return NotFound();
            }
            _logger.LogInformation("Deleted global phrase id={PhraseId}", phraseId);
            return NoContent();
        }

        /// <summary>
        /// Get max revision number for cache invalidation.
        /// </summary>
        [HttpGet("revision")]
        public async Task<ActionResult<long>> GetRevision()
        {
            var rev = await _repo.GetGlobalMaxRevisionAsync();
            _logger.LogInformation("Global phrases max revision: {Rev}", rev);
            return Ok(rev);
        }
    }
}
