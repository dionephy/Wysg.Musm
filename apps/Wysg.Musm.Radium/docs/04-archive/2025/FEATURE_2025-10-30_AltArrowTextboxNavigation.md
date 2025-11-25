# FEATURE: Alt+Arrow Navigation Between Adjacent TextBoxes and Editors

**Date**: 2025-01-30  
**Status**: ? Implemented (Complete Network)  
**Category**: User Interface, Keyboard Navigation

---

## Overview

Implemented a comprehensive keyboard shortcut feature using **Alt+Arrow keys** to navigate between adjacent textboxes/editors with optional text copying functionality. Now includes complete navigation across ReportInputsAndJsonPanel, CurrentReportEditorPanel, and PreviousReportEditorPanel with 26 total navigation paths.

---

## Problem Statement

Users needed a quick way to:
1. Navigate between related textboxes and editors without using the mouse
2. Copy selected text from one field to another
3. Navigate vertically through the form (Study Remark ¡æ Chief Complaint ¡æ Patient Remark ¡æ Patient History ¡æ EditorFindings ¡æ EditorConclusion)
4. Navigate horizontally between main and proofread fields
5. Navigate between current and previous report editors
6. View previous reports for reference without accidentally copying
7. Selectively copy content from previous reports when needed
8. Improve workflow efficiency across all editing areas

---

## Solution

### Navigation Behavior

When **Alt+Arrow** keys are pressed:

- **No Selection**: Focus moves to the adjacent textbox/editor
- **Has Selection + Copy Enabled**: Selected text is copied to the end of the target (with newline separator if needed)
- **Has Selection + Copy Disabled**: Focus moves without copying (useful for reference viewing)

---

## Complete Navigation Mappings (26 Paths)

### TextBox Navigation (ReportInputsAndJsonPanel)

#### Vertical Navigation (Up/Down)

| Source Field | Key | Target Field | Copy? |
|-------------|-----|--------------|-------|
| **Study Remark** | Alt+Down | Chief Complaint | ? Yes |
| **Chief Complaint** | Alt+Up | Study Remark | ? Yes |
| **Chief Complaint** | Alt+Down | Patient Remark | ? Yes |
| **Chief Complaint Proofread** | Alt+Up | Study Remark | ? Yes |
| **Chief Complaint Proofread** | Alt+Down | Patient Remark | ? Yes |
| **Patient Remark** | Alt+Up | Chief Complaint | ? Yes |
| **Patient Remark** | Alt+Down | Patient History | ? Yes |
| **Patient History** | Alt+Up | Patient Remark | ? Yes |
| **Patient History** | Alt+Down | EditorFindings | ? Yes |
| **Patient History Proofread** | Alt+Up | Patient Remark | ? Yes |
| **Patient History Proofread** | Alt+Down | EditorFindings | ? Yes |
| **EditorFindings** | Alt+Up | Patient History | ? Yes |

#### Horizontal Navigation (Left/Right)

| Source Field | Key | Target Field | Copy? |
|-------------|-----|--------------|-------|
| **Chief Complaint** | Alt+Right | Chief Complaint Proofread | ? Yes |
| **Chief Complaint Proofread** | Alt+Left | Chief Complaint | ? Yes |
| **Patient History** | Alt+Right | Patient History Proofread | ? Yes |
| **Patient History Proofread** | Alt+Left | Patient History | ? Yes |

### EditorControl Navigation (Current & Previous Reports)

#### Current Report Vertical Navigation

| Source | Key | Target | Copy? |
|--------|-----|--------|-------|
| **EditorFindings** | Alt+Down | EditorConclusion | ? Yes |
| **EditorConclusion** | Alt+Up | EditorFindings | ? Yes |

#### Current ¡ê Previous Horizontal Navigation

| Source | Key | Target | Copy? |
|--------|-----|--------|-------|
| **EditorFindings** | Alt+Right | EditorPreviousFindings | ? No |
| **EditorPreviousFindings** | Alt+Left | EditorFindings | ? No |
| **EditorPreviousHeader** | Alt+Left | EditorFindings | ? Yes |
| **EditorPreviousConclusion** | Alt+Left | EditorFindings | ? Yes |

