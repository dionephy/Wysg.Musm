# Phrase-to-SNOMED Mapping Feature - Design Summary

## Date: 2025-01-15

## Overview
Database schema design for mapping global and account-specific phrases to SNOMED CT concepts via Snowstorm API. Supports terminology management in Settings ⊥ Global Phrases tab for semantic standardization and interoperability.

## Context
- **Snowstorm**: SNOMED CT terminology server (running locally or remotely)
- **Radium App**: Uses central Azure SQL database with `radium.phrase` table
- **Global Phrases**: Phrases with `account_id IS NULL` (available to all accounts)
- **Account Phrases**: Phrases with `account_id = specific account` (scoped per account)

## Tables Created

### 1. `snomed.concept_cache`
**Purpose**: Cache SNOMED CT concepts from Snowstorm API to reduce API calls and enable offline operation.

**Schema**:
```sql
CREATE TABLE snomed.concept_cache (
    concept_id BIGINT NOT NULL PRIMARY KEY,              -- SNOMED CT Concept ID (e.g., 22298006)
    concept_id_str VARCHAR(18) NOT NULL UNIQUE,          -- String representation for compatibility
    fsn NVARCHAR(500) NOT NULL,                          -- Fully Specified Name
    pt NVARCHAR(500) NULL,                               -- Preferred Term (may differ from FSN)
    module_id VARCHAR(18) NULL,                          -- SNOMED CT Module ID
    active BIT NOT NULL DEFAULT 1,                       -- Active status from Snowstorm
    cached_at DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),  -- Cache timestamp
    expires_at DATETIME2(3) NULL                         -- Optional cache expiration
)
```

**Indexes**:
- `IX_snomed_concept_cache_fsn` on `fsn`
- `IX_snomed_concept_cache_pt` on `pt`
- `IX_snomed_concept_cache_cached_at` on `cached_at DESC`

**Example Data**:
| concept_id | concept_id_str | fsn | pt | active |
|---|---|---|---|---|
| 22298006 | "22298006" | "Myocardial infarction (disorder)" | "Myocardial infarction" | 1 |

### 2. `radium.global_phrase_snomed`
**Purpose**: Map global phrases (account_id IS NULL) to SNOMED concepts. Global mappings are available to all accounts.

**Schema**:
```sql
CREATE TABLE radium.global_phrase_snomed (
    id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    phrase_id BIGINT NOT NULL UNIQUE,                    -- FK to radium.phrase WHERE account_id IS NULL
    concept_id BIGINT NOT NULL,                          -- FK to snomed.concept_cache
    mapping_type VARCHAR(20) NOT NULL DEFAULT 'exact',   -- 'exact', 'broader', 'narrower', 'related'
    confidence DECIMAL(3,2) NULL,                        -- Optional confidence score (0.00-1.00)
    notes NVARCHAR(500) NULL,                            -- Optional mapping notes
    mapped_by BIGINT NULL,                               -- Optional: account_id of user who created mapping
    created_at DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    updated_at DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    
    CONSTRAINT FK_global_phrase_snomed_phrase FOREIGN KEY (phrase_id) 
        REFERENCES radium.phrase(id) ON DELETE CASCADE,
    CONSTRAINT FK_global_phrase_snomed_concept FOREIGN KEY (concept_id) 
        REFERENCES snomed.concept_cache(concept_id) ON DELETE RESTRICT
)
```

**Indexes**:
- `IX_global_phrase_snomed_concept` on `concept_id`
- `IX_global_phrase_snomed_mapping_type` on `mapping_type`
- `IX_global_phrase_snomed_mapped_by` on `mapped_by`

**Constraints**:
- **One phrase ⊥ one concept**: UNIQUE constraint on `phrase_id`
- **Mapping type validation**: CHECK constraint allows only 'exact', 'broader', 'narrower', 'related'
- **Confidence range**: CHECK constraint enforces 0.00-1.00 range

**Triggers**:
- `trg_global_phrase_snomed_touch`: Auto-update `updated_at` when fields change

### 3. `radium.phrase_snomed`
**Purpose**: Map account-specific phrases (account_id IS NOT NULL) to SNOMED concepts. Account mappings override global mappings.

**Schema**: Similar to `global_phrase_snomed` but without `mapped_by` column.

