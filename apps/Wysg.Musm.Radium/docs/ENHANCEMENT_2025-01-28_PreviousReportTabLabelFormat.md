# Enhancement: Previous Report Tab Label Format

**Date**: 2025-01-28  
**Type**: UI Enhancement  
**Status**: ? Implemented  
**Build**: ? Success

---

## Overview

Updated the previous report tabs to display "{Modality} {Study Date}" format instead of "{Study Date} {Modality}", and changed unknown modality from "UNK" to "OT" (Other).

---

## Changes

### 1. Tab Label Format

**Before**:
```
2025-10-25 CT
2025-09-15 MR
2024-12-01 UNK
```

**After**:
```
CT 2025-10-25
MR 2025-09-15
OT 2024-12-01
```

### 2. Unknown Modality Label

**Before**: `UNK` (Unknown)  
**After**: `OT` (Other)

This provides a more professional and concise label for studies where the modality cannot be determined from the study name.

---

## Implementation Details

### File 1: `MainViewModel.ReportifyHelpers.cs`

**Method**: `ExtractModality(string? studyName)`

**Change**:
```csharp
// BEFORE:
private string ExtractModality(string? studyName)
{
    if (string.IsNullOrWhiteSpace(studyName)) return "UNK";
    var m = _rxModality.Match(studyName); 
    if (!m.Success) return "UNK";
    // ...
}

// AFTER:
private string ExtractModality(string? studyName)
{
    if (string.IsNullOrWhiteSpace(studyName)) return "OT"; // Changed from "UNK" to "OT" (Other)
    var m = _rxModality.Match(studyName); 
    if (!m.Success) return "OT"; // Changed from "UNK" to "OT" (Other)
    // ...
}
```

### File 2: `MainViewModel.PreviousStudiesLoader.cs`

**Method**: `LoadPreviousStudiesForPatientAsync(string patientId)`

**Changes**:
1. Fixed modality extraction to use correct property name
2. Changed Title format from `"{Date} {Modality}"` to `"{Modality} {Date}"`

```csharp
// BEFORE:
string modality = ExtractModality(StudyName); // Wrong property!
var tab = new PreviousStudyTab
{
    // ...
    Title = $"{g.Key.StudyDateTime:yyyy-MM-dd} {modality}" // Old format
};

// AFTER:
string modality = ExtractModality(g.Key.Studyname); // Fixed: use g.Key.Studyname
var tab = new PreviousStudyTab
{
    // ...
    Title = $"{modality} {g.Key.StudyDateTime:yyyy-MM-dd}" // New format
};
```

---

## Examples

### CT Scan
- **Before**: `2025-10-25 CT`
- **After**: `CT 2025-10-25`

### MRI Scan
- **Before**: `2025-09-15 MR`
- **After**: `MR 2025-09-15`

### PET-CT Scan
- **Before**: `2025-08-20 PETCT`
- **After**: `PETCT 2025-08-20`

### Unknown/Other Modality
- **Before**: `2024-12-01 UNK`
- **After**: `OT 2024-12-01`

---

## Supported Modalities

The modality extraction regex recognizes the following modalities:

| Pattern | Normalized | Description |
|---------|------------|-------------|
| `CT` | `CT` | Computed Tomography |
| `MRI`, `MR` | `MR` | Magnetic Resonance Imaging |
| `XR`, `CR`, `DX` | `XR`, `CR`, `DX` | X-Ray variants |
| `US` | `US` | Ultrasound |
| `PET-CT`, `PET CT`, `PETCT`, `PET` | `PETCT`, `PET` | PET/PET-CT |
| `MAMMO`, `MMG` | `MAMMO` | Mammography |
| `DXA` | `DXA` | Bone Density |
| `NM` | `NM` | Nuclear Medicine |
| *(none)* | `OT` | Other/Unknown |

**Regex Pattern**: `\b(CT|MRI|MR|XR|CR|DX|US|PET[- ]?CT|PETCT|PET|MAMMO|MMG|DXA|NM)\b`

---

## Benefits

### User Experience
- ? **Modality First**: More intuitive - users typically think "I want the CT scan from October" rather than "I want the October scan which was a CT"
- ? **Scannable**: Easier to scan tabs by modality when it's the first element
- ? **Professional**: "OT" looks more professional than "UNK"
- ? **Compact**: Shorter unknown label saves space in tab strip

### Visual Grouping
When multiple studies are loaded, modalities are grouped visually:
```
CT 2025-10-25
CT 2025-09-15
MR 2025-08-10
MR 2025-07-05
OT 2025-06-01
```

This makes it easier to find all studies of a specific modality.

---

## Testing

### Test Case 1: Standard Modalities
**Input Studies**:
- Study: "CT Abdomen" (Date: 2025-10-25)
- Study: "MRI Brain" (Date: 2025-09-15)

**Tab Labels**:
- `CT 2025-10-25` ?
- `MR 2025-09-15` ?

### Test Case 2: Unknown Modality
**Input Study**:
- Study: "General Examination" (Date: 2024-12-01)

**Tab Label**:
- `OT 2024-12-01` ? (not "UNK 2024-12-01")

### Test Case 3: PET-CT Variants
**Input Studies**:
- Study: "PET-CT Whole Body" (Date: 2025-08-20)
- Study: "PET CT Oncology" (Date: 2025-07-10)

**Tab Labels**:
- `PETCT 2025-08-20` ?
- `PETCT 2025-07-10` ?

---

## Code Location

### Modified Files
1. **apps/Wysg.Musm.Radium/ViewModels/MainViewModel.ReportifyHelpers.cs**
   - Method: `ExtractModality(string? studyName)`
   - Change: Return "OT" instead of "UNK"

2. **apps/Wysg.Musm.Radium/ViewModels/MainViewModel.PreviousStudiesLoader.cs**
   - Method: `LoadPreviousStudiesForPatientAsync(string patientId)`
   - Changes:
     - Fixed: `ExtractModality(g.Key.Studyname)` (was using wrong property)
     - Changed: Title format to `"{modality} {date}"`

---

## Build Status

```
Command: run_build
Result: ºôµå ¼º°ø (Build Success)
Errors: 0
Warnings: 0
```

---

## Related Files

### UI Components
- **apps/Wysg.Musm.Radium/Controls/PreviousStudiesStrip.xaml**
  - Tab strip control (no changes required - binds to Title property)

### Data Models
- **apps/Wysg.Musm.Radium/ViewModels/MainViewModel.PreviousStudies.cs**
  - `PreviousStudyTab` model with `Title`, `Modality`, and `StudyDateTime` properties

---

**Status**: ? Implemented and Verified  
**Build**: ? Success  
**Deployed**: Ready for production