using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Wysg.Musm.Radium.Services.ApiClients
{
    /// <summary>
    /// Exported Report DTO (matches API)
    /// </summary>
    public class ExportedReportDto
    {
        public long Id { get; set; }
        public long AccountId { get; set; }
        public string Report { get; set; } = string.Empty;
        public DateTime ReportDateTime { get; set; }
        public DateTime UploadedAt { get; set; }
        public bool IsResolved { get; set; }
    }

    /// <summary>
    /// Create exported report request
    /// </summary>
    public class CreateExportedReportRequest
    {
        public string Report { get; set; } = string.Empty;
        public DateTime ReportDateTime { get; set; }
    }

    /// <summary>
    /// Paginated response
    /// </summary>
    public class PaginatedExportedReportsResponse
    {
        public List<ExportedReportDto> Data { get; set; } = new();
        public PaginationInfo Pagination { get; set; } = new();
    }

    /// <summary>
    /// Pagination info
    /// </summary>
    public class PaginationInfo
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
    }

    /// <summary>
    /// Exported report statistics
    /// </summary>
    public class ExportedReportStatsDto
    {
        public int TotalReports { get; set; }
        public int ResolvedReports { get; set; }
        public int UnresolvedReports { get; set; }
        public DateTime? LatestReportDateTime { get; set; }
    }

    /// <summary>
    /// API client for exported reports operations
    /// </summary>
    public interface IExportedReportsApiClient
    {
        Task<PaginatedExportedReportsResponse> GetAllAsync(
            long accountId, 
            bool unresolvedOnly = false, 
            DateTime? startDate = null,
            DateTime? endDate = null,
            int page = 1, 
            int pageSize = 50);
        Task<ExportedReportDto?> GetByIdAsync(long accountId, long id);
        Task<ExportedReportDto> CreateAsync(long accountId, string report, DateTime reportDateTime);
        Task MarkResolvedAsync(long accountId, long id);
        Task DeleteAsync(long accountId, long id);
        Task<ExportedReportStatsDto> GetStatsAsync(long accountId);
        Task<bool> IsAvailableAsync();
    }

    /// <summary>
    /// Implementation of exported reports API client
    /// </summary>
    public class ExportedReportsApiClient : ApiClientBase, IExportedReportsApiClient
    {
        public ExportedReportsApiClient(HttpClient httpClient, string baseUrl)
            : base(httpClient, baseUrl)
        {
        }

        public async Task<PaginatedExportedReportsResponse> GetAllAsync(
            long accountId, 
            bool unresolvedOnly = false, 
            DateTime? startDate = null,
            DateTime? endDate = null,
            int page = 1, 
            int pageSize = 50)
        {
            try
            {
                Debug.WriteLine($"[ExportedReportsApiClient] Getting reports for account {accountId}, page {page}");
                
                var queryParams = new List<string>
                {
                    $"unresolvedOnly={unresolvedOnly}",
                    $"page={page}",
                    $"pageSize={pageSize}"
                };

                if (startDate.HasValue)
                {
                    queryParams.Add($"startDate={startDate.Value:yyyy-MM-ddTHH:mm:ssZ}");
                }

                if (endDate.HasValue)
                {
                    queryParams.Add($"endDate={endDate.Value:yyyy-MM-ddTHH:mm:ssZ}");
                }

                var queryString = string.Join("&", queryParams);
                var endpoint = $"/api/accounts/{accountId}/exported-reports?{queryString}";
                
                return await GetAsync<PaginatedExportedReportsResponse>(endpoint) 
                       ?? new PaginatedExportedReportsResponse();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ExportedReportsApiClient] Error getting reports: {ex.Message}");
                throw;
            }
        }

        public async Task<ExportedReportDto?> GetByIdAsync(long accountId, long id)
        {
            try
            {
                Debug.WriteLine($"[ExportedReportsApiClient] Getting report {id} for account {accountId}");
                return await GetAsync<ExportedReportDto>($"/api/accounts/{accountId}/exported-reports/{id}");
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                Debug.WriteLine($"[ExportedReportsApiClient] Report {id} not found");
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ExportedReportsApiClient] Error getting report: {ex.Message}");
                throw;
            }
        }

        public async Task<ExportedReportDto> CreateAsync(long accountId, string report, DateTime reportDateTime)
        {
            if (string.IsNullOrWhiteSpace(report))
            {
                throw new ArgumentException("Report content cannot be empty", nameof(report));
            }

            try
            {
                Debug.WriteLine($"[ExportedReportsApiClient] Creating report for account {accountId}");
                var request = new CreateExportedReportRequest 
                { 
                    Report = report, 
                    ReportDateTime = reportDateTime 
                };
                var result = await PostAsync<ExportedReportDto>($"/api/accounts/{accountId}/exported-reports", request);
                
                if (result == null)
                {
                    throw new InvalidOperationException("Create returned null result");
                }
                
                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ExportedReportsApiClient] Error creating report: {ex.Message}");
                throw;
            }
        }

        public async Task MarkResolvedAsync(long accountId, long id)
        {
            try
            {
                Debug.WriteLine($"[ExportedReportsApiClient] Marking report {id} as resolved");
                await PostAsync($"/api/accounts/{accountId}/exported-reports/{id}/resolve", new { });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ExportedReportsApiClient] Error marking report as resolved: {ex.Message}");
                throw;
            }
        }

        public async Task DeleteAsync(long accountId, long id)
        {
            try
            {
                Debug.WriteLine($"[ExportedReportsApiClient] Deleting report {id}");
                await DeleteAsync($"/api/accounts/{accountId}/exported-reports/{id}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ExportedReportsApiClient] Error deleting report: {ex.Message}");
                throw;
            }
        }

        public async Task<ExportedReportStatsDto> GetStatsAsync(long accountId)
        {
            try
            {
                Debug.WriteLine($"[ExportedReportsApiClient] Getting stats for account {accountId}");
                return await GetAsync<ExportedReportStatsDto>($"/api/accounts/{accountId}/exported-reports/stats") 
                       ?? new ExportedReportStatsDto();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ExportedReportsApiClient] Error getting stats: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> IsAvailableAsync()
        {
            return await IsApiAvailableAsync();
        }
    }
}
