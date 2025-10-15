# Phrase-to-SNOMED Mapping - Deployment Guide

## Date: 2025-01-15

## Prerequisites

### 1. Snowstorm Server
- **Required**: SNOMED CT terminology server running
- **Options**:
  - Local: `docker run -p 8080:8080 snomedinternational/snowstorm:latest`
  - Remote: Cloud-hosted Snowstorm instance
- **Configuration**: Set base URL in application settings

### 2. SNOMED CT License
- **Required**: Valid SNOMED CT license from IHTSDO or affiliate
- **Verify**: Snowstorm configured with licensed edition (INT/US/KR)
- **Test**: Query Snowstorm API to confirm access

### 3. Database Access
- **Required**: Azure SQL Database connection with CREATE TABLE permissions
- **Schema**: `snomed` and `radium` schemas must exist
- **User**: Database user with DDL and DML permissions

## Deployment Steps

### Step 1: Deploy SQL Schema

#### Option A: SQL Server Management Studio (SSMS)
1. Connect to Azure SQL Database
2. Open file: `db\schema\central_db_phrase_snomed_mapping.sql`
3. Select database: `[your_central_db]`
4. Execute script (F5)
5. Verify: Check for error messages in Messages tab

#### Option B: Azure Data Studio
1. Connect to Azure SQL Database
2. Open file: `db\schema\central_db_phrase_snomed_mapping.sql`
3. Click "Run" button
4. Review output for errors

#### Option C: Command Line (sqlcmd)
```bash
sqlcmd -S your-server.database.windows.net -d your_central_db -U your_user -P your_password -i db\schema\central_db_phrase_snomed_mapping.sql
```

### Step 2: Verify Schema Deployment

Run these verification queries:

```sql
-- Check tables exist
SELECT TABLE_SCHEMA, TABLE_NAME 
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME IN ('concept_cache', 'global_phrase_snomed', 'phrase_snomed')
ORDER BY TABLE_SCHEMA, TABLE_NAME
```

Expected result:
| TABLE_SCHEMA | TABLE_NAME |
|---|---|
| radium | global_phrase_snomed |
| radium | phrase_snomed |
| snomed | concept_cache |

```sql
-- Check stored procedures exist
SELECT ROUTINE_SCHEMA, ROUTINE_NAME 
FROM INFORMATION_SCHEMA.ROUTINES 
WHERE ROUTINE_TYPE = 'PROCEDURE'
  AND ROUTINE_NAME IN ('upsert_concept', 'map_global_phrase_to_snomed', 'map_phrase_to_snomed')
ORDER BY ROUTINE_SCHEMA, ROUTINE_NAME
```

Expected result:
| ROUTINE_SCHEMA | ROUTINE_NAME |
|---|---|
| radium | map_global_phrase_to_snomed |
| radium | map_phrase_to_snomed |
| snomed | upsert_concept |

```sql
-- Check view exists
SELECT TABLE_SCHEMA, TABLE_NAME 
FROM INFORMATION_SCHEMA.VIEWS 
WHERE TABLE_NAME = 'v_phrase_snomed_combined'
```

Expected result:
| TABLE_SCHEMA | TABLE_NAME |
|---|---|
| radium | v_phrase_snomed_combined |

```sql
-- Check indexes exist
SELECT 
    s.name AS schema_name,
    t.name AS table_name,
    i.name AS index_name,
    i.type_desc AS index_type
FROM sys.indexes i
JOIN sys.tables t ON i.object_id = t.object_id
JOIN sys.schemas s ON t.schema_id = s.schema_id
WHERE t.name IN ('concept_cache', 'global_phrase_snomed', 'phrase_snomed')
  AND i.name IS NOT NULL
ORDER BY s.name, t.name, i.name
```

Expected result: 10+ indexes (see schema file for complete list)

```sql
-- Check foreign keys exist
SELECT 
    fk.name AS fk_name,
    OBJECT_SCHEMA_NAME(fk.parent_object_id) AS parent_schema,
    OBJECT_NAME(fk.parent_object_id) AS parent_table,
    OBJECT_SCHEMA_NAME(fk.referenced_object_id) AS referenced_schema,
    OBJECT_NAME(fk.referenced_object_id) AS referenced_table
FROM sys.foreign_keys fk
WHERE OBJECT_NAME(fk.parent_object_id) IN ('global_phrase_snomed', 'phrase_snomed')
ORDER BY parent_table, fk_name
```

Expected result: 4 foreign keys (2 per mapping table)

### Step 3: Test Stored Procedures