```sql
CREATE TABLE radium.phrase_snomed (
    id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    phrase_id BIGINT NOT NULL,                           -- FK to radium.phrase WHERE account_id IS NOT NULL
    concept_id BIGINT NOT NULL,                          -- FK to snomed.concept_cache
    mapping_type VARCHAR(20) NOT NULL DEFAULT 'exact',
    confidence DECIMAL(3,2) NULL,
    notes NVARCHAR(500) NULL,
    created_at DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    updated_at DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    
    CONSTRAINT UQ_phrase_snomed_phrase UNIQUE (phrase_id),
    CONSTRAINT FK_phrase_snomed_phrase FOREIGN KEY (phrase_id) 
        REFERENCES radium.phrase(id) ON DELETE CASCADE,
    CONSTRAINT FK_phrase_snomed_concept FOREIGN KEY (concept_id) 
        REFERENCES snomed.concept_cache(concept_id) ON DELETE RESTRICT
)
```

**Indexes**: Same pattern as global table (concept_id, mapping_type, created_at)

### 4. `radium.v_phrase_snomed_combined` (View)
**Purpose**: Unified view of global and account-specific mappings for easy querying.

**Logic**:
- UNION ALL of `global_phrase_snomed` and `phrase_snomed`
- Joins with `radium.phrase` and `snomed.concept_cache`
- Adds `mapping_source` column ('global' or 'account')

**Use Cases**:
- Application queries for phrase-concept lookups
- Reporting and analytics
- UI display of mapped phrases

## Stored Procedures

### 1. `snomed.upsert_concept`
**Purpose**: Idempotent caching of SNOMED concepts from Snowstorm API.

**Parameters**:
```sql
@concept_id BIGINT,
@concept_id_str VARCHAR(18),
@fsn NVARCHAR(500),
@pt NVARCHAR(500) = NULL,
@module_id VARCHAR(18) = NULL,
@active BIT = 1,
@expires_at DATETIME2(3) = NULL
```

**Behavior**: MERGE on `concept_id` (insert if new, update if exists)

### 2. `radium.map_global_phrase_to_snomed`
**Purpose**: Map a global phrase to SNOMED concept with validation.

**Parameters**:
```sql
@phrase_id BIGINT,
@concept_id BIGINT,
@mapping_type VARCHAR(20) = 'exact',
@confidence DECIMAL(3,2) = NULL,
@notes NVARCHAR(500) = NULL,
@mapped_by BIGINT = NULL
```

**Validations**:
- Verify phrase is global (account_id IS NULL)
- Verify concept exists in cache
- RAISERROR if validations fail

**Behavior**: MERGE on `phrase_id` (insert if new, update if exists)

### 3. `radium.map_phrase_to_snomed`
**Purpose**: Map an account-specific phrase to SNOMED concept with validation.

**Parameters**: Same as global procedure (without `mapped_by`)

**Validations**:
- Verify phrase is account-specific (account_id IS NOT NULL)
- Verify concept exists in cache
- RAISERROR if validations fail

## Design Decisions

### 1. Separate Concept Cache Table
**Rationale**:
- Many phrases can map to same concept (many-to-one relationship)
- Avoids duplicate concept data across mappings
- Enables efficient concept-level queries (e.g., "find all phrases for concept X")
- Reduces storage and network overhead (cache once, reference many times)

**Trade-off**: Additional join in queries, but indexed for performance

### 2. Separate Global and Account Mapping Tables
**Rationale**:
- Global mappings are shared across all accounts (read-mostly workload)
- Account mappings are scoped and mutable per account (read-write workload)
- Simplifies access control and query filtering
- Mirrors existing `radium.phrase` account_id pattern for consistency
- Account mappings can override global mappings without modifying shared data

**Trade-off**: Two tables instead of one, but UNION view simplifies application queries

### 3. One Phrase ⊥ One Concept
**Rationale**:
- Simplifies UI and user understanding (no ambiguity)
- Matches majority use case (phrases have primary clinical meaning)
- Reduces complexity in phrase highlighting and completion

**Trade-off**: Cannot express multiple meanings (polysemy). Future: Support secondary mappings if needed.

### 4. Mapping Types (exact/broader/narrower/related)
**Rationale**:
- Aligns with UMLS and SNOMED mapping standards
- Provides semantic relationship information
- Supports mapping quality assessment and auditing

**Example**:
- "heart attack" ⊥ exact ⊥ 22298006 (Myocardial infarction)
- "chest pain" ⊥ broader ⊥ 29857009 (Chest pain)
- "STEMI" ⊥ narrower ⊥ 401303003 (Acute ST segment elevation myocardial infarction)

