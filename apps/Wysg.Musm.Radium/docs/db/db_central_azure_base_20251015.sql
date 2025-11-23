-- =====================================================
-- Azure SQL Central Database - BASE SCHEMA (Idempotent)
-- Date: 2025-10-15
-- Version: 1.0 BASE
-- Database: Azure SQL (central_db)
-- =====================================================
-- 
-- This script creates the base schema for the central database.
-- It is fully idempotent and can be re-run safely without errors.
--
-- CONTAINS:
-- - app.account table
-- - radium.phrase table
-- - radium.hotkey table
-- - radium.snippet table
-- - radium.user_setting table
-- - All base indexes, triggers, constraints
--
-- DEPLOYMENT:
-- Run this script on your Azure SQL central database.
-- All operations use IF NOT EXISTS guards for safety.
--
-- NOTE: For SNOMED features, run db_central_azure_migration_20251019.sql
-- =====================================================

SET NOCOUNT ON;
GO

PRINT '=====================================================';
PRINT 'Azure SQL Central Database - BASE SCHEMA';
PRINT 'Deployment Starting';
PRINT 'Timestamp: ' + CONVERT(VARCHAR(30), SYSUTCDATETIME(), 120);
PRINT '=====================================================';
GO

-- =====================================================
-- SECTION 1: Create Schemas
-- =====================================================
PRINT '';
PRINT 'SECTION 1: Creating schemas...';
GO

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'app')
BEGIN
    EXEC('CREATE SCHEMA app');
    PRINT '  ? Created schema: app';
END
ELSE PRINT '  - Schema exists: app';
GO

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'radium')
BEGIN
    EXEC('CREATE SCHEMA radium');
    PRINT '  ? Created schema: radium';
END
ELSE PRINT '  - Schema exists: radium';
GO

-- =====================================================
-- SECTION 2: Create app.account Table
-- =====================================================
PRINT '';
PRINT 'SECTION 2: Creating app.account table...';
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE schema_id = SCHEMA_ID('app') AND name = 'account')
BEGIN
    CREATE TABLE [app].[account](
        [account_id] [bigint] IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [uid] [nvarchar](100) NOT NULL,
        [email] [nvarchar](320) NOT NULL,
        [display_name] [nvarchar](200) NULL,
        [is_active] [bit] NOT NULL DEFAULT 1,
        [created_at] [datetime2](3) NOT NULL DEFAULT SYSUTCDATETIME(),
        [last_login_at] [datetime2](3) NULL,
        CONSTRAINT [UQ_account_email] UNIQUE ([email]),
        CONSTRAINT [UQ_account_uid] UNIQUE ([uid])
    );
    PRINT '  ? Created table: app.account';
END
ELSE PRINT '  - Table exists: app.account';
GO

-- =====================================================
-- SECTION 3: Create radium.phrase Table
-- =====================================================
PRINT '';
PRINT 'SECTION 3: Creating radium.phrase table...';
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE schema_id = SCHEMA_ID('radium') AND name = 'phrase')
BEGIN
    CREATE TABLE [radium].[phrase](
        [id] [bigint] IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [account_id] [bigint] NULL,
        [text] [nvarchar](400) NOT NULL,
        [active] [bit] NOT NULL DEFAULT 1,
        [created_at] [datetime2](3) NOT NULL DEFAULT SYSUTCDATETIME(),
        [updated_at] [datetime2](3) NOT NULL DEFAULT SYSUTCDATETIME(),
        [rev] [bigint] NOT NULL DEFAULT 1,
        CONSTRAINT [FK_phrase_account] FOREIGN KEY([account_id])
            REFERENCES [app].[account] ([account_id]) ON DELETE CASCADE,
        CONSTRAINT [CK_phrase_text_not_blank] 
            CHECK (LEN(LTRIM(RTRIM([text]))) > 0)
    );
    PRINT '  ? Created table: radium.phrase';
END
ELSE PRINT '  - Table exists: radium.phrase';
GO

-- =====================================================
-- SECTION 4: Create radium.phrase Indexes
-- =====================================================
PRINT '';
PRINT 'SECTION 4: Creating radium.phrase indexes...';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_phrase_account_active' AND object_id = OBJECT_ID('radium.phrase'))
BEGIN
    CREATE INDEX IX_phrase_account_active ON radium.phrase(account_id, active);
    PRINT '  ? Created index: IX_phrase_account_active';
