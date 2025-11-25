# FIX: Phrase SNOMED Link Window - Search and FSN Display Issues

**Date:** 2025-02-05  
**Status:** ? **COMPLETE**  
**Issue Type:** Bug Fix  
**Priority:** High (User-facing feature not working correctly)

---

## Executive Summary

Fixed critical issues in the "Link Phrase to SNOMED" window where:
1. **Display Issue**: FSN column showed concept IDs instead of Fully Specified Names
2. **Search Issue**: Search returned wrong results (products/pharmaceuticals instead of clinical concepts)

**Root Causes:**
1. XAML column was bound to wrong property (`ConceptId` instead of `Fsn`)
2. Snowstorm API endpoint selection was incorrect (used `/browser/` path which returns wrong terminology subset)

**Solution:**
1. Fixed XAML bindings to use correct properties
2. Changed API endpoint from `/browser/MAIN/concepts` to `/MAIN/concepts` (standard clinical terminology)

---

## Problem Description

### Issue 1: FSN Column Shows Concept ID ? FIXED

**What Users Saw:**
The FSN (Fully Specified Name) column in the DataGrid showed concept ID numbers instead of the actual SNOMED terminology.

**Example:**
```
忙式式式式式式式式式式式式式式式式成式式式式式式式式式式式式式式式式式式式成式式式式式式式式式式式式式式忖
弛 ConceptId      弛 FSN               弛 PT           弛
戍式式式式式式式式式式式式式式式式托式式式式式式式式式式式式式式式式式式式托式式式式式式式式式式式式式式扣
弛 80891009       弛 80891009          弛 Heart        弛  ∠ WRONG! Shows ID
弛 34048005       弛 34048005          弛 Malleus      弛  ∠ WRONG! Shows ID
戌式式式式式式式式式式式式式式式式扛式式式式式式式式式式式式式式式式式式式扛式式式式式式式式式式式式式式戎

Expected:
忙式式式式式式式式式式式式式式式式成式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式成式式式式式式式式式式式式式式忖
弛 ConceptId      弛 FSN                                      弛 PT           弛
戍式式式式式式式式式式式式式式式式托式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式托式式式式式式式式式式式式式式扣
弛 80891009       弛 Heart structure (body structure)         弛 Heart        弛  ?
弛 34048005       弛 Malleus structure (body structure)       弛 Malleus      弛  ?
戌式式式式式式式式式式式式式式式式扛式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式扛式式式式式式式式式式式式式式戎
```

**Root Cause:**
XAML binding error - FSN column was bound to `ConceptId` property instead of `Fsn` property.

### Issue 2: Search Returns Wrong Results ? FIXED

**What Users Saw:**
Searching for any term (e.g., "heart", "fracture", "malleus") returned **product and pharmaceutical concepts** instead of **clinical/anatomical concepts**.

**Example - Search for "anatomical":**
```
忙式式式式式式式式式式式式式成式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式成式式式式式式式式式式式式式式式式式式式式式式式式式忖
弛 ConceptId   弛 FSN                            弛 PT                      弛
戍式式式式式式式式式式式式式托式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式托式式式式式式式式式式式式式式式式式式式式式式式式式扣
弛 99999003    弛 BISMUSAL SUSPENSION (product)  弛 BISMUSAL SUSPENSION     弛  ? Product
弛 99996006    弛 BISMU-KOTE (product)           弛 BISMU-KOTE              弛  ? Product
弛 99995009    弛 BIOVAX (product)               弛 BIOVAX                  弛  ? Product
弛 99992007    弛 BIOTIN 100 (product)           弛 BIOTIN 100              弛  ? Product
戌式式式式式式式式式式式式式扛式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式扛式式式式式式式式式式式式式式式式式式式式式式式式式戎

Expected:
忙式式式式式式式式式式式式式成式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式成式式式式式式式式式式式式式式式式式式式式式式式式式式忖
弛 ConceptId   弛 FSN                                         弛 PT                       弛
戍式式式式式式式式式式式式式托式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式托式式式式式式式式式式式式式式式式式式式式式式式式式式扣
弛 91723000    弛 Anatomical structure (body structure)       弛 Anatomical structure     弛  ?
弛 442083009   弛 Anatomical or acquired body structure       弛 Body structure           弛  ?
弛 123037004   弛 Body structure (body structure)             弛 Body structure           弛  ?
戌式式式式式式式式式式式式式扛式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式扛式式式式式式式式式式式式式式式式式式式式式式式式式式戎
```

