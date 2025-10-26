using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Npgsql;

namespace Wysg.Musm.Radium.Services
{
    public interface IRadStudyRepository
    {
        Task EnsurePatientStudyAsync(string patientNumber, string? patientName, string? sex, string? birthDateRaw, string? studyName, DateTime? studyDateTime);
        Task<long?> EnsureStudyAsync(string patientNumber, string? patientName, string? sex, string? birthDateRaw, string? studyName, DateTime? studyDateTime);
        Task<long?> UpsertPartialReportAsync(long studyId, DateTime? reportDateTime, string reportJson, bool isMine);
        Task<List<PatientReportRow>> GetReportsForPatientAsync(string patientNumber);
        Task<long?> GetStudyIdAsync(string patientNumber, string studyName, DateTime studyDateTime);
    }

    public sealed record PatientReportRow(long StudyId, DateTime StudyDateTime, string Studyname, DateTime? ReportDateTime, string ReportJson);

    public sealed class RadStudyRepository : IRadStudyRepository
    {
        private readonly IRadiumLocalSettings _settings;
        private readonly ITenantContext? _tenant;
        public RadStudyRepository(IRadiumLocalSettings settings, ITenantContext? tenant = null) { _settings = settings; _tenant = tenant; }
        private static string GetFallbackLocalCs() => "Host=127.0.0.1;Port=5432;Database=wysg_dev;Username=postgres;Password=`123qweas";
        private NpgsqlConnection Open() => new(_settings.LocalConnectionString ?? GetFallbackLocalCs());
        private long Tid => _tenant?.TenantId ?? 0L;

        public async Task EnsurePatientStudyAsync(string patientNumber, string? patientName, string? sex, string? birthDateRaw, string? studyName, DateTime? studyDateTime)
            => _ = await EnsureStudyAsync(patientNumber, patientName, sex, birthDateRaw, studyName, studyDateTime);

