# Change: Remove Legacy GetCurrent* Operations (2025-12-10)

**Status**: ? Complete  
**Area**: Automation procedures / Operation Manual

---

## Summary
- Removed the five `GetCurrent*` procedure operations (PatientNumber, StudyDateTime, Header, Findings, Conclusion). They duplicated `CustomModuleProperties` and encouraged fragile UI-thread reads that have better alternatives today.
- OperationExecutor and AutomationWindow no longer expose these operations; existing procedures that reference them will now show "(unsupported)" until updated.
- Operation manual content and the Procedure Operations combo box were refreshed to keep the catalog accurate.

## Implementation
| File | Description |
|------|-------------|
| `Services/OperationExecutor.cs` | Dropped the switch cases that routed the removed operations so they can no longer execute. |
| `Services/OperationExecutor.MainViewModelOps.cs` | Deleted the UI-thread accessor implementations entirely. |
| `Views/AutomationWindow.OperationItems.xaml` | Removed the operation names from the Custom Procedure dropdown. |
| `Views/AutomationWindow.Procedures.Exec.cs` | Deleted the argument-config branch that previously treated the operations as no-arg steps. |
| `ViewModels/OperationManualCatalog.cs` | Removed the manual entries so the Operation Manual dialog only lists supported commands. |
| `docs/NEW_OPERATIONS_UNLOC_2025_01_20.md` | Added a 2025-12-10 note explaining the retirement and the recommended replacement path. |

## Rationale
- The underlying data is already exposed through `CustomModuleProperties` and can be read using `Echo`, removing the need for bespoke operations.
- Maintaining UI-thread accessors inside automation code caused repeated cross-thread fixes and a growing maintenance burden.
- Keeping unsupported operations visible in the UI confused procedure authors and led to dead-end configs.

## Testing
- Verified the Operation Manual dialog and the Custom Procedure operation dropdown no longer list the removed commands.
- Ran `dotnet build` for the Radium solution; build succeeded.
- Manually confirmed existing procedures that rely on CustomModuleProperties continue to function.

---
**Ready for Use**: ?
