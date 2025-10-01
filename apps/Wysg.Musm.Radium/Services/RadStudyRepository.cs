using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Npgsql;
using System.Text.Json;

namespace Wysg.Musm.Radium.Services
{
    public interface IRadStudyRepository
    {
        Task EnsurePatientStudyAsync(string patientNumber, string? patientName, string? sex, string? birthDateRaw, string? studyName, DateTime? studyDateTime);
        Task<long?> EnsureStudyAsync(string patientNumber, string? patientName, string? sex, string? birthDateRaw, string? studyName, DateTime? studyDateTime);
        Task<long?> InsertPartialReportAsync(long studyId, string? createdBy, DateTime? reportDateTime, string reportJson, bool isMine, bool isCreated);
        Task<List<PatientReportRow>> GetReportsForPatientAsync(string patientNumber);
    }

    public sealed record PatientReportRow(long StudyId, DateTime StudyDateTime, string ReportJson);

    public sealed class RadStudyRepository : IRadStudyRepository
    {
        private readonly IRadiumLocalSettings _settings;
        public RadStudyRepository(IRadiumLocalSettings settings) { _settings = settings; }

        private static string GetFallbackLocalCs() => "Host=127.0.0.1;Port=5432;Database=wysg_dev;Username=postgres;Password=`123qweas";

        private NpgsqlConnection Open()
        {
            var raw = _settings.LocalConnectionString ?? GetFallbackLocalCs();
            return new NpgsqlConnection(raw);
        }

        public async Task EnsurePatientStudyAsync(string patientNumber, string? patientName, string? sex, string? birthDateRaw, string? studyName, DateTime? studyDateTime)
        {
            _ = await EnsureStudyAsync(patientNumber, patientName, sex, birthDateRaw, studyName, studyDateTime);
        }

