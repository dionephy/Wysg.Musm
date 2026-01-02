# Change: Default Integrations Values Updated

**Date:** 2025-12-26  
**Status:** Completed  
**Category:** Configuration

## Context
Default integration settings should point to local services without exposing the password. The previous defaults included a password in the local connection string and used a remote Snowstorm endpoint.

## Changes
- Default Local Connection String is now `Host=127.0.0.1;Port=5432;Database=wysg_dev;Username=postgres` (password appended internally when used).
- Default Snowstorm Base URL is now `http://127.0.0.1:8080/`.

## Files Changed
- `apps/Wysg.Musm.Radium/ViewModels/SettingsViewModel.cs`
- `apps/Wysg.Musm.Radium/Views/SettingsWindow.xaml` (tab header changed to "Integrations")
- `apps/Wysg.Musm.Radium/Views/SettingsTabs/NetworkSettingsTab.xaml` (GroupBox header changed to "Essential")

## Testing
1. Launch Settings ¡æ Integrations ¡æ Essential on a fresh profile with no saved settings.
2. Confirm Local Connection String shows the new default without password.
3. Confirm Snowstorm Base URL shows `http://127.0.0.1:8080/`.
4. Use Test Local to verify connectivity (password is appended internally).
