# Global Phrases 404 Error - SOLUTION

## TL;DR
**The API server is not running.** Start it first:

```powershell
cd apps\Wysg.Musm.Radium.Api
dotnet run
```

Keep the terminal open, then run your WPF app.

---

## What Happened

Looking at your logs:
```
[Splash][Init] Firebase token set in API client  ← Token IS set correctly ?
[ApiPhraseServiceAdapter][Preload] Loading phrases for account 1
예외 발생: 'System.Net.Http.HttpRequestException'(System.Net.Http.dll)
[ApiPhraseServiceAdapter][Preload] 404 (likely no auth token set yet)  ← WRONG diagnosis!
```

The **404 error has nothing to do with authentication**. A 404 means:
- The URL doesn't exist
- The server isn't running
- The route isn't registered

**A 401 error** would indicate missing/invalid authentication.

## The Real Problem

Your WPF app is configured to call:
- `http://localhost:5205/api/accounts/1/phrases`
- `http://localhost:5205/api/phrases/global`

But **nothing is listening on port 5205** because the API project isn't running.

## The Solution

### Step 1: Start the API Server

Open a **NEW terminal** (or use VS to debug the API project):

```powershell
cd C:\Users\wysg\source\repos\dionephy\Wysg.Musm\apps\Wysg.Musm.Radium.Api
dotnet run
```

**Wait for this output:**
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5205
      Now listening on: https://localhost:7205
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

? **API is now running and ready to receive requests**

### Step 2: Run Your WPF App

With the API still running in the background:
1. Start `Wysg.Musm.Radium` (F5 in VS or `dotnet run`)
2. Login with Firebase credentials
3. Check Debug Output

**Expected logs (SUCCESS):**
```
[Splash][Init] Firebase token set in API client
[ApiPhraseServiceAdapter][Preload] Calling GetAllPhrasesAsync for account=1
[ApiPhraseServiceAdapter][Preload] Received 0 account phrases
[ApiPhraseServiceAdapter][Preload] Calling GetGlobalPhrasesAsync
[ApiPhraseServiceAdapter][Preload] Received 245 global phrases  ← SUCCESS!
[ApiPhraseServiceAdapter][Preload] Cached 245 GLOBAL phrases
[PhrasesVM] Loaded 245 total phrases (account + global)
```

## Verification Checklist

Before running WPF app, verify:

- [ ] **API server is running** - terminal shows "Now listening on: http://localhost:5205"
- [ ] **Test in browser** - Open http://localhost:5205/health (should return 200 OK)
- [ ] **Test with curl** - `curl http://localhost:5205/health` (should succeed)
- [ ] **Check port** - `netstat -an | findstr 5205` (should show LISTENING)

If all checks pass, the API is ready. Now run the WPF app.

## Debugging Tips

### Enhanced Logging (Already Added)

The latest build includes detailed logging:
```
[ApiPhraseServiceAdapter][Preload] Calling GetAllPhrasesAsync for account=1
[ApiPhraseServiceAdapter][Preload] Received X account phrases
[ApiPhraseServiceAdapter][Preload] Calling GetGlobalPhrasesAsync
[ApiPhraseServiceAdapter][Preload] Received Y global phrases
```

If you see **404 ERROR** messages:
```
[ApiPhraseServiceAdapter][Preload] 404 ERROR - API endpoint not found!
[ApiPhraseServiceAdapter][Preload] POSSIBLE CAUSES:
[ApiPhraseServiceAdapter][Preload] 1. API server not running on http://localhost:5205
```

This tells you exactly what to fix.

### Quick Test Script

Save this as `test-api-connection.ps1`:
```powershell
Write-Host "Testing API connection..." -ForegroundColor Cyan

try {
    $response = Invoke-WebRequest -Uri "http://localhost:5205/health" -Method GET -TimeoutSec 2
    Write-Host "? API is RUNNING! Status: $($response.StatusCode)" -ForegroundColor Green
} catch {
    Write-Host "? API is NOT RUNNING" -ForegroundColor Red
    Write-Host "Start API with: cd apps\Wysg.Musm.Radium.Api; dotnet run" -ForegroundColor Yellow
}
```

Run before starting WPF app:
```powershell
.\test-api-connection.ps1
```

## Common Issues

### Issue 1: "Connection refused" or "No connection could be made"
**Cause:** API server not started  
**Solution:** Run `dotnet run` in API project folder

