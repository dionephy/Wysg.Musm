-- Created by GitHub Copilot in SSMS - review carefully before executing
-- =============================================
-- Database Structure Export
-- Database: musmdb
-- Generated: For reference documentation
-- Contains: Table schemas, indexes, constraints, views, and stored procedures
-- =============================================

-- =============================================
-- SCHEMAS
-- =============================================

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'app')
    EXEC('CREATE SCHEMA [app]');
GO

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'radium')
    EXEC('CREATE SCHEMA [radium]');
GO

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'snomed')
    EXEC('CREATE SCHEMA [snomed]');
GO

-- =============================================
-- TABLE: app.account
-- Base account table for user authentication and profile
-- =============================================

CREATE TABLE [app].[account] (
    [account_id] BIGINT IDENTITY(1,1) NOT NULL,
    [uid] NVARCHAR(100) NOT NULL,
    [email] NVARCHAR(320) NOT NULL,
    [display_name] NVARCHAR(200) NULL,
    [is_active] BIT NOT NULL CONSTRAINT [DF_account_is_active] DEFAULT ((1)),
    [created_at] DATETIME2(3) NOT NULL CONSTRAINT [DF_account_created_at] DEFAULT (SYSUTCDATETIME()),
    [last_login_at] DATETIME2(3) NULL,
    CONSTRAINT [PK__account__46A222CDC1D94DD5] PRIMARY KEY CLUSTERED ([account_id] ASC),
    CONSTRAINT [UQ_account_uid] UNIQUE NONCLUSTERED ([uid]),
    CONSTRAINT [UQ_account_email] UNIQUE NONCLUSTERED ([email])
);
GO

-- =============================================
-- TABLE: snomed.concept_cache
-- Cache for SNOMED CT concepts with FSN and preferred terms
-- =============================================

CREATE TABLE [snomed].[concept_cache] (
    [concept_id] BIGINT NOT NULL,
    [concept_id_str] VARCHAR(18) NOT NULL,
    [fsn] NVARCHAR(500) NOT NULL,
    [pt] NVARCHAR(500) NULL,
    [module_id] VARCHAR(18) NULL,
    [active] BIT NOT NULL CONSTRAINT [DF__concept_c__activ__56E8E7AB] DEFAULT ((1)),
    [cached_at] DATETIME2(3) NOT NULL CONSTRAINT [DF__concept_c__cache__57DD0BE4] DEFAULT (SYSUTCDATETIME()),
    [expires_at] DATETIME2(3) NULL,
    [semantic_tag] NVARCHAR(100) NULL,
    CONSTRAINT [PK__concept___7925FD2DC0301197] PRIMARY KEY CLUSTERED ([concept_id] ASC),
    CONSTRAINT [UQ__concept___51E8BC2C44B21935] UNIQUE NONCLUSTERED ([concept_id_str])
);
GO

CREATE NONCLUSTERED INDEX [IX_snomed_concept_cache_fsn] 
    ON [snomed].[concept_cache] ([fsn]);
GO

CREATE NONCLUSTERED INDEX [IX_snomed_concept_cache_pt] 
    ON [snomed].[concept_cache] ([pt]);
GO

CREATE NONCLUSTERED INDEX [IX_snomed_concept_cache_semantic_tag] 
    ON [snomed].[concept_cache] ([semantic_tag]);
GO

CREATE NONCLUSTERED INDEX [IX_snomed_concept_cache_cached_at] 
    ON [snomed].[concept_cache] ([cached_at] DESC);
GO

-- =============================================
-- TABLE: radium.phrase
-- Medical phrases with optional account ownership (NULL = global phrase)
-- =============================================

