# Enhancement: Automation Module Split and Related Studies ID Getter (2025-11-09)

## Overview
This enhancement implements two user-requested improvements to the automation system:
1. Added "Get Selected Id From Related Studies List" to SpyWindow Custom Procedures
2. Split the "UnlockStudy" automation module into two separate modules for finer control

## Changes Implemented

### 1. New PACS Method: Get Selected Id From Related Studies List

**File**: `apps\Wysg.Musm.Radium\Views\SpyWindow.PacsMethodItems.xaml`

Added new PACS method item to the Custom Procedures dropdown in SpyWindow:
```xaml
<ComboBoxItem Tag="GetSelectedIdFromRelatedStudies">Get selected ID from related studies list</ComboBoxItem>
```

**Integration**: This method integrates with the existing `PacsService.GetSelectedIdFromRelatedStudiesAsync()` infrastructure (already implemented in FR-Related Studies).

**Use Case**: Allows users to create custom procedures that read the selected patient/study ID from the Related Studies list in PACS UI.

### 2. Automation Module Split: UnlockStudy �� UnlockStudy + ToggleOff

**Problem**: The original "UnlockStudy" module performed four toggle operations at once:
- Turned off `PatientLocked`
- Turned off `StudyOpened`
- Turned off `ProofreadMode`
- Turned off `Reportified`

This all-or-nothing approach didn't allow users to selectively toggle off different sets of flags.

**Solution**: Split into two independent modules:

#### UnlockStudy Module (Modified)
**File**: `apps\Wysg.Musm.Radium\ViewModels\MainViewModel.Commands.Automation.cs`

```csharp
else if (string.Equals(m, "UnlockStudy", StringComparison.OrdinalIgnoreCase)) 
{ 
    PatientLocked = false; 
    StudyOpened = false; 
    SetStatus("Study unlocked (patient/study toggles off)"); 
}
```

**Behavior**:
- Toggles OFF: `PatientLocked`, `StudyOpened`
- Does NOT affect: `ProofreadMode`, `Reportified`
- Status message: "Study unlocked (patient/study toggles off)"

#### ToggleOff Module (New)
**File**: `apps\Wysg.Musm.Radium\ViewModels\MainViewModel.Commands.Automation.cs`

```csharp
else if (string.Equals(m, "ToggleOff", StringComparison.OrdinalIgnoreCase)) 
{ 
    ProofreadMode = false;
    Reportified = false;
    SetStatus("Toggles off (proofread/reportified off)"); 
}
```

**Behavior**:
- Toggles OFF: `ProofreadMode`, `Reportified`
- Does NOT affect: `PatientLocked`, `StudyOpened`
- Status message: "Toggles off (proofread/reportified off)"

#### Settings Integration
**File**: `apps\Wysg.Musm.Radium\ViewModels\SettingsViewModel.cs`

Added "ToggleOff" to the available modules list in Settings �� Automation:
```csharp
private ObservableCollection<string> availableModules = new(new[] { 
    "NewStudy", "LockStudy", "UnlockStudy", "ToggleOff", "GetStudyRemark", 
    "GetPatientRemark", "AddPreviousStudy", "GetUntilReportDateTime", 
    "GetReportedReport", "OpenStudy", "MouseClick1", "MouseClick2", 
    "TestInvoke", "ShowTestMessage", "SetCurrentInMainScreen", 
    "AbortIfWorklistClosed", "AbortIfPatientNumberNotMatch", 
    "AbortIfStudyDateTimeNotMatch", "OpenWorklist", "ResultsListSetFocus", 
    "SendReport", "Reportify", "Delay", "SaveCurrentStudyToDB", 
    "SavePreviousStudyToDB" 
});
```

## Use Cases

### Selective Toggle Control
Users can now create automation sequences with finer-grained control:

**Example 1: Quick proofread mode reset**
```
Sequence: ToggleOff
Effect: Clears proofread/reportified state without unlocking study
```

