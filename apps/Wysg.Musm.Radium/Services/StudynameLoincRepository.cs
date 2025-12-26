using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Npgsql;
using NpgsqlTypes;
using System.Diagnostics;
using System.Net.Sockets;
using System.IO;

namespace Wysg.Musm.Radium.Services
{
    public sealed class StudynameLoincRepository : IStudynameLoincRepository
    {
        private readonly IRadiumLocalSettings _settings;
        private readonly ITenantContext? _tenant;
        private string? _mapTableName; // cache resolved table name
        private static int _openCounter = 0;
        private static int _methodCallCounter = 0;
        public StudynameLoincRepository(IRadiumLocalSettings settings, ITenantContext? tenant = null)
        {
            _settings = settings; _tenant = tenant;
            Debug.WriteLine($"[Repo][Init] StudynameLoincRepository constructed Thread={Environment.CurrentManagedThreadId}");
        }

        private static string GetFallbackLocalCs()
            => "Host=127.0.0.1;Port=5432;Database=wysg_dev;Username=postgres;Password=`123qweas";

        private NpgsqlConnection Open()
        {
            var raw = _settings.LocalConnectionString ?? GetFallbackLocalCs();
            var b = new NpgsqlConnectionStringBuilder(raw);
            // Avoid logging full password
            var redacted = $"Host={b.Host};Port={b.Port};Db={b.Database};User={b.Username};SSLMode={b.SslMode};Pooling={b.Pooling}";
            var id = Interlocked.Increment(ref _openCounter);
            Debug.WriteLine($"[Repo][Open#{id}] Creating connection {redacted} Thread={Environment.CurrentManagedThreadId}");
            try
            {
                var cn = new NpgsqlConnection(b.ConnectionString);
                return cn;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Repo][Open#{id}][EX] {ex.GetType().Name} {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        private static void LogNetworkException(string phase, Exception ex)
        {
            if (ex is SocketException se)
            {
                Debug.WriteLine($"[Repo][NET][{phase}] SocketException {se.SocketErrorCode} {se.Message}");
            }
            else if (ex is IOException io && io.InnerException is SocketException ise)
            {
                Debug.WriteLine($"[Repo][NET][{phase}] IO->{ise.SocketErrorCode} {ise.Message}");
            }
        }

        private static async Task<string?> ResolveExistingMapTableAsync(NpgsqlConnection cn)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                await using var cmd = new NpgsqlCommand("select coalesce(to_regclass('med.rad_studyname_loinc_part')::text, to_regclass('med.rad_studyname_loinc')::text)", cn);
                var o = await cmd.ExecuteScalarAsync();
                sw.Stop();
                Debug.WriteLine($"[Repo][ResolveMapTbl] Elapsed={sw.ElapsedMilliseconds}ms Result={(o as string) ?? "<null>"}");
                var s = o as string;
                return string.IsNullOrWhiteSpace(s) ? null : s;
            }
            catch (Exception ex)
            {
                sw.Stop();
                Debug.WriteLine($"[Repo][ResolveMapTbl][EX] {ex.GetType().Name} {ex.Message}");
                throw;
            }
        }

        private async Task<string> GetMapTableAsync(NpgsqlConnection cn)
        {
            if (!string.IsNullOrEmpty(_mapTableName)) return _mapTableName!;
            var tbl = await ResolveExistingMapTableAsync(cn) ?? throw new InvalidOperationException("Mapping table not found. Ensure med.rad_studyname_loinc_part or med.rad_studyname_loinc exists.");
            _mapTableName = tbl;
            return tbl;
        }

