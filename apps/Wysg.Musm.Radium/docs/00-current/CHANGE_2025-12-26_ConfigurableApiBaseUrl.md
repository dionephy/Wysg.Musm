# Change: Configurable API Base URL

**Date:** 2025-12-26  
**Status:** Completed  
**Category:** Configuration

## Context
The API base URL was hardcoded (e.g., `http://localhost:5205/`). Users need to set and persist the API endpoint from the Integrations tab (formerly "Network") with a sensible local default.

## Changes
- Added `ApiBaseUrl` field to Settings ¡æ Integrations ¡æ Essential group with binding to settings storage.
- Default API base URL is now `http://127.0.0.1:5205/` for new profiles.
- Persisted via local settings (`RadiumLocalSettings`) alongside other integration values.

## Files Changed
- `apps/Wysg.Musm.Radium/Views/SettingsWindow.xaml` (tab header changed to "Integrations")
- `apps/Wysg.Musm.Radium/Views/SettingsTabs/NetworkSettingsTab.xaml` (GroupBox header changed to "Essential")
- `apps/Wysg.Musm.Radium/ViewModels/SettingsViewModel.cs`
- `apps/Wysg.Musm.Radium/Services/IRadiumLocalSettings.cs`
- `apps/Wysg.Musm.Radium/Services/RadiumLocalSettings.cs`

## Testing
1. Open Settings ¡æ Integrations ¡æ Essential: confirm API Base URL shows `http://127.0.0.1:5205/` on a fresh profile.
2. Change the API URL and click Save; reopen Settings to verify persistence.
3. Ensure existing Local/Snowstorm fields remain functional.
