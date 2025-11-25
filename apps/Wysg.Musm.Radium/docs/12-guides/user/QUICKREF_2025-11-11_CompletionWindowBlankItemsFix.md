# Quick Reference: Completion Window Blank Items Fix

**Date**: 2025-11-11  
**Issue**: Completion window showing blank items  
**Solution**: One-line fix - disable AvalonEdit's built-in filtering  

---

## Problem Summary

When typing phrases like "brain", the completion window appeared but showed **blank/empty items** (no visible text).

---

## Root Cause

**Double-filtering conflict** caused by `CompletionList.IsFiltering = true`:

1. **First filter** - `PhraseCompletionProvider` filters phrases by prefix (custom logic)
2. **Second filter** - AvalonEdit's `CompletionList` re-filters using its own algorithm
3. **Conflict** - The two filtering approaches don't align, causing all items to be hidden

---

## Solution

**File**: `src\Wysg.Musm.Editor\Completion\MusmCompletionWindow.cs`  
**Line**: 29

```csharp
// BEFORE:
CompletionList.IsFiltering = true;  // ? Caused double-filtering

// AFTER:
CompletionList.IsFiltering = false; // ? Disable redundant filtering
```

---

## Why This Works

- We **already filter** in `PhraseCompletionProvider.GetCompletions()`
- AvalonEdit's built-in filtering is **redundant**
- Disabling it prevents the filtering conflict
- Single filtering layer = no conflicts = items display correctly

---

## Testing

Run the application and type "brain":
```
? Before: Blank window (no visible text)
? After:  Shows "brain", "brain stem", "brain substance"
```

---

## Key Insight

**Framework features can conflict with custom implementations.**

When implementing custom filtering logic, disable the framework's built-in filtering to avoid conflicts. Always check what the base class does by default!

---

**Status**: ? FIXED  
**Build**: ? Success  
**Confidence**: Very High (simple, well-understood fix)