        public async Task<IReadOnlyList<StudynameRow>> GetStudynamesAsync()
        {
            var callId = Interlocked.Increment(ref _methodCallCounter);
            var sw = Stopwatch.StartNew();
            Debug.WriteLine($"[Repo][Call#{callId}] GetStudynamesAsync START Thread={Environment.CurrentManagedThreadId}");
            var list = new List<StudynameRow>();
            await using var cn = Open();
            try
            {
                try { await PgConnectionHelper.OpenWithLocalSslFallbackAsync(cn); }
                catch (Exception openEx) { LogNetworkException("Open", openEx); throw; }
                Debug.WriteLine($"[Repo][Call#{callId}] Connection Opened State={cn.FullState}");
                long tid = _tenant?.TenantId ?? 0L;
                string sql = tid > 0 ? "SELECT id, studyname FROM med.rad_studyname WHERE tenant_id=@tid ORDER BY studyname" : "SELECT id, studyname FROM med.rad_studyname ORDER BY studyname";
                await using var cmd = new NpgsqlCommand(sql, cn);
                if (tid > 0) cmd.Parameters.AddWithValue("@tid", tid);
                await using var rd = await cmd.ExecuteReaderAsync();
                while (await rd.ReadAsync()) list.Add(new StudynameRow(rd.GetInt64(0), rd.GetString(1)));
                sw.Stop();
                Debug.WriteLine($"[Repo][Call#{callId}] GetStudynamesAsync OK Rows={list.Count} Elapsed={sw.ElapsedMilliseconds}ms");
            }
            catch (PostgresException pex)
            {
                sw.Stop();
                Debug.WriteLine($"[Repo][Call#{callId}][PGX] SqlState={pex.SqlState} Msg={pex.MessageText} Table={pex.TableName} Schema={pex.SchemaName} Position={pex.Position}\n{pex.StackTrace}");
                Serilog.Log.Error(pex, "[StudynameRepo] GetStudynamesAsync PostgresException {Code} {Message}", pex.SqlState, pex.MessageText);
                throw;
            }
            catch (Exception ex)
            {
                sw.Stop();
                Debug.WriteLine($"[Repo][Call#{callId}][EX] {ex.GetType().Name} {ex.Message}\n{ex.StackTrace}");
                Serilog.Log.Error(ex, "[StudynameRepo] GetStudynamesAsync error");
                throw;
            }
            return list;
        }

        public async Task<IReadOnlyList<StudynameRow>> GetMappedStudynamesAsync()
        {
            var callId = Interlocked.Increment(ref _methodCallCounter);
            var sw = Stopwatch.StartNew();
            Debug.WriteLine($"[Repo][Call#{callId}] GetMappedStudynamesAsync START Thread={Environment.CurrentManagedThreadId}");
            var list = new List<StudynameRow>();
            await using var cn = Open();
            try
            {
                try { await PgConnectionHelper.OpenWithLocalSslFallbackAsync(cn); }
                catch (Exception openEx) { LogNetworkException("Open", openEx); throw; }
                Debug.WriteLine($"[Repo][Call#{callId}] Connection Opened State={cn.FullState}");
                var tbl = await GetMapTableAsync(cn);
                long tid = _tenant?.TenantId ?? 0L;
                string sql = tid > 0 
                    ? $@"SELECT DISTINCT s.id, s.studyname 
                         FROM med.rad_studyname s
                         INNER JOIN {tbl} m ON m.studyname_id = s.id
                         WHERE s.tenant_id=@tid 
                         ORDER BY s.studyname" 
                    : $@"SELECT DISTINCT s.id, s.studyname 
                         FROM med.rad_studyname s
                         INNER JOIN {tbl} m ON m.studyname_id = s.id
                         ORDER BY s.studyname";
                await using var cmd = new NpgsqlCommand(sql, cn);
                if (tid > 0) cmd.Parameters.AddWithValue("@tid", tid);
                await using var rd = await cmd.ExecuteReaderAsync();
                while (await rd.ReadAsync()) list.Add(new StudynameRow(rd.GetInt64(0), rd.GetString(1)));
                sw.Stop();
                Debug.WriteLine($"[Repo][Call#{callId}] GetMappedStudynamesAsync OK Rows={list.Count} Elapsed={sw.ElapsedMilliseconds}ms Table={tbl}");
            }
            catch (PostgresException pex)
            {
                sw.Stop();
                Debug.WriteLine($"[Repo][Call#{callId}][PGX] SqlState={pex.SqlState} Msg={pex.MessageText} Table={pex.TableName} Schema={pex.SchemaName} Position={pex.Position}\n{pex.StackTrace}");
                Serilog.Log.Error(pex, "[StudynameRepo] GetMappedStudynamesAsync PostgresException {Code} {Message}", pex.SqlState, pex.MessageText);
                throw;
            }
            catch (Exception ex)
            {
                sw.Stop();
                Debug.WriteLine($"[Repo][Call#{callId}][EX] {ex.GetType().Name} {ex.Message}\n{ex.StackTrace}");
                Serilog.Log.Error(ex, "[StudynameRepo] GetMappedStudynamesAsync error");
                throw;
            }
            return list;
        }

