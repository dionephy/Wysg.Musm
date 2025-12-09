# Feature: Label + Goto Custom Modules (2025-12-08)

**Status**: ? Complete  
**Owner**: Automation  
**Scope**: Settings ? Automation, Custom Modules, MainViewModel automation runtime

---

## Summary
Users can now declare explicit labels inside automation sequences and jump between them using new `Label` and `Goto` custom modules. Labels are persisted alongside custom modules and appear in the Automation library, enabling structured flows (e.g., skipping sections, retry loops) without duplicating large sequences.

---

## Key Changes
- **Create Module dialog**
  - Added `Label` module type with a simple text box (auto-saves as `LabelName:`).
  - Added `Goto` module type with a dropdown of existing labels.
  - Auto-generated names show the exact label or goto target for clarity.
- **Custom module store + UI**
  - Labels are persisted in a dedicated list inside `custom-modules.json`.
  - AutomationWindow lists label entries (with trailing colon) next to other custom modules.
  - Delete flow now handles both modules and labels.
- **Runtime execution**
  - Automation engine scans sequences for labels once per run.
  - Encountering a label is a no-op marker.
  - `Goto` modules jump to the first occurrence of the target label and continue with the next module.
  - Safety guard aborts execution if excessive goto hops suggest an infinite loop.
  - Goto execution bypasses `if` skip suppression so it can be used anywhere in the sequence.

---

## Usage Examples
```
Label:   ?  "RetrySend:" (shown in library as `RetrySend:`)
Goto:    ?  "Goto RetrySend:" (jumps to label)
```

1. Create a `Label` named `RetrySend`. Drag `RetrySend:` into Send Report sequence.
2. Append `Goto RetrySend:` after a status check to loop back when needed.
3. Combine with `If/End if` blocks to branch to specific sections.

---

## Validation Checklist
- [x] Create Label ? appears under Custom Modules panel and persists after restart.
- [x] Create Goto ? dropdown only shows saved labels and requires a selection.
- [x] Drag label/goto into automation panes ? saved to automation files.
- [x] Execute automation with labels ? no status errors; labels logged as no-ops.
- [x] Execute automation with goto ? jumps to target label and resumes with next item.
- [x] Delete label ? removed from list and from goto dropdown on next dialog open.
- [x] Loop protection ? repeated goto hopping aborts with clear status message.

---

## Follow-up Ideas
1. **Label preview** inside Automation panes (e.g., faint divider) for easier visual grouping.
2. **Goto validation** during SaveAutomation to warn if a referenced label is missing.
3. **Rename label** workflow so users can update label text without re-adding items manually.
4. **Scoped labels** (optional) to limit jumps to current pane.

---

**Build**: `dotnet build` ??  
**Docs**: This file  
**Tests**: Manual automation run with label/goto modules
