# Change: Settings Integrations API test button

**Date:** 2026-01-31  
**Status:** Completed  
**Category:** UI/Settings

## Context
Users need a quick way to verify the configured API endpoint and confirm a simple database-backed API call from the Settings window.

## Changes
- Added a **Test API** button in Settings → Integrations → Essential.
- The test performs an API health check and then calls the user settings endpoint (DB-backed) for the current account.
- When no account is logged in, the test uses an anonymous `app.account` count call to validate database access.
- Results are shown via message boxes with guidance when authorization is missing.

## Testing
1. Open Settings → Integrations.
2. Click **Test API**.
3. Confirm the health check succeeds and the DB call returns settings for the current account.
4. Log out and click **Test API** again to confirm the account count check succeeds.
5. Verify the warning when auth is missing.
