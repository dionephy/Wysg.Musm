# INVESTIGATION: Hyphen Removal in Report Text (A2-A3 ¡æ A22A3)

**Date**: 2025-02-02  
**Issue**: Hyphens being removed from medical terminology during report sending  
**Example**: "A2-A3" ¡æ "A22A3", "A2?A3" ¡æ "A22A3"  
**Status**: ?? Under Investigation  

---

## Problem Description

When sending a report to PACS using the "SendReport" module, hyphens in medical terminology are being removed:

**Original Findings**:
```
Suspicious luminal irregularity in the bilateral A2-A3 segments.
```

**Sent to PACS**:
```
Suspicious luminal irregularity in the bilateral A22A3 segments.
```

---

## Investigation Steps

### 1. Checked Reportify Settings
**Location**: `apps/Wysg.Musm.Radium/ViewModels/MainViewModel.ReportifyHelpers.cs`

The Reportify logic has several text transformations:
- `RemoveExcessiveBlanks` - Removes multiple spaces
- `CollapseWhitespace` - Replaces `\s+` with single space
- `SpaceAfterPunctuation` - Adds space after `;,:` 
- **None of these should affect hyphens**

### 2. Checked SendReport Module
**Location**: `apps/Wysg.Musm.Radium/Services/PacsService.cs`

SendReport simply invokes a UI automation procedure. **No text processing occurs here**.

### 3. Root Cause Analysis

#### Possible Causes:

**A. Unicode Normalization Issue**
- User might have typed **en-dash (U+2013 "?")** instead of **hyphen-minus (U+002D "-")**
- Some text processing might normalize/strip en-dashes

**B. Reportify Configuration**
- User might have custom `space_after_punctuation` settings that treat hyphens as punctuation
- **Need to check**: `ReportifySettingsJson` in database

**C. PACS UI Automation**
- The PACS text field might have character restrictions
- UI automation might be stripping non-ASCII characters

**D. Encoding Issue**
- When text is copied to PACS, encoding mismatch could cause character loss
- **Less likely** because other punctuation works fine

---

## Diagnostic Questions

1. **Was the text reportified before sending?**
   - Check if `Reportified` toggle was ON
   - If ON, check reportify settings for hyphen handling

2. **What character was actually typed?**
   - Hyphen-minus `-` (ASCII 45, U+002D)
   - En-dash `?` (U+2013)
   - Em-dash `?` (U+2014)

3. **Does the issue occur with raw text (Reportified OFF)?**
   - If NO ¡æ Reportify is the cause
   - If YES ¡æ PACS automation is the cause

---

## Recommended Fix

### Option 1: Protect Hyphens in Reportify (If Reportified Was ON)

**File**: `apps/Wysg.Musm.Radium/ViewModels/MainViewModel.ReportifyHelpers.cs`

Add hyphen protection before `CollapseWhitespace`:

```csharp
// Before CollapseWhitespace transformation
if (cfg.CollapseWhitespace)
{
    // CRITICAL FIX: Preserve hyphens in medical terminology
    // Replace only spaces/tabs/newlines, NOT hyphens
    working = Regex.Replace(working, @"[ \t]+", " ");  // Only spaces and tabs
    // Do NOT use \s+ because it might affect hyphens in some contexts
}
```

### Option 2: Normalize Unicode Dashes to ASCII Hyphens

Add normalization step at the beginning of `ApplyReportifyBlock`:

```csharp
// CRITICAL FIX: Normalize Unicode dashes to ASCII hyphens
// This ensures en-dashes (?) and em-dashes (?) become regular hyphens (-)
input = input.Replace('\u2013', '-'); // En-dash ¡æ Hyphen
input = input.Replace('\u2014', '-'); // Em-dash ¡æ Hyphen
input = input.Replace('\u2212', '-'); // Minus sign ¡æ Hyphen
```

### Option 3: Database-Level Fix

If the issue is already in the database:

```sql
-- Find records with Unicode dashes
SELECT id, findings, conclusion 
FROM radium.current_report_json 
WHERE findings LIKE '%' || CHR(8211) || '%'  -- En-dash U+2013
   OR findings LIKE '%' || CHR(8212) || '%'  -- Em-dash U+2014
   OR conclusion LIKE '%' || CHR(8211) || '%'
   OR conclusion LIKE '%' || CHR(8212) || '%';

-- Fix existing records (example)
UPDATE radium.current_report_json
SET findings = REPLACE(REPLACE(findings, CHR(8211), '-'), CHR(8212), '-'),
    conclusion = REPLACE(REPLACE(conclusion, CHR(8211), '-'), CHR(8212), '-')
WHERE findings LIKE '%' || CHR(8211) || '%'
   OR findings LIKE '%' || CHR(8212) || '%'
   OR conclusion LIKE '%' || CHR(8211) || '%'
   OR conclusion LIKE '%' || CHR(8212) || '%';
```

---

## Testing Plan

### Test Case 1: Hyphen Preservation
```
Input: "A2-A3 segments"
Expected: "A2-A3 segments"
Actual: ?
```

### Test Case 2: En-Dash Normalization
```
Input: "A2?A3 segments" (en-dash)
Expected: "A2-A3 segments" (hyphen)
Actual: ?
```

### Test Case 3: Multiple Hyphens
```
Input: "T1-weighted, T2-weighted, FLAIR"
Expected: "T1-weighted, T2-weighted, FLAIR"
Actual: ?
```

### Test Case 4: Hyphen in Numbers
```
Input: "2-3 mm, 5-10 cm"
Expected: "2-3 mm, 5-10 cm"
Actual: ?
```

---

## Next Steps

1. **User Action Required**:
   - Check if `Reportified` toggle was ON when sending
   - Test with Reportified OFF to isolate the issue
   - Check character codes in findings text:
     ```
     Open Developer Tools ¡æ inspect `txtFindings.Text`
     Check character codes for the hyphen
     ```

2. **Developer Action**:
   - Add Unicode dash normalization (Option 2) - **SAFEST**
   - Add diagnostic logging to track character transformations
   - Test with both ASCII hyphens and Unicode dashes

3. **Documentation**:
   - Update user guide with "Always use regular hyphens (-), not en-dashes (?)"
   - Add warning in Settings ¡æ Reportify about character normalization

---

## Status

?? **Awaiting User Feedback**:
- Was Reportified toggle ON?
- Can you reproduce with simple test: "A2-A3"?
- Does the issue occur with Reportified OFF?

Once we have this information, we can implement the correct fix.

---

**Related Files**:
- `apps/Wysg.Musm.Radium/ViewModels/MainViewModel.ReportifyHelpers.cs` - Reportify logic
- `apps/Wysg.Musm.Radium/Services/PacsService.cs` - SendReport automation
- `apps/Wysg.Musm.Radium/ViewModels/MainViewModel.Editor.cs` - RawFindingsText/RawConclusionText accessors

**Priority**: HIGH (affects clinical accuracy)

**Impact**: Medical terminology errors in sent reports