END
ELSE PRINT '  - Index exists: IX_phrase_account_active';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_phrase_account_rev' AND object_id = OBJECT_ID('radium.phrase'))
BEGIN
    CREATE INDEX IX_phrase_account_rev ON radium.phrase(account_id, rev);
    PRINT '  ? Created index: IX_phrase_account_rev';
END
ELSE PRINT '  - Index exists: IX_phrase_account_rev';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_phrase_account_text_unique' AND object_id = OBJECT_ID('radium.phrase'))
BEGIN
    CREATE UNIQUE INDEX IX_phrase_account_text_unique ON radium.phrase(account_id, [text])
    WHERE account_id IS NOT NULL;
    PRINT '  ? Created index: IX_phrase_account_text_unique';
END
ELSE PRINT '  - Index exists: IX_phrase_account_text_unique';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_phrase_global_active' AND object_id = OBJECT_ID('radium.phrase'))
BEGIN
    CREATE INDEX IX_phrase_global_active ON radium.phrase(active)
    WHERE account_id IS NULL;
    PRINT '  ? Created index: IX_phrase_global_active';
END
ELSE PRINT '  - Index exists: IX_phrase_global_active';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_phrase_global_text_unique' AND object_id = OBJECT_ID('radium.phrase'))
BEGIN
    CREATE UNIQUE INDEX IX_phrase_global_text_unique ON radium.phrase([text])
    WHERE account_id IS NULL;
    PRINT '  ? Created index: IX_phrase_global_text_unique';
END
ELSE PRINT '  - Index exists: IX_phrase_global_text_unique';
GO

-- =====================================================
-- SECTION 5: Create radium.phrase Trigger
-- =====================================================
PRINT '';
PRINT 'SECTION 5: Creating radium.phrase trigger...';
GO

IF OBJECT_ID('radium.trg_phrase_touch', 'TR') IS NOT NULL
BEGIN
    DROP TRIGGER radium.trg_phrase_touch;
    PRINT '  - Dropped existing trigger: trg_phrase_touch';
END
GO

CREATE TRIGGER [radium].[trg_phrase_touch] 
ON [radium].[phrase] 
AFTER UPDATE 
AS 
BEGIN 
    SET NOCOUNT ON;
    
    WITH Changed AS ( 
        SELECT i.id 
        FROM inserted i 
        JOIN deleted d ON i.id = d.id 
        WHERE (i.active <> d.active) OR (i.[text] <> d.[text]) 
    ) 
    UPDATE p 
    SET p.updated_at = SYSUTCDATETIME(), 
        p.rev = p.rev + 1 
    FROM radium.phrase p 
    INNER JOIN Changed c ON p.id = c.id;
END;
GO
PRINT '  ? Created trigger: trg_phrase_touch';
GO

-- =====================================================
-- SECTION 6: Create radium.hotkey Table
-- =====================================================
PRINT '';
PRINT 'SECTION 6: Creating radium.hotkey table...';
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE schema_id = SCHEMA_ID('radium') AND name = 'hotkey')
BEGIN
    CREATE TABLE [radium].[hotkey](
        [hotkey_id] [bigint] IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [account_id] [bigint] NOT NULL,
        [trigger_text] [nvarchar](64) NOT NULL,
        [expansion_text] [nvarchar](4000) NOT NULL,
        [description] [nvarchar](256) NULL,
        [is_active] [bit] NOT NULL DEFAULT 1,
        [created_at] [datetime2](3) NOT NULL DEFAULT SYSUTCDATETIME(),
        [updated_at] [datetime2](3) NOT NULL DEFAULT SYSUTCDATETIME(),
        [rev] [bigint] NOT NULL DEFAULT 1,
        CONSTRAINT [UQ_hotkey_account_trigger] UNIQUE ([account_id], [trigger_text]),
        CONSTRAINT [FK_hotkey_account] FOREIGN KEY([account_id])
            REFERENCES [app].[account] ([account_id]) ON DELETE CASCADE,
        CONSTRAINT [CK_hotkey_trigger_not_blank]
            CHECK (LEN(LTRIM(RTRIM([trigger_text]))) > 0),
        CONSTRAINT [CK_hotkey_expansion_not_blank]
            CHECK (LEN(LTRIM(RTRIM([expansion_text]))) > 0)
    );
    PRINT '  ? Created table: radium.hotkey';
END
ELSE PRINT '  - Table exists: radium.hotkey';
GO

