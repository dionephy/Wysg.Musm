# Global Phrases Feature Implementation Guide

## Overview

The Global Phrases feature extends the Radium phrase system to support **system-wide phrases** that are available to all accounts without requiring individual account ownership. This enables administrators to create a shared library of common medical phrases that all users can access.

## Database Schema Changes

### Before (Original Structure)
```sql
CREATE TABLE [radium].[phrase](
    [id] [bigint] IDENTITY(1,1) NOT NULL,
    [account_id] [bigint] NOT NULL,  -- NOT NULL, required FK
    [text] [nvarchar](400) NOT NULL,
    [active] [bit] NOT NULL,
    [created_at] [datetime2](3) NOT NULL,
    [updated_at] [datetime2](3) NOT NULL,
    [rev] [bigint] NOT NULL,
    CONSTRAINT [FK_phrase_account] FOREIGN KEY([account_id])
        REFERENCES [app].[account]([account_id]) ON DELETE CASCADE
)
```

### After (With Global Phrases Support)
```sql
CREATE TABLE [radium].[phrase](
    [id] [bigint] IDENTITY(1,1) NOT NULL,
    [account_id] [bigint] NULL,  -- NOW NULLABLE
    [text] [nvarchar](400) NOT NULL,
    [active] [bit] NOT NULL,
    [created_at] [datetime2](3) NOT NULL,
    [updated_at] [datetime2](3) NOT NULL,
    [rev] [bigint] NOT NULL,
    CONSTRAINT [FK_phrase_account] FOREIGN KEY([account_id])
        REFERENCES [app].[account]([account_id]) ON DELETE CASCADE
)

-- Filtered unique index for account-specific phrases
CREATE UNIQUE INDEX [IX_phrase_account_text_unique]
ON [radium].[phrase]([account_id], [text])
WHERE [account_id] IS NOT NULL;

-- Filtered unique index for global phrases
CREATE UNIQUE INDEX [IX_phrase_global_text_unique]
ON [radium].[phrase]([text])
WHERE [account_id] IS NULL;
```

### Key Changes:
1. **`account_id` is now NULLABLE** - NULL indicates a global phrase
2. **Filtered unique indexes** ensure:
   - No duplicate text per account (when account_id IS NOT NULL)
   - No duplicate global phrases (when account_id IS NULL)
   - Account-specific phrase can have same text as a global phrase
3. **Foreign key still enforced** - Non-null account_id values must reference valid accounts

## Semantics & Behavior

### Phrase Ownership Model

| account_id Value | Ownership | Visibility | Managed By |
|-----------------|-----------|------------|------------|
| NULL | System/Global | All accounts | Administrators |
| Valid ID | Specific account | That account only | Account owner |

### Phrase Precedence Rules

When a user's account has **both** a global phrase and an account-specific phrase with the same text:
- **Account-specific phrase takes precedence** in combined queries
- This allows users to override global phrases with their own versions

Example:
```
Global: "normal chest x-ray" (account_id = NULL)
Account 42: "normal chest x-ray" (account_id = 42)

When account 42 queries combined phrases:
¡æ Returns account 42's version only (no duplicate)
```

## API Changes

### IPhraseService Interface

```csharp
public interface IPhraseService
{
    // EXISTING: Account-specific queries
    Task<IReadOnlyList<string>> GetPhrasesForAccountAsync(long accountId);
    Task<IReadOnlyList<string>> GetPhrasesByPrefixAccountAsync(long accountId, string prefix, int limit);
    
    // NEW: Global phrase queries (account_id IS NULL)
    Task<IReadOnlyList<string>> GetGlobalPhrasesAsync();
    Task<IReadOnlyList<string>> GetGlobalPhrasesByPrefixAsync(string prefix, int limit);
    
    // NEW: Combined queries (global + account-specific, deduplicated)
    Task<IReadOnlyList<string>> GetCombinedPhrasesAsync(long accountId);
    Task<IReadOnlyList<string>> GetCombinedPhrasesByPrefixAsync(long accountId, string prefix, int limit);
    
    // MODIFIED: Now accepts nullable accountId (NULL = global)
    Task<PhraseInfo> UpsertPhraseAsync(long? accountId, string text, bool active = true);
    Task<PhraseInfo?> ToggleActiveAsync(long? accountId, long phraseId);
    
    // NEW: Global phrase management
    Task<IReadOnlyList<PhraseInfo>> GetAllGlobalPhraseMetaAsync();
    Task RefreshGlobalPhrasesAsync();
}
```

