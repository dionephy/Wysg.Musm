# Fix: RemoveExcessiveBlankLines Ignores Whitespace-Only Lines (2025-12-11)

**Date**: 2025-12-11  \
**Type**: Bug Fix  \
**Component**: Reportify Formatter

---

## Summary

`remove_excessive_blank_lines` failed when blank lines contained stray spaces or tabs. Users would see double-spacing even though the option was enabled. We now normalize whitespace-only blank lines both before and after the line-level transformations, ensuring there is at most a single blank line between paragraphs.

---

## Problem

**Input**
```
Limited evaluation due to metal artifact 
 
 
 

 suspicious mild enlargement of left submandibular gland with surrounding mild infiltration
```

**Previous Output**
```
Limited evaluation due to metal artifact.


Suspicious mild enlargement of left submandibular gland with surrounding mild infiltration.
```

Even though the blank lines only contained spaces, the formatter kept two empty lines in a row. The regex we used only matched raw `\n` sequences, so any whitespace between them prevented collapsing.

---

## Fix

1. **Regex Update** ? `RxBlankLines` now matches newline sequences that may include spaces/tabs between them, so we collapse runs like `\n   \n   \n` down to a single blank line.
2. **Post-Processing Pass** ? After the line-level transformation loop we run a lightweight filter that removes consecutive empty strings. The filter only emits a blank line when the previous emitted line was non-blank, preventing double-spacing even if the earlier regex missed something.

---

## Files Changed

| File | Description |
|------|-------------|
| `apps/Wysg.Musm.Radium/ViewModels/MainViewModel.ReportifyHelpers.cs` | Updated blank-line regex and added a pass to collapse whitespace-only blank lines after processing. |

---

## Result

**New Output**
```
Limited evaluation due to metal artifact.

Suspicious mild enlargement of left submandibular gland with surrounding mild infiltration.
```

Only a single blank line remains, matching the expected Reportify behavior.

---

## Testing

Manually verified combinations of:
- Blank lines containing only spaces or tabs
- Mixed blank lines (some empty, some whitespace)
- Leading/trailing blank lines

In every case, the formatter now keeps at most one blank line between paragraphs.

---

## Status

- ? Fix implemented
- ? Build succeeds (see agent run log)
- ? Backward compatible
