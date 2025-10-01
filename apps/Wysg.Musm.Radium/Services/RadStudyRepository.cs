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
    }

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
            if (string.IsNullOrWhiteSpace(patientNumber)) return;
            long patientId = 0;
            long studynameId = 0;
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
                    isMale = s.StartsWith("M"); // handles M / Male
                }
                DateTime? birthDate = null;
                if (!string.IsNullOrWhiteSpace(birthDateRaw))
                {
                    // Accept common formats; fallback to DateTime.TryParse
                    if (DateTime.TryParse(birthDateRaw.Trim(), out var bd)) birthDate = bd.Date;
                }

                // Upsert patient (include birth_date)
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

                if (!string.IsNullOrWhiteSpace(studyName))
                {
                    await using (var cmd = new NpgsqlCommand(@"INSERT INTO med.rad_studyname(studyname)
VALUES (@sn) ON CONFLICT (studyname) DO UPDATE SET studyname = EXCLUDED.studyname RETURNING id;", cn, (NpgsqlTransaction)tx))
                    {
                        cmd.Parameters.AddWithValue("@sn", studyName.Trim());
                        var o = await cmd.ExecuteScalarAsync();
                        if (o is long l) studynameId = l;
                    }
                }

                if (patientId > 0 && studynameId > 0 && studyDateTime != null)
                {
                    await using (var cmd = new NpgsqlCommand(@"INSERT INTO med.rad_study(patient_id, studyname_id, study_datetime)
VALUES (@pid, @sid, @dt)
ON CONFLICT (patient_id, studyname_id, study_datetime) DO NOTHING;", cn, (NpgsqlTransaction)tx))
                    {
                        cmd.Parameters.AddWithValue("@pid", patientId);
                        cmd.Parameters.AddWithValue("@sid", studynameId);
                        cmd.Parameters.AddWithValue("@dt", studyDateTime.Value);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                await tx.CommitAsync();
            }
            catch (PostgresException pex)
            {
                try { await tx.RollbackAsync(); } catch { }
                Debug.WriteLine($"[RadStudyRepo][PGX] {pex.SqlState} {pex.MessageText}");
            }
            catch (Exception ex)
            {
                try { await tx.RollbackAsync(); } catch { }
                Debug.WriteLine("[RadStudyRepo] EnsurePatientStudyAsync error: " + ex.Message);
            }
        }
    }
}
