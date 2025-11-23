using Microsoft.Data.SqlClient;
using Wysg.Musm.Radium.Api.Models.Dtos;

namespace Wysg.Musm.Radium.Api.Repositories
{
    public sealed class ExportedReportRepository : IExportedReportRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;
        private readonly ILogger<ExportedReportRepository> _logger;

        public ExportedReportRepository(
            ISqlConnectionFactory connectionFactory, 
            ILogger<ExportedReportRepository> logger)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
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
            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();

            // Build WHERE clause
            var whereClause = "WHERE account_id = @accountId";
            if (unresolvedOnly)
                whereClause += " AND is_resolved = 0";
            if (startDate.HasValue)
                whereClause += " AND report_datetime >= @startDate";
            if (endDate.HasValue)
                whereClause += " AND report_datetime <= @endDate";

            // Get total count
            var countSql = $"SELECT COUNT(*) FROM radium.exported_report {whereClause}";
            int totalCount;
            
            await using (var countCmd = new SqlCommand(countSql, connection) { CommandTimeout = 30 })
            {
                countCmd.Parameters.AddWithValue("@accountId", accountId);
                if (unresolvedOnly)
                    countCmd.Parameters.AddWithValue("@isResolved", 0);
                if (startDate.HasValue)
                    countCmd.Parameters.AddWithValue("@startDate", startDate.Value);
                if (endDate.HasValue)
                    countCmd.Parameters.AddWithValue("@endDate", endDate.Value);
                    
                totalCount = (int)await countCmd.ExecuteScalarAsync();
            }

            // Get paginated data
            var dataSql = $@"
                SELECT id, account_id, report, report_datetime, uploaded_at, is_resolved
                FROM radium.exported_report
                {whereClause}
                ORDER BY report_datetime DESC
                OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY";

            var reports = new List<ExportedReportDto>();
            
            await using (var dataCmd = new SqlCommand(dataSql, connection) { CommandTimeout = 30 })
            {
                dataCmd.Parameters.AddWithValue("@accountId", accountId);
                if (unresolvedOnly)
                    dataCmd.Parameters.AddWithValue("@isResolved", 0);
                if (startDate.HasValue)
                    dataCmd.Parameters.AddWithValue("@startDate", startDate.Value);
                if (endDate.HasValue)
                    dataCmd.Parameters.AddWithValue("@endDate", endDate.Value);
                dataCmd.Parameters.AddWithValue("@offset", (page - 1) * pageSize);
                dataCmd.Parameters.AddWithValue("@pageSize", pageSize);

                await using var reader = await dataCmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    reports.Add(MapToDto(reader));
                }
            }

            return (reports, totalCount);
        }

        public async Task<ExportedReportDto?> GetByIdAsync(long id, long accountId)
        {
            const string sql = @"
                SELECT id, account_id, report, report_datetime, uploaded_at, is_resolved
                FROM radium.exported_report
                WHERE id = @id AND account_id = @accountId";

            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection) { CommandTimeout = 30 };
            command.Parameters.AddWithValue("@id", id);
            command.Parameters.AddWithValue("@accountId", accountId);

            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapToDto(reader);
            }

            return null;
        }

        public async Task<ExportedReportDto> CreateAsync(long accountId, CreateExportedReportRequest request)
        {
            const string sql = @"
                INSERT INTO radium.exported_report(account_id, report, report_datetime, uploaded_at, is_resolved)
                OUTPUT INSERTED.id, INSERTED.account_id, INSERTED.report, INSERTED.report_datetime, 
                       INSERTED.uploaded_at, INSERTED.is_resolved
                VALUES(@accountId, @report, @reportDateTime, SYSUTCDATETIME(), 0)";

            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection) { CommandTimeout = 30 };
            command.Parameters.AddWithValue("@accountId", accountId);
            command.Parameters.AddWithValue("@report", request.Report);
            command.Parameters.AddWithValue("@reportDateTime", request.ReportDateTime);

            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                _logger.LogInformation("Created exported report for account {AccountId}", accountId);
                return MapToDto(reader);
            }

            throw new InvalidOperationException("Failed to create exported report");
        }

        public async Task<bool> MarkResolvedAsync(long id, long accountId)
        {
            const string sql = @"
                UPDATE radium.exported_report
                SET is_resolved = 1
                WHERE id = @id AND account_id = @accountId";

            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection) { CommandTimeout = 30 };
            command.Parameters.AddWithValue("@id", id);
            command.Parameters.AddWithValue("@accountId", accountId);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            
            if (rowsAffected > 0)
            {
                _logger.LogInformation("Marked exported report {Id} as resolved", id);
                return true;
            }

            return false;
        }

        public async Task<bool> DeleteAsync(long id, long accountId)
        {
            const string sql = @"
                DELETE FROM radium.exported_report
                WHERE id = @id AND account_id = @accountId";

            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection) { CommandTimeout = 30 };
            command.Parameters.AddWithValue("@id", id);
            command.Parameters.AddWithValue("@accountId", accountId);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            
            if (rowsAffected > 0)
            {
                _logger.LogInformation("Deleted exported report {Id}", id);
                return true;
            }

            return false;
        }

        public async Task<ExportedReportStatsDto> GetStatsAsync(long accountId)
        {
            const string sql = @"
                SELECT 
                    COUNT(*) as TotalReports,
                    SUM(CASE WHEN is_resolved = 1 THEN 1 ELSE 0 END) as ResolvedReports,
                    SUM(CASE WHEN is_resolved = 0 THEN 1 ELSE 0 END) as UnresolvedReports,
                    MAX(report_datetime) as LatestReportDateTime
                FROM radium.exported_report
                WHERE account_id = @accountId";

            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection) { CommandTimeout = 30 };
            command.Parameters.AddWithValue("@accountId", accountId);

            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new ExportedReportStatsDto
                {
                    TotalReports = reader.GetInt32(0),
                    ResolvedReports = reader.GetInt32(1),
                    UnresolvedReports = reader.GetInt32(2),
                    LatestReportDateTime = reader.IsDBNull(3) ? null : reader.GetDateTime(3)
                };
            }

            return new ExportedReportStatsDto();
        }

        private static ExportedReportDto MapToDto(SqlDataReader reader)
        {
            return new ExportedReportDto
            {
                Id = reader.GetInt64(0),
                AccountId = reader.GetInt64(1),
                Report = reader.GetString(2),
                ReportDateTime = reader.GetDateTime(3),
                UploadedAt = reader.GetDateTime(4),
                IsResolved = reader.GetBoolean(5)
            };
        }
    }
}
