# QUICK REFERENCE: Copy Study Remark to Chief Complaint Toggle

**Date**: 2025-02-10  
**Feature**: Copy Study Remark to Chief Complaint Toggle

## Quick Summary

Added a "copy" toggle button that controls whether Study Remark is automatically copied to Chief Complaint during GetStudyRemark automation.

## Key Changes

### UI (XAML)
- **Location**: Row 0, Column 2 in `ReportInputsAndJsonPanel.xaml`
- **Control**: ToggleButton with "copy" label
- **Binding**: `CopyStudyRemarkToChiefComplaint` property

### Settings Persistence
- **Interface**: `IRadiumLocalSettings.cs`
- **Implementation**: `RadiumLocalSettings.cs`
- **Storage**: Encrypted DPAPI file (`settings.dat`)
- **All Toggles Saved**:
  - CopyStudyRemarkToChiefComplaint
  - AutoChiefComplaint
  - AutoPatientHistory
  - AutoConclusion
  - AutoChiefComplaintProofread
  - AutoPatientHistoryProofread
  - AutoFindingsProofread
  - AutoConclusionProofread

### Behavior Logic
- **File**: `MainViewModel.Commands.Automation.cs`
- **Method**: `AcquireStudyRemarkAsync()`
- **Logic**:
  ```csharp
  StudyRemark = s; // Always filled
  
  if (CopyStudyRemarkToChiefComplaint)
      ChiefComplaint = s; // Only if toggle is ON
  ```

### Mutual Exclusion
- **File**: `MainViewModel.Commands.Init.cs`
- **Pattern**: Copy ON ? Auto OFF
  - When Copy turns ON ¡æ Auto turns OFF
  - When Auto turns ON ¡æ Copy turns OFF

## Usage

### For Users
1. **Enable copying**: Click "copy" toggle to ON (blue border)
2. **Disable copying**: Click "copy" toggle to OFF (default border)
3. **Settings persist**: Toggle state is saved automatically

### For Developers
```csharp
// Check toggle state
if (CopyStudyRemarkToChiefComplaint)
{
    // Copy logic here
}

// Save toggle changes
SaveToggleSettings(); // Called automatically in property setters

// Load toggles on startup
LoadToggleSettings(); // Called in MainViewModel constructor
```

## Files Modified

| File | Changes |
|------|---------|
| `ReportInputsAndJsonPanel.xaml` | Added "copy" toggle button |
| `IRadiumLocalSettings.cs` | Added 8 toggle properties |
| `RadiumLocalSettings.cs` | Implemented bool read/write helpers |
| `MainViewModel.Commands.Init.cs` | Added property, mutual exclusion, save/load |
| `MainViewModel.Commands.Automation.cs` | Conditional ChiefComplaint filling |
| `MainViewModel.cs` | LoadToggleSettings() call in constructor |

## Toggle States

| Copy | Auto | Result |
|------|------|--------|
| ON | OFF | Study Remark copied to Chief Complaint |
| OFF | ON | AI generates Chief Complaint |
| OFF | OFF | Chief Complaint unchanged |
| - | - | ? Both ON (impossible - mutual exclusion) |

## Debug Logging

```
[Automation][GetStudyRemark] Copy toggle ON - Set ChiefComplaint property: 'value'
[Automation][GetStudyRemark] Copy toggle OFF - ChiefComplaint not updated
[MainViewModel] Toggle settings saved to local settings
[MainViewModel] Toggle settings loaded from local settings
```

## Testing Checklist

- ? Copy toggle ON ¡æ Study Remark copied
- ? Copy toggle OFF ¡æ Study Remark NOT copied
- ? Copy ON ¡æ Auto turns OFF
- ? Auto ON ¡æ Copy turns OFF
- ? Settings persist across app restarts
- ? Build successful (no errors)

---

**Quick Ref Version**: 1.0  
**Last Updated**: 2025-02-10
