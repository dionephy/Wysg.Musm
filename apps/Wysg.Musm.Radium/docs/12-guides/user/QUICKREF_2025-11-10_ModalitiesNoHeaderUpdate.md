# QUICKREF: Modalities No Header Update Setting


**Date**: 2025-11-25  
**Type**: Quick Reference  
**Category**: User Reference  
**Status**: ? Active


## Summary

This guide provides quick reference information for users. For detailed implementation information, see the related plan and specification documents.

---

# QUICKREF: Modalities No Header Update Setting

**Feature**: Comma-separated modalities exclusion list  
**Location**: Settings → Automation tab  
**Setting Type**: Global (not PACS-specific)

---

## Quick Usage

### Open Settings
1. Settings window → Automation tab
2. Find textbox: "Following modalities don't send header (separated by comma):"

### Enter Modalities
Enter comma-separated modality codes (e.g., `XR,CR,DX`)

### Save
Click "Save Automation" button

---

## Examples

| Input | Effect |
|-------|--------|
| `XR` | Excludes XR only (original checkbox behavior) |
| `XR,CR,DX` | Excludes XR, CR, and DX modalities |
| `xr, cr, dx` | Same as above (case-insensitive, spaces trimmed) |
| `` (empty) | No exclusions (all modalities update headers) |

---

## When It Works

### AddPreviousStudy Module
- When loading previous studies
- Comparison field auto-fill is skipped for excluded modalities

### Header Updates
- When `ShouldSkipHeaderUpdateForXR()` is called
- Header component updates are skipped for excluded modalities

### Automation Module: If Modality with Header
- Add `If Modality with Header` to automation sequences when you only want certain steps to run for header-enabled modalities.
- If the current study modality **is not** in the exclusion list, the next modules execute normally.
- If the modality **is** excluded, execution jumps to the matching `End if` (or optional else-branch) so header-only logic is skipped automatically.
- Uses the same LOINC-based modality detection as Send Report and Comparison updates, so XR/CR/DX rules stay consistent.

---

## Storage

**Location**: `%LOCALAPPDATA%\Wysg.Musm\Radium\settings.dat` (encrypted)  
**Key**: `modalities_no_header_update`  
**Format**: Comma or semicolon separated string (e.g., "XR,CR,DX")

---

## Code Reference

### Read Setting
```csharp
var modalitiesNoHeaderUpdate = _localSettings.ModalitiesNoHeaderUpdate ?? string.Empty;
```

### Parse List
```csharp
var excludedModalities = modalitiesNoHeaderUpdate
    .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
    .Select(m => m.Trim().ToUpperInvariant())
    .Where(m => !string.IsNullOrEmpty(m))
    .ToList();
```

### Check Exclusion
```csharp
bool shouldSkip = excludedModalities.Contains(currentModality.ToUpperInvariant());
```

---

## Migration from Old Setting

**Old**: "Do not update header in XR" checkbox (checked)  
**New**: Enter "XR" in textbox

**Old**: "Do not update header in XR" checkbox (unchecked)  
**New**: Leave textbox empty

---
## Related Documents- `ENHANCEMENT_2025-11-10_ModalitiesNoHeaderUpdate.md` - Full specification- `IMPLEMENTATION_SUMMARY_AddPreviousStudyComparison.md` - Integration with AddPreviousStudy