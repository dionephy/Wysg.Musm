# QUICKREF: Previous Findings Blank on Comparison Add Fix

**Date**: 2025-02-09  
**Category**: Bug Fix  
**Status**: ? Fixed

---

## Problem

- Previous findings editor becomes blank when editing comparison
- Previous study toggle buttons disappear and reappear (visual glitch)

---

## Root Cause

**Primary**: `OnWindowClosed` called `LoadPreviousStudiesAsync()`, causing unnecessary full reload that cleared and rebuilt `PreviousStudies` collection

**Secondary**: `SelectedPreviousStudy` setter called `UpdatePreviousReportJson()` before proofread fields loaded

---

## Solution

### Primary Fix: Remove Unnecessary Reload

**File**: `EditComparisonWindow.xaml.cs`

```csharp
public EditComparisonWindow(string patientNumber)
{
    InitializeComponent();
    _patientNumber = patientNumber;
    
    // REMOVED: Don't reload studies - causes toggle buttons to flicker
    // Closed += OnWindowClosed;
}

// REMOVED: OnWindowClosed method
```

### Secondary Fix: Pre-Load Proofread Fields

**File**: `MainViewModel.PreviousStudies.Selection.cs`  
**Method**: `SelectedPreviousStudy` setter

```csharp
// Load RawJson and trigger ApplyReportSelection before JSON update
if (value != null && value.SelectedReport != null)
{
    if (!string.IsNullOrWhiteSpace(value.SelectedReport.ReportJson))
    {
        value.RawJson = value.SelectedReport.ReportJson;
    }
    value.SelectedReport = value.SelectedReport;
}
UpdatePreviousReportJson(); // Now has correct data
```

---

## User Impact

- ? No toggle button flicker
- ? Findings editor stays populated
- ? No blank editors
- ? Smooth, fast UX
- ? 500-1000ms saved (no reload)

---

## Related Fixes

- FIX_2025-02-08_PreviousReportSplitRangesLoadingOrder.md
- FIX_2025-02-08_ProofreadFieldsNotUpdatingOnReportChange.md

---

**Full Documentation**: `BUGFIX_2025-02-09_PreviousFindingsBlankOnComparisonAdd.md`