### 5. Confidence Score (Optional)
**Rationale**:
- Supports machine-assisted mapping (NLP, similarity algorithms)
- Allows flagging uncertain mappings for manual review
- Enables quality metrics (e.g., "95% of phrases have confidence >= 0.90")

**Default**: NULL (not specified) for manual mappings

### 6. CASCADE DELETE for Phrase, RESTRICT DELETE for Concept
**Rationale**:
- **Phrase deleted**: Mapping no longer relevant ⊥ CASCADE delete
- **Concept deleted**: Should not happen (concepts rarely retired), but if attempted ⊥ RESTRICT to prevent orphan mappings

### 7. Triggers for `updated_at`
**Rationale**:
- Automatic audit trail without application code
- Detects changes to concept_id, mapping_type, confidence, notes
- Avoids trigger recursion (only update if value actually changed)

## Integration with Snowstorm

### Snowstorm API Endpoints
1. **Search Concepts**: `GET /MAIN/concepts?term={query}&limit={limit}&activeFilter=true`
   - Returns: Array of concepts with `conceptId`, `fsn`, `pt`
   
2. **Get Concept Details**: `GET /MAIN/concepts/{conceptId}`
   - Returns: Full concept details including module, active status

### Workflow
1. User searches for "myocardial" in UI
2. Application queries Snowstorm API
3. Application caches results via `snomed.upsert_concept`
4. UI displays search results from cache
5. User selects concept and maps to phrase
6. Application calls `radium.map_global_phrase_to_snomed` or `radium.map_phrase_to_snomed`
7. Mapping saved to database

### Offline Mode
- If Snowstorm API unavailable, use cached concepts only
- Display warning: "Offline mode - showing cached concepts only"
- Periodic background sync when connection restored

## UI Integration (Settings ⊥ Global Phrases Tab)

### Proposed Layout
```
忙式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式忖
弛 Settings ⊥ Global Phrases                               弛
戍式式式式式式式式式式式式式式式式式式式式式式式式式成式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式扣
弛 Phrase List             弛 SNOMED Search                 弛
弛 忙式式式式式式式式式式式式式式式式式式式式式忖 弛 忙式式式式式式式式式式式式式式式式式式式式式式式式式式式忖 弛
弛 弛? myocardial         弛 弛 弛 Search: [myocardial    ]??弛 弛
弛 弛  infarction    [?]  弛 弛 戌式式式式式式式式式式式式式式式式式式式式式式式式式式式戎 弛
弛 弛? chest pain         弛 弛 Results:                      弛
弛 弛? headache           弛 弛 忙式式式式式式式式式式式式式式式式式式式式式式式式式式式忖 弛
弛 弛? ...                弛 弛 弛22298006 | Myocardial      弛 弛
弛 弛                     弛 弛 弛          infarction       弛 弛
弛 弛                     弛 弛 戍式式式式式式式式式式式式式式式式式式式式式式式式式式式扣 弛
弛 戌式式式式式式式式式式式式式式式式式式式式式戎 弛 弛401303003 | Acute STEMI   弛 弛
弛                         弛 戌式式式式式式式式式式式式式式式式式式式式式式式式式式式戎 弛
弛                         弛 [Map Selected Concept]        弛
戍式式式式式式式式式式式式式式式式式式式式式式式式式扛式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式扣
弛 Mapping Details (myocardial infarction)                 弛
弛 忙式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式忖 弛
弛 弛 Concept: 22298006 - Myocardial infarction (disorder)弛 弛
弛 弛 Type: [exact ∪]                                     弛 弛
弛 弛 Confidence: [收收收收收收收收收收] 100%                       弛 弛
弛 弛 Notes: [Standard mapping                          ] 弛 弛
弛 弛 [Save Mapping]  [Remove Mapping]                    弛 弛
弛 戌式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式戎 弛
戌式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式戎
```

### Indicators
- **[?]** next to phrase: Mapped to SNOMED
- **No indicator**: Not mapped (highlight in red during phrase highlighting)

## Future Enhancements (Not Implemented)

### 1. SNOMED Relationships
- Store parent-child, part-of, associated-with relationships
- Enable hierarchical queries (e.g., "all phrases for cardiovascular disorders")

### 2. Post-Coordinated Expressions
- Support SNOMED expression constraints (compositional grammar)
- Example: "Myocardial infarction [disorder] : Finding site = Left ventricle [body structure]"

