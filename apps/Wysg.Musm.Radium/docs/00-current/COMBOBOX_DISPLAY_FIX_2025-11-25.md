# SpyWindow ComboBox Display Fix - 2025-11-25

## Quick Summary

**Problem**: Bookmark ComboBox was showing "Wysg.Musm.Radium.Views.BookmarkItem" instead of bookmark names.

**Solution**: Fixed `SpyWindowComboBoxStyle` template to use `ContentPresenter` instead of `TextBlock` with `PriorityBinding`.

**Result**: ? Bookmark names now display correctly in the ComboBox.

---

## What Was Changed

### File: `apps/Wysg.Musm.Radium/Views/SpyWindow.Styles.xaml`

**Before:**
```xaml
<TextBlock Grid.Column="0" Margin="6,2,4,2" ...>
    <TextBlock.Text>
        <PriorityBinding>
            <Binding RelativeSource="{RelativeSource TemplatedParent}" Path="SelectedValue"/>
            <Binding RelativeSource="{RelativeSource TemplatedParent}" Path="SelectedItem.Name"/>
            <Binding RelativeSource="{RelativeSource TemplatedParent}" Path="Text"/>
            <Binding RelativeSource="{RelativeSource TemplatedParent}" Path="SelectionBoxItem"/>
        </PriorityBinding>
    </TextBlock.Text>
</TextBlock>
```

**After:**
```xaml
<ContentPresenter x:Name="ContentSite" Grid.Column="0" Margin="6,2,4,2" 
                VerticalAlignment="Center" HorizontalAlignment="Left"
                Content="{TemplateBinding SelectionBoxItem}"
                ContentTemplate="{TemplateBinding SelectionBoxItemTemplate}"
                ContentTemplateSelector="{TemplateBinding ItemTemplateSelector}"
                IsHitTestVisible="False"/>
```

---

## Why This Fix Works

### The Problem with PriorityBinding

`PriorityBinding` tries multiple binding paths in order until one succeeds. However:
- It was trying to bind to paths that don't exist on BookmarkItem
- It was ignoring the `DisplayMemberPath="Name"` on the ComboBox
- Result: WPF fell back to calling `ToString()` on BookmarkItem, which returned the type name

### The Solution: ContentPresenter

`ContentPresenter` is the standard WPF pattern for displaying selected items in a ComboBox:
- Respects `DisplayMemberPath` attribute
- Uses `SelectionBoxItem` which WPF automatically sets to the selected item's display value
- Works with `ContentTemplate` if you need custom formatting
- Is the recommended approach in WPF documentation

---

## What This Affects

### ? Fixed
- Bookmark ComboBox now shows bookmark names correctly
- Both built-in KnownControls and user bookmarks display properly
- Example: "report text" instead of "Wysg.Musm.Radium.Views.BookmarkItem"

### ? No Breaking Changes
- All existing ComboBoxes using `SpyWindowComboBoxStyle` still work
- DisplayMemberPath continues to work as expected
- Custom templates and selectors still supported

---

## Related Documentation

- **MAP_METHOD_EXPLANATION.md** - Explains what "Map Method" is for
- **ENHANCEMENT_2025-11-25_DynamicUIBookmarks.md** - Full dynamic bookmarks feature documentation
- **ENHANCEMENT_2025-11-25_SpyWindowUICleanup.md** - UI cleanup changes

---

## Map Method Explanation (Bonus Answer)

Since you asked about the "Map Method" ComboBox:

### What is Map Method?

The Map Method determines HOW the SpyWindow locates UI elements in the PACS application.

### Two Options:

1. **Chain** (Recommended ?)
   - Walks through UI hierarchy step-by-step
   - Uses multiple characteristics (Name, ClassName, AutomationId, etc.)
   - More robust and reliable
   - Works when AutomationId is missing
   - **Use this 95% of the time**

2. **AutomationIdOnly** (Fast but Fragile)
   - Searches for element using ONLY its AutomationId
   - Very fast but breaks easily if ID changes
   - Only use for prototyping or well-designed UIs

### Is It Really Necessary?

**Yes**, because:
- Different PACS systems have different automation support
- Some have AutomationIds (can use AutomationIdOnly)
- Others don't (must use Chain)
- Provides flexibility for different use cases

### Recommendation:

**Always use Chain (default)** unless you have a specific reason to use AutomationIdOnly.

For detailed explanation, see: `apps/Wysg.Musm.Radium/docs/MAP_METHOD_EXPLANATION.md`

---

*Fixed: 2025-11-25*  
*Build Status: ? Successful*

