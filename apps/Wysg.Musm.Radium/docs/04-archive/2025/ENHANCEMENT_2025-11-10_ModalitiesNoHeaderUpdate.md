# ENHANCEMENT: Modalities No Header Update - Comma-Separated List

**Date**: 2025-11-10  
**Status**: ? Implemented  
**Priority**: Medium - Improves flexibility for multi-modality exclusions

---

## Summary

Changed the "Do not update header in XR" checkbox to a textbox that accepts comma-separated modality codes, allowing users to exclude multiple modalities from header updates (not just XR).

---

## Changes

### UI Changes

**Before:**
- Checkbox labeled "Do not update header in XR"
- Boolean setting (on/off)
- Only excluded XR modality

**After:**
- Label: "Following modalities don't send header (separated by comma):"
- TextBox for entering comma-separated modality codes
- String setting (e.g., "XR,CR,DX")
- Supports multiple modalities

### Settings Storage

**Interface (`IRadiumLocalSettings.cs`):**
- **Removed**: `DoNotUpdateHeaderInXR` (bool property)
- **Added**: `ModalitiesNoHeaderUpdate` (string property with XML documentation)

**Implementation (`RadiumLocalSettings.cs`):**
- **Removed**: `do_not_update_header_in_xr` storage key
- **Added**: `modalities_no_header_update` storage key

### ViewModel Changes

**`SettingsViewModel.cs`:**
- **Property Changed**: `DoNotUpdateHeaderInXR` (bool) �� `ModalitiesNoHeaderUpdate` (string)
- **Constructor**: Loads string from local settings instead of parsing bool
- **Save Method**: Saves string directly instead of converting bool to "true"/"false"

**`SettingsViewModel.PacsProfiles.cs`:**
- **Property Changed**: `DoNotUpdateHeaderInXR` (bool) �� `ModalitiesNoHeaderUpdate` (string)
- **Save Logic**: Updated debug logs and save logic to use string property

### Logic Changes

**`MainViewModel.Editor.cs`:**
- **Method Updated**: `ShouldSkipHeaderUpdateForXR()` now:
  - Reads `ModalitiesNoHeaderUpdate` setting (comma-separated string)
  - Parses the string into a list of modalities
  - Checks if current study modality is in the exclusion list
  - Supports semicolon or comma as separator
  - Case-insensitive comparison
- **Method Removed**: `GetDoNotUpdateHeaderInXRSetting()` (no longer needed)

**`MainViewModel.Commands.AddPreviousStudy.cs`:**
- **Method Updated**: `UpdateComparisonFromPreviousStudyAsync()` now:
  - Reads `ModalitiesNoHeaderUpdate` setting
  - Parses comma-separated list
  - Checks if current modality is in exclusion list
  - Supports multiple modalities (not just XR)

### XAML Changes

**`AutomationSettingsTab.xaml`:**
- Replaced CheckBox with:
  - TextBlock label: "Following modalities don't send header (separated by comma):"
  - TextBox bound to `ModalitiesNoHeaderUpdate` property
  - Tooltip: "Enter comma-separated modality codes (e.g., XR,CR,DX) that should not update header fields"
  - Width: 150px

---

## Usage

### Setting Exclusions

1. Open Settings window �� Automation tab
2. Find the textbox labeled "Following modalities don't send header (separated by comma):"
3. Enter modality codes separated by commas (e.g., `XR,CR,DX`)
4. Click "Save Automation" button
5. Setting is saved globally (not PACS-specific)

### Examples

**Exclude XR only (original behavior):**
```
XR
```

**Exclude multiple modalities:**
```
XR,CR,DX
```

**Exclude with spaces (automatically trimmed):**
```
XR, CR, DX
```

**Semicolon separator also works:**
```
XR;CR;DX
```

### Effect

When AddPreviousStudy module runs or when header components are updated:
- If current study modality is in the exclusion list:
  - Header fields (Comparison, etc.) will NOT be updated
  - Status message shows: "Comparison not updated (modality '[modality]' excluded by settings)"
- If current study modality is NOT in the list:
  - Header fields are updated normally

---

## Technical Details

### Storage Format

Stored in `%LOCALAPPDATA%\Wysg.Musm\Radium\settings.dat` (encrypted with DPAPI):
```
modalities_no_header_update=XR,CR,DX
```

### Parsing Logic

```csharp
var modalitiesNoHeaderUpdate = _localSettings.ModalitiesNoHeaderUpdate ?? string.Empty;

var excludedModalities = modalitiesNoHeaderUpdate
    .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
    .Select(m => m.Trim().ToUpperInvariant())
    .Where(m => !string.IsNullOrEmpty(m))
    .ToList();

bool shouldSkip = excludedModalities.Contains(currentModality.ToUpperInvariant());
```

### Modality Extraction

Modality is extracted from study name using:
- LOINC mapping (if available) in `UpdateComparisonFromPreviousStudyAsync`
- First word extraction in `ShouldSkipHeaderUpdateForXR` (fallback)

---

## Files Modified

1. `apps\Wysg.Musm.Radium\Services\IRadiumLocalSettings.cs` - Changed property from bool to string
2. `apps\Wysg.Musm.Radium\Services\RadiumLocalSettings.cs` - Changed storage key
3. `apps\Wysg.Musm.Radium\ViewModels\SettingsViewModel.cs` - Updated property and save logic
4. `apps\Wysg.Musm.Radium\ViewModels\SettingsViewModel.PacsProfiles.cs` - Updated property type
5. `apps\Wysg.Musm.Radium\Views\SettingsTabs\AutomationSettingsTab.xaml` - Replaced checkbox with textbox
6. `apps\Wysg.Musm.Radium\ViewModels\MainViewModel.Editor.cs` - Updated skip logic to check list
7. `apps\Wysg.Musm.Radium\ViewModels\MainViewModel.Commands.AddPreviousStudy.cs` - Updated comparison skip logic

---

## Related Features

- FR-511: Add Previous Study Automation Module
- IMPLEMENTATION_SUMMARY_AddPreviousStudyComparison.md - Comparison field auto-fill feature

---

## Benefits

? **Flexibility** - Support multiple modalities, not just XR  
? **User Control** - Easy to add/remove modalities without code changes  
? **Backward Compatible** - Entering "XR" gives same behavior as before  
? **Clear UI** - Label and tooltip explain how to use the feature  
? **Case Insensitive** - "xr", "XR", "Xr" all work the same  
? **Multiple Separators** - Comma or semicolon both supported

---

## Testing Checklist

- [ ] Enter "XR" only - should behave like old checkbox
- [ ] Enter "XR,CR,DX" - all three modalities should be excluded
- [ ] Enter "xr, cr, dx" (with spaces/lowercase) - should work correctly
- [ ] Enter empty string - no exclusions, all modalities update headers
- [ ] Load XR study and run AddPreviousStudy - comparison should not update
- [ ] Load CT study (not in list) and run AddPreviousStudy - comparison should update
- [ ] Setting should persist across application restarts

---

## Migration Notes

### For Users

Old setting (checkbox checked) �� New setting: Enter "XR" in textbox  
Old setting (checkbox unchecked) �� New setting: Leave textbox empty

The old `do_not_update_header_in_xr` setting is not automatically migrated. Users must re-enter their preference in the new textbox format.

### For Developers

The `DoNotUpdateHeaderInXR` property has been completely removed from the codebase. Any code referencing it will fail to compile. Update to use `ModalitiesNoHeaderUpdate` instead.
