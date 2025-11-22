# ?? Troubleshooting: "Phrases are not showing"

## Issue: Phrases not displaying in editor

You started the app with `USE_API=1` but don't see debug output or phrases.

---

## ? Solution: Check Debug Output Window

### **The Problem:**
Debug messages from `Debug.WriteLine()` go to **Debug Output**, NOT the PowerShell console!

### **How to View Debug Output:**

#### **Option 1: Run in Visual Studio** (RECOMMENDED)
1. Open project in Visual Studio
2. Set environment variable in `launchSettings.json`:
   ```json
   {
     "profiles": {
       "Wysg.Musm.Radium": {
         "commandName": "Project",
         "environmentVariables": {
           "USE_API": "1",
           "RADIUM_API_URL": "http://localhost:5205"
         }
       }
     }
   }
   ```
3. Press F5 to debug
4. Open **View ¡æ Output** window (Ctrl+Alt+O)
5. Select "Debug" from dropdown
6. **Watch for these lines:**
   ```
   [DI] API Mode: ENABLED (via API)
   [DI] Using ApiPhraseServiceAdapter (API mode)
   [DI] Using ApiSnomedMapService (API mode) - SNOMED via REST API
   ```

#### **Option 2: Use DebugView** (Free tool)
1. Download [DebugView](https://learn.microsoft.com/en-us/sysinternals/downloads/debugview)
2. Run DebugView as Administrator
3. Check "Capture ¡æ Capture Global Win32"
4. Start your WPF app: `$env:USE_API="1"; dotnet run`
5. **Watch DebugView** for diagnostic messages

#### **Option 3: Add Console Logging** (Temporary)
Add this to `App.xaml.cs` ConfigureServices():
```csharp
// Add at the START of ConfigureServices()
Console.WriteLine("====================================");
Console.WriteLine($"[CONFIG] USE_API = {Environment.GetEnvironmentVariable("USE_API")}");
Console.WriteLine($"[CONFIG] RADIUM_API_URL = {Environment.GetEnvironmentVariable("RADIUM_API_URL")}");
Console.WriteLine("====================================");
```

---

## ?? Diagnostic Steps

### **Step 1: Verify Environment Variable**
```powershell
# Check if USE_API is actually set
$env:USE_API
# Should output: 1

# If empty, set it again IN THE SAME TERMINAL:
$env:USE_API = "1"
$env:RADIUM_API_URL = "http://localhost:5205"

# Verify
echo "USE_API: $env:USE_API"
echo "RADIUM_API_URL: $env:RADIUM_API_URL"
```

### **Step 2: Check API is Running**
```powershell
# In ANOTHER terminal, test API:
Invoke-WebRequest http://localhost:5205/health

# Expected: HTTP 200 OK
# If fails: Start API first!
cd apps/Wysg.Musm.Radium.Api
dotnet run
```

### **Step 3: Check Login Success**
The app should:
1. Show splash/login window
2. Login with Google (Firebase)
3. Close splash window
4. Show main window
5. **Phrases should appear** (if they don't, continue to Step 4)

### **Step 4: Check for Errors**
Look for error messages in:
- **Visual Studio Output** window (Debug)
- **Visual Studio Error List** (Ctrl+\\, E)
- **Event Viewer** ¡æ Windows Logs ¡æ Application

---

## ?? Quick Test Script

Save this as `test-api-mode-verbose.ps1`:

```powershell
# test-api-mode-verbose.ps1
Write-Host "?? Testing API Mode with Verbose Output" -ForegroundColor Cyan

# Step 1: Check API
Write-Host "`n1. Checking if API is running..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest http://localhost:5205/health -TimeoutSec 2
    Write-Host "   ? API is running (HTTP $($response.StatusCode))" -ForegroundColor Green
} catch {
    Write-Host "   ? API is NOT running!" -ForegroundColor Red
    Write-Host "   Start API first: cd apps\Wysg.Musm.Radium.Api; dotnet run" -ForegroundColor Yellow
    exit 1
}

# Step 2: Set environment variables
Write-Host "`n2. Setting environment variables..." -ForegroundColor Yellow
$env:USE_API = "1"
$env:RADIUM_API_URL = "http://localhost:5205"
Write-Host "   USE_API = $env:USE_API" -ForegroundColor Gray
Write-Host "   RADIUM_API_URL = $env:RADIUM_API_URL" -ForegroundColor Gray

# Step 3: Start WPF with console output
Write-Host "`n3. Starting WPF app..." -ForegroundColor Yellow
Write-Host "   Watch for [DI] messages in Debug Output window!" -ForegroundColor Cyan
Write-Host "   (Open Visual Studio ¡æ View ¡æ Output ¡æ Select 'Debug')" -ForegroundColor Cyan

cd apps\Wysg.Musm.Radium
dotnet run
```

---

## ?? Common Issues

### **Issue 1: No Debug Output Visible**
**Symptom:** App starts but no `[DI]` messages

**Cause:** Debug output only goes to **Debug window**, not console

**Fix:**
- Run in Visual Studio and check Output window
- Or use DebugView tool
- Or add `Console.WriteLine()` temporarily

---

### **Issue 2: Phrases Not Colored**
**Symptom:** Phrases appear but all white/default color

**Diagnosis:**
1. Check if SNOMED mappings exist in database:
   ```sql
   SELECT COUNT(*) FROM radium.global_phrase_snomed;
   -- Should be > 0
   ```

2. Check if API is being called:
   - Open browser DevTools
   - Watch Network tab
   - Should see: `GET /api/snomed/mappings?phraseIds=...`

**Fix:**
- If no mappings: Use Global Phrases window to add SNOMED mappings
- If API not called: Check `USE_API` is set correctly

---

### **Issue 3: "Unauthorized" (401)**
**Symptom:** API returns 401 Unauthorized

**Cause:** Firebase token not set or expired

**Fix:** Check `SplashLoginViewModel.cs` after login:
```csharp
// Should have this line after successful login:
_apiClient.SetAuthToken(idToken);
```

---

### **Issue 4: Environment Variable Not Persisting**
**Symptom:** `USE_API` works in one terminal but not another

**Cause:** Environment variables are per-process, not global

**Fix:**
```powershell
# Set in SAME terminal where you run the app:
$env:USE_API = "1"
dotnet run

# Or use launchSettings.json (Visual Studio):
# apps/Wysg.Musm.Radium/Properties/launchSettings.json
{
  "profiles": {
    "Wysg.Musm.Radium": {
      "environmentVariables": {
        "USE_API": "1"
      }
    }
  }
}
```

---

## ? Verification Checklist

- [ ] API is running (`Invoke-WebRequest http://localhost:5205/health`)
- [ ] Environment variable is set (`echo $env:USE_API` shows "1")
- [ ] App starts and shows login window
- [ ] Login succeeds (splash closes, main window opens)
- [ ] Debug Output window shows `[DI] API Mode: ENABLED`
- [ ] Debug Output shows `[DI] Using ApiSnomedMapService`
- [ ] Phrases appear in editor
- [ ] Phrases are colored (if SNOMED mappings exist)

---

## ?? Expected Debug Output

When app starts correctly in API mode, you should see:

```
[DI] Registering RadiumApiClient with base URL: http://localhost:5205
[DI] API Mode: ENABLED (via API)
[DI] Using ApiPhraseServiceAdapter (API mode)
[DI] Using ApiHotkeyServiceAdapter (API mode)
[DI] Using ApiSnippetServiceAdapter (API mode)
[DI] Using ApiSnomedMapService (API mode) - SNOMED via REST API
[App][Preload] Start tenant=2
[SNOMED] Loading mappings for 150 phrases...
[SNOMED] Loaded 45 mappings
[App][Preload] Done
```

**If you don't see these lines**, the environment variable is not set correctly!

---

## ?? Quick Fix Summary

1. **Start API** (Terminal 1):
   ```powershell
   cd apps/Wysg.Musm.Radium.Api
   dotnet run
   ```

2. **Start WPF in Visual Studio** (F5), OR:
   ```powershell
   # Terminal 2 (same session!)
   cd apps/Wysg.Musm.Radium
   $env:USE_API = "1"
   dotnet run
   ```

3. **Check Debug Output** in Visual Studio:
   - **View ¡æ Output** (Ctrl+Alt+O)
   - Select **"Debug"** from dropdown
   - Look for `[DI] API Mode: ENABLED`

4. **Login** and verify phrases appear

---

## ?? Still Not Working?

### **Last Resort: Add Console Output**

Edit `App.xaml.cs` ConfigureServices():

```csharp
private void ConfigureServices(HostBuilderContext context, IServiceCollection services)
{
    // ADD THIS AT THE TOP:
    var useApi = Environment.GetEnvironmentVariable("USE_API") == "1";
    Console.WriteLine("??????????????????????????????????????????");
    Console.WriteLine($"? USE_API = {useApi}                     ?");
    Console.WriteLine($"? Mode: {(useApi ? "API" : "Direct DB")}  ?");
    Console.WriteLine("??????????????????????????????????????????");
    
    // ... rest of ConfigureServices
}
```

Now you'll see output in PowerShell console!

---

**Next Step:** Try running in **Visual Studio with F5** and check the **Output window**! ??
