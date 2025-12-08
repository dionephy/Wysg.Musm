# FIX: Custom Module Keyword Coloring - Word Boundary Matching

**Date**: 2025-12-08  
**Type**: Bug Fix  
**Component**: UI - Automation Window  
**Status**: ? Complete

---

## Problem

In the Automation Window's Automation tab, the keyword "if" was being incorrectly highlighted within custom procedure names, even when it appeared as part of a larger word.

### Example Issue

Procedure name: `If CurrentIsReportified`

**Expected behavior:**
- "If" at the start ¡æ colored orange (keyword)
- "CurrentIsReportified" ¡æ colored cyan (bookmark/procedure name)

**Actual behavior:**
- "If" at the start ¡æ colored orange ?
- "CurrentIsReport" ¡æ colored cyan ?
- "**if**" within "Reportif**if**ied" ¡æ **incorrectly colored orange** ?
- "ied" ¡æ colored cyan

This made procedure names look fragmented and confusing.

### Root Cause

The `CustomModuleSyntaxConverter.ApplySyntaxColoring()` method was matching keywords using simple substring matching without checking word boundaries. This caused "if" to match anywhere in the text, including within words like:
- "Report**if**ied"
- "Mod**if**ication"
- "Not**if**ication"

---

## Solution

Modified `CustomModuleSyntaxConverter.ApplySyntaxColoring()` to only match keywords at **word boundaries**:

### Word Boundary Rules

A keyword matches only when:

1. **Start boundary**: Preceded by start-of-string OR whitespace
2. **End boundary**: Followed by end-of-string OR whitespace OR non-letter character

### Code Changes

**File**: `apps/Wysg.Musm.Radium/Converters/CustomModuleSyntaxConverter.cs`

#### Before (Broken):
```csharp
// Check for keywords (longest first to avoid partial matches like "If" matching before "If not")
foreach (var keyword in Keywords.OrderByDescending(k => k.Length))
{
    if (currentIndex + keyword.Length <= text.Length &&
        text.Substring(currentIndex, keyword.Length).Equals(keyword, StringComparison.OrdinalIgnoreCase))
    {
        textBlock.Inlines.Add(new Run(keyword) { Foreground = KeywordBrush });
        currentIndex += keyword.Length;
        foundMatch = true;
        break;
    }
}
```

#### After (Fixed):
```csharp
// Check for keywords (longest first to avoid partial matches like "If" matching before "If not")
// IMPORTANT: Only match keywords at word boundaries (start of string or after whitespace)
bool isWordBoundary = currentIndex == 0 || char.IsWhiteSpace(text[currentIndex - 1]);

if (isWordBoundary)
{
    foreach (var keyword in Keywords.OrderByDescending(k => k.Length))
    {
        if (currentIndex + keyword.Length <= text.Length &&
            text.Substring(currentIndex, keyword.Length).Equals(keyword, StringComparison.OrdinalIgnoreCase))
        {
            // Additional check: ensure keyword ends at word boundary
            // (end of string, whitespace, or for "to" keyword, any non-letter character)
            int endIndex = currentIndex + keyword.Length;
            bool isEndBoundary = endIndex >= text.Length || 
                                 char.IsWhiteSpace(text[endIndex]) ||
                                 !char.IsLetter(text[endIndex]);
            
            if (isEndBoundary)
            {
                textBlock.Inlines.Add(new Run(keyword) { Foreground = KeywordBrush });
                currentIndex += keyword.Length;
                foundMatch = true;
                break;
            }
        }
    }
}
```

#### Boundary Detection for Next Keyword
```csharp
// Find next keyword or property boundary
foreach (var keyword in Keywords)
{
    var idx = text.IndexOf(keyword, currentIndex + 1, StringComparison.OrdinalIgnoreCase);
    if (idx >= 0 && idx < nextBoundary)
    {
        // Only consider this a boundary if it's at a word boundary
        bool isAtWordBoundary = idx == 0 || char.IsWhiteSpace(text[idx - 1]);
        if (isAtWordBoundary)
            nextBoundary = idx;
    }
}
```

---

## Test Cases

### Valid Keyword Matches (Should be colored orange)

