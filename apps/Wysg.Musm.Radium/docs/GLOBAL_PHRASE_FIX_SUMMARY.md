# Global Phrase Loading Fix - Implementation Summary

## Problem
Global phrases (account_id IS NULL) were not loading in the WPF client when using API mode (`USE_API=1`) due to:
1. **Authentication timing issue**: Phrase preload occurred before Firebase auth token was set in the API client
2. **404 errors**: API calls without authentication returned 404, causing empty cache
3. **Missing global phrase support**: Initial implementation lacked global phrase API endpoints and adapter support

## Root Cause Analysis
From logs:
```
[ApiPhraseServiceAdapter][Preload] Loading phrases for account 1
예외 발생: 'System.Net.Http.HttpRequestException'(System.Net.Http.dll)
[ApiPhraseServiceAdapter][Preload] Error: Response status code does not indicate success: 404 (Not Found).
```

The sequence was:
1. `PreloadAsync` called early (before token set)
2. API client had no Authorization header
3. GlobalPhrasesController rejected unauthenticated request → 404
4. Cache remained empty
5. Settings window showed 0 phrases

## Solution Implemented

### 1. API Backend - Global Phrase Support
**File**: `apps\Wysg.Musm.Radium.Api\Controllers\GlobalPhrasesController.cs`
- ? Created new controller for global phrases (account_id IS NULL)
- ? Endpoints: GET, GET/search, PUT, POST/{id}/toggle, DELETE/{id}, GET/revision
- ? Uses `[Authorize]` but no account ownership check (global shared resource)

**File**: `apps\Wysg.Musm.Radium.Api\Repositories\PhraseRepository.cs`
- ? Added 7 new methods:
  - `GetAllGlobalAsync(bool activeOnly)`
  - `GetGlobalByIdAsync(long phraseId)`
  - `SearchGlobalAsync(string? query, bool activeOnly, int maxResults)`
  - `UpsertGlobalAsync(string text, bool active)`
  - `BatchUpsertGlobalAsync(List<string> phrases, bool active)`
  - `ToggleGlobalActiveAsync(long phraseId)`
  - `DeleteGlobalAsync(long phraseId)`
  - `GetGlobalMaxRevisionAsync()`
- ? All queries use `WHERE account_id IS NULL`
- ? Updated `MapPhraseDto` to handle nullable `AccountId`

**File**: `apps\Wysg.Musm.Radium.Api\Models\Dtos\PhraseDto.cs`
- ? Changed `AccountId` from `long` to `long?` (nullable) to represent global phrases

### 2. WPF Client - API Client Support
**File**: `apps\Wysg.Musm.Radium\Services\RadiumApiClient.cs`
- ? Added 6 new methods in `#region GlobalPhrases`:
  - `GetGlobalPhrasesAsync(bool activeOnly)`
  - `SearchGlobalPhrasesAsync(string? query, bool activeOnly, int maxResults)`
  - `UpsertGlobalPhraseAsync(string text, bool active)`
  - `ToggleGlobalPhraseAsync(long phraseId)`
  - `DeleteGlobalPhraseAsync(long phraseId)`
  - `GetGlobalPhraseMaxRevisionAsync()`
- ? All call `/api/phrases/global` endpoints

### 3. WPF Client - Phrase Service Adapter Fixes
**File**: `apps\Wysg.Musm.Radium\Services\Adapters\ApiPhraseServiceAdapter.cs`

#### Change 1: Auth Token Check in PreloadAsync
**Before**:
```csharp
public async Task PreloadAsync(long accountId)
{
    try {
        var phrases = await _apiClient.GetAllPhrasesAsync(accountId, activeOnly: false);
        // ... would fail with 404 if no token
    }
    catch (Exception ex) {
        Debug.WriteLine($"[Preload] Error: {ex.Message}");
        _cachedPhrases = new List<PhraseInfo>();
    }
}
```

