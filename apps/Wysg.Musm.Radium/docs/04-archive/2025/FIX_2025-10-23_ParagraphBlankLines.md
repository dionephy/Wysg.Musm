# FIX: Paragraph Mode Preserving Blank Lines

**Date**: 2025-10-23  
**Issue**: Paragraph mode not preserving blank lines between paragraphs  
**Status**: ? Fixed  
**Build**: ? Success

---

## Problem

In paragraph mode, blank lines between paragraphs were not being preserved properly. The user expected:

**Input**:
```
no acute intracranial hemorrhage
no acute skull fracture

Diffuse brain atrophy with periventricular leukoaraiosis
```

**Expected Output**:
```
1. No acute intracranial hemorrhage.
   No acute skull fracture.

2. Diffuse brain atrophy with periventricular leukoaraiosis.
```

**Actual Output** (before fix):
```
1. No acute intracranial hemorrhage.
   No acute skull fracture.
2. Diffuse brain atrophy with periventricular leukoaraiosis.
```
? No blank line between paragraphs

---

## Root Cause

The paragraph numbering logic was:
1. Split by `"\n\n"` (blank lines)
2. Number each paragraph
3. Join with `"\n\n"`

This should work, but the issue was that blank line normalization was happening **AFTER** paragraph processing in the line-level transformation loop, which could remove the preserved blank lines.

---

## Solution

### Key Changes

1. **Move blank line normalization BEFORE paragraph processing**
   - This ensures 3+ blank lines become exactly 2 newlines (one blank line)
   - Happens before paragraph numbering

2. **Preserve blank lines in line-level processing**
   - Don't modify blank lines during capitalization/punctuation pass
   - Keep them as empty strings

### Code Changes

**Before**:
```csharp
// Paragraph numbering
if (isConclusion && cfg.NumberConclusionParagraphs) {
    // ... number paragraphs ...
    input = string.Join("\n\n", resultParas);
}

// THEN normalize blank lines (too late!)
if (cfg.RemoveExcessiveBlankLines && (!isConclusion || !cfg.NumberConclusionParagraphs)) {
    input = RxBlankLines.Replace(input, "\n\n");
}
```

**After**:
```csharp
// Normalize blank lines FIRST (3+ newlines -> 2 newlines)
if (cfg.RemoveExcessiveBlankLines) {
    input = RxBlankLines.Replace(input, "\n\n");
}

// THEN paragraph numbering (preserves the normalized blank lines)
if (isConclusion && cfg.NumberConclusionParagraphs) {
    // ... number paragraphs ...
    input = string.Join("\n\n", resultParas);
}
```

---

## Testing

### Test Case 1: Two Paragraphs with Blank Line

**Input**:
```
no acute intracranial hemorrhage
no acute skull fracture

diffuse brain atrophy with periventricular leukoaraiosis
```

**Output**:
```
1. No acute intracranial hemorrhage.
   No acute skull fracture.

2. Diffuse brain atrophy with periventricular leukoaraiosis.
```
? Blank line preserved

---

### Test Case 2: Multiple Blank Lines (3+ newlines)

**Input**:
```
first paragraph


second paragraph
```

**After Normalization** (before numbering):
```
first paragraph

second paragraph
```

**After Numbering**:
```
1. First paragraph.

2. Second paragraph.
```
? Excessive blanks reduced to one blank line, then preserved

---

### Test Case 3: Line Mode (should remove all blanks)

**Input**:
```
first line

second line
```

**Output**:
```
1. First line.
2. Second line.
```
? All blank lines removed in line mode

---

## Modes Comparison

### Paragraph Mode (default)
- **Numbers**: Each paragraph (separated by blank lines)
- **Indents**: Continuation lines within a paragraph
- **Blank Lines**: PRESERVED (one blank line between paragraphs)
- **Use Case**: Multi-sentence findings with clear paragraph structure

**Example**:
```
Input:
  "finding A line 1
  finding A line 2

  finding B"

Output:
  "1. Finding A line 1.
     Finding A line 2.

  2. Finding B."
```

### Line Mode (`number_conclusion_lines_on_one_paragraph: true`)
- **Numbers**: Each non-blank line
- **Indents**: None
- **Blank Lines**: REMOVED (all blanks deleted)
- **Use Case**: Simple bullet-point lists

**Example**:
```
Input:
  "finding A

  finding B"

Output:
  "1. Finding A.
  2. Finding B."
```

---

## Processing Order

The correct processing order is now:

1. **Normalize line endings** (`\r\n` �� `\n`)
2. **Normalize excessive blank lines** (3+ newlines �� 2 newlines) ? MOVED UP
3. **Paragraph/Line numbering**
   - Paragraph mode: Number paragraphs, join with `\n\n`
   - Line mode: Number lines, join with `\n` (no blanks)
4. **Line-level transformations** (capitalization, punctuation, etc.)
   - Skip blank lines (don't modify them)

---

## Files Modified

**File**: `apps/Wysg.Musm.Radium/ViewModels/MainViewModel.ReportifyHelpers.cs`

**Method**: `ApplyReportifyBlock()`

**Changes**:
1. Moved `RemoveExcessiveBlankLines` normalization before paragraph processing
2. Ensured blank lines are preserved during line-level transformation loop
3. Added comments explaining the processing order

---

## Impact

### Positive
? **Paragraph Structure**: Properly preserved  
? **Readability**: Improved (clear paragraph separation)  
? **Consistency**: Blank line handling is predictable  
? **Backward Compatible**: Existing behavior for line mode unchanged

### No Breaking Changes
? **Line Mode**: Still removes all blanks  
? **Settings**: All existing config options work  
? **API**: No interface changes

---

## Summary

**Problem**: Blank lines between paragraphs were being removed  
**Cause**: Blank line normalization happened after paragraph processing  
**Solution**: Normalize blank lines FIRST, then number paragraphs  
**Result**: Paragraph mode now correctly preserves one blank line between numbered paragraphs  

**Status**: ? Fixed  
**Build**: ? Success
