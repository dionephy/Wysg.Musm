# Change: Settings Integrations API test button

Date: 2026-01-31
Type: Change
Status: Complete
Project: Wysg.Musm.Radium

Summary:
- Added a **Test API** button in Settings → Integrations.
- Runs an API health check and a simple database-backed settings call for the current account.
- Falls back to an anonymous `app.account` count call when no account is logged in.
- Shows clear status messages for connectivity and authorization issues.

Implementation:
- UI: `apps/Wysg.Musm.Radium/Views/SettingsTabs/NetworkSettingsTab.xaml`
- ViewModel: `apps/Wysg.Musm.Radium/ViewModels/SettingsViewModel.cs`

Build:
- Success.

Notes:
- The DB call requires a logged-in account and valid API token.