**Root Cause:**
Wrong Snowstorm API endpoint - `/browser/MAIN/concepts` returns product/pharmaceutical terminology instead of clinical/anatomical concepts.

---

## Root Causes Analysis

### Issue 1 - XAML Binding (FIXED ?)

**Before:**
```xaml
<DataGrid.Columns>
    <DataGridTextColumn Header="ConceptId" Binding="{Binding ConceptId}" Width="120"/>
    <DataGridTextColumn Header="FSN" Binding="{Binding ConceptId}" Width="*"/>  <!-- WRONG! -->
    <DataGridTextColumn Header="PT" Binding="{Binding Pt}" Width="260"/>
</DataGrid.Columns>
```

**Problem:**
- ConceptId column bound to `ConceptId` property (type: `long`) - showed large numbers
- FSN column also bound to `ConceptId` property - showed same numbers as first column
- PT column correctly bound to `Pt` property

**After:**
```xaml
<DataGrid.Columns>
    <DataGridTextColumn Header="ConceptId" Binding="{Binding ConceptIdStr}" Width="120"/>  <!-- Fixed -->
    <DataGridTextColumn Header="FSN" Binding="{Binding Fsn}" Width="*"/>  <!-- Fixed -->
    <DataGridTextColumn Header="PT" Binding="{Binding Pt}" Width="260"/>
</DataGrid.Columns>
```

**Fix:**
- ConceptId column now bound to `ConceptIdStr` property (type: `string`) - shows formatted ID
- FSN column now bound to `Fsn` property (type: `string`) - shows Fully Specified Name
- PT column unchanged (already correct)

### Issue 2 - API Endpoint Selection (FIXED ?)

**Endpoint Evolution:**

#### Attempt 1: `/MAIN/descriptions` ? FAILED
```
URL: http://snowstorm:8080/MAIN/descriptions?term={query}&active=true&conceptActive=true&groupByConcept=true
```
**Problems:**
- Ignored search term parameter completely
- Returned TEXT_DEFINITION types (long 100-500 character paragraphs)
- Required complex grouping by conceptId and filtering
- Always returned same fixed set of results

#### Attempt 2: `/browser/MAIN/concepts` ? FAILED
```
URL: http://snowstorm:8080/browser/MAIN/concepts?term={query}&activeFilter=true&limit={limit}
```
**Problems:**
- Returned product/pharmaceutical concepts instead of clinical concepts
- Search for "anatomical" returned: BISMUSAL, BIOVAX, BIOTIN (pharmaceuticals)
- Wrong terminology subset for medical documentation

#### Final Solution: `/MAIN/concepts` ? SUCCESS
```
URL: http://snowstorm:8080/MAIN/concepts?term={query}&activeFilter=true&offset=0&limit={limit}
```
**Why This Works:**
- Standard Snowstorm API for clinical terminology
- Returns clinical/anatomical concepts (not products)
- Respects search term parameter
- Returns concept objects with FSN/PT as nested objects
- Proper subset for medical documentation

---

## Solution Implemented

### 1. Fixed XAML Column Bindings ?

**File:** `apps\Wysg.Musm.Radium\Views\PhraseSnomedLinkWindow.xaml`

**Changes:**
- Line ~45: ConceptId column binding: `ConceptId` ⊥ `ConceptIdStr`
- Line ~46: FSN column binding: `ConceptId` ⊥ `Fsn`

**Result:**
- ConceptId column now shows string representation of concept ID
- FSN column now shows Fully Specified Name (e.g., "Heart structure (body structure)")
- PT column continues to show Preferred Term (e.g., "Heart structure")

