-- =====================================================
-- SNOMED Phrase Mapping - Complete Idempotent Migration
-- Date: 2025-01-19
-- Version: 1.0 FINAL
-- Database: Azure SQL (central_db)
-- =====================================================
-- 
-- PURPOSE:
-- This script adds SNOMED CT phrase mapping support to the central database.
-- It is fully idempotent and can be re-run safely without errors.
--
-- FEATURES:
-- - SNOMED concept cache with semantic tags
-- - Global and account-specific phrase-to-SNOMED mappings
-- - Phrase tags (JSON) with computed columns for performance
-- - Bulk import procedure with lowercase normalization
-- - Statistics and reporting views
-- - All necessary indexes and triggers
--
-- DEPLOYMENT:
-- Run this script on your Azure SQL central database.
-- All operations use IF NOT EXISTS guards for safety.
-- =====================================================

SET NOCOUNT ON;
GO

PRINT '=====================================================';
PRINT 'SNOMED Phrase Mapping Migration - Starting';
PRINT 'Timestamp: ' + CONVERT(VARCHAR(30), SYSUTCDATETIME(), 120);
PRINT '=====================================================';
GO

-- =====================================================
-- SECTION 1: Schema Creation
-- =====================================================
PRINT '';
PRINT 'SECTION 1: Creating schemas...';
GO

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'snomed')
BEGIN
    EXEC('CREATE SCHEMA snomed');
    PRINT '  ? Created schema: snomed';
END
ELSE
BEGIN
    PRINT '  - Schema already exists: snomed';
END
GO

-- =====================================================
-- SECTION 2: Add Tags Columns to radium.phrase
-- =====================================================
PRINT '';
PRINT 'SECTION 2: Adding tags columns to radium.phrase...';
GO

-- Add tags column (JSON)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('radium.phrase') AND name = 'tags')
BEGIN
    ALTER TABLE [radium].[phrase] ADD [tags] NVARCHAR(MAX) NULL;
    PRINT '  ? Added column: tags';
END
ELSE
BEGIN
    PRINT '  - Column already exists: tags';
END
GO

-- Add JSON validation constraint
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_phrase_tags_valid_json')
BEGIN
    ALTER TABLE [radium].[phrase] 
    ADD CONSTRAINT [CK_phrase_tags_valid_json] 
    CHECK ([tags] IS NULL OR ISJSON([tags]) = 1);
    PRINT '  ? Added constraint: CK_phrase_tags_valid_json';
END
ELSE
BEGIN
    PRINT '  - Constraint already exists: CK_phrase_tags_valid_json';
END
GO

-- Add computed column: tags_source
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('radium.phrase') AND name = 'tags_source')
BEGIN
    ALTER TABLE [radium].[phrase] 
    ADD [tags_source] AS CAST(JSON_VALUE([tags], '$.source') AS NVARCHAR(50)) PERSISTED;
    PRINT '  ? Added computed column: tags_source';
END
ELSE
BEGIN
    PRINT '  - Computed column already exists: tags_source';
END
GO

-- Add computed column: tags_semantic_tag
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('radium.phrase') AND name = 'tags_semantic_tag')
BEGIN
    ALTER TABLE [radium].[phrase] 
    ADD [tags_semantic_tag] AS CAST(JSON_VALUE([tags], '$.semantic_tag') AS NVARCHAR(100)) PERSISTED;
    PRINT '  ? Added computed column: tags_semantic_tag';
END
ELSE
BEGIN
    PRINT '  - Computed column already exists: tags_semantic_tag';
END
GO

-- =====================================================
-- SECTION 3: Create snomed.concept_cache Table
-- =====================================================
PRINT '';
PRINT 'SECTION 3: Creating snomed.concept_cache table...';
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE schema_id = SCHEMA_ID('snomed') AND name = 'concept_cache')
BEGIN
    CREATE TABLE snomed.concept_cache (
        concept_id BIGINT NOT NULL PRIMARY KEY,
        concept_id_str VARCHAR(18) NOT NULL UNIQUE,
        fsn NVARCHAR(500) NOT NULL,
        pt NVARCHAR(500) NULL,
        semantic_tag NVARCHAR(100) NULL,
        module_id VARCHAR(18) NULL,
        active BIT NOT NULL DEFAULT 1,
        cached_at DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
        expires_at DATETIME2(3) NULL
    );
    PRINT '  ? Created table: snomed.concept_cache';
