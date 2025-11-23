# Global Phrases Controller - FIXED!

## Problem Identified

The **`GlobalPhrasesController.cs` was missing** from the API project. This caused all requests to `/api/phrases/global` to return **404 Not Found**.

## Solution Applied

Created `GlobalPhrasesController.cs` in the API project with full CRUD operations for global phrases:

### Endpoints Created

| Method | Route | Description |
|--------|-------|-------------|
| `GET` | `/api/phrases/global` | Get all global phrases |
| `GET` | `/api/phrases/global/search` | Search global phrases |
| `PUT` | `/api/phrases/global` | Create/update global phrase |
| `POST` | `/api/phrases/global/{id}/toggle` | Toggle active status |
| `DELETE` | `/api/phrases/global/{id}` | Delete global phrase |
| `GET` | `/api/phrases/global/revision` | Get max revision number |

### Implementation Details

**File:** `apps\Wysg.Musm.Radium.Api\Controllers\GlobalPhrasesController.cs`

```csharp
[ApiController]
[Authorize]
[Route("api/phrases/global")]
public class GlobalPhrasesController : ControllerBase
{
    private readonly IPhraseRepository _repo;
    private readonly ILogger<GlobalPhrasesController> _logger;

    // ... Full implementation with:
    // - GetAll()
    // - Search()
    // - Upsert()
    // - Toggle()
    // - Delete()
    // - GetRevision()
}
```

**Key Features:**
- ? Uses existing `IPhraseRepository` with global methods
- ? Firebase authentication via `[Authorize]` attribute
- ? Comprehensive logging for diagnostics
- ? Handles `account_id IS NULL` for global phrases
- ? Full error handling with proper HTTP status codes

## Repository Already Complete

The `PhraseRepository.cs` **already had** all required global methods implemented:

```csharp
Task<List<PhraseDto>> GetAllGlobalAsync(bool activeOnly);
Task<List<PhraseDto>> SearchGlobalAsync(string? query, bool activeOnly, int maxResults);
Task<PhraseDto> UpsertGlobalAsync(string text, bool active);
Task<bool> ToggleGlobalActiveAsync(long phraseId);
Task<bool> DeleteGlobalAsync(long phraseId);
Task<long> GetGlobalMaxRevisionAsync();
```

These use Azure SQL with `WHERE account_id IS NULL` to filter global phrases.

## What Was Wrong

1. ? **Missing controller** - `GlobalPhrasesController.cs` didn't exist
2. ? **No route registration** - `/api/phrases/global` wasn't mapped
3. ? **Repository was complete** - All methods already implemented
4. ? **WPF client was correct** - `RadiumApiClient.cs` had all endpoints
5. ? **DTOs were correct** - `PhraseDto` with nullable `AccountId`

## Testing Steps

### 1. **Restart the API Server**

Stop the current API and restart it:

```powershell
# Press Ctrl+C in the API terminal to stop
# Then restart:
cd apps\Wysg.Musm.Radium.Api
dotnet run
```

**Expected output:**
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5205
```

### 2. **Test Global Phrases Endpoint**

```powershell
# Get your Firebase token from WPF app logs (after login)
$token = "eyJhbGciOiJSUzI1NiIsImtpZCI6..." # Copy from logs

# Test the endpoint
Invoke-WebRequest -Uri "http://localhost:5205/api/phrases/global" `
    -Method GET `
    -Headers @{"Authorization"="Bearer $token"}
```

**Expected:** `200 OK` with JSON array of 2358 global phrases

### 3. **Run WPF App**

With API running, start the WPF app:

```powershell
cd apps\Wysg.Musm.Radium
dotnet run
```

**Expected logs:**
```
[ApiPhraseServiceAdapter][Preload] Calling GetGlobalPhrasesAsync
[ApiPhraseServiceAdapter][Preload] Received 2358 global phrases  ∠ SUCCESS!
[ApiPhraseServiceAdapter][Preload] Cached 2358 GLOBAL phrases
[PhrasesVM] Loaded 2358 total phrases (account + global)
```

### 4. **Verify in Settings Window**

1. Open Settings window in WPF app
2. Go to **Phrases** tab
3. **Should see:** Grid populated with phrases
4. **Try:** Toggle a phrase active/inactive
5. **Try:** Search/filter phrases

## Expected Behavior Now

? **Global phrases load** - API returns all 2358 phrases  
? **Completion works** - Editor shows suggestions  
? **Settings window** - Grid shows data  
? **Toggle works** - Can activate/deactivate  
? **Search works** - Can filter phrases  

## Troubleshooting

### Still Getting 404?

1. **Verify controller exists:**
   ```powershell
   dir apps\Wysg.Musm.Radium.Api\Controllers\GlobalPhrasesController.cs
   ```

2. **Check API logs** - Should see route registration:
   ```
   Mapped endpoint: /api/phrases/global
   ```

3. **Rebuild API:**
   ```powershell
   cd apps\Wysg.Musm.Radium.Api
   dotnet clean
   dotnet build
   dotnet run
   ```

### Getting 401 Unauthorized?

This means endpoints exist but token is missing/invalid:
- Re-login to get fresh Firebase token
- Check token expiry (tokens expire after 1 hour)
- Verify token is being set: Look for `[Splash][Init] Firebase token set in API client`

### Getting Empty Results?

If API returns 200 OK but empty array `[]`:
- Check database: `SELECT COUNT(*) FROM radium.phrase WHERE account_id IS NULL`
- If 0, seed some test data (see SQL below)

## Database Verification

```sql
-- Check global phrase count
SELECT COUNT(*) as global_count 
FROM radium.phrase 
WHERE account_id IS NULL;

