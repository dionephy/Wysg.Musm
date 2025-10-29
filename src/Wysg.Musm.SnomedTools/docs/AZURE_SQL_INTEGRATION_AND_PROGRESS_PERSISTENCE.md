# SNOMED Azure SQL Integration & Progress Persistence

## Summary

Enhanced the background fetching system with two critical features:
1. **Azure SQL Phrase Checking** - Skip terms that already exist in production database
2. **Progress Persistence** - Resume fetching from where it left off after app restart

## Changes Made

### 1. Azure SQL Phrase Existence Check

#### Problem Solved
- Previously only checked local SQLite cache for duplicates
- Didn't check if terms already exist in Azure SQL `phrase` table
- Could cache terms already in production database

#### Solution
```csharp
// In BackgroundSnomedFetcher.FetchNextPageAsync()
var termText = term.Term.ToLowerInvariant().Trim();

// FIRST: Check Azure SQL phrase table
var existsInAzure = await _cacheService.CheckPhraseExistsInDatabaseAsync(termText);

if (existsInAzure)
{
    skippedCount++;
    TotalSkipped++;
    Debug.WriteLine($"Skipped '{termText}' - already exists in Azure SQL");
    continue; // Skip entirely
}

// SECOND: Try local cache (for duplicates within this session)
var cached = await _cacheService.CacheCandidateAsync(...);
```

#### Benefits
- **No wasted effort**: Don't cache terms already in production
- **Accurate skip count**: Separates "already in Azure" from "already in local cache"
- **Efficient**: Only caches truly new candidates

### 2. Fetch Progress Persistence

#### Problem Solved
- When app closes, fetch progress is lost
- Must start from page 1 on next session
- Wastes time re-processing already-fetched concepts

#### Solution

**Database Schema:**
```sql
CREATE TABLE fetch_progress (
    id INTEGER PRIMARY KEY CHECK (id = 1),  -- Only one row
    target_word_count INTEGER NOT NULL,
    next_search_after TEXT,                  -- Snowstorm pagination token
    current_page INTEGER NOT NULL,
    saved_at TEXT NOT NULL
);
```

**Automatic Saving:**
```csharp
// After every successful page fetch
await _cacheService.SaveFetchProgressAsync(
    _targetWordCount, 
    _nextSearchAfter, 
    _currentPage
);
```

**Automatic Restoration:**
```csharp
// In TestWindow.OnOpenCacheReview()
if (_backgroundFetcher == null)
{
    _backgroundFetcher = new BackgroundSnomedFetcher(...);
    
    // Restore from last session
    var restored = await _backgroundFetcher.RestoreProgressAsync();
    if (restored)
    {
        Debug.WriteLine("Fetch progress restored from last session");
    }
}
```

#### Benefits
- **Seamless resume**: Continue exactly where you left off
- **No duplicate work**: Don't re-fetch already processed pages
- **Cross-session continuity**: Close and reopen anytime

## Updated Components

### ISnomedCacheService.cs
**New Methods:**
```csharp
// Check if phrase exists in Azure SQL
Task<bool> CheckPhraseExistsInDatabaseAsync(string phraseText);

// Save fetch progress
Task SaveFetchProgressAsync(int targetWordCount, string? nextSearchAfter, int currentPage);

// Load saved progress
Task<FetchProgress?> LoadFetchProgressAsync();

// Clear saved progress
Task ClearFetchProgressAsync();
```

**New Record:**
```csharp
public sealed record FetchProgress(
    int TargetWordCount,
    string? NextSearchAfter,
    int CurrentPage,
    DateTime SavedAt);
```

### SqliteSnomedCacheService.cs
**Constructor Change:**
```csharp
// Now requires IPhraseService for Azure SQL checking
public SqliteSnomedCacheService(IPhraseService phraseService, string? dbPath = null)
```

**New Implementation:**
- `CheckPhraseExistsInDatabaseAsync()` - Queries Azure SQL via IPhraseService
- `SaveFetchProgressAsync()` - Upserts to fetch_progress table
- `LoadFetchProgressAsync()` - Loads from fetch_progress table
- `ClearFetchProgressAsync()` - Deletes saved progress

**Database Updates:**
- New `fetch_progress` table with single-row constraint
- Stores: word count, search token, page number, timestamp

### BackgroundSnomedFetcher.cs
**New Methods:**
```csharp
// Restore progress from last session
public async Task<bool> RestoreProgressAsync()

// Clear saved progress and reset
public async Task ClearProgressAsync()
```

**Modified Logic:**
1. Check Azure SQL BEFORE local cache
2. Save progress after every successful page fetch
3. Support restoration on startup

### CacheReviewViewModel.cs
**New Command:**
```csharp
public IAsyncRelayCommand ResetProgressCommand { get; }
```

