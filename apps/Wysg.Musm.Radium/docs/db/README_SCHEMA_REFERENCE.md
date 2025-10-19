# Azure SQL Central Database - Schema Reference Guide
**Date:** 2025-10-19  
**Version:** 2.0  
**Database:** central_db (Azure SQL)

---

## ?? **Quick Answer: Which Files to Reference**

### **For Copilot Threads, Reference BOTH Files:**

```
1. apps/Wysg.Musm.Radium/docs/db/db_central_azure_base_20251015.sql
   ? Base schema (account, phrase, hotkey, snippet, reportify_setting)
   ? ~500 lines (IDEMPOTENT)

2. apps/Wysg.Musm.Radium/docs/db/db_central_azure_migration_20251019.sql  
   ? SNOMED additions (tags, mapping tables, procedures)
   ? ~800 lines (IDEMPOTENT)

TOGETHER = Complete central database schema
BOTH FILES ARE SAFE TO RUN ON EXISTING DATABASES
```

---

## ?? **Schema Components**

### **File 1: Base Schema** (`db_central_azure_base_20251015.sql`) ? IDEMPOTENT

| Component | Count | Notes |
|-----------|-------|-------|
| Schemas | 2 | `app`, `radium` |
| Tables | 5 | account, phrase, hotkey, snippet, reportify_setting |
| Indexes | 12+ | Unique constraints, filtered indexes |
| Triggers | 3 | Auto-update `updated_at` and `rev` |
| Constraints | 15+ | FK, check, unique |

**Key Tables:**
- `app.account` - User accounts
- `radium.phrase` - Phrases (global & account-specific)
- `radium.hotkey` - Text expansion hotkeys
- `radium.snippet` - Advanced snippets with AST
- `radium.reportify_setting` - Report generation settings

---

### **File 2: SNOMED Additions** (`db_central_azure_migration_20251019.sql`) ? IDEMPOTENT

| Component | Count | Notes |
|-----------|-------|-------|
| Schemas | 1 | `snomed` |
| Tables | 3 | concept_cache, global_phrase_snomed, phrase_snomed |
| Columns Added | 3 | tags, tags_source, tags_semantic_tag (to phrase) |
| Indexes | 14+ | SNOMED-specific indexes |
| Triggers | 2 | SNOMED mapping triggers |
| Views | 2 | Combined mappings, statistics |
| Procedures | 4 | Upsert, mapping, bulk import |

**Key Tables:**
- `snomed.concept_cache` - Cached SNOMED concepts from Snowstorm
- `radium.global_phrase_snomed` - Global phrase ¡æ SNOMED mappings
- `radium.phrase_snomed` - Account phrase ¡æ SNOMED mappings

**Key Features:**
- Lowercase normalization for phrase import
- JSON tags with computed columns for performance
- Filtered indexes on computed columns
- Bulk import procedure with auto-mapping

---

## ?? **Complete Schema Summary**

### **Total Components** (Base + SNOMED)

```
Schemas:        3 (app, radium, snomed)
Tables:         8
Indexes:        26+
Triggers:       5
Views:          2
Procedures:     4
Columns:        50+
```

### **Table List** (All 8 Tables)

1. ? `app.account` - User accounts
2. ? `radium.phrase` - Phrases with SNOMED tags
3. ? `radium.hotkey` - Text expansion hotkeys
4. ? `radium.snippet` - Advanced snippets
5. ? `radium.reportify_setting` - Report settings
6. ? `snomed.concept_cache` - SNOMED concepts
7. ? `radium.global_phrase_snomed` - Global mappings
8. ? `radium.phrase_snomed` - Account mappings

---

## ?? **Usage Guide**

### **When Discussing Complete Schema:**
```
Reference both files:
- apps/Wysg.Musm.Radium/docs/db/db_central_azure_base_20251015.sql (base)
- apps/Wysg.Musm.Radium/docs/db/db_central_azure_migration_20251019.sql (SNOMED)
```

### **When Discussing Only SNOMED:**
```
Reference only:
- apps/Wysg.Musm.Radium/docs/db/db_central_azure_migration_20251019.sql
```

