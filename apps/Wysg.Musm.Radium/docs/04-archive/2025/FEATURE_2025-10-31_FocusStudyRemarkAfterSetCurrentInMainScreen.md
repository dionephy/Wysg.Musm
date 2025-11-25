# Feature: Focus Study Remark Textbox After SetCurrentInMainScreen Module

## Date
2025-01-31

## Overview
Added automatic focus to the "Study Remark" textbox in the top JSON grid after the `SetCurrentInMainScreen` automation module completes. This improves workflow efficiency by automatically positioning the user's cursor in the Study Remark field after the PACS screen layout is set.

## Problem Statement
Users reported that after the `SetCurrentInMainScreen` automation module completes (which switches the PACS to show the current study in the main screen and previous study in sub-screen), they had to manually click on the Study Remark textbox to enter data. This added unnecessary friction to the workflow.

## Solution
Implemented automatic focus management:
1. Added `RequestFocusStudyRemark` event to `MainViewModel`
2. Modified `RunSetCurrentInMainScreenAsync()` to trigger the focus request after screen layout is set
3. Added `OnRequestFocusStudyRemark` handler in `MainWindow` to actually focus the textbox

## Technical Implementation

### 1. MainViewModel Changes

**File**: `apps\Wysg.Musm.Radium\ViewModels\MainViewModel.cs`

Added event property for focus communication:
```csharp
// NEW: Event to communicate focus request for Study Remark textbox (after SetCurrentInMainScreen completes)
public event EventHandler? RequestFocusStudyRemark;
```

**File**: `apps\Wysg.Musm.Radium\ViewModels\MainViewModel.Commands.cs`

Modified `RunSetCurrentInMainScreenAsync()` to request focus after completion:
```csharp
private async Task RunSetCurrentInMainScreenAsync()
{
    try
    {
        await _pacs.SetCurrentStudyInMainScreenAsync();
        await _pacs.SetPreviousStudyInSubScreenAsync();
        SetStatus("Screen layout set: current study in main, previous study in sub");
        
        // NEW: Request focus on Study Remark textbox in top grid after screen layout is complete
        // Small delay to allow PACS UI to settle before focusing our textbox
        await Task.Delay(150);
        RequestFocusStudyRemark?.Invoke(this, EventArgs.Empty);
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine("[Automation] SetCurrentInMainScreen error: " + ex.Message);
        SetStatus("Screen layout failed", true);
    }
}
```

### 2. MainWindow Changes

**File**: `apps\Wysg.Musm.Radium\Views\MainWindow.xaml.cs`

Added event subscription in `OnLoaded`:
```csharp
// NEW: Listen for Study Remark focus request from ViewModel (e.g., after SetCurrentInMainScreen completes)
vm.RequestFocusStudyRemark += OnRequestFocusStudyRemark;
```

Added focus handler:
```csharp
private async void OnRequestFocusStudyRemark(object? sender, EventArgs e)
{
    // Focus Study Remark textbox when ViewModel requests it (after SetCurrentInMainScreen completes)
    System.Diagnostics.Debug.WriteLine("[MainWindow] Focus request received - focusing Study Remark textbox");
    
    // Small delay to ensure UI has finished updating before focusing
    await Task.Delay(50);
    
    // Activate this window first to ensure it's in foreground
    if (!this.IsActive)
    {
        System.Diagnostics.Debug.WriteLine("[MainWindow] Window not active - calling Activate()");
        this.Activate();
    }
    
    // Find the Study Remark textbox in the top grid (gridTopChild or gridSideTop depending on orientation)
    // The ReportInputsAndJsonPanel is used in both locations, so we need to find the active one
    System.Windows.Controls.TextBox? txtStudyRemark = null;
    
    try
    {
        // Try to find txtStudyRemark from gridTopChild (portrait orientation)
        if (gridTopChild != null && gridTopChild.Visibility == Visibility.Visible)
        {
            txtStudyRemark = gridTopChild.FindName("txtStudyRemark") as System.Windows.Controls.TextBox;
            System.Diagnostics.Debug.WriteLine($"[MainWindow] Found txtStudyRemark in gridTopChild: {txtStudyRemark != null}");
        }
        
        // If not found in gridTopChild, try gridSideTop (landscape orientation)
        if (txtStudyRemark == null && gridSideTop != null && gridSideTop.Visibility == Visibility.Visible)
        {
            txtStudyRemark = gridSideTop.FindName("txtStudyRemark") as System.Windows.Controls.TextBox;
            System.Diagnostics.Debug.WriteLine($"[MainWindow] Found txtStudyRemark in gridSideTop: {txtStudyRemark != null}");
        }
        
        // Focus the textbox if found
        if (txtStudyRemark != null)
        {
            txtStudyRemark.Focus();
            txtStudyRemark.CaretIndex = txtStudyRemark.Text?.Length ?? 0; // Move caret to end
            System.Diagnostics.Debug.WriteLine($"[MainWindow] Focused Study Remark textbox, caret at end (length={txtStudyRemark.Text?.Length ?? 0})");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("[MainWindow] Study Remark textbox not found in either panel");
        }
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"[MainWindow] Error focusing Study Remark textbox: {ex.Message}");
    }
    
    System.Diagnostics.Debug.WriteLine("[MainWindow] Focus Study Remark operation completed");
}
```

## Behavior

1. User runs automation sequence containing `SetCurrentInMainScreen` module
2. Module executes PACS methods to set screen layout
3. After successful completion, module waits 150ms for PACS UI to stabilize
4. Module raises `RequestFocusStudyRemark` event
5. MainWindow receives event and:
   - Waits 50ms for UI to finish updating
   - Activates the main window if not already active
   - Finds the Study Remark textbox (handles both portrait and landscape orientations)
   - Sets keyboard focus to the textbox
   - Moves caret to end of existing text

## Edge Cases Handled

- **Window not active**: Calls `Activate()` before focusing textbox
- **Multiple orientations**: Checks both `gridTopChild` (portrait) and `gridSideTop` (landscape) to find the visible textbox
- **Textbox not found**: Logs warning but doesn't crash
- **PACS UI timing**: 150ms delay after screen layout change + 50ms delay before focusing to avoid race conditions

## Testing

1. **Manual Test**:
   - Configure automation sequence with `SetCurrentInMainScreen` module
   - Run the automation
   - Verify Study Remark textbox receives focus automatically
   - Verify caret is at the end of existing text
   - Test in both portrait and landscape orientations

2. **Build Verification**:
   - Build succeeded with no errors
   - All existing tests pass

## Related Files Modified

1. `apps\Wysg.Musm.Radium\ViewModels\MainViewModel.cs`
2. `apps\Wysg.Musm.Radium\ViewModels\MainViewModel.Commands.cs`
3. `apps\Wysg.Musm.Radium\Views\MainWindow.xaml.cs`

## Documentation Updates

This document serves as the primary documentation for this feature. The following should also be updated:

- [ ] `apps\Wysg.Musm.Radium\docs\Spec.md` - Add FR-XXXX for this feature
- [ ] `apps\Wysg.Musm.Radium\docs\Plan.md` - Add change log entry
- [ ] User documentation (if applicable)

## Future Enhancements

1. Make the focus behavior configurable (enable/disable via settings)
2. Allow users to configure which field receives focus after which automation module
3. Add focus indicators or visual feedback when focus is set programmatically
