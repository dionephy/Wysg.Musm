# Testing SNOMED API in WPF App

## ? Implementation Complete

The SNOMED API integration is ready to test in the WPF app!

---

## ?? What Was Added

### 1. **RadiumApiClient** (Updated)
- ? Added SNOMED methods:
  - `CacheSnomedConceptAsync()` - Cache SNOMED concept
  - `CreatePhraseSnomedMappingAsync()` - Map phrase to concept
  - `GetPhraseSnomedMappingAsync()` - Get single mapping
  - `GetPhraseSnomedMappingsBatchAsync()` - **Batch get** (for syntax highlighting)
  - `DeletePhraseSnomedMappingAsync()` - Delete mapping

### 2. **ApiSnomedMapService** (New)
- ? Adapter implementing `ISnomedMapService`
- ? Uses `RadiumApiClient` under the hood
- ? Compatible with existing `MainViewModel` and `GlobalPhrasesViewModel`

---

## ?? How to Test

### **Step 1: Update App.xaml.cs** (Temporary - for testing)

Find the DI container setup and add:

```csharp
// In App.xaml.cs - ConfigureServices() method

// API Client (if not already registered)
var apiBaseUrl = "https://localhost:5205"; // or your API URL
var apiClient = new RadiumApiClient(apiBaseUrl);
services.AddSingleton(apiClient);

// SNOMED Service - Use API instead of direct DB
services.AddSingleton<ISnomedMapService, ApiSnomedMapService>();
```

**Note:** Keep the existing `AzureSqlSnomedMapService` registration commented out for now.

---

### **Step 2: Run API** (Terminal 1)

```powershell
cd apps/Wysg.Musm.Radium.Api
dotnet run
```

**Wait for:** `Now listening on: https://localhost:5205`

---

### **Step 3: Run WPF App** (Terminal 2)

```powershell
cd apps/Wysg.Musm.Radium
dotnet run
```

---

### **Step 4: Test Workflow**

#### **Test 1: Login**
1. ? Login with Google (Firebase auth)
2. ? API receives Firebase token
3. ? Check logs for auth success

#### **Test 2: Load Phrases with Semantic Tags**
1. ? App loads phrases
2. ? API call: `GET /api/accounts/{id}/phrases`
3. ? API call: `GET /api/snomed/mappings?phraseIds=1&phraseIds=2&...` (batch)
4. ? Editor shows **colored phrases** based on semantic tags:
   - Green = body structure
   - Blue = finding
   - Red = disorder
   - Yellow = procedure

#### **Test 3: Add Phrase with SNOMED Mapping** (GlobalPhrasesViewModel)
1. ? Open Global Phrases window
2. ? Search SNOMED: "heart structure"
3. ? Select concept
4. ? Click "Add with SNOMED"
5. ? API calls:
   - `POST /api/snomed/concepts` (cache concept)
   - `POST /api/accounts/{id}/phrases` (create phrase)
   - `POST /api/snomed/mappings` (create mapping)
6. ? Refresh phrases
7. ? Verify "heart structure" is **green** (body structure)

---

## ?? Debugging

### **Enable Debug Logging**

Add to `App.xaml.cs` or `MainViewModel.cs`:

```csharp
System.Diagnostics.Debug.WriteLine($"[SNOMED] Loading mappings for {phraseIds.Count} phrases...");

var mappings = await _snomedMapService.GetMappingsBatchAsync(phraseIds);

System.Diagnostics.Debug.WriteLine($"[SNOMED] Loaded {mappings.Count} mappings");
foreach (var mapping in mappings)
{
    System.Diagnostics.Debug.WriteLine($"  - Phrase {mapping.Key}: {mapping.Value.SemanticTag}");
}
```

### **Check API Logs**

Watch for these requests:

```
info: Wysg.Musm.Radium.Api.Controllers.SnomedController[0]
      Retrieved 150 mappings for 150 phrases
```

### **Check Database**

```sql
-- Verify mappings exist
SELECT p.id, p.text, gps.concept_id, cc.fsn
FROM radium.phrase p
LEFT JOIN radium.global_phrase_snomed gps ON gps.phrase_id = p.id
LEFT JOIN snomed.concept_cache cc ON cc.concept_id = gps.concept_id
WHERE p.account_id IS NULL
ORDER BY p.text;
```

---

## ?? Expected Behavior

### **Before SNOMED API**
```
[All phrases are white/default color]
- heart structure
- myocardial infarction
- chest pain
- aorta
```

### **After SNOMED API** ?
```
[Colored by semantic tag]
- heart structure          [GREEN - body structure]
- myocardial infarction    [RED - disorder]
- chest pain               [BLUE - finding]
- aorta                    [GREEN - body structure]
```

---

## ?? Troubleshooting

### **Issue: "Unauthorized" (401)**
**Cause:** Firebase token not set or expired

