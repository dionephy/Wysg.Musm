# Fix: Arrow Continuation Should Keep Single Numbered Item (2025-12-11)

**Date**: 2025-12-11  \
**Type**: Bug Fix  \
**Component**: Reportify (Conclusion Line Mode)

---

## Summary

When `number_conclusion_lines_on_one_paragraph`, `number_conclusion_paragraphs`, and `consider_arrow_bullet_continuation` were all enabled, Reportify still numbered every non-blank line. Lines that belonged to the same finding but were separated only by newlines were split into new numbered items, causing arrows or bullets to attach to the wrong line. This change keeps the entire block under a single number whenever an arrow or bullet marker exists inside the block.

---

## Problem Statement

**Input**
```
About 4 cm sized prominently enhancing mass involving right maxillary sinus, ostium, right middle nasal meatus.
No bony erosion.
--> R/o tumor, such as inverted papilloma.
```

**Settings**
- Number conclusion paragraphs: ?
- On one paragraph, number each line: ?
- Consider arrow & bullet continuation: ?

**Previous Output**
```
1. About 4 cm sized prominently enhancing mass involving right maxillary sinus, ostium, right middle nasal meatus.
2. No bony erosion.
   --> R/o tumor, such as inverted papilloma.
```

**Expected Output**
```
1. About 4 cm sized prominently enhancing mass involving right maxillary sinus, ostium, right middle nasal meatus.
   No bony erosion.
   --> R/o tumor, such as inverted papilloma.
```

Arrows correctly indented, but the line immediately before the arrow was still promoted to its own numbered conclusion, breaking the intended single block.

---

## Root Cause

Line-mode logic simply numbered every non-blank line unless it *started* with an arrow/bullet token. Even with `consider_arrow_bullet_continuation` enabled, only the arrow line itself became a continuation; preceding or intermediate lines stayed numbered individually. There was no concept of a "block" of lines that belong together.

---

## Solution

1. **Block Detection**
   - Split the paragraph into contiguous blocks of non-blank lines.
   - If a block contains any arrow/bullet lines (while continuation option is enabled), treat the entire block as a single numbered item.

2. **Single Number per Block**
   - First line in the block receives the next number (manual numbers stripped as before).
   - Remaining lines (including non-arrow text) are emitted as continuation lines with a 3-space indent, ensuring arrows stay attached to the correct base sentence.

3. **Fallback Preserved**
   - If the option is disabled or the block contains no arrows/bullets, line mode behaves exactly as before (every non-blank line gets its own number, with arrows still treated as continuations only when requested).

4. **Helpers**
   - Added small helpers to strip manual numbers and to detect arrow/bullet tokens consistently.

---

## Files Changed

| File | Description |
|------|-------------|
| `apps/Wysg.Musm.Radium/ViewModels/MainViewModel.ReportifyHelpers.cs` | Reworked line-mode numbering loop to group blocks with arrows/bullets and added helper methods. |

---

## Testing

Manual verification through unit-style inputs:

1. **Arrow Block** (input shown above) ¡æ now emits a single numbered item with both sentences + arrow indented.
2. **Bullet Block** (`- Recommendation` style) ¡æ identical behavior: bullets keep the block under one number.
3. **No Arrow/Bullet** ¡æ unchanged; every line still numbered individually when line mode is enabled.
4. **Multiple Blocks** separated by blank lines ¡æ each block evaluated independently; only blocks with markers collapse to single entries.

---

## Impact

- Restores the documented behavior where arrows/bullets keep related sentences under one numbered conclusion.
- Prevents recommendations from detaching from their parent finding.
- No changes for users who keep `consider_arrow_bullet_continuation` disabled.

---

## Build / Status

- ? Logic compiles (see solution build in final agent response).
- ? Backward compatible with existing Reportify settings.
