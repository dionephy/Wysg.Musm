using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace Wysg.Musm.Radium.Services
{
    /// <summary>
    /// Azure SQL implementation that operates on radium.* and snomed.* tables created in central_db(azure)_20251015.sql
    /// </summary>
    public sealed class AzureSqlSnomedMapService : ISnomedMapService
    {
        private readonly IRadiumLocalSettings _settings;
        public AzureSqlSnomedMapService(IRadiumLocalSettings settings) { _settings = settings; }
        private string Cs => _settings.CentralConnectionString ?? throw new InvalidOperationException("Central connection string missing");
        private SqlConnection Create() => new SqlConnection(Cs);

        public async Task<IReadOnlyList<SnomedConcept>> SearchCachedConceptsAsync(string query, int limit = 50)
        {
            var list = new List<SnomedConcept>();
            if (string.IsNullOrWhiteSpace(query)) return list;
            string sql = @"SELECT TOP (@lim) concept_id, concept_id_str, fsn, pt, active, cached_at
                           FROM snomed.concept_cache
                           WHERE fsn LIKE @like OR pt LIKE @like OR concept_id_str LIKE @like
                           ORDER BY LEN(fsn) ASC, cached_at DESC";
            await using var con = Create();
            await con.OpenAsync().ConfigureAwait(false);
            await using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@lim", limit);
            cmd.Parameters.AddWithValue("@like", "%" + query + "%");
            await using var rd = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess).ConfigureAwait(false);
            while (await rd.ReadAsync().ConfigureAwait(false))
            {
                list.Add(new SnomedConcept(
                    rd.GetInt64(0),
                    rd.GetString(1),
                    rd.GetString(2),
                    rd.IsDBNull(3) ? null : rd.GetString(3),
                    rd.GetBoolean(4),
                    rd.GetDateTime(5)
                ));
            }
            return list;
        }

        public async Task<PhraseSnomedMapping?> GetMappingAsync(long phraseId)
        {
            const string sqlAccount = @"SELECT p.id, p.account_id, ps.concept_id, cc.concept_id_str, cc.fsn, cc.pt,
                                                ps.mapping_type, ps.confidence, ps.notes, 'account', ps.created_at, ps.updated_at
                                         FROM radium.phrase p
                                         JOIN radium.phrase_snomed ps ON ps.phrase_id = p.id
                                         JOIN snomed.concept_cache cc ON cc.concept_id = ps.concept_id
                                         WHERE p.id = @pid";

            const string sqlGlobal = @"SELECT p.id, p.account_id, gps.concept_id, cc.concept_id_str, cc.fsn, cc.pt,
                                                gps.mapping_type, gps.confidence, gps.notes, 'global', gps.created_at, gps.updated_at
                                         FROM radium.phrase p
                                         JOIN radium.global_phrase_snomed gps ON gps.phrase_id = p.id
                                         JOIN snomed.concept_cache cc ON cc.concept_id = gps.concept_id
                                         WHERE p.id = @pid";

            await using var con = Create();
            await con.OpenAsync().ConfigureAwait(false);
            // Prefer account mapping if exists
            await using (var cmd = new SqlCommand(sqlAccount, con) { CommandTimeout = 30 })
            {
                cmd.Parameters.AddWithValue("@pid", phraseId);
                await using var rd = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
                if (await rd.ReadAsync().ConfigureAwait(false))
                {
                    return new PhraseSnomedMapping(
                        rd.GetInt64(0), rd.IsDBNull(1) ? null : rd.GetInt64(1),
                        rd.GetInt64(2), rd.GetString(3), rd.GetString(4), rd.IsDBNull(5) ? null : rd.GetString(5),
                        rd.GetString(6), rd.IsDBNull(7) ? null : rd.GetDecimal(7), rd.IsDBNull(8) ? null : rd.GetString(8),
                        rd.GetString(9), rd.GetDateTime(10), rd.GetDateTime(11)
                    );
                }
            }
            // Fallback to global
            await using (var cmd2 = new SqlCommand(sqlGlobal, con) { CommandTimeout = 30 })
            {
                cmd2.Parameters.AddWithValue("@pid", phraseId);
                await using var rd = await cmd2.ExecuteReaderAsync().ConfigureAwait(false);
                if (await rd.ReadAsync().ConfigureAwait(false))
                {
                    return new PhraseSnomedMapping(
                        rd.GetInt64(0), rd.IsDBNull(1) ? null : rd.GetInt64(1),
                        rd.GetInt64(2), rd.GetString(3), rd.GetString(4), rd.IsDBNull(5) ? null : rd.GetString(5),
                        rd.GetString(6), rd.IsDBNull(7) ? null : rd.GetDecimal(7), rd.IsDBNull(8) ? null : rd.GetString(8),
                        rd.GetString(9), rd.GetDateTime(10), rd.GetDateTime(11)
                    );
                }
            }
            return null;
        }

        public async Task<IReadOnlyDictionary<long, PhraseSnomedMapping>> GetMappingsBatchAsync(IEnumerable<long> phraseIds)
        {
            var ids = phraseIds?.ToList() ?? new List<long>();
            if (ids.Count == 0)
                return new Dictionary<long, PhraseSnomedMapping>();

            var result = new Dictionary<long, PhraseSnomedMapping>();

            await using var con = Create();
            await con.OpenAsync().ConfigureAwait(false);

            // Use XML parameter approach for better performance with large ID lists
            var idsXml = string.Join("", ids.Select(id => $"<i>{id}</i>"));
            var xmlParam = $"<ids>{idsXml}</ids>";

            // Query global mappings (most common for MainViewModel use case)
            const string sqlGlobal = @"
                DECLARE @ids_table TABLE (phrase_id BIGINT);
                INSERT INTO @ids_table (phrase_id)
                SELECT T.c.value('.', 'BIGINT')
                FROM @ids_xml.nodes('/ids/i') AS T(c);

                SELECT p.id, p.account_id, gps.concept_id, cc.concept_id_str, cc.fsn, cc.pt,
                       gps.mapping_type, gps.confidence, gps.notes, 'global', gps.created_at, gps.updated_at
                FROM @ids_table t
                JOIN radium.phrase p ON p.id = t.phrase_id
                JOIN radium.global_phrase_snomed gps ON gps.phrase_id = p.id
                JOIN snomed.concept_cache cc ON cc.concept_id = gps.concept_id;";

            await using (var cmd = new SqlCommand(sqlGlobal, con) { CommandTimeout = 60 })
            {
                cmd.Parameters.Add("@ids_xml", SqlDbType.Xml).Value = xmlParam;
                await using var rd = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
                while (await rd.ReadAsync().ConfigureAwait(false))
                {
                    var phraseId = rd.GetInt64(0);
                    result[phraseId] = new PhraseSnomedMapping(
                        phraseId, rd.IsDBNull(1) ? null : rd.GetInt64(1),
                        rd.GetInt64(2), rd.GetString(3), rd.GetString(4), rd.IsDBNull(5) ? null : rd.GetString(5),
                        rd.GetString(6), rd.IsDBNull(7) ? null : rd.GetDecimal(7), rd.IsDBNull(8) ? null : rd.GetString(8),
                        rd.GetString(9), rd.GetDateTime(10), rd.GetDateTime(11)
                    );
                }
            }

            // Also check account-specific mappings (override global if exists)
            const string sqlAccount = @"
                DECLARE @ids_table TABLE (phrase_id BIGINT);
                INSERT INTO @ids_table (phrase_id)
                SELECT T.c.value('.', 'BIGINT')
                FROM @ids_xml.nodes('/ids/i') AS T(c);

                SELECT p.id, p.account_id, ps.concept_id, cc.concept_id_str, cc.fsn, cc.pt,
                       ps.mapping_type, ps.confidence, ps.notes, 'account', ps.created_at, ps.updated_at
                FROM @ids_table t
                JOIN radium.phrase p ON p.id = t.phrase_id
                JOIN radium.phrase_snomed ps ON ps.phrase_id = p.id
                JOIN snomed.concept_cache cc ON cc.concept_id = ps.concept_id;";

            await using (var cmd2 = new SqlCommand(sqlAccount, con) { CommandTimeout = 60 })
            {
                cmd2.Parameters.Add("@ids_xml", SqlDbType.Xml).Value = xmlParam;
                await using var rd = await cmd2.ExecuteReaderAsync().ConfigureAwait(false);
                while (await rd.ReadAsync().ConfigureAwait(false))
                {
                    var phraseId = rd.GetInt64(0);
                    // Account-specific mapping overrides global
                    result[phraseId] = new PhraseSnomedMapping(
                        phraseId, rd.IsDBNull(1) ? null : rd.GetInt64(1),
                        rd.GetInt64(2), rd.GetString(3), rd.GetString(4), rd.IsDBNull(5) ? null : rd.GetString(5),
                        rd.GetString(6), rd.IsDBNull(7) ? null : rd.GetDecimal(7), rd.IsDBNull(8) ? null : rd.GetString(8),
                        rd.GetString(9), rd.GetDateTime(10), rd.GetDateTime(11)
                    );
                }
            }

            return result;
        }

        public async Task CacheConceptAsync(SnomedConcept concept)
        {
            if (concept == null) throw new ArgumentNullException(nameof(concept));

            const string sql = "EXEC snomed.upsert_concept @concept_id, @concept_id_str, @fsn, @pt, @module_id, @active, @expires_at";

            await using var con = Create();
            await con.OpenAsync().ConfigureAwait(false);
            await using var cmd = new SqlCommand(sql, con) { CommandType = CommandType.StoredProcedure };
            cmd.CommandText = "snomed.upsert_concept";
            cmd.Parameters.AddWithValue("@concept_id", concept.ConceptId);
            cmd.Parameters.AddWithValue("@concept_id_str", concept.ConceptIdStr);
            cmd.Parameters.AddWithValue("@fsn", concept.Fsn);
            cmd.Parameters.AddWithValue("@pt", (object?)concept.Pt ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@module_id", DBNull.Value); // Not provided by Snowstorm client
            cmd.Parameters.AddWithValue("@active", concept.Active);
            cmd.Parameters.AddWithValue("@expires_at", DBNull.Value); // Let DB default handle
            await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
        }

        public async Task<bool> MapPhraseAsync(long phraseId, long? accountId, long conceptId, string mappingType = "exact", decimal? confidence = null, string? notes = null, long? mappedBy = null)
        {
            await using var con = Create();
            await con.OpenAsync().ConfigureAwait(false);

            // verify concept exists
            await using (var chk = new SqlCommand("SELECT 1 FROM snomed.concept_cache WHERE concept_id=@id", con))
            {
                chk.Parameters.AddWithValue("@id", conceptId);
                var ok = await chk.ExecuteScalarAsync().ConfigureAwait(false) != null;
                if (!ok) throw new InvalidOperationException("SNOMED concept not in cache. Cache concept first.");
            }

            if (accountId == null)
            {
                // Ensure phrase is global
                await using (var v = new SqlCommand("SELECT 1 FROM radium.phrase WHERE id=@pid AND account_id IS NULL", con))
                {
                    v.Parameters.AddWithValue("@pid", phraseId);
                    if (await v.ExecuteScalarAsync().ConfigureAwait(false) == null)
                        throw new InvalidOperationException("Phrase must be global (account_id IS NULL)");
                }
                await using var cmd = new SqlCommand("radium.map_global_phrase_to_snomed", con) { CommandType = CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("@phrase_id", phraseId);
                cmd.Parameters.AddWithValue("@concept_id", conceptId);
                cmd.Parameters.AddWithValue("@mapping_type", mappingType);
                cmd.Parameters.AddWithValue("@confidence", (object?)confidence ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@notes", (object?)notes ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@mapped_by", (object?)mappedBy ?? DBNull.Value);
                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                return true;
            }
            else
            {
                // Ensure phrase is account-specific
                await using (var v = new SqlCommand("SELECT 1 FROM radium.phrase WHERE id=@pid AND account_id=@aid", con))
                {
                    v.Parameters.AddWithValue("@pid", phraseId);
                    v.Parameters.AddWithValue("@aid", accountId.Value);
                    if (await v.ExecuteScalarAsync().ConfigureAwait(false) == null)
                        throw new InvalidOperationException("Phrase must be account-specific for accountId");
                }
                await using var cmd = new SqlCommand("radium.map_phrase_to_snomed", con) { CommandType = CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("@phrase_id", phraseId);
                cmd.Parameters.AddWithValue("@concept_id", conceptId);
                cmd.Parameters.AddWithValue("@mapping_type", mappingType);
                cmd.Parameters.AddWithValue("@confidence", (object?)confidence ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@notes", (object?)notes ?? DBNull.Value);
                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                return true;
            }
        }
    }
}