CREATE TABLE [radium].[phrase] (
    [id] BIGINT IDENTITY(1,1) NOT NULL,
    [account_id] BIGINT NULL,
    [text] NVARCHAR(400) NOT NULL,
    [active] BIT NOT NULL CONSTRAINT [DF_phrase_active] DEFAULT ((1)),
    [created_at] DATETIME2(3) NOT NULL CONSTRAINT [DF_phrase_created] DEFAULT (SYSUTCDATETIME()),
    [updated_at] DATETIME2(3) NOT NULL CONSTRAINT [DF_phrase_updated] DEFAULT (SYSUTCDATETIME()),
    [rev] BIGINT NOT NULL CONSTRAINT [DF_phrase_rev] DEFAULT ((1)),
    [tags] NVARCHAR(MAX) NULL,
    [tags_source] AS (JSON_VALUE([tags], '$.source')) PERSISTED,
    [tags_semantic_tag] AS (JSON_VALUE([tags], '$.semantic_tag')) PERSISTED,
    CONSTRAINT [PK__phrase__3213E83FD97E3A5F] PRIMARY KEY CLUSTERED ([id] ASC),
    CONSTRAINT [FK_phrase_account] FOREIGN KEY ([account_id]) 
        REFERENCES [app].[account] ([account_id]) ON DELETE CASCADE,
    CONSTRAINT [CK_phrase_text_not_blank] CHECK (LEN(LTRIM(RTRIM([text]))) > 0),
    CONSTRAINT [CK_phrase_tags_valid_json] CHECK ([tags] IS NULL OR ISJSON([tags]) = 1),
    CONSTRAINT [CK_phrase_global_or_account_unique] CHECK ([account_id] IS NULL OR [account_id] IS NOT NULL)
);
GO

CREATE NONCLUSTERED INDEX [IX_phrase_account_active] 
    ON [radium].[phrase] ([account_id], [active]);
GO

CREATE NONCLUSTERED INDEX [IX_phrase_account_rev] 
    ON [radium].[phrase] ([account_id], [rev]);
GO

CREATE UNIQUE NONCLUSTERED INDEX [IX_phrase_account_text_unique] 
    ON [radium].[phrase] ([account_id], [text]) 
    WHERE [account_id] IS NOT NULL;
GO

CREATE NONCLUSTERED INDEX [IX_phrase_global_active] 
    ON [radium].[phrase] ([active]) 
    WHERE [account_id] IS NULL;
GO

CREATE NONCLUSTERED INDEX [IX_phrase_global_text] 
    ON [radium].[phrase] ([text]) 
    WHERE [account_id] IS NULL;
GO

CREATE UNIQUE NONCLUSTERED INDEX [IX_phrase_global_text_unique] 
    ON [radium].[phrase] ([text]) 
    WHERE [account_id] IS NULL;
GO

CREATE NONCLUSTERED INDEX [IX_phrase_tags_source] 
    ON [radium].[phrase] ([tags_source]);
GO

CREATE NONCLUSTERED INDEX [IX_phrase_tags_semantic_tag] 
    ON [radium].[phrase] ([tags_semantic_tag]);
GO

-- Legacy unique constraints (may be redundant with filtered indexes above)
CREATE UNIQUE NONCLUSTERED INDEX [UQ_phrase_account_text] 
    ON [radium].[phrase] ([account_id], [text]) 
    WHERE [account_id] IS NOT NULL;
GO

CREATE UNIQUE NONCLUSTERED INDEX [UQ_phrase_global_text] 
    ON [radium].[phrase] ([text]) 
    WHERE [account_id] IS NULL;
GO

-- =============================================
-- TABLE: radium.phrase_snomed
-- Maps account-specific phrases to SNOMED concepts
-- =============================================