-- =====================================================
-- SECTION 7: Create radium.hotkey Indexes
-- =====================================================
PRINT '';
PRINT 'SECTION 7: Creating radium.hotkey indexes...';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_hotkey_account_active' AND object_id = OBJECT_ID('radium.hotkey'))
BEGIN
    CREATE INDEX IX_hotkey_account_active ON radium.hotkey(account_id, is_active);
    PRINT '  ? Created index: IX_hotkey_account_active';
END
ELSE PRINT '  - Index exists: IX_hotkey_account_active';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_hotkey_account_rev' AND object_id = OBJECT_ID('radium.hotkey'))
BEGIN
    CREATE INDEX IX_hotkey_account_rev ON radium.hotkey(account_id, rev);
    PRINT '  ? Created index: IX_hotkey_account_rev';
END
ELSE PRINT '  - Index exists: IX_hotkey_account_rev';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_hotkey_account_trigger_active' AND object_id = OBJECT_ID('radium.hotkey'))
BEGIN
    CREATE INDEX IX_hotkey_account_trigger_active 
    ON radium.hotkey(account_id, trigger_text, is_active)
    INCLUDE (expansion_text, description);
    PRINT '  ? Created index: IX_hotkey_account_trigger_active';
END
ELSE PRINT '  - Index exists: IX_hotkey_account_trigger_active';
GO

-- =====================================================
-- SECTION 8: Create radium.hotkey Trigger
-- =====================================================
PRINT '';
PRINT 'SECTION 8: Creating radium.hotkey trigger...';
GO

IF OBJECT_ID('radium.trg_hotkey_touch', 'TR') IS NOT NULL
BEGIN
    DROP TRIGGER radium.trg_hotkey_touch;
    PRINT '  - Dropped existing trigger: trg_hotkey_touch';
END
GO

CREATE TRIGGER [radium].[trg_hotkey_touch] 
ON [radium].[hotkey]
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
PRINT '  ? Created trigger: trg_hotkey_touch';
GO

-- =====================================================
-- SECTION 9: Create radium.snippet Table
-- =====================================================
PRINT '';
PRINT 'SECTION 9: Creating radium.snippet table...';
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE schema_id = SCHEMA_ID('radium') AND name = 'snippet')
BEGIN
    CREATE TABLE [radium].[snippet](
        [snippet_id] [bigint] IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [account_id] [bigint] NOT NULL,
        [trigger_text] [nvarchar](64) NOT NULL,
        [snippet_text] [nvarchar](4000) NOT NULL,
        [snippet_ast] [nvarchar](max) NOT NULL,
        [description] [nvarchar](256) NULL,
        [is_active] [bit] NOT NULL DEFAULT 1,
        [created_at] [datetime2](3) NOT NULL DEFAULT SYSUTCDATETIME(),
        [updated_at] [datetime2](3) NOT NULL DEFAULT SYSUTCDATETIME(),
        [rev] [bigint] NOT NULL DEFAULT 1,
        CONSTRAINT [UQ_snippet_account_trigger] UNIQUE ([account_id], [trigger_text]),
        CONSTRAINT [FK_snippet_account] FOREIGN KEY([account_id])
            REFERENCES [app].[account] ([account_id]) ON DELETE CASCADE,
        CONSTRAINT [CK_snippet_trigger_not_blank]
            CHECK (LEN(LTRIM(RTRIM([trigger_text]))) > 0),
        CONSTRAINT [CK_snippet_text_not_blank]
            CHECK (LEN(LTRIM(RTRIM([snippet_text]))) > 0),
        CONSTRAINT [CK_snippet_ast_not_blank]
            CHECK (LEN(LTRIM(RTRIM([snippet_ast]))) > 0)
    );
    PRINT '  ? Created table: radium.snippet';
END
ELSE PRINT '  - Table exists: radium.snippet';
GO

-- =====================================================
-- SECTION 10: Create radium.snippet Indexes
-- =====================================================
PRINT '';
PRINT 'SECTION 10: Creating radium.snippet indexes...';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_snippet_account_active' AND object_id = OBJECT_ID('radium.snippet'))
BEGIN
    CREATE INDEX IX_snippet_account_active ON radium.snippet(account_id, is_active);
    PRINT '  ? Created index: IX_snippet_account_active';
END
ELSE PRINT '  - Index exists: IX_snippet_account_active';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_snippet_account_rev' AND object_id = OBJECT_ID('radium.snippet'))
BEGIN
    CREATE INDEX IX_snippet_account_rev ON radium.snippet(account_id, rev);
    PRINT '  ? Created index: IX_snippet_account_rev';
