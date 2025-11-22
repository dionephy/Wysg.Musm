# ?? SNOMED API Testing Guide - Quick Start

## ? Setup Complete!

Your `App.xaml.cs` is now configured for **easy API/DB switching** via environment variable.

---

## ?? How to Test

### **Step 1: Choose Your Mode**

#### **Option A: API Mode** (Test SNOMED API)
```powershell
# Set environment variable BEFORE running WPF
$env:USE_API = "1"
$env:RADIUM_API_URL = "http://localhost:5205"

# Start API first (Terminal 1)
cd apps/Wysg.Musm.Radium.Api
dotnet run

# Then start WPF (Terminal 2)
cd apps/Wysg.Musm.Radium
dotnet run
```

**Expected Console Output:**
```
[DI] Registering RadiumApiClient with base URL: http://localhost:5205
[DI] API Mode: ENABLED (via API)
[DI] Using ApiPhraseServiceAdapter (API mode)
[DI] Using ApiHotkeyServiceAdapter (API mode)
[DI] Using ApiSnippetServiceAdapter (API mode)
[DI] Using ApiSnomedMapService (API mode) - SNOMED via REST API
```

#### **Option B: Direct DB Mode** (Current behavior)
```powershell
# No environment variable = direct DB access
cd apps/Wysg.Musm.Radium
dotnet run
```

**Expected Console Output:**
```
[DI] API Mode: DISABLED (direct DB)
[DI] Using AzureSqlPhraseService (detected Azure SQL connection string)
[DI] Using AzureSqlHotkeyService (direct DB mode)
[DI] Using AzureSqlSnippetService (direct DB mode)
[DI] Using AzureSqlSnomedMapService (direct DB mode)
```

---

### **Step 2: Verify SNOMED API Integration**

#### **Test 1: Login and Load Phrases** ?
1. Start API + WPF in API mode
2. Login with Google
3. **Watch Debug Output** for:
```
[DI] Using ApiSnomedMapService (API mode) - SNOMED via REST API
[SNOMED] Loading mappings for 150 phrases...
[SNOMED] Loaded 45 mappings
```

4. **Check Editor** - Phrases should be colored:
   - ?? Green = body structure ("heart", "aorta")
   - ?? Blue = finding ("chest pain", "headache")
   - ?? Red = disorder ("myocardial infarction")
   - ?? Yellow = procedure ("CT scan")

#### **Test 2: Global Phrases Window** ?
1. Open **Tools ¡æ Global Phrases** (or Settings ¡æ Global Phrases)
2. Verify SNOMED mappings appear in the "SNOMED Mapping" column
3. Try searching for a concept in "Bulk SNOMED" tab
4. Select a concept and click "Add with SNOMED"
5. **Verify API calls** in Debug Output:
```
POST http://localhost:5205/api/snomed/concepts (cache concept)
POST http://localhost:5205/api/accounts/2/phrases (create phrase)
POST http://localhost:5205/api/snomed/mappings (create mapping)
```

#### **Test 3: SNOMED Browser** ?
1. Open **Tools ¡æ SNOMED Browser**
2. Select a domain (e.g., "body structure")
3. Navigate pages
4. Click "Add" button on a term
5. **Verify API calls**:
```
POST http://localhost:5205/api/snomed/concepts
POST http://localhost:5205/api/accounts/2/phrases
POST http://localhost:5205/api/snomed/mappings
```

---

## ?? Debug Output Reference

### **Successful API Integration**
```
[DI] Registering RadiumApiClient with base URL: http://localhost:5205
[DI] API Mode: ENABLED (via API)
[DI] Using ApiPhraseServiceAdapter (API mode)
[DI] Using ApiHotkeyServiceAdapter (API mode)
[DI] Using ApiSnippetServiceAdapter (API mode)
[DI] Using ApiSnomedMapService (API mode) - SNOMED via REST API
[App][Preload] Start tenant=2
[SNOMED] Batch query took 127ms
[App][Preload] Done
```

### **API Connection Issues**
```
[ApiSnomedMapService] Error mapping phrase 123: No connection could be made...
```
**Fix:** Make sure API is running (`dotnet run` in Radium.Api)

### **Authentication Issues**
```
HTTP 401 Unauthorized
```
**Fix:** Check Firebase token is set in `RadiumApiClient` after login

---

## ?? API vs Direct DB Comparison

| Feature | Direct DB | API Mode | Notes |
|---------|-----------|----------|-------|
| **Phrases** | ? Fast (direct SQL) | ? Works via API | ~50ms overhead |
| **SNOMED Mappings** | ? Fast (batch query) | ? Works via API | ~150-300ms |
| **Phrase Coloring** | ? Works | ? Works | Semantic tags load on startup |
| **Bulk SNOMED Add** | ? Works | ? Works | Multiple API calls |
| **SNOMED Browser** | ? Works | ? Works | Pagination works |
| **VPN Required?** | ?? YES | ? NO | API accessible remotely |

---

## ?? Quick Test Scenarios