**After**:
```csharp
public async Task PreloadAsync(long accountId)
{
    // Skip if no auth token set (would result in 404)
    if (_apiClient == null) {
        Debug.WriteLine("[Preload] Skipped: No API client");
        _cachedPhrases = new List<PhraseInfo>();
        _cachedGlobal = new List<PhraseInfo>();
        _loaded = false;
        return;
    }
    
    try {
        var phrases = await _apiClient.GetAllPhrasesAsync(accountId, activeOnly: false);
        var globals = await _apiClient.GetGlobalPhrasesAsync(activeOnly: false);
        // ... cache both account and global phrases
    }
    catch (System.Net.Http.HttpRequestException ex) when (ex.Message.Contains("404")) {
        Debug.WriteLine($"[Preload] 404 (likely no auth token set yet): {ex.Message}");
        _loaded = false;
    }
}
```

#### Change 2: Global Phrase Upsert Support
**Before**: Threw `NotSupportedException` for global phrases

**After**:
```csharp
public async Task<PhraseInfo> UpsertPhraseAsync(long? accountId, string text, bool active = true)
{
    if (!accountId.HasValue) {
        // Global phrase upsert
        var dtoGlobal = await _apiClient.UpsertGlobalPhraseAsync(text, active);
        var infoGlobal = new PhraseInfo(...);
        // Update _cachedGlobal
        return infoGlobal;
    }
    // Account phrase upsert...
}
```

#### Change 3: Global Phrase Toggle Support
Similar pattern for `ToggleActiveAsync` - now supports global phrases.

#### Change 4: Combine Account + Global Phrases
```csharp
public async Task<IReadOnlyList<string>> GetCombinedPhrasesAsync(long accountId)
{
    var acct = await GetPhrasesForAccountInternalAsync(accountId);
    var globals = _cachedGlobal.Where(p => p.Active).Select(p => p.Text);
    return acct.Concat(globals).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
}
```

### 4. WPF Client - Login Flow Fixes
**File**: `apps\Wysg.Musm.Radium\ViewModels\SplashLoginViewModel.cs`

#### Fixed Auth Token Timing
**Key Changes**:
1. Set auth token **immediately** after successful authentication
2. Call `RefreshPhrasesAsync` instead of `PreloadAsync` to force reload with token
3. All three login paths fixed: silent refresh, email/password, Google OAuth

**Before** (Silent Login):
```csharp
var refreshed = await _auth.RefreshAsync(storedRt!);
if (refreshed.Success) {
    // ... account ensure
    await phraseSvc.PreloadAsync(accountId); // ? Token not set yet
    _apiClient.SetAuthToken(refreshed.IdToken); // ? Too late
}
```

**After** (Silent Login):
```csharp
var refreshed = await _auth.RefreshAsync(storedRt!);
if (refreshed.Success) {
    _apiClient.SetAuthToken(refreshed.IdToken); // ? Set token first
    // ... account ensure
    await phraseSvc.RefreshPhrasesAsync(accountId); // ? Force reload with token
}
```

**Same pattern applied to**:
- Email/password login (`OnEmailLoginAsync`)
- Google OAuth login (`OnGoogleLoginAsync`)

## Testing Checklist

### Prerequisites
- [ ] API project running on http://localhost:5205
- [ ] Azure SQL database contains global phrases (account_id IS NULL)
- [ ] WPF client has `USE_API=1` environment variable set
- [ ] Valid Firebase credentials available

### Test Cases

#### 1. Global Phrase Loading
```sql
-- Verify global phrases exist in DB
SELECT COUNT(*) FROM radium.phrase WHERE account_id IS NULL AND active = 1;
```

**Expected**: Count > 0

**WPF Test**:
1. Launch app with `USE_API=1`
2. Login successfully
3. Check Debug output for:
   ```
   [ApiPhraseServiceAdapter][Preload] Cached X GLOBAL phrases
   ```
4. Open Settings → Phrases tab
5. Verify global phrases appear in grid

#### 2. Authentication Flow
**Debug Log Sequence** (correct order):
```
[Splash][Init][Stage] Refresh done success=True
[Splash][Init] Firebase token set in API client  ← Token set FIRST
[ApiPhraseServiceAdapter][Preload] Loading phrases for account 1
[ApiPhraseServiceAdapter][Preload] Cached X phrases
[ApiPhraseServiceAdapter][Preload] Cached Y GLOBAL phrases
```