END
ELSE
BEGIN
    PRINT '  - Table already exists: snomed.concept_cache';
    
    -- Add semantic_tag column if missing (from older versions)
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('snomed.concept_cache') AND name = 'semantic_tag')
    BEGIN
        ALTER TABLE [snomed].[concept_cache] ADD [semantic_tag] NVARCHAR(100) NULL;
        PRINT '  ? Added missing column: semantic_tag';
    END
END
GO

-- =====================================================
-- SECTION 4: Create Mapping Tables
-- =====================================================
PRINT '';
PRINT 'SECTION 4: Creating phrase-SNOMED mapping tables...';
GO

-- Global phrase mapping table
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE schema_id = SCHEMA_ID('radium') AND name = 'global_phrase_snomed')
BEGIN
    CREATE TABLE radium.global_phrase_snomed (
        id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        phrase_id BIGINT NOT NULL UNIQUE,
        concept_id BIGINT NOT NULL,
        mapping_type VARCHAR(20) NOT NULL DEFAULT 'exact',
        confidence DECIMAL(3,2) NULL,
        notes NVARCHAR(500) NULL,
        mapped_by BIGINT NULL,
        created_at DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
        updated_at DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
        
        CONSTRAINT FK_global_phrase_snomed_phrase FOREIGN KEY (phrase_id) 
            REFERENCES radium.phrase(id) ON DELETE CASCADE,
        
        CONSTRAINT FK_global_phrase_snomed_concept FOREIGN KEY (concept_id) 
            REFERENCES snomed.concept_cache(concept_id) ON DELETE NO ACTION,
        
        CONSTRAINT CK_global_phrase_snomed_mapping_type 
            CHECK (mapping_type IN ('exact', 'broader', 'narrower', 'related')),
        
        CONSTRAINT CK_global_phrase_snomed_confidence 
            CHECK (confidence IS NULL OR (confidence >= 0.00 AND confidence <= 1.00))
    );
    PRINT '  ? Created table: radium.global_phrase_snomed';
END
ELSE
BEGIN
    PRINT '  - Table already exists: radium.global_phrase_snomed';
END
GO

-- Account-specific phrase mapping table
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE schema_id = SCHEMA_ID('radium') AND name = 'phrase_snomed')
BEGIN
    CREATE TABLE radium.phrase_snomed (
        id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        phrase_id BIGINT NOT NULL,
        concept_id BIGINT NOT NULL,
        mapping_type VARCHAR(20) NOT NULL DEFAULT 'exact',
        confidence DECIMAL(3,2) NULL,
        notes NVARCHAR(500) NULL,
        created_at DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
        updated_at DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
        
        CONSTRAINT UQ_phrase_snomed_phrase UNIQUE (phrase_id),
        
        CONSTRAINT FK_phrase_snomed_phrase FOREIGN KEY (phrase_id) 
            REFERENCES radium.phrase(id) ON DELETE CASCADE,
        
        CONSTRAINT FK_phrase_snomed_concept FOREIGN KEY (concept_id) 
            REFERENCES snomed.concept_cache(concept_id) ON DELETE NO ACTION,
        
        CONSTRAINT CK_phrase_snomed_mapping_type 
            CHECK (mapping_type IN ('exact', 'broader', 'narrower', 'related')),
        
        CONSTRAINT CK_phrase_snomed_confidence 
            CHECK (confidence IS NULL OR (confidence >= 0.00 AND confidence <= 1.00))
    );
    PRINT '  ? Created table: radium.phrase_snomed';
END
ELSE
BEGIN
    PRINT '  - Table already exists: radium.phrase_snomed';
END
GO

-- =====================================================
-- SECTION 5: Create Indexes
-- =====================================================
PRINT '';
PRINT 'SECTION 5: Creating indexes...';
GO

-- Indexes on snomed.concept_cache
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_snomed_concept_cache_fsn' AND object_id = OBJECT_ID('snomed.concept_cache'))
BEGIN
    CREATE INDEX IX_snomed_concept_cache_fsn ON snomed.concept_cache(fsn);
    PRINT '  ? Created index: IX_snomed_concept_cache_fsn';
END
ELSE PRINT '  - Index exists: IX_snomed_concept_cache_fsn';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_snomed_concept_cache_pt' AND object_id = OBJECT_ID('snomed.concept_cache'))
BEGIN
    CREATE INDEX IX_snomed_concept_cache_pt ON snomed.concept_cache(pt);
    PRINT '  ? Created index: IX_snomed_concept_cache_pt';