        public async Task<long> EnsureStudynameAsync(string studyname)
        {
            var callId = Interlocked.Increment(ref _methodCallCounter);
            var sw = Stopwatch.StartNew();
            Debug.WriteLine($"[Repo][Call#{callId}] EnsureStudynameAsync '{studyname}' START");
            await using var cn = Open();
            try
            {
                try { await PgConnectionHelper.OpenWithLocalSslFallbackAsync(cn); }
                catch (Exception openEx) { LogNetworkException("Open", openEx); throw; }
                long tid = _tenant?.TenantId ?? 0L;
                string sql = tid > 0
                    ? @"INSERT INTO med.rad_studyname(tenant_id, studyname)
VALUES (@tid, @n)
ON CONFLICT (tenant_id, studyname) DO UPDATE SET studyname = EXCLUDED.studyname
RETURNING id;"
                    : @"INSERT INTO med.rad_studyname(studyname)
VALUES (@n)
ON CONFLICT (studyname) DO UPDATE SET studyname = EXCLUDED.studyname
RETURNING id;";
                await using var cmd = new NpgsqlCommand(sql, cn);
                if (tid > 0) cmd.Parameters.AddWithValue("@tid", tid);
                cmd.Parameters.AddWithValue("@n", studyname);
                var id = (long)await cmd.ExecuteScalarAsync();
                sw.Stop();
                Debug.WriteLine($"[Repo][Call#{callId}] EnsureStudynameAsync OK Id={id} Elapsed={sw.ElapsedMilliseconds}ms");
                return id;
            }
            catch (PostgresException pex)
            {
                sw.Stop();
                Debug.WriteLine($"[Repo][Call#{callId}][PGX] SqlState={pex.SqlState} Msg={pex.MessageText} Table={pex.TableName} Schema={pex.SchemaName} Position={pex.Position}\n{pex.StackTrace}");
                Serilog.Log.Error(pex, "[StudynameRepo] EnsureStudynameAsync PostgresException {Code} {Message}", pex.SqlState, pex.MessageText);
                throw;
            }
        }

        public async Task<IReadOnlyList<PartRow>> GetPartsAsync()
        {
            var callId = Interlocked.Increment(ref _methodCallCounter);
            var sw = Stopwatch.StartNew();
            Debug.WriteLine($"[Repo][Call#{callId}] GetPartsAsync START");
            var list = new List<PartRow>();
            await using var cn = Open();
            try
            {
                try { await PgConnectionHelper.OpenWithLocalSslFallbackAsync(cn); }
                catch (Exception openEx) { LogNetworkException("Open", openEx); throw; }
                await using var cmd = new NpgsqlCommand("SELECT part_number, part_type_name, part_name FROM loinc.part ORDER BY part_type_name, part_name", cn);
                await using var rd = await cmd.ExecuteReaderAsync();
                while (await rd.ReadAsync())
                    list.Add(new PartRow(rd.GetString(0), rd.IsDBNull(1) ? string.Empty : rd.GetString(1), rd.IsDBNull(2) ? string.Empty : rd.GetString(2)));
                sw.Stop();
                Debug.WriteLine($"[Repo][Call#{callId}] GetPartsAsync OK Rows={list.Count} Elapsed={sw.ElapsedMilliseconds}ms");
            }
            catch (PostgresException pex)
            {
                sw.Stop();
                Debug.WriteLine($"[Repo][Call#{callId}][PGX] SqlState={pex.SqlState} Msg={pex.MessageText} Table={pex.TableName} Schema={pex.SchemaName} Position={pex.Position}");
                throw;
            }
            return list;
        }