CREATE TABLE [radium].[phrase_snomed] (
    [id] BIGINT IDENTITY(1,1) NOT NULL,
    [phrase_id] BIGINT NOT NULL,
    [concept_id] BIGINT NOT NULL,
    [mapping_type] VARCHAR(20) NOT NULL CONSTRAINT [DF__phrase_sn__mappi__681373AD] DEFAULT ('exact'),
    [confidence] DECIMAL(3,2) NULL,
    [notes] NVARCHAR(500) NULL,
    [created_at] DATETIME2(3) NOT NULL CONSTRAINT [DF__phrase_sn__creat__690797E6] DEFAULT (SYSUTCDATETIME()),
    [updated_at] DATETIME2(3) NOT NULL CONSTRAINT [DF__phrase_sn__updat__69FBBC1F] DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK__phrase_s__3213E83FBB6D797F] PRIMARY KEY CLUSTERED ([id] ASC),
    CONSTRAINT [UQ_phrase_snomed_phrase] UNIQUE NONCLUSTERED ([phrase_id]),
    CONSTRAINT [FK_phrase_snomed_phrase] FOREIGN KEY ([phrase_id]) 
        REFERENCES [radium].[phrase] ([id]) ON DELETE CASCADE,
    CONSTRAINT [FK_phrase_snomed_concept] FOREIGN KEY ([concept_id]) 
        REFERENCES [snomed].[concept_cache] ([concept_id]),
    CONSTRAINT [CK_phrase_snomed_mapping_type] 
        CHECK ([mapping_type] IN ('exact', 'broader', 'narrower', 'related')),
    CONSTRAINT [CK_phrase_snomed_confidence] 
        CHECK ([confidence] IS NULL OR ([confidence] >= 0.00 AND [confidence] <= 1.00))
);
GO

CREATE NONCLUSTERED INDEX [IX_phrase_snomed_concept] 
    ON [radium].[phrase_snomed] ([concept_id]);
GO

CREATE NONCLUSTERED INDEX [IX_phrase_snomed_mapping_type] 
    ON [radium].[phrase_snomed] ([mapping_type]);
GO

CREATE NONCLUSTERED INDEX [IX_phrase_snomed_created] 
    ON [radium].[phrase_snomed] ([created_at] DESC);
GO

-- =============================================
-- TABLE: radium.global_phrase_snomed
-- Maps global phrases to SNOMED concepts (includes mapped_by field)
-- =============================================

CREATE TABLE [radium].[global_phrase_snomed] (
    [id] BIGINT IDENTITY(1,1) NOT NULL,
    [phrase_id] BIGINT NOT NULL,
    [concept_id] BIGINT NOT NULL,
    [mapping_type] VARCHAR(20) NOT NULL CONSTRAINT [DF__global_ph__mappi__5E8A0973] DEFAULT ('exact'),
    [confidence] DECIMAL(3,2) NULL,
    [notes] NVARCHAR(500) NULL,
    [mapped_by] BIGINT NULL,
    [created_at] DATETIME2(3) NOT NULL CONSTRAINT [DF__global_ph__creat__5F7E2DAC] DEFAULT (SYSUTCDATETIME()),
    [updated_at] DATETIME2(3) NOT NULL CONSTRAINT [DF__global_ph__updat__607251E5] DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK__global_p__3213E83F47A06847] PRIMARY KEY CLUSTERED ([id] ASC),
    CONSTRAINT [UQ__global_p__8737606914827523] UNIQUE NONCLUSTERED ([phrase_id]),
    CONSTRAINT [FK_global_phrase_snomed_phrase] FOREIGN KEY ([phrase_id]) 
        REFERENCES [radium].[phrase] ([id]) ON DELETE CASCADE,
    CONSTRAINT [FK_global_phrase_snomed_concept] FOREIGN KEY ([concept_id]) 
        REFERENCES [snomed].[concept_cache] ([concept_id]),
    CONSTRAINT [CK_global_phrase_snomed_mapping_type] 
        CHECK ([mapping_type] IN ('exact', 'broader', 'narrower', 'related')),
    CONSTRAINT [CK_global_phrase_snomed_confidence] 
        CHECK ([confidence] IS NULL OR ([confidence] >= 0.00 AND [confidence] <= 1.00))
);
GO

CREATE NONCLUSTERED INDEX [IX_global_phrase_snomed_concept] 
    ON [radium].[global_phrase_snomed] ([concept_id]);
GO

CREATE NONCLUSTERED INDEX [IX_global_phrase_snomed_mapping_type] 
    ON [radium].[global_phrase_snomed] ([mapping_type]);
GO

CREATE NONCLUSTERED INDEX [IX_global_phrase_snomed_mapped_by] 
    ON [radium].[global_phrase_snomed] ([mapped_by]);
GO

-- =============================================
-- TABLE: radium.hotkey
-- Text expansion hotkeys per account
-- =============================================

