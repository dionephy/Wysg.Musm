# Global Phrase 404 Error - Troubleshooting Guide

## Problem Summary
The WPF client is getting **404 errors** when trying to load global phrases, even though the auth token is being set correctly.

## Log Evidence
```
[Splash][Init] Firebase token set in API client  ¡ç Token IS being set
[ApiPhraseServiceAdapter][Preload] Loading phrases for account 1
[ApiPhraseServiceAdapter][Preload] 404 (likely no auth token set yet): Response status code does not indicate success: 404 (Not Found).
```

## Root Cause Analysis
The **404 error means the API endpoint doesn't exist or isn't accessible**. This has nothing to do with authentication - a 401 would indicate auth failure. 404 means:

1. **API server is not running**
2. **Endpoint route doesn't exist**
3. **Controller not registered in DI**
4. **Wrong base URL**

## Step-by-Step Troubleshooting

### Step 1: Verify API Server is Running

**Check if the API is running:**
```powershell
# In a separate terminal, navigate to API project
cd apps\Wysg.Musm.Radium.Api

# Run the API server
dotnet run

# You should see output like:
# info: Microsoft.Hosting.Lifetime[14]
#       Now listening on: http://localhost:5205
```

**Expected Output:**
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5205
      Now listening on: https://localhost:7205
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

If you don't see this, **the API is NOT running** and all requests will fail.

### Step 2: Test API Endpoints Manually

Once the API is running, test the endpoints:

**Test Account Phrases Endpoint:**
```powershell
# Get a test Firebase token first (from successful login in WPF app logs)
$token = "eyJhbGciOiJSUzI1NiIsImtpZCI6..." # Copy from logs

# Test account phrases endpoint
curl -H "Authorization: Bearer $token" http://localhost:5205/api/accounts/1/phrases
```

**Expected Response:** `200 OK` with JSON array (even if empty: `[]`)  
**Actual if broken:** `404 Not Found`

**Test Global Phrases Endpoint:**
```powershell
curl -H "Authorization: Bearer $token" http://localhost:5205/api/phrases/global
```

**Expected Response:** `200 OK` with JSON array  
**Actual if broken:** `404 Not Found`

### Step 3: Verify GlobalPhrasesController Exists

Check if the file exists:
```
apps\Wysg.Musm.Radium.Api\Controllers\GlobalPhrasesController.cs
```

If missing, **the controller was not created**. Create it:

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wysg.Musm.Radium.Api.Models.Dtos;
using Wysg.Musm.Radium.Api.Repositories;

namespace Wysg.Musm.Radium.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/phrases/global")]
    public class GlobalPhrasesController : ControllerBase
    {
        private readonly IPhraseRepository _repo;
        private readonly ILogger<GlobalPhrasesController> _logger;

        public GlobalPhrasesController(IPhraseRepository repo, ILogger<GlobalPhrasesController> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<List<PhraseDto>>> GetAll([FromQuery] bool activeOnly = false)
        {
            var list = await _repo.GetAllGlobalAsync(activeOnly);
            return Ok(list);
        }

        [HttpGet("search")]
        public async Task<ActionResult<List<PhraseDto>>> Search(
            [FromQuery] string? query, 
            [FromQuery] bool activeOnly = true, 
            [FromQuery] int maxResults = 100)
        {
            var list = await _repo.SearchGlobalAsync(query, activeOnly, maxResults);
            return Ok(list);
        }

        [HttpPut]
        public async Task<ActionResult<PhraseDto>> Upsert([FromBody] UpsertPhraseRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Text))
                return BadRequest("Phrase text cannot be empty");
            
            var dto = await _repo.UpsertGlobalAsync(request.Text.Trim(), request.Active);
            return Ok(dto);
        }

        [HttpPost("{phraseId}/toggle")]
        public async Task<IActionResult> Toggle(long phraseId)
        {
            if (!await _repo.ToggleGlobalActiveAsync(phraseId))
                return NotFound();
            return NoContent();
        }

        [HttpDelete("{phraseId}")]
        public async Task<IActionResult> Delete(long phraseId)
        {
            if (!await _repo.DeleteGlobalAsync(phraseId))
                return NotFound();
            return NoContent();
        }

        [HttpGet("revision")]
        public async Task<ActionResult<long>> GetRevision()
        {
            var rev = await _repo.GetGlobalMaxRevisionAsync();
            return Ok(rev);
        }
    }
}
```

### Step 4: Verify Repository Methods Exist

Check `PhraseRepository.cs` has these methods:
- `GetAllGlobalAsync(bool activeOnly)`
- `SearchGlobalAsync(string? query, bool activeOnly, int maxResults)`
- `UpsertGlobalAsync(string text, bool active)`
- `ToggleGlobalActiveAsync(long phraseId)`
- `DeleteGlobalAsync(long phraseId)`
- `GetGlobalMaxRevisionAsync()`

If missing, the implementation is incomplete.

### Step 5: Check API Startup Logs

When you start the API with `dotnet run`, look for these logs:

```
info: Microsoft.AspNetCore.Routing.EndpointMiddleware[0]
      Mapped endpoint routes:
      - /api/accounts/{accountId}/phrases
      - /api/phrases/global ¡ç Should see this!