        public async Task<long?> EnsureStudyAsync(string patientNumber, string? patientName, string? sex, string? birthDateRaw, string? studyName, DateTime? studyDateTime)
        {
            if (string.IsNullOrWhiteSpace(patientNumber) || string.IsNullOrWhiteSpace(studyName) || studyDateTime == null) return null;
            long patientId = 0; long studynameId = 0; long studyId = 0;
            await using var cn = Open();
            await PgConnectionHelper.OpenWithLocalSslFallbackAsync(cn);
            await using var tx = await cn.BeginTransactionAsync();
            try
            {
                // Normalize fields
                var name = string.IsNullOrWhiteSpace(patientName) ? patientNumber : patientName!.Trim();
                bool isMale = false;
                if (!string.IsNullOrWhiteSpace(sex))
                {
                    var s = sex.Trim().ToUpperInvariant();
                    isMale = s.StartsWith("M");
                }
                DateTime? birthDate = null;
                if (!string.IsNullOrWhiteSpace(birthDateRaw) && DateTime.TryParse(birthDateRaw.Trim(), out var bd)) birthDate = bd.Date;

                // Upsert patient
                await using (var cmd = new NpgsqlCommand(@"INSERT INTO med.patient(patient_number, patient_name, is_male, birth_date)
VALUES (@num, @name, @male, @bdate)
ON CONFLICT (patient_number) DO UPDATE SET
  patient_name = COALESCE(EXCLUDED.patient_name, med.patient.patient_name),
  is_male = EXCLUDED.is_male,
  birth_date = COALESCE(EXCLUDED.birth_date, med.patient.birth_date)
RETURNING id;", cn, (NpgsqlTransaction)tx))
                {
                    cmd.Parameters.AddWithValue("@num", patientNumber);
                    cmd.Parameters.AddWithValue("@name", name);
                    cmd.Parameters.AddWithValue("@male", isMale);
                    cmd.Parameters.AddWithValue("@bdate", birthDate.HasValue ? birthDate.Value : (object)DBNull.Value);
                    var o = await cmd.ExecuteScalarAsync();
                    if (o is long l) patientId = l;
                }

                await using (var cmd = new NpgsqlCommand(@"INSERT INTO med.rad_studyname(studyname)
VALUES (@sn) ON CONFLICT (studyname) DO UPDATE SET studyname = EXCLUDED.studyname RETURNING id;", cn, (NpgsqlTransaction)tx))
                {
                    cmd.Parameters.AddWithValue("@sn", studyName!.Trim());
                    var o = await cmd.ExecuteScalarAsync();
                    if (o is long l) studynameId = l;
                }

                if (patientId > 0 && studynameId > 0 && studyDateTime != null)
                {
                    await using (var cmd = new NpgsqlCommand(@"INSERT INTO med.rad_study(patient_id, studyname_id, study_datetime)
VALUES (@pid, @sid, @dt)
ON CONFLICT (patient_id, studyname_id, study_datetime) DO UPDATE SET study_datetime = EXCLUDED.study_datetime
RETURNING id;", cn, (NpgsqlTransaction)tx))
                    {
                        cmd.Parameters.AddWithValue("@pid", patientId);
                        cmd.Parameters.AddWithValue("@sid", studynameId);
                        cmd.Parameters.AddWithValue("@dt", studyDateTime.Value);
                        var o = await cmd.ExecuteScalarAsync();
                        if (o is long l) studyId = l;
                    }
                }

                await tx.CommitAsync();
                return studyId == 0 ? null : studyId;
            }
            catch (PostgresException pex)
            {
                try { await tx.RollbackAsync(); } catch { }
                Debug.WriteLine($"[RadStudyRepo][EnsureStudy][PGX] {pex.SqlState} {pex.MessageText}");
                return null;
            }
            catch (Exception ex)
            {
                try { await tx.RollbackAsync(); } catch { }
                Debug.WriteLine("[RadStudyRepo] EnsureStudy error: " + ex.Message);
                return null;
            }
        }

        public async Task<long?> InsertPartialReportAsync(long studyId, string? createdBy, DateTime? reportDateTime, string reportJson, bool isMine, bool isCreated)
        {
            await using var cn = Open();
            await PgConnectionHelper.OpenWithLocalSslFallbackAsync(cn);
            try
            {
                await using var cmd = new NpgsqlCommand(@"INSERT INTO med.rad_report(study_id, is_created, is_mine, created_by, report_datetime, report)
VALUES (@sid, @isCreated, @isMine, @createdBy, @dt, @json)
RETURNING id;", cn);
                cmd.Parameters.AddWithValue("@sid", studyId);
                cmd.Parameters.AddWithValue("@isCreated", isCreated);
                cmd.Parameters.AddWithValue("@isMine", isMine);
                cmd.Parameters.AddWithValue("@createdBy", (object?)createdBy ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@dt", reportDateTime.HasValue ? reportDateTime.Value : (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@json", NpgsqlTypes.NpgsqlDbType.Jsonb, reportJson);
                var o = await cmd.ExecuteScalarAsync();
                return o is long l ? l : null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[RadStudyRepo] InsertPartialReport error: " + ex.Message);
                return null;
            }
        }

        public async Task<List<PatientReportRow>> GetReportsForPatientAsync(string patientNumber)
        {
            var list = new List<PatientReportRow>();
            if (string.IsNullOrWhiteSpace(patientNumber)) return list;
            await using var cn = Open();
            await PgConnectionHelper.OpenWithLocalSslFallbackAsync(cn);
            try
            {
                await using var cmd = new NpgsqlCommand(@"SELECT rs.id, rs.study_datetime, rr.report
FROM med.rad_study rs
JOIN med.patient p ON p.id = rs.patient_id AND p.patient_number = @num
JOIN med.rad_report rr ON rr.study_id = rs.id -- only studies having reports
WHERE (rr.report ->> 'header_and_findings') IS NOT NULL OR (rr.report ->> 'conclusion') IS NOT NULL
ORDER BY rs.study_datetime DESC;", cn);
                cmd.Parameters.AddWithValue("@num", patientNumber);
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    long studyId = reader.GetInt64(0);
                    DateTime dt = reader.GetDateTime(1);
                    string json = reader.IsDBNull(2) ? "{}" : reader.GetString(2);
                    list.Add(new PatientReportRow(studyId, dt, json));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[RadStudyRepo] GetReportsForPatient error: " + ex.Message);
            }
            return list;
        }
    }
}
