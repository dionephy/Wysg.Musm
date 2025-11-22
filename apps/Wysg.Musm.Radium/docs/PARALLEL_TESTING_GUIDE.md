# ??? Parallel Testing: API vs Direct Database

## Overview

Your app now supports **both** database access methods:
- **Direct Database** (default, current production method)
- **API Access** (new, for testing and future production)

You can switch between them using a simple environment variable!

---

## ?? How to Use

### Default Mode: Direct Database Access ?

**No configuration needed!** Just run your app:

```powershell
cd apps\Wysg.Musm.Radium
dotnet run
```

Your app will use **direct Azure SQL database access** (current behavior).

---

### API Mode: Use the Backend API ??

#### Step 1: Start the API
```powershell
# Terminal 1: Run the API
cd apps\Wysg.Musm.Radium.Api
dotnet run
```

Wait for:
```
Now listening on: http://localhost:5205
```

#### Step 2: Enable API Mode in WPF App
```powershell
# Terminal 2: Set environment variable and run WPF app
cd apps\Wysg.Musm.Radium
$env:USE_API = "1"
dotnet run
```

Your app will now use the **API** for Hotkeys and Snippets! ??

---

## ?? Configuration Options

### Environment Variables

| Variable | Values | Default | Description |
|----------|--------|---------|-------------|
| `USE_API` | `0` or `1` | `0` | Enable (`1`) or disable (`0`) API mode |
| `RADIUM_API_URL` | URL | `http://localhost:5205` | API base URL |

### Examples

#### Local Testing (API + WPF both on localhost)
```powershell
$env:USE_API = "1"
$env:RADIUM_API_URL = "http://localhost:5205"
dotnet run
```

#### Testing with Azure Deployed API
```powershell
$env:USE_API = "1"
$env:RADIUM_API_URL = "https://api-wysg-musm.azurewebsites.net"
dotnet run
```

#### Production Direct DB (default)
```powershell
# No environment variables needed
dotnet run
```

---

## ?? What's Different Between Modes?

### Direct Database Mode (Default)

```
WPF App ¡æ AzureSqlHotkeyService ¡æ Azure SQL Database
         ¡æ AzureSqlSnippetService ¡æ Azure SQL Database
```

**Pros:**
- ? Direct access (no middleware)
- ? Proven, stable, currently in use
- ? Lower latency

**Cons:**
- ?? Database credentials in WPF app
- ?? Can't scale to web/mobile easily

---

### API Mode (New)

```
WPF App ¡æ ApiHotkeyServiceAdapter ¡æ RadiumApiClient 
         ¡æ ApiSnippetServiceAdapter ¡æ (HTTP)
                                    ¡æ API (Radium.Api)
                                    ¡æ Azure SQL Database
```

**Pros:**
- ? No database credentials in WPF
- ? Centralized business logic
- ? Ready for web/mobile clients
- ? Firebase authentication

**Cons:**
- ?? Extra network hop (slightly slower)
- ?? Requires API to be running

---

## ?? Testing Strategy

### Phase 1: Validate API Works (Week 1)

1. **Start both API and WPF**:
```powershell
# Terminal 1
cd apps\Wysg.Musm.Radium.Api
dotnet run

# Terminal 2
cd apps\Wysg.Musm.Radium
$env:USE_API = "1"
dotnet run
```

2. **Test Hotkeys**:
   - Create a hotkey
   - Edit a hotkey
   - Toggle active/inactive
   - Delete a hotkey

3. **Test Snippets**:
   - Same tests as Hotkeys

4. **Compare with Direct DB**:
   - Run without `USE_API=1`
   - Verify same data appears

---

### Phase 2: Performance Testing (Week 2)

Compare performance between modes:

```powershell
# Test 1: Direct DB
cd apps\Wysg.Musm.Radium
dotnet run
# Measure: Time to load hotkeys, time to save, etc.

# Test 2: API Mode
$env:USE_API = "1"
dotnet run
# Measure: Same operations
```

**Expected Results:**
- API mode: slightly slower (acceptable trade-off for security)
- Both modes: functionally identical

---

### Phase 3: Stability Testing (Week 3)

1. **Use API mode for daily work**
2. **Monitor for issues**:
   - Network errors?
   - Authentication problems?
   - Data consistency issues?

3. **Keep Direct DB as fallback**:
```powershell
# If API has issues, instantly revert:
Remove-Item env:USE_API
dotnet run
```

---

## ?? How to Tell Which Mode You're In

### Check Debug Output

When your app starts, look for these lines in Debug output:

