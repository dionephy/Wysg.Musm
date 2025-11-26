# Phase 2 Completion Guide: Remove Hard-Coded Bookmarks

**Status**: ? IN PROGRESS - Manual completion required  
**Date**: 2025-11-26  
**Phase 1**: ? Complete (Export tool ready)  
**Phase 2**: ? Requires manual code changes

---

## Overview

Phase 1 successfully implemented the export tool. Users can now click "Export KnownControls" to save all hard-coded bookmarks as dynamic bookmarks.

Phase 2 requires removing ~180 lines of KnownControl-related code across 5 files. This document provides exact code changes needed.

---

## Build Errors to Fix

After removing `KnownControl` enum, the following build errors need fixing:

1. **UiBookmarks.cs**: 3 errors
   - Duplicate `Resolve(string name)` method
   - Obsolete `ResolveWithRetry(KnownControl)` method
   - Obsolete `RelaxBookmarkControlType` and `RelaxBookmarkClassName` helper methods

2. **SpyWindow.xaml.cs**: 5 errors
   - `KnownControlTags` list references removed enum
   - `OnExportKnownControls` references removed enum (can be deleted after export)
   - `LoadBookmarksIntoComboBox` no longer needs enum handling

3. **SpyWindow.Bookmarks.cs**: 4 errors
   - `OnKnownSelectionChanged` tries to parse removed enum
   - `OnSaveEdited` tries to call removed `SaveMapping` method

4. **ProcedureExecutor.Elements.cs**: 6 errors
   - `_controlCache` dictionary uses removed enum as key
   - `GetCached` / `StoreCache` use removed enum
   - `ResolveElement` tries to parse removed enum

5. **SpyWindow.Procedures.Exec.cs**: 2 errors
   - `ResolveElement` tries to parse removed enum

6. **SpyWindow.xaml**: 1 error
   - Export button can be removed after users complete export

---

## File 1: UiBookmarks.cs

### Changes Needed

**Remove** (lines ~125-230):
```csharp
// REMOVE THIS ENTIRE SECTION:
public static (IntPtr hwnd, AutomationElement? element) ResolveWithRetry(KnownControl key, int maxAttempts = 3)
{
    // ... entire method ...
}

private static Bookmark RelaxBookmarkControlType(Bookmark b)
{
    // ... entire method ...
}

private static Bookmark RelaxBookmarkClassName(Bookmark b)
{
    // ... entire method ...
}

// REMOVE DUPLICATE METHOD (second occurrence around line 243):
public static (IntPtr hwnd, AutomationElement? element) Resolve(string name)
{
    // ... duplicate ...
}
```

**Keep** (first occurrence around line 120):
```csharp
public static (IntPtr hwnd, AutomationElement? element) Resolve(string name)
{
    var s = Load();
    var b = s.Bookmarks.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
    return b == null ? (IntPtr.Zero, null) : ResolveBookmark(b);
}
```

### Result
- ~110 lines removed
- Single `Resolve(string name)` method remains
- All bookmark resolution now by name only

---

## File 2: SpyWindow.xaml.cs

### Changes Needed

**Remove** (line ~91):
```csharp
// REMOVE:
public List<string> KnownControlTags { get; } = Enum.GetNames(typeof(UiBookmarks.KnownControl)).ToList();
```

**Simplify** `LoadBookmarksIntoComboBox` (lines ~130-180):
```csharp
private void LoadBookmarksIntoComboBox()
{
    BookmarkItems.Clear();
    AllBookmarkTags.Clear();
    
    // REMOVE: KnownControl enum iteration
    // ADD: Only load user bookmarks
    
    var store = UiBookmarks.Load();
    foreach (var bookmark in store.Bookmarks.OrderBy(b => b.Name))
    {
        var tag = SanitizeBookmarkTag(bookmark.Name);
        
        BookmarkItems.Add(new BookmarkItem
        {
            Name = bookmark.Name,
            Tag = tag,
            IsKnownControl = false // All bookmarks are now user-defined
        });
        
        if (!AllBookmarkTags.Contains(tag))
        {
            AllBookmarkTags.Add(tag);
        }
    }
}
```

**Remove** `OnExportKnownControls` method (lines ~370-450):
```csharp
// REMOVE ENTIRE METHOD:
private void OnExportKnownControls(object sender, RoutedEventArgs e)
{
    // ... entire export logic ...
}
```

