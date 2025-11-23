# Quick Start - Get API Running for Global Phrases

## The Issue
You're getting **404 errors** because **the API server isn't running**. The WPF client is trying to connect to `http://localhost:5205` but nothing is listening there.

## Solution: Start the API Server

### Option 1: Command Line (Recommended)

```powershell
# Open a NEW terminal window (keep it running)
cd C:\Users\wysg\source\repos\dionephy\Wysg.Musm\apps\Wysg.Musm.Radium.Api

# Run the API
dotnet run
```

**Expected output:**
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5205
      Now listening on: https://localhost:7205
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

**Keep this terminal open** - the API needs to stay running while you use the WPF app.

### Option 2: Visual Studio

1. **Right-click** `Wysg.Musm.Radium.Api` project
2. Select **"Set as Startup Project"**
3. Press **F5** (Start Debugging) or **Ctrl+F5** (Start Without Debugging)

### Option 3: Multiple Startup Projects

Configure VS to run both API and WPF together:

1. **Right-click solution** ¡æ **Properties**
2. Select **"Multiple startup projects"**
3. Set both projects to **"Start"**:
   - `Wysg.Musm.Radium.Api` ¡æ Start
   - `Wysg.Musm.Radium` ¡æ Start
4. Click **OK**
5. Press **F5**

## Verify API is Running

### Test in Browser
Open: http://localhost:5205/health

**Expected:** `200 OK` or `Healthy` response

### Test in PowerShell
```powershell
Invoke-WebRequest -Uri "http://localhost:5205/health" -Method GET
```

## Then Run WPF App

Once the API is running:

1. **Set `Wysg.Musm.Radium` as startup project** (if using Option 1 or 2)
2. **Press F5** to debug
3. **Login** with your Firebase credentials
4. **Check Debug Output** - should see:
   ```
   [ApiPhraseServiceAdapter][Preload] Calling GetAllPhrasesAsync for account=1
   [ApiPhraseServiceAdapter][Preload] Received X account phrases
   [ApiPhraseServiceAdapter][Preload] Calling GetGlobalPhrasesAsync
   [ApiPhraseServiceAdapter][Preload] Received Y global phrases
   ```

## Quick Diagnostic

Run this to check current status:

```powershell
# Check if API port is open
Test-NetConnection -ComputerName localhost -Port 5205

# If NameResolutionSucceeded = False ¡æ API not running
# If TcpTestSucceeded = True ¡æ API is running ?
```

## Common Issues

### Issue: "Connection refused"
**Solution:** API server not started. Run `dotnet run` in API project folder.

### Issue: "Port already in use"
**Solution:** Another process is using port 5205.

Find and kill it:
```powershell
# Find process on port 5205
netstat -ano | findstr :5205

# Kill process (replace PID with actual number)
taskkill /F /PID 12345
```

### Issue: "404 Not Found"
**Solution:** API is running but routes not configured. Check:
- `GlobalPhrasesController.cs` exists
- `Program.cs` has `app.MapControllers();`
- Rebuild API project

### Issue: "401 Unauthorized"
**Solution:** This means endpoints exist but token is missing/invalid.
- Verify token is being set: Look for `[Splash][Init] Firebase token set in API client`
- Check token expiry (tokens expire after 1 hour)
- Re-login to get fresh token

## Expected Full Startup Sequence

### Terminal 1: API Server
```
C:\...\Wysg.Musm\apps\Wysg.Musm.Radium.Api> dotnet run
Building...
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5205
Application started. Press Ctrl+C to shut down.
```

### Terminal 2 (or VS): WPF App
```
[DI] API Mode: ENABLED (via API)
[Splash][Init] Begin
[Splash][Init][Stage] Refresh done success=True
[Splash][Init] Firebase token set in API client
[ApiPhraseServiceAdapter][Preload] Calling GetAllPhrasesAsync for account=1
[ApiPhraseServiceAdapter][Preload] Received 0 account phrases
[ApiPhraseServiceAdapter][Preload] Calling GetGlobalPhrasesAsync
[ApiPhraseServiceAdapter][Preload] Received 245 global phrases ¡ç SUCCESS!
```

## Still Getting 404?

If API is running and you still get 404, the endpoints might not exist. Run this diagnostic:

```powershell
# Navigate to API folder
cd apps\Wysg.Musm.Radium.Api

# Check if GlobalPhrasesController exists
dir Controllers\GlobalPhrasesController.cs

# If missing, create it from the troubleshooting guide
```

## Database Check

Verify global phrases exist in your database:

```sql
-- Connect to your Azure SQL or local database
SELECT COUNT(*) as global_phrase_count 
FROM radium.phrase 
WHERE account_id IS NULL;

-- Should return count > 0
```

If count = 0, you have no global phrases. Seed some:

```sql
INSERT INTO radium.phrase (account_id, text, active, created_at, updated_at, rev)
VALUES 
    (NULL, 'Normal study', 1, SYSUTCDATETIME(), SYSUTCDATETIME(), 1),
    (NULL, 'No significant abnormality', 1, SYSUTCDATETIME(), SYSUTCDATETIME(), 1),
    (NULL, 'Unremarkable', 1, SYSUTCDATETIME(), SYSUTCDATETIME(), 1);
```

## Success Indicators

? API terminal shows "Now listening on: http://localhost:5205"  
? Browser test returns HTTP 200 or "Healthy"  
? WPF logs show "Received X global phrases"  
? Settings window shows global phrases in grid  

## Summary

**The #1 most common issue is forgetting to start the API server.**

?? **Just run this in a terminal and keep it open:**
```powershell
cd apps\Wysg.Musm.Radium.Api
dotnet run
```

Then start your WPF app. Global phrases should load!
