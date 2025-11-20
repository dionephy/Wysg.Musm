# Enhancement: Alt+Arrow Navigation Direction Changes

**Date**: 2025-02-11  
**Type**: UI Enhancement

---

## What Changed

Reorganized Alt+Arrow navigation directions in `ReportInputsAndJsonPanel` to provide more intuitive and workflow-optimized navigation paths between textboxes.

---

## New Navigation Map

### txtStudyRemark
- **Alt+Right** (copyable) ¡æ txtChiefComplaint
- **Alt+Down** ¡æ txtFindings

### txtChiefComplaint
- **Alt+Left** ¡æ txtStudyRemark
- **Alt+Right** ¡æ txtPatientRemark
- **Alt+Down** ¡æ txtPatientHistory

### txtPatientRemark
- **Alt+Left** (copyable) ¡æ txtPatientHistory
- **Alt+Down** ¡æ editorFindings (if TargetEditor is set)

### txtPatientHistory
- **Alt+Up** ¡æ txtChiefComplaint
- **Alt+Down** ¡æ txtFindingsProofread
- **Alt+Left** ¡æ txtStudyRemark
- **Alt+Right** ¡æ txtPatientRemark

### txtFindings
- **Alt+Up** ¡æ txtStudyRemark
- **Alt+Down** ¡æ txtConclusion
- **Alt+Right** (copyable) ¡æ txtFindingsProofread

### txtFindingsProofread
- **Alt+Up** ¡æ txtPatientHistory
- **Alt+Down** ¡æ txtConclusionProofread
- **Alt+Left** ¡æ txtFindings
- **Alt+Right** ¡æ txtPatientRemark

### txtConclusion
- **Alt+Up** ¡æ txtFindings
- **Alt+Down** ¡æ editorFindings (if TargetEditor is set)
- **Alt+Right** ¡æ txtConclusionProofread

### txtConclusionProofread
- **Alt+Up** ¡æ txtFindingsProofread
- **Alt+Down** ¡æ editorFindings (if TargetEditor is set)
- **Alt+Left** ¡æ txtConclusion
- **Alt+Right** ¡æ txtPatientRemark

---

## Why This Matters

- **Improved Workflow** - Navigation paths now follow natural reading and editing patterns
- **Better Spatial Logic** - Horizontal navigation (Left/Right) for related fields, vertical navigation (Up/Down) for sequential fields
- **Copyable Transitions** - Key transitions that commonly involve text copying are marked as copyable (e.g., Study Remark ¡æ Chief Complaint, Patient Remark ¡æ Patient History, Findings ¡æ Findings PR)
- **Consistent Behavior** - All navigation paths are bidirectional where it makes sense

---

## Example Behavior

```
User in txtStudyRemark:
  Alt+Right ¡æ txtChiefComplaint (with text copy if selection exists)
  Alt+Down ¡æ txtFindings (focus only)

User in txtChiefComplaint:
  Alt+Left ¡æ txtStudyRemark (focus only)
  Alt+Right ¡æ txtPatientRemark (focus only)
  Alt+Down ¡æ txtPatientHistory (focus only)

User in txtFindings:
  Alt+Up ¡æ txtStudyRemark (focus only)
  Alt+Down ¡æ txtConclusion (focus only)
  Alt+Right ¡æ txtFindingsProofread (with text copy if selection exists)

User in txtFindingsProofread:
  Alt+Up ¡æ txtPatientHistory (focus only)
  Alt+Down ¡æ txtConclusionProofread (focus only)
  Alt+Left ¡æ txtFindings (focus only)
  Alt+Right ¡æ txtPatientRemark (focus only)
```

---

## Technical Implementation

### Key Changes

1. **Removed bidirectional pair setup** - Changed from `SetupAltArrowPair()` to individual `SetupOneWayAltArrow()` calls for finer control
2. **Added copyText parameter** - Each navigation path explicitly specifies whether text should be copied on transition
3. **Simplified logic** - Each textbox has clearly defined navigation targets for each direction

