SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [app].[account](
	[account_id] [bigint] IDENTITY(1,1) NOT NULL,
	[uid] [nvarchar](100) NOT NULL,
	[email] [nvarchar](320) NOT NULL,
	[display_name] [nvarchar](200) NULL,
	[is_active] [bit] NOT NULL,
	[created_at] [datetime2](3) NOT NULL,
	[last_login_at] [datetime2](3) NULL
) ON [PRIMARY]
GO
ALTER TABLE [app].[account] ADD PRIMARY KEY CLUSTERED 
(
	[account_id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
ALTER TABLE [app].[account] ADD  CONSTRAINT [UQ_account_email] UNIQUE NONCLUSTERED 
(
	[email] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
ALTER TABLE [app].[account] ADD  CONSTRAINT [UQ_account_uid] UNIQUE NONCLUSTERED 
(
	[uid] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
ALTER TABLE [app].[account] ADD  CONSTRAINT [DF_account_is_active]  DEFAULT ((1)) FOR [is_active]
GO
ALTER TABLE [app].[account] ADD  CONSTRAINT [DF_account_created_at]  DEFAULT (sysutcdatetime()) FOR [created_at]
GO






SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [radium].[phrase](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[account_id] [bigint] NULL,
	[text] [nvarchar](400) NOT NULL,
	[active] [bit] NOT NULL,
	[created_at] [datetime2](3) NOT NULL,
	[updated_at] [datetime2](3) NOT NULL,
	[rev] [bigint] NOT NULL
) ON [PRIMARY]
GO
ALTER TABLE [radium].[phrase] ADD PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
CREATE NONCLUSTERED INDEX [IX_phrase_account_active] ON [radium].[phrase]
(
	[account_id] ASC,
	[active] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
CREATE NONCLUSTERED INDEX [IX_phrase_account_rev] ON [radium].[phrase]
(
	[account_id] ASC,
	[rev] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_phrase_account_text_unique] ON [radium].[phrase]
(
	[account_id] ASC,
	[text] ASC
)
WHERE ([account_id] IS NOT NULL)
WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
CREATE NONCLUSTERED INDEX [IX_phrase_global_active] ON [radium].[phrase]
(
	[active] ASC
)
WHERE ([account_id] IS NULL)
WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_phrase_global_text_unique] ON [radium].[phrase]
(
	[text] ASC
)
WHERE ([account_id] IS NULL)
WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
ALTER TABLE [radium].[phrase] ADD  CONSTRAINT [DF_phrase_active]  DEFAULT ((1)) FOR [active]
GO
ALTER TABLE [radium].[phrase] ADD  CONSTRAINT [DF_phrase_created]  DEFAULT (sysutcdatetime()) FOR [created_at]
GO
ALTER TABLE [radium].[phrase] ADD  CONSTRAINT [DF_phrase_updated]  DEFAULT (sysutcdatetime()) FOR [updated_at]
GO
ALTER TABLE [radium].[phrase] ADD  CONSTRAINT [DF_phrase_rev]  DEFAULT ((1)) FOR [rev]
GO
ALTER TABLE [radium].[phrase]  WITH CHECK ADD  CONSTRAINT [FK_phrase_account] FOREIGN KEY([account_id])
REFERENCES [app].[account] ([account_id])
ON DELETE CASCADE
GO
ALTER TABLE [radium].[phrase] CHECK CONSTRAINT [FK_phrase_account]
GO
ALTER TABLE [radium].[phrase]  WITH CHECK ADD  CONSTRAINT [CK_phrase_global_or_account_unique] CHECK  (([account_id] IS NULL OR [account_id] IS NOT NULL))
GO
ALTER TABLE [radium].[phrase] CHECK CONSTRAINT [CK_phrase_global_or_account_unique]
GO
ALTER TABLE [radium].[phrase]  WITH CHECK ADD  CONSTRAINT [CK_phrase_text_not_blank] CHECK  ((len(ltrim(rtrim([text])))>(0)))
GO
ALTER TABLE [radium].[phrase] CHECK CONSTRAINT [CK_phrase_text_not_blank]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TRIGGER [radium].[trg_phrase_touch] ON [radium].[phrase] AFTER UPDATE AS BEGIN SET NOCOUNT ON; ;WITH Changed AS ( SELECT i.id FROM inserted i JOIN deleted d ON i.id = d.id WHERE ( (i.active <> d.active) OR (i.[text] <> d.[text]) ) ) UPDATE p SET p.updated_at = SYSUTCDATETIME(), p.rev = p.rev + 1 FROM radium.phrase p INNER JOIN Changed c ON p.id = c.id; END; 
GO
ALTER TABLE [radium].[phrase] ENABLE TRIGGER [trg_phrase_touch]
GO






SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [radium].[reportify_setting](
	[account_id] [bigint] NOT NULL,
	[settings_json] [nvarchar](max) NOT NULL,
	[updated_at] [datetime2](3) NOT NULL,
	[rev] [bigint] NOT NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
ALTER TABLE [radium].[reportify_setting] ADD PRIMARY KEY CLUSTERED 
(
	[account_id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
CREATE NONCLUSTERED INDEX [IX_reportify_updated_at] ON [radium].[reportify_setting]
(
	[updated_at] DESC
)WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
ALTER TABLE [radium].[reportify_setting] ADD  CONSTRAINT [DF_reportify_updated]  DEFAULT (sysutcdatetime()) FOR [updated_at]
GO
ALTER TABLE [radium].[reportify_setting] ADD  CONSTRAINT [DF_reportify_rev]  DEFAULT ((1)) FOR [rev]
GO
ALTER TABLE [radium].[reportify_setting]  WITH CHECK ADD  CONSTRAINT [FK_reportify_account] FOREIGN KEY([account_id])
REFERENCES [app].[account] ([account_id])
ON DELETE CASCADE
GO
ALTER TABLE [radium].[reportify_setting] CHECK CONSTRAINT [FK_reportify_account]
GO




SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [radium].[hotkey](
	[hotkey_id] [bigint] IDENTITY(1,1) NOT NULL,
	[account_id] [bigint] NOT NULL,
	[trigger_text] [nvarchar](64) NOT NULL,
	[expansion_text] [nvarchar](4000) NOT NULL,
	[description] [nvarchar](256) NULL,
	[is_active] [bit] NOT NULL,
	[created_at] [datetime2](3) NOT NULL,
	[updated_at] [datetime2](3) NOT NULL,
	[rev] [bigint] NOT NULL
) ON [PRIMARY]
GO
ALTER TABLE [radium].[hotkey] ADD  CONSTRAINT [PK_hotkey] PRIMARY KEY CLUSTERED 
(
	[hotkey_id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
ALTER TABLE [radium].[hotkey] ADD  CONSTRAINT [UQ_hotkey_account_trigger] UNIQUE NONCLUSTERED 
(
	[account_id] ASC,
	[trigger_text] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
CREATE NONCLUSTERED INDEX [IX_hotkey_account_active] ON [radium].[hotkey]
(
	[account_id] ASC,
	[is_active] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
CREATE NONCLUSTERED INDEX [IX_hotkey_account_rev] ON [radium].[hotkey]
(
	[account_id] ASC,
	[rev] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
CREATE NONCLUSTERED INDEX [IX_hotkey_account_trigger_active] ON [radium].[hotkey]
(
	[account_id] ASC,
	[trigger_text] ASC,
	[is_active] ASC
)
INCLUDE([expansion_text],[description]) WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
ALTER TABLE [radium].[hotkey] ADD  CONSTRAINT [DF_hotkey_active]  DEFAULT ((1)) FOR [is_active]
GO
ALTER TABLE [radium].[hotkey] ADD  CONSTRAINT [DF_hotkey_created]  DEFAULT (sysutcdatetime()) FOR [created_at]
GO
ALTER TABLE [radium].[hotkey] ADD  CONSTRAINT [DF_hotkey_updated]  DEFAULT (sysutcdatetime()) FOR [updated_at]
GO
ALTER TABLE [radium].[hotkey] ADD  CONSTRAINT [DF_hotkey_rev]  DEFAULT ((1)) FOR [rev]
GO
ALTER TABLE [radium].[hotkey]  WITH CHECK ADD  CONSTRAINT [FK_hotkey_account] FOREIGN KEY([account_id])
REFERENCES [app].[account] ([account_id])
ON DELETE CASCADE
GO
ALTER TABLE [radium].[hotkey] CHECK CONSTRAINT [FK_hotkey_account]
GO
ALTER TABLE [radium].[hotkey]  WITH CHECK ADD  CONSTRAINT [CK_hotkey_expansion_not_blank] CHECK  ((len(ltrim(rtrim([expansion_text])))>(0)))
GO
ALTER TABLE [radium].[hotkey] CHECK CONSTRAINT [CK_hotkey_expansion_not_blank]
GO
ALTER TABLE [radium].[hotkey]  WITH CHECK ADD  CONSTRAINT [CK_hotkey_trigger_not_blank] CHECK  ((len(ltrim(rtrim([trigger_text])))>(0)))
GO
ALTER TABLE [radium].[hotkey] CHECK CONSTRAINT [CK_hotkey_trigger_not_blank]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Trigger to auto-update updated_at and rev on meaningful changes
CREATE TRIGGER [radium].[trg_hotkey_touch] ON [radium].[hotkey]
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    WITH Changed AS (
        SELECT i.hotkey_id
        FROM inserted i
        JOIN deleted d ON i.hotkey_id = d.hotkey_id
        WHERE (i.is_active <> d.is_active) 
           OR (i.trigger_text <> d.trigger_text)
           OR (i.expansion_text <> d.expansion_text)
           OR (ISNULL(i.description,N'') <> ISNULL(d.description,N''))
    )
    UPDATE h
    SET h.updated_at = SYSUTCDATETIME(),
        h.rev = h.rev + 1
    FROM radium.hotkey h
    INNER JOIN Changed c ON h.hotkey_id = c.hotkey_id;
END;
GO
ALTER TABLE [radium].[hotkey] ENABLE TRIGGER [trg_hotkey_touch]
GO


SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [radium].[snippet](
	[snippet_id] [bigint] IDENTITY(1,1) NOT NULL,
	[account_id] [bigint] NOT NULL,
	[trigger_text] [nvarchar](64) NOT NULL,
	[snippet_text] [nvarchar](4000) NOT NULL,
	[snippet_ast] [nvarchar](max) NOT NULL,
	[description] [nvarchar](256) NULL,
	[is_active] [bit] NOT NULL,
	[created_at] [datetime2](3) NOT NULL,
	[updated_at] [datetime2](3) NOT NULL,
	[rev] [bigint] NOT NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
ALTER TABLE [radium].[snippet] ADD  CONSTRAINT [PK_snippet] PRIMARY KEY CLUSTERED 
(
	[snippet_id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
ALTER TABLE [radium].[snippet] ADD  CONSTRAINT [UQ_snippet_account_trigger] UNIQUE NONCLUSTERED 
(
	[account_id] ASC,
	[trigger_text] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
CREATE NONCLUSTERED INDEX [IX_snippet_account_active] ON [radium].[snippet]
(
	[account_id] ASC,
	[is_active] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
CREATE NONCLUSTERED INDEX [IX_snippet_account_rev] ON [radium].[snippet]
(
	[account_id] ASC,
	[rev] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
CREATE NONCLUSTERED INDEX [IX_snippet_account_trigger_active] ON [radium].[snippet]
(
	[account_id] ASC,
	[trigger_text] ASC,
	[is_active] ASC
)
INCLUDE([snippet_text],[snippet_ast],[description]) WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
ALTER TABLE [radium].[snippet] ADD  CONSTRAINT [DF_snippet_active]  DEFAULT ((1)) FOR [is_active]
GO
ALTER TABLE [radium].[snippet] ADD  CONSTRAINT [DF_snippet_created]  DEFAULT (sysutcdatetime()) FOR [created_at]
GO
ALTER TABLE [radium].[snippet] ADD  CONSTRAINT [DF_snippet_updated]  DEFAULT (sysutcdatetime()) FOR [updated_at]
GO
ALTER TABLE [radium].[snippet] ADD  CONSTRAINT [DF_snippet_rev]  DEFAULT ((1)) FOR [rev]
GO
ALTER TABLE [radium].[snippet]  WITH CHECK ADD  CONSTRAINT [FK_snippet_account] FOREIGN KEY([account_id])
REFERENCES [app].[account] ([account_id])
ON DELETE CASCADE
GO
ALTER TABLE [radium].[snippet] CHECK CONSTRAINT [FK_snippet_account]
GO
ALTER TABLE [radium].[snippet]  WITH CHECK ADD  CONSTRAINT [CK_snippet_trigger_not_blank] CHECK  ((len(ltrim(rtrim([trigger_text])))>(0)))
GO
ALTER TABLE [radium].[snippet] CHECK CONSTRAINT [CK_snippet_trigger_not_blank]
GO
ALTER TABLE [radium].[snippet]  WITH CHECK ADD  CONSTRAINT [CK_snippet_text_not_blank] CHECK  ((len(ltrim(rtrim([snippet_text])))>(0)))
GO
ALTER TABLE [radium].[snippet] CHECK CONSTRAINT [CK_snippet_text_not_blank]
GO
ALTER TABLE [radium].[snippet]  WITH CHECK ADD  CONSTRAINT [CK_snippet_ast_not_blank] CHECK  ((len(ltrim(rtrim([snippet_ast])))>(0)))
GO
ALTER TABLE [radium].[snippet] CHECK CONSTRAINT [CK_snippet_ast_not_blank]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Trigger to auto-update updated_at and rev on meaningful changes
CREATE TRIGGER [radium].[trg_snippet_touch] ON [radium].[snippet]
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    WITH Changed AS (
        SELECT i.snippet_id
        FROM inserted i
        JOIN deleted d ON i.snippet_id = d.snippet_id
        WHERE (i.is_active <> d.is_active) 
           OR (i.trigger_text <> d.trigger_text)
           OR (i.snippet_text <> d.snippet_text)
           OR (i.snippet_ast <> d.snippet_ast)
           OR (ISNULL(i.description,N'') <> ISNULL(d.description,N''))
    )
    UPDATE s
    SET s.updated_at = SYSUTCDATETIME(),
        s.rev = s.rev + 1
    FROM radium.snippet s
    INNER JOIN Changed c ON s.snippet_id = c.snippet_id;
END;
GO
ALTER TABLE [radium].[snippet] ENABLE TRIGGER [trg_snippet_touch]
GO
-- =====================================================
-- Phrase-to-SNOMED Mapping Tables for Central Database
-- Date: 2025-01-15
-- Purpose: Support mapping global and account-specific phrases to SNOMED CT concepts via Snowstorm
-- =====================================================

-- Schema: snomed - for SNOMED CT concept cache from Snowstorm
-- Schema: radium - for phrase mappings (matches existing radium.phrase, radium.hotkey, radium.snippet)

-- =====================================================
-- 1. SNOMED Concept Cache (from Snowstorm API)
-- =====================================================

-- Create snomed schema if not exists
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'snomed')
BEGIN
    EXEC('CREATE SCHEMA snomed')
END
GO

-- Cache SNOMED CT concepts retrieved from Snowstorm
-- Reduces API calls and improves performance
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE schema_id = SCHEMA_ID('snomed') AND name = 'concept_cache')
BEGIN
    CREATE TABLE snomed.concept_cache (
        concept_id BIGINT NOT NULL PRIMARY KEY,                    -- SNOMED CT Concept ID (e.g., 22298006)
        concept_id_str VARCHAR(18) NOT NULL UNIQUE,                -- String representation for compatibility
        fsn NVARCHAR(500) NOT NULL,                                -- Fully Specified Name
        pt NVARCHAR(500) NULL,                                     -- Preferred Term (may differ from FSN)
        module_id VARCHAR(18) NULL,                                -- SNOMED CT Module ID
        active BIT NOT NULL DEFAULT 1,                             -- Active status from Snowstorm
        cached_at DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),  -- When cached from Snowstorm
        expires_at DATETIME2(3) NULL                               -- Optional expiration for cache invalidation
    )
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_snomed_concept_cache_fsn' AND object_id = OBJECT_ID('snomed.concept_cache'))
    CREATE INDEX IX_snomed_concept_cache_fsn ON snomed.concept_cache(fsn)
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_snomed_concept_cache_pt' AND object_id = OBJECT_ID('snomed.concept_cache'))
    CREATE INDEX IX_snomed_concept_cache_pt ON snomed.concept_cache(pt)
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_snomed_concept_cache_cached_at' AND object_id = OBJECT_ID('snomed.concept_cache'))
    CREATE INDEX IX_snomed_concept_cache_cached_at ON snomed.concept_cache(cached_at DESC)
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.extended_properties 
    WHERE major_id = OBJECT_ID('snomed.concept_cache') 
    AND name = N'MS_Description'
    AND minor_id = 0
)
    EXEC sp_addextendedproperty 
        @name = N'MS_Description', 
        @value = N'Cache of SNOMED CT concepts from Snowstorm to reduce API calls and improve performance', 
        @level0type = N'SCHEMA', @level0name = 'snomed',
        @level1type = N'TABLE', @level1name = 'concept_cache'
GO

-- =====================================================
-- 2. Global Phrase to SNOMED Mapping
-- =====================================================

-- Map global phrases (account_id IS NULL) to SNOMED concepts
-- Global phrases are available to all accounts
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE schema_id = SCHEMA_ID('radium') AND name = 'global_phrase_snomed')
BEGIN
    CREATE TABLE radium.global_phrase_snomed (
        id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        phrase_id BIGINT NOT NULL UNIQUE,                          -- FK to radium.phrase WHERE account_id IS NULL
        concept_id BIGINT NOT NULL,                                -- FK to snomed.concept_cache
        mapping_type VARCHAR(20) NOT NULL DEFAULT 'exact',         -- 'exact', 'broader', 'narrower', 'related'
        confidence DECIMAL(3,2) NULL,                              -- Optional confidence score (0.00-1.00)
        notes NVARCHAR(500) NULL,                                  -- Optional mapping notes
        mapped_by BIGINT NULL,                                     -- Optional: account_id of user who created mapping
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
    )
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_global_phrase_snomed_concept' AND object_id = OBJECT_ID('radium.global_phrase_snomed'))
    CREATE INDEX IX_global_phrase_snomed_concept ON radium.global_phrase_snomed(concept_id)
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_global_phrase_snomed_mapping_type' AND object_id = OBJECT_ID('radium.global_phrase_snomed'))
    CREATE INDEX IX_global_phrase_snomed_mapping_type ON radium.global_phrase_snomed(mapping_type)
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_global_phrase_snomed_mapped_by' AND object_id = OBJECT_ID('radium.global_phrase_snomed'))
    CREATE INDEX IX_global_phrase_snomed_mapped_by ON radium.global_phrase_snomed(mapped_by)
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.extended_properties 
    WHERE major_id = OBJECT_ID('radium.global_phrase_snomed') 
    AND name = N'MS_Description'
    AND minor_id = 0
)
    EXEC sp_addextendedproperty 
        @name = N'MS_Description', 
        @value = N'Maps global phrases (account_id IS NULL) to SNOMED CT concepts. One phrase can map to one concept. Global mappings available to all accounts.', 
        @level0type = N'SCHEMA', @level0name = 'radium',
        @level1type = N'TABLE', @level1name = 'global_phrase_snomed'
GO

-- =====================================================
-- 3. Account-Specific Phrase to SNOMED Mapping
-- =====================================================

-- Map account-specific phrases to SNOMED concepts
-- Allows per-account customization while global mappings provide defaults
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE schema_id = SCHEMA_ID('radium') AND name = 'phrase_snomed')
BEGIN
    CREATE TABLE radium.phrase_snomed (
        id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        phrase_id BIGINT NOT NULL,                                 -- FK to radium.phrase WHERE account_id IS NOT NULL
        concept_id BIGINT NOT NULL,                                -- FK to snomed.concept_cache
        mapping_type VARCHAR(20) NOT NULL DEFAULT 'exact',         -- 'exact', 'broader', 'narrower', 'related'
        confidence DECIMAL(3,2) NULL,                              -- Optional confidence score (0.00-1.00)
        notes NVARCHAR(500) NULL,                                  -- Optional mapping notes
        created_at DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
        updated_at DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
        
        CONSTRAINT UQ_phrase_snomed_phrase UNIQUE (phrase_id),     -- One phrase ¡æ one concept
        
        CONSTRAINT FK_phrase_snomed_phrase FOREIGN KEY (phrase_id) 
            REFERENCES radium.phrase(id) ON DELETE CASCADE,
        
        CONSTRAINT FK_phrase_snomed_concept FOREIGN KEY (concept_id) 
            REFERENCES snomed.concept_cache(concept_id) ON DELETE NO ACTION,
        
        CONSTRAINT CK_phrase_snomed_mapping_type 
            CHECK (mapping_type IN ('exact', 'broader', 'narrower', 'related')),
        
        CONSTRAINT CK_phrase_snomed_confidence 
            CHECK (confidence IS NULL OR (confidence >= 0.00 AND confidence <= 1.00))
    )
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_phrase_snomed_concept' AND object_id = OBJECT_ID('radium.phrase_snomed'))
    CREATE INDEX IX_phrase_snomed_concept ON radium.phrase_snomed(concept_id)
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_phrase_snomed_mapping_type' AND object_id = OBJECT_ID('radium.phrase_snomed'))
    CREATE INDEX IX_phrase_snomed_mapping_type ON radium.phrase_snomed(mapping_type)
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_phrase_snomed_created' AND object_id = OBJECT_ID('radium.phrase_snomed'))
    CREATE INDEX IX_phrase_snomed_created ON radium.phrase_snomed(created_at DESC)
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.extended_properties 
    WHERE major_id = OBJECT_ID('radium.phrase_snomed') 
    AND name = N'MS_Description'
    AND minor_id = 0
)
    EXEC sp_addextendedproperty 
        @name = N'MS_Description', 
        @value = N'Maps account-specific phrases to SNOMED CT concepts. One phrase can map to one concept. Account-level mappings override global mappings.', 
        @level0type = N'SCHEMA', @level0name = 'radium',
        @level1type = N'TABLE', @level1name = 'phrase_snomed'
GO

-- =====================================================
-- 4. Triggers for updated_at
-- =====================================================

-- Trigger for global_phrase_snomed
IF OBJECT_ID('radium.trg_global_phrase_snomed_touch', 'TR') IS NOT NULL
    DROP TRIGGER radium.trg_global_phrase_snomed_touch
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

-- Trigger for phrase_snomed
IF OBJECT_ID('radium.trg_phrase_snomed_touch', 'TR') IS NOT NULL
    DROP TRIGGER radium.trg_phrase_snomed_touch
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

-- =====================================================
-- 5. Views for Unified Phrase-SNOMED Lookup
-- =====================================================

-- View: Combined phrase-SNOMED mappings (global + account-specific)
-- Account-specific mappings take precedence over global
IF OBJECT_ID('radium.v_phrase_snomed_combined', 'V') IS NOT NULL
    DROP VIEW radium.v_phrase_snomed_combined
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

IF NOT EXISTS (
    SELECT 1 FROM sys.extended_properties 
    WHERE major_id = OBJECT_ID('radium.v_phrase_snomed_combined') 
    AND name = N'MS_Description'
    AND minor_id = 0
)
    EXEC sp_addextendedproperty 
        @name = N'MS_Description', 
        @value = N'Unified view of phrase-to-SNOMED mappings combining global and account-specific mappings. Use for lookups and reporting.', 
        @level0type = N'SCHEMA', @level0name = 'radium',
        @level1type = N'VIEW', @level1name = 'v_phrase_snomed_combined'
GO

-- =====================================================
-- 6. Stored Procedures for Common Operations
-- =====================================================

-- Upsert SNOMED concept from Snowstorm API result
IF OBJECT_ID('snomed.upsert_concept', 'P') IS NOT NULL
    DROP PROCEDURE snomed.upsert_concept
GO

CREATE PROCEDURE snomed.upsert_concept
    @concept_id BIGINT,
    @concept_id_str VARCHAR(18),
    @fsn NVARCHAR(500),
    @pt NVARCHAR(500) = NULL,
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
            module_id = @module_id,
            active = @active,
            cached_at = SYSUTCDATETIME(),
            expires_at = @expires_at
    WHEN NOT MATCHED THEN
        INSERT (concept_id, concept_id_str, fsn, pt, module_id, active, cached_at, expires_at)
        VALUES (@concept_id, @concept_id_str, @fsn, @pt, @module_id, @active, SYSUTCDATETIME(), @expires_at);
END
GO

-- Map global phrase to SNOMED concept
IF OBJECT_ID('radium.map_global_phrase_to_snomed', 'P') IS NOT NULL
    DROP PROCEDURE radium.map_global_phrase_to_snomed
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
    
    -- Verify phrase is global (account_id IS NULL)
    IF NOT EXISTS (SELECT 1 FROM radium.phrase WHERE id = @phrase_id AND account_id IS NULL)
    BEGIN
        RAISERROR('Phrase must be global (account_id IS NULL)', 16, 1)
        RETURN
    END
    
    -- Verify concept exists in cache
    IF NOT EXISTS (SELECT 1 FROM snomed.concept_cache WHERE concept_id = @concept_id)
    BEGIN
        RAISERROR('SNOMED concept not found in cache. Please cache concept first.', 16, 1)
        RETURN
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

-- Map account phrase to SNOMED concept
IF OBJECT_ID('radium.map_phrase_to_snomed', 'P') IS NOT NULL
    DROP PROCEDURE radium.map_phrase_to_snomed
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
    
    -- Verify phrase is account-specific (account_id IS NOT NULL)
    IF NOT EXISTS (SELECT 1 FROM radium.phrase WHERE id = @phrase_id AND account_id IS NOT NULL)
    BEGIN
        RAISERROR('Phrase must be account-specific (account_id IS NOT NULL)', 16, 1)
        RETURN
    END
    
    -- Verify concept exists in cache
    IF NOT EXISTS (SELECT 1 FROM snomed.concept_cache WHERE concept_id = @concept_id)
    BEGIN
        RAISERROR('SNOMED concept not found in cache. Please cache concept first.', 16, 1)
        RETURN
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

-- =====================================================
-- 7. Sample Seed Data (Optional)
-- =====================================================

-- Example: Cache some common concepts
-- EXEC snomed.upsert_concept 
--     @concept_id = 22298006, 
--     @concept_id_str = '22298006',
--     @fsn = 'Myocardial infarction (disorder)',
--     @pt = 'Myocardial infarction'

-- =====================================================
-- End of Schema
-- =====================================================




-- Add this at the end of your central_db(azure)_20251015.sql file
-- (after the existing SNOMED mapping tables section)

-- =====================================================
-- SNOMED Phrase Import Enhancements (2025-10-19)
-- =====================================================

-- Add tags column and computed columns
ALTER TABLE [radium].[phrase] ADD [tags] NVARCHAR(MAX) NULL;
ALTER TABLE [radium].[phrase] ADD CONSTRAINT [CK_phrase_tags_valid_json] 
    CHECK ([tags] IS NULL OR ISJSON([tags]) = 1);

ALTER TABLE [radium].[phrase] 
    ADD [tags_source] AS CAST(JSON_VALUE([tags], '$.source') AS NVARCHAR(50)) PERSISTED;

ALTER TABLE [radium].[phrase] 
    ADD [tags_semantic_tag] AS CAST(JSON_VALUE([tags], '$.semantic_tag') AS NVARCHAR(100)) PERSISTED;

-- Add semantic_tag to concept_cache
ALTER TABLE [snomed].[concept_cache] ADD [semantic_tag] NVARCHAR(100) NULL;

-- Add indexes
CREATE NONCLUSTERED INDEX [IX_phrase_tags_source] 
ON [radium].[phrase]([tags_source])
INCLUDE ([id], [text], [active])
WHERE [tags] IS NOT NULL;

CREATE NONCLUSTERED INDEX [IX_phrase_tags_semantic_tag] 
ON [radium].[phrase]([tags_semantic_tag])
INCLUDE ([id], [text], [active], [tags_source])
WHERE [tags] IS NOT NULL;

CREATE INDEX [IX_snomed_concept_cache_semantic_tag] 
ON [snomed].[concept_cache]([semantic_tag]) 
WHERE [semantic_tag] IS NOT NULL;

-- Update global phrase index
DROP INDEX [IX_phrase_global_text] ON [radium].[phrase];
CREATE NONCLUSTERED INDEX [IX_phrase_global_text] 
ON [radium].[phrase]([text])
INCLUDE ([id], [active], [tags], [tags_source], [tags_semantic_tag])
WHERE [account_id] IS NULL
WITH (ONLINE = ON);

-- Create statistics view
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

-- Create import procedure
-- (Use the corrected procedure from previous message)

-- ===================================================
-- 6. Bulk SNOMED Import Procedure
-- ===================================================
IF OBJECT_ID('radium.import_snomed_phrases', 'P') IS NOT NULL
    DROP PROCEDURE radium.import_snomed_phrases;
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
    
    -- Build JSON for each concept and insert
    INSERT INTO radium.phrase (account_id, [text], active, tags)
    SELECT TOP (@max_count)
        NULL,  -- Global phrase
        cc.pt,
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
          WHERE p.text = cc.pt 
            AND p.account_id IS NULL
      )
    ORDER BY LEN(cc.pt), cc.pt;
    
    DECLARE @imported INT = @@ROWCOUNT;
    
    -- Auto-create mappings for imported phrases
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

PRINT 'SNOMED import procedure created successfully';
GO