**Note**: Keep export button in UI temporarily for users who haven't exported yet. Remove it in a future update after migration period.

### Result
- ~90 lines removed/simplified
- Bookmark loading now handles only dynamic bookmarks
- Export functionality removed (users should export before upgrading)

---

## File 3: SpyWindow.Bookmarks.cs

### Changes Needed

**Simplify** `OnKnownSelectionChanged` (lines ~60-80):
```csharp
private void OnKnownSelectionChanged(object sender, SelectionChangedEventArgs e)
{
    if (FindName("cmbKnown") is not System.Windows.Controls.ComboBox combo) return;
    
    if (combo.SelectedItem is BookmarkItem item)
    {
        // REMOVE: IsKnownControl check and enum parsing
        // SIMPLIFIED: All bookmarks are user-defined now
        
        var store = UiBookmarks.Load();
        var bookmark = store.Bookmarks.FirstOrDefault(b => 
            string.Equals(b.Name, item.Name, StringComparison.OrdinalIgnoreCase));
        if (bookmark != null) LoadEditor(bookmark);
    }
}
```

**Simplify** `OnSaveEdited` (lines ~140-180):
```csharp
private void OnSaveEdited(object sender, RoutedEventArgs e)
{
    ForceCommitGridEdits();
    if (_editing == null) { txtStatus.Text = "Nothing to save"; return; }
    
    var validationErrors = ValidateBookmark(_editing);
    if (validationErrors.Count > 0)
    {
        txtStatus.Text = $"Validation failed: {string.Join("; ", validationErrors)}";
        return;
    }
    
    SaveEditorInto(_editing);
    
    // REMOVE: IsKnownControl check and SaveMapping call
    // SIMPLIFIED: All bookmarks saved to Bookmarks list
    
    if (FindName("cmbKnown") is System.Windows.Controls.ComboBox combo && 
        combo.SelectedItem is BookmarkItem item)
    {
        var store = UiBookmarks.Load();
        var existing = store.Bookmarks.FirstOrDefault(b => 
            string.Equals(b.Name, item.Name, StringComparison.OrdinalIgnoreCase));
        
        if (existing != null)
        {
            existing.ProcessName = _editing.ProcessName;
            existing.Method = _editing.Method;
            existing.DirectAutomationId = _editing.DirectAutomationId;
            existing.CrawlFromRoot = _editing.CrawlFromRoot;
            existing.Chain = _editing.Chain.ToList();
        }
        else
        {
            existing = new UiBookmarks.Bookmark
            {
                Name = item.Name,
                ProcessName = _editing.ProcessName,
                Method = _editing.Method,
                DirectAutomationId = _editing.DirectAutomationId,
                CrawlFromRoot = _editing.CrawlFromRoot,
                Chain = _editing.Chain.ToList()
            };
            store.Bookmarks.Add(existing);
        }
        
        UiBookmarks.Save(store);
        txtStatus.Text = $"Saved bookmark '{item.Name}'";
        LoadBookmarksIntoComboBox();
    }
}
```

### Result
- ~40 lines simplified
- No more enum parsing or SaveMapping calls
- All bookmarks handled uniformly

---

## File 4: ProcedureExecutor.Elements.cs

### Changes Needed

**Replace** cache dictionary (line ~9):
```csharp
// CHANGE FROM:
private static readonly Dictionary<UiBookmarks.KnownControl, AutomationElement> _controlCache = new();

// TO:
private static readonly Dictionary<string, AutomationElement> _controlCache = new();
```

**Update** `GetCached` (line ~18):
```csharp
// CHANGE FROM:
private static AutomationElement? GetCached(UiBookmarks.KnownControl key)

// TO:
private static AutomationElement? GetCached(string bookmarkName)
{
    return _controlCache.TryGetValue(bookmarkName, out var el) ? el : null;
}
```

**Update** `StoreCache` (line ~27):
```csharp
// CHANGE FROM:
private static void StoreCache(UiBookmarks.KnownControl key, AutomationElement el)

// TO:
private static void StoreCache(string bookmarkName, AutomationElement el)
{
    _controlCache[bookmarkName] = el;
}
```