END
ELSE PRINT '  - Index exists: IX_snomed_concept_cache_pt';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_snomed_concept_cache_semantic_tag' AND object_id = OBJECT_ID('snomed.concept_cache'))
BEGIN
    CREATE INDEX IX_snomed_concept_cache_semantic_tag ON snomed.concept_cache(semantic_tag) 
    WHERE semantic_tag IS NOT NULL;
    PRINT '  ? Created index: IX_snomed_concept_cache_semantic_tag';
END
ELSE PRINT '  - Index exists: IX_snomed_concept_cache_semantic_tag';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_snomed_concept_cache_cached_at' AND object_id = OBJECT_ID('snomed.concept_cache'))
BEGIN
    CREATE INDEX IX_snomed_concept_cache_cached_at ON snomed.concept_cache(cached_at DESC);
    PRINT '  ? Created index: IX_snomed_concept_cache_cached_at';
END
ELSE PRINT '  - Index exists: IX_snomed_concept_cache_cached_at';
GO

-- Indexes on radium.global_phrase_snomed
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_global_phrase_snomed_concept' AND object_id = OBJECT_ID('radium.global_phrase_snomed'))
BEGIN
    CREATE INDEX IX_global_phrase_snomed_concept ON radium.global_phrase_snomed(concept_id);
    PRINT '  ? Created index: IX_global_phrase_snomed_concept';
END
ELSE PRINT '  - Index exists: IX_global_phrase_snomed_concept';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_global_phrase_snomed_mapping_type' AND object_id = OBJECT_ID('radium.global_phrase_snomed'))
BEGIN
    CREATE INDEX IX_global_phrase_snomed_mapping_type ON radium.global_phrase_snomed(mapping_type);
    PRINT '  ? Created index: IX_global_phrase_snomed_mapping_type';
END
ELSE PRINT '  - Index exists: IX_global_phrase_snomed_mapping_type';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_global_phrase_snomed_mapped_by' AND object_id = OBJECT_ID('radium.global_phrase_snomed'))
BEGIN
    CREATE INDEX IX_global_phrase_snomed_mapped_by ON radium.global_phrase_snomed(mapped_by);
    PRINT '  ? Created index: IX_global_phrase_snomed_mapped_by';
END
ELSE PRINT '  - Index exists: IX_global_phrase_snomed_mapped_by';
GO

-- Indexes on radium.phrase_snomed
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_phrase_snomed_concept' AND object_id = OBJECT_ID('radium.phrase_snomed'))
BEGIN
    CREATE INDEX IX_phrase_snomed_concept ON radium.phrase_snomed(concept_id);
    PRINT '  ? Created index: IX_phrase_snomed_concept';
END
ELSE PRINT '  - Index exists: IX_phrase_snomed_concept';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_phrase_snomed_mapping_type' AND object_id = OBJECT_ID('radium.phrase_snomed'))
BEGIN
    CREATE INDEX IX_phrase_snomed_mapping_type ON radium.phrase_snomed(mapping_type);
    PRINT '  ? Created index: IX_phrase_snomed_mapping_type';
END
ELSE PRINT '  - Index exists: IX_phrase_snomed_mapping_type';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_phrase_snomed_created' AND object_id = OBJECT_ID('radium.phrase_snomed'))
BEGIN
    CREATE INDEX IX_phrase_snomed_created ON radium.phrase_snomed(created_at DESC);
    PRINT '  ? Created index: IX_phrase_snomed_created';
END
ELSE PRINT '  - Index exists: IX_phrase_snomed_created';
GO

-- Indexes on radium.phrase (tags columns)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_phrase_tags_source' AND object_id = OBJECT_ID('radium.phrase'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_phrase_tags_source] 
    ON [radium].[phrase]([tags_source])
    INCLUDE ([id], [text], [active])
    WHERE [tags] IS NOT NULL;
    PRINT '  ? Created index: IX_phrase_tags_source';
END
ELSE PRINT '  - Index exists: IX_phrase_tags_source';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_phrase_tags_semantic_tag' AND object_id = OBJECT_ID('radium.phrase'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_phrase_tags_semantic_tag] 
    ON [radium].[phrase]([tags_semantic_tag])
    INCLUDE ([id], [text], [active], [tags_source])
    WHERE [tags] IS NOT NULL;
    PRINT '  ? Created index: IX_phrase_tags_semantic_tag';
