# FEATURE: Proofread Toggle Binding Implementation

**Date**: 2025-01-24  
**Status**: ? Implemented  
**Build**: ? Success  
**Update**: 2025-01-24 - Added Reportified toggle support

---

## Overview

This feature implements binding logic for **both** the **Proofread** toggle and **Reportified** toggle in current report editors. The editors dynamically switch between different text versions based on which toggles are active:

1. **Reportified ON**: Shows reportified (formatted) text, regardless of proofread state
2. **Reportified OFF + Proofread ON**: Shows proofread text
3. **Both OFF**: Shows raw (editable) text

### User Story

As a radiologist reviewing reports:
- I want to **toggle between reportified, proofread, and raw versions** of report fields
- **When Reportified is ON** ¡æ I see formatted text (capitalized, numbered, punctuated) in **read-only mode**
- **When Reportified is OFF and Proofread is ON** ¡æ I see grammar-corrected text in **read-only mode**
- **When both are OFF** ¡æ I see the original raw text in **editable mode**
- **Reportified takes priority** ¡æ When both toggles are ON, reportified text is shown

---

## Technical Implementation

### 1. Current Report Editors

#### Fields Affected
| Field | Raw Property | Proofread Property | Reportified Property | Display Property |
|-------|-------------|-------------------|---------------------|------------------|
| Findings | `FindingsText` (`_rawFindings` when reportified) | `FindingsProofread` | `FindingsText` (transformed) | `FindingsDisplay` |
| Conclusion | `ConclusionText` (`_rawConclusion` when reportified) | `ConclusionProofread` | `ConclusionText` (transformed) | `ConclusionDisplay` |

#### Computed Property Pattern with Priority Logic

```csharp
public string FindingsDisplay
{
    get
    {
        string result;
        
        // PRIORITY 1: Both Reportified AND ProofreadMode are ON
        // Show reportified version of PROOFREAD text
        if (_reportified && ProofreadMode && !string.IsNullOrWhiteSpace(_findingsProofread))
        {
            result = ApplyReportifyBlock(_findingsProofread, false);
        }
        // PRIORITY 2: Only Reportified is ON
        // Show reportified version of RAW text
        else if (_reportified)
        {
            result = _findingsText; // This already contains reportified(raw)
        }
        // PRIORITY 3: Only ProofreadMode is ON
        // Show proofread text as-is (not reportified)
        else if (ProofreadMode && !string.IsNullOrWhiteSpace(_findingsProofread))
        {
            result = _findingsProofread;
        }
        // PRIORITY 4: Both OFF
        // Show raw text
        else
        {
            result = _findingsText;
        }
        return result;
    }
}
```

**Logic**:
1. **Both toggles ON**: Apply reportify transformation to proofread text on-the-fly
2. **Only Reportified**: Return reportified version of raw text (pre-computed)
3. **Only Proofread**: Return proofread text as-is
4. **Both OFF**: Return raw text

#### Property Notifications

When `Reportified` toggles:
```csharp
public bool Reportified 
{ 
    get => _reportified; 
    set 
    { 
        bool actualChanged = (_reportified != value);
        _reportified = value;
        OnPropertyChanged(nameof(Reportified));
        
        // Notify display properties since reportified state affects them
        OnPropertyChanged(nameof(FindingsDisplay));
        OnPropertyChanged(nameof(ConclusionDisplay));
        
        // Only apply transformations if value actually changed
        if (actualChanged)
        {
            ToggleReportified(value);
        }
    } 
}
```

When `ProofreadMode` toggles:
```csharp
public bool ProofreadMode 
{ 
    get => _proofreadMode; 
    set 
    { 
        if (SetProperty(ref _proofreadMode, value))
        {
            // Notify computed display properties for editors
            OnPropertyChanged(nameof(FindingsDisplay));
            OnPropertyChanged(nameof(ConclusionDisplay));
        }
    } 
}
```

---

## Binding Strategy

### Current Report Editors (? Implemented)

**XAML Binding with Multi-State Switching**:

The editors use `Style.Triggers` with `DataTrigger` and `MultiDataTrigger` to support three states:

```xml
<!-- Example: Findings Editor -->
<editor:EditorControl x:Name="EditorFindings" 
                      Grid.Row="1"
                      CaretOffsetAdjustment="{Binding FindingsCaretOffsetAdjustment, Mode=OneWay}"
                      PhraseSnapshot="{Binding CurrentPhraseSnapshot}" 
                      PhraseSemanticTags="{Binding PhraseSemanticTags}"
                      BorderThickness="1,0,1,1"
                      BorderBrush="#2D2D30"
                      Margin="0"
                      MinHeight="20"
                      VerticalScrollBarVisibility="Disabled"
                      HorizontalScrollBarVisibility="Disabled">
    <editor:EditorControl.Style>
        <Style TargetType="editor:EditorControl">
            <!-- Default: Editable, binds to raw FindingsText -->
            <Setter Property="DocumentText" Value="{Binding FindingsText, Mode=TwoWay}"/>
            <Setter Property="IsReadOnly" Value="False"/>
            <Style.Triggers>
                <!-- PRIORITY 1: When Reportified is ON: Read-only, shows reportified text -->
                <DataTrigger Binding="{Binding Reportified}" Value="True">
                    <Setter Property="DocumentText" Value="{Binding FindingsDisplay, Mode=OneWay}"/>
                    <Setter Property="IsReadOnly" Value="True"/>
                </DataTrigger>
                <!-- PRIORITY 2: When ProofreadMode is ON (and Reportified is OFF): Read-only, shows proofread text -->
                <MultiDataTrigger>
                    <MultiDataTrigger.Conditions>
                        <Condition Binding="{Binding Reportified}" Value="False"/>
                        <Condition Binding="{Binding ProofreadMode}" Value="True"/>
                    </MultiDataTrigger.Conditions>
                    <Setter Property="DocumentText" Value="{Binding FindingsDisplay, Mode=OneWay}"/>
                    <Setter Property="IsReadOnly" Value="True"/>
                </MultiDataTrigger>
            </Style.Triggers>
        </Style>
    </editor:EditorControl.Style>
</editor:EditorControl>
```

**Behavior Table**:

| Reportified | ProofreadMode | DocumentText Binding | IsReadOnly | Display Shows |
|------------|--------------|---------------------|-----------|--------------|
| False | False | `FindingsText` (TwoWay) | False | Raw text (editable) |
| False | True | `FindingsDisplay` (OneWay) | True | Proofread text (read-only) |
| True | False | `FindingsDisplay` (OneWay) | True | Reportified(raw) text (read-only) |
| **True** | **True** | `FindingsDisplay` (OneWay) | True | **Reportified(proofread) text (read-only)** ¡ç Shows formatted proofread version |

The same pattern is applied to the `Conclusion` editor.

---
### ?? Behavior Matrix

| Reportified | Proofread | Result |
|------------|----------|--------|
| ? OFF | ? OFF | Raw text (editable) ?? |
| ? OFF | ? ON | Proofread text (read-only) ?? |
| ? ON | ? OFF | **Reportified(raw)** text (read-only) ???? |
| ? ON | ? ON | **Reportified(proofread)** text (read-only) ????? ¡ç New! |

## Fallback Behavior

### Blank Proofread Version

When proofread version is blank or null, the computed property falls back appropriately:

```csharp
// Example: Proofread is blank
FindingsText = "no acute findings"
FindingsProofread = ""  // ¡ç blank
Reportified = false
ProofreadMode = true

// FindingsDisplay = "no acute findings" (falls back to raw since proofread is blank)
```

### Reportified Text Storage

When `Reportified = true`:
- Raw text is stored in `_rawFindings` and `_rawConclusion`
- Transformed text is stored in `_findingsText` and `_conclusionText`
- Database saves always use raw values via `RawFindingsText` and `RawConclusionText` properties
- PACS sends always use raw values

---

## Property Change Notification Flow

### Reportified Toggle

```
User toggles Reportified ON
    ¡é
Reportified setter
    ¡é
SetProperty() ¡æ notifies Reportified
OnPropertyChanged(nameof(FindingsDisplay))
OnPropertyChanged(nameof(ConclusionDisplay))
    ¡é
ToggleReportified(true) ¡æ Applies reportify transformations
    ¡é
_findingsText and _conclusionText now contain formatted text
    ¡é
UI re-evaluates FindingsDisplay and ConclusionDisplay
    ¡é
Style triggers activate (Reportified = True) ¡æ DocumentText switches to FindingsDisplay, IsReadOnly = True
    ¡é
Editors update to show reportified text in read-only mode
```

### Proofread Toggle (when Reportified is OFF)

```
User toggles ProofreadMode ON (Reportified = false)
    ¡é
ProofreadMode setter
    ¡é
SetProperty() ¡æ notifies ProofreadMode
OnPropertyChanged(nameof(FindingsDisplay))
OnPropertyChanged(nameof(ConclusionDisplay))
    ¡é
UI re-evaluates display properties
    ¡é
Style triggers activate (MultiDataTrigger: Reportified = False AND ProofreadMode = True)
    ¡é
Editors switch to FindingsDisplay (showing proofread text), IsReadOnly = True
```

---

## Files Modified

### 1. MainViewModel.Editor.cs
**Location**: `apps\Wysg.Musm.Radium\ViewModels\MainViewModel.Editor.cs`