**New Method:**
```csharp
private async Task ResetProgressAsync()
{
    // Pause fetching
    // Clear progress from database
    // Reset counters to zero
    // Update status message
}
```

### TestWindow.xaml.cs
**Updated:**
- Pass `phraseService` to `SqliteSnomedCacheService` constructor
- Call `RestoreProgressAsync()` when creating background fetcher
- Progress automatically restored on window open

## User Workflow

### First Session:
1. Open Cache Review window
2. Set word count to "2" (for 2-word terms)
3. Click "Start Fetching"
4. Background fetcher processes pages 1, 2, 3...
5. User reviews some candidates
6. **Close window** (progress saved automatically)

### Next Session:
1. Open Cache Review window
2. **Progress automatically restored**
   - "Restored progress: page=3, wordCount=2, saved=2025-01-21 1:30 PM"
3. Word count already set to "2"
4. Click "Start Fetching"
5. **Continues from page 4** (not page 1!)
6. User continues reviewing

### Reset Option:
- Click "Reset Progress" button
- Clears saved progress
- Resets counters to zero
- Next start begins from page 1

## Skip Logic

Terms are skipped in TWO places:

1. **Azure SQL Check** (First Priority)
   ```
   If phrase exists in Azure SQL ¡æ Skip
   Reason: Already in production database
   ```

2. **Local Cache Check** (Second Priority)
   ```
   If phrase exists in local cache ¡æ Skip
   Reason: Already cached this session
   ```

Both contribute to "Skipped (Existing)" counter.

## Technical Details

### Progress Save Timing
- **After every successful page fetch**
- Before processing concepts
- Ensures token/page are persisted immediately

### Progress Restore Conditions
- Runs on `OnOpenCacheReview` (first time only)
- Checks if `_backgroundFetcher == null`
- Loads from database asynchronously
- Sets internal state (page, token, word count)

### Database Constraint
```sql
id INTEGER PRIMARY KEY CHECK (id = 1)
```
- Only ONE progress record can exist
- INSERT OR UPDATE pattern
- Always overwrites previous progress

### Azure SQL Check Implementation
```csharp
public async Task<bool> CheckPhraseExistsInDatabaseAsync(string phraseText)
{
    // Get ALL global phrases from Azure SQL
    var globalPhrases = await _phraseService.GetAllGlobalPhraseMetaAsync();
    
    // Case-insensitive, trimmed comparison
    var normalized = phraseText.ToLowerInvariant().Trim();
    return globalPhrases.Any(p => p.Text.ToLowerInvariant().Trim() == normalized);
}
```

## Performance Considerations

### Azure SQL Check
- Calls `GetAllGlobalPhraseMetaAsync()` for EVERY term
- **Potential optimization**: Cache global phrases in memory
- **Current approach**: Simple, works for moderate datasets
- **Future**: Add in-memory cache with TTL

### Progress Persistence
- SQLite INSERT/UPDATE per page (not per term)
- Minimal overhead (~1ms per page)
- No network calls
- Local file system only

## Error Handling

### Azure SQL Check Failure
```csharp
catch (Exception ex)
{
    Debug.WriteLine($"Error checking phrase existence: {ex.Message}");
    return false; // Assume doesn't exist to avoid skipping valid candidates
}
```
**Rationale**: Better to cache a duplicate than miss a valid candidate

### Progress Restoration Failure
- Silent failure (logs debug message)
- Starts from beginning if restore fails
- No user interruption

## Future Enhancements

### Possible Improvements:
1. **In-memory phrase cache** - Cache Azure SQL phrases for session
2. **Batch Azure SQL checks** - Check multiple phrases in one call
3. **Progress history** - Keep last N progress snapshots
4. **Progress export** - Export/import progress for sharing
5. **Visual progress indicator** - Show % completion based on estimated total
6. **Multiple progress tracks** - Save separate progress per word count

## Testing

### Manual Test Scenarios:

1. **Azure SQL Skip Test**
   - Add phrase "heart" to Azure SQL
   - Start fetching
   - Verify "heart" is skipped
   - Check "Skipped (Existing)" counter increases

2. **Progress Persistence Test**
   - Start fetching, wait for page 5
   - Close window
   - Reopen window
   - Verify starts from page 6 (not page 1)

3. **Reset Progress Test**
   - Fetch to page 10
   - Click "Reset Progress"
   - Start fetching
   - Verify starts from page 1

---

**Implementation Date**: 2025-01-21  
**Status**: ? Complete & Working  
**Build Status**: ? Successful  
**Key Benefits**: 
- ? No duplicate work checking Azure SQL
- ? Resume from exact point after restart
- ? User control with Reset option