END
ELSE PRINT '  - Index exists: IX_phrase_tags_semantic_tag';
GO

-- Update IX_phrase_global_text index to include tags columns
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_phrase_global_text' AND object_id = OBJECT_ID('radium.phrase'))
BEGIN
    -- Check if it has the old definition (without tags columns)
    DECLARE @has_tags_column INT;
    SELECT @has_tags_column = COUNT(*)
    FROM sys.index_columns ic
    JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
    WHERE ic.object_id = OBJECT_ID('radium.phrase')
      AND ic.index_id = (SELECT index_id FROM sys.indexes WHERE name = 'IX_phrase_global_text' AND object_id = OBJECT_ID('radium.phrase'))
      AND c.name = 'tags';
    
    IF @has_tags_column = 0
    BEGIN
        DROP INDEX [IX_phrase_global_text] ON [radium].[phrase];
        PRINT '  ? Dropped old index: IX_phrase_global_text';
        
        CREATE NONCLUSTERED INDEX [IX_phrase_global_text] 
        ON [radium].[phrase]([text])
        INCLUDE ([id], [active], [tags], [tags_source], [tags_semantic_tag])
        WHERE [account_id] IS NULL
        WITH (ONLINE = ON);
        PRINT '  ? Created updated index: IX_phrase_global_text (with tags columns)';
    END
    ELSE
    BEGIN
        PRINT '  - Index already up-to-date: IX_phrase_global_text';
    END
END
ELSE
BEGIN
    CREATE NONCLUSTERED INDEX [IX_phrase_global_text] 
    ON [radium].[phrase]([text])
    INCLUDE ([id], [active], [tags], [tags_source], [tags_semantic_tag])
    WHERE [account_id] IS NULL
    WITH (ONLINE = ON);
    PRINT '  ? Created index: IX_phrase_global_text';
END
GO

-- =====================================================
-- SECTION 6: Create Triggers
-- =====================================================
PRINT '';
PRINT 'SECTION 6: Creating triggers...';
GO

-- Trigger for global_phrase_snomed
IF OBJECT_ID('radium.trg_global_phrase_snomed_touch', 'TR') IS NOT NULL
BEGIN
    DROP TRIGGER radium.trg_global_phrase_snomed_touch;
    PRINT '  - Dropped existing trigger: trg_global_phrase_snomed_touch';
END
GO

CREATE TRIGGER trg_global_phrase_snomed_touch
ON radium.global_phrase_snomed
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    WITH Changed AS (
        SELECT i.id
        FROM inserted i
        JOIN deleted d ON i.id = d.id
        WHERE (i.concept_id <> d.concept_id)
           OR (i.mapping_type <> d.mapping_type)
           OR (ISNULL(i.confidence, -1) <> ISNULL(d.confidence, -1))
           OR (ISNULL(i.notes, N'') <> ISNULL(d.notes, N''))
    )
    UPDATE gps
    SET gps.updated_at = SYSUTCDATETIME()
    FROM radium.global_phrase_snomed gps
    INNER JOIN Changed c ON gps.id = c.id;
END
GO
PRINT '  ? Created trigger: trg_global_phrase_snomed_touch';
GO

-- Trigger for phrase_snomed
IF OBJECT_ID('radium.trg_phrase_snomed_touch', 'TR') IS NOT NULL
BEGIN
    DROP TRIGGER radium.trg_phrase_snomed_touch;
    PRINT '  - Dropped existing trigger: trg_phrase_snomed_touch';
END
GO

CREATE TRIGGER trg_phrase_snomed_touch
ON radium.phrase_snomed
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    WITH Changed AS (
        SELECT i.id
        FROM inserted i
        JOIN deleted d ON i.id = d.id
        WHERE (i.concept_id <> d.concept_id)
           OR (i.mapping_type <> d.mapping_type)
           OR (ISNULL(i.confidence, -1) <> ISNULL(d.confidence, -1))
           OR (ISNULL(i.notes, N'') <> ISNULL(d.notes, N''))
    )
    UPDATE ps
    SET ps.updated_at = SYSUTCDATETIME()
    FROM radium.phrase_snomed ps
    INNER JOIN Changed c ON ps.id = c.id;
END
GO
PRINT '  ? Created trigger: trg_phrase_snomed_touch';
GO

