# Phrase-to-SNOMED Mapping Schema Diagram

## Entity Relationship Diagram

```
��������������������������������������������������������������
��   radium.phrase             ��
��  (existing table)           ��
��������������������������������������������������������������
�� ? id (PK)                   ��
�� ? account_id (nullable)     ��������
�� ? text                      ��  ��
�� ? active                    ��  ��
�� ? created_at                ��  ��
�� ? updated_at                ��  ��
�� ? rev                       ��  ��
��������������������������������������������������������������  ��
                                 ��
         ������������������������������������������������������������������������������������������������������
         ��                                                 ��
         �� WHERE account_id IS NULL                       �� WHERE account_id IS NOT NULL
��������������������������������������������������������������                 ��������������������������������������������������������������
�� radium.global_phrase_snomed ��                 ��   radium.phrase_snomed      ��
��������������������������������������������������������������                 ��������������������������������������������������������������
�� ? id (PK)                   ��                 �� ? id (PK)                   ��
�� ? phrase_id (UNIQUE, FK) ������������������������������������   �� ? phrase_id (UNIQUE, FK) ��������������������
�� ? concept_id (FK) ����������������������������          ��   �� ? concept_id (FK) ����������������������������  ��
�� ? mapping_type              ��  ��          ��   �� ? mapping_type              ��  ��  ��
�� ? confidence                ��  ��          ��   �� ? confidence                ��  ��  ��
�� ? notes                     ��  ��          ��   �� ? notes                     ��  ��  ��
�� ? mapped_by                 ��  ��          ��   �� ? created_at                ��  ��  ��
�� ? created_at                ��  ��          ��   �� ? updated_at                ��  ��  ��
�� ? updated_at                ��  ��          ��   ��������������������������������������������������������������  ��  ��
��������������������������������������������������������������  ��          ��                                    ��  ��
         ��                       ��          ��                                    ��  ��
         ��                       ��          ��                                    ��  ��
         ��                       ��          ��                                    ��  ��
         ��              ��������������������������������������������������������������                          ��  ��
         ��              ��  snomed.concept_cache       ��                          ��  ��
         ��              ��������������������������������������������������������������                          ��  ��
         ��              �� ? concept_id (PK) �禡������������������������������������������������������������������������  ��
         ��              �� ? concept_id_str (UNIQUE)   ��                             ��
         ��              �� ? fsn                       ��                             ��
         ��              �� ? pt                        ��                             ��
         ��              �� ? module_id                 ��                             ��
         ��              �� ? active                    ��                             ��
         ��              �� ? cached_at                 ��                             ��
         ��              �� ? expires_at                ��                             ��
         ��              ��������������������������������������������������������������                             ��
         ��                         ��                                                ��
         ��                         ��                                                ��
         ��                         ���� Cached from Snowstorm API                    ��
         ��                                                                          ��
         ��                                                                          ��
         ��������������������������������������������������������������������������������������������������������������������������������������������������������
                                        ��
                      ������������������������������������������������������������������������������
                      �� radium.v_phrase_snomed_combined     ��
                      ��           (VIEW - UNION ALL)        ��
                      ������������������������������������������������������������������������������
                      �� ? phrase_id                         ��
                      �� ? account_id                        ��
                      �� ? phrase_text                       ��
                      �� ? concept_id                        ��
                      �� ? concept_id_str                    ��
                      �� ? fsn                               ��
                      �� ? pt                                ��
                      �� ? mapping_type                      ��
                      �� ? confidence                        ��
                      �� ? notes                             ��
                      �� ? mapping_source ('global'|'account')��
                      �� ? created_at                        ��
                      �� ? updated_at                        ��
                      ������������������������������������������������������������������������������
```

## Relationships

### Foreign Keys
- `global_phrase_snomed.phrase_id` �� `phrase.id` (CASCADE DELETE)
- `global_phrase_snomed.concept_id` �� `concept_cache.concept_id` (RESTRICT DELETE)
- `phrase_snomed.phrase_id` �� `phrase.id` (CASCADE DELETE)
- `phrase_snomed.concept_id` �� `concept_cache.concept_id` (RESTRICT DELETE)

### Cardinality
- **One phrase �� One concept**: UNIQUE constraint on `phrase_id` in both mapping tables
- **One concept �� Many phrases**: Many phrases can map to same concept (via concept_id)
- **One account �� Many phrases**: Phrases scoped by account_id (NULL = global)