        public async Task<long?> EnsureStudyAsync(string patientNumber, string? patientName, string? sex, string? birthDateRaw, string? studyName, DateTime? studyDateTime)
        {
            if (string.IsNullOrWhiteSpace(patientNumber) || string.IsNullOrWhiteSpace(studyName) || studyDateTime == null) return null;
            long patientId = 0; long studynameId = 0; long studyId = 0;
            await using var cn = Open();
            await PgConnectionHelper.OpenWithLocalSslFallbackAsync(cn);
            await using var tx = await cn.BeginTransactionAsync();
            try
            {
                var name = string.IsNullOrWhiteSpace(patientName) ? patientNumber : patientName!.Trim();
                bool isMale = !string.IsNullOrWhiteSpace(sex) && sex.Trim().StartsWith("M", System.StringComparison.OrdinalIgnoreCase);
                DateTime? birthDate = null;
                if (!string.IsNullOrWhiteSpace(birthDateRaw) && DateTime.TryParse(birthDateRaw.Trim(), out var bd)) birthDate = bd.Date;

                await using (var cmd = new NpgsqlCommand(@"INSERT INTO med.patient(tenant_id, patient_number, patient_name, is_male, birth_date)
VALUES (@tid, @num, @name, @male, @bdate)
ON CONFLICT (tenant_id, patient_number) DO UPDATE SET
  patient_name = COALESCE(EXCLUDED.patient_name, med.patient.patient_name),
  is_male = EXCLUDED.is_male,
  birth_date = COALESCE(EXCLUDED.birth_date, med.patient.birth_date)
RETURNING id;", cn, (NpgsqlTransaction)tx))
                {
                    cmd.Parameters.AddWithValue("@tid", Tid);
                    cmd.Parameters.AddWithValue("@num", patientNumber);
                    cmd.Parameters.AddWithValue("@name", name);
                    cmd.Parameters.AddWithValue("@male", isMale);
                    cmd.Parameters.AddWithValue("@bdate", birthDate.HasValue ? birthDate.Value : (object)DBNull.Value);
                    var o = await cmd.ExecuteScalarAsync(); if (o is long l) patientId = l;
                }
                await using (var cmd = new NpgsqlCommand(@"INSERT INTO med.rad_studyname(tenant_id, studyname)
VALUES (@tid, @sn) ON CONFLICT (tenant_id, studyname) DO UPDATE SET studyname = EXCLUDED.studyname RETURNING id;", cn, (NpgsqlTransaction)tx))
                {
                    cmd.Parameters.AddWithValue("@tid", Tid);
                    cmd.Parameters.AddWithValue("@sn", studyName!.Trim());
                    var o = await cmd.ExecuteScalarAsync(); if (o is long l) studynameId = l;
                }
                if (patientId > 0 && studynameId > 0 && studyDateTime != null)
                {
                    await using var cmd = new NpgsqlCommand(@"INSERT INTO med.rad_study(patient_id, studyname_id, study_datetime)
VALUES (@pid, @sid, @dt)
ON CONFLICT (patient_id, studyname_id, study_datetime) DO UPDATE SET study_datetime = EXCLUDED.study_datetime
RETURNING id;", cn, (NpgsqlTransaction)tx);
                    cmd.Parameters.AddWithValue("@pid", patientId);
                    cmd.Parameters.AddWithValue("@sid", studynameId);
                    cmd.Parameters.AddWithValue("@dt", studyDateTime.Value);
                    var o = await cmd.ExecuteScalarAsync(); if (o is long l) studyId = l;
                }
                await tx.CommitAsync();
                return studyId == 0 ? null : studyId;
            }
            catch (PostgresException pex) { try { await tx.RollbackAsync(); } catch { } Debug.WriteLine($"[RadStudyRepo][EnsureStudy][PGX] {pex.SqlState} {pex.MessageText}"); return null; }
            catch (Exception ex) { try { await tx.RollbackAsync(); } catch { } Debug.WriteLine("[RadStudyRepo] EnsureStudy error: " + ex.Message); return null; }
        }

        public async Task<long?> UpsertPartialReportAsync(long studyId, DateTime? reportDateTime, string reportJson, bool isMine)
        {
            await using var cn = Open();
            await PgConnectionHelper.OpenWithLocalSslFallbackAsync(cn);
            try
            {
                await using var cmd = new NpgsqlCommand(@"INSERT INTO med.rad_report(study_id, is_mine, report_datetime, report)
VALUES (@sid, @isMine, @dt, @json)
ON CONFLICT (study_id, report_datetime) DO UPDATE SET
  is_mine = EXCLUDED.is_mine,
  report = EXCLUDED.report
RETURNING id;", cn);
                cmd.Parameters.AddWithValue("@sid", studyId);
                cmd.Parameters.AddWithValue("@isMine", isMine);
                cmd.Parameters.AddWithValue("@dt", reportDateTime.HasValue ? reportDateTime.Value : (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@json", NpgsqlTypes.NpgsqlDbType.Jsonb, reportJson);
                var o = await cmd.ExecuteScalarAsync();
                return o is long l ? l : null;
            }
            catch (Exception ex) { Debug.WriteLine("[RadStudyRepo] UpsertPartialReport error: " + ex.Message); return null; }
        }

        public async Task<List<PatientReportRow>> GetReportsForPatientAsync(string patientNumber)
        {
            var list = new List<PatientReportRow>(); if (string.IsNullOrWhiteSpace(patientNumber)) return list;
            await using var cn = Open(); await PgConnectionHelper.OpenWithLocalSslFallbackAsync(cn);
            try
            {
                await using var cmd = new NpgsqlCommand(@"SELECT rs.id, rs.study_datetime, sn.studyname, rr.report_datetime, rr.report
FROM med.rad_study rs
JOIN med.patient p ON p.id = rs.patient_id AND p.tenant_id=@tid AND p.patient_number = @num
JOIN med.rad_studyname sn ON sn.id = rs.studyname_id
JOIN med.rad_report rr ON rr.study_id = rs.id
WHERE (rr.report ->> 'header_and_findings') IS NOT NULL
   OR (rr.report ->> 'final_conclusion') IS NOT NULL
   OR (rr.report ->> 'conclusion') IS NOT NULL
ORDER BY rs.study_datetime DESC, rr.report_datetime DESC NULLS LAST;", cn);
                cmd.Parameters.AddWithValue("@tid", Tid);
                cmd.Parameters.AddWithValue("@num", patientNumber);
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    long sid = reader.GetInt64(0);
                    DateTime studyDt = reader.GetDateTime(1);
                    string studyname = reader.IsDBNull(2) ? string.Empty : reader.GetString(2);
                    DateTime? reportDt = reader.IsDBNull(3) ? null : reader.GetDateTime(3);
                    string json = reader.IsDBNull(4) ? "{}" : reader.GetString(4);
                    list.Add(new PatientReportRow(sid, studyDt, studyname, reportDt, json));
                }
            }
            catch (Exception ex) { Debug.WriteLine("[RadStudyRepo] GetReportsForPatient error: " + ex.Message); }
            return list;
        }
        
        public async Task<long?> GetStudyIdAsync(string patientNumber, string studyName, DateTime studyDateTime)
        {
            if (string.IsNullOrWhiteSpace(patientNumber) || string.IsNullOrWhiteSpace(studyName)) return null;
            
            await using var cn = Open();
            await PgConnectionHelper.OpenWithLocalSslFallbackAsync(cn);
            
            try
            {
                await using var cmd = new NpgsqlCommand(@"SELECT rs.id
FROM med.rad_study rs
JOIN med.patient p ON p.id = rs.patient_id AND p.tenant_id=@tid AND p.patient_number = @num
JOIN med.rad_studyname sn ON sn.id = rs.studyname_id AND sn.studyname = @sname
WHERE rs.study_datetime = @dt
LIMIT 1;", cn);
                cmd.Parameters.AddWithValue("@tid", Tid);
                cmd.Parameters.AddWithValue("@num", patientNumber);
                cmd.Parameters.AddWithValue("@sname", studyName);
                cmd.Parameters.AddWithValue("@dt", studyDateTime);
                
                var result = await cmd.ExecuteScalarAsync();
                return result is long l ? l : null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[RadStudyRepo] GetStudyIdAsync error: {ex.Message}");
                return null;
            }
        }
    }
}