-- =====================================================
-- SECTION 7: Create Views
-- =====================================================
PRINT '';
PRINT 'SECTION 7: Creating views...';
GO

-- Combined mapping view
IF OBJECT_ID('radium.v_phrase_snomed_combined', 'V') IS NOT NULL
BEGIN
    DROP VIEW radium.v_phrase_snomed_combined;
    PRINT '  - Dropped existing view: v_phrase_snomed_combined';
END
GO

CREATE VIEW radium.v_phrase_snomed_combined AS
WITH account_mappings AS (
    SELECT 
        p.id AS phrase_id,
        p.account_id,
        p.text AS phrase_text,
        ps.concept_id,
        cc.concept_id_str,
        cc.fsn,
        cc.pt,
        cc.semantic_tag,
        ps.mapping_type,
        ps.confidence,
        ps.notes,
        'account' AS mapping_source,
        ps.created_at,
        ps.updated_at
    FROM radium.phrase p
    INNER JOIN radium.phrase_snomed ps ON ps.phrase_id = p.id
    INNER JOIN snomed.concept_cache cc ON cc.concept_id = ps.concept_id
    WHERE p.account_id IS NOT NULL
),
global_mappings AS (
    SELECT 
        p.id AS phrase_id,
        p.account_id,
        p.text AS phrase_text,
        gps.concept_id,
        cc.concept_id_str,
        cc.fsn,
        cc.pt,
        cc.semantic_tag,
        gps.mapping_type,
        gps.confidence,
        gps.notes,
        'global' AS mapping_source,
        gps.created_at,
        gps.updated_at
    FROM radium.phrase p
    INNER JOIN radium.global_phrase_snomed gps ON gps.phrase_id = p.id
    INNER JOIN snomed.concept_cache cc ON cc.concept_id = gps.concept_id
    WHERE p.account_id IS NULL
)
SELECT * FROM account_mappings
UNION ALL
SELECT * FROM global_mappings
GO
PRINT '  ? Created view: v_phrase_snomed_combined';
GO

-- Statistics view
IF OBJECT_ID('radium.v_snomed_phrase_stats', 'V') IS NOT NULL
BEGIN
    DROP VIEW radium.v_snomed_phrase_stats;
    PRINT '  - Dropped existing view: v_snomed_phrase_stats';
END
GO

CREATE VIEW [radium].[v_snomed_phrase_stats] AS
SELECT 
    p.tags_semantic_tag AS semantic_tag,
    JSON_VALUE(p.tags, '$.import_batch') AS import_batch,
    COUNT(*) AS phrase_count,
    SUM(CASE WHEN p.active = 1 THEN 1 ELSE 0 END) AS active_count,
    MIN(p.created_at) AS first_imported,
    MAX(p.created_at) AS last_imported,
    COUNT(DISTINCT gps.id) AS mapped_count
FROM radium.phrase p
LEFT JOIN radium.global_phrase_snomed gps ON gps.phrase_id = p.id
WHERE p.account_id IS NULL
  AND p.tags_source = 'snomed'
GROUP BY 
    p.tags_semantic_tag,
    JSON_VALUE(p.tags, '$.import_batch')
GO
PRINT '  ? Created view: v_snomed_phrase_stats';
GO

-- =====================================================
-- SECTION 8: Create Stored Procedures
-- =====================================================
PRINT '';
PRINT 'SECTION 8: Creating stored procedures...';
GO

-- Upsert SNOMED concept
IF OBJECT_ID('snomed.upsert_concept', 'P') IS NOT NULL
BEGIN
    DROP PROCEDURE snomed.upsert_concept;
    PRINT '  - Dropped existing procedure: upsert_concept';
END
GO

CREATE PROCEDURE snomed.upsert_concept
    @concept_id BIGINT,
    @concept_id_str VARCHAR(18),
    @fsn NVARCHAR(500),
    @pt NVARCHAR(500) = NULL,
    @semantic_tag NVARCHAR(100) = NULL,
    @module_id VARCHAR(18) = NULL,
    @active BIT = 1,
    @expires_at DATETIME2(3) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    MERGE snomed.concept_cache AS target
    USING (SELECT @concept_id AS concept_id) AS source
    ON target.concept_id = source.concept_id
    WHEN MATCHED THEN
        UPDATE SET 
            concept_id_str = @concept_id_str,
            fsn = @fsn,
            pt = @pt,
            semantic_tag = @semantic_tag,
            module_id = @module_id,
            active = @active,
            cached_at = SYSUTCDATETIME(),
            expires_at = @expires_at
    WHEN NOT MATCHED THEN
        INSERT (concept_id, concept_id_str, fsn, pt, semantic_tag, module_id, active, cached_at, expires_at)
        VALUES (@concept_id, @concept_id_str, @fsn, @pt, @semantic_tag, @module_id, @active, SYSUTCDATETIME(), @expires_at);
