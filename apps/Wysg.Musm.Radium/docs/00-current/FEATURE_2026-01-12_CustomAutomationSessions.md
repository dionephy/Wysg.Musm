# FEATURE 2026-01-12 Custom automation sessions

## Summary
Added support for user-defined automation sessions in AutomationWindow. Users can append their own sessions (name + module list) after the existing Delete panel without affecting built-in sessions.

## Changes
- `AutomationWindow.xaml`: New "Custom Sessions" section after the Delete pane with buttons to add, rename, or delete a custom session and a draggable module list for each session.
- `AutomationWindow.Automation.cs`: Hooked custom session list bindings, drag/drop handling, validation, and reserved-name checks so built-in sessions cannot be renamed or deleted. Added add/rename/delete handlers using the new UI.
- `SettingsViewModel.PacsProfiles.cs`: Introduced `CustomAutomationSession` model and persisted `CustomSessions` to `%APPDATA%/Wysg.Musm/Radium/Accounts/{account}/Pacs/{pacs}/automation.json` alongside existing sequences.

## Usage
1. Open AutomationWindow and scroll past the Delete pane to the new **Custom Sessions** area.
2. Click **Add session**, enter a unique name (built-in names are reserved), then drag modules from the Library/Custom Modules into the session list.
3. Use **Rename** or **Delete** on a custom session header to manage it. Built-in sessions remain unchanged.
4. Click **Save Automation** to persist; custom sessions serialize to the `CustomSessions` array in `automation.json` for the current tenant/PACS.

## Validation
- Add a custom session, drop modules into it, save automation, and verify `automation.json` contains `CustomSessions` with the saved name and sequence.
- Restart AutomationWindow and confirm custom sessions reload with the same names and modules.
- Confirm dragging modules to the Delete pane removes them and built-in sessions remain non-editable via the new controls.
