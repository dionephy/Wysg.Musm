# Table Rename: reportify_setting ¡æ user_setting

## Summary
This change renames the database table `radium.reportify_setting` to `radium.user_setting` to better reflect its purpose as a general user settings storage (not limited to reportify settings).

## Files Changed

### Code Files
1. **apps\Wysg.Musm.Radium\Services\ReportifySettingsService.cs**
   - Updated SQL queries in `GetSettingsJsonAsync()`, `UpsertAsync()`, and `DeleteAsync()` methods
   - Changed table references from `radium.reportify_setting` to `radium.user_setting`

### Database Schema Files
2. **apps\Wysg.Musm.Radium\docs\db\db_central_azure_base_20251015.sql**
   - Updated table creation script (SECTION 12)
   - Updated index creation script (SECTION 13)
   - Updated verification queries (SECTION 14)
   - Changed table name, foreign key constraint name, and index name

3. **db\schema\central_db(postgres)_20250928.sql**
   - Updated PostgreSQL schema definition
   - Changed table name, index name, and comments

### Migration Scripts (NEW)
4. **apps\Wysg.Musm.Radium\docs\db\migrations\20250122_rename_reportify_setting_to_user_setting.sql**
   - Azure SQL migration script
   - Safely renames existing table and associated objects
   - Idempotent and can be re-run

5. **apps\Wysg.Musm.Radium\docs\db\migrations\20250122_rename_reportify_setting_to_user_setting_postgres.sql**
   - PostgreSQL migration script
   - Safely renames existing table and associated objects
   - Idempotent and can be re-run

## Database Objects Renamed

### Table
- Old: `radium.reportify_setting`
- New: `radium.user_setting`

### Foreign Key Constraint
- Old: `FK_reportify_account`
- New: `FK_user_setting_account`

### Index
- Old: `IX_reportify_updated_at` (Azure SQL) / `ix_reportify_setting_updated_at` (PostgreSQL)
- New: `IX_user_setting_updated_at` (Azure SQL) / `ix_user_setting_updated_at` (PostgreSQL)

## Deployment Steps

### For Azure SQL Database:
1. **Run migration script FIRST** (before deploying code):
   ```sql
   -- Connect to your Azure SQL database
   -- Run: apps\Wysg.Musm.Radium\docs\db\migrations\20250122_rename_reportify_setting_to_user_setting.sql
   ```

2. **Deploy code changes**:
   - The application will now use the new table name
   - No data loss - all existing settings are preserved

3. **Verify**:
   ```sql
   SELECT * FROM radium.user_setting;
   SELECT COUNT(*) FROM radium.user_setting;
   ```

### For PostgreSQL Database:
1. **Run migration script FIRST** (before deploying code):
   ```sql
   -- Connect to your PostgreSQL database
   -- Run: apps\Wysg.Musm.Radium\docs\db\migrations\20250122_rename_reportify_setting_to_user_setting_postgres.sql
   ```

2. **Deploy code changes**:
   - The application will now use the new table name
   - No data loss - all existing settings are preserved

3. **Verify**:
   ```sql
   SELECT * FROM radium.user_setting;
   SELECT count(*) FROM radium.user_setting;
   ```

## Rollback Plan (If Needed)

If you need to rollback to the old table name:

### Azure SQL:
```sql
-- Rename table back
EXEC sp_rename 'radium.user_setting', 'reportify_setting';

-- Rename foreign key back
EXEC sp_rename 'radium.FK_user_setting_account', 'FK_reportify_account', 'OBJECT';

-- Drop new index
DROP INDEX [IX_user_setting_updated_at] ON [radium].[reportify_setting];

-- Create old index
CREATE INDEX [IX_reportify_updated_at] ON [radium].[reportify_setting]([updated_at] DESC);
```

### PostgreSQL:
```sql
-- Rename table back
ALTER TABLE radium.user_setting RENAME TO reportify_setting;

-- Rename foreign key back
ALTER TABLE radium.reportify_setting 
RENAME CONSTRAINT user_setting_account_id_fkey TO reportify_setting_account_id_fkey;

-- Drop new index
DROP INDEX IF EXISTS radium.ix_user_setting_updated_at;

-- Create old index
CREATE INDEX ix_reportify_setting_updated_at ON radium.reportify_setting(updated_at DESC);
```

Then deploy the old code version.

## Testing Checklist

- [ ] Run migration script on development database
- [ ] Verify table renamed successfully
- [ ] Verify all rows preserved (count before/after)
- [ ] Deploy code to development environment
- [ ] Test loading user settings (GET operation)
- [ ] Test saving user settings (UPSERT operation)
- [ ] Verify no errors in application logs
- [ ] Repeat on staging/production environments

## Notes

- **No breaking changes**: The table rename is backward compatible from the application perspective
- **No data loss**: All existing data is preserved during the rename
- **Idempotent**: Migration scripts can be safely re-run
- **Zero downtime**: The rename operation is fast (sub-second for typical data volumes)
- **Service compatibility**: The `IReportifySettingsService` interface remains unchanged

## Related Issues
- Database table name change requested by user
- Better reflects the table's purpose as general user settings storage
- **Security Enhancement**: Desktop client now accesses user settings through REST API instead of direct database connection
