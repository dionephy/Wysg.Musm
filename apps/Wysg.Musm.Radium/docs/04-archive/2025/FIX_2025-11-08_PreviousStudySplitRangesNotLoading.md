# Fix: Previous Study Split Ranges Not Persisting to Database (2025-02-08)

**Status**: ? Fixed  
**Date**: 2025-02-08  
**Category**: Critical Bug Fix

---

## Problem Statement

Split range changes made via "Split Header" / "Split Conclusion" / "Split Findings" buttons were being saved correctly to the database, but when the report was reloaded (e.g., after closing and reopening the patient), the split ranges would reset to default values (`0, 0`).

### User Impact

- User splits a previous report using the split buttons
- Split appears to work correctly in the UI
- User clicks "Save Previous Study to DB" ¡æ JSON saved correctly with split ranges
- User closes patient and reopens ¡æ **Split ranges are lost!**
- Textboxes (Header temp, Findings split, Conclusion split) show unsplit content

---

## Root Cause

The loading logic in `LoadPreviousStudiesForPatientAsync` was only reading the **text fields** from the JSON (e.g., `header_and_findings`, `final_conclusion`, `findings_proofread`, etc.), but it was **completely skipping** the `PrevReport` section which contains the split range values.

### What Was Happening

1. **Save**: Split ranges correctly written to `PrevReport` section:
   ```json
   "PrevReport": {
     "header_and_findings_header_splitter_from": 36,
     "header_and_findings_header_splitter_to": 47,
     ...
   }
   ```

2. **Load**: `LoadPreviousStudiesForPatientAsync` reads JSON but skips `PrevReport` section
   - `tab.HfHeaderFrom` never set ¡æ remains `null`
   - `tab.HfHeaderTo` never set ¡æ remains `null`

3. **JSON Regeneration**: `UpdatePreviousReportJson()` uses default values when properties are `null`:
   ```csharp
   int hfFrom = Clamp(tab.HfHeaderFrom ?? 0, 0, hf.Length);  // null becomes 0!
   int hfTo = Clamp(tab.HfHeaderTo ?? 0, 0, hf.Length);      // null becomes 0!
   ```

4. **Result**: Split ranges overwritten with defaults (`0, 0`) in regenerated JSON

---

## Solution

Enhanced `LoadPreviousStudiesForPatientAsync` to read split range properties from the `PrevReport` section of the JSON and populate them into the `PreviousStudyTab` object.

### Code Changes

**File**: `MainViewModel.PreviousStudiesLoader.cs`

**Added Variables**:
```csharp
// CRITICAL FIX: Variables to hold split range properties from PrevReport section
int? hfHeaderFrom = null;
int? hfHeaderTo = null;
int? hfConclusionFrom = null;
int? hfConclusionTo = null;
int? fcHeaderFrom = null;
int? fcHeaderTo = null;
int? fcFindingsFrom = null;
int? fcFindingsTo = null;
```

**Added JSON Parsing**:
```csharp
// CRITICAL FIX: Read split ranges from PrevReport section
if (root.TryGetProperty("PrevReport", out var prevReport) && prevReport.ValueKind == JsonValueKind.Object)
{
    int? GetInt(string name)
    {
        if (prevReport.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out var i))
            return i;
        return null;
    }
    
    hfHeaderFrom = GetInt("header_and_findings_header_splitter_from");
    hfHeaderTo = GetInt("header_and_findings_header_splitter_to");
    hfConclusionFrom = GetInt("header_and_findings_conclusion_splitter_from");
    hfConclusionTo = GetInt("header_and_findings_conclusion_splitter_to");
    fcHeaderFrom = GetInt("final_conclusion_header_splitter_from");
    fcHeaderTo = GetInt("final_conclusion_header_splitter_to");
    fcFindingsFrom = GetInt("final_conclusion_findings_splitter_from");
    fcFindingsTo = GetInt("final_conclusion_findings_splitter_to");
    
    Debug.WriteLine($"[PrevLoad] Loaded split ranges from DB: HfHeaderFrom={hfHeaderFrom}, HfHeaderTo={hfHeaderTo}");
}
```

**Added Tab Property Population**:
```csharp
// CRITICAL FIX: Populate split range properties in the tab
tab.HfHeaderFrom = hfHeaderFrom;
tab.HfHeaderTo = hfHeaderTo;
tab.HfConclusionFrom = hfConclusionFrom;
tab.HfConclusionTo = hfConclusionTo;
tab.FcHeaderFrom = fcHeaderFrom;
tab.FcHeaderTo = fcHeaderTo;
tab.FcFindingsFrom = fcFindingsFrom;
tab.FcFindingsTo = fcFindingsTo;
```