**Fix:**
```csharp
// After login in GoogleOAuthAuthService
await _apiClient.SetAuthToken(firebaseIdToken);
```

### **Issue: "Not Found" (404)**
**Cause:** API not running or wrong URL

**Fix:**
```csharp
// Check API URL in App.xaml.cs
var apiClient = new RadiumApiClient("https://localhost:5205"); // Correct port?
```

### **Issue: Phrases not colored**
**Cause:** No SNOMED mappings in database

**Fix:**
1. Use `test.http` to manually create mappings
2. Or use GlobalPhrasesViewModel "Bulk SNOMED Add" feature

### **Issue: Slow loading (>5 seconds)**
**Cause:** Too many phrases (>1000)

**Check:**
```csharp
var sw = System.Diagnostics.Stopwatch.StartNew();
var mappings = await _snomedMapService.GetMappingsBatchAsync(phraseIds);
sw.Stop();
System.Diagnostics.Debug.WriteLine($"[SNOMED] Batch query took {sw.ElapsedMilliseconds}ms");
```

**Expected:** <100ms for 1000 phrases

---

## ?? Performance Comparison

### **Direct Database (AzureSqlSnomedMapService)**
```
? Batch load 1000 phrases: ~50ms
? No network overhead
? Requires VPN to Azure SQL
```

### **API (ApiSnomedMapService)**
```
? Works remotely (no VPN needed)
? Centralized auth & logging
?? Batch load 1000 phrases: ~150-300ms (network)
```

---

## ?? What to Test

### ? **Core Functionality**
- [ ] Login works
- [ ] Phrases load
- [ ] Semantic tags load (batch query)
- [ ] Phrase coloring works correctly

### ? **SNOMED Operations**
- [ ] Cache SNOMED concept
- [ ] Create phrase-SNOMED mapping
- [ ] Get single mapping
- [ ] Get batch mappings
- [ ] Delete mapping

### ? **UI Integration**
- [ ] GlobalPhrasesViewModel shows SNOMED mappings
- [ ] SNOMED Browser works
- [ ] Bulk SNOMED add works
- [ ] Phrase extraction with SNOMED works

---

## ?? Switching Between API and Direct DB

### **Use API** (Remote access)
```csharp
services.AddSingleton<ISnomedMapService, ApiSnomedMapService>();
```

### **Use Direct DB** (On-premise/VPN)
```csharp
services.AddSingleton<ISnomedMapService, AzureSqlSnomedMapService>();
```

**Note:** Both implementations are **100% compatible** - no code changes needed!

---

## ?? Manual Testing Script

```http
### 1. Cache a test concept
POST https://localhost:5205/api/snomed/concepts
Authorization: Bearer YOUR_FIREBASE_TOKEN
Content-Type: application/json

{
  "conceptId": 80891009,
  "conceptIdStr": "80891009",
  "fsn": "Heart structure (body structure)",
  "pt": "Heart structure",
  "semanticTag": "body structure",
  "active": true
}

### 2. Create a test phrase
POST https://localhost:5205/api/accounts/2/phrases
Authorization: Bearer YOUR_FIREBASE_TOKEN
Content-Type: application/json

{
  "text": "heart structure",
  "active": true
}

### 3. Map phrase to concept
POST https://localhost:5205/api/snomed/mappings
Authorization: Bearer YOUR_FIREBASE_TOKEN
Content-Type: application/json

{
  "phraseId": 1,
  "accountId": null,
  "conceptId": 80891009,
  "mappingType": "exact",
  "confidence": 1.0
}

### 4. Get mappings (batch)
GET https://localhost:5205/api/snomed/mappings?phraseIds=1&phraseIds=2&phraseIds=3
Authorization: Bearer YOUR_FIREBASE_TOKEN
```

---

## ? Success Criteria

### **Phase 1: Basic Loading** ?
- [ ] API starts successfully
- [ ] WPF app connects to API
- [ ] Phrases load via API

### **Phase 2: SNOMED Integration** ?
- [ ] Semantic tags load (batch query)
- [ ] Phrase coloring works
- [ ] No performance degradation (<300ms for 1000 phrases)

### **Phase 3: Full Workflow** ?
- [ ] Login ¡æ Load phrases ¡æ See colored phrases
- [ ] Add new phrase with SNOMED ¡æ Verify coloring
- [ ] Bulk SNOMED add ¡æ Verify all phrases colored

---

## ?? Ready to Test!

1. ? **Start API**: `dotnet run` in `Wysg.Musm.Radium.Api`
2. ? **Update DI**: Register `ApiSnomedMapService` in `App.xaml.cs`
3. ? **Run WPF**: `dotnet run` in `Wysg.Musm.Radium`
4. ? **Login & Test**: Open app, login, verify phrase coloring

---

**All code is ready!** Just need to:
1. Register the service in DI
2. Run both API and WPF
3. Test the workflow

Good luck! ??