## Data Flow

### Mapping Creation Flow
```
��������������������������������
�� User searches��
��  "infarction"��
��������������������������������
       ��
������������������������������������������������
�� Snowstorm API Query  ��
�� GET /concepts?term=..��
������������������������������������������������
       ��
��������������������������������������������������������
�� Cache concepts via       ��
�� snomed.upsert_concept    ��
��������������������������������������������������������
       ��
��������������������������������������������������������
�� concept_cache populated  ��
�� with search results      ��
��������������������������������������������������������
       ��
��������������������������������������������������������
�� User selects concept     ��
�� and maps to phrase       ��
��������������������������������������������������������
       ��
����������������������������������������������������������������
�� Call stored procedure:       ��
�� - map_global_phrase_to_snomed��
�� - map_phrase_to_snomed       ��
����������������������������������������������������������������
       ��
����������������������������������������������������������������
�� Mapping saved to database    ��
�� (global or account table)    ��
����������������������������������������������������������������
```

### Query Flow (Application Lookup)
```
��������������������������������������������������������
�� App needs phrase-concept ��
�� mapping for phrase_id=123��
��������������������������������������������������������
       ��
������������������������������������������������������������������������
�� Query v_phrase_snomed_combined   ��
�� WHERE phrase_id = 123            ��
������������������������������������������������������������������������
       ��
������������������������������������������������������������������������
�� View JOINs:                      ��
�� - phrase table                   ��
�� - mapping table (global or acct) ��
�� - concept_cache                  ��
������������������������������������������������������������������������
       ��
������������������������������������������������������������������������
�� Return row with:                 ��
�� - phrase_text                    ��
�� - concept_id, fsn, pt            ��
�� - mapping_type, confidence       ��
�� - mapping_source                 ��
������������������������������������������������������������������������
```

## Mapping Type Examples

### exact
```
Phrase: "myocardial infarction"
Concept: 22298006 | Myocardial infarction (disorder)
Rationale: Exact match in meaning
```

### broader
```
Phrase: "chest pain"
Concept: 29857009 | Chest pain (finding)
Rationale: Phrase is more general than specific MI concepts
```

### narrower
```
Phrase: "STEMI"
Concept: 401303003 | Acute ST segment elevation myocardial infarction (disorder)
Rationale: Phrase is more specific than general MI concept 22298006
```

### related
```
Phrase: "heart attack"
Concept: 22298006 | Myocardial infarction (disorder)
Rationale: Common term for MI, but not exact clinical terminology
```

## Account vs Global Mapping Strategy

### Scenario 1: Global mapping exists
```
Global: "myocardial infarction" �� 22298006
Account A: (no override)
Account B: (no override)

Result: All accounts use concept 22298006
```

### Scenario 2: Account overrides global
```
Global: "myocardial infarction" �� 22298006
Account A: "myocardial infarction" �� 401303003 (STEMI)
Account B: (no override)

Result:
- Account A uses concept 401303003 (override)
- Account B uses concept 22298006 (global default)
```

### Scenario 3: Account-specific phrase
```
Global: (no mapping)
Account A: "�ɱٰ��" (Korean) �� 22298006
Account B: (no such phrase)

Result:
- Account A has Korean phrase mapped to 22298006
- Account B unaffected (phrase not visible)
```

## Index Strategy

### Concept Cache Indexes
```
IX_snomed_concept_cache_fsn    �� Fast search by Fully Specified Name
IX_snomed_concept_cache_pt     �� Fast search by Preferred Term
IX_snomed_concept_cache_cached_at �� Fast refresh query for stale concepts
```

### Mapping Table Indexes
```
IX_global_phrase_snomed_concept �� Fast reverse lookup (all phrases for concept X)
IX_global_phrase_snomed_mapping_type �� Fast filter by mapping type
IX_global_phrase_snomed_mapped_by �� Fast audit query (who mapped what)
IX_phrase_snomed_concept �� Fast reverse lookup (all phrases for concept X)
IX_phrase_snomed_mapping_type �� Fast filter by mapping type
IX_phrase_snomed_created �� Fast temporal query (recent mappings)
```

## Stored Procedure Call Examples

### Upsert Concept from Snowstorm
```sql
EXEC snomed.upsert_concept
    @concept_id = 22298006,
    @concept_id_str = '22298006',
    @fsn = 'Myocardial infarction (disorder)',
    @pt = 'Myocardial infarction',
    @module_id = '900000000000207008',
    @active = 1
```

