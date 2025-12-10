# FEATURE: Current Study Report Globals (2025-12-10)

**Status**: ? Implemented

## Summary
Automation authors can now read/write four new global properties that reflect the current study's report metadata:

1. `Current Study Report Datetime`
2. `Current Study Report Reporter`
3. `Current Study Report Header and Findings`
4. `Current Study Report Conclusion`

They map directly to the existing `MainViewModel` fields (`CurrentReportDateTime`, `ReportRadiologist`, `ReportedHeaderAndFindings`, `ReportedFinalConclusion`) so procedure output and custom modules stay in sync with the UI.

## Details
- All four entries appear in both the **Custom Module property picker** (Set modules) and the **procedure variable dropdown** (Var type arguments).
- Values are automatically serialized into `CurrentReportJson`, meaning downstream modules and exports see the same data.
- The `ClearCurrentFields` procedure now clears the report datetime alongside the previously cleared reporter/header/conclusion fields, ensuring these globals reset whenever a new study workflow begins.

## Files Updated
- `Models/CustomModule.cs` ? Declared the new property names and exposed them through `AllProperties` / `AllReadableProperties`.
- `ViewModels/MainViewModel.Commands.Automation.Custom.cs` ? Hooked the property getters/setters to the underlying view-model fields.
- `Services/Procedures/ClearCurrentFieldsProcedure.cs` ? Clears `CurrentReportDateTime` so the datetime global never leaks between studies.

Build status: ? `dotnet build`
