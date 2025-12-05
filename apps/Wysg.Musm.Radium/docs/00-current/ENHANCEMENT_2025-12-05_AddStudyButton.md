# Enhancement: Add "Add" Button to Main Window

**Date**: 2025-12-05  
**Type**: Enhancement  
**Status**: ? Complete  
**Priority**: Normal

---

## Summary

Added an "Add" button next to the "New" button in the main window's CurrentReportEditorPanel. The button runs the "Add Study" automation session.

## User Request

> in the main window, next to "New" button, i want an "Add" button that runs "Add study" automation session.

## Problem Statement

Previously, users had to use a global hotkey or navigate elsewhere to run the "Add Study" automation. There was no direct button in the main editing panel for this common action.

## Solution

Added an "Add" button in the `CurrentReportEditorPanel.xaml` control, positioned immediately after the "New" button.

## Implementation

### File Modified

**`apps/Wysg.Musm.Radium/Controls/CurrentReportEditorPanel.xaml`**

Added button in the upper row controls:

```xaml
<StackPanel Grid.Row="0" Orientation="Horizontal" Margin="0,0,0,0" VerticalAlignment="Center">
    <Button Content="New" Command="{Binding NewStudyCommand}" FontSize="12" Margin="2,2,0,2"/>
    <Button Content="Add" Command="{Binding AddStudyCommand}" FontSize="12" Margin="2,2,0,2"/>  <!-- NEW -->
    <Button Content="Send Preview" Command="{Binding SendReportPreviewCommand}" Margin="2,2,0,2"/>
    <!-- ...rest of buttons... -->
</StackPanel>
```

## Technical Details

### Command Binding

The "Add" button uses the existing `AddStudyCommand` which:
- Is already defined in `MainViewModel.Commands.Init.cs`
- Calls `OnRunAddStudyAutomation()` handler in `MainViewModel.Commands.Handlers.cs`
- Runs the `AddStudySequence` automation configured for the current PACS profile
- Is only enabled when `PatientLocked == true` (study must be locked first)

### Automation Flow

When clicked, the button:
1. Retrieves `AddStudySequence` from current PACS profile settings
2. Parses the comma-separated module list
3. Executes modules sequentially via `RunModulesSequentially(modules, "Add Study")`
4. Shows green completion message: "? Add Study completed successfully"

### Button Enable State

The button is **disabled** when:
- `PatientLocked == false` (no study locked)

The button is **enabled** when:
- `PatientLocked == true` (study is locked)

This ensures users can only add studies after the initial study has been locked.

## Button Order (After Change)

1. **New** - Runs "New Study" automation
2. **Add** - Runs "Add Study" automation (NEW)
3. **Send Preview** - Sends report preview
4. **Send** - Sends final report
5. **Technique** - Edit study technique
6. **Comparison** - Edit comparison
7. **Phrases** - Extract phrases
8. **Preorder** - Save preorder
9. **Test** - Run test automation

## Testing

### Test Case 1: Button Visible
1. Open application
2. ? Verify "Add" button appears next to "New" button

### Test Case 2: Button Disabled Initially
1. Open application (no study loaded)
2. ? Verify "Add" button is disabled (grayed out)

### Test Case 3: Button Enabled After Lock
1. Run "New Study" automation
2. Lock a study (PatientLocked = true)
3. ? Verify "Add" button is now enabled

### Test Case 4: Add Study Runs
1. Lock a study
2. Select another study in PACS worklist
3. Click "Add" button
4. ? Verify "Add Study" automation runs
5. ? Verify green completion message appears

## Backward Compatibility

? **Full backward compatibility maintained**

- No changes to existing automation sequences
- No changes to command logic
- Existing keyboard shortcuts remain functional
- No breaking changes

## Build Status

? **Build Successful** - No errors, no warnings

---

**Implementation Date**: 2025-12-05  
**Build Status**: ? Success  
**Ready for Use**: ? Complete

---

*New "Add" button successfully added to main window for quick access to Add Study automation.*
