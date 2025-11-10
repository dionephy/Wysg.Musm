# Fix: Previous Report Split Ranges Loading Order (2025-02-08)

**Status**: ? Fixed  
**Date**: 2025-02-08  
**Category**: Critical Bug Fix

---

## Problem Statement

When switching between reports in the Previous Report ComboBox, if Report B had no findings (empty `header_and_findings`), switching back to Report A would show a concatenated conclusion editor containing:
- Header from Report A
- Findings from Report A  
- Conclusion from Report A

Instead of showing just the conclusion.

### User Impact

- User selects Report A (has findings + split ranges) กๆ conclusion shows correctly
- User changes to Report B (no findings, no split ranges) กๆ conclusion shows correctly (empty or minimal)
- User changes back to Report A กๆ conclusion shows CONCATENATED content ?
- Expected: conclusion should show only the conclusion portion ?

### Example

**Report A JSON:**
```json
{
  "header_and_findings": "Clinical information: syncope\r\nNo acute hemorrhage.",
  "final_conclusion": "1. No acute intracranial hemorrhage.\r2. No acute skull fracture.",
  "PrevReport": {
    "header_and_findings_conclusion_splitter_to": 142
  }
}
```

**Report B JSON:**
```json
{
  "header_and_findings": "",
  "final_conclusion": "1. No acute intracranial hemorrhage.\r2. No acute skull fracture."
  // No PrevReport section - no split ranges
}
```

**Bug Sequence:**
1. Select Report A กๆ split ranges loaded correctly (hfCTo=142)
2. Select Report B กๆ split ranges cleared (hfCTo=0) because no PrevReport section
3. Select Report A again:
   - **BUG**: `Findings` set to Report A's value กๆ triggers property change กๆ computes splits with `hfCTo=0` (stale from Report B)
   - Conclusion computed as `hf[0..142] + fc[0..64]` กๆ concatenates everything!
   - **THEN** split ranges loaded from Report A's JSON (too late!)

---

## Root Cause

The `ApplyReportSelection()` method was loading data in the wrong order:

```csharp
// OLD ORDER (WRONG):
Findings = rep.Findings;  // ก็ Property change fires, computes splits with STALE ranges (0 from Report B)
Conclusion = rep.Conclusion;  // ก็ Same issue
RawJson = rep.ReportJson;  // ก็ Updates JSON
LoadProofreadFieldsFromRawJson();  // ก็ Loads correct split ranges, but TOO LATE
```

When `Findings` was set, it triggered property change events which called `UpdatePreviousReportJson()`, which computed split outputs using the **stale split ranges from Report B** (all zeros because Report B had no `PrevReport` section).

By the time `LoadProofreadFieldsFromRawJson()` loaded the correct split ranges from Report A's JSON, the damage was already done - `ConclusionOut` had been computed with wrong ranges and the editor was already showing the concatenated content.

---

## Solution

Changed `ApplyReportSelection()` to load split ranges **BEFORE** setting `Findings` and `Conclusion`:

```csharp
// NEW ORDER (CORRECT):
RawJson = rep.ReportJson;  // ก็ Update JSON first
LoadProofreadFieldsFromRawJson();  // ก็ Load correct split ranges BEFORE setting fields
Findings = rep.Findings;  // ก็ Property change fires, computes splits with CORRECT ranges
Conclusion = rep.Conclusion;  // ก็ Same, now correct
```

Now when `Findings` is set and triggers property changes, the split ranges are already correct, so `UpdatePreviousReportJson()` computes the split outputs correctly.

---

## Code Changes

**File**: `MainViewModel.PreviousStudies.Models.cs`

### Before

```csharp
public void ApplyReportSelection(PreviousReportChoice? rep)
{
    if (rep == null)
    {
        // Clear fields
        Findings = string.Empty;  // ก็ Wrong order
        Conclusion = string.Empty;
        RawJson = string.Empty;
        LoadProofreadFieldsFromRawJson();
        return;
    }
    
    // Set original fields
    OriginalFindings = rep.Findings;
    OriginalConclusion = rep.Conclusion;
    Findings = rep.Findings;  // ก็ Property change fires with stale split ranges!
    Conclusion = rep.Conclusion;
    
    // Update JSON and load split ranges (too late!)
    RawJson = rep.ReportJson ?? string.Empty;
    LoadProofreadFieldsFromRawJson();
}
```

### After

```csharp
public void ApplyReportSelection(PreviousReportChoice? rep)
{
    if (rep == null)
    {
        // CRITICAL FIX: Update RawJson and load split ranges FIRST
        RawJson = string.Empty;
        LoadProofreadFieldsFromRawJson();  // ก็ Clears split ranges before setting fields
        
        // Then clear fields
        OriginalFindings = string.Empty;
        OriginalConclusion = string.Empty;
        Findings = string.Empty;
        Conclusion = string.Empty;
        return;
    }
    
    // CRITICAL FIX: Update RawJson FIRST
    RawJson = rep.ReportJson ?? string.Empty;
    
    // CRITICAL FIX: Load split ranges BEFORE setting Findings/Conclusion
    LoadProofreadFieldsFromRawJson();
    
    // Now set fields - split ranges are already correct
    OriginalFindings = rep.Findings;
    OriginalConclusion = rep.Conclusion;
    Findings = rep.Findings;  // ก็ Property change fires with CORRECT split ranges!
    Conclusion = rep.Conclusion;
}
```