END
GO
PRINT '  ? Created procedure: snomed.upsert_concept';
GO

-- Map global phrase to SNOMED
IF OBJECT_ID('radium.map_global_phrase_to_snomed', 'P') IS NOT NULL
BEGIN
    DROP PROCEDURE radium.map_global_phrase_to_snomed;
    PRINT '  - Dropped existing procedure: map_global_phrase_to_snomed';
END
GO

CREATE PROCEDURE radium.map_global_phrase_to_snomed
    @phrase_id BIGINT,
    @concept_id BIGINT,
    @mapping_type VARCHAR(20) = 'exact',
    @confidence DECIMAL(3,2) = NULL,
    @notes NVARCHAR(500) = NULL,
    @mapped_by BIGINT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    IF NOT EXISTS (SELECT 1 FROM radium.phrase WHERE id = @phrase_id AND account_id IS NULL)
    BEGIN
        RAISERROR('Phrase must be global (account_id IS NULL)', 16, 1);
        RETURN;
    END
    
    IF NOT EXISTS (SELECT 1 FROM snomed.concept_cache WHERE concept_id = @concept_id)
    BEGIN
        RAISERROR('SNOMED concept not found in cache. Please cache concept first.', 16, 1);
        RETURN;
    END
    
    MERGE radium.global_phrase_snomed AS target
    USING (SELECT @phrase_id AS phrase_id) AS source
    ON target.phrase_id = source.phrase_id
    WHEN MATCHED THEN
        UPDATE SET 
            concept_id = @concept_id,
            mapping_type = @mapping_type,
            confidence = @confidence,
            notes = @notes,
            mapped_by = @mapped_by,
            updated_at = SYSUTCDATETIME()
    WHEN NOT MATCHED THEN
        INSERT (phrase_id, concept_id, mapping_type, confidence, notes, mapped_by)
        VALUES (@phrase_id, @concept_id, @mapping_type, @confidence, @notes, @mapped_by);
END
GO
PRINT '  ? Created procedure: radium.map_global_phrase_to_snomed';
GO

-- Map account phrase to SNOMED
IF OBJECT_ID('radium.map_phrase_to_snomed', 'P') IS NOT NULL
BEGIN
    DROP PROCEDURE radium.map_phrase_to_snomed;
    PRINT '  - Dropped existing procedure: map_phrase_to_snomed';
END
GO

CREATE PROCEDURE radium.map_phrase_to_snomed
    @phrase_id BIGINT,
    @concept_id BIGINT,
    @mapping_type VARCHAR(20) = 'exact',
    @confidence DECIMAL(3,2) = NULL,
    @notes NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    IF NOT EXISTS (SELECT 1 FROM radium.phrase WHERE id = @phrase_id AND account_id IS NOT NULL)
    BEGIN
        RAISERROR('Phrase must be account-specific (account_id IS NOT NULL)', 16, 1);
        RETURN;
    END
    
    IF NOT EXISTS (SELECT 1 FROM snomed.concept_cache WHERE concept_id = @concept_id)
    BEGIN
        RAISERROR('SNOMED concept not found in cache. Please cache concept first.', 16, 1);
        RETURN;
    END
    
    MERGE radium.phrase_snomed AS target
    USING (SELECT @phrase_id AS phrase_id) AS source
    ON target.phrase_id = source.phrase_id
    WHEN MATCHED THEN
        UPDATE SET 
            concept_id = @concept_id,
            mapping_type = @mapping_type,
            confidence = @confidence,
            notes = @notes,
            updated_at = SYSUTCDATETIME()
    WHEN NOT MATCHED THEN
        INSERT (phrase_id, concept_id, mapping_type, confidence, notes)
        VALUES (@phrase_id, @concept_id, @mapping_type, @confidence, @notes);
END
GO
PRINT '  ? Created procedure: radium.map_phrase_to_snomed';
GO