#### Previous Report Vertical Navigation

| Source | Key | Target | Copy? |
|--------|-----|--------|-------|
| **EditorPreviousHeader** | Alt+Down | EditorPreviousFindings | ? No |
| **EditorPreviousFindings** | Alt+Up | EditorPreviousHeader | ? No |
| **EditorPreviousFindings** | Alt+Down | EditorPreviousConclusion | ? No |
| **EditorPreviousConclusion** | Alt+Up | EditorPreviousFindings | ? No |

---

## Copy Behavior Logic

### Copy Enabled (?):
- **Within current report**: User is actively editing, likely wants to copy content
- **Previous ¡æ Current**: User wants to bring reference content to current report
- **TextBox ¡æ EditorControl**: Transitioning from input to main editing

### Copy Disabled (?):
- **Current ¡æ Previous**: Previous reports are typically read-only reference
- **Within previous report**: Navigating for reference viewing only

---

## Implementation Details

### Files Modified/Created

1. **`ReportInputsAndJsonPanel.xaml`**
   - Added x:Names for dynamic control access

2. **`ReportInputsAndJsonPanel.xaml.cs`**
   - TextBox navigation logic
   - TargetEditor dependency property
   - TextBox ¡æ EditorControl navigation

3. **`CenterEditingArea.xaml.cs`** (NEW NAVIGATION HUB)
   - EditorControl navigation logic
   - Configurable copy/no-copy behavior
   - Cross-panel navigation management

4. **`MainWindow.xaml.cs`**
   - Wired TargetEditor connections

### Key Features

- **Complete Navigation Network**: 26 total paths covering all editing areas
- **Smart Copy Logic**: Configurable per navigation path
- **Cross-Control Support**: TextBox ¡ê EditorControl (AvalonEdit)
- **Cross-Panel Support**: Current ¡ê Previous report editors
- **Dynamic Control Location**: FindName() for controls in templates
- **Conditional Copy**: Some paths copy, others don't
- **Caret Positioning**: Always positions at end after navigation
- **Event Handling**: PreviewKeyDown with e.Handled = true
- **SystemKey Detection**: Properly handles Alt+Arrow via e.SystemKey

---

## Technical Implementation

### Centralized Navigation Hub

`CenterEditingArea.xaml.cs` serves as the central hub for EditorControl navigation:

```csharp
private void SetupEditorNavigation()
{
    // Current report vertical (with copy)
    SetupEditorPair(EditorFindings, EditorConclusion, Key.Down, Key.Up, copyText: true);
 
    // Current <-> Previous horizontal
    SetupEditorPair(EditorFindings, EditorPreviousFindings, Key.Right, Key.Left, copyText: false);
    SetupOneWayEditor(EditorPreviousHeader, EditorFindings, Key.Left, copyText: true);
    SetupOneWayEditor(EditorPreviousConclusion, EditorFindings, Key.Left, copyText: true);
    
    // Previous report vertical (no copy)
    SetupOneWayEditor(EditorPreviousHeader, EditorPreviousFindings, Key.Down, copyText: false);
    // ... more mappings
}
```

### Conditional Copy Pattern

```csharp
private void HandleEditorNavigation(EditorControl source, EditorControl target, bool copyText)
{
 var sourceEditor = source.FindName("Editor") as ICSharpCode.AvalonEdit.TextEditor;
    var targetEditor = target.FindName("Editor") as ICSharpCode.AvalonEdit.TextEditor;
    
    if (copyText && !string.IsNullOrEmpty(sourceEditor.SelectedText))
 {
     // Copy mode: append selected text
        var selectedText = sourceEditor.SelectedText;
        var targetText = targetEditor.Text ?? string.Empty;
   targetEditor.Text = string.IsNullOrEmpty(targetText) 
  ? selectedText 
            : targetText + "\n" + selectedText;
    }
    else
  {
      // Navigate-only mode: just move focus
    }
    
    targetEditor.Focus();
    targetEditor.CaretOffset = targetEditor.Text?.Length ?? 0;
}
```

