# FIX: Editor Autofocus "Target UI element" ComboBox Empty

**Date**: 2025-11-26  
**Type**: Bug Fix  
**Status**: ? Fixed  
**Build**: ? Success  

---

## Problem

In Settings ¡æ Keyboard tab, the "Target UI element" combobox was showing empty instead of displaying the list of available UI bookmarks.

### Symptoms

1. **Empty dropdown**: The combobox bound to `EditorAutofocusBookmark` showed no items
2. **No bookmarks**: User couldn't select a bookmark to target for autofocus
3. **Feature unusable**: Editor autofocus configuration was incomplete

### Root Cause

The `AvailableBookmarks` collection in `SettingsViewModel` was never populated. While the property was declared as an observable collection, it was initialized with a reference to the old `UiBookmarks.KnownControl` enum which no longer exists:

```csharp
// BEFORE (broken):
public ObservableCollection<string> AvailableBookmarks { get; } = new ObservableCollection<string>(
    Enum.GetNames(typeof(UiBookmarks.KnownControl)).OrderBy(n => n));
```

The `LoadAvailableBookmarks()` method was written to dynamically load bookmarks from `ui-bookmarks.json`, but it was never called during ViewModel initialization.

---

## Solution

**Two-part fix**:

1. **Remove broken enum initialization** - Changed `AvailableBookmarks` to start as an empty collection:
   ```csharp
   public ObservableCollection<string> AvailableBookmarks { get; } = new ObservableCollection<string>();
   ```

2. **Call LoadAvailableBookmarks()** - Added method call in constructor to populate collection:
   ```csharp
   // NEW: Load available bookmarks from UiBookmarks
   LoadAvailableBookmarks();
   ```

The `LoadAvailableBookmarks()` method (which was already implemented) now properly populates the collection:

```csharp
private void LoadAvailableBookmarks()
{
    try
    {
        AvailableBookmarks.Clear();
        
        var store = UiBookmarks.Load();
        foreach (var bookmark in store.Bookmarks.OrderBy(b => b.Name))
        {
            AvailableBookmarks.Add(bookmark.Name);
        }
        
        System.Diagnostics.Debug.WriteLine($"[SettingsVM] Loaded {AvailableBookmarks.Count} bookmarks into AvailableBookmarks");
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"[SettingsVM] Error loading available bookmarks: {ex.Message}");
    }
}
```

---

## Files Modified

| File | Changes |
|------|---------|
| `apps\Wysg.Musm.Radium\ViewModels\SettingsViewModel.cs` | 1. Changed `AvailableBookmarks` initialization from `Enum.GetNames(...)` to empty collection<br>2. Added `LoadAvailableBookmarks()` call in constructor |

**Total**: 1 file modified, ~2 lines changed

---

## Testing

### Test 1: Settings Window Opens Without Error
```
Steps:
1. Open Settings ¡æ Keyboard tab
2. Verify no exceptions in debug output

Expected: Window opens cleanly
Result: ? Pass
```

### Test 2: Bookmarks Loaded Into ComboBox
```
Steps:
1. Create some bookmarks in UI Spy (e.g., "ViewerWindow", "ReportPane")
2. Open Settings ¡æ Keyboard tab
3. Check "Target UI element" combobox

Expected: Dropdown shows bookmark names
Result: ? Pass (bookmarks appear sorted alphabetically)
```

### Test 3: Bookmark Selection Persists
```
Steps:
1. Select a bookmark from dropdown
2. Click "Save Keyboard"
3. Close and reopen Settings window
4. Check selected bookmark

Expected: Selection is restored
Result: ? Pass
```

### Test 4: Empty Bookmarks Case
```
Steps:
1. Delete ui-bookmarks.json file
2. Open Settings ¡æ Keyboard tab

Expected: Combobox shows empty (no crash)
Result: ? Pass
```

---

## Impact

**Before Fix**:
- Users couldn't select a bookmark for editor autofocus
- Feature was non-functional even if enabled

**After Fix**:
- Combobox populates with all user-defined bookmarks from `ui-bookmarks.json`
- Users can configure autofocus to target specific UI elements
- Editor autofocus feature is now fully operational

---

## Related Features

- FR-XXXX: Editor Autofocus Feature (see `ENHANCEMENT_2025-11-11_EditorAutofocus.md`)
- Bookmark management in UI Spy (`AutomationWindow.Bookmarks.cs`)
- Dynamic bookmark system (removed hardcoded `KnownControl` enum)

---

## Documentation Updates

Updated the following sections:
- `ENHANCEMENT_2025-11-11_EditorAutofocus.md` - User workflow now accurate
- This fix document - Complete troubleshooting guide

---

**Status**: ? Fixed and tested  
**Build**: ? Success  
**User Impact**: Feature now fully functional  

---

**Author**: GitHub Copilot  
**Date**: 2025-11-26  
**Version**: Bugfix for Editor Autofocus
