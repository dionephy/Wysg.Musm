# Change: API Base URL Now Sourced from Settings

**Date:** 2025-12-26  
**Status:** Completed  
**Category:** Configuration

## Context
The WPF client continued working even when the API Base URL in Settings ¡æ Network was incorrect because the app fell back to environment variables or `appsettings`. The API endpoint must always come from the user-configured setting (with only the UI default when unset).

## Changes
- All API client registrations and `RadiumApiClient` now resolve the base URL exclusively from `IRadiumLocalSettings.ApiBaseUrl` (UI Network tab), with no environment or `appsettings` fallback.
- Default used only when the setting is empty remains `http://127.0.0.1:5205/` to match the Network tab default.

## Files Changed
- `apps/Wysg.Musm.Radium/App.xaml.cs`

## Testing
1. Set API Base URL in Settings ¡æ Network to an invalid port; restart app ¢¡ API calls fail as expected.
2. Set API Base URL to `http://127.0.0.1:5205/`; restart app ¢¡ API clients use the configured URL.
3. `dotnet build` ¢¡ success.