CREATE TABLE [radium].[hotkey] (
    [hotkey_id] BIGINT IDENTITY(1,1) NOT NULL,
    [account_id] BIGINT NOT NULL,
    [trigger_text] NVARCHAR(64) NOT NULL,
    [expansion_text] NVARCHAR(4000) NOT NULL,
    [is_active] BIT NOT NULL CONSTRAINT [DF_hotkey_active] DEFAULT ((1)),
    [created_at] DATETIME2(3) NOT NULL CONSTRAINT [DF_hotkey_created] DEFAULT (SYSUTCDATETIME()),
    [updated_at] DATETIME2(3) NOT NULL CONSTRAINT [DF_hotkey_updated] DEFAULT (SYSUTCDATETIME()),
    [rev] BIGINT NOT NULL CONSTRAINT [DF_hotkey_rev] DEFAULT ((1)),
    [description] NVARCHAR(256) NULL,
    CONSTRAINT [PK_hotkey] PRIMARY KEY CLUSTERED ([hotkey_id] ASC),
    CONSTRAINT [UQ_hotkey_account_trigger] UNIQUE NONCLUSTERED ([account_id], [trigger_text]),
    CONSTRAINT [FK_hotkey_account] FOREIGN KEY ([account_id]) 
        REFERENCES [app].[account] ([account_id]) ON DELETE CASCADE,
    CONSTRAINT [CK_hotkey_trigger_not_blank] CHECK (LEN(LTRIM(RTRIM([trigger_text]))) > 0),
    CONSTRAINT [CK_hotkey_expansion_not_blank] CHECK (LEN(LTRIM(RTRIM([expansion_text]))) > 0)
);
GO

CREATE NONCLUSTERED INDEX [IX_hotkey_account_active] 
    ON [radium].[hotkey] ([account_id], [is_active]);
GO

CREATE NONCLUSTERED INDEX [IX_hotkey_account_rev] 
    ON [radium].[hotkey] ([account_id], [rev]);
GO

CREATE NONCLUSTERED INDEX [IX_hotkey_account_trigger_active] 
    ON [radium].[hotkey] ([account_id], [trigger_text], [is_active]);
GO

-- =============================================
-- TABLE: radium.snippet
-- Template snippets with variable support
-- =============================================

CREATE TABLE [radium].[snippet] (
    [snippet_id] BIGINT IDENTITY(1,1) NOT NULL,
    [account_id] BIGINT NOT NULL,
    [trigger_text] NVARCHAR(64) NOT NULL,
    [snippet_text] NVARCHAR(4000) NOT NULL,
    [snippet_ast] NVARCHAR(MAX) NOT NULL,
    [description] NVARCHAR(256) NULL,
    [is_active] BIT NOT NULL CONSTRAINT [DF_snippet_active] DEFAULT ((1)),
    [created_at] DATETIME2(3) NOT NULL CONSTRAINT [DF_snippet_created] DEFAULT (SYSUTCDATETIME()),
    [updated_at] DATETIME2(3) NOT NULL CONSTRAINT [DF_snippet_updated] DEFAULT (SYSUTCDATETIME()),
    [rev] BIGINT NOT NULL CONSTRAINT [DF_snippet_rev] DEFAULT ((1)),
    CONSTRAINT [PK_snippet] PRIMARY KEY CLUSTERED ([snippet_id] ASC),
    CONSTRAINT [UQ_snippet_account_trigger] UNIQUE NONCLUSTERED ([account_id], [trigger_text]),
    CONSTRAINT [FK_snippet_account] FOREIGN KEY ([account_id]) 
        REFERENCES [app].[account] ([account_id]) ON DELETE CASCADE,
    CONSTRAINT [CK_snippet_trigger_not_blank] CHECK (LEN(LTRIM(RTRIM([trigger_text]))) > 0),
    CONSTRAINT [CK_snippet_text_not_blank] CHECK (LEN(LTRIM(RTRIM([snippet_text]))) > 0),
    CONSTRAINT [CK_snippet_ast_not_blank] CHECK (LEN(LTRIM(RTRIM([snippet_ast]))) > 0)
);
GO

CREATE NONCLUSTERED INDEX [IX_snippet_account_active] 
    ON [radium].[snippet] ([account_id], [is_active]);
