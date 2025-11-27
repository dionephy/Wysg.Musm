# Custom Modules UI Enhancements - 2025-11-27

**Date**: 2025-11-27  
**Type**: Enhancement  
**Status**: ? Complete (4/4 features implemented + 4 improvements)  
**Priority**: Normal

---

## Overview

This enhancement addresses four user-requested changes to the Custom Modules feature plus four additional improvements:

### Original Features:
1. ? Rename "NewStudy" module to "NewStudy(Obsolete)" 
2. ? Multi-line display for "Set" custom modules (e.g., "Set sth\nto sth2")
3. ? Syntax coloring for custom module text
4. ? Delete custom module functionality

### Additional Improvements (2025-11-27):
5. ? Context menu only shows when clicking on actual items (not blank space)
6. ? Syntax coloring applied to all automation panes (not just Custom Modules)
7. ? Built-in modules colored entirely in orange
8. ? **Fixed built-in module detection** to prevent false positives (e.g., "SetCurrentTogglesOff" was incorrectly colored by phrase)

---

## Completed Changes

### 1. ? NewStudy Module Renamed to "NewStudy(Obsolete)"

**Changes Made**:
- Updated `apps/Wysg.Musm.Radium/ViewModels/SettingsViewModel.cs`
  - Changed AvailableModules list to include "NewStudy(Obsolete)" instead of "NewStudy"
- Updated `apps/Wysg.Musm.Radium/ViewModels/MainViewModel.Commands.Automation.cs`
  - Added backward compatibility handler to accept both "NewStudy" and "NewStudy(Obsolete)"

**Code**:
```csharp
// SettingsViewModel.cs
private ObservableCollection<string> availableModules = new(new[] { 
    "NewStudy(Obsolete)", // CHANGED from "NewStudy"
    "LockStudy", 
    "UnlockStudy", 
    // ... rest
});

// MainViewModel.Commands.Automation.cs
if (string.Equals(m, "NewStudy", StringComparison.OrdinalIgnoreCase) || 
    string.Equals(m, "NewStudy(Obsolete)", StringComparison.OrdinalIgnoreCase)) 
{ 
    await RunNewStudyProcedureAsync(); 
}
```

**Impact**:
- Existing automation sequences using "NewStudy" will continue to work
- Settings UI shows "NewStudy(Obsolete)" in Available Modules list
- Users are informed that NewStudy is deprecated in favor of modular procedures

---

### 2. ? Multi-Line Display for "Set" Custom Modules

**Implementation**: Custom Value Converter

**File Created**: `apps/Wysg.Musm.Radium/Converters/CustomModuleSyntaxConverter.cs`

**Behavior**:
- Detects "Set X to Y" pattern
- Inserts line break before " to "
- Display format:
  ```
  Set Current Patient Name
  to Get current patient name
  ```

**Code Example**:
```csharp
// Check if this is a "Set X to Y" pattern - add line break
var displayText = moduleName;
if (moduleName.StartsWith("Set ", StringComparison.OrdinalIgnoreCase))
{
    var toIndex = moduleName.IndexOf(" to ", StringComparison.OrdinalIgnoreCase);
    if (toIndex > 0)
    {
        // Insert newline before " to "
        displayText = moduleName.Substring(0, toIndex) + "\n" + moduleName.Substring(toIndex);
    }
}
```

**Features**:
- Automatic text wrapping
- MaxWidth set to 180 pixels
- Works for all "Set" type modules
- Does not affect underlying module name data

---

### 3. ? Syntax Coloring for Custom Module Text

**Implementation**: Integrated into CustomModuleSyntaxConverter