### **Scenario 1: Verify Phrase Coloring** (2 min)
```
1. Start API mode (USE_API=1)
2. Login
3. Type "heart" in editor
4. Verify it appears GREEN (body structure)
5. Type "chest pain"
6. Verify it appears BLUE (finding)
```

### **Scenario 2: Add New SNOMED Phrase** (5 min)
```
1. Open Global Phrases window
2. Click "Bulk SNOMED" tab
3. Search: "myocardial infarction"
4. Select first result
5. Click "Add Selected with SNOMED"
6. Verify phrase added
7. Verify phrase is RED in editor (disorder)
```

### **Scenario 3: SNOMED Browser Navigation** (3 min)
```
1. Open SNOMED Browser
2. Select "procedure" domain
3. Click "Next Page" 3 times
4. Click "Add" on any term
5. Verify phrase created
6. Verify phrase colored correctly in editor
```

---

## ?? Troubleshooting

### **Issue: Phrases not colored**
**Symptom:** All phrases are white/default color

**Diagnosis:**
```csharp
// Add to MainViewModel.LoadPhrasesAsync():
Debug.WriteLine($"[SNOMED] PhraseSemanticTags count: {PhraseSemanticTags.Count}");
foreach (var tag in PhraseSemanticTags.Take(5))
{
    Debug.WriteLine($"  - {tag.Key}: {tag.Value}");
}
```

**Fix:**
1. Check API is running
2. Check `USE_API=1` is set
3. Verify SNOMED mappings exist in database
4. Check Debug Output for "Using ApiSnomedMapService"

---

### **Issue: "Unauthorized" (401)**
**Symptom:** API returns HTTP 401

**Fix:**
```csharp
// Verify token is set after login in SplashLoginViewModel.cs
Debug.WriteLine($"[Auth] Setting API token: {idToken.Substring(0, 20)}...");
_apiClient.SetAuthToken(idToken);
```

---

### **Issue: Slow loading (>5 seconds)**
**Symptom:** Editor takes forever to show colored phrases

**Diagnosis:**
```csharp
var sw = System.Diagnostics.Stopwatch.StartNew();
var mappings = await _snomedMapService.GetMappingsBatchAsync(phraseIds);
sw.Stop();
Debug.WriteLine($"[SNOMED] Batch query took {sw.ElapsedMilliseconds}ms");
```

**Expected:** <300ms for 1000 phrases

**Fix:** If >1000ms, check network or API performance

---

## ?? Complete Test Script

```powershell
# Terminal 1: Start API
cd apps/Wysg.Musm.Radium.Api
dotnet run
# Wait for: "Now listening on: http://localhost:5205"

# Terminal 2: Start WPF in API mode
cd apps/Wysg.Musm.Radium
$env:USE_API = "1"
$env:RADIUM_API_URL = "http://localhost:5205"
dotnet run

# WPF App:
# 1. Login with Google
# 2. Verify phrases are colored
# 3. Open Global Phrases ¡æ Verify SNOMED column
# 4. Add new phrase with SNOMED
# 5. Verify coloring updates

# Terminal 3: Monitor API logs
# Watch for:
# - POST /api/snomed/concepts
# - POST /api/snomed/mappings
# - GET /api/snomed/mappings?phraseIds=...
```

---

## ?? Environment Variables

```powershell
# API Mode
$env:USE_API = "1"                                    # Enable API mode
$env:RADIUM_API_URL = "http://localhost:5205"        # API base URL

# Disable for troubleshooting
$env:RAD_DISABLE_PHRASE_PRELOAD = "1"                # Skip phrase preload
$env:RAD_TRACE_PG = "1"                              # Enable SQL tracing

# To clear:
Remove-Item Env:\USE_API
Remove-Item Env:\RADIUM_API_URL
```

---

## ? Success Checklist

After testing, verify:

- [ ] API starts successfully (`dotnet run`)
- [ ] WPF starts in API mode (Debug Output shows "API Mode: ENABLED")
- [ ] Login works (Firebase auth succeeds)
- [ ] Phrases load
- [ ] Phrases are colored correctly (Green/Blue/Red/Yellow)
- [ ] Global Phrases window shows SNOMED mappings
- [ ] Bulk SNOMED add works
- [ ] SNOMED Browser works
- [ ] Performance is acceptable (<300ms for mappings)
- [ ] No errors in Debug Output
- [ ] API logs show successful requests

---

## ?? You're Ready!

Your WPF app is now fully integrated with the SNOMED API!

**Next Steps:**
1. ? Test locally (this guide)
2. ? Deploy API to Azure
3. ? Update `RADIUM_API_URL` to production
4. ? Test with production API

---

## ?? Related Docs

- **`SNOMED_API_WPF_TESTING.md`** - Detailed testing guide
- **`SNOMED_API_COMPLETE.md`** - API implementation reference
- **`API_QUICK_REFERENCE.md`** - General API usage

---

**Happy Testing!** ??

If you encounter issues, check Debug Output first - it shows exactly which services are being used!