**Changes**:
- Updated `FindingsDisplay` and `ConclusionDisplay` computed properties to prioritize reportified text
- Updated `Reportified` setter to notify display properties when toggle changes
- Removed notifications for non-existent display properties (ChiefComplaintDisplay, etc.)

### 2. MainViewModel.Commands.cs
**Location**: `apps\Wysg.Musm.Radium\ViewModels\MainViewModel.Commands.cs`

**Changes**:
- Updated `ProofreadMode` setter to notify `FindingsDisplay` and `ConclusionDisplay` only

### 3. CurrentReportEditorPanel.xaml
**Location**: `apps\Wysg.Musm.Radium\Controls\CurrentReportEditorPanel.xaml`

**Changes**:
- Updated `EditorFindings` to use multi-state binding with both `Reportified` and `ProofreadMode` triggers
- Updated `EditorConclusion` to use multi-state binding with both `Reportified` and `ProofreadMode` triggers
- Added priority logic: Reportified takes precedence over Proofread mode

### 4. PreviousReportTextAndJsonPanel.xaml
**Location**: `apps\Wysg.Musm.Radium\Controls\PreviousReportTextAndJsonPanel.xaml`

**Changes**:
- Previous report proofread bindings remain unchanged (support ProofreadMode only)

---

## Testing Strategy

### Integration Testing Checklist

#### Current Report Editors

- [ ] **Both OFF**: Editors are editable, show raw text
- [ ] **Proofread ON (Reportified OFF)**: Editors are read-only, show proofread text
- [ ] **Reportified ON (Proofread OFF)**: Editors are read-only, show reportified text
- [ ] **Both ON**: Editors are read-only, show reportified text (reportified takes priority)
- [ ] **Toggle Reportified OFF while Proofread ON**: Editors switch from reportified to proofread text
- [ ] **Toggle Reportified ON while Proofread ON**: Editors switch from proofread to reportified text
- [ ] **Edit raw text, then toggle Reportified**: Transformed text appears correctly
- [ ] **Database save while Reportified ON**: Raw values are saved (not reportified text)
- [ ] **PACS send while Reportified ON**: Raw values are sent (not reportified text)

#### Fallback Behavior

- [ ] **Proofread blank + Proofread ON**: Shows raw text (proofread fallback)
- [ ] **Reportified ON + blank raw text**: Shows empty (no fallback needed)

---

## Known Limitations

1. **Previous Report Panel**: The previous report panel does not support reportified toggle - it only supports proofread toggle. This is by design as previous reports are typically viewed "as-is" from the database.

2. **Header Fields Not Affected**: The implementation focuses on Findings and Conclusion editors. Header fields (ChiefComplaint, PatientHistory, etc.) do not have reportified versions - they are always shown in their current state.

3. **No Visual Distinction**: When showing reportified vs. proofread vs. raw text, there is no visual indicator beyond the read-only state. Users must rely on the toggle button states.

---

## Future Enhancements

1. **Visual Indicators**: Add different border colors or backgrounds to distinguish:
   - Reportified text (e.g., blue border)
   - Proofread text (e.g., green border)
   - Raw/Editable text (default gray border)

2. **Tooltip Hints**: Add tooltips showing "Reportified version", "Proofread version", or "Raw version (editable)"

3. **Status Bar Indicator**: Show current editor mode in a status bar (e.g., "Mode: Reportified" or "Mode: Proofread")

4. **Previous Report Reportified Support**: Add reportified toggle support to previous report panel if needed

---

## Related Features

- **FR-XXX**: Proofread Toggle Binding (this feature)
- **FR-YYY**: Reportified Toggle Integration (this feature - updated)
- **FR-ZZZ**: Auto-Generate Proofread Text (future)

---

## References

- **Spec**: `apps\Wysg.Musm.Radium\docs\Spec.md` (to be updated with FR numbers)
- **Tasks**: `apps\Wysg.Musm.Radium\docs\Tasks.md` (to be updated with task items)
- **Implementation**: 
  - `apps\Wysg.Musm.Radium\ViewModels\MainViewModel.Editor.cs`
  - `apps\Wysg.Musm.Radium\ViewModels\MainViewModel.Commands.cs`
  - `apps\Wysg.Musm.Radium\Controls\CurrentReportEditorPanel.xaml`
- **Related Docs**:
  - `apps\Wysg.Musm.Radium\docs\CRITICAL_FIX_2025-01-23_ReportifySavingWrongValues.md`

### ?? What Was Implemented

**Priority Logic** (in order):
1. **Both Reportified AND Proofread ON** ¡æ Show reportified(proofread) text, read-only ¡ç **NEW!**
2. **Only Reportified ON** ¡æ Show reportified(raw) text, read-only
3. **Only Proofread ON** ¡æ Show proofread text, read-only  
4. **Both OFF** ¡æ Show raw text, editable
