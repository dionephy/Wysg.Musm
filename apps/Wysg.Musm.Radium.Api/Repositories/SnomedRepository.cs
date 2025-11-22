using System.Data;
using System.Text;
using Microsoft.Data.SqlClient;
using Wysg.Musm.Radium.Api.Models.Dtos;

namespace Wysg.Musm.Radium.Api.Repositories;

/// <summary>
/// Azure SQL implementation of SNOMED repository.
/// Handles concept caching and phrase-SNOMED mappings.
/// </summary>
public sealed class SnomedRepository : ISnomedRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public SnomedRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    /// <inheritdoc />
    public async Task CacheConceptAsync(SnomedConceptDto concept)
    {
        if (concept == null)
            throw new ArgumentNullException(nameof(concept));

        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();
        
        await using var command = new SqlCommand("snomed.upsert_concept", connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 30
        };

        command.Parameters.AddWithValue("@concept_id", concept.ConceptId);
        command.Parameters.AddWithValue("@concept_id_str", concept.ConceptIdStr);
        command.Parameters.AddWithValue("@fsn", concept.Fsn);
        command.Parameters.AddWithValue("@pt", (object?)concept.Pt ?? DBNull.Value);
        command.Parameters.AddWithValue("@semantic_tag", (object?)concept.SemanticTag ?? DBNull.Value);
        command.Parameters.AddWithValue("@module_id", (object?)concept.ModuleId ?? DBNull.Value);
        command.Parameters.AddWithValue("@active", concept.Active);
        command.Parameters.AddWithValue("@expires_at", (object?)concept.ExpiresAt ?? DBNull.Value);

        await command.ExecuteNonQueryAsync();
    }

    /// <inheritdoc />
    public async Task<SnomedConceptDto?> GetConceptAsync(long conceptId)
    {
        const string sql = @"
            SELECT concept_id, concept_id_str, fsn, pt, semantic_tag, module_id, active, cached_at, expires_at
            FROM snomed.concept_cache
            WHERE concept_id = @concept_id";

        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();
        
        await using var command = new SqlCommand(sql, connection) { CommandTimeout = 30 };
        command.Parameters.AddWithValue("@concept_id", conceptId);

        await using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new SnomedConceptDto
            {
                ConceptId = reader.GetInt64(0),
                ConceptIdStr = reader.GetString(1),
                Fsn = reader.GetString(2),
                Pt = reader.IsDBNull(3) ? null : reader.GetString(3),
                SemanticTag = reader.IsDBNull(4) ? null : reader.GetString(4),
                ModuleId = reader.IsDBNull(5) ? null : reader.GetString(5),
                Active = reader.GetBoolean(6),
                CachedAt = reader.GetDateTime(7),
                ExpiresAt = reader.IsDBNull(8) ? null : reader.GetDateTime(8)
            };
        }

        return null;
    }

    /// <inheritdoc />
    public async Task CreateMappingAsync(
        long phraseId,
        long? accountId,
        long conceptId,
        string mappingType = "exact",
        decimal? confidence = null,
        string? notes = null,
        long? mappedBy = null)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        // Verify concept exists in cache
        await using (var checkCmd = new SqlCommand("SELECT 1 FROM snomed.concept_cache WHERE concept_id = @id", connection))
        {
            checkCmd.Parameters.AddWithValue("@id", conceptId);
            var exists = await checkCmd.ExecuteScalarAsync() != null;
            if (!exists)
                throw new InvalidOperationException($"SNOMED concept {conceptId} not found in cache. Cache the concept first.");
        }

        if (accountId == null)
        {
            // Global phrase mapping
            await using var cmd = new SqlCommand("radium.map_global_phrase_to_snomed", connection)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = 30
            };

            cmd.Parameters.AddWithValue("@phrase_id", phraseId);
            cmd.Parameters.AddWithValue("@concept_id", conceptId);
            cmd.Parameters.AddWithValue("@mapping_type", mappingType);
            cmd.Parameters.AddWithValue("@confidence", (object?)confidence ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@notes", (object?)notes ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@mapped_by", (object?)mappedBy ?? DBNull.Value);

            await cmd.ExecuteNonQueryAsync();
        }
        else
        {
            // Account-specific phrase mapping
            await using var cmd = new SqlCommand("radium.map_phrase_to_snomed", connection)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = 30
            };

            cmd.Parameters.AddWithValue("@phrase_id", phraseId);
            cmd.Parameters.AddWithValue("@concept_id", conceptId);
            cmd.Parameters.AddWithValue("@mapping_type", mappingType);
            cmd.Parameters.AddWithValue("@confidence", (object?)confidence ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@notes", (object?)notes ?? DBNull.Value);

            await cmd.ExecuteNonQueryAsync();
        }
    }

    /// <inheritdoc />
    public async Task<PhraseSnomedMappingDto?> GetMappingAsync(long phraseId)
    {
        // Try account-specific mapping first
        const string sqlAccount = @"
            SELECT p.id, p.account_id, ps.concept_id, cc.concept_id_str, cc.fsn, cc.pt,
                   ps.mapping_type, ps.confidence, ps.notes, 'account', ps.created_at, ps.updated_at
            FROM radium.phrase p
            JOIN radium.phrase_snomed ps ON ps.phrase_id = p.id
            JOIN snomed.concept_cache cc ON cc.concept_id = ps.concept_id
            WHERE p.id = @phrase_id";

        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        // Check account-specific mapping
        await using (var cmd = new SqlCommand(sqlAccount, connection) { CommandTimeout = 30 })
        {
            cmd.Parameters.AddWithValue("@phrase_id", phraseId);
            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return ReadMapping(reader);
            }
        }

        // Fallback to global mapping
        const string sqlGlobal = @"
            SELECT p.id, p.account_id, gps.concept_id, cc.concept_id_str, cc.fsn, cc.pt,
                   gps.mapping_type, gps.confidence, gps.notes, 'global', gps.created_at, gps.updated_at
            FROM radium.phrase p
            JOIN radium.global_phrase_snomed gps ON gps.phrase_id = p.id
            JOIN snomed.concept_cache cc ON cc.concept_id = gps.concept_id
            WHERE p.id = @phrase_id";

        await using (var cmd = new SqlCommand(sqlGlobal, connection) { CommandTimeout = 30 })
        {
            cmd.Parameters.AddWithValue("@phrase_id", phraseId);
            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return ReadMapping(reader);
            }
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<Dictionary<long, PhraseSnomedMappingDto>> GetMappingsBatchAsync(IEnumerable<long> phraseIds)
    {
        var ids = phraseIds?.ToList() ?? new List<long>();
        if (ids.Count == 0)
            return new Dictionary<long, PhraseSnomedMappingDto>();

        var result = new Dictionary<long, PhraseSnomedMappingDto>();

        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        // Use XML parameter for efficient batch query
        var idsXml = string.Join("", ids.Select(id => $"<i>{id}</i>"));
        var xmlParam = $"<ids>{idsXml}</ids>";

        // Query global mappings
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
            JOIN snomed.concept_cache cc ON cc.concept_id = gps.concept_id";

        await using (var cmd = new SqlCommand(sqlGlobal, connection) { CommandTimeout = 60 })
        {
            cmd.Parameters.Add("@ids_xml", SqlDbType.Xml).Value = xmlParam;
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var mapping = ReadMapping(reader);
                result[mapping.PhraseId] = mapping;
            }
        }

        // Query account-specific mappings (override global if exists)
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
            JOIN snomed.concept_cache cc ON cc.concept_id = ps.concept_id";

        await using (var cmd = new SqlCommand(sqlAccount, connection) { CommandTimeout = 60 })
        {
            cmd.Parameters.Add("@ids_xml", SqlDbType.Xml).Value = xmlParam;
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var mapping = ReadMapping(reader);
                result[mapping.PhraseId] = mapping; // Account mapping overrides global
            }
        }

        return result;
    }

    /// <inheritdoc />
    public async Task DeleteMappingAsync(long phraseId)
    {
        const string sql = @"
            DELETE FROM radium.phrase_snomed WHERE phrase_id = @phrase_id;
            DELETE FROM radium.global_phrase_snomed WHERE phrase_id = @phrase_id;";

        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();
        
        await using var command = new SqlCommand(sql, connection) { CommandTimeout = 30 };
        command.Parameters.AddWithValue("@phrase_id", phraseId);

        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Helper method to read a PhraseSnomedMappingDto from a SqlDataReader.
    /// </summary>
    private static PhraseSnomedMappingDto ReadMapping(SqlDataReader reader)
    {
        var fsn = reader.GetString(4);
        var semanticTag = ExtractSemanticTag(fsn);

        return new PhraseSnomedMappingDto
        {
            PhraseId = reader.GetInt64(0),
            AccountId = reader.IsDBNull(1) ? null : reader.GetInt64(1),
            ConceptId = reader.GetInt64(2),
            ConceptIdStr = reader.GetString(3),
            Fsn = fsn,
            Pt = reader.IsDBNull(5) ? null : reader.GetString(5),
            SemanticTag = semanticTag,
            MappingType = reader.GetString(6),
            Confidence = reader.IsDBNull(7) ? null : reader.GetDecimal(7),
            Notes = reader.IsDBNull(8) ? null : reader.GetString(8),
            Source = reader.GetString(9),
            CreatedAt = reader.GetDateTime(10),
            UpdatedAt = reader.GetDateTime(11)
        };
    }

    /// <summary>
    /// Extract semantic tag from FSN (text in parentheses at the end).
    /// Example: "Heart structure (body structure)" ¡æ "body structure"
    /// </summary>
    private static string? ExtractSemanticTag(string? fsn)
    {
        if (string.IsNullOrWhiteSpace(fsn))
            return null;

        var lastOpenParen = fsn.LastIndexOf('(');
        var lastCloseParen = fsn.LastIndexOf(')');

        if (lastOpenParen >= 0 && lastCloseParen > lastOpenParen)
        {
            return fsn.Substring(lastOpenParen + 1, lastCloseParen - lastOpenParen - 1).Trim();
        }

        return null;
    }
}