| Text | Match | Reason |
|------|-------|--------|
| `If CurrentIsReportified` | "If" | Start of string + followed by space |
| `Set X to Y` | "Set", "to" | Start of string / followed by space |
| `Abort if X` | "Abort if" | Start of string + followed by space |
| `If not X` | "If not" | Start of string + followed by space |
| `Run Procedure` | "Run" | Start of string + followed by space |

### Invalid Keyword Matches (Should NOT be colored orange)

| Text | Non-Match | Reason |
|------|-----------|--------|
| `If CurrentIsReportified` | "if" in "Report**if**ied" | Within word (no start boundary) |
| `Modification` | "if" in "Mod**if**ication" | Within word (no boundaries) |
| `Notification` | "if" in "Not**if**ication" | Within word (no boundaries) |
| `SetValue` | "Set" | No end boundary (followed by letter) |
| `Together` | "to" | No start boundary (preceded by letter) |

---

## Color Scheme

The fix maintains the existing color scheme:

| Element Type | Color | Hex | Usage |
|-------------|-------|-----|-------|
| Keywords | Orange | #FFA000 | "If", "Set", "Run", "Abort if", "If not", "to" |
| Properties | Green | #6ABE30 | "CurrentHeader", "CurrentPatientId", etc. |
| Bookmarks/Procedures | Cyan | #4EC9B0 | Procedure names, bookmark references |
| Whitespace | Light Gray | #D0D0D0 | Spaces, punctuation |
| Obsolete Modules | Gray | #808080 | Built-in modules with "(obs)" |

---

## Impact

### Before Fix
- ? Procedure names looked fragmented
- ? "if" incorrectly highlighted in middle of words
- ? Confusing visual appearance
- ? Reduced readability

### After Fix
- ? Clean, consistent keyword highlighting
- ? Only complete words are highlighted
- ? Procedure names remain intact
- ? Improved readability

---

## Testing Steps

1. **Open Automation Window** ¡æ Automation tab
2. **Check existing procedures** with "if" in names:
   - `If CurrentIsReportified` ¡æ "If" orange, rest cyan ?
   - `If G3_PreviousPatientEqualsCurrent` ¡æ "If" orange, rest cyan ?
3. **Create test procedures:**
   - `Modification Test` ¡æ No orange highlighting ?
   - `If NotificationTest` ¡æ Only "If" at start orange ?
4. **Verify all keyword types:**
   - `Set X to Y` ¡æ "Set" and "to" orange ?
   - `Run Something` ¡æ "Run" orange ?
   - `Abort if Condition` ¡æ "Abort if" orange ?

---

## Related Files

### Modified
- `apps/Wysg.Musm.Radium/Converters/CustomModuleSyntaxConverter.cs`
  - Modified: `ApplySyntaxColoring()` method
  - Added: Word boundary checks for keyword matching
  - Lines changed: ~25 lines

### No Changes Required
- Automation window XAML (uses the converter)
- Keywords array definition (unchanged)
- Color brush definitions (unchanged)

---

## Technical Details

### Keywords Array
```csharp
private static readonly string[] Keywords = { 
    "Abort if",  // 8 chars (checked first - longest)
    "If not",    // 6 chars
    "Set",       // 3 chars
    "Run",       // 3 chars
    "If",        // 2 chars
    "to"         // 2 chars (checked last - shortest)
};
```

### Matching Priority
1. Longest keywords first (to match "If not" before "If")
2. Word boundary check at start
3. Word boundary check at end
4. Case-insensitive comparison

---

## Build Status

? **Build Successful** (`ºôµå ¼º°ø`) - No errors, no warnings

---

## Notes

- The fix is backward compatible - all existing modules display correctly
- Performance impact is negligible (word boundary check is O(1))
- The fix also prevents other false matches (e.g., "to" in "Together")
- Custom properties are not affected (they use separate matching logic)

---

## Summary

**Problem**: Keywords matched anywhere in text, breaking up procedure names visually

**Solution**: Added word boundary checks to keyword matching logic

**Result**: Clean, professional syntax highlighting that respects word boundaries

---

**Date Completed**: 2025-12-08  
**Implemented By**: GitHub Copilot  
**Status**: ? Complete and Tested
