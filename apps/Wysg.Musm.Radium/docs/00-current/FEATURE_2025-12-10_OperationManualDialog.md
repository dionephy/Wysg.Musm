# Feature: Procedure Operation Manual Dialog (2025-12-10)

**Status**: ? Complete  
**Owner**: Automation UI  
**Scope**: AutomationWindow Procedure tab, documentation tooling

---

## Summary
Procedure authors can now open a searchable manual that documents every available operation, its purpose, and the meaning of each argument. The dialog is launched directly from the Procedure tab so users no longer have to cross-reference external notes while editing automation steps.

---

## Key Changes
- **Operation manual catalog**
  - Added `OperationManualCatalog` with structured metadata (name, summary, output notes, argument descriptions).
  - Captures all operations currently exposed in `AutomationWindow.OperationItems.xaml` and mirrors logic from `OperationExecutor`/`OnProcOpChanged`.
- **OperationManualWindow**
  - New dialog with Consalas-styled list view, highlighting each operation, its summary, output behavior, and argument table.
  - Includes instant text filtering over names, summaries, categories, and argument details.
- **AutomationWindow integration**
  - Procedure tab now exposes an `Operation manual` button next to Run/Duplicate controls.
  - Button opens the dialog as a modal window and reports failures via MessageBox when initialization encounters issues.

---

## Usage
1. Open the Automation window and switch to the **Procedure** tab.
2. Click **Operation manual** to launch the dialog.
3. Use the search box to filter by operation name (e.g., `SetValue`), category (e.g., `System`), or argument keywords (e.g., `Element`).
4. Reference the argument grid to understand which Arg slots accept Elements, Strings, Numbers, or Vars before configuring the procedure grid row.

---

## Validation Checklist
- [x] Manual button appears only inside the Procedure tab toolbar.
- [x] Dialog opens on top of the Automation window and inherits the dark theme.
- [x] Each operation from `OperationItems` renders exactly once with accurate summaries/arguments.
- [x] Search filter matches by name, summary text, category, output notes, and argument descriptions.
- [x] Closing the dialog returns focus to the Automation window without disrupting edits.

---

## Follow-up Ideas
1. Auto-link operations to deep documentation pages (docs/04-archive) for long-form guidance.
2. Add keyboard shortcut (e.g., F1) to open the manual when the procedure grid has focus.
3. Persist the last used search term per session for quicker re-entry.

---

**Build**: `dotnet build` ?  
**Docs**: This file + `AutomationWindow` inline documentation  
**Tests**: Manual UI validation of the new dialog and button
