# Feature: Smart Conclusion Numbering Mode Detection

**Date**: 2025-01-28  
**Type**: Enhancement  
**Status**: ? Implemented  
**Build**: ? Success

---

## Overview

The "Number each line on one paragraph" setting now works **contextually** based on the input text structure:

- **Single paragraph** (no blank lines) ¡æ Setting is honored (line mode or paragraph mode)
- **Multiple paragraphs** (with blank lines) ¡æ **Force paragraph mode** (setting ignored)

This prevents inappropriate line-by-line numbering when the text has a clear paragraph structure.

---

## Problem

Users would enable "Number each line on one paragraph" expecting it to number every line, but when they had text with multiple paragraphs (separated by blank lines), it would still number every line, destroying the paragraph structure:

**Input** (multiple paragraphs with blank lines):
```
Finding 1
--> Detail A

Finding 2
--> Detail B
```

**Before Fix** (line mode applied inappropriately):
```
1. Finding 1.
2. --> Detail A.
3. Finding 2.
4. --> Detail B.
```
? Paragraph structure lost!

**After Fix** (smart detection):
```
1. Finding 1.
    --> Detail A.
   
2. Finding 2.
    --> Detail B.
```
? Paragraph structure preserved!

---

## Solution

### Smart Detection Logic

```csharp
// Check if input has multiple paragraphs (separated by blank lines)
var hasMultipleParagraphs = input.Contains("\n\n");

// Line mode ONLY applies when there's a single paragraph
var effectiveLineMode = cfg.NumberConclusionLinesOnOneParagraph && !hasMultipleParagraphs;

if (effectiveLineMode)
{
    // LINE MODE: Number each line as separate item
    // (only when no blank line separators exist)
}
else
{
    // PARAGRAPH MODE: Number paragraphs, indent continuation lines
    // (always when blank lines detected, or setting is off)
}
```

### Decision Matrix

| Input Structure | Setting | Effective Mode | Behavior |
|-----------------|---------|----------------|----------|
| **Single paragraph** (no `\n\n`) | Line mode ON | **Line mode** | Number every line |
| **Single paragraph** (no `\n\n`) | Line mode OFF | Paragraph mode | Number first line, indent rest |
| **Multiple paragraphs** (has `\n\n`) | Line mode ON | **Paragraph mode** ?? | **Force** paragraph mode (override setting) |
| **Multiple paragraphs** (has `\n\n`) | Line mode OFF | Paragraph mode | Number first line of each paragraph |

---

## Examples

### Example 1: Single Paragraph

**Input**:
```
Finding 1
--> Detail A
--> Detail B
```

**Line Mode ON**:
```
1. Finding 1.
2. --> Detail A.
3. --> Detail B.
```
? Setting honored (no blank lines)

**Line Mode OFF**:
```
1. Finding 1.
    --> Detail A.
    --> Detail B.
```
? Paragraph mode (default)

---

### Example 2: Multiple Paragraphs

**Input**:
```
Finding 1
--> Detail A

Finding 2
--> Detail B

Finding 3
```

**Line Mode ON** (?? overridden):
```
1. Finding 1.
    --> Detail A.
   
2. Finding 2.
    --> Detail B.
   
3. Finding 3.
```
? **Forced paragraph mode** (blank lines detected)

**Line Mode OFF**:
```
1. Finding 1.
    --> Detail A.
   
2. Finding 2.
    --> Detail B.
   
3. Finding 3.
```
? Paragraph mode (default)

---

## Debug Logging

When reportifying, the debug output shows the detection logic:

```
[Reportify] Conclusion numbering mode: LineMode=True, HasMultipleParagraphs=True, EffectiveMode=PARAGRAPH
```

This shows:
- **LineMode=True**: Setting is ON (user enabled line mode)
- **HasMultipleParagraphs=True**: Blank lines detected in input
- **EffectiveMode=PARAGRAPH**: **Overridden to paragraph mode** ?

---

## Benefits

### User Experience
- ? **Prevents unexpected behavior** when text has paragraph structure
- ? **Preserves paragraph structure** automatically
- ? **No manual mode switching required** (smart detection)
- ? **Works intuitively** for both single and multi-paragraph inputs

### Technical
- ? **Simple detection** (`input.Contains("\n\n")`)
- ? **No breaking changes** (backward compatible)
- ? **Clear debug logging** (shows detection logic)

---

## Code Location

**File**: `apps/Wysg.Musm.Radium/ViewModels/MainViewModel.ReportifyHelpers.cs`

**Method**: `ApplyReportifyBlock(string input, bool isConclusion)`

**Lines**: Smart detection in paragraph numbering section (STEP 2)

---

## Testing

### Test Case 1: Single Paragraph, Line Mode ON
- Input: `"line1\nline2\nline3"` (no `\n\n`)
- Expected: Line mode applied (every line numbered)
- Result: ? Pass

### Test Case 2: Multiple Paragraphs, Line Mode ON
- Input: `"line1\nline2\n\nline3\nline4"` (has `\n\n`)
- Expected: Paragraph mode applied (override setting)
- Result: ? Pass

### Test Case 3: Multiple Paragraphs, Line Mode OFF
- Input: `"line1\nline2\n\nline3\nline4"` (has `\n\n`)
- Expected: Paragraph mode applied (default)
- Result: ? Pass

---

## Build Status

```
Command: run_build
Result: ºôµå ¼º°ø (Build Success)
Errors: 0
```

---

**Status**: ? Implemented and Verified  
**Build**: ? Success  
**Deployed**: Ready for production