# Fix: Convert Selected to Global via API

**Date:** 2026-01-13
**Projects:** Wysg.Musm.Radium, Wysg.Musm.Radium.Api

## Problem
In Settings ¡æ Global Phrases, clicking **Convert Selected to Global** failed in API mode with:

```
Error converting phrases: Converting to global phrases not supported in API mode
```

Direct SQL access is deprecated, so conversion needed to go through the Radium API.

## Changes
- Added API DTOs and endpoint `POST /api/accounts/{accountId}/phrases/convert-global` returning conversion counts.
- Implemented repository logic to move account phrases to global (deduplicates by text, tracks duplicates skipped, deletes source rows in a transaction).
- Extended `RadiumApiClient` and `ApiPhraseServiceAdapter` to call the new endpoint and refresh caches so the UI updates after conversion.

## Usage
- From Global Phrases tab, select account phrases and choose **Convert Selected to Global**. The request is sent via API; duplicates are skipped, and new globals are created for unique texts.

## Notes
- Global phrases keep existing entries when a duplicate text already exists; the account-scoped source rows are removed.
- Cache refresh is triggered after conversion so completion data and lists stay in sync.