---

## How It Works Now

### Save Flow (unchanged)

1. User clicks "Split Header" button
2. `OnSplitHeaderTop()` updates `tab.HfHeaderFrom` and `tab.HfHeaderTo`
3. `UpdatePreviousReportJson()` generates JSON with split ranges:
   ```json
   "PrevReport": {
     "header_and_findings_header_splitter_from": 36,
     "header_and_findings_header_splitter_to": 47,
     ...
   }
   ```
4. User clicks "Save" ¡æ JSON saved to database

### Load Flow (NOW FIXED)

1. `LoadPreviousStudiesForPatientAsync()` reads JSON from database
2. **NEW**: Parses `PrevReport` section and extracts split ranges
3. **NEW**: Populates `tab.HfHeaderFrom`, `tab.HfHeaderTo`, etc. with loaded values
4. When `UpdatePreviousReportJson()` runs, it uses the **loaded values** instead of defaults:
   ```csharp
   int hfFrom = Clamp(tab.HfHeaderFrom ?? 0, 0, hf.Length);  // 36 (from DB)
   int hfTo = Clamp(tab.HfHeaderTo ?? 0, 0, hf.Length);      // 47 (from DB)
   ```
5. Split outputs (Header temp, Findings split, Conclusion split) calculated correctly

---

## Testing

### Test Case 1: Split Ranges Persist After Reload

**Steps:**
1. Open previous study A
2. In "Previous Header and Findings" textbox, select characters 36-47
3. Click "Split Header" button
4. Verify "Header (temp)" shows the split text
5. Click "Save Previous Study to DB" button
6. Close patient
7. Reopen same patient
8. Open previous study A

**Before Fix:**
- ? "Header (temp)" is empty
- ? "Findings (split)" shows full text (not split)
- ? JSON shows `"header_and_findings_header_splitter_from": 0`

**After Fix:**
- ? "Header (temp)" shows the split text
- ? "Findings (split)" shows correctly split content
- ? JSON shows `"header_and_findings_header_splitter_from": 36`

---

### Test Case 2: Multiple Split Operations Persist

**Steps:**
1. Open previous study A
2. Split header at position 36-47
3. Split conclusion at position 200-250
4. Click "Save Previous Study to DB"
5. Close and reopen patient
6. Open previous study A

**Expected Result:**
- ? Both splits preserved
- ? Header temp shows correct content
- ? Findings split shows middle portion
- ? Conclusion split shows correct content

---

### Test Case 3: Reports Without Splits Still Work

**Steps:**
1. Open previous study B (never split)
2. Verify content displays correctly
3. Click "Save Previous Study to DB"
4. Close and reopen patient
5. Open previous study B

**Expected Result:**
- ? Content unchanged
- ? No errors in debug output
- ? Split range properties remain `null` (as expected)

---

## Debug Output

### Before Fix

```
[PrevLoad] JSON parse error: (none)
[PrevJson] Update (tab, from raw DB JSON) htLen=0 hfLen=262 fcLen=187
```

No debug output about split ranges ¡æ they were never loaded!

### After Fix

```
[PrevLoad] Loaded split ranges from DB: HfHeaderFrom=36, HfHeaderTo=47
[PrevJson] Update (tab, from raw DB JSON) htLen=35 hfLen=262 fcLen=187
```

Split ranges now loaded and used in JSON generation!

---

## Related Fixes

This fix completes the split range persistence feature chain:

1. **2025-02-08**: Disabled auto-save on tab switch ([FIX_2025-02-08_DisableAutoSaveOnPreviousTabSwitch.md](FIX_2025-02-08_DisableAutoSaveOnPreviousTabSwitch.md))
2. **2025-02-08**: Fixed "Save" button not syncing JSON ([FIX_2025-02-08_SaveButtonNotUpdatingPreviousStudyJSON.md](FIX_2025-02-08_SaveButtonNotUpdatingPreviousStudyJSON.md))
3. **2025-02-08**: ? **THIS FIX** - Fixed split ranges not loading from database

---

## Build Status

? **Build successful with no compilation errors**

---

## Summary

The split range persistence issue was caused by incomplete JSON loading. The save operation was working correctly, but the load operation was only reading text fields and completely ignoring the split range values stored in the `PrevReport` section.

**Key Points:**
- ? Split ranges now load from database correctly
- ? Split UI state persists across sessions
- ? No changes to save logic (it was already correct)
- ? Backward compatible (reports without splits still work)
- ? Debug logging added for verification

The implementation is complete and resolves the issue completely!