#### Test 1: Upsert Concept
```sql
-- Insert test concept
EXEC snomed.upsert_concept
    @concept_id = 22298006,
    @concept_id_str = '22298006',
    @fsn = 'Myocardial infarction (disorder)',
    @pt = 'Myocardial infarction',
    @module_id = '900000000000207008',
    @active = 1

-- Verify insert
SELECT * FROM snomed.concept_cache WHERE concept_id = 22298006
```

Expected: 1 row with FSN = "Myocardial infarction (disorder)"

```sql
-- Update same concept (idempotent upsert)
EXEC snomed.upsert_concept
    @concept_id = 22298006,
    @concept_id_str = '22298006',
    @fsn = 'Myocardial infarction (disorder)',
    @pt = 'Heart attack',  -- Changed PT
    @module_id = '900000000000207008',
    @active = 1

-- Verify update
SELECT * FROM snomed.concept_cache WHERE concept_id = 22298006
```

Expected: Still 1 row, PT updated to "Heart attack", cached_at updated

#### Test 2: Map Global Phrase (Success Case)
```sql
-- Prerequisites: Create test global phrase
INSERT INTO radium.phrase (account_id, text, active)
VALUES (NULL, 'test myocardial infarction', 1)

-- Get phrase_id
DECLARE @phrase_id BIGINT
SELECT @phrase_id = id FROM radium.phrase WHERE text = 'test myocardial infarction'

-- Map to concept
EXEC radium.map_global_phrase_to_snomed
    @phrase_id = @phrase_id,
    @concept_id = 22298006,
    @mapping_type = 'exact',
    @confidence = 1.0,
    @notes = 'Test mapping',
    @mapped_by = NULL

-- Verify mapping
SELECT * FROM radium.global_phrase_snomed WHERE phrase_id = @phrase_id
```

Expected: 1 row with concept_id = 22298006, mapping_type = 'exact'

#### Test 3: Map Global Phrase (Error Case - Account Phrase)
```sql
-- Prerequisites: Create test account phrase
INSERT INTO radium.phrase (account_id, text, active)
VALUES (1, 'test account phrase', 1)

-- Get phrase_id
DECLARE @phrase_id BIGINT
SELECT @phrase_id = id FROM radium.phrase WHERE text = 'test account phrase'

-- Attempt to map via global procedure (should fail)
EXEC radium.map_global_phrase_to_snomed
    @phrase_id = @phrase_id,
    @concept_id = 22298006,
    @mapping_type = 'exact',
    @confidence = 1.0,
    @notes = 'This should fail',
    @mapped_by = NULL
```

Expected: Error message "Phrase must be global (account_id IS NULL)"

#### Test 4: Map Account Phrase (Success Case)
```sql
-- Use phrase from Test 3
DECLARE @phrase_id BIGINT
SELECT @phrase_id = id FROM radium.phrase WHERE text = 'test account phrase'

-- Map via correct procedure
EXEC radium.map_phrase_to_snomed
    @phrase_id = @phrase_id,
    @concept_id = 22298006,
    @mapping_type = 'exact',
    @confidence = 0.95,
    @notes = 'Account-specific mapping'

-- Verify mapping
SELECT * FROM radium.phrase_snomed WHERE phrase_id = @phrase_id
```

Expected: 1 row with concept_id = 22298006, mapping_type = 'exact'

#### Test 5: View Query
```sql
-- Query combined view
SELECT 
    phrase_id,
    phrase_text,
    account_id,
    concept_id_str,
    fsn,
    pt,
    mapping_type,
    mapping_source
FROM radium.v_phrase_snomed_combined
ORDER BY mapping_source, phrase_text
```

Expected: 2 rows (1 global, 1 account) with full phrase and concept details

#### Test 6: Cleanup
```sql
-- Remove test data
DELETE FROM radium.phrase WHERE text IN ('test myocardial infarction', 'test account phrase')
DELETE FROM snomed.concept_cache WHERE concept_id = 22298006
```

Note: Mappings CASCADE deleted automatically

### Step 4: Configure Application Settings

Add Snowstorm connection to application configuration:

#### appsettings.json (or equivalent)
```json
{
  "Snowstorm": {
    "BaseUrl": "http://localhost:8080",
    "Branch": "MAIN",
    "Edition": "INT",
    "Timeout": 30,
    "EnableCache": true,
    "CacheExpirationDays": 7
  }
}
```

