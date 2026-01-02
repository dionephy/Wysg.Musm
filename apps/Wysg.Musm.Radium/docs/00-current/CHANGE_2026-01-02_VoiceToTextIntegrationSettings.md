# Change: Voice to text integration settings surface

**Date:** 2026-01-02  
**Status:** Completed  
**Category:** UI/Settings

## Context
Users need to control the foreign textbox sync ("Sync text") feature from Settings and hide the toggle when voice-to-text integration is not desired.

## Changes
- Added a **Voice to text** group in Settings ¡æ Integrations with enable checkbox plus bookmark pickers for the target textbox and toggle button (shares the UI bookmark list from Automation).
- Persisted voice-to-text settings in local settings storage.
- Main window hides the **Sync text** toggle when voice-to-text integration is disabled and forces it off if disabled.

## Additional Controls (2026-01-02 update)
- Added **Save** button in Voice to text group to persist enable/disable and bookmark selections per current user.
- Added **Test textbox** button to read the selected textbox bookmark's Value/Name and show it in a message box for quick validation.
- Added **Text toggle** button to invoke the selected toggle bookmark directly from Settings for verification.
- Bookmark pickers now sit on the same row for compact layout.

## Persistence and test actions (2026-01-02 update)
- Voice-to-text enable and bookmark selections now load and save with the main **Save** action (user-scoped configuration).
- The toggle action button is renamed to **Test toggle** and no longer shows a success message; it only invokes the bookmark (errors still show).

## Visibility fix (2026-01-02 update)
- Text sync toggle on the main toolbar now reflects the saved voice-to-text enable flag on startup.
- Saving voice-to-text settings immediately updates the main window toggle visibility without restarting.

## Visibility sync on save (2026-01-02 update)
- Saving settings (either main Save or Voice to text Save) now updates the main window toggle visibility to match the Enable voice to text integration flag immediately.

## Bookmark-driven text sync (2026-01-02 update)
- Text sync now resolves the textbox from the configured **Text bookmark** in Settings ¡æ Integrations ¡æ Voice to text, falling back to `ForeignTextbox` only if none is set.

## Toggle test UI changes (2026-01-02 update)
- Renamed **Test toggle** to **Test button** (invokes the selected toggle bookmark).
- Added **Check toggle** button to show the current state (On/Off/Indeterminate) of the selected toggle bookmark via a message box.

## Testing
1. Open Settings ¡æ Integrations ¡æ Voice to text.
2. Toggle **Enable voice to text integration** on/off; select bookmarks from the dropdowns.
3. Save settings, reopen Settings: selections persist and dropdowns share the UI bookmark list.
4. When disabled, the Sync text toggle in the main window is hidden and any active sync is turned off; when enabled, the toggle reappears.
5. Verify that text sync works with the configured textbox bookmark and falls back to `ForeignTextbox` only when no bookmark is set.
