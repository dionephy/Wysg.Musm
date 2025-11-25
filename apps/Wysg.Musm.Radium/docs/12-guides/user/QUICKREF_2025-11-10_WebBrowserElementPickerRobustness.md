# QUICKREF: Web Browser Element Picker Troubleshooting

**Feature**: Web Browser Element Picker Robustness  
**Issue Fixed**: Bookmarks failing validation due to dynamic tab titles  
**Date**: 2025-11-10

---

## Common Issues & Solutions

### Issue: "Validate: not found" Error

**Symptoms:**
```
Step 2: Looking for Name='ITR Worklist Report - Microsoft Edge'
Result: not found (0 ms)
```

**Root Cause:** Browser tab title changed after bookmark was created

**Solution:** ? **Already Fixed!**
- Pick Web button now creates robust bookmarks
- Name matching disabled for browser window nodes
- Uses structural matching (ClassName + ControlTypeId)

---

## How the Fix Works

### Before Fix (Fragile)
```
Browser Window:
  ? UseName=True �� PROBLEM! Title changes with tabs
  ? UseClassName=True
  ? UseControlTypeId=True
```

### After Fix (Robust)
```
Browser Window:
  ? UseName=False �� FIXED! Ignores dynamic titles
  ? UseClassName=True �� Structural match
  ? UseControlTypeId=True �� Stable identifier
```

---

## Bookmark Structure Optimization

### Level 0-2: Browser Chrome (Structural)
- **Disabled**: UseName, UseAutomationId, UseIndex
- **Enabled**: UseClassName, UseControlTypeId
- **Scope**: Descendants (fast)

### Level 3+: Web Content (Content-based)
- **Disabled**: UseName, UseIndex
- **Enabled**: UseClassName, UseControlTypeId, UseAutomationId
- **Scope**: Descendants (fast)

---

## Testing Your Bookmarks

### Quick Test
1. Create bookmark with "Pick Web"
2. Change browser tab title
3. Click "Validate" in SpyWindow
4. Result should show: ? "found and highlighted"

### Expected Results
```
Step 1: ClassName='Chrome_WidgetWin_1' �� Match ?
Step 2: ClassName='BrowserRootView' �� Match ?
...
Step 14: AutomationId='job-report-view-report-text' �� Match ?
Resolved: Found and highlighted (45 ms)
```

---

## Browser Compatibility

| Browser | Status | Notes |
|---------|--------|-------|
| **Edge** | ? Tested | Chrome_WidgetWin_1 |
| **Chrome** | ? Tested | Chrome_WidgetWin_1 |
| **Firefox** | ? Tested | MozillaWindowClass |

---

## Performance

- **Resolution Time**: ~45ms (typical)
- **Search Strategy**: Descendants scope (fast hierarchical)
- **No Retries**: Single attempt with structural matching

---

## Tips

### ? Best Practices
- Use "Pick Web" button (not regular "Pick")
- Test bookmark after creation
- Descriptive names: "EdgeWorklist_ReportTextarea"

### ?? If Validation Still Fails
1. Check browser version (updates may change structure)
2. Verify element has AutomationId (Level 3+)
3. Try re-capturing with "Pick Web"

---

## Status Messages

### Success
```
"Saved web bookmark 'MyElement' from window 'Browser Title' (optimized for web stability)"
"Validate: found and highlighted (45 ms)"
```

### Failure (Should Not Occur)
```
"Validate: not found (64 ms)"
�� Re-capture with "Pick Web" button
```

---

## Technical Details

### Attribute Priority (Web Content)
1. **AutomationId** (highest stability)
2. **ClassName** (CSS classes)
3. **ControlTypeId** (HTML element type)
4. **Name** (disabled - too dynamic)
5. **Index** (disabled - too brittle)

### Browser Window Attributes
- ClassName: e.g., "Chrome_WidgetWin_1", "BrowserRootView"
- ControlTypeId: e.g., 50032 (Window), 50033 (Pane)

---

## See Also

- Full bugfix documentation: `BUGFIX_2025-11-10_WebBrowserElementPickerRobustness.md`
- Original feature: `ENHANCEMENT_2025-11-10_WebBrowserElementPicker.md`
- UI Bookmarks system: `Services\UiBookmarks.cs`

---

**Last Updated**: 2025-11-25  
**Status**: ? Fixed and Tested