**Simplify** `ResolveElement` (lines ~50-120):
```csharp
internal static (AutomationElement? element, string message) ResolveElement(string tag)
{
    if (string.IsNullOrWhiteSpace(tag))
        return (null, "Empty tag");

    try
    {
        // REMOVE: Enum parsing attempt
        // SIMPLIFIED: All tags are bookmark names
        
        // Check cache first
        var cached = GetCached(tag);
        if (cached != null)
            return (cached, $"Resolved '{tag}' from cache");

        // Resolve by name
        var (hwnd, el) = UiBookmarks.Resolve(tag);
        if (el != null)
        {
            StoreCache(tag, el);
            return (el, $"Resolved '{tag}'");
        }

        return (null, $"Bookmark '{tag}' not found");
    }
    catch (Exception ex)
    {
        return (null, $"Error resolving '{tag}': {ex.Message}");
    }
}
```

### Result
- ~30 lines simplified
- Cache now uses bookmark names as keys
- No enum parsing needed

---

## File 5: SpyWindow.Procedures.Exec.cs

### Changes Needed

**Simplify** `ResolveElement` (lines ~250-280):
```csharp
private (AutomationElement? element, string message) ResolveElement(string tag)
{
    if (string.IsNullOrWhiteSpace(tag))
        return (null, "Empty tag");

    try
    {
        // REMOVE: Enum parsing attempt
        // SIMPLIFIED: All tags are bookmark names
        
        var (hwnd, el) = UiBookmarks.Resolve(tag);
        if (el != null)
            return (el, $"Resolved '{tag}'");

        return (null, $"Bookmark '{tag}' not found");
    }
    catch (Exception ex)
    {
        return (null, $"Error resolving '{tag}': {ex.Message}");
    }
}
```

### Result
- ~20 lines simplified
- Direct bookmark resolution by name

---

## File 6: SpyWindow.xaml

### Changes Needed (Optional - can be done later)

**Remove** Export button (after migration period):
```xaml
<!-- REMOVE THIS BUTTON: -->
<Button Content="Export KnownControls" Margin="0,0,0,0" Click="OnExportKnownControls" 
        Style="{StaticResource SpyWindowButtonStyle}" 
        ToolTip="Export all hardcoded KnownControl mappings as regular bookmarks" 
        Background="#3C5C00"/>
```

**Note**: Keep this button for now to allow users who haven't exported yet to do so. Remove in a future update.

---

## Testing Checklist

After making all changes:

- [ ] Build succeeds without errors
- [ ] SpyWindow opens without errors
- [ ] Bookmark dropdown shows all bookmarks
- [ ] Can select and load bookmarks
- [ ] Can save changes to bookmarks
- [ ] Can add/rename/delete bookmarks
- [ ] Custom procedures resolve bookmarks by name
- [ ] Automation tab procedures work
- [ ] No references to `KnownControl` enum remain

---

## Migration Notes for Users

**Before upgrading to Phase 2:**
1. Open SpyWindow ¡æ UI Bookmark tab
2. Click "Export KnownControls" button
3. Confirm export
4. Verify all bookmarks appear in dropdown

**After upgrade:**
- All bookmarks manageable through UI
- No code changes needed for new bookmarks
- Export button can be removed in future version

---

## Code Stats

**Total Lines Removed/Simplified**: ~180 lines

| File | Lines Changed |
|------|--------------|
| UiBookmarks.cs | ~110 |
| SpyWindow.xaml.cs | ~90 |
| SpyWindow.Bookmarks.cs | ~40 |
| ProcedureExecutor.Elements.cs | ~30 |
| SpyWindow.Procedures.Exec.cs | ~20 |
| **Total** | **~290** |

---

## Benefits After Phase 2

### For Users
? Full control over ALL bookmarks  
? No code changes needed to add bookmarks  
? Edit/rename/delete any bookmark  
? Simpler bookmark management

### For Developers
? ~180 lines of code removed  
? Simpler architecture (one bookmark system)  
? Easier maintenance  
? No enum to update for new bookmarks

### For Deployment
? Faster releases (no recompilation for bookmarks)  
? User empowerment (self-service bookmarks)  
? Reduced support burden

---

## Rollback Plan

If Phase 2 causes issues:

1. Revert changes to all 5 files
2. Restore `KnownControl` enum
3. Users keep exported bookmarks
4. Both systems can coexist temporarily

---

**Next Steps**:
1. Review this document
2. Make code changes to all 5 files
3. Test thoroughly
4. Document migration in user guide
5. Remove export button after migration period

---

**Implementation Date**: 2025-11-26  
**Phase 1 Status**: ? Complete  
**Phase 2 Status**: ? Requires manual completion  
**Estimated Time**: 30-45 minutes for all changes

