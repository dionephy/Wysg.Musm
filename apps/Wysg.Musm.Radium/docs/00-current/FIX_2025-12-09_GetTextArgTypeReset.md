# FIX: GetText Arg1 Type Reset During Set/Run

**Date**: 2025-12-09  
**Type**: Bug Fix  
**Component**: Automation Window ¡¤ Procedure Tab  
**Status**: ? Complete

---

## Problem

Executing a `GetText` row via the **Set** button or running a custom procedure caused the row's **Arg1** to revert to `Element` and clear the user-selected bookmark/variable value. This regression blocked the runtime element chaining scenario introduced in [`docs/GET_SELECTED_ELEMENT.md`](../GET_SELECTED_ELEMENT.md), where `GetSelectedElement` caches an element into a procedure variable and later operations (like `GetText`) are expected to consume that cached reference by switching Arg1 to `Var`.

### Symptoms
- After clicking **Set** on a `GetText` row that targeted `var1`, Arg1 switched back to `Element` and the value became empty.
- Running any procedure containing `GetText(varX)` produced the same reset when the grid refreshed its rows.
- Downstream operations relying on the cached element (e.g., `IsVisible(var1)`) failed because the intermediate `GetText` no longer referenced the cached element.

---

## Root Cause

`AutomationWindow.Procedures.Exec.OnProcOpChanged` still used legacy logic that grouped `GetText` with other element-only operations and always executed:

```csharp
row.Arg1.Type = nameof(ArgKind.Element);
row.Arg1Enabled = true;
```

The handler runs not only when the user changes the operation dropdown but also whenever the grid rebinds (Set/Run). Therefore, even after the user intentionally flipped Arg1 to `Var`, the next refresh forced it back to `Element`, wiping the value.

---

## Solution

- Treat `GetText` separately inside `OnProcOpChanged` so that:
  - The handler only defaults Arg1 to `Element` when the row is still using the initial `String`/`Number` placeholder.
  - Existing `Element` or `Var` selections remain untouched during Set/Run refresh cycles.
- Continue to disable Arg2/Arg3 as before since `GetText` does not use them.

This preserves the user¡¯s choice (bookmark vs. cached element variable) while keeping the original defaulting behavior for freshly added rows.

### Files Modified
- `apps/Wysg.Musm.Radium/Views/AutomationWindow.Procedures.Exec.cs`
  - Split the `GetText` case from the element-only block and added guarded defaulting logic that respects prior selections.

---

## Testing
1. Add a `GetSelectedElement` row and capture it into `var1`.
2. Add a `GetText` row, set Arg1 Type to `Var`, Value to `var1`, then click **Set**.
   - **Expected:** Arg1 remains `Var`, value stays `var1`, output preview populated.
3. Run the full procedure.
   - **Expected:** After execution, the grid still shows Arg1 as `Var` with the cached element key.
4. Repeat with a `GetText` row left at the default (Element) to confirm defaults still work.

---

## Related Documents
- `docs/GET_SELECTED_ELEMENT.md` ? runtime element cache design and operation chaining requirements.
- `docs/00-current/ENHANCEMENT_2025-12-04_GetTextOnceOperation.md` ? context around different GetText variants.

---

## Status
- ? Fix implemented and tested on 2025-12-09.
- ? Ready for inclusion in the next automation tooling build.
