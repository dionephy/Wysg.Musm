# Change Log - Remove TakeLast/ToDateTime Operations

**Date**: 2025-12-10  
**Owner**: Automation Platform  

## Summary
Per automation team request, the legacy `TakeLast` and `ToDateTime` procedures were removed from Radium. These steps provided redundant behavior (string slicing via `Split`/`Merge` and datetime formatting already handled inside `GetCurrentStudyDateTime`). Removing them simplifies the operation surface and prevents users from leaning on partially tested code paths.

## Implementation
- Deleted the private helpers from `OperationExecutor.StringOps`.
- Removed the dispatcher cases in `OperationExecutor.ExecuteOperation` so the names are no longer routable.
- Updated `AutomationWindow` UI: dropdown options, arg-binding logic, and manual catalog entries now omit the retired items.
- Cleaned affected docs so no quick reference points to the removed names.

## Impact
- Existing procedures referencing `TakeLast` / `ToDateTime` will now return `(unsupported)` during execution. Users should migrate to:
  - Use `Split` followed by `Var` indexing to obtain the desired slice.
  - Rely on `GetCurrentStudyDateTime` (already returns formatted datetime) or handle conversions inside custom modules/snippets.
- Operation manual entry count decreased by two; UI combo menus shortened accordingly.

## Validation
- Manual spot check of AutomationWindow confirmed the operations disappear from the combo box.
- OperationManualCatalog rebuilt successfully (no missing references).
- Pending build verification will ensure no dangling references remain.

## Next Steps
- Communicate removal to automation authors via the upcoming weekly doc digest.
- Encourage migration to the supported patterns listed above.
