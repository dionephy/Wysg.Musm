# QUICKREF: Web Browser Element Picker

**Date**: 2025-11-25  
**Type**: Quick Reference  
**Category**: User Reference  
**Status**: ? Active

---

## Summary

This guide provides quick reference information for users. For detailed implementation information, see the related plan and specification documents.

---

# QUICKREF: Web Browser Element Picker

**Button**: "Pick Web" (next to "Pick" button in SpyWindow)  
**Purpose**: Capture web elements with automatic optimization for stability

---

## Quick Steps

```
1. Click "Pick Web"
2. Move mouse to web element (1500ms)
3. Click "Validate" to verify
4. Select bookmark from dropdown (e.g., "ReportText")
5. Click "Save" to map
```

---

## What It Does

- ? Captures web element tree
- ? Disables UseName for first 3 levels (browser windows)
- ? Enables AutomationId for web content (level 3+)
- ? Changes Scope to Descendants (faster)
- ? Does NOT auto-save (user must Save)

---

## Optimization Applied

| Level | UseName | UseClassName | UseControlTypeId | UseAutomationId | Scope |
|-------|---------|--------------|------------------|-----------------|-------|
| 0-2 (Browser) | ? | ? | ? | ? | Descendants |
| 3+ (Web) | ? | ? | ? | ? | Descendants |

---

## Example

```
Click "Pick Web" → Point to textarea
Status: "Captured web element from 'msedge' (optimized for web stability)"
Grid shows optimized settings
Click "Validate" → "found and highlighted (45 ms)" ?
Select "ReportText" from Bookmark dropdown
Click "Save" → "Saved mapping for ReportText"
```

---

## Why Use It?

**Problem**: Browser tab titles change → bookmarks break  
**Solution**: Pick Web ignores titles, uses structure + AutomationId  
**Result**: Bookmarks work with any tab title

---

## Common Bookmarks

- `ReportText` - Report textarea
- `StudyList` - Study list grid
- `SendButton` - Send report button
- `CloseButton` - Close window button

---

## Troubleshooting

**Q**: Validation fails after tab title changes  
**A**: Re-capture with "Pick Web" (old bookmark uses UseName)

**Q**: Does it auto-save the bookmark?  
**A**: No - you must select bookmark from dropdown and click Save

**Q**: Can I modify the captured chain?  
**A**: Yes - edit in grid before clicking Save

---

## Files

- Bookmarks saved to: `%APPDATA%\Wysg.Musm\Radium\ui-bookmarks.json`
- Reload list: Click "Reload" button in SpyWindow

---

**Status**: ? Complete  
**Workflow**: Pick Web → Validate → Select → Save  
**Auto-Save**: No