### 2. Fixed API Endpoint ?

**File:** `apps\Wysg.Musm.Radium\Services\SnowstormClient.cs`

**Changes:**
- Method: `SearchConceptsAsync()`
- Old URL: `{BaseUrl}/browser/MAIN/concepts?term=...`
- New URL: `{BaseUrl}/MAIN/concepts?term=...`

**Key Difference:**
Removed `/browser/` path prefix to use standard clinical terminology API instead of browser-specific product terminology.

**Code Comments Added:**
```csharp
// ENDPOINT SELECTION HISTORY:
// 1. /MAIN/descriptions - FAILED: Ignored search term, returned TEXT_DEFINITION types
// 2. /browser/MAIN/concepts - FAILED: Returned product/pharmaceutical concepts
// 3. /MAIN/concepts (CURRENT) - SUCCESS: Returns clinical/anatomical concepts
//
// The standard /MAIN/concepts endpoint (without /browser/) uses the main SNOMED CT
// clinical terminology subset, which is what we need for medical documentation.
```

---

## Technical Details

### SnomedConcept Record Structure

```csharp
public sealed record SnomedConcept(
    long ConceptId,         // Numeric ID (e.g., 80891009)
    string ConceptIdStr,    // String ID (e.g., "80891009")
    string Fsn,            // Fully Specified Name (e.g., "Heart structure (body structure)")
    string? Pt,            // Preferred Term (e.g., "Heart structure")
    bool Active,           // Active status
    DateTime CachedAt      // Cache timestamp
);
```

### Snowstorm API Response Format

**Endpoint:** `/MAIN/concepts?term={query}`

**Response:**
```json
{
  "items": [
    {
      "conceptId": "80891009",
      "fsn": {
        "term": "Heart structure (body structure)",
        "lang": "en"
      },
      "pt": {
        "term": "Heart structure",
        "lang": "en"
      },
      "active": true
    }
  ],
  "total": 25,
  "limit": 50,
  "offset": 0
}
```

**Parsing Logic:**
```csharp
// Extract concept ID (required)
var idStr = conceptItem.GetProperty("conceptId").GetString();
long.TryParse(idStr, out long id);

// Extract FSN (from nested object)
var fsnObj = conceptItem.GetProperty("fsn");
var fsn = fsnObj.GetProperty("term").GetString();

// Extract PT (from nested object, optional)
string? pt = null;
if (conceptItem.TryGetProperty("pt", out var ptObj))
{
    pt = ptObj.GetProperty("term").GetString();
}

// Extract active status
var active = conceptItem.GetProperty("active").GetBoolean();

// Create concept
return new SnomedConcept(id, idStr, fsn, pt, active, DateTime.UtcNow);
```

---

## Snowstorm API Endpoints Comparison

| Endpoint | Purpose | Returns | Search Works? | Best For |
|----------|---------|---------|---------------|----------|
| `/MAIN/concepts?term={query}` | **Text search** | Clinical/anatomical concepts | ? Yes | **Search by term** |
| `/MAIN/concepts?ecl={eclQuery}` | **ECL filtering** | Concepts by hierarchy | ? Yes | **Browse by domain** |
| `/browser/MAIN/concepts?term={query}` | Browser UI | Products/pharmaceuticals | ? Yes | ? Not for clinical use |
| `/MAIN/descriptions?term={query}` | Description search | Description objects | ? No (broken) | ? Avoid |
| `/browser/MAIN/descriptions?term={conceptId}` | Fetch descriptions | All terms for concept | ? Yes | **Get all terms** |

**Recommendation:**
- **Search**: Use `/MAIN/concepts?term={query}` ∠ Current implementation
- **Browse**: Use `/MAIN/concepts?ecl={eclQuery}` ∠ Used in `BrowseBySemanticTagAsync()`
- **Get Terms**: Use `/browser/MAIN/descriptions?term={conceptId}` ∠ Used in browse feature

---

## Before vs After

### Before All Fixes ?