### Code Structure

```csharp
private void SetupAltArrowNavigation()
{
    // Find textboxes
    var studyRemark = FindName("txtStudyRemark") as TextBox;
    var patientRemark = FindName("txtPatientRemark") as TextBox;
    
    // Setup navigation paths
    SetupOneWayAltArrow(studyRemark, txtChiefComplaint, Key.Right, copyText: true);
    SetupOneWayAltArrow(studyRemark, txtFindings, Key.Down, copyText: false);
    // ... (all other paths)
}

private void SetupOneWayAltArrow(TextBox source, TextBox target, Key key, bool copyText)
{
    source.PreviewKeyDown += (s, e) =>
    {
        var actualKey = e.Key == Key.System ? e.SystemKey : e.Key;
        
        if (e.KeyboardDevice.Modifiers == ModifierKeys.Alt && actualKey == key)
        {
            HandleAltArrowNavigation(source, target, copyText);
            e.Handled = true;
        }
    };
}

private void HandleAltArrowNavigation(TextBox source, TextBox target, bool copyText)
{
    if (!copyText || string.IsNullOrEmpty(source.SelectedText))
    {
        // No selection or copying disabled: just move focus
        target.Focus();
        target.CaretIndex = target.Text?.Length ?? 0;
    }
    else
    {
        // Has selection and copying enabled: copy to end of target
        var selectedText = source.SelectedText;
        var targetText = target.Text ?? string.Empty;
        
        if (!string.IsNullOrEmpty(targetText))
        {
            target.Text = targetText + "\n" + selectedText;
        }
        else
        {
            target.Text = selectedText;
        }
        
        target.Focus();
        target.CaretIndex = target.Text.Length;
    }
}
```

---

## Key File Changes

- `apps\Wysg.Musm.Radium\Controls\ReportInputsAndJsonPanel.xaml.cs` - Updated `SetupAltArrowNavigation()` method with new navigation paths

---

## Build Status

? **Build successful** - No compilation errors

---

## Testing Checklist

- [ ] Test all txtStudyRemark navigation paths (Alt+Right, Alt+Down)
- [ ] Test all txtChiefComplaint navigation paths (Alt+Left, Alt+Right, Alt+Down)
- [ ] Test all txtPatientRemark navigation paths (Alt+Left, Alt+Down)
- [ ] Test all txtPatientHistory navigation paths (Alt+Up, Alt+Down, Alt+Left, Alt+Right)
- [ ] Test all txtFindings navigation paths (Alt+Up, Alt+Down, Alt+Right)
- [ ] Test all txtFindingsProofread navigation paths (Alt+Up, Alt+Down, Alt+Left, Alt+Right)
- [ ] Test all txtConclusion navigation paths (Alt+Up, Alt+Down, Alt+Right)
- [ ] Test all txtConclusionProofread navigation paths (Alt+Up, Alt+Down, Alt+Left, Alt+Right)
- [ ] Verify copyable transitions work correctly (text is appended with newline)
- [ ] Verify non-copyable transitions work correctly (focus only, caret at end)
- [ ] Test in both portrait and landscape orientations
- [ ] Test with Reverse toggle enabled

---

## Benefits

- ? More intuitive navigation patterns
- ? Better spatial logic (horizontal for related, vertical for sequential)
- ? Optimized for common workflows
- ? Clear copyable vs non-copyable transitions
- ? Bidirectional navigation where appropriate
- ? Consistent with existing Alt+Arrow behavior in CenterEditingArea

---

## Future Enhancements

1. Add visual indicators showing available navigation paths when Alt is held
2. Allow users to customize navigation paths via settings
3. Add navigation path visualization in settings/help documentation
4. Consider adding Ctrl+Arrow shortcuts for non-copyable versions of copyable paths
