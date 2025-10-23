# Diagnosis: Reportify Numbering Every Line (Line Mode Active)

**Date**: 2025-01-23  
**Issue**: Conclusion text being numbered as individual lines instead of paragraphs  
**Status**: ?? Investigating

---

## User's Input

```
Chronologic change and decreased amount of ICH in left frontal lobe with decreased edema and mass effect 
 almost resolved other contusional hemorrhages and SAH in both frontal and temporal lobes 
 chronologic change of SDH in left cerebral convexity 

 decreased amount of extra-axial fluid collection in right cerebral convexity 
 
partially resolved compression of left lateral ventricle 
 partially resolved midline shifting to the right side 

 otherwise no significant interval change
```

---

## Current Output (Wrong - Line Mode)

```
1. Chronologic change and decreased amount of ICH in left frontal lobe with decreased edema and mass effect.
2. Almost resolved other contusional hemorrhages and SAH in both frontal and temporal lobes.
3. Chronologic change of SDH in left cerebral convexity.
4. Decreased amount of extra-axial fluid collection in right cerebral convexity.
5. Partially resolved compression of left lateral ventricle.
6. Partially resolved midline shifting to the right side.
7. Otherwise no significant interval change.
```

---

## Expected Output (Paragraph Mode)

```
1. Chronologic change and decreased amount of ICH in left frontal lobe with decreased edema and mass effect.
   Almost resolved other contusional hemorrhages and SAH in both frontal and temporal lobes.
   Chronologic change of SDH in left cerebral convexity.

2. Decreased amount of extra-axial fluid collection in right cerebral convexity.

3. Partially resolved compression of left lateral ventricle.
   Partially resolved midline shifting to the right side.

4. Otherwise no significant interval change.
```

---

## Analysis

### Paragraph Detection

Looking at the input, the paragraphs should be:

**Paragraph 1** (3 lines):
```
Chronologic change and decreased amount of ICH in left frontal lobe with decreased edema and mass effect 
 almost resolved other contusional hemorrhages and SAH in both frontal and temporal lobes 
 chronologic change of SDH in left cerebral convexity 
```

**Blank line separator** (one blank line)

**Paragraph 2** (1 line):
```
 decreased amount of extra-axial fluid collection in right cerebral convexity 
```

**Blank line separator** (two blank lines ¡æ normalized to one)

**Paragraph 3** (2 lines):
```
partially resolved compression of left lateral ventricle 
 partially resolved midline shifting to the right side 
```

**Blank line separator** (one blank line)

**Paragraph 4** (1 line):
```
 otherwise no significant interval change
```

---

## Problem

The code is treating this as **LINE MODE** where:
- Every non-blank line gets numbered
- All blank lines are removed
- No continuation line indentation

This suggests `NumberConclusionLinesOnOneParagraph = true` (line mode is active).

---

## Verification Needed

Check the reportify settings JSON to see if `number_conclusion_lines_on_one_paragraph` is `true` or `false`.

**Expected** (paragraph mode): `"number_conclusion_lines_on_one_paragraph": false`  
**Current** (likely): `"number_conclusion_lines_on_one_paragraph": true`

---

## Solution

If the setting is wrong:
1. Open Settings ¡æ Reportify tab
2. Find "Number each line (line mode)" checkbox
3. **Uncheck** it (should be OFF for paragraph mode)
4. Click "Save Reportify Settings"
5. Try reportify again

If the setting is correct but behavior is wrong:
- There may be a bug in the paragraph detection logic
- The split by `\n\n` may not be working correctly
- Need to debug the `ApplyReportifyBlock` method

---

## Code Location

**File**: `apps/Wysg.Musm.Radium/ViewModels/MainViewModel.ReportifyHelpers.cs`

**Method**: `ApplyReportifyBlock(string input, bool isConclusion)`

**Line Mode Logic**:
```csharp
if (cfg.NumberConclusionLinesOnOneParagraph)
{
    // LINE MODE: Number each line, remove all blank lines
    var lines = input.Split('\n');
    // ... numbers every non-blank line ...
}
```

**Paragraph Mode Logic**:
```csharp
else
{
    // PARAGRAPH MODE: Number paragraphs, preserve blank lines
    var paras = input.Split("\n\n", StringSplitOptions.None);
    // ... numbers each paragraph ...
}
```

---

## Diagnostic Steps

1. **Check Settings UI**:
   - Open Settings ¡æ Reportify
   - Look for "Number each line (line mode)" checkbox
   - Current state: ???

2. **Check JSON**:
   - Open Settings ¡æ Reportify
   - Look at the JSON output
   - Find `"number_conclusion_lines_on_one_paragraph"`
   - Current value: ???

3. **Test with Known Input**:
   - Try a simple test:
     ```
     paragraph one line one
     paragraph one line two
     
     paragraph two
     ```
   - Expected (paragraph mode): Numbers 1 and 2 with continuation
   - Actual: ???

---

## Next Steps

1. User should check their reportify settings
2. If settings show line mode OFF but behavior is line mode, we have a bug
3. If settings show line mode ON, user needs to turn it OFF

**Status**: Waiting for user confirmation of settings