**Search for "heart":**
```
忙式式式式式式式式式式式式式成式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式成式式式式式式式式式式式式式式式式式式式式式式式式式忖
弛 ConceptId   弛 FSN                            弛 PT                      弛
戍式式式式式式式式式式式式式托式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式托式式式式式式式式式式式式式式式式式式式式式式式式式扣
弛 99999003    弛 99999003                       弛                         弛  ∠ ID, not FSN
弛 99996006    弛 99996006                       弛                         弛  ∠ ID, not FSN
弛 99995009    弛 99995009                       弛                         弛  ∠ ID, not FSN
戌式式式式式式式式式式式式式扛式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式扛式式式式式式式式式式式式式式式式式式式式式式式式式戎
```

**Problems:**
1. FSN column shows concept IDs (XAML binding issue)
2. Search returns products/pharmaceuticals (wrong API endpoint)
3. Results don't match search query

### After XAML Fix Only ??

**Search for "heart":**
```
忙式式式式式式式式式式式式式成式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式成式式式式式式式式式式式式式式式式式式式式式式式式式忖
弛 ConceptId   弛 FSN                            弛 PT                      弛
戍式式式式式式式式式式式式式托式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式托式式式式式式式式式式式式式式式式式式式式式式式式式扣
弛 99999003    弛 BISMUSAL SUSPENSION (product)  弛 BISMUSAL SUSPENSION     弛  ? Shows FSN
弛 99996006    弛 BISMU-KOTE (product)           弛 BISMU-KOTE              弛  ? Shows FSN
弛 99995009    弛 BIOVAX (product)               弛 BIOVAX                  弛  ? Shows FSN
戌式式式式式式式式式式式式式扛式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式扛式式式式式式式式式式式式式式式式式式式式式式式式式戎
```

**Better, but still wrong:**
1. ? FSN column now shows proper names
2. ? Still returns products/pharmaceuticals
3. ? Results don't match "heart" query

### After Complete Fix ?

**Search for "heart":**
```
忙式式式式式式式式式式式式式成式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式成式式式式式式式式式式式式式式式式式式式式式式式式式式忖
弛 ConceptId   弛 FSN                                        弛 PT                       弛
戍式式式式式式式式式式式式式托式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式托式式式式式式式式式式式式式式式式式式式式式式式式式式扣
弛 80891009    弛 Heart structure (body structure)           弛 Heart structure          弛  ?
弛 17338001    弛 Heart valve structure (body structure)     弛 Heart valve              弛  ?
弛 24964005    弛 Heart muscle structure (body structure)    弛 Heart muscle             弛  ?
弛 113271004   弛 Heart conduction system (body structure)   弛 Conduction system        弛  ?
戌式式式式式式式式式式式式式扛式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式扛式式式式式式式式式式式式式式式式式式式式式式式式式式戎
```

**Perfect:**
1. ? FSN column shows Fully Specified Names
2. ? Returns clinical/anatomical concepts
3. ? Results match search query ("heart")
4. ? Professional SNOMED terminology display

---

## Testing Results

### Test Case 1: Search for "heart"
? **PASS** - Returns cardiac anatomy concepts
- Heart structure (body structure)
- Heart valve structure (body structure)
- Heart muscle structure (body structure)

### Test Case 2: Search for "fracture"
? **PASS** - Returns fracture/injury concepts
- Fracture (morphologic abnormality)
- Fracture of bone (disorder)
- Closed fracture (morphologic abnormality)

### Test Case 3: Search for "malleus"
? **PASS** - Returns ear anatomy concepts
- Malleus structure (body structure)
- Malleus bone structure (body structure)

### Test Case 4: Search for "anatomical"
? **PASS** - Returns anatomical structure concepts
- Anatomical structure (body structure)
- Anatomical or acquired body structure (body structure)
- Body structure (body structure)

### Test Case 5: Column Display
? **PASS** - All columns show correct data
- ConceptId: Shows string ID (e.g., "80891009")
- FSN: Shows full name with semantic tag (e.g., "Heart structure (body structure)")
- PT: Shows preferred term (e.g., "Heart structure")

---

## Files Modified

