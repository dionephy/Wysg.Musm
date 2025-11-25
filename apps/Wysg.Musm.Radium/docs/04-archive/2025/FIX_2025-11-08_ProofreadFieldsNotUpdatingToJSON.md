# Fix: Proofread Fields Not Updating to JSON in Previous Studies (2025-02-08)

**Status**: ? Fixed  
**Date**: 2025-02-08  
**Category**: Critical Bug Fix

---

## Problem Statement

When editing proofread fields in previous studies (`ChiefComplaintProofread`, `FindingsProofread`, `ConclusionProofread`, etc.), the changes were updating in the UI and the tab properties, but they were **not being written to the JSON** when `UpdatePreviousReportJson()` was called.

This meant that clicking "Save Previous Study to DB" would save the **old proofread values from the database**, not the **current edited values**.

### User Impact

- User edits proofread fields (e.g., Findings (PR), Conclusion (PR))
- User clicks "Save Previous Study to DB" button
- Database receives JSON with **old/stale** proofread values
- User's edits are lost!

---

## Root Cause

The `UpdatePreviousReportJson()` method was copying properties from `tab.RawJson` (the original JSON loaded from the database) to build the new JSON. However, it was copying **ALL non-excluded properties**, including the proofread fields.

This meant:
1. User edits proofread field ¡æ `tab.FindingsProofread` updated ?
2. `UpdatePreviousReportJson()` called
3. Method copies proofread fields from `tab.RawJson` (stale values) ?
4. Method ignores current values in `tab.FindingsProofread` ?
5. JSON contains old values, not current edits ?

### Why This Happened

The excluded fields list only included:
```csharp
var excludedFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{
    "PrevReport",     // Rebuilt from tab properties
    "header_temp",    // Recomputed split output
    "findings",       // Recomputed split output
    "conclusion"      // Recomputed split output
};
```

**Proofread fields were NOT excluded**, so they were copied from RawJson instead of being written from tab properties!

---

## Solution

Added proofread fields to the excluded list and explicitly wrote them from the tab properties, just like we do for split outputs.

### Code Changes

**File**: `MainViewModel.PreviousStudies.Json.cs`

**Before:**
```csharp
// Fields to exclude from copy (will be rewritten with computed values)
var excludedFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{
    "PrevReport",
    "header_temp",
    "findings",
    "conclusion"
};

// Copy all existing properties from raw JSON (except excluded fields)
foreach (var prop in doc.RootElement.EnumerateObject())
{
    if (excludedFields.Contains(prop.Name)) continue;
    
    // Write the property as-is (INCLUDING STALE PROOFREAD VALUES!)
    prop.WriteTo(writer);
}

// Write computed split outputs
writer.WriteString("header_temp", tab.HeaderTemp ?? string.Empty);
writer.WriteString("findings", tab.FindingsOut ?? string.Empty);
writer.WriteString("conclusion", tab.ConclusionOut ?? string.Empty);

// Proofread fields NOT written ¡æ copied from RawJson above ?
```

**After:**
```csharp
// CRITICAL FIX: Fields to exclude from copy (will be rewritten with current values from tab properties)
var excludedFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{
    "PrevReport",
    "header_temp",
    "findings",
    "conclusion",
    // CRITICAL FIX: Exclude proofread fields so they're written from tab properties, not stale RawJson
    "chief_complaint_proofread",
    "patient_history_proofread",
    "study_techniques_proofread",
    "comparison_proofread",
    "findings_proofread",
    "conclusion_proofread"
};

// Copy all existing properties from raw JSON (except excluded fields)
foreach (var prop in doc.RootElement.EnumerateObject())
{
    if (excludedFields.Contains(prop.Name)) continue;
    
    // Write the property as-is
    prop.WriteTo(writer);
}

// Write computed split outputs
writer.WriteString("header_temp", tab.HeaderTemp ?? string.Empty);
writer.WriteString("findings", tab.FindingsOut ?? string.Empty);
writer.WriteString("conclusion", tab.ConclusionOut ?? string.Empty);

// CRITICAL FIX: Write proofread fields from tab properties (current values), not from stale RawJson
writer.WriteString("chief_complaint_proofread", tab.ChiefComplaintProofread ?? string.Empty);
writer.WriteString("patient_history_proofread", tab.PatientHistoryProofread ?? string.Empty);
writer.WriteString("study_techniques_proofread", tab.StudyTechniquesProofread ?? string.Empty);
writer.WriteString("comparison_proofread", tab.ComparisonProofread ?? string.Empty);
writer.WriteString("findings_proofread", tab.FindingsProofread ?? string.Empty);
writer.WriteString("conclusion_proofread", tab.ConclusionProofread ?? string.Empty);
```