        public async Task<IReadOnlyList<CommonPartRow>> GetCommonPartsAsync(int limit = 50)
        {
            var callId = Interlocked.Increment(ref _methodCallCounter);
            var sw = Stopwatch.StartNew();
            Debug.WriteLine($"[Repo][Call#{callId}] GetCommonPartsAsync START limit={limit}");
            var list = new List<CommonPartRow>();
            await using var cn = Open();
            try
            {
                try { await PgConnectionHelper.OpenWithLocalSslFallbackAsync(cn); }
                catch (Exception openEx) { LogNetworkException("Open", openEx); throw; }
                var tbl = await GetMapTableAsync(cn);
                var sql = $@"SELECT p.part_number, p.part_type_name, p.part_name, COUNT(*) AS usage
                         FROM {tbl} m
                         JOIN loinc.part p ON p.part_number = m.part_number
                         GROUP BY p.part_number, p.part_type_name, p.part_name
                         ORDER BY usage DESC, p.part_name
                         LIMIT @lim";
                await using var cmd = new NpgsqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@lim", limit);
                await using var rd = await cmd.ExecuteReaderAsync();
                while (await rd.ReadAsync())
                    list.Add(new CommonPartRow(rd.GetString(0), rd.IsDBNull(1) ? string.Empty : rd.GetString(1), rd.IsDBNull(2) ? string.Empty : rd.GetString(2), rd.GetInt64(3)));
                sw.Stop();
                Debug.WriteLine($"[Repo][Call#{callId}] GetCommonPartsAsync OK Rows={list.Count} Elapsed={sw.ElapsedMilliseconds}ms Table={tbl}");
            }
            catch (PostgresException pex)
            {
                sw.Stop();
                Debug.WriteLine($"[Repo][Call#{callId}][PGX] SqlState={pex.SqlState} Msg={pex.MessageText} Table={pex.TableName} Schema={pex.SchemaName} Position={pex.Position}");
                throw;
            }
            return list;
        }

        public async Task<IReadOnlyList<MappingRow>> GetMappingsAsync(long studynameId)
        {
            var callId = Interlocked.Increment(ref _methodCallCounter);
            var sw = Stopwatch.StartNew();
            Debug.WriteLine($"[Repo][Call#{callId}] GetMappingsAsync START studynameId={studynameId}");
            var list = new List<MappingRow>();
            await using var cn = Open();
            try
            {
                try { await PgConnectionHelper.OpenWithLocalSslFallbackAsync(cn); }
                catch (Exception openEx) { LogNetworkException("Open", openEx); throw; }
                var tbl = await GetMapTableAsync(cn);
                await using var cmd = new NpgsqlCommand($"SELECT part_number, part_sequence_order FROM {tbl} WHERE studyname_id=@id ORDER BY part_sequence_order, part_number", cn);
                cmd.Parameters.AddWithValue("@id", studynameId);
                await using var rd = await cmd.ExecuteReaderAsync();
                while (await rd.ReadAsync()) list.Add(new MappingRow(rd.GetString(0), rd.GetString(1)));
                sw.Stop();
                Debug.WriteLine($"[Repo][Call#{callId}] GetMappingsAsync OK Rows={list.Count} Elapsed={sw.ElapsedMilliseconds}ms Table={tbl}");
            }
            catch (PostgresException pex)
            {
                sw.Stop();
                Debug.WriteLine($"[Repo][Call#{callId}][PGX] SqlState={pex.SqlState} Msg={pex.MessageText} Table={pex.TableName} Schema={pex.SchemaName} Position={pex.Position}");
                throw;
            }
            return list;
        }