---

## Usage Examples

### Example 1: Complete Form Flow

**Scenario**: Navigate from top to bottom through entire form

1. **Study Remark** ¡æ Alt+Down ¡æ **Chief Complaint**
2. Alt+Down ¡æ **Patient Remark**
3. Alt+Down ¡æ **Patient History**
4. Alt+Down ¡æ **EditorFindings**
5. Alt+Down ¡æ **EditorConclusion**

### Example 2: Reference Previous Report

**Scenario**: View previous findings without copying

1. In **EditorFindings** (current report)
2. Press **Alt+Right** ¡æ **EditorPreviousFindings** (no copy, just view)
3. Press **Alt+Down** ¡æ **EditorPreviousConclusion** (no copy)
4. Press **Alt+Left** ¡æ **EditorFindings** (back to current)

### Example 3: Copy From Previous Report

**Scenario**: Bring specific content from previous to current

1. Navigate to **EditorPreviousHeader**
2. Select relevant text (e.g., "Previous CT showed...")
3. Press **Alt+Left** ¡æ Text copied to **EditorFindings**
4. Continue editing in EditorFindings

### Example 4: Navigate Previous Report Sections

**Scenario**: Review all sections of previous report

1. **EditorPreviousHeader** ¡æ Alt+Down ¡æ **EditorPreviousFindings**
2. Alt+Down ¡æ **EditorPreviousConclusion**
3. Alt+Up ¡æ **EditorPreviousFindings**
4. Alt+Up ¡æ **EditorPreviousHeader**

---

## Future Enhancements

### Short-term
- [ ] Update user guide with EditorControl examples
- [ ] Add keyboard shortcut reference card
- [ ] Visual feedback when Alt is pressed

### Medium-term
- [ ] Navigation to/from JSON editors
- [ ] User-configurable copy/no-copy per path
- [ ] Navigation history (Alt+Shift+Arrow to go back)

### Long-term
- [ ] Ctrl+Alt+Arrow for cut-and-move
- [ ] Custom key mappings
- [ ] Navigation breadcrumb display

---

## Technical Notes

- Uses WPF `PreviewKeyDown` event
- Checks `ModifierKeys.Alt`
- Uses `e.SystemKey` for arrow key detection
- Supports both TextBox and EditorControl (AvalonEdit)
- Conditional copy logic per navigation path
- Centralized management in CenterEditingArea
- No interference with other shortcuts
- Preserves data binding

---

## Testing Scenarios

? **TextBox Navigation** (16 paths)
- All vertical paths working
- All horizontal paths working
- TextBox ¡æ EditorControl working
- EditorControl ¡æ TextBox working

? **EditorControl Navigation** (10 paths)
- Current report vertical working
- Current ¡ê Previous horizontal working
- Previous report vertical working
- Copy behavior correct per spec
- No-copy behavior working

? **Text Copying**
- Selected text copied when enabled
- No copy when disabled
- Newline separator added correctly
- Caret positioned at end

? **Cross-Panel**
- TextBox ¡æ EditorControl seamless
- EditorControl ¡ê EditorControl seamless
- Current ¡ê Previous seamless

---

## Related Files

- `apps\Wysg.Musm.Radium\Controls\ReportInputsAndJsonPanel.xaml`
- `apps\Wysg.Musm.Radium\Controls\ReportInputsAndJsonPanel.xaml.cs`
- `apps\Wysg.Musm.Radium\Controls\CenterEditingArea.xaml.cs` ? NEW
- `apps\Wysg.Musm.Radium\Views\MainWindow.xaml.cs`
- `apps\Wysg.Musm.Radium\Controls\CurrentReportEditorPanel.xaml`
- `apps\Wysg.Musm.Radium\Controls\PreviousReportEditorPanel.xaml`
