# SNOMED Background Fetching Updates - Manual Start & Duplicate Skipping

## Changes Made

### 1. Skip Already Existing Terms (Automatic Duplicate Detection)
The SQLite cache service already had a `UNIQUE(concept_id, term_text)` constraint, which means:
- **Duplicates are automatically skipped** during caching
- The `CacheCandidateAsync` method returns `false` when a duplicate is encountered
- Background fetcher now tracks **skipped count** separately

### 2. Manual Start/Pause Control
Changed the background fetcher to **not start automatically** when window opens:
- Starts in **paused state** by default
- User must **set word count** and click **"Start Fetching"** button
- User can **pause and resume** fetching at any time

## Updated Components

### BackgroundSnomedFetcher.cs
**New Features:**
- `IsPaused` property - tracks whether fetcher is paused
- `TotalSkipped` property - counts skipped duplicates
- `Start()` method - manually start fetching
- `Pause()` method - pause fetching
- Constructor starts in paused state (`_isPaused = true`)
- Background loop checks pause state and waits when paused

**Benefits:**
- No automatic fetching on window open
- User has full control over when to fetch
- Tracks both new cached items AND skipped duplicates

### CacheReviewViewModel.cs
**New Features:**
- `TargetWordCount` property with setter (user can change word count)
- `BackgroundSkippedCount` property (displays skipped duplicates)
- `IsBackgroundRunning` property (reflects pause state)
- `StartFetchCommand` - starts background fetching
- `PauseFetchCommand` - pauses background fetching
- Initial status message: "Set word count and click Start to begin fetching"

**Benefits:**
- User can set word count before starting
- Clear visual feedback of running/paused state
- Tracks skip statistics

### CacheReviewWindow.xaml
**New UI Elements:**
1. **Controls Row:**
   - Target Word Count input field (editable)
   - "Start Fetching" button (green, enabled when paused)
   - "Pause Fetching" button (orange, enabled when running)

2. **Statistics Row:**
   - Concepts Fetched (total concepts processed)
   - New Cached (new items added to cache)
   - **Skipped (Existing)** - NEW! Shows duplicates skipped
   - Current Page
   - Running indicator (green dot when active)

3. **Info Text:**
   - Explains that duplicates are automatically skipped

## User Workflow

### Before (Old Behavior):
1. Open window ¡æ fetching starts immediately
2. No control over word count without external changes
3. No visibility into skipped duplicates

### After (New Behavior):
1. Open window ¡æ **paused state**, shows "Set word count and click Start to begin fetching"
2. User sets desired word count (1-10)
3. User clicks **"Start Fetching"**
4. Background fetcher begins, showing:
   - Concepts Fetched
   - New Cached
   - **Skipped (Existing)** duplicates
   - Current Page
5. User can **pause/resume** at any time
6. User reviews pending candidates (independent of fetching state)

## Technical Implementation

### Duplicate Detection
```csharp
// In SqliteSnomedCacheService.cs
const string insertSql = @"
    INSERT OR IGNORE INTO cached_candidates (...)
    VALUES (...)
";
var rowsAffected = await cmd.ExecuteNonQueryAsync();
return rowsAffected > 0; // Returns false if duplicate (not inserted)
```

### Pause State Check
```csharp
// In BackgroundSnomedFetcher.cs
while (!cancellationToken.IsCancellationRequested)
{
    bool paused;
    lock (_stateLock)
    {
        paused = _isPaused;
    }

    if (paused)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
        continue; // Skip fetching when paused
    }
    
    // ... fetch logic ...
}
```

### Statistics Tracking
```csharp
if (cached)
{
    cachedCount++;
    TotalCached++;
}
else
{
    skippedCount++;  // NEW!
    TotalSkipped++;  // NEW!
}
```

## Benefits

### 1. User Control
- **No surprises**: Fetching doesn't start automatically
- **Configurability**: Set word count before starting
- **Flexibility**: Pause/resume as needed

### 2. Resource Efficiency
- **Skip duplicates**: Don't re-cache existing terms
- **Visibility**: User sees what's being skipped
- **Smart caching**: Database constraint prevents duplicates

### 3. Better UX
- **Clear status**: See running/paused state
- **Progress tracking**: Separate counts for new vs. skipped
- **Informed decisions**: Know what's already in database

## Database Efficiency

The `UNIQUE(concept_id, term_text)` constraint ensures:
- No duplicate entries at database level
- Fast lookup (indexed unique constraint)
- Atomic insert-or-skip operation
- No need for explicit duplicate checking in code

## Future Enhancements

Possible improvements:
- Add "Reset" button to clear fetcher state and start from page 1
- Show percentage of duplicates vs. new items
- Export skipped items report
- Filter by duplicate status in review UI
- Auto-pause when certain thresholds reached

---

**Implementation Date**: 2025-01-21  
**Status**: ? Complete & Working  
**Build Status**: ? Successful
