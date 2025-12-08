# Feature: Header Format Template in Report Format Tab

Date: 2025-12-08
Type: Feature
Status: Complete
Project: Wysg.Musm.Radium

Summary:
- Added a textbox in Settings ¡æ Report Format tab to configure a header format template.
- Supported placeholders: {Chief Complaint}, {Patient History Lines}, {Techniques}, {Comparison}.
- Persisted the template in local settings and applied immediately on Save.
- When Settings window is closed (Saved), existing header content refreshes to match the new format.

Implementation:
- UI: apps/Wysg.Musm.Radium/Views/SettingsTabs/ReportifySettingsTab.xaml
  - Inserted header template textbox above the report formatting pane.
- ViewModel: apps/Wysg.Musm.Radium/ViewModels/SettingsViewModel.HeaderFormat.cs
  - Added `HeaderFormatTemplate` property.
- Local Settings: apps/Wysg.Musm.Radium/Services/IRadiumLocalSettings.cs + RadiumLocalSettings.cs
  - Added `HeaderFormatTemplate` persistence key.
- Main VM: apps/Wysg.Musm.Radium/ViewModels/MainViewModel.Editor.cs
  - Implemented `OnHeaderFormatTemplateChanged()` and `BuildHeaderFromTemplate()`.
  - Refreshes header when template changes and header has content.

Build:
- Success. No errors.

Notes:
- Default template used if none is set:
  Clinical information: {Chief Complaint}
  - {Patient History Lines}
  Techniques: {Techniques}
  Comparison: {Comparison}
- Comparison shows N/A if other header content exists but comparison is empty.
