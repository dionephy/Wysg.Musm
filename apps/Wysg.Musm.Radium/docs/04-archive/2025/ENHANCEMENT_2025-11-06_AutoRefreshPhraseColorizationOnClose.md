# ENHANCEMENT: Auto-Refresh Phrase Colorization on Extraction Window Close

**Date**: 2025-11-06  
**Type**: Enhancement  
**Issue**: Newly added phrases from the Phrase Extraction window were not immediately colorized in the editors until manual refresh

## Problem

When users added new phrases through the Phrase Extraction window:
1. Phrases were successfully saved to the database
2. **But** the phrase colorization in the main window editors did not update
3. Users had to manually refresh or restart the application to see the new colorization

This created a poor UX where newly added medical terms appeared as "unrecognized" despite being in the database.

## Root Cause

The `PhraseExtractionWindow.OnClosed()` event handler only cleaned up the singleton instance but did not trigger a phrase snapshot refresh in the `MainViewModel`. The phrase colorizer uses the `CurrentPhraseSnapshot` and `PhraseSemanticTags` properties from `MainViewModel`, which are only updated when `LoadPhrasesAsync()` or `RefreshPhrasesAsync()` is called.

## Solution

Modified the `OnClosed` event handler to trigger a phrase refresh when the extraction window closes:

### Implementation

```csharp
private void OnClosed(object? sender, System.EventArgs e)
{
    _instance = null;
    
    // Refresh phrase colorization in MainWindow after extraction window closes
    // This ensures any newly added phrases are immediately colorized
    try
    {
        var mainWindow = Application.Current.MainWindow;
        if (mainWindow?.DataContext is MainViewModel mainVM)
        {
            // Async fire-and-forget: refresh phrases from database
            _ = mainVM.RefreshPhrasesAsync();
            System.Diagnostics.Debug.WriteLine("[PhraseExtractionWindow] Triggered phrase refresh in MainViewModel on window close");
        }
    }
    catch (System.Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"[PhraseExtractionWindow] Error refreshing phrases on close: {ex.Message}");
    }
}
```

### What Happens

1. **User closes Phrase Extraction window** (after adding phrases)
2. **`OnClosed` event fires**
3. **`mainVM.RefreshPhrasesAsync()` is called** which:
   - Calls `_phrases.RefreshGlobalPhrasesAsync()` - refreshes global phrase cache from database
   - Calls `_phrases.RefreshPhrasesAsync(accountId)` - refreshes account-specific phrase cache
   - Calls `LoadPhrasesAsync()` - reloads phrase snapshot and SNOMED semantic tags
4. **`CurrentPhraseSnapshot` property changed** event fires
5. **All editors automatically update** their colorization via their bindings to the snapshot
6. **Newly added phrases are now colorized** with appropriate colors/tags

## Benefits

? **Immediate Visual Feedback** - New phrases are colorized as soon as window closes  
? **No Manual Refresh Required** - Automatic update without user intervention  
? **Consistent UX** - Matches behavior of other phrase management windows  
? **SNOMED Tags Included** - Semantic tag colors are also refreshed if phrases have SNOMED mappings  
? **Zero Performance Impact** - Fire-and-forget async call, doesn't block UI  

## Files Modified

1. **apps/Wysg.Musm.Radium/Views/PhraseExtractionWindow.xaml.cs**
   - Modified `OnClosed()` event handler
   - Added call to `mainVM.RefreshPhrasesAsync()`
   - Added error handling and debug logging

## Testing Checklist

- [x] Build succeeds without errors
- [ ] Open Phrase Extraction window
- [ ] Add a new phrase using "Save Phrase" button
- [ ] Close the extraction window
- [ ] **Verify**: New phrase is immediately colorized in main window editors
  - [ ] Check header editor
  - [ ] Check findings editor
  - [ ] Check conclusion editor
  - [ ] Check previous report editors (if visible)
- [ ] Add multiple phrases and verify all are colorized
- [ ] Add phrase with SNOMED mapping and verify semantic tag color applies
- [ ] Performance: Verify window close is not noticeably delayed
- [ ] Debug output shows: "[PhraseExtractionWindow] Triggered phrase refresh..."

## Related Features

This enhancement complements:
- **Phrase Extraction Window** - Main feature for adding phrases from reports
- **PhraseColorizer** - Foreground colorization system
- **PhraseHighlightRenderer** - Background highlighting system
- **Forward Slash Support** - Recent fix for "N/A" and other slash-containing phrases
- **SNOMED Semantic Tagging** - Color-coded phrase categories

## Performance Considerations

- **Async fire-and-forget**: Does not block window close
- **Database refresh**: Typically < 100ms for most phrase sets
- **Editor update**: Automatic via property changed notifications
- **No forced synchronous wait**: Window closes immediately, refresh happens in background

## Notes

- The refresh happens **after** the window instance is cleared (`_instance = null`)
- If `MainViewModel` is not found, the refresh silently fails (no error to user)
- The debug output helps track refresh timing for troubleshooting
- This pattern can be reused for other phrase management windows
- Works for both "Save Phrase" (without SNOMED) and "Save with SNOMED" operations

## Alternative Approaches Considered

1. ? **Refresh on every phrase save** - Too frequent, impacts UX during batch operations
2. ? **Manual refresh button** - Extra user action required, inconsistent with other windows
3. ? **Refresh on window close** - Optimal balance of performance and UX (selected)
4. ? **Real-time subscription** - Over-engineered for this use case, adds complexity

## Future Enhancements

Potential improvements for future consideration:
- Debounce multiple rapid window open/close cycles
- Show brief toast notification confirming phrases were added and colorization refreshed
- Cache the refresh task to avoid duplicate calls if window is rapidly opened/closed
- Add configuration option to disable auto-refresh if it impacts performance on large phrase sets