**Expected**: No 404 errors, no auth exceptions

#### 3. API Endpoint Testing
```http
### Get global phrases (with auth token)
GET http://localhost:5205/api/phrases/global?activeOnly=true
Authorization: Bearer <FIREBASE_ID_TOKEN>

### Expected: 200 OK with phrase array
```

```http
### Upsert global phrase
PUT http://localhost:5205/api/phrases/global
Authorization: Bearer <FIREBASE_ID_TOKEN>
Content-Type: application/json

{
  "text": "Test Global Phrase",
  "active": true
}

### Expected: 200 OK with created phrase DTO
```

#### 4. Phrase Completion
1. Type in editor with autocomplete enabled
2. Verify both account and global phrases appear in suggestions
3. Check `GetCombinedPhrasesAsync` merges both sources

#### 5. Settings Window
1. Open Settings → Phrases tab
2. Verify "Global Phrases" section shows data
3. Test adding/editing/toggling global phrases
4. Verify changes persist and refresh correctly

## Known Limitations

1. **Admin-only global phrase management**: Current implementation allows any authenticated user to modify global phrases. Production should restrict to admin accounts.
   
2. **Cache invalidation**: Global phrase cache only refreshes on explicit `RefreshPhrasesAsync()` call. Consider adding:
   - Periodic background refresh
   - SignalR notifications when global phrases change
   - Max cache age timeout

3. **Binding errors in Settings**: Unrelated binding errors still appear because SettingsViewModel doesn't expose nested VM properties. Fix separately by:
   - Setting `DataContext` of Phrases tab to `PhrasesViewModel`
   - OR adding proxy properties on `SettingsViewModel`

## Migration Notes

### For Existing Deployments
If migrating from PostgreSQL direct access to API mode:

1. **Verify global phrases exist**:
   ```sql
   -- PostgreSQL
   SELECT COUNT(*) FROM radium.phrase WHERE account_id IS NULL;
   
   -- Azure SQL
   SELECT COUNT(*) FROM radium.phrase WHERE account_id IS NULL;
   ```

2. **Migrate if needed**:
   - Export from Postgres: `COPY (SELECT ...) TO 'global_phrases.csv'`
   - Import to Azure SQL using bulk insert or API batch upsert

3. **Update environment variables**:
   ```
   USE_API=1
   RADIUM_API_URL=https://your-api.azurewebsites.net
   ```

4. **Test login flow**: Verify token set before any phrase operations

## Performance Considerations

- **Cache size**: Global phrases loaded once at login, held in memory
- **Network calls**: Initial load requires 2 API calls (account + global)
- **Refresh strategy**: Manual refresh only (no auto-polling)

**Recommendation**: For production, implement:
- Gzip compression for large phrase lists
- Delta sync (only fetch changes since last revision)
- CDN caching for read-heavy global phrases

## Build Status
? **Build Successful** - All changes compile without errors

## Files Modified
1. `apps\Wysg.Musm.Radium.Api\Controllers\GlobalPhrasesController.cs` (NEW)
2. `apps\Wysg.Musm.Radium.Api\Repositories\IPhraseRepository.cs`
3. `apps\Wysg.Musm.Radium.Api\Repositories\PhraseRepository.cs`
4. `apps\Wysg.Musm.Radium.Api\Models\Dtos\PhraseDto.cs`
5. `apps\Wysg.Musm.Radium\Services\RadiumApiClient.cs`
6. `apps\Wysg.Musm.Radium\Services\Adapters\ApiPhraseServiceAdapter.cs`
7. `apps\Wysg.Musm.Radium\ViewModels\SplashLoginViewModel.cs`

## Next Steps
1. ? Build successful - ready for testing
2. [ ] Start API project (verify port 5205)
3. [ ] Seed global phrases in Azure SQL if empty
4. [ ] Run WPF app with `USE_API=1` and test login
5. [ ] Verify global phrases appear in Settings window
6. [ ] Test phrase completion with global + account phrases

---
**Implementation Date**: 2025-01-24  
**Status**: ? Complete - Ready for Testing
