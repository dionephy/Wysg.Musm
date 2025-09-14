using System.Collections.Generic;
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
    }

    public sealed class TenantLookup : ITenantLookup
    {
        private readonly IDb _db;
        public TenantLookup(IDb db) { _db = db; }
        public Task<IReadOnlyList<TenantDto>> GetAllAsync() => _db.QueryAsync(
            @"SELECT id, code, name FROM app.tenant ORDER BY name",
            map: rd => new TenantDto(rd.GetInt64(0), rd.GetString(1), rd.GetString(2))
        ).ContinueWith(t => (IReadOnlyList<TenantDto>)t.Result);
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

        public Task<IReadOnlyList<SctConceptDto>> SearchSctAsync(string query, int limit = 50) => _db.QueryAsync(
            @"SELECT c.id, COALESCE(td.term, '') AS term, COALESCE(c.moduleid, '') AS module, COALESCE(c.effectivetime, '') AS effective
               FROM snomedct.concept_latest c
               LEFT JOIN LATERAL (
                   SELECT t.term
                   FROM snomedct.textdefinition_f t
                   WHERE t.conceptid = c.id
                   ORDER BY t.effectivetime DESC
                   LIMIT 1
               ) td ON true
               WHERE c.id LIKE @q || '%' OR td.term ILIKE '%' || @q || '%'
               ORDER BY c.id
               LIMIT @limit",
            new { q = query, limit },
            rd => new SctConceptDto(rd.GetString(0), rd.GetString(1), rd.GetString(2), rd.GetString(3))
        ).ContinueWith(t => (IReadOnlyList<SctConceptDto>)t.Result);

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
    }
}