END
ELSE PRINT '  - Index exists: IX_snippet_account_rev';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_snippet_account_trigger_active' AND object_id = OBJECT_ID('radium.snippet'))
BEGIN
    CREATE INDEX IX_snippet_account_trigger_active 
    ON radium.snippet(account_id, trigger_text, is_active)
    INCLUDE (snippet_text, snippet_ast, description);
    PRINT '  ? Created index: IX_snippet_account_trigger_active';
END
ELSE PRINT '  - Index exists: IX_snippet_account_trigger_active';
GO

-- =====================================================
-- SECTION 11: Create radium.snippet Trigger
-- =====================================================
PRINT '';
PRINT 'SECTION 11: Creating radium.snippet trigger...';
GO

IF OBJECT_ID('radium.trg_snippet_touch', 'TR') IS NOT NULL
BEGIN
    DROP TRIGGER radium.trg_snippet_touch;
    PRINT '  - Dropped existing trigger: trg_snippet_touch';
END
GO

CREATE TRIGGER [radium].[trg_snippet_touch] 
ON [radium].[snippet]
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
PRINT '  ? Created trigger: trg_snippet_touch';
GO

-- =====================================================
-- SECTION 12: Create radium.user_setting Table
-- =====================================================
PRINT '';
PRINT 'SECTION 12: Creating radium.user_setting table...';
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE schema_id = SCHEMA_ID('radium') AND name = 'user_setting')
BEGIN
    CREATE TABLE [radium].[user_setting](
        [account_id] [bigint] NOT NULL PRIMARY KEY,
        [settings_json] [nvarchar](max) NOT NULL,
        [updated_at] [datetime2](3) NOT NULL DEFAULT SYSUTCDATETIME(),
        [rev] [bigint] NOT NULL DEFAULT 1,
        CONSTRAINT [FK_user_setting_account] FOREIGN KEY([account_id])
            REFERENCES [app].[account] ([account_id]) ON DELETE CASCADE
    );
    PRINT '  ? Created table: radium.user_setting';
END
ELSE PRINT '  - Table exists: radium.user_setting';
GO

-- =====================================================
-- SECTION 13: Create radium.user_setting Index
-- =====================================================
PRINT '';
PRINT 'SECTION 13: Creating radium.user_setting index...';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_user_setting_updated_at' AND object_id = OBJECT_ID('radium.user_setting'))
BEGIN
    CREATE INDEX IX_user_setting_updated_at ON radium.user_setting(updated_at DESC);
    PRINT '  ? Created index: IX_user_setting_updated_at';
END
ELSE PRINT '  - Index exists: IX_user_setting_updated_at';
GO

-- =====================================================
-- SECTION 14: Verification
-- =====================================================
PRINT '';
PRINT 'SECTION 14: Verifying base schema...';
GO

DECLARE @table_count INT, @index_count INT, @trigger_count INT;

SELECT @table_count = COUNT(*) 
FROM sys.tables 
WHERE schema_id IN (SCHEMA_ID('app'), SCHEMA_ID('radium'))
  AND name IN ('account', 'phrase', 'hotkey', 'snippet', 'user_setting');

SELECT @index_count = COUNT(*) 
FROM sys.indexes 
WHERE object_id IN (
    OBJECT_ID('radium.phrase'),
    OBJECT_ID('radium.hotkey'),
    OBJECT_ID('radium.snippet'),
    OBJECT_ID('radium.user_setting')
)
AND name LIKE 'IX_%';

SELECT @trigger_count = COUNT(*) 
FROM sys.triggers 
WHERE name IN ('trg_phrase_touch', 'trg_hotkey_touch', 'trg_snippet_touch');

PRINT '  ? Tables created/verified: ' + CAST(@table_count AS VARCHAR(10)) + '/5';
PRINT '  ? Indexes created/verified: ' + CAST(@index_count AS VARCHAR(10));
PRINT '  ? Triggers created/verified: ' + CAST(@trigger_count AS VARCHAR(10)) + '/3';
GO

-- =====================================================
-- SECTION 15: Completion
-- =====================================================
PRINT '';
PRINT '=====================================================';
PRINT 'Azure SQL Central Database - BASE SCHEMA COMPLETE';
PRINT 'Timestamp: ' + CONVERT(VARCHAR(30), SYSUTCDATETIME(), 120);
PRINT '=====================================================';
PRINT '';
PRINT 'Next steps:';
PRINT '1. Verify all tables exist (see verification above)';
PRINT '2. Run db_central_azure_migration_20251019.sql for SNOMED support';
PRINT '';
GO