**Color Scheme**:
- **Keywords** ("Abort if", "Set", "to", "Run"): Orange (#FFA000)
- **Properties**: Green (#6ABE30)
- **Bookmarks/Procedures**: Cyan (#4EC9B0)
- **Built-in Modules**: Orange (#FFA000) - entire module name
- **Default**: Light Gray (#D0D0D0)

**Features**:
- Parses module name and applies appropriate colors
- Handles multi-line display with LineBreak elements
- Matches longest keywords/properties first to avoid partial matches
- Property names from `CustomModuleProperties.AllProperties` are automatically recognized
- **Built-in modules (without special keywords at start) are colored entirely in orange**

**Code Example**:
```csharp
private static readonly Brush KeywordBrush = new SolidColorBrush(Color.FromRgb(255, 160, 0)); // Orange
private static readonly Brush PropertyBrush = new SolidColorBrush(Color.FromRgb(106, 190, 48)); // Green
private static readonly Brush BookmarkBrush = new SolidColorBrush(Color.FromRgb(78, 201, 176)); // Cyan

// Check if this is a custom module (starts with special keywords) or built-in module
// Custom modules start with: "Set ", "Run ", or "Abort if "
bool isCustomModule = moduleName.StartsWith("Set ", StringComparison.OrdinalIgnoreCase) ||
                      moduleName.StartsWith("Run ", StringComparison.OrdinalIgnoreCase) ||
                      moduleName.StartsWith("Abort if ", StringComparison.OrdinalIgnoreCase);

if (!isCustomModule)
{
    // Built-in module - display entirely in orange
    textBlock.Inlines.Add(new Run(moduleName) { Foreground = KeywordBrush });
    return textBlock;
}
```

**Visual Examples**:
```
Built-in modules (all orange):
LockStudy
UnlockStudy
GetStudyRemark
SaveCurrentStudyToDB
SetCurrentTogglesOff  ¡ç Fixed! No longer colored by phrase

Custom modules (syntax colored):
Set [Green:Current Patient Name]
to [Cyan:Get current patient name]

Abort if [Cyan:Patient Number Not Match]

Run [Cyan:Clear Report]
```

---

### 4. ? Delete Custom Module Functionality

**Implementation**: Context Menu on Custom Modules List

**Files Modified**:
- `apps/Wysg.Musm.Radium/Views/AutomationWindow.Automation.cs`
  - Added `OnDeleteCustomModule` handler
  - Added `OnCustomModulesContextMenuOpening` handler (NEW)
  - Added `FindVisualParent<T>` helper method (NEW)
- `apps/Wysg.Musm.Radium/Views/AutomationWindow.xaml`
  - Added context menu to lstCustomModules ListBox
  - Added ContextMenuOpening event handler (NEW)

**Features**:
- Right-click on custom module to show context menu
- **NEW**: Context menu only shows when clicking on an actual item (not blank space)
- "Delete Module" option with confirmation dialog
- Removes module from custom-modules.json
- Removes module from UI immediately
- Removes module from AvailableModules list
- Warning that it won't be removed from existing sequences automatically

**Code**:
```csharp
private void OnCustomModulesContextMenuOpening(object sender, ContextMenuEventArgs e)
{
    try
    {
        if (sender is not ListBox list) return;
        
        // Check if the mouse is over an actual item
        var mousePosition = Mouse.GetPosition(list);
        var element = list.InputHitTest(mousePosition) as DependencyObject;
        
        // Walk up the visual tree to find a ListBoxItem
        var listBoxItem = FindVisualParent<ListBoxItem>(element);
        
        if (listBoxItem == null || list.SelectedItem == null)
        {
            // Not over an item - cancel the context menu
            e.Handled = true;
        }
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"[AutomationWindow] Error in ContextMenuOpening: {ex.Message}");
    }
}

private static T? FindVisualParent<T>(DependencyObject? child) where T : DependencyObject
{
    while (child != null)
    {
        if (child is T parent)
            return parent;
        child = VisualTreeHelper.GetParent(child);
    }
    return null;
}
```

**XAML Context Menu**:
```xaml
<ListBox x:Name="lstCustomModules" ... ContextMenuOpening="OnCustomModulesContextMenuOpening">
    <ListBox.ContextMenu>
        <ContextMenu Background="#2D2D30" BorderBrush="#3C3C3C">
            <MenuItem Header="Delete Module" Click="OnDeleteCustomModule" 
                      Foreground="#D0D0D0" Background="#2D2D30"/>
        </ContextMenu>
    </ListBox.ContextMenu>
    <ListBox.ItemTemplate>
        <DataTemplate>
            <ContentControl Content="{Binding Converter={StaticResource CustomModuleSyntaxConverter}}"/>
        </DataTemplate>
    </ListBox.ItemTemplate>
</ListBox>
```

---

### 5. ? Context Menu Only on Items (2025-11-27)

**Problem**: Context menu appeared even when clicking blank space in Custom Modules pane

**Solution**: Added `ContextMenuOpening` event handler that checks if the mouse is over an actual ListBoxItem

**Implementation**:
- Added `OnCustomModulesContextMenuOpening` event handler
- Uses `InputHitTest` and visual tree walking to find if click is on a ListBoxItem
- Cancels context menu display if not over an item

**Result**: Context menu now only appears when right-clicking on an actual module item

---

### 6. ? Syntax Coloring Applied to All Automation Panes (2025-11-27)

**Problem**: Syntax coloring was only applied to Custom Modules pane, not to automation panes (NewStudy, AddStudy, shortcuts, etc.)

**Solution**: Added ItemTemplate with CustomModuleSyntaxConverter to all automation ListBoxes

**Panes Updated**:
1. New Study (`lstNewStudy`)
2. Add Study (`lstAddStudy`)
3. Shortcut: Open study (new) (`lstShortcutOpenNew`)
4. Shortcut: Open study (add) (`lstShortcutOpenAdd`)
5. Shortcut: Open study (after open) (`lstShortcutOpenAfterOpen`)
6. Send Report (`lstSendReport`)
7. Send Report Preview (`lstSendReportPreview`)
8. Shortcut: Send Report (preview) (`lstShortcutSendReportPreview`)
9. Shortcut: Send Report (reportified) (`lstShortcutSendReportReportified`)
10. Test (`lstTest`)
11. Built-in Modules (`lstLibrary`)

**XAML Pattern**:
```xaml
<ListBox x:Name="lstNewStudy" ...>
    <ListBox.ItemTemplate>
        <DataTemplate>
            <ContentControl Content="{Binding Converter={StaticResource CustomModuleSyntaxConverter}}"/>
        </DataTemplate>
    </ListBox.ItemTemplate>
</ListBox>
```

**Result**: All modules in all panes now have consistent syntax coloring

---

### 7. ? Built-in Modules Colored Orange (2025-11-27)

**Problem**: Built-in modules (like LockStudy, UnlockStudy, GetStudyRemark) were not specially colored

**Solution**: Updated CustomModuleSyntaxConverter to detect built-in modules and color them entirely in orange

**Detection Logic**:
```csharp
// Check if this is a custom module (starts with special keywords) or built-in module
// Custom modules start with: "Set ", "Run ", or "Abort if "
bool isCustomModule = moduleName.StartsWith("Set ", StringComparison.OrdinalIgnoreCase) ||
                      moduleName.StartsWith("Run ", StringComparison.OrdinalIgnoreCase) ||
                      moduleName.StartsWith("Abort if ", StringComparison.OrdinalIgnoreCase);

if (!isCustomModule)
{
    // Built-in module - display entirely in orange
    textBlock.Inlines.Add(new Run(moduleName) { Foreground = KeywordBrush });
    return textBlock;
}
```

**Result**: Built-in modules are now clearly distinguished with orange color

---

### 8. ? Fixed Built-in Module Detection (2025-11-27 Update 2)

**Problem**: The converter was using `Contains()` to check for keywords, which caused false positives. For example, "SetCurrentTogglesOff" was incorrectly detected as a custom module because it *contains* "Set" and "to" as substrings, resulting in phrase-by-phrase coloring instead of being colored entirely orange as a built-in module.

**Root Cause**:
```csharp
// OLD BUGGY CODE:
bool isBuiltInModule = !Keywords.Any(k => moduleName.Contains(k, StringComparison.OrdinalIgnoreCase));

// This caused:
// "SetCurrentTogglesOff" ¡æ Contains "Set" ¡æ false positive ¡æ phrase coloring
```

**Solution**: Changed detection to use `StartsWith()` instead of `Contains()` to check if the module name *begins* with the specific keyword patterns used by custom modules.

**New Detection Logic**:
```csharp
// NEW FIXED CODE:
// Custom modules start with: "Set ", "Run ", or "Abort if " (note the space after "Set" and "Run")
bool isCustomModule = moduleName.StartsWith("Set ", StringComparison.OrdinalIgnoreCase) ||
                      moduleName.StartsWith("Run ", StringComparison.OrdinalIgnoreCase) ||
                      moduleName.StartsWith("Abort if ", StringComparison.OrdinalIgnoreCase);

if (!isCustomModule)
{
    // Built-in module - display entirely in orange
    textBlock.Inlines.Add(new Run(moduleName) { Foreground = KeywordBrush });
    return textBlock;
}
```

**Result**: 
- Built-in modules like "SetCurrentTogglesOff", "UnlockStudy", "GetStudyRemark" are now correctly identified and colored entirely in orange
- Custom modules starting with "Set ", "Run ", or "Abort if " (with space) are still syntax-colored with mixed colors
- No more false positives!

**Examples**:
```
Built-in modules (now ALL orange):
? SetCurrentTogglesOff   ¡ç FIXED! Was being colored by phrase
? UnlockStudy
? LockStudy
? GetStudyRemark
? SaveCurrentStudyToDB
? ClearCurrentFields

Custom modules (still syntax colored):
Set [Green:Current Patient Name]
to [Cyan:Get current patient name]

Run [Cyan:Clear Report]

Abort if [Cyan:Patient Number Not Match]
```

---

## Files Created

1. `apps/Wysg.Musm.Radium/Converters/CustomModuleSyntaxConverter.cs`
   - Combined converter for multi-line display and syntax coloring
   - Returns formatted TextBlock with colored inline runs
   - **Updated**: Fixed built-in module detection to use `StartsWith()` instead of `Contains()`

---

## Files Modified

1. `apps/Wysg.Musm.Radium/ViewModels/SettingsViewModel.cs`
   - Renamed "NewStudy" to "NewStudy(Obsolete)"

2. `apps/Wysg.Musm.Radium/ViewModels/MainViewModel.Commands.Automation.cs`
   - Added backward compatibility for both "NewStudy" and "NewStudy(Obsolete)"

3. `apps/Wysg.Musm.Radium/Views/AutomationWindow.Automation.cs`
   - Added `OnDeleteCustomModule` method
   - **Added**: `OnCustomModulesContextMenuOpening` method
   - **Added**: `FindVisualParent<T>` helper method

4. `apps/Wysg.Musm.Radium/Views/AutomationWindow.xaml`
   - Added namespace declaration for converters
   - Added CustomModuleSyntaxConverter to Window resources
   - Added context menu to lstCustomModules
   - **Added**: ContextMenuOpening event handler
   - **Added**: ItemTemplate to all automation panes (11 ListBoxes)

5. `apps/Wysg.Musm.Radium/Converters/CustomModuleSyntaxConverter.cs`
   - **Updated**: Changed built-in module detection from `Contains()` to `StartsWith()`
   - **Updated**: Added specific keyword patterns: "Set ", "Run ", "Abort if " (with spaces)

---

## Testing Checklist

### Feature 1: NewStudy(Obsolete)
- [x] "NewStudy(Obsolete)" appears in Available Modules
- [x] Backward compatibility maintained for "NewStudy"
- [x] Build successful

### Feature 2: Multi-Line Display
- [x] "Set X to Y" displays on two lines
- [x] Other module types display normally (single line)
- [x] Text wrapping works correctly
- [x] Display doesn't affect underlying data
- [x] Build successful

### Feature 3: Syntax Coloring
- [x] Keywords display in orange
- [x] Properties display in green
- [x] Bookmarks/procedures display in cyan
- [x] Colors integrated with multi-line display
- [x] **Built-in modules display entirely in orange**
- [x] Build successful

### Feature 4: Delete Module
- [x] Right-click context menu implemented
- [x] **Context menu only shows when clicking on items (not blank space)**
- [x] Delete confirmation dialog implemented
- [x] Module removed from JSON storage
- [x] Module removed from UI
- [x] Module removed from AvailableModules
- [x] Warning about existing sequences included
- [x] Build successful

### Feature 5: Context Menu Improvements
- [x] Context menu only appears when clicking on an item
- [x] Context menu does not appear when clicking blank space
- [x] Visual tree walking correctly identifies ListBoxItem
- [x] Build successful

### Feature 6: All Panes Colored
- [x] NewStudy pane has syntax coloring
- [x] AddStudy pane has syntax coloring
- [x] All shortcut panes have syntax coloring
- [x] Send Report panes have syntax coloring
- [x] Test pane has syntax coloring
- [x] Built-in Modules pane has syntax coloring
- [x] Custom Modules pane has syntax coloring
- [x] Build successful

### Feature 7: Built-in Module Coloring
- [x] Built-in modules (without keywords) are orange
- [x] Custom modules (with keywords) have mixed colors
- [x] Color distinction is clear and consistent
- [x] Build successful

### Feature 8: Fixed Built-in Detection (Update 2)
- [x] **"SetCurrentTogglesOff" now correctly colored entirely orange (no more phrase coloring)**
- [x] All built-in modules without "Set ", "Run ", or "Abort if " prefix are orange
- [x] Custom modules starting with "Set ", "Run ", or "Abort if " still get syntax coloring
- [x] No false positives
- [x] Build successful

---

## Usage Guide

### Creating a Custom Module
1. Open Automation Window (AutomationWindow)
2. Go to Automation tab
3. Click "Create Module" button
4. Fill in module details and click Save

### Viewing Custom Modules
- Custom modules now display with:
  - Multi-line format for "Set X to Y" patterns
  - Syntax coloring (keywords in orange, properties in green, procedures in cyan)
  - Automatic text wrapping
- **Built-in modules display entirely in orange (including "SetCurrentTogglesOff")**
- **All automation panes (NewStudy, AddStudy, shortcuts, etc.) have consistent coloring**

### Deleting a Custom Module
1. Right-click on the custom module in the Custom Modules list **(must click on actual item, not blank space)**
2. Click "Delete Module" from context menu
3. Confirm deletion in the dialog
4. Module is removed from the list

**Note**: 
- Context menu only appears when clicking on an actual module item
- Deleting a custom module does NOT automatically remove it from existing automation sequences
- You must manually update your sequences

---

## Visual Examples

### Multi-Line Display:
```
Before:
Set Current Patient Name to Get current patient name

After:
Set Current Patient Name
to Get current patient name
```

### Syntax Coloring:

**Custom Modules:**
```
Set [Green:Current Patient Name]
to [Cyan:Get current patient name]

Abort if [Cyan:Patient Number Not Match]

Run [Cyan:Clear Report]
```

**Built-in Modules (all orange):**
```
[Orange:LockStudy]
[Orange:UnlockStudy]
[Orange:GetStudyRemark]
[Orange:SaveCurrentStudyToDB]
[Orange:ClearCurrentFields]
[Orange:ClearPreviousStudies]
[Orange:SetCurrentTogglesOff]  ¡ç FIXED!
```

---

## Build Status

? **Build Successful** - All features implemented and tested

---

## Implementation Summary

**Total Features**: 8 (4 original + 4 improvements)  
**Features Complete**: 8 (100%)  
**Files Created**: 1  
**Files Modified**: 5  
**Build Status**: ? Success  
**Ready for Production**: ? Yes

---

## Changelog

### 2025-11-27 - Update 2: Fixed Built-in Module Detection
- **CRITICAL FIX**: Changed detection from `Contains()` to `StartsWith()` to prevent false positives
- "SetCurrentTogglesOff" and similar modules now correctly colored entirely orange
- Custom module detection now checks for "Set ", "Run ", "Abort if " prefixes (with spaces)
- No more phrase-by-phrase coloring of built-in modules

### 2025-11-27 - Additional Improvements
- Added context menu showing only on actual items (not blank space)
- Applied syntax coloring to all automation panes (11 ListBoxes total)
- Added orange coloring for all built-in modules
- Updated converter to detect and handle built-in modules

### 2025-11-27 - Initial Implementation
- Renamed NewStudy to NewStudy(Obsolete)
- Implemented multi-line display for "Set" modules
- Implemented syntax coloring for custom modules
- Implemented delete custom module functionality

---

## Future Enhancements

Potential improvements for future iterations:
1. Edit existing custom modules (in-place or via dialog)
2. Duplicate custom modules
3. Export/import custom module sets
4. Module preview before creation
5. Customizable color themes for syntax highlighting
6. Drag-and-drop reordering of modules within Custom Modules list

---

**Implementation Date**: 2025-11-27  
**Last Updated**: 2025-11-27 (Update 2 - Fixed built-in detection)  
**Build Status**: ? Success  
**Features Complete**: 8/8  
**Ready for Use**: ? Complete

---

*All requested custom module UI enhancements plus additional improvements have been successfully implemented and tested. Critical fix applied to prevent false positive detection of built-in modules.*
