# UI Change: Rename "Reportify" Tab to "Report Format"

Date: 2025-12-08
Type: UI Change
Status: Complete
Project: Wysg.Musm.Radium

Summary:
- Updated Settings window tab header from "Reportify" to "Report Format".
- Updated header text within the tab to show "Report Format (Preview Skeleton)".
- No functional logic changes; existing bindings and commands remain.

Files Modified:
- apps/Wysg.Musm.Radium/Views/SettingsWindow.xaml
- apps/Wysg.Musm.Radium/Views/SettingsTabs/ReportifySettingsTab.xaml

Build Status:
- Success (no compilation errors).

Notes:
- Tab `x:Name` remains `tabReportify` for stability; only user-facing text changed.
- Control class remains `ReportifySettingsTab`.