---

## How It Works Now

### Flow After Fix

1. User changes ComboBox selection to Report A
2. `SelectedReport` setter calls `ApplyReportSelection(reportA)`
3. `RawJson` updated with Report A's JSON ?
4. `LoadProofreadFieldsFromRawJson()` parses JSON and loads split ranges ?
5. `Findings` set to Report A's value
6. Property change event fires กๆ `UpdatePreviousReportJson()` called
7. Split outputs computed using **correct split ranges from Report A** ?
8. `ConclusionOut` = `hf[142..142] + fc[0..64]` = only conclusion (correct!) ?

---

## Debug Output Comparison

### Before Fix

```
[PrevTab] ApplyReportSelection: applying report datetime=2025-11-10 11:54:40, findings len=142, conclusion len=64
[PrevTab] PropertyChanged -> Findings  ก็ WRONG: Fires with stale split ranges (hfCTo=0)
[PrevJson] Split computation details:
[PrevJson]   hf.Length=142, fc.Length=64
[PrevJson]   hfCTo=0  ก็ STALE value from Report B!
[PrevJson]   splitConclusion (hf[0..142] + fc[0..64]): len=208  ก็ CONCATENATED!
[PrevJson]   splitConclusion content: 'Clinical information...\nNo acute skull fracture.\n1. No acute intracranial hemorrhage.\n2. No acute skull fracture.'  ก็ BUG!
[PrevTab] ApplyReportSelection: Updated RawJson length=1229
[PrevTab] LoadProofreadFieldsFromRawJson: Loaded split ranges - HfHeaderFrom=72, HfHeaderTo=83  ก็ TOO LATE!
[PrevTab] PropertyChanged -> HfConclusionTo  ก็ Correct value loaded, but damage already done
[PrevJson]   hfCTo=142  ก็ NOW correct, but ConclusionOut already set wrong
```

### After Fix

```
[PrevTab] ApplyReportSelection: applying report datetime=2025-11-10 11:54:40, findings len=142, conclusion len=64
[PrevTab] ApplyReportSelection: Updated RawJson length=1229
[PrevTab] LoadProofreadFieldsFromRawJson: Loaded split ranges - HfHeaderFrom=72, HfHeaderTo=83  ก็ LOADED FIRST!
[PrevTab] PropertyChanged -> HfConclusionTo  ก็ Correct value loaded BEFORE setting Findings
[PrevJson]   hfCTo=142  ก็ CORRECT value ready
[PrevTab] PropertyChanged -> Findings  ก็ CORRECT: Fires with correct split ranges
[PrevJson] Split computation details:
[PrevJson]   hf.Length=142, fc.Length=64
[PrevJson]   hfCTo=142  ก็ CORRECT value!
[PrevJson]   splitConclusion (hf[142..142] + fc[0..64]): len=64  ก็ CORRECT!
[PrevJson]   splitConclusion content: '1. No acute intracranial hemorrhage.\n2. No acute skull fracture.'  ก็ ONLY CONCLUSION!
```

---

## Testing

### Test Case 1: Switch from Report with Split Ranges to Report without Split Ranges and Back

**Steps:**
1. Select Report A (has findings, has split ranges)
2. Verify conclusion editor shows only conclusion
3. Select Report B (no findings, no split ranges)
4. Verify conclusion editor shows only conclusion (or empty)
5. Select Report A again
6. Verify conclusion editor shows only conclusion (NOT concatenated)

**Before Fix:**
- ? Step 6: Conclusion shows header + findings + conclusion (concatenated)

**After Fix:**
- ? Step 6: Conclusion shows only conclusion

---

### Test Case 2: Multiple Report Switches

**Steps:**
1. Select Report A (has split ranges)
2. Select Report B (no split ranges)
3. Select Report C (has different split ranges)
4. Select Report A again
5. Verify each report shows correct split content

**Expected Result:**
- ? Each report displays correctly regardless of previous selection
- ? No concatenation or stale split range issues

---

### Test Case 3: Null Report Selection

**Steps:**
1. Select Report A
2. Change to "no selection" (null)
3. Verify editors clear
4. Select Report A again
5. Verify Report A displays correctly

**Expected Result:**
- ? Clearing and reselecting works correctly
- ? Split ranges loaded in correct order

---

## Build Status

? **Build successful with no compilation errors**

---

## Related Fixes

This completes the series of previous report selection fixes:

1. ? **2025-02-08**: Proofread fields not updating to JSON
2. ? **2025-02-08**: Proofread fields not updating on report change  
3. ? **2025-02-08**: **Split ranges loading order** (this fix)

All previous report selection and editing features now work correctly!

---

## Summary

The issue was that split ranges were being loaded AFTER setting `Findings` and `Conclusion`, causing split output computation to use stale/cleared split ranges from the previously selected report. The fix reorders the operations to load split ranges BEFORE setting the report fields, ensuring split outputs are always computed with correct ranges.

**Key Points:**
- ? Split ranges loaded before setting report fields
- ? Split outputs computed with correct ranges
- ? No more concatenated conclusion editors
- ? Works correctly when switching between reports with and without split ranges
- ? No breaking changes to existing functionality

This was the final piece of the previous report selection puzzle!
