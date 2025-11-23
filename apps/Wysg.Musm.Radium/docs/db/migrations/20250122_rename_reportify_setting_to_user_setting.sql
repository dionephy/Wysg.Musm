-- =====================================================
-- Migration: Rename reportify_setting to user_setting
-- Date: 2025-01-22
-- Purpose: Rename table to more generic name for user settings
-- =====================================================
-- This migration script renames the radium.reportify_setting table
-- to radium.user_setting to better reflect its purpose as a
-- general user settings storage (not just reportify settings).
--
-- DEPLOYMENT:
-- Run this on your Azure SQL database BEFORE deploying code changes.
-- The script is idempotent and safe to re-run.
-- =====================================================

SET NOCOUNT ON;
GO

PRINT '=====================================================';
PRINT 'Migration: Rename reportify_setting to user_setting';
PRINT 'Started: ' + CONVERT(VARCHAR(30), SYSUTCDATETIME(), 120);
PRINT '=====================================================';
GO

-- =====================================================
-- STEP 1: Check if old table exists
-- =====================================================
IF EXISTS (SELECT 1 FROM sys.tables WHERE schema_id = SCHEMA_ID('radium') AND name = 'reportify_setting')
BEGIN
    PRINT 'Step 1: Old table radium.reportify_setting found';
    
    -- =====================================================
    -- STEP 2: Check if new table already exists
    -- =====================================================
    IF EXISTS (SELECT 1 FROM sys.tables WHERE schema_id = SCHEMA_ID('radium') AND name = 'user_setting')
    BEGIN
        PRINT 'WARNING: New table radium.user_setting already exists!';
        PRINT 'Skipping rename. Please verify data manually.';
    END
    ELSE
    BEGIN
        PRINT 'Step 2: New table does not exist, proceeding with rename';
        
        -- =====================================================
        -- STEP 3: Drop old index (will be recreated on new table)
        -- =====================================================
        IF EXISTS (SELECT 1 FROM sys.indexes 
                   WHERE name = 'IX_reportify_updated_at' 
                   AND object_id = OBJECT_ID('radium.reportify_setting'))
        BEGIN
            DROP INDEX [IX_reportify_updated_at] ON [radium].[reportify_setting];
            PRINT 'Step 3: Dropped old index IX_reportify_updated_at';
        END
        ELSE
        BEGIN
            PRINT 'Step 3: Old index not found (already dropped or never created)';
        END
        
        -- =====================================================
        -- STEP 4: Rename the table
        -- =====================================================
        EXEC sp_rename 'radium.reportify_setting', 'user_setting';
        PRINT 'Step 4: Renamed table to radium.user_setting';
        
        -- =====================================================
        -- STEP 5: Rename foreign key constraint
        -- =====================================================
        IF EXISTS (SELECT 1 FROM sys.foreign_keys 
                   WHERE name = 'FK_reportify_account' 
                   AND parent_object_id = OBJECT_ID('radium.user_setting'))
        BEGIN
            EXEC sp_rename 'radium.FK_reportify_account', 'FK_user_setting_account', 'OBJECT';
            PRINT 'Step 5: Renamed foreign key constraint to FK_user_setting_account';
        END
        ELSE
        BEGIN
            PRINT 'Step 5: Foreign key constraint not found or already renamed';
        END
        
        -- =====================================================
        -- STEP 6: Create new index with updated name
        -- =====================================================
        IF NOT EXISTS (SELECT 1 FROM sys.indexes 
                       WHERE name = 'IX_user_setting_updated_at' 
                       AND object_id = OBJECT_ID('radium.user_setting'))
        BEGIN
            CREATE INDEX [IX_user_setting_updated_at] ON [radium].[user_setting]([updated_at] DESC);
            PRINT 'Step 6: Created new index IX_user_setting_updated_at';
        END
        ELSE
        BEGIN
            PRINT 'Step 6: New index already exists';
        END
        
        PRINT '? Migration completed successfully!';
    END
END
ELSE
BEGIN
    -- =====================================================
    -- STEP 7: Check if new table exists (already migrated)
    -- =====================================================
    IF EXISTS (SELECT 1 FROM sys.tables WHERE schema_id = SCHEMA_ID('radium') AND name = 'user_setting')
    BEGIN
        PRINT 'Migration already completed - radium.user_setting exists';
    END
    ELSE
    BEGIN
        PRINT 'ERROR: Neither old nor new table exists!';
        PRINT 'Please run the base schema script first.';
    END
END
GO

-- =====================================================
-- VERIFICATION
-- =====================================================
PRINT '';
PRINT 'Verification:';
GO

IF EXISTS (SELECT 1 FROM sys.tables WHERE schema_id = SCHEMA_ID('radium') AND name = 'user_setting')
BEGIN
    DECLARE @row_count INT;
    SELECT @row_count = COUNT(*) FROM [radium].[user_setting];
    PRINT '  ? Table radium.user_setting exists with ' + CAST(@row_count AS VARCHAR(10)) + ' row(s)';
END
ELSE
BEGIN
    PRINT '  ? Table radium.user_setting does NOT exist';
END
GO

IF EXISTS (SELECT 1 FROM sys.indexes 
           WHERE name = 'IX_user_setting_updated_at' 
           AND object_id = OBJECT_ID('radium.user_setting'))
BEGIN
    PRINT '  ? Index IX_user_setting_updated_at exists';
END
ELSE
BEGIN
    PRINT '  ? Index IX_user_setting_updated_at does NOT exist';
END
GO

IF EXISTS (SELECT 1 FROM sys.foreign_keys 
           WHERE name = 'FK_user_setting_account' 
           AND parent_object_id = OBJECT_ID('radium.user_setting'))
BEGIN
    PRINT '  ? Foreign key FK_user_setting_account exists';
END
ELSE
BEGIN
    PRINT '  ? Foreign key FK_user_setting_account does NOT exist';
END
GO

PRINT '';
PRINT '=====================================================';
PRINT 'Migration completed: ' + CONVERT(VARCHAR(30), SYSUTCDATETIME(), 120);
PRINT '=====================================================';
GO
