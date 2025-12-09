# Enhancement: If Modality with Header Built-in Module (2025-12-09)

**Status**: ? Complete  
**Area**: Automation Window ¡æ Procedures  
**Request**: "I want a built-in module that only runs the next steps when the current modality is one that still sends headers."

---

## Summary
- Added a new built-in control module `If Modality with Header` that branches automation flows based on the current study modality.
- The module evaluates the `ModalitiesNoHeaderUpdate` setting (same list used by Send Report and header updates).
- When the modality **is not** in the exclusion list the next modules run; otherwise execution skips to the matching `End if`.
- Validation, UI lists, and helper utilities were updated so the module behaves like existing `If` blocks.

---

## Implementation Details
| File | Change |
|------|--------|
| `ViewModels/MainViewModel.Commands.Automation.Report.cs` | Added shared helpers to parse the modality exclusion list and reuse it for both Send Report logic and the new module. |
| `ViewModels/MainViewModel.Commands.Automation.Core.cs` | Introduced runtime handling for `If Modality with Header`, pushing an automation if-block that mirrors custom `If` modules. |
| `Views/AutomationWindow.Automation.cs` | Validation now recognizes the new built-in module and requires it to close with `End if`. |
| `ViewModels/SettingsViewModel.cs` | Added the module to the Available Modules list so it appears in the Automation library. |
| `docs/12-guides/user/QUICKREF_2025-11-10_ModalitiesNoHeaderUpdate.md` | Documented how the new module ties to the modalities without header setting. |

### Execution Flow
1. The module calls a shared helper that extracts the current study modality (LOINC-aware) and parses the exclusion list.
2. If the modality is excluded, the automation engine marks the block as not met and skips to `End if`.
3. Otherwise it behaves like a normal `If` block and the contained modules run.
4. Because the helper is shared, Send Report continues to choose between `SendReport` and `SendReportWithoutHeader` using the same logic.

---

## Usage Tips
- Place `If Modality with Header` before steps that set headers, comparison text, or any other header-only automation.
- Combine with labels/`Goto` just like other `If` blocks.
- Optional else-branches are not required?simply add `End if` after the block.

---

## Testing Checklist
- [x] Automation pane validation errors when `End if` is missing.
- [x] Block executes when modality **not** in exclusion list.
- [x] Block is skipped when modality **is** in exclusion list.
- [x] Send Report continues to respect the same setting via shared helpers.
- [x] Build succeeds.

---

**Ready for use**: ?  Users can now author modality-aware automation flows without custom procedures.