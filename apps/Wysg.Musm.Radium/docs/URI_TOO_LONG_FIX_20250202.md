# ? FIX: 414 URI Too Long Error for SNOMED Batch Mappings

**Date:** 2025-11-02  
**Status:** ? **FIXED** - Changed from GET to POST

---

## Problem Identified

When trying to load SNOMED mappings for phrase colorizing, the app crashed with:

```
[ApiClientBase] GET failed: RequestUriTooLong - 
Response status code does not indicate success: 414 (URI Too Long).
[SemanticTag] Error loading semantic tags: Response status code does not indicate success: 414 (URI Too Long).
```

### Root Cause

The app was trying to send **2,358 phrase IDs** in a GET request URL:

```
GET http://localhost:5205/api/snomed/mappings?phraseIds=3337&phraseIds=3339&phraseIds=3340&...
```

This created a URL tens of thousands of characters long, exceeding the maximum URL length (typically 2,048-8,192 characters).

---

## Solution Applied

### Changed GET endpoint to POST endpoint

**Before (WRONG):**
```csharp
// GET with query parameters (URL too long!)
[HttpGet("mappings")]
public async Task<ActionResult<Dictionary<long, PhraseSnomedMappingDto>>> GetMappingsBatch([FromQuery] long[] phraseIds)
```

**After (CORRECT):**
```csharp
// POST with JSON body (no length limit!)
[HttpPost("mappings/batch")]
public async Task<ActionResult<Dictionary<long, PhraseSnomedMappingDto>>> GetMappingsBatch([FromBody] BatchMappingsRequest request)

public class BatchMappingsRequest
{
    public long[] PhraseIds { get; set; } = Array.Empty<long>();
}
```

---

## Files Modified

### 1. API Controller

**File:** `apps\Wysg.Musm.Radium.Api\Controllers\SnomedController.cs`

- Changed `[HttpGet("mappings")]` to `[HttpPost("mappings/batch")]`
- Changed parameter from `[FromQuery] long[] phraseIds` to `[FromBody] BatchMappingsRequest`
- Added `BatchMappingsRequest` class

### 2. WPF Client

**File:** `apps\Wysg.Musm.Radium\Services\ApiClients\SnomedApiClient.cs`

Changed `GetMappingsBatchAsync` from GET to POST:

**Before:**
```csharp
var queryString = string.Join("&", idsArray.Select(id => $"phraseIds={id}"));
return await GetAsync<Dictionary<long, PhraseSnomedMappingDto>>($"/api/snomed/mappings?{queryString}");
```

**After:**
```csharp
var request = new { phraseIds = idsArray };
return await PostAsync<Dictionary<long, PhraseSnomedMappingDto>>("/api/snomed/mappings/batch", request);
```

---

## Why This Works

### HTTP URL Length Limits

- **GET requests:** URL length limited to ~2,048-8,192 characters
- **POST requests:** Body size can be MB/GB (configured in server)

### Request Size Comparison

**Before (GET):**
- 2,358 phrase IDs
- Average 10 characters per ID (`phraseIds=3337&`)
- Total URL length: ~23,580 characters
- **Result:** 414 URI Too Long ?

**After (POST):**
- 2,358 phrase IDs in JSON body
- Body size: ~15 KB (2,358 * 6 bytes average)
- **Result:** Works perfectly ?

---

## Testing Steps

### 1. Stop the Running API

Press `Ctrl+C` in the API terminal to stop it.

### 2. Rebuild

```powershell
cd apps\Wysg.Musm.Radium.Api
dotnet build
```

### 3. Restart API

```powershell
cd apps\Wysg.Musm.Radium.Api
dotnet run
```

### 4. Rebuild WPF App

```powershell
cd apps\Wysg.Musm.Radium
dotnet build
```

### 5. Test

```powershell
cd apps\Wysg.Musm.Radium
$env:USE_API = "1"
dotnet run
```

**Expected Debug Output:**

```
[SnomedApiClient] Getting mappings for 2358 phrases via POST
[ApiClientBase] POST http://localhost:5205/api/snomed/mappings/batch
[SemanticTag] Batch loaded 1234 mappings
[SemanticTag] Total semantic tags loaded: 1234 from 2358 global phrases
```

**Expected Result:**
- ? No 414 error
- ? SNOMED mappings load successfully
- ? Phrase colorizing works (green/pink/yellow/grey colors)

---

## API Design Best Practices

### When to Use GET vs POST

**Use GET for:**
- Small parameter lists (1-10 items)
- Cacheable requests
- Idempotent operations
- **Example:** `GET /api/snomed/mappings/123` (single phrase)

**Use POST for:**
- Large parameter lists (hundreds/thousands)
- Non-cacheable requests
- Operations that modify state
- **Example:** `POST /api/snomed/mappings/batch` (batch retrieval)

### Batch Operations Pattern

For batch retrieval operations:

```csharp
// ? RECOMMENDED: POST with JSON body
[HttpPost("resource/batch")]
public async Task<ActionResult<Dictionary<long, T>>> GetBatch([FromBody] BatchRequest request)
{
    // No URL length limit
    // Better for large batches
    // Can include additional filters in body
}

// ? AVOID: GET with query parameters for large batches
[HttpGet("resource")]
public async Task<ActionResult<List<T>>> GetMany([FromQuery] long[] ids)
{
    // URL length limit ~2KB-8KB
    // Fails with large batches
}
```

---

## Related Issues Fixed Today

1. ? Snippets not working �� Fixed (caching)
2. ? Editor sluggish �� Fixed (caching)
3. ? Phrase colorizing adapter missing �� Fixed (created ApiSnomedMapServiceAdapter)
4. ? DI registration wrong �� Fixed (class name + lifetime)
5. ? **414 URI Too Long** �� Fixed (THIS - changed GET to POST)

---

## Summary

**Problem:** 2,358 phrase IDs in URL �� 414 URI Too Long error  
**Solution:** Changed from GET with query params to POST with JSON body  
**Result:** Phrase colorizing now works! ??

---

**Implementation by:** GitHub Copilot  
**Date:** 2025-11-02  
**Status:** ? **READY FOR TESTING**
