# Enhancement: Dynamic Bookmarks in Procedure Operation Grid

**Date**: 2025-11-26  
**Status**: ? Implemented  
**Category**: UI Automation / Custom Procedures

---

## Summary

Dynamic user-created bookmarks now appear in the procedure operation grid's Arg selection dropdown, alongside predefined KnownControl bookmarks. Each dynamic bookmark is automatically assigned a sanitized tag (without spaces) for use in code.

---

## Problem

### Before
- Only predefined KnownControl enum values appeared in procedure Arg ComboBoxes
- Users could create dynamic bookmarks in the UI Bookmark tab
- BUT these dynamic bookmarks were NOT available for selection in custom procedures
- No way to reference user-created bookmarks in automation code

### User Impact
```
User creates bookmark: "My Custom Button"
User opens Procedure tab -> wants to use bookmark in GetText operation
Arg1 ComboBox shows: Only SearchResultsList, ReportText, etc. (KnownControls)
"My Custom Button" is NOT visible ?
User cannot use their custom bookmark in procedures
```

---

## Solution

### Automatic Tag Generation

When users create or load dynamic bookmarks, the system now:
1. Generates a sanitized tag from the bookmark name
2. Adds the tag to a combined collection (`AllBookmarkTags`)
3. Makes the tag available in all procedure Arg ComboBoxes

### Tag Sanitization Rules

```csharp
Input: "My Custom Button"
Output: "My_Custom_Button"

Input: "Search Results List (Web)"
Output: "Search_Results_List_Web"

Input: "123-Button"
Output: "Bookmark_123_Button"  // Prepends "Bookmark_" if starts with number

Input: "Test___Multiple___Underscores"
Output: "Test_Multiple_Underscores"  // Removes consecutive underscores
```

**Rules**:
- Replace spaces with underscores
- Remove non-alphanumeric characters (except underscore)
- Remove consecutive underscores
- Trim leading/trailing underscores
- Prepend "Bookmark_" if starts with number

---

## Technical Implementation

### 1. New Collection

**SpyWindow.xaml.cs**:
```csharp
// NEW: Combined collection for procedure Arg ComboBoxes
public ObservableCollection<string> AllBookmarkTags { get; } = new();
```

### 2. Population Logic

**LoadBookmarksIntoComboBox()**:
```csharp
private void LoadBookmarksIntoComboBox()
{
    BookmarkItems.Clear();
    AllBookmarkTags.Clear(); // NEW
    
    // Add KnownControls
    foreach (UiBookmarks.KnownControl knownCtrl in Enum.GetValues(typeof(UiBookmarks.KnownControl)))
    {
        var name = knownCtrl.ToString();
        // ... format display name ...
        AllBookmarkTags.Add(name); // NEW: Add to combined collection
    }
    
    // Add user bookmarks with generated tags
    var store = UiBookmarks.Load();
    foreach (var bookmark in store.Bookmarks.OrderBy(b => b.Name))
    {
        var tag = SanitizeBookmarkTag(bookmark.Name); // NEW: Generate tag
        
        BookmarkItems.Add(new BookmarkItem
        {
            Name = bookmark.Name,
            Tag = tag, // NEW: Store tag
            IsKnownControl = false
        });
        
        if (!AllBookmarkTags.Contains(tag))
        {
            AllBookmarkTags.Add(tag); // NEW: Add to combined collection
        }
    }
}
```

### 3. XAML Binding Update

**SpyWindow.xaml** - Changed from `KnownControlTags` to `AllBookmarkTags`:

```xaml
<!-- Before -->
<ComboBox ItemsSource="{Binding DataContext.KnownControlTags, ElementName=Root}" .../>

<!-- After -->
<ComboBox ItemsSource="{Binding DataContext.AllBookmarkTags, ElementName=Root}" .../>
```

Applied to:
- Arg1 Element ComboBox
- Arg2 Element ComboBox
- Arg3 Element ComboBox

### 4. Element Resolution

