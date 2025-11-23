using Wysg.Musm.Radium.Api.Models.Dtos;

namespace Wysg.Musm.Radium.Api.Repositories
{
    public interface IExportedReportRepository
    {
        Task<(List<ExportedReportDto> reports, int totalCount)> GetAllByAccountAsync(
            long accountId, 
            bool unresolvedOnly, 
            DateTime? startDate,
            DateTime? endDate,
            int page, 
            int pageSize);
            
        Task<ExportedReportDto?> GetByIdAsync(long id, long accountId);
        
        Task<ExportedReportDto> CreateAsync(long accountId, CreateExportedReportRequest request);
        
        Task<bool> MarkResolvedAsync(long id, long accountId);
        
        Task<bool> DeleteAsync(long id, long accountId);
        
        Task<ExportedReportStatsDto> GetStatsAsync(long accountId);
    }
}
