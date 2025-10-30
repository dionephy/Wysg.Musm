# Cross-Thread Access Fix - UpdateCommandStates

## Issue

**Error:** "다른 스레드가 이 개체를 소유하고 있어 호출 스레드가 해당 개체에 액세스할 수 없습니다"  
**Translation:** "Calling thread cannot access this object because a different thread owns it"

**Location:** `UpdateCommandStates()` method  
**Cause:** Method was being called from background thread during `RefreshListsAsync()`, but WPF commands require UI thread access

## Root Cause

```csharp
private async Task RefreshListsAsync()
{
    // This runs on background thread after ConfigureAwait(false)
    var pending = await _cacheService.GetPendingCandidatesAsync(100).ConfigureAwait(false);
    
    // Categorize (still on background thread)
    var organisms = pending.Where(...).ToList();
    
    // Update collections (needs UI thread!)
    OrganismCandidates.Clear(); // ? Cross-thread access!
    
    // Update commands (needs UI thread!)
    UpdateCommandStates(); // ? Cross-thread access!
}
```

The problem:
1. `ConfigureAwait(false)` allows continuation on background thread
2. `ObservableCollection` operations require UI thread in WPF
3. `ICommand.NotifyCanExecuteChanged()` requires UI thread

## Solution

### 1. Thread-Safe UpdateCommandStates

Added dispatcher check to ensure UI thread execution:

```csharp
private void UpdateCommandStates()
{
    // Check if we're on UI thread
    if (!_dispatcher.CheckAccess())
    {
        // If not, marshal to UI thread
        _dispatcher.InvokeAsync(() => UpdateCommandStates());
        return;
    }

    // Now safe to update commands
    ((AsyncRelayCommand)AcceptSelectedOrganismsCommand).NotifyCanExecuteChanged();
    ((AsyncRelayCommand)RejectSelectedOrganismsCommand).NotifyCanExecuteChanged();
    ((AsyncRelayCommand)AcceptSelectedSubstancesCommand).NotifyCanExecuteChanged();
    ((AsyncRelayCommand)RejectSelectedSubstancesCommand).NotifyCanExecuteChanged();
    ((AsyncRelayCommand)AcceptSelectedOthersCommand).NotifyCanExecuteChanged();
    ((AsyncRelayCommand)RejectSelectedOthersCommand).NotifyCanExecuteChanged();
    ((AsyncRelayCommand)SaveAllAcceptedCommand).NotifyCanExecuteChanged();
}
```

**Key Points:**
- `Dispatcher.CheckAccess()` returns `true` if already on UI thread
- If on background thread, uses `InvokeAsync()` to marshal to UI thread
- Prevents cross-thread access exceptions

### 2. Proper Thread Marshaling in RefreshListsAsync

Wrapped ALL UI updates in `InvokeAsync`:

```csharp
private async Task RefreshListsAsync()
{
    // Background work (safe - no UI access)
    var pending = await _cacheService.GetPendingCandidatesAsync(100).ConfigureAwait(false);
    PendingCount = pending.Count;
    var organisms = pending.Where(c => GetCandidateCategory(c) == Organism).ToList();
    var substances = pending.Where(c => GetCandidateCategory(c) == Substance).ToList();
    var others = pending.Where(c => GetCandidateCategory(c) == Other).ToList();

    // ALL UI work wrapped in InvokeAsync
    await _dispatcher.InvokeAsync(() =>
    {
        // Unsubscribe old items
        foreach (var item in OrganismCandidates)
            item.PropertyChanged -= OnSelectablePropertyChanged;
        // ... (same for other collections)

        // Clear and populate collections (safe - on UI thread now)
        OrganismCandidates.Clear();
        foreach (var candidate in organisms)
        {
            var selectable = new SelectableCachedCandidate(candidate);
            selectable.PropertyChanged += OnSelectablePropertyChanged;
            OrganismCandidates.Add(selectable);
        }
        // ... (same for other collections)

        // Update command states (safe - on UI thread now)
        UpdateCommandStates();
    });
}
```

