using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wysg.Musm.Radium.Api.Extensions;
using Wysg.Musm.Radium.Api.Models.Dtos;
using Wysg.Musm.Radium.Api.Services;

namespace Wysg.Musm.Radium.Api.Controllers
{
    /// <summary>
    /// Exported reports management endpoints
    /// </summary>
    [ApiController]
    [Authorize] // Require Firebase authentication
    [Route("api/accounts/{accountId}/exported-reports")]
    [Produces("application/json")]
    public sealed class ExportedReportsController : ControllerBase
    {
        private readonly IExportedReportService _exportedReportService;
        private readonly ILogger<ExportedReportsController> _logger;

        public ExportedReportsController(
            IExportedReportService exportedReportService, 
            ILogger<ExportedReportsController> logger)
        {
            _exportedReportService = exportedReportService ?? throw new ArgumentNullException(nameof(exportedReportService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Get all exported reports for an account (paginated)
        /// </summary>
        /// <param name="accountId">Account ID</param>
        /// <param name="unresolvedOnly">Filter to unresolved reports only</param>
        /// <param name="startDate">Filter by report date (start)</param>
        /// <param name="endDate">Filter by report date (end)</param>
        /// <param name="page">Page number (default 1)</param>
        /// <param name="pageSize">Page size (default 50, max 100)</param>
        /// <returns>Paginated list of exported reports</returns>
        [HttpGet]
        [ProducesResponseType(typeof(PaginatedExportedReportsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<PaginatedExportedReportsResponse>> GetAll(
            long accountId,
            [FromQuery] bool unresolvedOnly = false,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
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
                var (reports, totalCount) = await _exportedReportService.GetAllByAccountAsync(
                    accountId, unresolvedOnly, startDate, endDate, page, pageSize);

                var response = new PaginatedExportedReportsResponse
                {
                    Data = reports,
                    Pagination = new PaginationInfo
                    {
                        Page = page,
                        PageSize = pageSize,
                        TotalCount = totalCount,
                        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                    }
                };

                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid parameters for GetAll");
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting exported reports for account {AccountId}", accountId);
                return StatusCode(500, new { error = "An error occurred while retrieving exported reports" });
            }
        }

        /// <summary>
        /// Get a specific exported report by ID
        /// </summary>
        /// <param name="accountId">Account ID</param>
        /// <param name="id">Report ID</param>
        /// <returns>Exported report details</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ExportedReportDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ExportedReportDto>> GetById(long accountId, long id)
        {
            // Validate user has access to this account
            var userAccountId = User.GetAccountId();
            if (userAccountId.HasValue && userAccountId.Value != accountId)
            {
                return Forbid();
            }

            try
            {
                var report = await _exportedReportService.GetByIdAsync(id, accountId);
                if (report == null)
                {
                    return NotFound(new { error = $"Exported report {id} not found for account {accountId}" });
                }

                return Ok(report);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid parameters: accountId={AccountId}, id={Id}", accountId, id);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting exported report {Id} for account {AccountId}", id, accountId);
                return StatusCode(500, new { error = "An error occurred while retrieving the exported report" });
            }
        }

        /// <summary>
        /// Create a new exported report
        /// </summary>
        /// <param name="accountId">Account ID</param>
        /// <param name="request">Report data</param>
        /// <returns>Created exported report</returns>
        [HttpPost]
        [ProducesResponseType(typeof(ExportedReportDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ExportedReportDto>> Create(
            long accountId, 
            [FromBody] CreateExportedReportRequest request)
        {
            // Validate user has access to this account
            var userAccountId = User.GetAccountId();
            if (userAccountId.HasValue && userAccountId.Value != accountId)
            {
                return Forbid();
            }

            try
            {
                var report = await _exportedReportService.CreateAsync(accountId, request);
                return CreatedAtAction(nameof(GetById), new { accountId, id = report.Id }, report);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid create request for account {AccountId}", accountId);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating exported report for account {AccountId}", accountId);
                return StatusCode(500, new { error = "An error occurred while creating the exported report" });
            }
        }

        /// <summary>
        /// Mark an exported report as resolved
        /// </summary>
        /// <param name="accountId">Account ID</param>
        /// <param name="id">Report ID</param>
        /// <returns>No content on success</returns>
        [HttpPost("{id}/resolve")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> MarkResolved(long accountId, long id)
        {
            // Validate user has access to this account
            var userAccountId = User.GetAccountId();
            if (userAccountId.HasValue && userAccountId.Value != accountId)
            {
                return Forbid();
            }

            try
            {
                var success = await _exportedReportService.MarkResolvedAsync(id, accountId);
                if (!success)
                {
                    return NotFound(new { error = $"Exported report {id} not found for account {accountId}" });
                }

                return NoContent();
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid parameters: accountId={AccountId}, id={Id}", accountId, id);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking exported report {Id} as resolved", id);
                return StatusCode(500, new { error = "An error occurred while marking the report as resolved" });
            }
        }

        /// <summary>
        /// Delete an exported report
        /// </summary>
        /// <param name="accountId">Account ID</param>
        /// <param name="id">Report ID</param>
        /// <returns>No content on success</returns>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Delete(long accountId, long id)
        {
            // Validate user has access to this account
            var userAccountId = User.GetAccountId();
            if (userAccountId.HasValue && userAccountId.Value != accountId)
            {
                return Forbid();
            }

            try
            {
                var success = await _exportedReportService.DeleteAsync(id, accountId);
                if (!success)
                {
                    return NotFound(new { error = $"Exported report {id} not found for account {accountId}" });
                }

                return NoContent();
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid parameters: accountId={AccountId}, id={Id}", accountId, id);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting exported report {Id}", id);
                return StatusCode(500, new { error = "An error occurred while deleting the exported report" });
            }
        }

        /// <summary>
        /// Get statistics for exported reports
        /// </summary>
        /// <param name="accountId">Account ID</param>
        /// <returns>Report statistics</returns>
        [HttpGet("stats")]
        [ProducesResponseType(typeof(ExportedReportStatsDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ExportedReportStatsDto>> GetStats(long accountId)
        {
            // Validate user has access to this account
            var userAccountId = User.GetAccountId();
            if (userAccountId.HasValue && userAccountId.Value != accountId)
            {
                return Forbid();
            }

            try
            {
                var stats = await _exportedReportService.GetStatsAsync(accountId);
                return Ok(stats);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid account ID: {AccountId}", accountId);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stats for account {AccountId}", accountId);
                return StatusCode(500, new { error = "An error occurred while retrieving statistics" });
            }
        }
    }
}
