# Change: Remove Central Connection String from Network Tab

**Date:** 2025-12-26  
**Status:** Completed  
**Category:** UI Cleanup

## Context
The central (Azure SQL) connection string is no longer required because all transactions now flow through the API. Keeping the input encouraged unused configuration and confused operators.

## Changes
- Removed the central connection string label and textbox from the Network settings tab.
- Dropped the central connection string property/defaults, Save handling, and Test Central command from `SettingsViewModel`.

## Files Changed
- `apps/Wysg.Musm.Radium/Views/SettingsTabs/NetworkSettingsTab.xaml`
- `apps/Wysg.Musm.Radium/ViewModels/SettingsViewModel.cs`

## Testing
1. Open Settings ¡æ Network and confirm only Local/Intranet and Snowstorm fields remain.
2. Verify the Network tab shows Test Local and Save buttons only.
3. Use Test Local to confirm connectivity and ensure Save completes without error.
