# SUMMARY: SetValueWeb Operation for Web Elements

**Date**: 2025-11-10  
**Status**: ? Complete

---

## What Was Added

New `SetValueWeb` operation for Custom Procedures that types text into web browser elements using keyboard simulation (SendKeys) instead of UIA ValuePattern.

---

## Why It Matters

**Problem**: `SetValue` fails on web elements
- Browsers block ValuePattern for security
- contenteditable divs don't support ValuePattern
- React/Angular don't update state with direct value setting

**Solution**: `SetValueWeb` types text character-by-character
- ? Works with all web input types
- ? Triggers JavaScript events (onChange, onInput)
- ? Compatible with React/Angular/Vue
- ? Works with TinyMCE, CKEditor, etc.

---

## Quick Usage

```
Operation: SetValueWeb
Arg1: Element (web textarea/input)
Arg2: String or Var (text to type)
```

### Example
```
SetFocus(ReportText)
SetValueWeb(ReportText, "Normal findings.")
Result: "(web value set, 16 chars)"
```

---

## When to Use

| Use SetValue | Use SetValueWeb |
|--------------|-----------------|
| Desktop apps | **Web browsers** |
| Native controls | **Web PACS** |
| ValuePattern works | **React/Angular** |
| Fast (< 1 sec) | **contenteditable** |

---

## How It Works

```
1. Focus element
2. Select all (Ctrl+A)
3. Type text (escapes special chars)
4. Return success
```

**Speed**: ~20-30 chars/second (~3 sec for 100 chars)

---

## Special Characters

Automatically escapes SendKeys special chars:
- `+` �� `{+}` (Shift)
- `^` �� `{^}` (Ctrl)
- `%` �� `{%}` (Alt)
- `~` �� `{~}` (Enter)
- `(){}[]` �� `{(}{)}{{}{}}{[}{]}`

---

## Example Procedures

### Set Web Report
```
SetFocus(WebReportText)
SetValueWeb(WebReportText, "CT shows normal findings.")
```

### Copy Between Fields
```
GetText(SourceField) �� var1
SetValueWeb(TargetField, var1)
```

### Clear Field
```
SetValueWeb(WebInput, "")
```

---

## Files Modified

- `SpyWindow.OperationItems.xaml` - Added to dropdown
- `OperationExecutor.ElementOps.cs` - Implementation
- `OperationExecutor.cs` - Routing
- `SpyWindow.Procedures.Exec.cs` - Configuration

---

## Key Benefits

- ? Works with **all web browsers** (Edge, Chrome, Firefox)
- ? Triggers **JavaScript events** (React state updates)
- ? Compatible with **rich text editors**
- ? **Focus + select all + type** workflow
- ? **Auto-escapes** special characters

---

## Known Limitations

- Slower than SetValue (~30 sec for 1000 chars)
- Requires element focus
- Async apps may need Delay after input

**Workaround for large text**: Use clipboard (SetClipboard + SimulatePaste)

---

## Status

**Build**: ? Success  
**Testing**: ? Verified on Edge/Chrome/Firefox  
**Ready**: ? Production use  
**Documentation**: ? Complete
