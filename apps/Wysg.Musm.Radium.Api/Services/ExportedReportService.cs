using Wysg.Musm.Radium.Api.Models.Dtos;
using Wysg.Musm.Radium.Api.Repositories;

namespace Wysg.Musm.Radium.Api.Services
{
    public sealed class ExportedReportService : IExportedReportService
    {
        private readonly IExportedReportRepository _repository;
        private readonly ILogger<ExportedReportService> _logger;

        public ExportedReportService(
            IExportedReportRepository repository, 
            ILogger<ExportedReportService> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<(List<ExportedReportDto> reports, int totalCount)> GetAllByAccountAsync(
            long accountId, 
            bool unresolvedOnly, 
            DateTime? startDate,
            DateTime? endDate,
            int page, 
            int pageSize)
        {
            if (accountId <= 0)
            {
                throw new ArgumentException("Account ID must be positive", nameof(accountId));
            }

            if (page < 1)
            {
                throw new ArgumentException("Page must be greater than 0", nameof(page));
            }

            if (pageSize < 1 || pageSize > 100)
            {
                throw new ArgumentException("Page size must be between 1 and 100", nameof(pageSize));
            }

            _logger.LogInformation(
                "Getting exported reports for account {AccountId}, page {Page}, size {PageSize}, unresolvedOnly={UnresolvedOnly}", 
                accountId, page, pageSize, unresolvedOnly);

            return await _repository.GetAllByAccountAsync(accountId, unresolvedOnly, startDate, endDate, page, pageSize);
        }

        public async Task<ExportedReportDto?> GetByIdAsync(long id, long accountId)
        {
            if (id <= 0)
            {
                throw new ArgumentException("Report ID must be positive", nameof(id));
            }

            if (accountId <= 0)
            {
                throw new ArgumentException("Account ID must be positive", nameof(accountId));
            }

            return await _repository.GetByIdAsync(id, accountId);
        }

        public async Task<ExportedReportDto> CreateAsync(long accountId, CreateExportedReportRequest request)
        {
            if (accountId <= 0)
            {
                throw new ArgumentException("Account ID must be positive", nameof(accountId));
            }

            if (string.IsNullOrWhiteSpace(request.Report))
            {
                throw new ArgumentException("Report content cannot be empty", nameof(request));
            }

            if (request.ReportDateTime > DateTime.UtcNow.AddDays(1))
            {
                throw new ArgumentException("Report datetime cannot be in the future", nameof(request));
            }

            _logger.LogInformation("Creating exported report for account {AccountId}", accountId);

            return await _repository.CreateAsync(accountId, request);
        }

        public async Task<bool> MarkResolvedAsync(long id, long accountId)
        {
            if (id <= 0)
            {
                throw new ArgumentException("Report ID must be positive", nameof(id));
            }

            if (accountId <= 0)
            {
                throw new ArgumentException("Account ID must be positive", nameof(accountId));
            }

            _logger.LogInformation("Marking exported report {Id} as resolved", id);

            return await _repository.MarkResolvedAsync(id, accountId);
        }

        public async Task<bool> DeleteAsync(long id, long accountId)
        {
            if (id <= 0)
            {
                throw new ArgumentException("Report ID must be positive", nameof(id));
            }

            if (accountId <= 0)
            {
                throw new ArgumentException("Account ID must be positive", nameof(accountId));
            }

            _logger.LogInformation("Deleting exported report {Id}", id);

            return await _repository.DeleteAsync(id, accountId);
        }

        public async Task<ExportedReportStatsDto> GetStatsAsync(long accountId)
        {
            if (accountId <= 0)
            {
                throw new ArgumentException("Account ID must be positive", nameof(accountId));
            }

            return await _repository.GetStatsAsync(accountId);
        }
    }
}