### **When Discussing Base Tables Only:**
```
Reference only:
- apps/Wysg.Musm.Radium/docs/db/db_central_azure_base_20251015.sql
```

---

## ?? **Deployment Order**

### **Fresh Database:**
1. Run `db_central_azure_base_20251015.sql` (creates base schema)
2. Run `db_central_azure_migration_20251019.sql` (adds SNOMED support)

### **Existing Database:**
Run either or both files - **both are fully idempotent**:
- `db_central_azure_base_20251015.sql` (safe to re-run)
- `db_central_azure_migration_20251019.sql` (safe to re-run)

---

## ? **Verification Query**

After deployment, run this to verify everything exists:

```sql
-- Verify all components exist
SELECT 
    'Tables' AS component_type,
    COUNT(*) AS count,
    8 AS expected
FROM sys.tables 
WHERE schema_id IN (SCHEMA_ID('radium'), SCHEMA_ID('snomed'), SCHEMA_ID('app'))
  AND name IN ('account', 'phrase', 'hotkey', 'snippet', 'reportify_setting',
               'concept_cache', 'global_phrase_snomed', 'phrase_snomed')
UNION ALL
SELECT 'Stored Procedures', COUNT(*), 4
FROM sys.procedures 
WHERE schema_id IN (SCHEMA_ID('radium'), SCHEMA_ID('snomed'))
  AND name IN ('upsert_concept', 'map_global_phrase_to_snomed', 
               'map_phrase_to_snomed', 'import_snomed_phrases')
UNION ALL
SELECT 'Views', COUNT(*), 2
FROM sys.views 
WHERE schema_id = SCHEMA_ID('radium')
  AND name IN ('v_phrase_snomed_combined', 'v_snomed_phrase_stats')
UNION ALL
SELECT 'Triggers', COUNT(*), 5
FROM sys.triggers
WHERE name IN ('trg_phrase_touch', 'trg_hotkey_touch', 'trg_snippet_touch',
               'trg_global_phrase_snomed_touch', 'trg_phrase_snomed_touch')
UNION ALL
SELECT 'Phrase Tags Columns', COUNT(*), 3
FROM sys.columns
WHERE object_id = OBJECT_ID('radium.phrase')
  AND name IN ('tags', 'tags_source', 'tags_semantic_tag');

-- Expected: All counts should match expected values
```

---

## ?? **Version History**

| Date | Version | Description |
|------|---------|-------------|
| 2025-10-15 | 1.0 | Base schema (account, phrase, hotkey, snippet) |
| 2025-10-19 | 2.0 | Added SNOMED mapping support |

---

## ?? **Troubleshooting**

### **If schema deployment fails:**

1. **Check dependencies:**
   ```sql
   SELECT * FROM sys.foreign_keys;
   ```

2. **Verify schemas exist:**
   ```sql
   SELECT * FROM sys.schemas WHERE name IN ('app', 'radium', 'snomed');
   ```

3. **Check for conflicts:**
   ```sql
   SELECT * FROM sys.tables WHERE name LIKE '%phrase%';
   ```

### **If SNOMED features don't work:**

1. **Verify tags columns exist:**
   ```sql
   SELECT * FROM sys.columns 
   WHERE object_id = OBJECT_ID('radium.phrase') 
   AND name LIKE 'tags%';
   ```

2. **Check procedures exist:**
   ```sql
   SELECT * FROM sys.procedures 
   WHERE schema_id IN (SCHEMA_ID('snomed'), SCHEMA_ID('radium'));
   ```

---

## ?? **Related Documentation**

- **SNOMED Integration Guide:** `apps/Wysg.Musm.Radium/docs/snomed-semantic-tag-debugging.md`
- **Phrase Highlighting:** `apps/Wysg.Musm.Radium/docs/phrase-highlighting-usage.md`
- **C# Service Layer:** `apps/Wysg.Musm.Radium/Services/ISnomedMapService.cs`

---

**Last Updated:** 2025-10-19  
**Maintained By:** Radium Development Team