### PhraseInfo Record

```csharp
// Changed: AccountId is now nullable
public record PhraseInfo(long Id, long? AccountId, string Text, bool Active, DateTime UpdatedAt, long Rev);
```

## Usage Examples

### Creating a Global Phrase (Administrator)

```csharp
var phraseService = serviceProvider.GetRequiredService<IPhraseService>();

// Pass NULL for accountId to create a global phrase
var globalPhrase = await phraseService.UpsertPhraseAsync(
    accountId: null,  // NULL = global phrase
    text: "normal chest radiograph",
    active: true
);

Console.WriteLine($"Created global phrase ID: {globalPhrase.Id}");
```

### Creating an Account-Specific Phrase (User)

```csharp
// Pass specific accountId to create user phrase
var userPhrase = await phraseService.UpsertPhraseAsync(
    accountId: 42,  // Specific account
    text: "my custom template",
    active: true
);
```

### Querying Global Phrases Only

```csharp
// Get all active global phrases
var globalPhrases = await phraseService.GetGlobalPhrasesAsync();

// Search global phrases by prefix
var globalMatches = await phraseService.GetGlobalPhrasesByPrefixAsync("normal", limit: 20);
```

### Querying Combined Phrases (Recommended for Completion)

```csharp
// Get both global and account-specific phrases (deduplicated)
var allPhrases = await phraseService.GetCombinedPhrasesAsync(accountId: 42);

// Search combined phrases by prefix (for autocomplete)
var completionMatches = await phraseService.GetCombinedPhrasesByPrefixAsync(
    accountId: 42,
    prefix: "norm",
    limit: 50
);
```

### Managing Global Phrases

```csharp
// List all global phrases with metadata
var globalMeta = await phraseService.GetAllGlobalPhraseMetaAsync();
foreach (var phrase in globalMeta)
{
    Console.WriteLine($"ID: {phrase.Id}, Text: {phrase.Text}, Active: {phrase.Active}");
}

// Toggle global phrase activation
var updated = await phraseService.ToggleActiveAsync(
    accountId: null,  // NULL = operating on global phrases
    phraseId: 123
);

// Refresh global phrases cache
await phraseService.RefreshGlobalPhrasesAsync();
```

## Migration Process

### Step 1: Backup Database
```sql
-- Backup the phrase table before migration
SELECT * INTO radium.phrase_backup FROM radium.phrase;
```

### Step 2: Run Migration Script
Execute the migration script at `db\migrations\20250108_add_global_phrases.sql`

### Step 3: Verify Schema
```sql
-- Check that account_id is now nullable
SELECT 
    COLUMN_NAME, 
    IS_NULLABLE, 
    DATA_TYPE 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_SCHEMA = 'radium' 
AND TABLE_NAME = 'phrase' 
AND COLUMN_NAME = 'account_id';

-- Should return IS_NULLABLE = 'YES'
```

### Step 4: Test Global Phrase Creation
```sql
-- Insert a test global phrase
INSERT INTO radium.phrase(account_id, text, active)
VALUES (NULL, 'Test Global Phrase', 1);

-- Verify it was created
SELECT * FROM radium.phrase WHERE account_id IS NULL;

-- Clean up test
DELETE FROM radium.phrase WHERE account_id IS NULL AND text = 'Test Global Phrase';
```

### Step 5: Update Application Code
Deploy the updated PhraseService and IPhraseService implementations.

### Step 6: Update Completion Providers (Recommended)
Change phrase completion providers to use `GetCombinedPhrasesByPrefixAsync` instead of `GetPhrasesByPrefixAccountAsync` to include global phrases in autocomplete.

## Performance Considerations

### Indexing Strategy
The migration creates several indexes optimized for different query patterns:

1. **IX_phrase_account_text_unique**: Fast lookups for account-specific phrases
2. **IX_phrase_global_text_unique**: Fast lookups for global phrases
3. **IX_phrase_global_active**: Efficient filtering of active global phrases
4. **IX_phrase_active_all**: Supports combined queries across global and account phrases