```

If `/api/phrases/global` is **NOT listed**, the controller is not being discovered.

**Common causes:**
- Controller class not `public`
- Missing `[ApiController]` attribute
- Wrong namespace
- Not included in build (check `.csproj`)

### Step 6: Check Azure SQL Connection

If endpoints exist but return 404/500:

```sql
-- Connect to Azure SQL and verify global phrases exist
SELECT COUNT(*) FROM radium.phrase WHERE account_id IS NULL;
```

**Expected:** Count > 0  
**If 0:** No global phrases in database - need to seed data

**Seed global phrases:**
```sql
INSERT INTO radium.phrase (account_id, text, active, created_at, updated_at, rev)
VALUES 
    (NULL, 'Normal study', 1, SYSUTCDATETIME(), SYSUTCDATETIME(), 1),
    (NULL, 'No significant abnormality', 1, SYSUTCDATETIME(), SYSUTCDATETIME(), 1),
    (NULL, 'Unremarkable', 1, SYSUTCDATETIME(), SYSUTCDATETIME(), 1);
```

### Step 7: Verify Base URL Configuration

Check `appsettings.Development.json` in WPF project:

```json
{
  "ApiSettings": {
    "BaseUrl": "http://localhost:5205"
  }
}
```

And environment variable:
```powershell
$env:RADIUM_API_URL
# Should return: http://localhost:5205
```

### Step 8: Test Without Authentication

Temporarily remove `[Authorize]` from `GlobalPhrasesController`:

```csharp
[ApiController]
// [Authorize] ¡ç Comment out temporarily
[Route("api/phrases/global")]
public class GlobalPhrasesController : ControllerBase
```

Then test:
```powershell
curl http://localhost:5205/api/phrases/global
```

If this works, the problem is **authentication configuration**.  
If this still fails, the problem is **routing or controller registration**.

## Quick Diagnostic Checklist

- [ ] API server is running (`dotnet run` in API project)
- [ ] Listening on http://localhost:5205
- [ ] `GlobalPhrasesController.cs` exists
- [ ] Repository methods implemented
- [ ] Database has global phrases (account_id IS NULL)
- [ ] Base URL is correct in config
- [ ] Controller is public and has `[ApiController]`
- [ ] No build errors in API project

## Expected Working Flow

1. **WPF App starts** ¡æ Loads `appsettings.json` ¡æ BaseUrl = http://localhost:5205
2. **User logs in** ¡æ Firebase token obtained ¡æ `_apiClient.SetAuthToken(token)`
3. **Token set** ¡æ All HTTP requests include `Authorization: Bearer {token}` header
4. **API call** ¡æ `GET http://localhost:5205/api/phrases/global`
5. **API server** ¡æ Authenticates token ¡æ Routes to `GlobalPhrasesController.GetAll()`
6. **Controller** ¡æ Calls `_repo.GetAllGlobalAsync()` ¡æ Queries `WHERE account_id IS NULL`
7. **Response** ¡æ `200 OK` with JSON array of global phrases
8. **WPF** ¡æ Deserializes ¡æ Caches in `_cachedGlobal`

