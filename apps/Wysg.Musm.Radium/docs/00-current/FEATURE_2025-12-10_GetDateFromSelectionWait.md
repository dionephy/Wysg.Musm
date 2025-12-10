# FEATURE: GetDateFromSelectionWait Operation (2025-12-10)

**Status**: ? Implemented

## Summary
- Added a new custom procedure operation `GetDateFromSelectionWait`.
- Mirrors `GetValueFromSelection` but keeps polling the selected row until the chosen column parses as a valid `DateTime` (after trimming).
- Uses the same 5-second / 200ms interval wait profile as `GetTextWait` to stay consistent with other wait-style operations.

## Usage
| Argument | Type            | Description |
|----------|-----------------|-------------|
| Arg1     | `Element`       | Bookmark or cached list/grid element that exposes the desired selection. |
| Arg2     | `String` (opt.) | Header/column name to inspect. Defaults to `ID` when omitted. |

**Output**: The trimmed column text once it can be parsed as a valid date; `null` if the timeout expires.

## Motivation
Automation flows that need the *reported* study timestamp previously relied on ad-hoc loops inside view-model code. Exposing the wait logic as a reusable operation lets procedures replicate the same behavior declaratively and aligns with the approach taken by `GetTextWait`.

## Files Updated
- `Services/OperationExecutor.ElementOps.cs` ? core implementation with retry loop.
- `Services/OperationExecutor.cs` ? new operation routing entry.
- `Views/AutomationWindow.OperationItems.xaml` ? surfaced in the operation picker.
- `Views/AutomationWindow.Procedures.Exec.cs` ? argument presets identical to `GetValueFromSelection`.
- `ViewModels/OperationManualCatalog.cs` ? manual entry with argument/behavior summary.

Build status: ? `dotnet build` passes.
