# SUMMARY: Web Browser Element Picker

**Feature**: Web-optimized element capture for SpyWindow  
**Status**: ? Complete  
**Date**: 2025-02-10

---

## What Changed

Added "Pick Web" button to SpyWindow that works exactly like "Pick" but applies automatic optimization for web browser stability.

---

## Workflow

```
Click "Pick Web" ¡æ Move mouse to element ¡æ Captured with optimization
¡æ Click "Validate" ¡æ Select bookmark from dropdown ¡æ Click "Save"
```

**NO AUTO-SAVE** - User must select predefined bookmark and click Save button.

---

## Web Optimization

### Browser Windows (Level 0-2)
- ? UseName (titles change)
- ? UseClassName (stable structure)
- ? UseControlTypeId (stable type)
- ? Scope=Descendants (fast)

### Web Content (Level 3+)
- ? UseName (dynamic)
- ? UseClassName (CSS)
- ? UseControlTypeId (element type)
- ? UseAutomationId (best identifier)
- ? Scope=Descendants (fast)

---

## Why It Matters

**Problem**: Tab titles change ¡æ bookmarks break  
**Solution**: Ignore names, use structure + AutomationId  
**Result**: Bookmarks work even when tab titles change

---

## Example

```
User clicks "Pick Web" ¡æ Element captured
Grid shows: UseName=off, UseClassName=on for first 3 levels
User clicks "Validate" ¡æ Success (45 ms)
User selects "ReportText" from dropdown
User clicks "Save" ¡æ Bookmark mapped and saved
```

---

## Status Messages

- **During**: "Pick Web arming... (1500ms)"
- **After**: "Captured web element from 'msedge' (optimized for web stability)"
- **Success**: "Validate: found and highlighted (45 ms)"
- **Saved**: "Saved mapping for ReportText"

---

## Files Modified

- `SpyWindow.xaml` - Added button
- `SpyWindow.Bookmarks.cs` - Added OnPickWeb with optimization logic

---

**Key Point**: Same workflow as regular "Pick", but with smart web defaults.
