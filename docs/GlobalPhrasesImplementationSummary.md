# Global Phrases Implementation Summary

## Request
User requested implementation of "global phrases" where `account_id` in `radium.phrase` table can be NULL to create phrases available to all accounts.

## Problem Analysis

**Original Challenge**: The database schema had `account_id` as a **NOT NULL** foreign key with `ON DELETE CASCADE`, making it impossible to have account_id = NULL.

**User's Question**: "would it be impossible if it is referenced to account_id of app.account?"

**Answer**: **No, it's possible!** SQL Server and PostgreSQL foreign keys **automatically allow NULL values** in the child table even when the parent column is NOT NULL. This is standard SQL behavior.

## Solution Implemented

### Database Schema Changes

**Approach**: Make `account_id` nullable with filtered unique indexes

**Files Created**:
- `db\migrations\20250108_add_global_phrases.sql` - Complete migration script with rollback

**Key Changes**:
1. Altered `account_id` column from NOT NULL to **NULL**
2. Dropped original `UQ_phrase_account_text` constraint
3. Created **filtered unique index** for account phrases: `IX_phrase_account_text_unique` (WHERE account_id IS NOT NULL)
4. Created **filtered unique index** for global phrases: `IX_phrase_global_text_unique` (WHERE account_id IS NULL)
5. Added performance indexes for global phrase queries
6. Preserved existing FK constraint (automatically handles NULL values)

### Code Changes

**Modified Files**:
1. `apps\Wysg.Musm.Radium\Services\IPhraseService.cs`
   - Changed `PhraseInfo` record to use `long? AccountId` (nullable)
   - Added global phrase query methods
   - Added combined phrase query methods (global + account)
   - Modified `UpsertPhraseAsync` and `ToggleActiveAsync` to accept `long? accountId`

2. `apps\Wysg.Musm.Radium\Services\PhraseService.cs`
   - Updated `PhraseRow` class to use nullable `AccountId`
   - Implemented `GetGlobalPhrasesAsync()` and `GetGlobalPhrasesByPrefixAsync()`
   - Implemented `GetCombinedPhrasesAsync()` and `GetCombinedPhrasesByPrefixAsync()`
   - Modified internal upsert/toggle methods to handle NULL account_id in SQL
   - Added `LoadGlobalPhrasesAsync()` for global phrase caching
   - Used `GLOBAL_KEY = -1` as dictionary key for global phrase state

3. `apps\Wysg.Musm.Radium\Services\AzureSqlPhraseService.cs` **[NEW FIX]**
   - Updated `PhraseRow` class to use nullable `AccountId`
   - Implemented all global phrase methods (GetGlobalPhrasesAsync, GetGlobalPhrasesByPrefixAsync, etc.)
   - Implemented combined phrase methods with deduplication
   - Modified `UpsertPhraseAsync` to accept `long? accountId` with conditional SQL
   - Modified `ToggleActiveAsync` to accept `long? accountId` with conditional SQL
   - Added `LoadGlobalSnapshotAsync()` for loading global phrases
   - Added `GetAllGlobalPhraseMetaAsync()` and `RefreshGlobalPhrasesAsync()`

### Documentation Updates

**Files Updated**:
1. `apps\Wysg.Musm.Radium\docs\Spec.md` - Added FR-273..FR-278
2. `apps\Wysg.Musm.Radium\docs\Plan.md` - Added change log entry
3. `apps\Wysg.Musm.Radium\docs\Tasks.md` - Added tasks T376..T392

**Files Created**:
4. `docs\GlobalPhrasesFeature.md` - Comprehensive implementation guide

## Key Features

### 1. Three Query Modes
- **Account-only**: `GetPhrasesForAccountAsync(accountId)`
- **Global-only**: `GetGlobalPhrasesAsync()`
- **Combined**: `GetCombinedPhrasesAsync(accountId)` - merges both with deduplication

### 2. Precedence Rules
When both global and account-specific phrase have same text:
- **Account-specific takes precedence** (no duplicate in combined results)

### 3. Synchronous Flow
Global phrases follow the same strict synchronous database flow as account phrases (FR-258..FR-260):
- User action ¡æ Database update ¡æ Snapshot update ¡æ UI display

### 4. Two Implementations
Both PostgreSQL (PhraseService) and Azure SQL (AzureSqlPhraseService) implementations support global phrases with identical behavior.

### 5. API Usage

**Creating Global Phrase** (admin/system):
```csharp
await phraseService.UpsertPhraseAsync(
    accountId: null,  // NULL = global
    text: "normal chest x-ray",
    active: true
);
```