## Common Solutions

### Solution 1: API Not Running
```powershell
cd apps\Wysg.Musm.Radium.Api
dotnet run
```

### Solution 2: Controller Not Created
Copy `GlobalPhrasesController.cs` from Step 3 above.

### Solution 3: Wrong Port
Check if API is running on different port:
```powershell
netstat -an | findstr 5205
```

### Solution 4: Firewall Blocking
```powershell
# Allow port in Windows Firewall
netsh advfirewall firewall add rule name="Radium API" dir=in action=allow protocol=TCP localport=5205
```

## Testing Script

Run this PowerShell script to diagnose:

```powershell
# test-api.ps1
Write-Host "=== Radium API Diagnostic ===" -ForegroundColor Cyan

# Check if API is running
Write-Host "`n1. Checking if API is running on port 5205..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "http://localhost:5205/health" -Method GET -TimeoutSec 2
    Write-Host "? API is running! Status: $($response.StatusCode)" -ForegroundColor Green
} catch {
    Write-Host "? API is NOT running or not responding" -ForegroundColor Red
    Write-Host "  Start API with: cd apps\Wysg.Musm.Radium.Api; dotnet run" -ForegroundColor Yellow
    exit
}

# Check global phrases endpoint (without auth - will fail if [Authorize] present)
Write-Host "`n2. Testing global phrases endpoint (no auth)..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "http://localhost:5205/api/phrases/global" -Method GET
    Write-Host "? Endpoint exists! Status: $($response.StatusCode)" -ForegroundColor Green
} catch {
    if ($_.Exception.Response.StatusCode -eq 401) {
        Write-Host "? Endpoint exists but requires authentication (expected)" -ForegroundColor Green
    } elseif ($_.Exception.Response.StatusCode -eq 404) {
        Write-Host "? Endpoint NOT FOUND (404)" -ForegroundColor Red
        Write-Host "  GlobalPhrasesController is missing or not registered" -ForegroundColor Yellow
    } else {
        Write-Host "? Error: $($_.Exception.Message)" -ForegroundColor Red
    }
}

# Check account phrases endpoint
Write-Host "`n3. Testing account phrases endpoint..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "http://localhost:5205/api/accounts/1/phrases" -Method GET
    Write-Host "? Endpoint exists! Status: $($response.StatusCode)" -ForegroundColor Green
} catch {
    if ($_.Exception.Response.StatusCode -eq 401) {
        Write-Host "? Endpoint exists but requires authentication (expected)" -ForegroundColor Green
    } elseif ($_.Exception.Response.StatusCode -eq 404) {
        Write-Host "? Endpoint NOT FOUND (404)" -ForegroundColor Red
    } else {
        Write-Host "? Error: $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host "`n=== Diagnostic Complete ===" -ForegroundColor Cyan
```

Save as `test-api.ps1` and run:
```powershell
.\test-api.ps1
```

## Next Steps

After confirming the API is running and endpoints exist:

1. **Run the WPF app** with improved logging (already added)
2. **Check Debug Output** for detailed error messages
3. **Copy the Firebase token** from logs
4. **Test manually** with curl/Postman using the token
5. **Verify database** has global phrases

## Still Not Working?

If you've verified everything above and still get 404:

1. **Check Program.cs** in API project - ensure controllers are mapped:
```csharp
app.MapControllers(); // Must be present!
```

2. **Check for route conflicts** - ensure no other controller uses `/api/phrases/global`

3. **Check build output** - ensure `GlobalPhrasesController.cs` is included:
```powershell
cd apps\Wysg.Musm.Radium.Api
dotnet build -v detailed | findstr GlobalPhrases
```

4. **Restart VS** - sometimes IntelliSense/build cache causes issues

5. **Clean solution**:
```powershell
dotnet clean
dotnet build
```

---

**Most likely issue**: API server is not running. Start it first!
