-- Migration: Drop is_created and created_by columns from med.rad_report
-- Date: 2025-01-21
-- Reason: These columns are now stored as JSON fields (report_radiologist) instead
-- Breaking Change: Yes - applications must migrate to reading radiologist from JSON before running this

-- Step 1: Verify that all applications are using the new JSON-based approach
-- Before running this migration, ensure:
-- 1. All report saves include report_radiologist in the JSON
-- 2. All report loads read report_radiologist from JSON (not from created_by column)
-- 3. Backup the database in case rollback is needed

-- Step 2: Drop the columns (PostgreSQL 11+)
ALTER TABLE med.rad_report 
  DROP COLUMN IF EXISTS is_created,
  DROP COLUMN IF EXISTS created_by;

-- Step 3: Verify the table structure
-- Expected columns after migration:
-- - id (bigint, primary key)
-- - study_id (bigint, foreign key)
-- - is_mine (boolean)
-- - report_datetime (timestamp with time zone)
-- - report (jsonb) -- Contains report_radiologist field
-- - created_at (timestamp with time zone)

-- Rollback Instructions (if needed):
-- If you need to rollback, run:
-- ALTER TABLE med.rad_report ADD COLUMN is_created boolean NOT NULL DEFAULT true;
-- ALTER TABLE med.rad_report ADD COLUMN created_by text COLLATE pg_catalog."default";
-- Then update application code to populate these columns again.

-- Notes:
-- - The report_radiologist field in JSON should be populated by GetReportedReport module
-- - Previous studies will show radiologist from JSON report_radiologist field
-- - This change aligns with the pattern of storing all report metadata in JSON