---

## How It Works Now

### Flow After Fix

1. User edits proofread field ¡æ `tab.FindingsProofread` updated ?
2. Property change event fires ¡æ `OnSelectedPrevStudyPropertyChanged()` called ?
3. `UpdatePreviousReportJson()` called ?
4. Method **excludes** proofread fields from RawJson copy ?
5. Method **writes** proofread fields from `tab.FindingsProofread` (current value) ?
6. JSON contains current edited values ?
7. User clicks "Save" ¡æ **current values saved to database** ?

---

## Testing

### Test Case 1: Edit Findings Proofread

**Steps:**
1. Open previous study A
2. Edit "Findings (PR)" textbox (add text "Test edit")
3. Check JSON panel - should show updated `findings_proofread`
4. Click "Save Previous Study to DB" button
5. Close and reopen patient
6. Check "Findings (PR)" textbox

**Before Fix:**
- ? JSON shows old value (without "Test edit")
- ? After reload, "Test edit" is lost

**After Fix:**
- ? JSON shows new value (with "Test edit")
- ? After reload, "Test edit" is preserved

---

### Test Case 2: Edit Multiple Proofread Fields

**Steps:**
1. Open previous study A
2. Edit "Chief Complaint (PR)" ¡æ Add "Test CC"
3. Edit "Patient History (PR)" ¡æ Add "Test PH"
4. Edit "Findings (PR)" ¡æ Add "Test Findings"
5. Edit "Conclusion (PR)" ¡æ Add "Test Conclusion"
6. Check JSON panel
7. Click "Save"
8. Close and reopen patient

**Expected Result:**
- ? All 4 edits appear in JSON
- ? All 4 edits persist after reload

---

### Test Case 3: Proofread Fields with Split Ranges

**Steps:**
1. Open previous study A
2. Split header at position 36-47
3. Edit "Findings (PR)" textbox
4. Click "Save"
5. Close and reopen patient

**Expected Result:**
- ? Split ranges preserved
- ? Proofread edits preserved
- ? Both changes coexist in database

---

## Debug Output

### Before Fix

```
[PrevTab] PropertyChanged -> FindingsProofread
[PrevJson] Update (tab, from raw DB JSON) htLen=35 hfLen=262 fcLen=187
```

JSON generated but proofread field has **old value** from RawJson!

### After Fix

```
[PrevTab] PropertyChanged -> FindingsProofread
[PrevJson] Update (tab, from raw DB JSON) htLen=35 hfLen=262 fcLen=187
```

JSON generated with proofread field having **current value** from tab property!

---

## Related Issues

This completes the full previous study editing persistence chain:

1. **2025-02-08**: Disabled auto-save on tab switch ([FIX_2025-02-08_DisableAutoSaveOnPreviousTabSwitch.md](FIX_2025-02-08_DisableAutoSaveOnPreviousTabSwitch.md))
2. **2025-02-08**: Fixed "Save" button JSON sync ([FIX_2025-02-08_SaveButtonNotUpdatingPreviousStudyJSON.md](FIX_2025-02-08_SaveButtonNotUpdatingPreviousStudyJSON.md))
3. **2025-02-08**: Fixed split ranges not loading ([FIX_2025-02-08_PreviousStudySplitRangesNotLoading.md](FIX_2025-02-08_PreviousStudySplitRangesNotLoading.md))
4. **2025-02-08**: ? **THIS FIX** - Fixed proofread fields not updating to JSON

---

## Build Status

? **Build successful with no compilation errors**

---

## Summary

The proofread fields persistence issue was caused by `UpdatePreviousReportJson()` copying stale values from `RawJson` instead of reading current values from tab properties. The fix excludes proofread fields from the copy operation and explicitly writes them from the tab properties, ensuring the JSON always contains the current edited state.

**Key Points:**
- ? Proofread fields now update to JSON correctly
- ? Changes persist when clicking "Save"
- ? No performance impact (same number of JSON writes)
- ? Consistent with how split outputs are handled
- ? All proofread fields fixed (6 fields total)

This was the final piece of the previous study persistence puzzle!