#### Environment Variables (Azure App Service)
```
SNOWSTORM_BASE_URL=https://snowstorm.your-domain.com
SNOWSTORM_BRANCH=MAIN
SNOWSTORM_EDITION=INT
SNOWSTORM_TIMEOUT=30
SNOWSTORM_ENABLE_CACHE=true
SNOWSTORM_CACHE_EXPIRATION_DAYS=7
```

### Step 5: Verify Application Integration

Once C# service layer is implemented:

#### Test Snowstorm API Connection
```csharp
// In Startup.cs or Program.cs
var snowstormService = services.GetService<ISnowstormService>();
var isConnected = await snowstormService.TestConnectionAsync();
if (!isConnected)
{
    logger.LogWarning("Snowstorm API not available - running in offline mode");
}
```

#### Test Concept Search
```csharp
var results = await snowstormService.SearchConceptsAsync("myocardial", limit: 10);
Console.WriteLine($"Found {results.Count} concepts");
foreach (var concept in results)
{
    Console.WriteLine($"{concept.ConceptId} - {concept.Fsn}");
}
```

Expected output:
```
Found 10 concepts
22298006 - Myocardial infarction (disorder)
401303003 - Acute ST segment elevation myocardial infarction (disorder)
...
```

## Rollback Procedure

If deployment fails or needs to be reverted:

```sql
-- Drop in reverse order to avoid FK constraint errors

-- Drop view
DROP VIEW IF EXISTS radium.v_phrase_snomed_combined

-- Drop stored procedures
DROP PROCEDURE IF EXISTS radium.map_phrase_to_snomed
DROP PROCEDURE IF EXISTS radium.map_global_phrase_to_snomed
DROP PROCEDURE IF EXISTS snomed.upsert_concept

-- Drop mapping tables (CASCADE will handle this)
DROP TABLE IF EXISTS radium.phrase_snomed
DROP TABLE IF EXISTS radium.global_phrase_snomed

-- Drop concept cache
DROP TABLE IF EXISTS snomed.concept_cache

-- Optional: Drop snomed schema if no other tables
-- DROP SCHEMA IF EXISTS snomed
```

## Troubleshooting

### Issue 1: Schema creation fails
**Symptoms**: "CREATE TABLE permission denied"

**Solution**: Verify user has db_ddladmin role or equivalent
```sql
ALTER ROLE db_ddladmin ADD MEMBER [your_user]
```

### Issue 2: Foreign key constraint errors
**Symptoms**: "FK_phrase_snomed_phrase could not be created"

**Solution**: Verify radium.phrase table exists and has id column
```sql
SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'phrase'
```

### Issue 3: Stored procedure execution fails
**Symptoms**: "Cannot find the object 'radium.phrase'"

**Solution**: Ensure schema qualifier is correct and table exists
```sql
-- Test table access
SELECT TOP 1 * FROM radium.phrase
```

### Issue 4: View query returns no rows
**Symptoms**: v_phrase_snomed_combined query returns empty result

**Solution**: 
1. Verify mappings exist in global or account tables
2. Check phrase.active = 1
3. Run individual table queries to isolate issue

```sql
-- Test global mappings
SELECT COUNT(*) FROM radium.global_phrase_snomed

-- Test account mappings
SELECT COUNT(*) FROM radium.phrase_snomed

-- Test concept cache
SELECT COUNT(*) FROM snomed.concept_cache
```

### Issue 5: Snowstorm API unreachable
**Symptoms**: SearchConceptsAsync throws HttpRequestException

**Solution**:
1. Verify Snowstorm is running: `curl http://localhost:8080/version`
2. Check firewall rules allow port 8080
3. Verify network connectivity from application server
4. Enable offline mode: `SNOWSTORM_ENABLE_CACHE=true`

### Issue 6: Performance degradation
**Symptoms**: View queries take > 5 seconds

**Solution**:
1. Check query execution plan: `SET SHOWPLAN_ALL ON`
2. Verify indexes exist: Run index verification query from Step 2
3. Update statistics: `UPDATE STATISTICS radium.phrase_snomed`
4. Consider pagination: Limit results to 100-500 rows per query
5. Monitor Azure SQL DTU usage: May need higher tier

## Monitoring and Maintenance

### Health Checks

Run these queries periodically:

#### Concept Cache Staleness
```sql
-- Concepts not refreshed in 30 days
SELECT COUNT(*) AS stale_count
FROM snomed.concept_cache
WHERE cached_at < DATEADD(day, -30, GETUTCDATE())
```

