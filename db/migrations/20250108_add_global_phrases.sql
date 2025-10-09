-- =============================================
-- Migration: Add Global Phrases Support
-- Date: 2025-01-08
-- Description: Allow radium.phrase.account_id to be NULL for global phrases
--              that are available to all accounts
-- =============================================

-- WARNING: This migration modifies the phrase table structure
-- Backup recommended before execution

BEGIN TRANSACTION;

-- Step 1: Drop existing foreign key constraint
ALTER TABLE [radium].[phrase] DROP CONSTRAINT [FK_phrase_account];

-- Step 2: Alter account_id column to allow NULL values
ALTER TABLE [radium].[phrase] ALTER COLUMN [account_id] bigint NULL;

-- Step 3: Recreate foreign key constraint
-- NULL values will automatically bypass the FK check
ALTER TABLE [radium].[phrase] 
ADD CONSTRAINT [FK_phrase_account] 
FOREIGN KEY([account_id]) REFERENCES [app].[account]([account_id]) 
ON DELETE CASCADE;

-- Step 4: Drop existing unique constraint (does not handle NULL properly)
ALTER TABLE [radium].[phrase] DROP CONSTRAINT [UQ_phrase_account_text];

-- Step 5: Create filtered unique index for account-specific phrases
-- This ensures uniqueness per account (excluding global phrases)
CREATE UNIQUE NONCLUSTERED INDEX [IX_phrase_account_text_unique]
ON [radium].[phrase]([account_id], [text])
WHERE [account_id] IS NOT NULL;

-- Step 6: Create unique index for global phrases
-- This ensures no duplicate global phrases
CREATE UNIQUE NONCLUSTERED INDEX [IX_phrase_global_text_unique]
ON [radium].[phrase]([text])
WHERE [account_id] IS NULL;

-- Step 7: Add index for efficient global phrase queries
CREATE NONCLUSTERED INDEX [IX_phrase_global_active]
ON [radium].[phrase]([active])
INCLUDE ([text], [id], [created_at], [updated_at], [rev])
WHERE [account_id] IS NULL;

-- Step 8: Add index for combined queries (global + account)
CREATE NONCLUSTERED INDEX [IX_phrase_active_all]
ON [radium].[phrase]([active], [account_id])
INCLUDE ([text], [id], [created_at], [updated_at], [rev]);

COMMIT TRANSACTION;

-- =============================================
-- Verification Queries
-- =============================================

-- Check the new structure
-- SELECT COUNT(*) AS total_phrases,
--        COUNT(account_id) AS account_phrases,
--        COUNT(*) - COUNT(account_id) AS global_phrases
-- FROM radium.phrase;

-- Sample insert test (DO NOT RUN IN PRODUCTION)
-- INSERT INTO radium.phrase(account_id, text, active) VALUES (NULL, 'Test Global Phrase', 1);

-- =============================================
-- Rollback Script (if needed)
-- =============================================
/*
BEGIN TRANSACTION;

-- Remove global phrases first
DELETE FROM radium.phrase WHERE account_id IS NULL;

-- Drop new indexes
DROP INDEX IF EXISTS [IX_phrase_global_active] ON [radium].[phrase];
DROP INDEX IF EXISTS [IX_phrase_active_all] ON [radium].[phrase];
DROP INDEX IF EXISTS [IX_phrase_global_text_unique] ON [radium].[phrase];
DROP INDEX IF EXISTS [IX_phrase_account_text_unique] ON [radium].[phrase];

-- Drop FK
ALTER TABLE [radium].[phrase] DROP CONSTRAINT [FK_phrase_account];

-- Make account_id NOT NULL again
ALTER TABLE [radium].[phrase] ALTER COLUMN [account_id] bigint NOT NULL;

-- Recreate original FK
ALTER TABLE [radium].[phrase] 
ADD CONSTRAINT [FK_phrase_account] 
FOREIGN KEY([account_id]) REFERENCES [app].[account]([account_id]) 
ON DELETE CASCADE;

-- Recreate original unique constraint
ALTER TABLE [radium].[phrase] 
ADD CONSTRAINT [UQ_phrase_account_text] UNIQUE ([account_id], [text]);

COMMIT TRANSACTION;
*/