**SpyWindow.Procedures.Exec.cs**:
```csharp
private AutomationElement? ResolveElement(ProcArg arg, Dictionary<string, string?> vars)
{
    var type = ParseArgKind(arg.Type);
    
    if (type == ArgKind.Element)
    {
        var tag = arg.Value ?? string.Empty;
        
        // Try KnownControl first
        if (Enum.TryParse<UiBookmarks.KnownControl>(tag, out var key))
        {
            var tuple = UiBookmarks.Resolve(key);
            return tuple.element;
        }
        
        // NEW: If not KnownControl, try dynamic bookmark by tag/name
        var bookmarkByTag = UiBookmarks.Resolve(tag);
        if (bookmarkByTag.element != null)
        {
            return bookmarkByTag.element;
        }
        
        return null;
    }
    // ... Var type handling ...
}
```

**ProcedureExecutor.Elements.cs**: Same resolution logic for runtime execution

---

## User Workflow

### Step 1: Create Dynamic Bookmark

```
1. Open SpyWindow -> UI Bookmark tab
2. Use "Pick" or "Pick Web" to capture element
3. Click "+" button
4. Enter name: "My Custom Button"
5. Click Save

Status shows: "Added bookmark 'My Custom Button' with tag 'My_Custom_Button'"
```

### Step 2: Use in Procedure

```
1. Open SpyWindow -> Procedure tab
2. Select custom procedure
3. Add operation: GetText
4. Set Arg1 Type = Element
5. Click Arg1 dropdown

ComboBox now shows:
  - SearchResultsList (KnownControl)
  - ReportText (KnownControl)
  - ...
  - My_Custom_Button (Dynamic) ? NEW!
  
6. Select "My_Custom_Button"
7. Click "Set" to execute
8. Operation resolves bookmark and gets text
```

---

## Examples

### Example 1: Web Button Bookmark

```
Bookmark Name: "Submit Report Button (Web)"
Generated Tag: "Submit_Report_Button_Web"

Usage in Procedure:
Operation: ClickElement
Arg1 Type: Element
Arg1 Value: Submit_Report_Button_Web
Result: Clicks the button ?
```

### Example 2: Custom Field Bookmark

```
Bookmark Name: "Patient Age Field"
Generated Tag: "Patient_Age_Field"

Usage in Procedure:
Operation: GetText
Arg1 Type: Element
Arg1 Value: Patient_Age_Field
Result: Gets age value from field ?
```

### Example 3: Complex Name with Special Characters

```
Bookmark Name: "Report #1 (Findings) - Section 2.1"
Generated Tag: "Report_1_Findings_Section_2_1"

Usage in Procedure:
Operation: SetValue
Arg1 Type: Element
Arg1 Value: Report_1_Findings_Section_2_1
Arg2 Type: String
Arg2 Value: "No abnormality detected"
Result: Sets value in findings field ?
```

---

## Benefits

### For Users
- ? **Reusable bookmarks**: Create once, use everywhere
- ? **No manual tagging**: Tags generated automatically
- ? **Consistent naming**: Sanitization ensures valid identifiers
- ? **Visible feedback**: Status shows generated tag
- ? **Unified dropdown**: KnownControls and dynamic bookmarks together

### For Procedures
- ? **More flexible**: Reference any saved bookmark
- ? **Code-friendly**: Tags work in automation scripts
- ? **Type-safe**: Same resolution as KnownControls
- ? **Cached resolution**: No performance impact

### For Development
- ? **Clean architecture**: Single collection for all bookmarks
- ? **Backward compatible**: Existing procedures still work
- ? **Easy extension**: Add more bookmark types later

---

## Tag Storage

### BookmarkItem Model

```csharp
public class BookmarkItem : INotifyPropertyChanged
{
    public string Name { get; set; }           // Display: "My Custom Button"
    public string? Tag { get; set; }           // Code: "My_Custom_Button"
    public bool IsKnownControl { get; set; }   // false for dynamic bookmarks
}
```

### JSON Storage (ui-bookmarks.json)