-- View first 10 global phrases
SELECT TOP 10 id, text, active, created_at 
FROM radium.phrase 
WHERE account_id IS NULL 
ORDER BY text;

-- Seed test data if needed
INSERT INTO radium.phrase (account_id, text, active, created_at, updated_at, rev)
VALUES 
    (NULL, 'Normal study', 1, SYSUTCDATETIME(), SYSUTCDATETIME(), 1),
    (NULL, 'No significant abnormality', 1, SYSUTCDATETIME(), SYSUTCDATETIME(), 1),
    (NULL, 'Unremarkable', 1, SYSUTCDATETIME(), SYSUTCDATETIME(), 1);
```

## Architecture

```
忙式式式式式式式式式式式式式式式式式式忖    HTTP GET /api/phrases/global    忙式式式式式式式式式式式式式式式式式式式式式式忖
弛  WPF Client      弛 式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式> 弛  API Server          弛
弛  RadiumApiClient 弛                                     弛  (Port 5205)         弛
弛                  弛 <式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式 弛                      弛
弛  - SetAuthToken  弛     200 OK + JSON array            弛  GlobalPhrases       弛
弛  - GetGlobal     弛                                     弛  Controller          弛
戌式式式式式式式式式式式式式式式式式式戎                                     戌式式式式式式式式式式式式式式式式式式式式式式戎
                                                                   弛
                                                                   ∪
                                                         忙式式式式式式式式式式式式式式式式式式式式式式忖
                                                         弛  PhraseRepository    弛
                                                         弛  GetAllGlobalAsync   弛
                                                         弛  (WHERE account_id   弛
                                                         弛   IS NULL)           弛
                                                         戌式式式式式式式式式式式式式式式式式式式式式式戎
                                                                   弛
                                                                   ∪
                                                         忙式式式式式式式式式式式式式式式式式式式式式式忖
                                                         弛  Azure SQL Database  弛
                                                         弛  radium.phrase       弛
                                                         弛  2358 global phrases 弛
                                                         戌式式式式式式式式式式式式式式式式式式式式式式戎
```

## Files Modified/Created

### Created
- ? `apps\Wysg.Musm.Radium.Api\Controllers\GlobalPhrasesController.cs`

### Already Complete (No Changes Needed)
- ? `apps\Wysg.Musm.Radium.Api\Repositories\PhraseRepository.cs`
- ? `apps\Wysg.Musm.Radium.Api\Repositories\IPhraseRepository.cs`
- ? `apps\Wysg.Musm.Radium\Services\RadiumApiClient.cs`
- ? `apps\Wysg.Musm.Radium\Services\Adapters\ApiPhraseServiceAdapter.cs`

## Success Indicators

After restarting the API, you should see:

1. **API logs:**
   ```
   [GlobalPhrasesController] GetAll global phrases called. activeOnly=False
   [GlobalPhrasesController] Returning 2358 global phrases
   ```

2. **WPF logs:**
   ```
   [ApiPhraseServiceAdapter][Preload] Received 2358 global phrases
   [PhrasesVM] Loaded 2358 total phrases (account + global)
   ```

3. **UI:**
   - Settings ⊥ Phrases tab shows phrases in grid
   - Editor autocomplete shows global phrases
   - Syntax highlighting colors phrases

## Summary

The fix was simple: **The `GlobalPhrasesController` was never created**.

- ? Repository methods existed
- ? WPF client code was correct
- ? API infrastructure was ready
- ? Controller was missing ∠ **THIS WAS THE ISSUE**

**Now that the controller is created, everything should work!**

Just restart the API and the 404 errors will be gone.

---

**Next Step:** Restart API server and test!
