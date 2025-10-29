# SNOMED Background Fetching & Caching Implementation

## Summary

Implemented a comprehensive background fetching and local caching system for SNOMED Tools that allows continuous synonym fetching while the user reviews and selects candidates for saving.

## Architecture

### Core Components

1. **BackgroundSnomedFetcher** (`Services/BackgroundSnomedFetcher.cs`)
   - Runs continuously in a background thread
   - Fetches SNOMED concepts from Snowstorm API
   - Filters synonyms based on word count
   - Caches matching candidates automatically
   - Provides real-time progress events
   - Lifecycle: Starts immediately on instantiation, runs until Dispose()

2. **ISnomedCacheService** (`Abstractions/ISnomedCacheService.cs`)
   - Interface for managing cached SNOMED candidates
   - Supports states: Pending, Accepted, Rejected, Saved
   - Provides CRUD operations for candidates

3. **SqliteSnomedCacheService** (`Services/SqliteSnomedCacheService.cs`)
   - SQLite-based implementation of cache service
   - Stores candidates in local database
   - Prevents duplicates via UNIQUE constraint
   - Supports cleanup of old candidates

4. **CacheReviewViewModel** (`ViewModels/CacheReviewViewModel.cs`)
   - Manages UI state for cache review window
   - Subscribes to background fetcher events
   - Allows user to accept/reject candidates
   - Batch save accepted candidates to database

5. **CacheReviewWindow** (`Views/CacheReviewWindow.xaml`)
   - WPF UI for reviewing cached candidates
   - Real-time background fetch status display
   - One-click accept/reject workflow
   - Batch save functionality

## Key Features

### Background Fetching
- **Continuous Operation**: Fetches concepts non-stop until window closes
- **Automatic Caching**: Matching synonyms are cached automatically
- **Configurable Word Count**: Filter synonyms by number of words
- **Progress Tracking**: Real-time updates on fetched/cached counts
- **Auto-restart**: When all concepts are processed, waits 60s and restarts

### Local Caching
- **SQLite Storage**: Lightweight, file-based database
- **Duplicate Prevention**: UNIQUE constraint on (concept_id, term_text)
- **Status Management**: Pending ⊥ Accepted/Rejected ⊥ Saved workflow
- **Cleanup**: Delete old candidates after configurable days

### User Review Workflow
1. Background fetcher runs automatically, caching candidates
2. User opens Cache Review window
3. Current pending candidate is displayed
4. User can:
   - **Accept**: Mark for saving (moves to Accepted state)
   - **Reject**: Ignore permanently (moves to Rejected state)
   - **Save All Accepted**: Batch save all accepted candidates to database
5. Window shows real-time background fetch progress
6. Background fetcher continues running independently

## Database Schema

```sql
CREATE TABLE cached_candidates (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    concept_id INTEGER NOT NULL,
    concept_id_str TEXT NOT NULL,
    concept_fsn TEXT NOT NULL,
    concept_pt TEXT,
    term_text TEXT NOT NULL,
    term_type TEXT NOT NULL,
    word_count INTEGER NOT NULL,
    cached_at TEXT NOT NULL,
    status INTEGER NOT NULL DEFAULT 0,  -- 0=Pending, 1=Accepted, 2=Rejected, 3=Saved
    UNIQUE(concept_id, term_text)
);

CREATE INDEX idx_status ON cached_candidates(status);
CREATE INDEX idx_word_count ON cached_candidates(word_count);
CREATE INDEX idx_cached_at ON cached_candidates(cached_at);
CREATE INDEX idx_term_text ON cached_candidates(term_text);
```

## File Structure

```
src/Wysg.Musm.SnomedTools/
戍式式 Abstractions/
弛   戌式式 ISnomedCacheService.cs           # Cache service interface
戍式式 Services/
弛   戍式式 BackgroundSnomedFetcher.cs       # Background fetching engine
弛   戌式式 SqliteSnomedCacheService.cs      # SQLite cache implementation
戍式式 ViewModels/
弛   戌式式 CacheReviewViewModel.cs          # Cache review UI logic
戍式式 Views/
弛   戍式式 CacheReviewWindow.xaml           # Cache review UI
弛   戌式式 CacheReviewWindow.xaml.cs        # Cache review code-behind
戌式式 TestWindow.xaml                      # Updated with Cache Review button
```

## Usage

### From TestWindow

1. Open SnomedTools standalone test application
2. Select service mode (Mock or Real)
3. Click "Cache Review with Background Fetching" button
4. Background fetcher starts automatically
5. Review and accept/reject candidates
6. Save accepted candidates to database

### Integration with Main Application

```csharp
// Create cache service
var cacheService = new SqliteSnomedCacheService();

// Create background fetcher
var backgroundFetcher = new BackgroundSnomedFetcher(
    snowstormClient,
    cacheService,
    targetWordCount: 1  // Fetch 1-word synonyms
);

// Create ViewModel
var viewModel = new CacheReviewViewModel(
    cacheService,
    phraseService,
    snomedMapService,
    backgroundFetcher
);

// Show window
var window = new CacheReviewWindow(viewModel);
window.ShowDialog();

// Cleanup (important!)
backgroundFetcher.Dispose();
```

## Configuration

- **Default Database**: `%LocalAppData%/Wysg.Musm.SnomedTools/snomed_cache.db`
- **Default Word Count**: 1 (can be changed via `SetTargetWordCount()`)
- **Fetch Delay**: 2 seconds between API calls
- **Page Size**: 50 concepts per fetch
- **Restart Delay**: 60 seconds after all concepts processed

## Benefits

1. **No Interruption**: Fetching continues in background, user can review anytime
2. **Efficient**: Only fetches matching candidates, reduces API calls
3. **Persistent**: Cache survives application restarts
4. **Flexible**: User can accept/reject before final save
5. **Scalable**: Handles large datasets with pagination
6. **Resumable**: Can close and reopen review window without losing progress

## Future Enhancements

- Add filter by semantic tag (body structure, finding, etc.)
- Support multiple word counts simultaneously
- Add search/filter in cached candidates
- Export accepted candidates
- Import candidates from file
- Bulk accept/reject operations
- Auto-accept high confidence matches

## Dependencies

- **Microsoft.Data.Sqlite**: SQLite database support
- **CommunityToolkit.Mvvm**: MVVM helpers
- **System.Text.Json**: JSON serialization

## Testing

Run the TestWindow in Mock Mode to test with sample data without requiring network or database connections.

---

**Implementation Date**: 2025-01-21  
**Author**: GitHub Copilot  
**Status**: ? Complete & Working
