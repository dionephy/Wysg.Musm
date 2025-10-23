# CRITICAL FIX: Reportify Saving Wrong Values

**Date**: 2025-01-23  
**Issue**: Reportified (formatted) text being saved instead of raw text  
**Status**: ? Fixed  
**Build**: ? Success

---

## Problems Fixed

### 1. JSON Saving Reportified Text
**Issue**: `CurrentReportJson` was saving the currently **displayed** text (`FindingsText`, `ConclusionText`), which is the reportified/formatted version when `Reportified=true`.

**Impact**: 
- Database saves contained formatted text (capitalized, numbered, etc.)
- Original raw text was lost
- Editing loaded reports would show formatted text, not original

**Example**:
```
Raw Input: "no acute intracranial hemorrhage"
Reportified Display: "1. No acute intracranial hemorrhage."
Database Saved (WRONG): "1. No acute intracranial hemorrhage."  ?
Database Should Save: "no acute intracranial hemorrhage"  ?
```

### 2. PACS Send Using Reportified Text
**Issue**: `RunSendReportAsync()` was using `FindingsText` and `ConclusionText` directly, sending reportified text to PACS.

**Impact**:
- PACS received formatted text instead of original
- Reports in PACS system had unwanted numbering/formatting
- Inconsistent with user's original intent

**Example**:
```
User's Original Text:
  "no acute intracranial hemorrhage
  no acute skull fracture"

What Was Sent to PACS (WRONG):
  "1. No acute intracranial hemorrhage.
  2. No acute skull fracture."  ?

What Should Be Sent (CORRECT):
  "no acute intracranial hemorrhage
  no acute skull fracture"  ?
```

---

## Root Cause

The reportify feature has two modes:
1. **Reportified=false**: Display raw text (what user typed)
2. **Reportified=true**: Display formatted text (capitalized, numbered, etc.)

The issue was that save/send operations were using the **displayed** text, not the **raw** text.

---

## Solution

### Fix 1: UpdateCurrentReportJson - Always Save Raw Values

**Before**:
```csharp
var obj = new
{
    header_and_findings = FindingsText ?? string.Empty,  // ? Reportified!
    final_conclusion = ConclusionText ?? string.Empty,   // ? Reportified!
};
```

**After**:
```csharp
var obj = new
{
    // Always save RAW values
    header_and_findings = _reportified ? _rawFindings : (FindingsText ?? string.Empty),
    final_conclusion = _reportified ? _rawConclusion : (ConclusionText ?? string.Empty),
};
```

### Fix 2: Add Raw Value Accessors

**New**:
```csharp
public string RawFindingsText => _reportified ? _rawFindings : (_findingsText ?? string.Empty);
public string RawConclusionText => _reportified ? _rawConclusion : (_conclusionText ?? string.Empty);
```

### Fix 3: SendReport Uses Raw Values

**Before**:
```csharp
var findings = FindingsText ?? string.Empty;  // ?
var conclusion = ConclusionText ?? string.Empty;  // ?
```

**After**:
```csharp
var findings = RawFindingsText;  // ?
var conclusion = RawConclusionText;  // ?
```

---

## Testing

### Database Save While Reportified

**Before Fix**: Saved "1. No acute intracranial hemorrhage." ?  
**After Fix**: Saved "no acute intracranial hemorrhage" ?

### Send Report While Reportified

**Before Fix**: Sent "Normal study." ?  
**After Fix**: Sent "normal study" ?

---

## Files Modified

1. `apps/Wysg.Musm.Radium/ViewModels/MainViewModel.Editor.cs`
   - `UpdateCurrentReportJson()` - Save raw values
   - Added `RawFindingsText` and `RawConclusionText` properties

2. `apps/Wysg.Musm.Radium/ViewModels/MainViewModel.Commands.cs`
   - `RunSendReportAsync()` - Use raw values

---

**Status**: ? Fixed  
**Build**: ? Success  
**Impact**: Database and PACS now receive raw text
