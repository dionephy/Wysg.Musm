# ? Azure SQL Central Database - Final Deployment Guide
**Date:** 2025-10-19  
**Status:** READY FOR PRODUCTION

---

## ?? **You Now Have TWO Idempotent Files**

### **File 1: Base Schema** ? SAFE
```
apps/Wysg.Musm.Radium/docs/db/db_central_azure_base_20251015.sql
```
- Creates: account, phrase, hotkey, snippet, reportify_setting
- ~500 lines
- **Fully idempotent** - can run on existing databases

### **File 2: SNOMED Migration** ? SAFE
```
apps/Wysg.Musm.Radium/docs/db/db_central_azure_migration_20251019.sql
```
- Adds: SNOMED tables, tags columns, procedures, views
- ~800 lines
- **Fully idempotent** - can run on existing databases

---

## ?? **How to Deploy**

### **Scenario A: Fresh Database (Empty)**
Run both files in order:

```powershell
# Step 1: Create base schema
sqlcmd -S your-server.database.windows.net `
       -d central_db `
       -U your_user `
       -i "apps\Wysg.Musm.Radium\docs\db\db_central_azure_base_20251015.sql"

# Step 2: Add SNOMED support
sqlcmd -S your-server.database.windows.net `
       -d central_db `
       -U your_user `
       -i "apps\Wysg.Musm.Radium\docs\db\db_central_azure_migration_20251019.sql"
```

### **Scenario B: Existing Database (Your Current Situation)**

**Option 1: Run migration only (recommended)**
```powershell
# Your database already has base tables, just add SNOMED
sqlcmd -S your-server.database.windows.net `
       -d central_db `
       -U your_user `
       -i "apps\Wysg.Musm.Radium\docs\db\db_central_azure_migration_20251019.sql"
```

**Option 2: Run both (also safe)**
```powershell
# Both files are idempotent, so running both is also fine
sqlcmd ... -i "db_central_azure_base_20251015.sql"
sqlcmd ... -i "db_central_azure_migration_20251019.sql"
```

---

## ? **Expected Results**

### **After Running Base File:**
```
SECTION 1: Creating schemas...
  ? Created schema: app
  ? Created schema: radium
SECTION 2: Creating app.account table...
  ? Created table: app.account
...
  ? Tables created/verified: 5/5
  ? Indexes created/verified: 11
  ? Triggers created/verified: 3/3
```

### **After Running Migration File:**
```
SECTION 1: Creating schemas...
  ? Created schema: snomed
SECTION 2: Adding tags columns to radium.phrase...
  ? Added column: tags
  ? Added computed column: tags_source
  ? Added computed column: tags_semantic_tag
...
  ? Tables created/verified: 3/3
  ? Stored procedures created: 4/4
  ? Views created: 2/2
```

---

## ?? **Verification Query**

After deployment, run this to verify everything:

```sql
-- Should return count = expected for all rows
SELECT 
    'Tables' AS component, COUNT(*) AS count, 8 AS expected
FROM sys.tables 
WHERE schema_id IN (SCHEMA_ID('radium'), SCHEMA_ID('snomed'), SCHEMA_ID('app'))
  AND name IN ('account', 'phrase', 'hotkey', 'snippet', 'reportify_setting',
               'concept_cache', 'global_phrase_snomed', 'phrase_snomed')
UNION ALL
SELECT 'Procedures', COUNT(*), 4
FROM sys.procedures 
WHERE name IN ('upsert_concept', 'map_global_phrase_to_snomed', 
               'map_phrase_to_snomed', 'import_snomed_phrases')
UNION ALL
SELECT 'Views', COUNT(*), 2
FROM sys.views 
WHERE name IN ('v_phrase_snomed_combined', 'v_snomed_phrase_stats')
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
```

**Expected Output:**
```
component               count  expected
----------------------  -----  --------
Tables                  8      8
Procedures              4      4
Views                   2      2
Triggers                5      5
Phrase Tags Columns     3      3
```

---

## ?? **File Organization**

Your `apps/Wysg.Musm.Radium/docs/db/` folder:

```
? db_central_azure_base_20251015.sql        (Base schema - idempotent)
? db_central_azure_migration_20251019.sql   (SNOMED migration - idempotent)
? README_SCHEMA_REFERENCE.md                (This guide)
? DEPLOYMENT_GUIDE.md                       (Quick reference)
```

---

## ?? **Important Notes**

1. **Both files are idempotent** - Safe to run multiple times
2. **No data loss** - Only adds new components, never drops
3. **Order matters for fresh DB** - Run base first, then migration
4. **Order doesn't matter for existing DB** - Both files check existence

---

## ?? **You're Ready!**

Your schema files are now:
- ? **Idempotent** - Can re-run safely
- ? **Complete** - All base + SNOMED features
- ? **Production-ready** - Tested structure
- ? **Well-documented** - Clear comments

Just run the migration file on your existing database and you're done! ??

---

**Last Updated:** 2025-10-19  
**Author:** Radium Development Team
