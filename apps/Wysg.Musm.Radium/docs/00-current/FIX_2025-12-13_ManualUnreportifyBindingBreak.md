# FIX: Manual Un-Reportify Breaking Editor Binding

**Date**: 2025-12-13  
**Status**: ? Completed  
**Build**: ? Success  
**Priority**: High  
**Type**: Bug Fix  

---

## Problem Statement

When manually toggling the Reportified toggle button OFF (un-reportify) after the text had been reportified, the EditorFindings binding would break. The editor would show incorrect or empty text, and further edits would not work properly.

### Symptoms
- After toggling Reportified ON ¡æ editing ¡æ toggling OFF, the Findings editor would show empty or incorrect text
- The two-way binding between `FindingsText` and the editor `DocumentText` was broken
- Further typing in the editor would not update the underlying property

---

## Root Cause Analysis

The issue was caused by a **race condition** between XAML trigger evaluation and property value updates:

### Original Flow (Broken)
1. User clicks Reportified toggle OFF
2. `Reportified` setter called with `value = false`
3. `_reportified = false` (backing field updated)
4. `OnPropertyChanged(nameof(Reportified))` raised
5. **XAML trigger evaluates** and switches binding from `FindingsDisplay` (OneWay) to `FindingsText` (TwoWay)
6. **At this point, `_findingsText` still contains the reportified text!**
7. `ToggleReportified(false)` is called (too late)
8. `FindingsText = _rawFindings` attempts to set the raw text
9. But the binding was already established with the wrong (reportified) value

### The Problem
The XAML Style.Trigger in `CurrentReportEditorPanel.xaml` switches the `DocumentText` binding based on `Reportified`:

```xml
<Style.Triggers>
    <DataTrigger Binding="{Binding Reportified}" Value="True">
        <Setter Property="DocumentText" Value="{Binding FindingsDisplay, Mode=OneWay}"/>
    </DataTrigger>
</Style.Triggers>
```

When `Reportified` changes from `True` to `False`, the trigger deactivates and the default binding takes over:
```xml
<Setter Property="DocumentText" Value="{Binding FindingsText, Mode=TwoWay}"/>
```

The issue is that **XAML evaluates the binding immediately when the trigger condition changes**, but `_findingsText` was still holding the reportified text at that moment.

---

## Solution

### Fix Applied
Update the backing fields (`_findingsText`, `_conclusionText`, `_headerText`) **before** raising `PropertyChanged(Reportified)` when un-reportifying.

### New Flow (Fixed)
1. User clicks Reportified toggle OFF
2. `Reportified` setter called with `value = false`
3. **NEW: Detect un-reportify case (`actualChanged && !value`)**
4. **NEW: Update backing fields with raw values: `_findingsText = _rawFindings`**
5. `_reportified = false` (flag updated)
6. `OnPropertyChanged(nameof(Reportified))` raised
7. XAML trigger evaluates and switches binding to `FindingsText`
8. **`_findingsText` already contains the raw text! Binding works correctly.**
9. `OnPropertyChanged(nameof(FindingsText))` ensures UI refresh

---

## Files Modified

### `apps/Wysg.Musm.Radium/ViewModels/MainViewModel.Editor.cs`

#### `Reportified` Property Setter
```csharp
public bool Reportified 
{ 
    get => _reportified; 
    set 
    { 
        bool actualChanged = (_reportified != value);
        
        // CRITICAL FIX: When turning OFF (un-reportifying), update backing fields 
        // BEFORE raising PropertyChanged so XAML triggers see the correct values
        if (actualChanged && !value)
        {
            _suppressAutoToggle = true;
            _findingsText = _rawFindings;
            _conclusionText = _rawConclusion;
            _headerText = _rawHeader;
            _suppressAutoToggle = false;
        }
        
        _reportified = value;
        
        // PropertyChanged events...
        OnPropertyChanged(nameof(Reportified));
        OnPropertyChanged(nameof(FindingsDisplay));
        OnPropertyChanged(nameof(ConclusionDisplay));
        OnPropertyChanged(nameof(HeaderDisplay));
        OnPropertyChanged(nameof(FindingsText));
        OnPropertyChanged(nameof(ConclusionText));
        OnPropertyChanged(nameof(HeaderText));
        
        // Only apply transformations when turning ON
        if (actualChanged && value)
        {
            ToggleReportified(value);
        }
        
        OnPropertyChanged(nameof(RawFindingsTextEditable));
        OnPropertyChanged(nameof(RawConclusionTextEditable));
    } 
}
```