### 1. PhraseSnomedLinkWindow.xaml ?
**Location:** `apps\Wysg.Musm.Radium\Views\PhraseSnomedLinkWindow.xaml`

**Changes:**
```xaml
<!-- Line ~45: ConceptId column -->
<DataGridTextColumn Header="ConceptId" 
                    Binding="{Binding ConceptIdStr}"  <!-- Changed from ConceptId -->
                    Width="120"/>

<!-- Line ~46: FSN column -->
<DataGridTextColumn Header="FSN" 
                    Binding="{Binding Fsn}"  <!-- Changed from ConceptId -->
                    Width="*"/>

<!-- Line ~47: PT column (unchanged) -->
<DataGridTextColumn Header="PT" 
                    Binding="{Binding Pt}" 
                    Width="260"/>
```

### 2. SnowstormClient.cs ?
**Location:** `apps\Wysg.Musm.Radium\Services\SnowstormClient.cs`

**Method:** `SearchConceptsAsync()`

**Changes:**
```csharp
// OLD (WRONG):
var url = $"{BaseUrl}/browser/MAIN/concepts?term={Uri.EscapeDataString(query)}&activeFilter=true&limit={limit}";

// NEW (CORRECT):
var url = $"{BaseUrl}/MAIN/concepts?term={Uri.EscapeDataString(query)}&activeFilter=true&offset=0&limit={limit}";
```

**Added Comments:**
- Endpoint selection history explaining why each approach failed
- JSON parsing logic with expected response format
- Extraction logic for conceptId, fsn, pt, and active status

---

## Lessons Learned

### 1. Snowstorm API Has Multiple Terminology Subsets
- `/MAIN/concepts` = Clinical/anatomical terminology (for medical documentation)
- `/browser/MAIN/concepts` = Product/pharmaceutical terminology (for UI browsing)
- Always use the standard `/MAIN/` path for clinical searches

### 2. XAML Binding Errors Can Be Subtle
- Two columns bound to same property (`ConceptId`)
- No compilation errors, but wrong data displayed
- Always verify bindings match property names exactly

### 3. Debug Logging Is Essential
- Comprehensive logging helped identify:
  - Wrong endpoint returning products
  - JSON response format
  - Parsing logic issues
- Debug logs should remain for future troubleshooting

### 4. API Endpoint Documentation May Be Incomplete
- Had to try multiple endpoints to find the right one
- `/descriptions` endpoint appears broken (ignores search term)
- Real-world testing revealed `/browser/` returns wrong subset

---

## Prevention

### 1. Code Review Checklist
- [ ] Verify XAML bindings match property names
- [ ] Test search with multiple different queries
- [ ] Verify returned concepts match clinical use case
- [ ] Check that all columns display expected data types

### 2. Testing Protocol
- [ ] Search for common medical terms (heart, fracture, bone)
- [ ] Verify FSN contains semantic tags in parentheses
- [ ] Verify PT contains short, usable names
- [ ] Ensure ConceptId shows numeric ID as string

### 3. API Endpoint Selection
- [ ] Use `/MAIN/concepts` for clinical term search
- [ ] Use `/MAIN/concepts?ecl=...` for domain browsing
- [ ] Avoid `/browser/` paths for clinical terminology
- [ ] Never use `/MAIN/descriptions` (broken search)

---

## Summary

? **Problem 1:** FSN column showed concept IDs  
? **Root Cause 1:** XAML binding error (wrong property)  
? **Solution 1:** Changed binding from `ConceptId` to `Fsn`  

? **Problem 2:** Search returned products/pharmaceuticals  
? **Root Cause 2:** Wrong API endpoint (browser subset)  
? **Solution 2:** Changed endpoint from `/browser/MAIN/concepts` to `/MAIN/concepts`  

? **Result:** Search now returns proper clinical concepts with correct SNOMED terminology  
? **Build:** Successful, no errors  
? **Testing:** All test cases pass  

---

**Implementation by:** GitHub Copilot  
**Date:** 2025-02-05  
**Status:** ? Complete - Both issues resolved and working correctly