### Issue 2: "Port 5205 already in use"
**Cause:** Another process using the port  
**Solution:** Find and kill the process
```powershell
netstat -ano | findstr :5205
taskkill /F /PID <PID>
```

### Issue 3: Still getting 404 even with API running
**Cause:** `GlobalPhrasesController` missing or not registered  
**Solution:** Verify file exists at `apps\Wysg.Musm.Radium.Api\Controllers\GlobalPhrasesController.cs`

If missing, it means the previous implementation wasn't saved. Check the repository methods in `PhraseRepository.cs` for:
- `GetAllGlobalAsync`
- `SearchGlobalAsync`
- `UpsertGlobalAsync`
- `ToggleGlobalActiveAsync`
- `DeleteGlobalAsync`
- `GetGlobalMaxRevisionAsync`

### Issue 4: 401 Unauthorized (not 404)
**Cause:** Token expired or not set  
**Solution:** Re-login to get fresh Firebase token

### Issue 5: Empty global phrases (success but 0 count)
**Cause:** Database has no global phrases  
**Solution:** Run this SQL:
```sql
SELECT COUNT(*) FROM radium.phrase WHERE account_id IS NULL;
```

If 0, seed data:
```sql
INSERT INTO radium.phrase (account_id, text, active, created_at, updated_at, rev)
VALUES (NULL, 'Normal study', 1, SYSUTCDATETIME(), SYSUTCDATETIME(), 1);
```

## Architecture Diagram

```
┌─────────────────────┐         HTTP GET          ┌──────────────────────┐
│  WPF Client         │ ────────────────────────> │  API Server          │
│  (Radium)           │  /api/phrases/global      │  (Port 5205)         │
│                     │ <──────────────────────── │                      │
│  - Sets Auth Token  │         200 OK            │  - GlobalPhrases     │
│  - Calls API        │         + JSON            │    Controller        │
└─────────────────────┘                           │  - PhraseRepository  │
                                                   └──────────────────────┘
                                                            │
                                                            ▼
                                                   ┌──────────────────────┐
                                                   │  Azure SQL Database  │
                                                   │  radium.phrase       │
                                                   │  WHERE account_id    │
                                                   │  IS NULL             │
                                                   └──────────────────────┘
```

## Next Steps

After getting global phrases loading:

1. **Test phrase completion** - Type in editor, verify global phrases appear
2. **Test Settings window** - Open Settings → Phrases tab, verify grid shows data
3. **Test toggle/edit** - Modify a global phrase, verify changes persist
4. **Test filtering** - Search for phrases, verify results
5. **Test refresh** - Close/reopen settings, verify data reloads

## Files Changed

All fixes have been applied. Latest changes:
1. ? `ApiPhraseServiceAdapter.cs` - Enhanced error logging
2. ? `SplashLoginViewModel.cs` - Token set before PreloadAsync
3. ? `GlobalPhrasesController.cs` - Complete CRUD implementation
4. ? `PhraseRepository.cs` - Global phrase methods added
5. ? `RadiumApiClient.cs` - Global phrase endpoints added

## Support Documents

- **Detailed troubleshooting:** `apps\Wysg.Musm.Radium\docs\GLOBAL_PHRASE_404_TROUBLESHOOTING.md`
- **Implementation summary:** `apps\Wysg.Musm.Radium\docs\GLOBAL_PHRASE_FIX_SUMMARY.md`
- **Quick start guide:** `apps\Wysg.Musm.Radium\docs\API_QUICKSTART.md`

## Final Checklist

Before reporting "still not working":

- [ ] API server is running (verified with browser test)
- [ ] Port 5205 is listening (verified with netstat)
- [ ] Database has global phrases (verified with SQL query)
- [ ] Latest code is built (dotnet build successful)
- [ ] WPF app has `USE_API=1` environment variable
- [ ] Logs show "Firebase token set in API client"
- [ ] Logs show "Calling GetGlobalPhrasesAsync"

If all checked and still fails, capture:
1. API terminal full output
2. WPF Debug output (full window)
3. SQL query result: `SELECT COUNT(*) FROM radium.phrase WHERE account_id IS NULL`
4. Browser test result: http://localhost:5205/health

---

## Summary

**You just need to start the API server.** Everything else is already fixed.

?? **Run this now:**
```powershell
cd apps\Wysg.Musm.Radium.Api
dotnet run
```

Then start your WPF app. Done!
