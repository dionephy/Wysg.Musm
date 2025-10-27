# Delete Global Phrases and SNOMED-CT Mappings

## Overview

This document provides SQL queries to remove global phrases (phrases where `account_id IS NULL`) and their associated SNOMED-CT mappings from the central database.

## Database Structure

### Tables Involved

1. **`radium.phrase`** - Stores all phrases (global and account-specific)
   - Global phrases: `account_id IS NULL`
   - Account phrases: `account_id IS NOT NULL`

2. **`radium.global_phrase_snomed`** - Maps global phrases to SNOMED concepts
   - Has FK constraint: `ON DELETE CASCADE` from `radium.phrase`

3. **`snomed.concept_cache`** - Cached SNOMED concepts
   - Referenced by mappings but not deleted

### Cascade Behavior

The foreign key constraint `FK_global_phrase_snomed_phrase` has `ON DELETE CASCADE`, which means:
- Deleting a phrase automatically deletes its SNOMED mappings
- You don't need to manually delete mappings first

## Safe Deletion Procedure

### Step 1: Create Backup

**CRITICAL: Always backup before deletion!**

```sql
-- Create backup tables with timestamp
DECLARE @timestamp NVARCHAR(20) = FORMAT(SYSUTCDATETIME(), 'yyyyMMddHHmmss');
DECLARE @backup_phrase NVARCHAR(100) = 'radium.phrase_backup_' + @timestamp;
DECLARE @backup_mapping NVARCHAR(100) = 'radium.global_phrase_snomed_backup_' + @timestamp;

-- Backup global phrases
EXEC('SELECT * INTO ' + @backup_phrase + ' FROM radium.phrase WHERE account_id IS NULL');

-- Backup global mappings
EXEC('SELECT * INTO ' + @backup_mapping + ' FROM radium.global_phrase_snomed');

PRINT 'Backup created:';
PRINT '  - ' + @backup_phrase;
PRINT '  - ' + @backup_mapping;
```

### Step 2: Preview What Will Be Deleted

```sql
-- Count statistics
SELECT 
    'Total Global Phrases' AS metric,
    COUNT(*) AS count
FROM radium.phrase
WHERE account_id IS NULL
UNION ALL
SELECT 
    'SNOMED-tagged Global Phrases',
    COUNT(*)
FROM radium.phrase
WHERE account_id IS NULL AND tags_source = 'snomed'
UNION ALL
SELECT 
    'Global SNOMED Mappings',
    COUNT(*)
FROM radium.global_phrase_snomed;

-- Sample preview (first 100 rows)
SELECT TOP 100
    p.id,
    p.text,
    p.tags_source,
    p.tags_semantic_tag,
    p.created_at,
    gps.concept_id,
    cc.concept_id_str,
    cc.fsn,
    cc.pt
FROM radium.phrase p
LEFT JOIN radium.global_phrase_snomed gps ON gps.phrase_id = p.id
LEFT JOIN snomed.concept_cache cc ON cc.concept_id = gps.concept_id
WHERE p.account_id IS NULL
ORDER BY p.created_at DESC;
```

### Step 3: Delete All Global Phrases

```sql
-- =====================================================
-- DELETE ALL GLOBAL PHRASES AND THEIR SNOMED-CT MAPPINGS
-- =====================================================
-- WARNING: This will permanently delete ALL global phrases!
-- Ensure you have created a backup (Step 1) before running.
-- =====================================================

BEGIN TRANSACTION;

-- Delete all global phrases
-- (Cascade will automatically delete global_phrase_snomed records)
DELETE FROM radium.phrase
WHERE account_id IS NULL;

-- Check results
DECLARE @deleted_count INT = @@ROWCOUNT;

PRINT 'Deletion Summary:';
PRINT '  - Global phrases deleted: ' + CAST(@deleted_count AS NVARCHAR(20));

-- Verify deletion
SELECT 
    'Remaining Global Phrases' AS verification,
    COUNT(*) AS count
FROM radium.phrase
WHERE account_id IS NULL
UNION ALL
SELECT 
    'Remaining Global Mappings',
    COUNT(*)
FROM radium.global_phrase_snomed;

-- If verification shows 0 for both, commit
-- Otherwise, rollback and investigate
COMMIT TRANSACTION;

-- To rollback instead of commit, use:
-- ROLLBACK TRANSACTION;
```

## Alternative Options

### Option A: Delete Only SNOMED-Imported Phrases

If you want to keep manually-created global phrases:

```sql
BEGIN TRANSACTION;

DELETE FROM radium.phrase
WHERE account_id IS NULL
  AND tags_source = 'snomed';

PRINT 'SNOMED-tagged global phrases deleted: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

COMMIT TRANSACTION;
```

### Option B: Delete by Semantic Tag

Delete only phrases with a specific semantic tag (e.g., "body structure"):

```sql
BEGIN TRANSACTION;

DELETE FROM radium.phrase
WHERE account_id IS NULL
  AND tags_semantic_tag = 'body structure';

PRINT 'Phrases with semantic tag deleted: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

COMMIT TRANSACTION;
```

### Option C: Delete by Import Batch

Delete phrases from a specific import batch:

```sql
BEGIN TRANSACTION;

DECLARE @batch_name NVARCHAR(100) = 'snomed-body structure-20250119';

DELETE FROM radium.phrase
WHERE account_id IS NULL
  AND JSON_VALUE(tags, '$.import_batch') = @batch_name;

PRINT 'Phrases from batch "' + @batch_name + '" deleted: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

COMMIT TRANSACTION;
```