### Query Patterns

- **Account-only queries**: Use existing indexes (IX_phrase_account_active)
- **Global-only queries**: Use IX_phrase_global_active
- **Combined queries**: Application-level merge with O(n+m) deduplication

### Caching
PhraseService maintains separate in-memory snapshots for:
- Each account's phrases (keyed by accountId)
- Global phrases (keyed by GLOBAL_KEY = -1)

## Security & Access Control

### Current Implementation
- No built-in access control for global phrase creation
- All accounts can **read** global phrases
- Only admin-level code can **create/modify** global phrases (by passing NULL accountId)

### Recommendations for Production

1. **Add Permission Check in UI**:
```csharp
// Only allow admins to manage global phrases
if (!currentUser.IsAdmin && accountId == null)
{
    throw new UnauthorizedAccessException("Only administrators can manage global phrases");
}
```

2. **Add Database-Level Check (Optional)**:
Create a stored procedure that validates admin permission before allowing NULL account_id inserts.

3. **Audit Trail**:
Consider adding audit logging for global phrase modifications:
```csharp
if (accountId == null)
{
    _auditLogger.LogGlobalPhraseChange(phraseId, currentUser, action: "create/update/delete");
}
```

## Rollback Procedure

If you need to rollback the migration:

```sql
-- See rollback section in db\migrations\20250108_add_global_phrases.sql

-- WARNING: This will delete all global phrases
-- Backup first: SELECT * FROM radium.phrase WHERE account_id IS NULL
```

## Testing Checklist

- [ ] Create global phrase (account_id = NULL)
- [ ] Create account-specific phrase
- [ ] Query global phrases only
- [ ] Query account phrases only
- [ ] Query combined phrases (verify deduplication)
- [ ] Test prefix search for each mode
- [ ] Toggle global phrase active state
- [ ] Toggle account phrase active state
- [ ] Verify uniqueness constraints (duplicate global text should fail)
- [ ] Verify uniqueness constraints (duplicate account text should fail)
- [ ] Verify account phrase can have same text as global phrase
- [ ] Test refresh operations for global and account phrases
- [ ] Verify cascade delete (delete account should not affect global phrases)

## Future Enhancements

1. **Global Phrase Management UI**:
   - Admin panel in Settings window
   - Ability to import/export global phrase libraries
   - Bulk operations (activate/deactivate multiple)

2. **Phrase Categories**:
   - Tag global phrases by category (anatomy, findings, modality)
   - Filter completions by category

3. **Versioning**:
   - Track changes to global phrases over time
   - Allow rolling back to previous versions

4. **Synchronization**:
   - Sync global phrases across multiple installations
   - Central repository for shared medical phrase libraries

## Troubleshooting

### Issue: "Cannot insert NULL value in column 'account_id'"
**Cause**: Migration not applied yet
**Solution**: Run the migration script to make account_id nullable

### Issue: "Violation of UNIQUE KEY constraint"
**Cause**: Trying to create duplicate global phrase or duplicate account phrase
**Solution**: Check existing phrases before insert, or use UPSERT logic

### Issue: Global phrases not appearing in completion
**Cause**: Completion provider not updated to use combined query
**Solution**: Change completion provider to call `GetCombinedPhrasesByPrefixAsync`

### Issue: Performance degradation with large global phrase set
**Cause**: Combined queries merge and deduplicate in-memory
**Solution**: Consider caching combined results or implementing database-side UNION query

## Related Functional Requirements

- **FR-273**: System MUST support global phrases accessible to all accounts
- **FR-274**: Support three query modes (account, global, combined)
- **FR-275**: Apply synchronous database flow to global phrases
- **FR-276**: Accept nullable account_id in upsert/toggle operations
- **FR-277**: Combined queries use account-specific precedence
- **FR-278**: Database enforces uniqueness via filtered indexes

## References

- Main implementation: `apps\Wysg.Musm.Radium\Services\PhraseService.cs`
- Interface: `apps\Wysg.Musm.Radium\Services\IPhraseService.cs`
- Migration script: `db\migrations\20250108_add_global_phrases.sql`
- Specification: `apps\Wysg.Musm.Radium\docs\Spec.md` (FR-273..FR-278)