### Map Global Phrase to SNOMED
```sql
EXEC radium.map_global_phrase_to_snomed
    @phrase_id = 123,
    @concept_id = 22298006,
    @mapping_type = 'exact',
    @confidence = 1.0,
    @notes = 'Standard mapping',
    @mapped_by = 456  -- account_id of admin
```

### Map Account Phrase to SNOMED
```sql
EXEC radium.map_phrase_to_snomed
    @phrase_id = 789,
    @concept_id = 22298006,
    @mapping_type = 'exact',
    @confidence = 0.95,
    @notes = 'Account-specific override'
```

## Query Examples

### Get mapping for specific phrase
```sql
SELECT phrase_text, concept_id_str, fsn, pt, mapping_type, mapping_source
FROM radium.v_phrase_snomed_combined
WHERE phrase_id = 123
```

### Get all phrases mapped to specific concept
```sql
SELECT phrase_text, mapping_type, account_id
FROM radium.v_phrase_snomed_combined
WHERE concept_id = 22298006
ORDER BY account_id NULLS FIRST, phrase_text
```

### Get unmapped global phrases
```sql
SELECT p.id, p.text
FROM radium.phrase p
LEFT JOIN radium.global_phrase_snomed gps ON gps.phrase_id = p.id
WHERE p.account_id IS NULL
  AND gps.id IS NULL
  AND p.active = 1
ORDER BY p.text
```

### Get phrases mapped by specific user
```sql
SELECT phrase_text, concept_id_str, fsn, created_at
FROM radium.v_phrase_snomed_combined
WHERE mapping_source = 'global'
  AND phrase_id IN (
    SELECT phrase_id FROM radium.global_phrase_snomed
    WHERE mapped_by = 456
  )
ORDER BY created_at DESC
```

### Get recent mappings (audit)
```sql
SELECT phrase_text, concept_id_str, fsn, mapping_type, 
       mapping_source, created_at, updated_at
FROM radium.v_phrase_snomed_combined
WHERE created_at >= DATEADD(day, -7, GETUTCDATE())
ORDER BY created_at DESC
```

## Performance Benchmarks (Expected)

### Azure SQL S1 Tier
| Operation | Target | Notes |
|---|---|---|
| Concept cache lookup by ID | < 10ms | Clustered PK lookup |
| Concept search by FSN | < 50ms | Indexed search |
| Phrase mapping lookup | < 20ms | UNIQUE index on phrase_id |
| View query (100 phrases) | < 100ms | JOIN with indexes |
| View query (1000 phrases) | < 1s | May need pagination |
| Stored procedure execution | < 50ms | MERGE with indexes |
| Bulk insert (1000 mappings) | < 10s | Batched inserts |

### Optimization Strategies
1. **Application-level caching**: Cache frequently used mappings in memory
2. **Pagination**: Load phrases in batches (100-500 at a time)
3. **Materialized view**: Convert view to table if performance degrades
4. **Covering indexes**: Add if query plans show key lookups
5. **Read replicas**: For read-heavy workloads in production

## Compliance and Licensing

### SNOMED CT Licensing Requirements
- ? Requires valid SNOMED CT license (IHTSDO or affiliate)
- ? Snowstorm must be configured with licensed edition
- ? No redistribution of concept definitions outside licensed environment
- ? Display SNOMED CT attribution in UI

### Data Privacy (HIPAA/GDPR)
- ? No PHI in concept_cache (only SNOMED standard concepts)
- ? Phrase text may contain PHI �� encrypt database at rest
- ? Audit trail for compliance (mapped_by, timestamps)
- ? Right to erasure: CASCADE delete ensures mapping removed with phrase

### Terminology Standards
- ? UMLS mapping guidelines (exact/broader/narrower/related)
- ? HL7 FHIR ConceptMap resource compatible
- ? ICD-10 cross-mapping (future enhancement)

## Conclusion

This schema design provides:
- ? **Scalability**: Handles 100K+ phrases and concepts
- ? **Performance**: Indexed for fast lookups and queries
- ? **Flexibility**: Supports global and account-specific mappings
- ? **Integrity**: Foreign keys and constraints enforce data quality
- ? **Audit**: Timestamps and mapping_source track changes
- ? **Interoperability**: SNOMED CT standard for semantic exchange
- ? **Usability**: View simplifies application queries

Next steps: Deploy schema, test procedures, implement UI integration.