**Creating Account Phrase**:
```csharp
await phraseService.UpsertPhraseAsync(
    accountId: 42,
    text: "my custom phrase",
    active: true
);
```

**Querying for Autocomplete** (recommended):
```csharp
// Gets global + account phrases, deduplicated
var matches = await phraseService.GetCombinedPhrasesByPrefixAsync(
    accountId: 42,
    prefix: "norm",
    limit: 50
);
```

## Migration Path

1. **Backup**: Run backup of `radium.phrase` table
2. **Execute**: Run `db\migrations\20250108_add_global_phrases.sql`
3. **Verify**: Check schema changes and create test global phrase
4. **Deploy**: Update application code
5. **Test**: Verify all phrase operations work correctly

## Benefits

? **Centralized library**: Administrators can maintain common medical phrases  
? **No duplication**: Each user doesn't need to recreate common phrases  
? **Override capability**: Users can override global phrases with their own versions  
? **Performance**: Filtered indexes optimize both global and account queries  
? **Backward compatible**: Existing account phrases continue to work unchanged  
? **Standard SQL**: Uses built-in NULL handling in foreign keys (no custom logic)  
? **Multi-backend**: Works with both PostgreSQL and Azure SQL implementations

## Remaining Work

The following tasks are marked as incomplete in Tasks.md:

- [ ] **T385**: Update completion provider to use combined phrases by default
- [ ] **T386**: Add global phrases management UI in Settings window
- [ ] **T387**: Add unit tests for global phrase operations
- [ ] **T388**: Add integration tests for combined phrase queries

## Testing Recommendations

1. **Unit Tests**: Test global phrase CRUD operations independently
2. **Integration Tests**: Test combined queries with various edge cases:
   - Account with no phrases + globals
   - Account with phrases + no globals
   - Account phrase overriding global phrase
   - Prefix search across both sets
3. **Performance Tests**: Verify combined query performance with large datasets
4. **UI Tests**: Test global phrase management interface (when implemented)

## Security Considerations

**Current State**: No access control on global phrase creation

**Recommendations**:
1. Add permission check in UI layer (only admins can create global phrases)
2. Consider adding database-level role check via stored procedure
3. Add audit logging for global phrase modifications
4. Document who can create/modify global phrases in user manual

## Rollback

If issues arise, the migration script includes a complete rollback procedure:
- Deletes all global phrases (account_id IS NULL)
- Reverts schema to original state
- Restores original indexes and constraints

See rollback section in `db\migrations\20250108_add_global_phrases.sql`

## Functional Requirements Coverage

- **FR-273**: ? Global phrases with NULL account_id supported
- **FR-274**: ? Three query modes implemented (account, global, combined)
- **FR-275**: ? Synchronous database flow applied to global phrases
- **FR-276**: ? Nullable account_id accepted in upsert/toggle operations
- **FR-277**: ? Account-specific precedence in combined queries
- **FR-278**: ? Filtered unique indexes enforce proper uniqueness

## Files Changed/Created

### Created:
- `db\migrations\20250108_add_global_phrases.sql`
- `docs\GlobalPhrasesFeature.md`
- `docs\GlobalPhrasesImplementationSummary.md`
- `docs\GlobalPhrasesArchitectureDiagram.md`

### Modified:
- `apps\Wysg.Musm.Radium\Services\IPhraseService.cs`
- `apps\Wysg.Musm.Radium\Services\PhraseService.cs`
- `apps\Wysg.Musm.Radium\Services\AzureSqlPhraseService.cs` **[FIXED]**
- `apps\Wysg.Musm.Radium\docs\Spec.md`
- `apps\Wysg.Musm.Radium\docs\Plan.md`
- `apps\Wysg.Musm.Radium\docs\Tasks.md`

## Build Status

? **Build successful!** All compilation errors fixed  
? **No compilation errors** in PhraseService  
? **No compilation errors** in AzureSqlPhraseService  
? **No compilation errors** in IPhraseService  

The AzureSqlPhraseService has been updated to implement all new global phrases methods, and the solution now builds successfully.

## Conclusion

The global phrases feature is fully designed and implemented at the service layer for **both PostgreSQL and Azure SQL backends**. The database migration script is ready to execute. All compilation errors have been resolved. The remaining work involves:
1. Updating completion providers to use combined queries
2. Adding admin UI for managing global phrases
3. Adding comprehensive tests

The solution properly handles the FK constraint with NULL values, which is standard SQL behavior and requires no workarounds. Both PhraseService and AzureSqlPhraseService now support global phrases with identical API semantics.