### 3. Multi-Branch Support
- Support different SNOMED editions (INT, US, KR)
- Store branch information in concept_cache
- UI filter by edition

### 4. Temporal Tables (SQL Server 2016+)
- Automatic audit history tracking
- Query historical mappings (e.g., "what was the mapping on 2024-01-01?")

### 5. Machine-Assisted Mapping
- NLP-based concept suggestions
- Similarity algorithms (string matching, embedding distance)
- Batch mapping suggestions for review

## Migration from Existing Systems

If Radium has legacy phrase-SNOMED mappings in old format:

### CSV Import Format
```csv
phrase_text,concept_id,mapping_type,confidence,notes
myocardial infarction,22298006,exact,1.0,"Standard mapping"
chest pain,29857009,broader,0.9,"General symptom"
```

### Import Workflow
1. Validate CSV: Check columns, data types, required fields
2. Lookup phrases: Match `phrase_text` to existing `radium.phrase.id`
3. Validate concepts: Ensure all `concept_id` exist in cache (fetch from Snowstorm if missing)
4. Preview: Show import summary with warnings (e.g., "Phrase not found: X")
5. Execute: Batch insert/update via stored procedures
6. Report: Show success/error counts

## Performance Considerations

### Expected Scale
- **Phrases**: 10,000 - 100,000 per account
- **Concepts**: 50,000 - 500,000 cached (subset of SNOMED ~350K concepts)
- **Mappings**: 10,000 - 100,000 per account

### Optimization Strategies
1. **Indexes**: All key columns indexed for fast lookups
2. **Caching**: Application-level cache for frequently used concepts
3. **Pagination**: Load phrases in batches (e.g., 100 at a time)
4. **Materialized View**: Consider if `v_phrase_snomed_combined` queries slow
5. **Query Plans**: Monitor execution plans and add covering indexes as needed

### Benchmark Targets (Azure SQL S1 tier)
- Concept cache lookup: < 10ms
- Phrase-concept join query: < 100ms
- View query (1000 phrases): < 1 second
- Stored procedure execution: < 50ms

## Security and Access Control

### Permissions (Future Implementation)
- **Read**: All authenticated users can view global mappings
- **Write**: Only administrators can create/modify global mappings
- **Account-specific**: Users can create/modify their own account mappings

### Audit Trail
- `mapped_by` column tracks who created global mappings
- `updated_at` tracks when mappings changed
- Future: Temporal tables or audit log for full history

## Compliance and Standards

### SNOMED CT Licensing
- Requires SNOMED CT license from IHTSDO or affiliate (e.g., NLM for US)
- Snowstorm must be configured with licensed edition
- Respect SNOMED CT usage terms (no redistribution of concept definitions)

### Terminology Mapping Standards
- **UMLS**: Unified Medical Language System mapping guidelines
- **HL7 FHIR**: ConceptMap resource for interoperability
- **ICD-10**: Consider dual mapping to SNOMED + ICD-10 for billing

## Files Created
- `db\schema\central_db_phrase_snomed_mapping.sql` - Complete SQL schema
- `db\schema\PHRASE_SNOMED_MAPPING_SUMMARY.md` - This design document
- **Updated**:
  - `apps\Wysg.Musm.Radium\docs\Spec.md` - Added FR-900 through FR-915
  - `apps\Wysg.Musm.Radium\docs\Plan.md` - Added implementation plan
  - `apps\Wysg.Musm.Radium\docs\Tasks.md` - Added tasks T900-T930

## Build Status
? Build succeeded with no errors
? All documentation updated cumulatively
? All schema tasks marked complete (T900-T918)

## Next Steps
1. **Deploy SQL schema** to Azure central database
2. **Test stored procedures** with sample data (V301-V309)
3. **Implement C# service layer** for Snowstorm API (T919-T920)
4. **Design UI** for Global Phrases tab (T921-T923)
5. **Implement phrase highlighting** with SNOMED colors (T926)
6. **Add export/import** functionality (T928-T929)

## Questions to Resolve
1. **Snowstorm URL**: Where is Snowstorm hosted? (localhost:8080 or remote server?)
2. **SNOMED Edition**: Which edition(s) to support? (INT, US, KR?)
3. **Access Control**: Who can create global mappings? (admin only or power users?)
4. **Cache Expiration**: How often to refresh concept cache? (weekly, monthly?)
5. **Multi-Concept Mapping**: Future requirement for phrases with multiple meanings?
