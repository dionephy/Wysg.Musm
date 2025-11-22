# ?? SNOMED API - Quick Reference Card

## ? Status: READY TO TEST

---

## ?? Quick Start (Copy & Paste)

### **Start API + WPF in API Mode**
```powershell
# Terminal 1: Start API
cd apps/Wysg.Musm.Radium.Api
dotnet run

# Terminal 2: Start WPF (wait 5 seconds first!)
cd apps/Wysg.Musm.Radium
$env:USE_API = "1"
dotnet run
```

### **Start WPF in Direct DB Mode**
```powershell
cd apps/Wysg.Musm.Radium
dotnet run
# (No environment variable = direct DB)
```

---

## ?? Verify Mode

**Look for these lines in Debug Output:**

### **API Mode:**
```
[DI] API Mode: ENABLED (via API)
[DI] Using ApiSnomedMapService (API mode) - SNOMED via REST API
```

### **Direct DB Mode:**
```
[DI] API Mode: DISABLED (direct DB)
[DI] Using AzureSqlSnomedMapService (direct DB mode)
```

---

## ?? Test Phrase Coloring

| Phrase | Expected Color | Semantic Tag |
|--------|----------------|--------------|
| "heart" | ?? GREEN | body structure |
| "chest pain" | ?? BLUE | finding |
| "myocardial infarction" | ?? RED | disorder |
| "CT scan" | ?? YELLOW | procedure |

---

## ?? Testing Checklist

- [ ] API starts successfully
- [ ] WPF shows "API Mode: ENABLED" in Debug Output
- [ ] Login works
- [ ] Phrases are colored correctly
- [ ] Global Phrases window shows SNOMED mappings
- [ ] Can add new phrase with SNOMED mapping
- [ ] SNOMED Browser works

---

## ?? Environment Variables

```powershell
# Enable API mode
$env:USE_API = "1"
$env:RADIUM_API_URL = "http://localhost:5205"

# Disable API mode
Remove-Item Env:\USE_API
Remove-Item Env:\RADIUM_API_URL

# Check current mode
echo "USE_API: $env:USE_API"
echo "RADIUM_API_URL: $env:RADIUM_API_URL"
```

---

## ?? Documentation

| File | Purpose |
|------|---------|
| **`SNOMED_API_QUICK_TEST.md`** | ? START HERE - Quick testing guide |
| **`SNOMED_API_TEST_SCRIPTS.md`** | PowerShell test scripts |
| **`SNOMED_API_INTEGRATION_COMPLETE.md`** | Complete implementation summary |
| **`SNOMED_API_WPF_TESTING.md`** | Detailed troubleshooting guide |

**All files in:** `apps/Wysg.Musm.Radium/docs/`

---

## ?? Troubleshooting

### **Issue: API not responding**
```powershell
# Check if API is running
Invoke-WebRequest http://localhost:5205/health
```

### **Issue: Phrases not colored**
```
1. Check Debug Output for "Using ApiSnomedMapService"
2. Verify API is running
3. Check SNOMED mappings exist in database
```

### **Issue: Unauthorized (401)**
```
1. Check Firebase token is set after login
2. Look for SetAuthToken() call in SplashLoginViewModel
```

---

## ?? Quick Commands

```powershell
# API Mode
$env:USE_API="1"; cd apps\Wysg.Musm.Radium; dotnet run

# Direct DB Mode
Remove-Item Env:\USE_API; cd apps\Wysg.Musm.Radium; dotnet run

# Check API Health
Invoke-WebRequest http://localhost:5205/health

# Monitor API (separate terminal)
cd apps\Wysg.Musm.Radium.Api; dotnet run
```

---

## ?? Performance Expectations

| Operation | Direct DB | API Mode |
|-----------|-----------|----------|
| Load phrases | ~50ms | ~150ms |
| Load SNOMED tags | ~50ms | ~200ms |
| **Verdict** | Fastest | Usable |

**API mode is ~100-200ms slower but perfectly acceptable!**

---

## ? What Was Changed

**File:** `apps/Wysg.Musm.Radium/App.xaml.cs`

**Change:** Added SNOMED service registration with API/DB switching

**Lines Changed:** ~15 lines in `ConfigureServices()`

**Breaking Changes:** ? NONE (100% backward compatible)

---

## ?? You're Ready!

1. ? Code is ready
2. ? Documentation is ready
3. ? Test scripts are ready
4. ?? **Next:** Test it! (5 minutes)

---

**START HERE:** Open `SNOMED_API_QUICK_TEST.md` and follow the 5-minute test guide!

---

?? **Happy Testing!**