#### `ToggleReportified` Method
Simplified to only handle the "turn ON" case:
```csharp
private void ToggleReportified(bool value)
{
    // Only called when turning ON (reportifying)
    // Un-reportify logic is handled in the Reportified setter
    if (value)
    {
        CaptureRawIfNeeded();
        _suppressAutoToggle = true;
        HeaderText = ApplyReportifyBlock(_rawHeader, false);
        FindingsText = ApplyReportifyBlock(_rawFindings, false);
        ConclusionText = ApplyReportifyConclusion(_rawConclusion);
        _suppressAutoToggle = false;
    }
}
```

---

## Technical Details

### Why This Fix Works

1. **Timing is Critical**: XAML triggers evaluate synchronously when the bound property changes. By updating the backing fields before changing `_reportified`, we ensure the XAML sees the correct values.

2. **Bypassing Setter Guards**: When un-reportifying, we update `_findingsText` directly instead of through `FindingsText = ...`. This avoids the setter's guard logic that could interfere with the update.

3. **Explicit PropertyChanged**: After updating all backing fields, we raise `PropertyChanged` for each affected property to ensure all bindings refresh.

### Related XAML Binding Pattern

The `CurrentReportEditorPanel.xaml` uses a Style.Trigger pattern:

```xml
<editor:EditorControl.Style>
    <Style TargetType="editor:EditorControl">
        <!-- Default (Reportified=False): Editable, binds to FindingsText -->
        <Setter Property="DocumentText" Value="{Binding FindingsText, Mode=TwoWay}"/>
        <Setter Property="IsReadOnly" Value="False"/>
        <Style.Triggers>
            <!-- When Reportified=True: Read-only, binds to FindingsDisplay -->
            <DataTrigger Binding="{Binding Reportified}" Value="True">
                <Setter Property="DocumentText" Value="{Binding FindingsDisplay, Mode=OneWay}"/>
                <Setter Property="IsReadOnly" Value="True"/>
            </DataTrigger>
        </Style.Triggers>
    </Style>
</editor:EditorControl.Style>
```

This pattern requires the underlying data to be consistent at the moment the trigger evaluates.

---

## Testing

### Test Case 1: Basic Un-Reportify
1. Enter text in Findings: `no acute findings`
2. Toggle Reportified ON ¡æ Shows `No acute findings.`
3. Toggle Reportified OFF
4. ? Expected: Shows `no acute findings` (raw text restored)
5. ? Editor is editable and typing works

### Test Case 2: Edit While Reportified Then Un-Reportify
1. Enter text in Findings: `no acute findings`
2. Toggle Reportified ON
3. Edit the raw text in top grid textbox
4. Toggle Reportified OFF
5. ? Expected: Shows the edited raw text
6. ? Two-way binding works correctly

### Test Case 3: Multiple Toggle Cycles
1. Toggle ON ¡æ OFF ¡æ ON ¡æ OFF multiple times
2. ? Expected: Each cycle works correctly without binding issues

---

## Related Documentation

- `REPORTIFIED_TOGGLE_AUTOMATION_FIX_2025_01_21.md` - Previous fix for automation toggle
- `FEATURE-ProofreadToggleBinding.md` - Proofread toggle binding implementation
- `REPORTIFIED_RESULTSLIST_FIX_2025_01_19.md` - Previous reportified-related fixes

---

## Summary

| Aspect | Before Fix | After Fix |
|--------|-----------|-----------|
| Un-reportify binding | ? Broken | ? Works |
| Editor shows correct text | ? Empty/wrong | ? Raw text |
| Two-way binding | ? Broken | ? Functional |
| Typing in editor | ? No effect | ? Updates property |

The fix ensures that when manually toggling Reportified OFF, the backing fields are updated before XAML triggers re-evaluate, preventing the binding from capturing stale (reportified) values.