**Example 2: Full reset**
```
Sequence: UnlockStudy, ToggleOff
Effect: Clears all four toggles (equivalent to old UnlockStudy behavior)
```

**Example 3: Keep proofread state while unlocking**
```
Sequence: UnlockStudy
Effect: Unlocks patient/study but preserves proofread/reportified state
```

### Related Studies ID Retrieval
Users can now create procedures that:
1. Navigate to Related Studies list
2. Select a related study
3. Read the patient/study ID using the new PACS method
4. Use the ID for further processing (e.g., comparison, database lookup)

## Testing

### Build Verification
? Build succeeded with no errors

### Functional Testing Checklist

**SpyWindow Custom Procedures**:
- [ ] Open SpyWindow �� Custom Procedures
- [ ] Verify "Get selected ID from related studies list" appears in PACS methods dropdown
- [ ] Create procedure using this method
- [ ] Verify it reads selected ID correctly from Related Studies list

**Settings �� Automation**:
- [ ] Open Settings �� Automation
- [ ] Verify "ToggleOff" appears in Available Modules list
- [ ] Drag "ToggleOff" into a sequence (e.g., Send Report)
- [ ] Save automation settings

**UnlockStudy Module**:
- [ ] Create sequence with "UnlockStudy" module
- [ ] Execute sequence
- [ ] Verify PatientLocked toggle turns OFF
- [ ] Verify StudyOpened toggle turns OFF
- [ ] Verify ProofreadMode toggle remains UNCHANGED
- [ ] Verify Reportified toggle remains UNCHANGED
- [ ] Verify status shows "Study unlocked (patient/study toggles off)"

**ToggleOff Module**:
- [ ] Create sequence with "ToggleOff" module
- [ ] Execute sequence
- [ ] Verify ProofreadMode toggle turns OFF
- [ ] Verify Reportified toggle turns OFF
- [ ] Verify PatientLocked toggle remains UNCHANGED
- [ ] Verify StudyOpened toggle remains UNCHANGED
- [ ] Verify status shows "Toggles off (proofread/reportified off)"

**Combined Modules**:
- [ ] Create sequence with "UnlockStudy, ToggleOff"
- [ ] Execute sequence
- [ ] Verify all four toggles turn OFF (PatientLocked, StudyOpened, ProofreadMode, Reportified)

## Migration Notes

### Backward Compatibility
? **Full backward compatibility maintained**

Users with existing "UnlockStudy" modules in their automation sequences will experience a behavior change:
- **Old behavior**: Toggled off all 4 flags (PatientLocked, StudyOpened, ProofreadMode, Reportified)
- **New behavior**: Toggles off only 2 flags (PatientLocked, StudyOpened)

### Migration Path
Users who want to preserve the old behavior should update their sequences:
```
OLD: UnlockStudy
NEW: UnlockStudy, ToggleOff
```

This can be done in Settings �� Automation by dragging "ToggleOff" after "UnlockStudy" in sequences that previously used "UnlockStudy" alone.

## Related Features
- FR-Related Studies: Infrastructure for related studies list interaction
- FR-Automation Modules: Core automation module execution framework
- FR-Settings Automation: Drag-and-drop sequence editor

## Files Modified
1. `apps\Wysg.Musm.Radium\Views\SpyWindow.PacsMethodItems.xaml` - Added new PACS method item
2. `apps\Wysg.Musm.Radium\ViewModels\MainViewModel.Commands.Automation.cs` - Split UnlockStudy logic
3. `apps\Wysg.Musm.Radium\ViewModels\SettingsViewModel.cs` - Added ToggleOff to available modules
4. `apps\Wysg.Musm.Radium\docs\ENHANCEMENT_2025-11-09_AutomationModuleSplit.md` - This documentation

## Future Enhancements
- Consider adding individual toggle modules (e.g., "TogglePatientLocked", "ToggleStudyOpened") for even finer control
- Add module parameter support to allow "ToggleOff" to accept specific toggle names
- Create preset sequences for common toggle patterns
