namespace Wysg.Musm.Radium.Api.Models.Dtos
{
    /// <summary>
    /// Exported report DTO
    /// </summary>
    public sealed class ExportedReportDto
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
    public sealed class CreateExportedReportRequest
    {
        public required string Report { get; set; }
        public required DateTime ReportDateTime { get; set; }
    }

    /// <summary>
    /// Paginated exported reports response
    /// </summary>
    public sealed class PaginatedExportedReportsResponse
    {
        public IReadOnlyList<ExportedReportDto> Data { get; set; } = Array.Empty<ExportedReportDto>();
        public PaginationInfo Pagination { get; set; } = new();
    }

    /// <summary>
    /// Exported reports statistics
    /// </summary>
    public sealed class ExportedReportStatsDto
    {
        public int TotalReports { get; set; }
        public int ResolvedReports { get; set; }
        public int UnresolvedReports { get; set; }
        public DateTime? LatestReportDateTime { get; set; }
    }
}