-- Bulk import SNOMED phrases (with lowercase normalization)
IF OBJECT_ID('radium.import_snomed_phrases', 'P') IS NOT NULL
BEGIN
    DROP PROCEDURE radium.import_snomed_phrases;
    PRINT '  - Dropped existing procedure: import_snomed_phrases';
END
GO

CREATE PROCEDURE [radium].[import_snomed_phrases]
    @semantic_tag NVARCHAR(100),
    @max_count INT = 5000,
    @import_batch NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @batch NVARCHAR(100) = ISNULL(@import_batch, 
        CONCAT('snomed-', @semantic_tag, '-', FORMAT(SYSUTCDATETIME(), 'yyyyMMdd')));
    
    -- Import phrases with lowercase normalization
    INSERT INTO radium.phrase (account_id, [text], active, tags)
    SELECT TOP (@max_count)
        NULL,  -- Global phrase
        LOWER(cc.pt),  -- Lowercase for consistency
        1,
        (SELECT 
            'snomed' AS [source],
            CAST(cc.concept_id AS NVARCHAR(20)) AS concept_id,
            cc.semantic_tag AS semantic_tag,
            @batch AS import_batch,
            CAST(1 AS BIT) AS is_preferred
         FOR JSON PATH, WITHOUT_ARRAY_WRAPPER)
    FROM snomed.concept_cache cc
    WHERE cc.semantic_tag = @semantic_tag
      AND cc.active = 1
      AND cc.pt IS NOT NULL
      AND LEN(cc.pt) > 2
      AND NOT EXISTS (
          SELECT 1 FROM radium.phrase p 
          WHERE p.text = LOWER(cc.pt)  -- Check lowercase
            AND p.account_id IS NULL
      )
    ORDER BY LEN(cc.pt), cc.pt;
    
    DECLARE @imported INT = @@ROWCOUNT;
    
    -- Auto-create mappings
    INSERT INTO radium.global_phrase_snomed (phrase_id, concept_id, mapping_type, confidence)
    SELECT 
        p.id,
        CAST(JSON_VALUE(p.tags, '$.concept_id') AS BIGINT),
        'exact',
        1.0
    FROM radium.phrase p
    WHERE JSON_VALUE(p.tags, '$.import_batch') = @batch
      AND NOT EXISTS (
          SELECT 1 FROM radium.global_phrase_snomed gps 
          WHERE gps.phrase_id = p.id
      );
    
    DECLARE @mapped INT = @@ROWCOUNT;
    
    -- Return summary
    SELECT 
        @semantic_tag AS semantic_tag,
        @imported AS phrases_imported,
        @mapped AS mappings_created,
        @batch AS import_batch;
END
GO
PRINT '  ? Created procedure: radium.import_snomed_phrases (with lowercase normalization)';
GO

-- =====================================================
-- SECTION 9: Verification
-- =====================================================
PRINT '';
PRINT 'SECTION 9: Verifying migration...';
GO

-- Count objects created
DECLARE @table_count INT, @index_count INT, @proc_count INT, @view_count INT, @trigger_count INT;

SELECT @table_count = COUNT(*) 
FROM sys.tables 
WHERE schema_id IN (SCHEMA_ID('snomed'), SCHEMA_ID('radium'))
  AND name IN ('concept_cache', 'global_phrase_snomed', 'phrase_snomed');

SELECT @index_count = COUNT(*) 
FROM sys.indexes 
WHERE object_id IN (
    OBJECT_ID('snomed.concept_cache'),
    OBJECT_ID('radium.global_phrase_snomed'),
    OBJECT_ID('radium.phrase_snomed'),
    OBJECT_ID('radium.phrase')
)
AND name LIKE '%snomed%' OR name LIKE '%tags%';

SELECT @proc_count = COUNT(*) 
FROM sys.procedures 
WHERE schema_id IN (SCHEMA_ID('snomed'), SCHEMA_ID('radium'))
  AND name IN ('upsert_concept', 'map_global_phrase_to_snomed', 'map_phrase_to_snomed', 'import_snomed_phrases');

SELECT @view_count = COUNT(*) 
FROM sys.views 
WHERE schema_id = SCHEMA_ID('radium')
  AND name IN ('v_phrase_snomed_combined', 'v_snomed_phrase_stats');

SELECT @trigger_count = COUNT(*) 
FROM sys.triggers 
WHERE name IN ('trg_global_phrase_snomed_touch', 'trg_phrase_snomed_touch');