```json
{
  "Bookmarks": [
    {
      "Name": "My Custom Button",
      "ProcessName": "msedge",
      "Method": 0,
      "Chain": [...]
    }
  ]
}
```

**Note**: Tags are NOT stored in JSON - they're regenerated on load to ensure consistency with sanitization rules.

---

## Testing Checklist

### Bookmark Creation
- [x] Create bookmark with spaces ¡æ tag has underscores
- [x] Create bookmark with special chars ¡æ tag sanitized
- [x] Create bookmark starting with number ¡æ tag prefixed
- [x] Status message shows generated tag
- [x] Bookmark appears in AllBookmarkTags collection

### Procedure Usage
- [x] Dynamic bookmark appears in Arg1 ComboBox
- [x] Dynamic bookmark appears in Arg2 ComboBox
- [x] Dynamic bookmark appears in Arg3 ComboBox
- [x] Selecting dynamic bookmark resolves element
- [x] Operations execute successfully with dynamic bookmarks

### Resolution
- [x] KnownControl resolution still works
- [x] Dynamic bookmark resolution works
- [x] Failed resolution returns null (no crash)
- [x] Cache behavior unchanged for KnownControls

### Edge Cases
- [x] Bookmark with only special characters ¡æ "Bookmark"
- [x] Very long bookmark name ¡æ full tag generated
- [x] Duplicate tags ¡æ handled by Contains() check
- [x] Rename bookmark ¡æ tag regenerated
- [x] Delete bookmark ¡æ tag removed from collection

---

## Limitations

1. **Tags not editable**: Users cannot customize tags (auto-generated only)
2. **No persistent tags**: Tags regenerated on each load (not stored in JSON)
3. **Potential conflicts**: Different names might generate same tag (rare)
4. **No validation**: System doesn't prevent tag collisions

### Future Enhancements

1. **Manual tag editing**: Allow users to customize generated tags
2. **Tag preview**: Show tag in bookmark editor before saving
3. **Conflict detection**: Warn when tag already exists
4. **Tag aliases**: Multiple tags for same bookmark
5. **Export/import**: Share bookmarks with tags preserved

---

## Files Modified

1. **SpyWindow.xaml.cs** (~50 lines added)
   - Added `AllBookmarkTags` collection
   - Added `SanitizeBookmarkTag()` method
   - Updated `LoadBookmarksIntoComboBox()` to populate tags
   - Updated `OnAddBookmark()` to show generated tag

2. **SpyWindow.xaml** (3 ComboBox bindings changed)
   - Changed Arg1 Element ComboBox from `KnownControlTags` to `AllBookmarkTags`
   - Changed Arg2 Element ComboBox from `KnownControlTags` to `AllBookmarkTags`
   - Changed Arg3 Element ComboBox from `KnownControlTags` to `AllBookmarkTags`

3. **SpyWindow.Procedures.Exec.cs** (~10 lines added)
   - Updated `ResolveElement()` to handle dynamic bookmarks

4. **ProcedureExecutor.Elements.cs** (~10 lines added)
   - Updated `ResolveElement()` to handle dynamic bookmarks

---

## Related Features

- **Dynamic UI Bookmarks** (ENHANCEMENT_2025-11-25_DynamicUIBookmarks.md)
  - Base functionality for creating user bookmarks
  - This enhancement extends bookmarks to procedures

- **Custom Procedures** (existing feature)
  - Operations grid where dynamic bookmarks now appear
  - Arg ComboBoxes now show both KnownControls and dynamic bookmarks

---

## Conclusion

Dynamic bookmarks are now fully integrated with custom procedures. Users can create bookmarks in the UI Bookmark tab and immediately use them in automation procedures without any manual configuration. The automatic tag generation ensures code-friendly identifiers while maintaining user-friendly display names.

**User Impact**: Greatly improved flexibility in automation - users can now reference ANY saved bookmark in procedures, not just predefined KnownControls.

---

**Implementation Date**: 2025-11-26  
**Build Status**: ? Success  
**Feature**: Ready for use  
**Breaking Changes**: None