        public async Task SaveMappingsAsync(long studynameId, IEnumerable<MappingRow> items)
        {
            var callId = Interlocked.Increment(ref _methodCallCounter);
            var sw = Stopwatch.StartNew();
            Debug.WriteLine($"[Repo][Call#{callId}] SaveMappingsAsync START studynameId={studynameId}");
            await using var cn = Open();
            await PgConnectionHelper.OpenWithLocalSslFallbackAsync(cn); // open exceptions propagate to caller
            var tbl = await GetMapTableAsync(cn);
            await using var tx = await cn.BeginTransactionAsync();
            try
            {
                await using (var del = new NpgsqlCommand($"DELETE FROM {tbl} WHERE studyname_id=@id", cn, (NpgsqlTransaction)tx))
                {
                    del.Parameters.AddWithValue("@id", studynameId);
                    await del.ExecuteNonQueryAsync();
                }
                int inserted = 0;
                await using (var ins = new NpgsqlCommand($"INSERT INTO {tbl}(studyname_id, part_number, part_sequence_order) VALUES (@id, @p, @o)", cn, (NpgsqlTransaction)tx))
                {
                    var pId = ins.Parameters.Add("@id", NpgsqlDbType.Bigint);
                    var pNum = ins.Parameters.Add("@p", NpgsqlDbType.Text);
                    var pOrd = ins.Parameters.Add("@o", NpgsqlDbType.Text);
                    foreach (var it in items)
                    {
                        pId.Value = studynameId;
                        pNum.Value = it.PartNumber;
                        pOrd.Value = it.PartSequenceOrder ?? "A";
                        await ins.ExecuteNonQueryAsync();
                        inserted++;
                    }
                }
                await tx.CommitAsync();
                sw.Stop();
                Debug.WriteLine($"[Repo][Call#{callId}] SaveMappingsAsync COMMIT inserted={inserted} Elapsed={sw.ElapsedMilliseconds}ms Table={tbl}");
            }
            catch (PostgresException pex)
            {
                sw.Stop();
                Debug.WriteLine($"[Repo][Call#{callId}][PGX] SaveMappingsAsync ROLLBACK SqlState={pex.SqlState} Msg={pex.MessageText} Table={pex.TableName}");
                try { await tx.RollbackAsync(); } catch { }
                throw;
            }
        }

        public async Task<IReadOnlyList<PlaybookMatchRow>> GetPlaybookMatchesAsync(IEnumerable<string> partNumbers)
        {
            var callId = Interlocked.Increment(ref _methodCallCounter);
            var sw = Stopwatch.StartNew();
            Debug.WriteLine($"[Repo][Call#{callId}] GetPlaybookMatchesAsync START parts={string.Join(',', partNumbers ?? Array.Empty<string>())}");
            var list = new List<PlaybookMatchRow>();
            var numbers = partNumbers?.Distinct().ToArray() ?? Array.Empty<string>();
            if (numbers.Length < 2)
            {
                Debug.WriteLine($"[Repo][Call#{callId}] GetPlaybookMatchesAsync SKIP parts<3");
                return list;
            }
            await using var cn = Open();
            try
            {
                try { await PgConnectionHelper.OpenWithLocalSslFallbackAsync(cn); }
                catch (Exception openEx) { LogNetworkException("Open", openEx); throw; }
                var sql = @"SELECT rb.loinc_number, max(rb.long_common_name) AS long_common_name
                        FROM loinc.rplaybook rb
                        WHERE rb.part_number = ANY(@nums)
                        GROUP BY rb.loinc_number
                        HAVING COUNT(DISTINCT rb.part_number) = @n
                        ORDER BY max(rb.long_common_name)";
                await using var cmd = new NpgsqlCommand(sql, cn);
                var pNums = cmd.Parameters.Add("@nums", NpgsqlDbType.Array | NpgsqlDbType.Text);
                pNums.Value = numbers;
                cmd.Parameters.Add("@n", NpgsqlDbType.Integer).Value = numbers.Length;
                await using var rd = await cmd.ExecuteReaderAsync();
                while (await rd.ReadAsync()) list.Add(new PlaybookMatchRow(rd.GetString(0), rd.IsDBNull(1) ? string.Empty : rd.GetString(1)));
                sw.Stop();
                Debug.WriteLine($"[Repo][Call#{callId}] GetPlaybookMatchesAsync OK Rows={list.Count} Elapsed={sw.ElapsedMilliseconds}ms");
            }
            catch (PostgresException pex)
            {
                sw.Stop();
                Debug.WriteLine($"[Repo][Call#{callId}][PGX] SqlState={pex.SqlState} Msg={pex.MessageText}");
                throw;
            }
            return list;
        }