PRINT '  ? Tables created/verified: ' + CAST(@table_count AS VARCHAR(10)) + '/3';
PRINT '  ? Indexes created/verified: ' + CAST(@index_count AS VARCHAR(10));
PRINT '  ? Stored procedures created: ' + CAST(@proc_count AS VARCHAR(10)) + '/4';
PRINT '  ? Views created: ' + CAST(@view_count AS VARCHAR(10)) + '/2';
PRINT '  ? Triggers created: ' + CAST(@trigger_count AS VARCHAR(10)) + '/2';
GO

-- =====================================================
-- SECTION 10: Completion
-- =====================================================
PRINT '';
PRINT '=====================================================';
PRINT 'SNOMED Phrase Mapping Migration - COMPLETED';
PRINT 'Timestamp: ' + CONVERT(VARCHAR(30), SYSUTCDATETIME(), 120);
PRINT '=====================================================';
PRINT '';
PRINT 'Next steps:';
PRINT '1. Run verification query (see below)';
PRINT '2. Test with sample data';
PRINT '3. Implement C# service layer';
PRINT '';
GO

-- =====================================================
-- VERIFICATION QUERY (Run this after migration)
-- =====================================================
/*
-- Verify all components exist
SELECT 
    'Tables' AS component_type,
    COUNT(*) AS count,
    3 AS expected
FROM sys.tables 
WHERE schema_id IN (SCHEMA_ID('radium'), SCHEMA_ID('snomed'), SCHEMA_ID('app'))
  AND name IN ('concept_cache', 'global_phrase_snomed', 'phrase_snomed')
UNION ALL
SELECT 'Stored Procedures', COUNT(*), 4
FROM sys.procedures 
WHERE schema_id IN (SCHEMA_ID('radium'), SCHEMA_ID('snomed'))
  AND name IN ('upsert_concept', 'map_global_phrase_to_snomed', 'map_phrase_to_snomed', 'import_snomed_phrases')
UNION ALL
SELECT 'Views', COUNT(*), 2
FROM sys.views 
WHERE schema_id IN (SCHEMA_ID('radium'), SCHEMA_ID('snomed'))
  AND name IN ('v_phrase_snomed_combined', 'v_snomed_phrase_stats')
UNION ALL
SELECT 'Triggers', COUNT(*), 2
FROM sys.triggers
WHERE name IN ('trg_global_phrase_snomed_touch', 'trg_phrase_snomed_touch')
UNION ALL
SELECT 'Phrase Columns', COUNT(*), 3
FROM sys.columns
WHERE object_id = OBJECT_ID('radium.phrase')
  AND name IN ('tags', 'tags_source', 'tags_semantic_tag');

-- Expected output: All counts should match expected values
*/

-- =====================================================
-- TEST SCRIPT (Run this to test the migration)
-- =====================================================
/*
-- 1. Cache a test concept
EXEC snomed.upsert_concept 
    @concept_id = 80891009,
    @concept_id_str = '80891009',
    @fsn = 'Heart structure (body structure)',
    @pt = 'Heart structure',
    @semantic_tag = 'body structure',
    @module_id = '900000000000207008',
    @active = 1;

-- 2. Create a global phrase (lowercase)
INSERT INTO radium.phrase (account_id, text, active, tags)
VALUES (
    NULL,
    'heart',
    1,
    (SELECT 
        'snomed' AS [source],
        '80891009' AS concept_id,
        'body structure' AS semantic_tag,
        'manual-test' AS import_batch,
        CAST(1 AS BIT) AS is_preferred
     FOR JSON PATH, WITHOUT_ARRAY_WRAPPER)
);

-- 3. Map it
DECLARE @phrase_id BIGINT = SCOPE_IDENTITY();
EXEC radium.map_global_phrase_to_snomed 
    @phrase_id = @phrase_id,
    @concept_id = 80891009,
    @mapping_type = 'exact',
    @confidence = 1.0;

-- 4. Verify
SELECT * FROM radium.v_phrase_snomed_combined WHERE phrase_text = 'heart';
SELECT * FROM radium.v_snomed_phrase_stats;

-- 5. Cleanup test data
DELETE FROM radium.phrase WHERE text = 'heart' AND account_id IS NULL;
DELETE FROM snomed.concept_cache WHERE concept_id = 80891009;
*/
