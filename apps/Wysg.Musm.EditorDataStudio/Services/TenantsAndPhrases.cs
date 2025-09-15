using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Npgsql;

namespace Wysg.Musm.EditorDataStudio.Services
{
    public interface ITenantLookup
    {
        Task<IReadOnlyList<TenantDto>> GetAllAsync();
    }

    public interface IPhraseWriter
    {
        Task<IReadOnlyList<PhraseDto>> ListAsync(long tenantId);
        Task<long> EnsurePhraseAsync(string tenantName, string text, bool caseSensitive, string lang, string tagsJson);
        Task<IReadOnlyList<SctConceptDto>> SearchSctAsync(string query, int limit = 50);
        Task<long> EnsurePhraseWithSctAsync(long tenantId, string text, bool caseSensitive, string lang, string rootConceptId, string? expressionCg = null, string? edition = null, string? moduleId = null, System.DateTime? effectiveTime = null, string? tagsJson = null);
        Task MapPhraseToSctAsync(long phraseId, string rootConceptId, string? expressionCg = null, string? edition = null, string? moduleId = null, System.DateTime? effectiveTime = null);
        Task<PhraseSctMappingDto?> GetPhraseSctAsync(long phraseId);
    }

    public sealed class TenantLookup : ITenantLookup
    {
        private readonly IDb _db;
        public TenantLookup(IDb db) { _db = db; }

        public async Task<IReadOnlyList<TenantDto>> GetAllAsync()
        {
            var result = await _db.QueryAsync(
                @"SELECT id, code, name FROM app.tenant ORDER BY name",
                map: rd => new TenantDto(rd.GetInt64(0), rd.GetString(1), rd.GetString(2))
            );
            return result;
        }
    }

    public sealed class PhraseWriter : IPhraseWriter
    {
        private readonly IDb _db;
        public PhraseWriter(IDb db) { _db = db; }

        public Task<IReadOnlyList<PhraseDto>> ListAsync(long tenantId) => _db.QueryAsync(
            @"SELECT id, text, case_sensitive, lang, active, updated_at FROM content.phrase WHERE tenant_id=@tenantId ORDER BY updated_at DESC LIMIT 500",
            new { tenantId },
            rd => new PhraseDto(rd.GetInt64(0), rd.GetString(1), rd.GetBoolean(2), rd.GetString(3), rd.GetBoolean(4), rd.GetDateTime(5))
        ).ContinueWith(t => (IReadOnlyList<PhraseDto>)t.Result);

        public async Task<long> EnsurePhraseAsync(string tenantName, string text, bool caseSensitive, string lang, string tagsJson)
        {
            // Cast tags to jsonb in SQL so we can pass a plain string payload
            const string sql = @"SELECT content.ensure_phrase(@tenantName, @text, @caseSensitive, @lang, @tags::jsonb)";
            var result = await _db.QuerySingleAsync<long>(sql, new { tenantName, text, caseSensitive, lang, tags = tagsJson })
                                   .ConfigureAwait(false);
            return result;
        }

        public async Task<IReadOnlyList<SctConceptDto>> SearchSctAsync(string query, int limit = 50)
        {
            // Use server-side function that combines trigram and ILIKE, across all languages, then project label
            const string sql = @"
SELECT s.conceptid, COALESCE(s.label_ko_en, '') AS term, COALESCE(c.moduleid,'') AS module, COALESCE(c.effectivetime,'') AS effective
FROM snomedct.search_labels(@q, NULL, @limit) s
LEFT JOIN snomedct.concept_latest c ON c.id = s.conceptid
ORDER BY length(s.label_ko_en) ASC, s.score DESC, s.conceptid ASC";
            var rows = await _db.QueryAsync(sql, new { q = query, limit }, rd => new SctConceptDto(rd.GetString(0), rd.GetString(1), rd.GetString(2), rd.GetString(3)));
            return rows;
        }

        public async Task<long> EnsurePhraseWithSctAsync(long tenantId, string text, bool caseSensitive, string lang, string rootConceptId, string? expressionCg = null, string? edition = null, string? moduleId = null, System.DateTime? effectiveTime = null, string? tagsJson = null)
        {
            const string sql = @"SELECT content.ensure_phrase_with_sct(@tenantId, @text, @rootConceptId, @caseSensitive, @tags::jsonb, @lang, @expressionCg, @edition, @moduleId, @effectiveTime)";
            var result = await _db.QuerySingleAsync<long>(sql, new { tenantId, text, rootConceptId, caseSensitive, tags = tagsJson ?? "{}", lang, expressionCg, edition, moduleId, effectiveTime })
                                   .ConfigureAwait(false);
            return result;
        }

        public async Task MapPhraseToSctAsync(long phraseId, string rootConceptId, string? expressionCg = null, string? edition = null, string? moduleId = null, System.DateTime? effectiveTime = null)
        {
            const string sql = @"INSERT INTO content.phrase_sct(phrase_id, root_concept_id, expression_cg, edition, module_id, effective_time)
                                 VALUES(@phraseId, @rootConceptId, @expressionCg, @edition, @moduleId, @effectiveTime)
                                 ON CONFLICT (phrase_id) DO UPDATE
                                    SET root_concept_id = EXCLUDED.root_concept_id,
                                        expression_cg   = EXCLUDED.expression_cg,
                                        edition         = EXCLUDED.edition,
                                        module_id       = EXCLUDED.module_id,
                                        effective_time  = EXCLUDED.effective_time";
            _ = await _db.ExecuteAsync(sql, new { phraseId, rootConceptId, expressionCg, edition, moduleId, effectiveTime }).ConfigureAwait(false);
        }

        public async Task<PhraseSctMappingDto?> GetPhraseSctAsync(long phraseId)
        {
            const string sqlHead = @"
SELECT ps.root_concept_id,
       COALESCE(snomedct.label_ko_en(ps.root_concept_id), '') AS root_term,
       ps.expression_cg,
       ps.edition,
       ps.module_id,
       to_char(ps.effective_time, 'YYYY-MM-DD') AS effective_time
FROM content.phrase_sct ps
WHERE ps.phrase_id = @phraseId";

            var head = await _db.QuerySingleAsync(sqlHead, new { phraseId }, rd => new PhraseSctMappingDto(
                rd.GetString(0), rd.GetString(1),
                rd.IsDBNull(2) ? null : rd.GetString(2),
                rd.IsDBNull(3) ? null : rd.GetString(3),
                rd.IsDBNull(4) ? null : rd.GetString(4),
                rd.IsDBNull(5) ? null : rd.GetString(5),
                new List<PhraseSctRoleDto>()
            ));

            if (head == null) return null;

            const string sqlRoles = @"
SELECT a.role_group,
       a.attribute_id,
       COALESCE(snomedct.label_ko_en(a.attribute_id), '') AS attribute_term,
       a.value_concept_id,
       COALESCE(snomedct.label_ko_en(a.value_concept_id), '') AS value_term
FROM content.phrase_sct_attribute a
WHERE a.phrase_id = @phraseId
ORDER BY a.role_group, a.attribute_id, a.value_concept_id";

            var roles = await _db.QueryAsync(sqlRoles, new { phraseId }, rd => new PhraseSctRoleDto(
                rd.GetInt32(0), rd.GetString(1), rd.GetString(2), rd.GetString(3), rd.GetString(4)
            ));

            head.Roles.AddRange(roles);
            return head;
        }
    }
}