        public async Task<IReadOnlyList<PlaybookPartDetailRow>> GetPlaybookPartsAsync(string loincNumber)
        {
            var callId = Interlocked.Increment(ref _methodCallCounter);
            var sw = Stopwatch.StartNew();
            Debug.WriteLine($"[Repo][Call#{callId}] GetPlaybookPartsAsync START loinc={loincNumber}");
            var list = new List<PlaybookPartDetailRow>();
            await using var cn = Open();
            try
            {
                try { await PgConnectionHelper.OpenWithLocalSslFallbackAsync(cn); }
                catch (Exception openEx) { LogNetworkException("Open", openEx); throw; }
                var sql = @"SELECT part_number, coalesce(part_name,''), coalesce(part_sequence_order,'A')
                        FROM loinc.rplaybook
                        WHERE loinc_number=@id
                        ORDER BY part_sequence_order, part_type_name, part_name";
                await using var cmd = new NpgsqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@id", loincNumber);
                await using var rd = await cmd.ExecuteReaderAsync();
                while (await rd.ReadAsync()) list.Add(new PlaybookPartDetailRow(rd.GetString(0), rd.GetString(1), rd.GetString(2)));
                sw.Stop();
                Debug.WriteLine($"[Repo][Call#{callId}] GetPlaybookPartsAsync OK Rows={list.Count} Elapsed={sw.ElapsedMilliseconds}ms");
            }
            catch (PostgresException pex)
            {
                sw.Stop();
                Debug.WriteLine($"[Repo][Call#{callId}][PGX] SqlState={pex.SqlState} Msg={pex.MessageText}");
                throw;
            }
            return list;
        }

        public async Task<StudynameDbDiagnostics> GetDiagnosticsAsync()
        {
            var callId = Interlocked.Increment(ref _methodCallCounter);
            var sw = Stopwatch.StartNew();
            Debug.WriteLine($"[Repo][Call#{callId}] GetDiagnosticsAsync START");
            await using var cn = Open();
            await PgConnectionHelper.OpenWithLocalSslFallbackAsync(cn);
            var builder = new NpgsqlConnectionStringBuilder(cn.ConnectionString);
            var src = _settings.LocalConnectionString != null && cn.ConnectionString.Contains(_settings.LocalConnectionString) ? "LocalConnectionString" : "Fallback";
            var mapTable = await GetMapTableAsync(cn);
            long studynameCount = 0, studyCount = 0, mappingCount = 0;
            long tid = _tenant?.TenantId ?? 0L;
            if (tid > 0)
            {
                await using (var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM med.rad_studyname WHERE tenant_id=@tid", cn)) { cmd.Parameters.AddWithValue("@tid", tid); studynameCount = (long)await cmd.ExecuteScalarAsync(); }
                await using (var cmd = new NpgsqlCommand(@"SELECT COUNT(*) FROM med.rad_study s
JOIN med.patient p ON p.id = s.patient_id AND p.tenant_id=@tid", cn)) { cmd.Parameters.AddWithValue("@tid", tid); studyCount = (long)await cmd.ExecuteScalarAsync(); }
            }
            else
            {
                await using (var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM med.rad_studyname", cn)) studynameCount = (long)await cmd.ExecuteScalarAsync();
                await using (var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM med.rad_study", cn)) studyCount = (long)await cmd.ExecuteScalarAsync();
            }
            await using (var cmd = new NpgsqlCommand($"SELECT COUNT(*) FROM {mapTable}", cn)) mappingCount = (long)await cmd.ExecuteScalarAsync();
            sw.Stop();
            Debug.WriteLine($"[Repo][Call#{callId}] GetDiagnosticsAsync OK Elapsed={sw.ElapsedMilliseconds}ms");
            return new StudynameDbDiagnostics(builder.Database, builder.Username, builder.Host, builder.Port, studynameCount, studyCount, mappingCount, mapTable, src, DateTime.UtcNow);
        }
    }
}
