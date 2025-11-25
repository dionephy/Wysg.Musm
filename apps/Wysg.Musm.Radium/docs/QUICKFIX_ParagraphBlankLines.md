# Quick Fix: Paragraph Blank Lines Preserved

**Date**: 2025-10-23  
**Status**: ? Fixed

---

## Issue

Paragraph mode wasn't preserving blank lines between paragraphs.

**Before**:
```
1. No acute intracranial hemorrhage.
   No acute skull fracture.
2. Diffuse brain atrophy.
```
? No blank line between paragraphs

**After**:
```
1. No acute intracranial hemorrhage.
   No acute skull fracture.

2. Diffuse brain atrophy.
```
? Blank line preserved

---

## Fix

**Changed processing order**:

1. ~~Paragraph numbering �� Blank line normalization~~ ?
2. **Blank line normalization �� Paragraph numbering** ?

Now blank lines are normalized first (3+ �� 2), then paragraphs are numbered and joined with `\n\n`.

---

## Modes

### Paragraph Mode (default)
- Numbers paragraphs
- Indents continuation lines
- **Preserves blank lines** ?

### Line Mode (`number_conclusion_lines_on_one_paragraph: true`)
- Numbers each line
- No indentation
- **Removes all blank lines** ?

---

**File**: `apps/Wysg.Musm.Radium/ViewModels/MainViewModel.ReportifyHelpers.cs`  
**Build**: ? Success