**Benefits:**
- ? All ObservableCollection operations on UI thread
- ? All event subscriptions on UI thread
- ? All command updates on UI thread
- ? No cross-thread exceptions

## Technical Details

### Dispatcher.CheckAccess()
```csharp
// Returns true if current thread is the UI thread
bool isUIThread = _dispatcher.CheckAccess();
```

### Dispatcher.InvokeAsync()
```csharp
// Marshals action to UI thread asynchronously
await _dispatcher.InvokeAsync(() => 
{
    // Code here runs on UI thread
});
```

### Why This Happens

WPF dependency objects (like `ObservableCollection`, `ICommand`) have **thread affinity**:
- Created on UI thread
- Can only be accessed from UI thread
- Cross-thread access throws `InvalidOperationException`

`ConfigureAwait(false)` is commonly used to:
- Avoid deadlocks in async code
- Improve performance by not capturing context

But it means:
- Continuation may run on background thread
- Must explicitly marshal back to UI thread for UI operations

## Testing

### Before Fix:
```
1. Open Cache Review window
2. Click "Refresh"
3. ? Exception: "다른 스레드가 이 개체를 소유하고 있어..."
4. Window crashes or becomes unresponsive
```

### After Fix:
```
1. Open Cache Review window
2. Click "Refresh"
3. ? Lists populate correctly
4. ? Commands update properly
5. ? No exceptions
```

## Similar Issues Fixed

This pattern should be applied to:
- ? `RefreshAsync()` - Already uses `_dispatcher.InvokeAsync`
- ? `AcceptSelectedAsync()` - Already uses `_dispatcher.InvokeAsync`
- ? `RejectSelectedAsync()` - Already uses `_dispatcher.InvokeAsync`
- ? `SaveAllAcceptedAsync()` - Already uses `_dispatcher.InvokeAsync`
- ? `UpdateCommandStates()` - Now thread-safe

## Best Practices

### DO:
```csharp
// ? Wrap all UI updates in InvokeAsync
await _dispatcher.InvokeAsync(() =>
{
    ObservableCollection.Add(item);
    PropertyValue = newValue;
    Command.NotifyCanExecuteChanged();
});
```

### DON'T:
```csharp
// ? Direct UI update from background thread
var result = await DatabaseCallAsync().ConfigureAwait(false);
ObservableCollection.Add(result); // Exception!
```

### PATTERN:
```csharp
private async Task SomeMethodAsync()
{
    // 1. Do background work
    var data = await GetDataAsync().ConfigureAwait(false);
    var processed = ProcessData(data); // Still on background thread
    
    // 2. Marshal to UI thread for UI updates
    await _dispatcher.InvokeAsync(() =>
    {
        // All UI updates here
        Collection.Clear();
        foreach (var item in processed)
            Collection.Add(item);
    });
}
```

## Prevention

To avoid future cross-thread issues:

1. **Always check thread context** when updating UI
2. **Use `_dispatcher.InvokeAsync()`** for all UI updates after async operations
3. **Avoid `ConfigureAwait(false)`** if you need UI thread immediately after
4. **Keep UI updates minimal** - process data on background thread, update UI once
5. **Test with debugger** - cross-thread exceptions are easy to catch

## Performance Impact

**Minimal:**
- `CheckAccess()` is very fast (simple thread ID check)
- `InvokeAsync()` only marshals if needed
- UI updates should be batched anyway for best performance

## Related Documentation

- WPF Threading Model: https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/threading-model
- Dispatcher Class: https://learn.microsoft.com/en-us/dotnet/api/system.windows.threading.dispatcher
- ConfigureAwait: https://devblogs.microsoft.com/dotnet/configureawait-faq/

---

**Issue Date**: 2025-01-21  
**Status**: ? Fixed  
**Impact**: Critical - Prevented window crashes  
**Fix Type**: Thread synchronization
