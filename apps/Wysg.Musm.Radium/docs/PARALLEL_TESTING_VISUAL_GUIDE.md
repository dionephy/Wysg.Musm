# ??? Visual Guide: Parallel Testing

## ?? What You Have Now

```
忙式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式忖
弛         Your WPF App                      弛
弛         (Wysg.Musm.Radium)               弛
弛                                          弛
弛  忙式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式忖     弛
弛  弛    HotkeysViewModel             弛     弛
弛  弛    SnippetsViewModel            弛     弛
弛  弛    (No changes needed!)         弛     弛
弛  戌式式式式式式式式式式成式式式式式式式式式式式式式式式式式式式式式式戎     弛
弛             ⊿                            弛
弛  忙式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式忖     弛
弛  弛  IHotkeyService                弛     弛
弛  弛  ISnippetService               弛     弛
弛  弛  (Interface)                   弛     弛
弛  戌式式式式式式式式式式成式式式式式式式式式式式式式式式式式式式式式式戎     弛
弛             ⊿                            弛
弛      [Environment Variable]             弛
弛         USE_API = ?                     弛
弛             ⊿                            弛
弛   忙式式式式式式式式式扛式式式式式式式式式式忖               弛
弛   ⊿                    ⊿               弛
弛 USE_API=0           USE_API=1          弛
弛 (Default)           (New!)             弛
戌式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式戎
     ⊿                    ⊿
忙式式式式式式式式式式式式忖      忙式式式式式式式式式式式式式忖
弛 Direct DB  弛      弛  API Mode   弛
弛            弛      弛             弛
弛 AzureSql   弛      弛  Adapter    弛
弛 Services   弛      弛  ⊿          弛
弛            弛      弛  API Client 弛
弛            弛      弛  ⊿          弛
弛            弛      弛  HTTP       弛
戌式式式式式成式式式式式式戎      戌式式式式式式成式式式式式式戎
      ⊿                    ⊿
忙式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式忖
弛      Azure SQL Database          弛
弛      (musmdb)                    弛
戌式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式戎
```

---

## ?? How to Switch

### Mode 1: Direct Database (Default)

```powershell
# Just run - no setup needed!
cd apps\Wysg.Musm.Radium
dotnet run
```

```
? Uses: AzureSqlHotkeyService
? Uses: AzureSqlSnippetService
? Direct connection to database
? Current production method
```

---

### Mode 2: API Mode (Testing)

```powershell
# Terminal 1: Start API
cd apps\Wysg.Musm.Radium.Api
dotnet run
# Wait for: "Now listening on: http://localhost:5205"

# Terminal 2: Run WPF with API flag
cd apps\Wysg.Musm.Radium
$env:USE_API = "1"
dotnet run
```

```
? Uses: ApiHotkeyServiceAdapter
? Uses: ApiSnippetServiceAdapter
? Calls API via HTTP
? New, testing method
```

---

## ?? How to Verify Current Mode

### Look in Debug Output

#### Direct DB Mode:
```
[DI] API Mode: DISABLED (direct DB)
[DI] Using AzureSqlHotkeyService (direct DB mode)
[DI] Using AzureSqlSnippetService (direct DB mode)
```

#### API Mode:
```
[DI] API Mode: ENABLED (via API)
[DI] Registering RadiumApiClient with base URL: http://localhost:5205
[DI] Using ApiHotkeyServiceAdapter (API mode)
[DI] Using ApiSnippetServiceAdapter (API mode)
```

---

## ?? Comparison

| Aspect | Direct DB | API Mode |
|--------|-----------|----------|
| **Setup** | None | Start API first |
| **Speed** | Fast | Slightly slower |
| **Security** | DB creds in app | No creds in app |
| **Risk** | Low (proven) | Low (can revert) |
| **Future** | Limited | Web/mobile ready |

---

## ?? Quick Test

### Step 1: Test Direct DB
```powershell
cd apps\Wysg.Musm.Radium
dotnet run
```
- Login
- Create a hotkey: `test1` ⊥ `Direct DB test`
- Note the hotkey ID

### Step 2: Test API Mode
```powershell
# Keep app running or restart
cd apps\Wysg.Musm.Radium.Api
dotnet run
# New terminal:
cd apps\Wysg.Musm.Radium
$env:USE_API = "1"
dotnet run
```
- Login
- See `test1` hotkey (same data!)
- Create another: `test2` ⊥ `API test`

### Step 3: Verify
```powershell
# Switch back to Direct DB
Remove-Item env:USE_API
# Restart app
```
- See both `test1` AND `test2`
- **Same database, different access paths!** ?

---

## ? Quick Commands

### Check environment:
```powershell
echo $env:USE_API  # Should be "1" for API mode
```

### Enable API mode:
```powershell
$env:USE_API = "1"
```

### Disable API mode (back to Direct DB):
```powershell
Remove-Item env:USE_API
```

### Check if API is running:
```powershell
Invoke-WebRequest -Uri "http://localhost:5205/health"
```

---

## ?? Decision Tree

```
忙式 Need to test API? 式忖
弛                     弛
弛    YES          NO  弛
弛     ⊿            ⊿  弛
弛  USE_API=1   USE_API=0
弛   (API)      (Direct DB)
弛     ⊿            ⊿
弛  Start API   Just run
弛  first!      
弛     ⊿
弛  Test it!
弛     ⊿
弛  Works? 式忖
弛          弛
弛   YES    NO
弛    ⊿     ⊿
弛  Keep   Switch
弛  using  back!
弛   ⊿
弛  $env:USE_API = ""
戌式式式式式式式式式式式式式式式式式式式式式式戎
```

---

## ?? Migration Path

```
Week 1: Test API locally
        ⊿
Week 2: Use API daily
        ⊿
Week 3: Monitor stability
        ⊿
Week 4: Deploy API to Azure
        ⊿
Week 5+: Production on API
```

---

## ?? Summary

**You have TWO paths, ONE app!**

- ? Direct DB (default, safe)
- ? API mode (new, testable)
- ? Switch anytime
- ? Zero risk
- ? Production ready

**Start testing today!** ??

---

## ?? Full Documentation

- `PARALLEL_TESTING_SUMMARY.md` - Complete implementation guide
- `PARALLEL_TESTING_GUIDE.md` - Detailed usage
- `PARALLEL_TESTING_QUICK_REF.md` - Quick reference
