# Change: Hide Local Connection Password in Network Tab

**Date:** 2025-12-26  
**Status:** Completed  
**Category:** UI Cleanup

## Context
The Network tab displayed the full local/intranet PostgreSQL connection string including the password. The password should be hidden from the UI while keeping connections functional with a fixed credential.

## Changes
- Strip the password from the Local Connection String textbox while editing.
- On save and test, automatically append the hardcoded password `` `123qweas`` to the connection string before use.
- Default and persisted values now omit the password in the UI but include it when used to connect.

## Files Changed
- `apps/Wysg.Musm.Radium/ViewModels/SettingsViewModel.cs`

## Testing
1. Open Settings ¡æ Network: confirm Local Connection String shows no password segment.
2. Click **Test Local**: succeeds using the hardcoded password.
3. Click **Save**: persists the connection string with the password appended; reopening settings still hides the password.