### Option D: Deactivate Instead of Delete

Keep the data but mark as inactive:

```sql
-- Deactivate all global phrases
UPDATE radium.phrase
SET active = 0,
    updated_at = SYSUTCDATETIME(),
    rev = rev + 1
WHERE account_id IS NULL;

PRINT 'Global phrases deactivated: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

-- To reactivate later:
-- UPDATE radium.phrase SET active = 1 WHERE account_id IS NULL;
```

## Restore from Backup

If you need to restore deleted data:

```sql
-- Find your backup table (replace timestamp)
DECLARE @backup_table NVARCHAR(100) = 'radium.phrase_backup_20250124123456';

-- Restore phrases
EXEC('INSERT INTO radium.phrase (account_id, text, active, tags, created_at, updated_at, rev)
      SELECT account_id, text, active, tags, created_at, updated_at, rev 
      FROM ' + @backup_table);

PRINT 'Phrases restored: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

-- Note: Mappings will need to be recreated manually or from mapping backup
```

## Verification Queries

### Check Deletion Success

```sql
-- Should return 0 for both
SELECT 
    'Global Phrases' AS table_name,
    COUNT(*) AS remaining_count
FROM radium.phrase
WHERE account_id IS NULL
UNION ALL
SELECT 
    'Global SNOMED Mappings',
    COUNT(*)
FROM radium.global_phrase_snomed;
```

### Check Account Phrases Are Intact

```sql
-- Verify account-specific phrases were NOT deleted
SELECT 
    'Account Phrases' AS table_name,
    COUNT(*) AS count
FROM radium.phrase
WHERE account_id IS NOT NULL;

-- This should match the count before deletion
```

### Check SNOMED Concept Cache

```sql
-- Concept cache should remain intact
SELECT 
    'SNOMED Concepts' AS table_name,
    COUNT(*) AS count
FROM snomed.concept_cache;

-- Concepts are not deleted, only phrase-to-concept mappings
```

## Important Notes

### Cascade Deletion

- **`global_phrase_snomed`** records are automatically deleted when their parent phrase is deleted
- **`snomed.concept_cache`** records are **NOT** deleted (FK uses `ON DELETE NO ACTION`)

### What Gets Deleted

? **Deleted:**
- All rows in `radium.phrase` where `account_id IS NULL`
- All rows in `radium.global_phrase_snomed` (via cascade)

? **NOT Deleted:**
- Account-specific phrases (`account_id IS NOT NULL`)
- Account-specific mappings (`radium.phrase_snomed`)
- SNOMED concepts (`snomed.concept_cache`)

### Performance Considerations

For large datasets (10,000+ phrases):

1. **Delete in batches:**
```sql
WHILE 1 = 1
BEGIN
    DELETE TOP (1000) FROM radium.phrase
    WHERE account_id IS NULL;
    
    IF @@ROWCOUNT = 0 BREAK;
    
    PRINT 'Deleted batch, remaining: ' + CAST((SELECT COUNT(*) FROM radium.phrase WHERE account_id IS NULL) AS NVARCHAR(20));
    
    WAITFOR DELAY '00:00:01'; -- 1 second pause between batches
END
```

2. **Monitor transaction log size**
3. **Consider running during off-peak hours**

## Troubleshooting

### Error: FK Constraint Violation

```
Msg 547: The DELETE statement conflicted with the REFERENCE constraint...
```

**Cause:** Another table references `radium.phrase` without CASCADE delete.

**Solution:** Check for additional FK constraints:
```sql
SELECT 
    OBJECT_NAME(parent_object_id) AS referencing_table,
    OBJECT_NAME(referenced_object_id) AS referenced_table,
    name AS constraint_name,
    delete_referential_action_desc
FROM sys.foreign_keys
WHERE referenced_object_id = OBJECT_ID('radium.phrase');
```

### Error: Transaction Log Full

```
Msg 9002: The transaction log for database 'central_db' is full...
```

**Solution:**
1. Delete in smaller batches (see Performance Considerations)
2. Backup transaction log
3. Increase transaction log size (if using SQL Server)

### Deletion Takes Too Long

**Solution:**
- Use batch deletion (see Performance Considerations)
- Disable triggers temporarily (advanced, be careful)
- Check for missing indexes on `account_id`

## Related Documentation

- [README_SCHEMA_REFERENCE.md](README_SCHEMA_REFERENCE.md) - Complete schema documentation
- [db_central_azure_base_20251015.sql](db_central_azure_base_20251015.sql) - Base schema
- [db_central_azure_migration_20251019.sql](db_central_azure_migration_20251019.sql) - SNOMED migration

## Summary

**Recommended deletion query:**

```sql
-- 1. Backup
SELECT * INTO radium.phrase_backup_[timestamp] FROM radium.phrase WHERE account_id IS NULL;
SELECT * INTO radium.global_phrase_snomed_backup_[timestamp] FROM radium.global_phrase_snomed;

-- 2. Preview
SELECT COUNT(*) AS phrases_to_delete FROM radium.phrase WHERE account_id IS NULL;

-- 3. Delete
BEGIN TRANSACTION;
DELETE FROM radium.phrase WHERE account_id IS NULL;
COMMIT TRANSACTION;

-- 4. Verify
SELECT COUNT(*) AS remaining FROM radium.phrase WHERE account_id IS NULL; -- Should be 0
```

---

**Last Updated:** 2025-01-24  
**Database Version:** Azure SQL (central_db)  
**Schema Version:** 2.0 (Base + SNOMED Migration)
