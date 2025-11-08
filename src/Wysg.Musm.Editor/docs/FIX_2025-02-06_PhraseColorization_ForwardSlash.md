# FIX: Phrase Colorization for Terms with Forward Slash (N/A)

**Date**: 2025-02-06  
**Issue**: Phrases containing forward slash characters (e.g., "N/A") are not being colorized in the editor despite existing in the global phrases database

## Problem

The phrase colorizer and highlighter were skipping forward slash (`/`) characters as standalone punctuation during tokenization. This caused phrases like "N/A" to be broken up:
- "N" (matched as a letter)
- "/" (skipped as punctuation)
- "A" (matched as a letter)

As a result, "N/A" was never matched as a complete phrase even though it exists in the database.

## Root Cause

The tokenization logic in both `PhraseColorizer.cs` and `PhraseHighlightRenderer.cs` only treated hyphens (`-`) as valid characters within words/phrases, but not forward slashes (`/`).

### Before:
```csharp
// Skip standalone punctuation
if (char.IsPunctuation(text[i])) { i++; continue; }

// Find word boundaries (include hyphens as part of words for phrases like "COVID-19")
while (i < text.Length && !char.IsWhiteSpace(text, i) && (char.IsLetterOrDigit(text, i) || text[i] == '-'))
    i++;
```

This worked for:
- ? "follow-up" (hyphen)
- ? "COVID-19" (hyphen)
- ? "N/A" (forward slash)
- ? "w/o" (forward slash - "without")
- ? "h/o" (forward slash - "history of")

## Solution

Updated both `PhraseColorizer.cs` and `PhraseHighlightRenderer.cs` to treat forward slash (`/`) as a valid character within medical terms, just like hyphens.

### Changes:

1. **Updated punctuation skip condition**:
```csharp
// Skip standalone punctuation (except hyphen and forward slash which are part of medical terms)
if (char.IsPunctuation(text[i]) && text[i] != '-' && text[i] != '/') { i++; continue; }
```

2. **Updated word boundary detection**:
```csharp
// Find word boundaries (include hyphens and forward slashes as part of words for phrases like "COVID-19" and "N/A")
while (i < text.Length && !char.IsWhiteSpace(text, i) && (char.IsLetterOrDigit(text, i) || text[i] == '-' || text[i] == '/'))
    i++;
```

3. **Updated multi-word lookahead**:
```csharp
// Skip punctuation before next word (except hyphen and forward slash)
if (char.IsPunctuation(text[scanPos]) && text[scanPos] != '-' && text[scanPos] != '/') break;

// Find next word (include hyphens and forward slashes)
while (scanPos < text.Length && !char.IsWhiteSpace(text, scanPos) && (char.IsLetterOrDigit(text, scanPos) || text[scanPos] == '-' || text[scanPos] == '/'))
    scanPos++;
```

## Files Modified

1. **src/Wysg.Musm.Editor/Ui/PhraseColorizer.cs**
   - Updated `FindMatchesInLine()` method to include `/` as valid character
   - Applied to 3 locations: punctuation skip, word boundary, and lookahead

2. **src/Wysg.Musm.Editor/Ui/PhraseHighlightRenderer.cs**
   - Updated `FindPhraseMatches()` method to include `/` as valid character
   - Applied to 3 locations: punctuation skip, word boundary, and lookahead

## Testing Checklist

- [x] Build succeeds without errors
- [ ] "N/A" is now colorized when it appears in headers
  - [ ] "Clinical information: N/A" shows colorized "N/A"
  - [ ] "Comparison: N/A" shows colorized "N/A"
- [ ] Other slash-containing medical terms work:
  - [ ] "w/o" (without)
  - [ ] "h/o" (history of)
  - [ ] "s/p" (status post)
- [ ] Hyphenated terms still work:
  - [ ] "follow-up"
  - [ ] "COVID-19"
  - [ ] "non-specific"
- [ ] Multi-word phrases with slashes work:
  - [ ] "with/without contrast"
  - [ ] "and/or"
- [ ] SNOMED semantic tag colors apply correctly to slash-containing phrases

## Impact

This fix enables colorization for common medical abbreviations that use forward slashes:
- **N/A** - Not applicable
- **w/o** - Without
- **h/o** - History of
- **s/p** - Status post
- **w/** - With
- **c/w** - Consistent with
- **r/o** - Rule out

These are commonly used in radiology reports and should be treated as complete phrases for proper syntax highlighting and SNOMED tagging.

## Notes

- The fix treats both `-` and `/` as valid non-separating punctuation within medical terms
- Other punctuation (`.`, `,`, `;`, etc.) still acts as phrase boundaries, which is correct
- This matches the behavior of medical terminology systems where abbreviations with slashes are single concepts
- The phrase must still exist in the global phrases database to be colorized

## Related Issues

- Original issue: "N/A" not colorized in headers
- Previous similar issue: "follow-up" hyphenation (already fixed)
- Applies to all medical abbreviations using forward slashes
