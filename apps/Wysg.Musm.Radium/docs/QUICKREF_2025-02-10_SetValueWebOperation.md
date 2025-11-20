# QUICKREF: SetValueWeb Operation

**Purpose**: Type text into web browser elements using keyboard simulation

---

## Quick Start

```
Operation: SetValueWeb
Arg1: Element (web control)
Arg2: String or Var (text)
```

---

## Example

```
SetFocus(ReportText)
SetValueWeb(ReportText, "Normal findings.")
```

**Result**: `"(web value set, 16 chars)"`

---

## Why Use It?

**SetValue fails on web elements because**:
- ? Browsers block ValuePattern
- ? React/Angular don't update state
- ? contenteditable doesn't support ValuePattern

**SetValueWeb works because**:
- ? Types text (triggers events)
- ? React/Angular state updates
- ? Works with all web inputs

---

## SetValue vs SetValueWeb

| Feature | SetValue | SetValueWeb |
|---------|----------|-------------|
| **Works on web** | ? Often fails | ? Always |
| **Speed** | Fast | Slow (typing) |
| **JS Events** | ? No | ? Yes |
| **Focus** | Optional | Required |

---

## Common Patterns

### Basic Web Input
```
SetFocus(WebTextArea)
SetValueWeb(WebTextArea, "text")
```

### Copy Value
```
GetText(Source) ¡æ var1
SetValueWeb(Target, var1)
```

### Clear Field
```
SetValueWeb(WebInput, "")
```

### With Validation
```
SetValueWeb(ReportText, "Test")
Delay(100)
GetText(ReportText) ¡æ var1
IsMatch(var1, "Test") ¡æ var2
```

---

## Special Characters

Auto-escaped (you don't need to do anything):
- `+` `^` `%` `~` `(` `)` `{` `}` `[` `]`

Example:
```
Input:  "100% complete"
Types:  "100{%} complete" (automatic)
```

---

## Performance

| Text Length | Time |
|-------------|------|
| 10 chars | ~0.5 sec |
| 100 chars | ~3 sec |
| 1000 chars | ~30 sec |

**For large text**: Use clipboard instead
```
SetClipboard("Large text...")
SetFocus(WebTextArea)
SimulatePaste
```

---

## Browser Support

- ? Microsoft Edge
- ? Google Chrome
- ? Firefox
- ? All Chromium browsers

---

## How It Works

```
1. Focus element
2. Ctrl+A (select all)
3. Type text char-by-char
4. Done
```

---

## Troubleshooting

### "Text didn't appear"
- ? Use SetFocus first
- ? Add Delay(100) after if slow app

### "Only partial text appeared"
- ? Check for special chars (auto-escaped)
- ? Add Delay if text > 100 chars

### "Slow for large text"
- ? Use clipboard: SetClipboard + SimulatePaste

---

## When to Use

**Use SetValueWeb for**:
- ? Web browsers (Edge, Chrome, Firefox)
- ? Web PACS systems
- ? React/Angular/Vue apps
- ? contenteditable divs
- ? Rich text editors (TinyMCE, CKEditor)

**Use SetValue for**:
- ? Desktop applications
- ? Native Windows controls
- ? When ValuePattern works

---

## Error Messages

- `"(no element)"` - Element not found
- `"(web value set, N chars)"` - Success (N = char count)
- `"(error: message)"` - Exception during typing

---

## Related Operations

- **SetValue** - Desktop-optimized (faster)
- **SetFocus** - Focus element (required first)
- **SetClipboard** - For large text
- **SimulatePaste** - Paste from clipboard
- **GetText** - Validate after input

---

**Status**: ? Ready to use  
**Build**: ? Success  
**Docs**: Full guide in ENHANCEMENT document