**Direct DB Mode:**
```
[DI] API Mode: DISABLED (direct DB)
[DI] Using AzureSqlHotkeyService (direct DB mode)
[DI] Using AzureSqlSnippetService (direct DB mode)
```

**API Mode:**
```
[DI] API Mode: ENABLED (via API)
[DI] Using ApiHotkeyServiceAdapter (API mode)
[DI] Using ApiSnippetServiceAdapter (API mode)
```

### Check at Runtime

Add this to any ViewModel to check:

```csharp
var hotkeyService = ((App)Application.Current).Services.GetRequiredService<IHotkeyService>();
var isUsingApi = hotkeyService is Wysg.Musm.Radium.Services.Adapters.ApiHotkeyServiceAdapter;
Debug.WriteLine($"Using API: {isUsingApi}");
```

---

## ?? Troubleshooting

### Problem: API mode not working

**Check 1: Is API running?**
```powershell
Invoke-WebRequest -Uri "http://localhost:5205/health"
```
Expected: `200 OK`

**Check 2: Is environment variable set?**
```powershell
echo $env:USE_API
```
Expected: `1`

**Check 3: Is token set?**
- Login to your app
- Check Debug output for: `"Firebase token set in API client"`

---

### Problem: "Connection refused" errors

**Cause**: API is not running

**Solution**:
```powershell
cd apps\Wysg.Musm.Radium.Api
dotnet run
```

---

### Problem: Authentication errors

**Cause**: Firebase token not set or expired

**Solution**:
- Logout and login again
- Check `appsettings.Development.json` has `ValidateToken: false` for local testing

---

### Problem: Want to switch back to Direct DB

**Solution**:
```powershell
Remove-Item env:USE_API
# Restart your app
```

---

## ?? Decision Matrix: When to Use Which Mode?

| Scenario | Use Direct DB | Use API |
|----------|---------------|---------|
| **Daily production work** | ? Yes (stable) | ? Not yet |
| **Testing new API** | | ? Yes |
| **Development** | ? Yes | ? Yes (both!) |
| **After API is proven stable** | | ? Yes |
| **Web/mobile clients** | ? No | ? Yes (only option) |

---

## ?? Migration Timeline (Suggested)

### Week 1-2: Local API Testing
- ? Test API mode locally
- ? Verify functionality matches Direct DB
- ? Keep Direct DB as default

### Week 3-4: Daily Use (API Mode)
- ? Use `USE_API=1` for your daily work
- ? Monitor for issues
- ? Direct DB still available as fallback

### Week 5-6: Deploy API to Azure
- ? Deploy API to Azure App Service
- ? Test from WPF pointing to Azure API
- ? Keep Direct DB as fallback

### Week 7+: Production Cutover
- ? Make API mode the default
- ? Remove Direct DB code (optional)
- ? Full production on API

---

## ?? Pro Tips

### 1. Quick Toggle Script

Create `run-api-mode.ps1`:
```powershell
# Quick script to run in API mode
$env:USE_API = "1"
$env:RADIUM_API_URL = "http://localhost:5205"
cd apps\Wysg.Musm.Radium
dotnet run
```

### 2. VS Launch Settings

Add to `launchSettings.json`:
```json
{
  "profiles": {
    "Direct DB": {
      "commandName": "Project",
      "environmentVariables": {
        "USE_API": "0"
      }
    },
    "API Mode": {
      "commandName": "Project",
      "environmentVariables": {
        "USE_API": "1",
        "RADIUM_API_URL": "http://localhost:5205"
      }
    }
  }
}
```

### 3. Monitor Both Modes

Run side-by-side:
```powershell
# Terminal 1: Direct DB mode
cd apps\Wysg.Musm.Radium
dotnet run

# Terminal 2: API mode
cd apps\Wysg.Musm.Radium
$env:USE_API = "1"
dotnet run
```

Compare behavior in real-time!

---

## ? Summary

You now have:
- ? **Flexible architecture** - Switch between DB and API anytime
- ? **Zero risk** - Direct DB still works (default)
- ? **Easy testing** - One environment variable to toggle
- ? **Future-proof** - Ready for web/mobile when needed
- ? **No code changes needed** - ViewModels unchanged!

**Start testing the API today without any risk!** ??

---

## ?? Related Documentation

- `WPF_API_INTEGRATION_COMPLETE.md` - Full integration guide
- `API_QUICK_REFERENCE.md` - Quick API usage reference
- `FIREBASE_AZURE_GUIDE.md` - Firebase + Azure architecture