#### Unmapped Phrases
```sql
-- Active global phrases without mappings
SELECT COUNT(*) AS unmapped_count
FROM radium.phrase p
LEFT JOIN radium.global_phrase_snomed gps ON gps.phrase_id = p.id
WHERE p.account_id IS NULL
  AND p.active = 1
  AND gps.id IS NULL
```

#### Mapping Quality
```sql
-- Mappings with low confidence
SELECT COUNT(*) AS low_confidence_count
FROM (
    SELECT confidence FROM radium.global_phrase_snomed
    UNION ALL
    SELECT confidence FROM radium.phrase_snomed
) AS mappings
WHERE confidence < 0.80
```

### Periodic Tasks

#### Weekly: Refresh Concept Cache
```sql
-- Update cached_at for recently queried concepts
UPDATE snomed.concept_cache
SET cached_at = GETUTCDATE()
WHERE concept_id IN (
    SELECT DISTINCT concept_id 
    FROM radium.global_phrase_snomed
    UNION
    SELECT DISTINCT concept_id 
    FROM radium.phrase_snomed
)
```

#### Monthly: Audit Report
```sql
-- Generate mapping activity report
SELECT 
    DATEPART(year, created_at) AS year,
    DATEPART(month, created_at) AS month,
    COUNT(*) AS new_mappings,
    AVG(CAST(confidence AS FLOAT)) AS avg_confidence
FROM (
    SELECT created_at, confidence FROM radium.global_phrase_snomed
    UNION ALL
    SELECT created_at, confidence FROM radium.phrase_snomed
) AS all_mappings
GROUP BY DATEPART(year, created_at), DATEPART(month, created_at)
ORDER BY year DESC, month DESC
```

#### Quarterly: Concept Activation Status
```sql
-- Check for retired concepts
SELECT cc.concept_id_str, cc.fsn, cc.active, COUNT(*) AS usage_count
FROM snomed.concept_cache cc
LEFT JOIN (
    SELECT concept_id FROM radium.global_phrase_snomed
    UNION ALL
    SELECT concept_id FROM radium.phrase_snomed
) AS mappings ON mappings.concept_id = cc.concept_id
WHERE cc.active = 0
GROUP BY cc.concept_id_str, cc.fsn, cc.active
HAVING COUNT(*) > 0
```

## Production Checklist

Before deploying to production:

- [ ] SQL schema deployed and verified
- [ ] All stored procedures tested successfully
- [ ] View query performance acceptable (< 1 second)
- [ ] Snowstorm API connection configured and tested
- [ ] Application settings configured (base URL, timeout, etc.)
- [ ] Error handling implemented (API failures, offline mode)
- [ ] Logging configured (mapping creation, API calls, errors)
- [ ] Backup strategy in place (database regular backups)
- [ ] Monitoring dashboards set up (concept cache size, mapping count)
- [ ] User documentation prepared (how to map phrases, interpretation of mapping types)
- [ ] SNOMED CT license compliance verified
- [ ] Security review completed (data access, PHI protection)
- [ ] Performance testing completed (1000+ phrase queries)
- [ ] Rollback plan documented and tested

## Support Resources

### Documentation
- **Spec.md**: FR-900 through FR-915 (functional requirements)
- **Plan.md**: Implementation approach and risk mitigation
- **Tasks.md**: T900-T930 (implementation tasks)
- **Schema Diagram**: `db\schema\PHRASE_SNOMED_MAPPING_DIAGRAM.md`
- **Summary**: `db\schema\PHRASE_SNOMED_MAPPING_SUMMARY.md`

### External Resources
- **SNOMED International**: https://www.snomed.org/
- **Snowstorm Documentation**: https://github.com/IHTSDO/snowstorm
- **UMLS Mapping Guidelines**: https://www.nlm.nih.gov/research/umls/
- **HL7 FHIR ConceptMap**: http://hl7.org/fhir/conceptmap.html

### Contact
For schema issues or questions:
1. Check troubleshooting section above
2. Review verification queries
3. Consult team lead or DBA
4. Escalate to development team

## Deployment Completion

After successful deployment:

1. Mark tasks T900-T918 as complete in Tasks.md
2. Update build status in documentation
3. Notify team of new feature availability
4. Schedule training session for end users
5. Begin implementation of C# service layer (T919-T920)

## Version History

| Version | Date | Changes |
|---|---|---|
| 1.0 | 2025-01-15 | Initial schema deployment |

---

**Deployment Status**: ? Schema SQL file ready for deployment  
**Build Status**: ? Build succeeded with no errors  
**Next Phase**: C# service layer implementation (T919-T925)
