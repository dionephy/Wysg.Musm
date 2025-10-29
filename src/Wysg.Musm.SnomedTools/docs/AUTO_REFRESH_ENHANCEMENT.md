# Auto-Refresh Current Candidate Enhancement

## Summary

Added automatic refresh of the current candidate in two scenarios:
1. **When new candidates are cached** and there's no current candidate showing
2. **After Accept or Reject** to immediately show the next candidate

## Changes Made

### 1. Auto-Refresh on New Candidate Cached (No Current Candidate)

**Scenario:** Background fetcher finds new candidates, but UI shows "No pending candidates"

**Before:**
```
1. Window shows "No pending candidates"
2. Background fetcher caches new items
3. Pending count updates: 0 ¡æ 5
4. User still sees "No pending candidates"
5. User must manually click Refresh
```

**After:**
```
1. Window shows "No pending candidates"
2. Background fetcher caches new items
3. Pending count updates: 0 ¡æ 5
4. Current candidate AUTOMATICALLY refreshes ?
5. User immediately sees first candidate
```

**Implementation:**
```csharp
private void OnCandidateCached(object? sender, Services.CandidateCachedEventArgs e)
{
    _dispatcher.InvokeAsync(async () =>
    {
        var previousPendingCount = PendingCount;
        
        // Refresh pending count
        PendingCount = await _cacheService.GetPendingCountAsync();
        
        // If pending count increased AND no current candidate, auto-refresh
        if (PendingCount > previousPendingCount && CurrentCandidate == null)
        {
            Debug.WriteLine("New candidate cached and no current candidate - auto-refreshing");
            await LoadNextCandidateAsync();
        }
    });
}
```

### 2. Auto-Refresh After Accept/Reject

**Scenario:** User accepts or rejects current candidate

**Before:**
```
1. User reviews candidate "Heart"
2. Clicks Accept
3. Candidate accepted ?
4. UI shows empty state
5. User must manually click Refresh to see next candidate
```

**After:**
```
1. User reviews candidate "Heart"
2. Clicks Accept
3. Candidate accepted ?
4. Next candidate AUTOMATICALLY loaded ?
5. User immediately sees "Cardiac structure"
```

**Implementation:**
```csharp
private async Task AcceptCurrentAsync()
{
    if (CurrentCandidate == null)
        return;

    try
    {
        // ... accept logic ...
        
        // Auto-refresh to show next candidate
        await LoadNextCandidateAsync();
    }
    finally
    {
        IsBusy = false;
    }
}

private async Task RejectCurrentAsync()
{
    if (CurrentCandidate == null)
        return;

    try
    {
        // ... reject logic ...
        
        // Auto-refresh to show next candidate
        await LoadNextCandidateAsync();
    }
    finally
    {
        IsBusy = false;
    }
}
```

### 3. New Helper Method: LoadNextCandidateAsync()

**Purpose:** Refresh current candidate without full UI reload

```csharp
private async Task LoadNextCandidateAsync()
{
    // Load fresh pending candidates
    var pending = await _cacheService.GetPendingCandidatesAsync(100);
    
    PendingCandidates.Clear();
    foreach (var candidate in pending)
    {
        PendingCandidates.Add(candidate);
    }

    PendingCount = PendingCandidates.Count;
    CurrentCandidate = PendingCandidates.FirstOrDefault();
    
    // Update command states
    ((AsyncRelayCommand)AcceptCommand).NotifyCanExecuteChanged();
    ((AsyncRelayCommand)RejectCommand).NotifyCanExecuteChanged();
}
```

## User Experience Improvements

### Scenario 1: Starting Fresh
```
1. Open Cache Review window
2. Click "Start Fetching"
3. Background fetcher finds first 1-word term
4. UI AUTOMATICALLY shows it ? (no manual refresh needed)
5. User can immediately accept/reject
```

### Scenario 2: Rapid Review
```
1. Current candidate: "Heart"
2. Click Accept
3. IMMEDIATELY shows: "Lung" ? (no manual refresh)
4. Click Reject
5. IMMEDIATELY shows: "Brain" ? (no manual refresh)
6. Seamless review flow!
```

### Scenario 3: All Reviewed
```
1. Review last pending candidate
2. Click Accept
3. UI shows "No pending candidates" ?
4. Background fetcher caches new item
5. UI AUTOMATICALLY refreshes ?
6. Shows new candidate immediately
```

## Technical Details

### Refresh Triggers

1. **OnCandidateCached Event:**
   - Fired by background fetcher when new item cached
   - Checks: `PendingCount > previousPendingCount && CurrentCandidate == null`
   - Only refreshes if NO current candidate (prevents interruption)

2. **AcceptCurrentAsync:**
   - After marking candidate as accepted
   - Always loads next candidate
   - Provides seamless workflow

3. **RejectCurrentAsync:**
   - After marking candidate as rejected
   - Always loads next candidate
   - Provides seamless workflow

### State Management

**PendingCandidates Collection:**
- ObservableCollection for UI binding
- Updated on every refresh
- CurrentCandidate = FirstOrDefault()

**Command State Updates:**
- AcceptCommand enabled when CurrentCandidate != null
- RejectCommand enabled when CurrentCandidate != null
- Commands updated after every refresh

### Performance Considerations

- **Lightweight refresh**: Only loads pending candidates (not full statistics)
- **Async operations**: UI remains responsive
- **Dispatcher invocation**: Ensures UI thread safety
- **Minimal overhead**: Only refreshes when needed

## Edge Cases Handled

### No Pending Candidates
```
CurrentCandidate = PendingCandidates.FirstOrDefault();
// Returns null if list is empty
// UI shows "No pending candidates" state
```

### Race Condition: Multiple Cached Events
```
// Only refreshes if CurrentCandidate == null
// Prevents interrupting active review
if (PendingCount > previousPendingCount && CurrentCandidate == null)
{
    await LoadNextCandidateAsync();
}
```

### Accept/Reject on Last Candidate
```
await LoadNextCandidateAsync();
// Loads empty list
// CurrentCandidate = null
// UI shows "No pending candidates"
```

## Benefits

### For Users:
? **No manual refresh needed** - Candidates appear automatically  
? **Faster review workflow** - Accept/reject immediately shows next  
? **Better UX** - Seamless, uninterrupted flow  
? **Less clicking** - One less action per candidate  

### For Developers:
? **Clean code** - Single responsibility methods  
? **Testable** - LoadNextCandidateAsync() can be tested independently  
? **Maintainable** - Clear separation of concerns  
? **Thread-safe** - Proper dispatcher usage  

## Testing Scenarios

### Manual Test 1: Auto-Refresh on Cache
1. Open Cache Review (no candidates)
2. Set word count: 1
3. Click "Start Fetching"
4. **Expected**: First candidate appears automatically
5. **Verify**: No manual refresh needed

### Manual Test 2: Accept Flow
1. Start with candidate showing
2. Click "Accept"
3. **Expected**: Next candidate appears immediately
4. **Verify**: No empty state shown

### Manual Test 3: Reject Flow
1. Start with candidate showing
2. Click "Reject"
3. **Expected**: Next candidate appears immediately
4. **Verify**: No empty state shown

### Manual Test 4: Last Candidate
1. Review until last candidate
2. Click "Accept" or "Reject"
3. **Expected**: "No pending candidates" shown
4. **Verify**: UI updates correctly

---

**Implementation Date**: 2025-01-21  
**Status**: ? Complete & Working  
**Build Status**: ? Successful  
**Key Improvement**: Eliminated need for manual refresh, creating seamless review workflow