GO

CREATE NONCLUSTERED INDEX [IX_snippet_account_rev] 
    ON [radium].[snippet] ([account_id], [rev]);
GO

CREATE NONCLUSTERED INDEX [IX_snippet_account_trigger_active] 
    ON [radium].[snippet] ([account_id], [trigger_text], [is_active]);
GO

-- =============================================
-- TABLE: radium.user_setting
-- JSON-based user settings per account
-- =============================================

CREATE TABLE [radium].[user_setting] (
    [account_id] BIGINT NOT NULL,
    [settings_json] NVARCHAR(MAX) NOT NULL,
    [updated_at] DATETIME2(3) NOT NULL CONSTRAINT [DF_reportify_updated] DEFAULT (SYSUTCDATETIME()),
    [rev] BIGINT NOT NULL CONSTRAINT [DF_reportify_rev] DEFAULT ((1)),
    CONSTRAINT [PK__reportif__46A222CD16A9328A] PRIMARY KEY CLUSTERED ([account_id] ASC),
    CONSTRAINT [FK_reportify_account] FOREIGN KEY ([account_id]) 
        REFERENCES [app].[account] ([account_id]) ON DELETE CASCADE
);
GO

CREATE NONCLUSTERED INDEX [IX_reportify_updated_at] 
    ON [radium].[user_setting] ([updated_at] DESC);
GO

-- =============================================
-- TABLE: radium.exported_report
-- Tracks exported medical reports per account
-- =============================================

CREATE TABLE [radium].[exported_report] (
    [id] BIGINT IDENTITY(1,1) NOT NULL,
    [account_id] BIGINT NOT NULL,
    [report] NVARCHAR(MAX) NOT NULL,
    [report_datetime] DATETIME2(7) NOT NULL,
    [uploaded_at] DATETIME2(7) NOT NULL,
    [is_resolved] BIT NOT NULL CONSTRAINT [DF__exported___is_re__1B9317B3] DEFAULT ((0)),
    CONSTRAINT [PK_exported_report] PRIMARY KEY CLUSTERED ([id] ASC),
    CONSTRAINT [FK_exported_report_account] FOREIGN KEY ([account_id]) 
        REFERENCES [app].[account] ([account_id])
);
GO

CREATE NONCLUSTERED INDEX [IX_exported_report_account_id] 
    ON [radium].[exported_report] ([account_id]);
GO

CREATE NONCLUSTERED INDEX [IX_exported_report_report_datetime] 
    ON [radium].[exported_report] ([report_datetime]);
GO

-- =============================================
-- VIEW: radium.v_phrase_snomed_combined
-- Unified view of account-specific and global phrase-to-SNOMED mappings
-- =============================================

GO
CREATE VIEW [radium].[v_phrase_snomed_combined] AS
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
SELECT * FROM global_mappings;
GO

-- =============================================
-- VIEW: radium.v_snomed_phrase_stats
-- Statistics for SNOMED-imported phrases
-- =============================================

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
    JSON_VALUE(p.tags, '$.import_batch');
GO

-- =============================================
-- STORED PROCEDURE: snomed.upsert_concept
-- Inserts or updates a SNOMED concept in the cache
-- =============================================

GO
CREATE PROCEDURE [snomed].[upsert_concept]
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
END;
GO

-- =============================================
-- STORED PROCEDURE: radium.import_snomed_phrases
-- Bulk imports SNOMED phrases as global phrases
-- =============================================

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
END;
GO

-- =============================================
-- STORED PROCEDURE: radium.map_phrase_to_snomed
-- Maps an account-specific phrase to a SNOMED concept
-- =============================================

GO
CREATE PROCEDURE [radium].[map_phrase_to_snomed]
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
END;
GO

-- =============================================
-- STORED PROCEDURE: radium.map_global_phrase_to_snomed
-- Maps a global phrase to a SNOMED concept
-- =============================================

GO
CREATE PROCEDURE [radium].[map_global_phrase_to_snomed]
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
END;
GO

-- =============================================
-- END OF DATABASE STRUCTURE SCRIPT
-- =============================================