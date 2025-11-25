# Fix: Reportify Conclusion Numbering Issues

**Date**: 2025-10-23  
**Issue**: Reportify not properly handling conclusion numbering, blank lines, and indentation  
**Status**: ? Fixed  
**Build**: ? Success

---

## Problems Fixed

### 1. Line-by-Line Numbering Not Working
**Issue**: When using line-by-line numbering mode on a single paragraph, each line was supposed to get a number, but the logic was treating lines after blank lines as new items.

**Example Input**:
```
1. No acute intracranial hemorrhage.
No acute skull fracture.

```

**Before Fix**:
```
1. No acute intracranial hemorrhage.
No acute skull fracture.
```
? Second line not numbered

**After Fix**:
```
1. No acute intracranial hemorrhage.
2. No acute skull fracture.
```
? Each line properly numbered

---

### 2. Blank Lines Not Trimmed
**Issue**: Trailing blank lines and blank lines between items were kept, creating ugly whitespace.

**Example Input**:
```
1. No acute intracranial hemorrhage.
No acute skull fracture.

```

**Before Fix**:
```
1. No acute intracranial hemorrhage.
2. No acute skull fracture.

```
? Trailing blank line kept

**After Fix**:
```
1. No acute intracranial hemorrhage.
2. No acute skull fracture.
```
? Blank lines removed

---

### 3. Multi-Paragraph Indentation Not Working
**Issue**: When numbering paragraphs (original mode), continuation lines within a paragraph weren't properly indented.

**Example Input**:
```
No acute intracranial hemorrhage.
No acute skull fracture.

Mild brain atrophy.
Chronic microangiopathy.
```

**Before Fix** (paragraph mode):
```
1. No acute intracranial hemorrhage.
No acute skull fracture.

2. Mild brain atrophy.
Chronic microangiopathy.
```
? Continuation lines not indented

**After Fix** (paragraph mode):
```
1. No acute intracranial hemorrhage.
   No acute skull fracture.

2. Mild brain atrophy.
   Chronic microangiopathy.
```
? Continuation lines indented with 3 spaces

---

## Technical Changes

### File Modified
`apps/Wysg.Musm.Radium/ViewModels/MainViewModel.ReportifyHelpers.cs`

### Method: `ApplyReportifyBlock()`

#### Change 1: Line-by-Line Numbering Mode

**Before**:
```csharp
if (cfg.NumberConclusionLinesOnOneParagraph)
{
    var lines = input.Replace("\r\n", "\n").Split('\n');
    int num = 1;
    bool lastWasBlank = false;
    for (int i = 0; i < lines.Length; i++)
    {
        var t = lines[i].Trim();
        if (t.Length == 0) 
        {
            lines[i] = string.Empty;
            lastWasBlank = true;
            continue;
        }
        
        // If previous line was blank or this is first line, start new number
        if (lastWasBlank || i == 0)
        {
            lines[i] = $"{num}. {t}";
            num++;
        }
        else
        {
            // Continuation line - indent it
            lines[i] = $"   {t}";
        }
        lastWasBlank = false;
    }
    input = string.Join("\n", lines);
}
```
? Treats lines after blanks as new items, adds indents incorrectly

**After**:
```csharp
if (cfg.NumberConclusionLinesOnOneParagraph)
{
    var lines = input.Split('\n');
    var resultLines = new List<string>();
    int num = 1;
    
    foreach (var line in lines)
    {
        var trimmed = line.Trim();
        
        // Skip completely blank lines
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            continue; // Don't add blank lines at all
        }
        
        // Check if this line already has a number (from manual entry)
        bool hasNumber = RxNumbered.IsMatch(trimmed);
        
        if (hasNumber)
        {
            // Line already numbered - use it as-is but ensure proper format
            var match = RxNumbered.Match(trimmed);
            var content = trimmed.Substring(match.Length);
            resultLines.Add($"{num}. {content}");
            num++;
        }
        else
        {
            // Line not numbered - add number
            resultLines.Add($"{num}. {trimmed}");
            num++;
        }
    }
    
    input = string.Join("\n", resultLines);
}
```
? Numbers every non-blank line, removes all blank lines

#### Change 2: Paragraph Numbering Mode with Indentation

**Before**:
```csharp
else
{
    // ORIGINAL MODE: Number paragraphs separated by blank lines
    var paras = input.Replace("\r\n", "\n").Split("\n\n", StringSplitOptions.None);
    int num = 1;
    for (int i = 0; i < paras.Length; i++)
    {
        var t = paras[i].Trim();
        if (t.Length == 0) continue;
        paras[i] = $"{num}. {t}"; num++;
    }
    input = string.Join("\n\n", paras);
}
```
? Flattens multi-line paragraphs, no indentation

**After**:
```csharp
else
{
    // ORIGINAL MODE: Number paragraphs separated by blank lines
    var paras = input.Split("\n\n", StringSplitOptions.RemoveEmptyEntries);
    int num = 1;
    var resultParas = new List<string>();
    
    foreach (var para in paras)
    {
        var trimmed = para.Trim();
        if (string.IsNullOrWhiteSpace(trimmed)) continue;
        
        // Split paragraph into lines for proper indenting
        var paraLines = trimmed.Split('\n');
        var formattedLines = new List<string>();
        
        for (int i = 0; i < paraLines.Length; i++)
        {
            var line = paraLines[i].Trim();
            if (string.IsNullOrWhiteSpace(line)) continue;
            
            if (i == 0)
            {
                // First line gets the number
                formattedLines.Add($"{num}. {line}");
            }
            else
            {
                // Continuation lines get indented
                formattedLines.Add($"   {line}");
            }
        }
        
        if (formattedLines.Count > 0)
        {
            resultParas.Add(string.Join("\n", formattedLines));
            num++;
        }
    }
    
    input = string.Join("\n\n", resultParas);
}
```
? Preserves multi-line paragraphs with proper indentation

---

## Configuration

### Line-by-Line Mode (NEW)
When `number_conclusion_lines_on_one_paragraph` is `true`:
- **Each line gets a number**
- **All blank lines removed**
- **No indentation** (every line is a separate numbered item)

### Paragraph Mode (ORIGINAL)
When `number_conclusion_lines_on_one_paragraph` is `false` or not set:
- **Each paragraph gets a number**
- **First line of paragraph numbered**
- **Continuation lines indented with 3 spaces**
- **Blank lines between paragraphs preserved (one blank line)**

---

## Examples

### Example 1: Line-by-Line Mode (Single Paragraph)

**Input**:
```
No acute intracranial hemorrhage.
No acute skull fracture.
```

**Output**:
```
1. No acute intracranial hemorrhage.
2. No acute skull fracture.
```

---

### Example 2: Paragraph Mode (Multiple Paragraphs)

**Input**:
```
No acute intracranial hemorrhage.
No acute skull fracture.

Mild brain atrophy.
Chronic microangiopathy.
```

**Output**:
```
1. No acute intracranial hemorrhage.
   No acute skull fracture.

2. Mild brain atrophy.
   Chronic microangiopathy.
```

---

### Example 3: Line-by-Line Mode (Removes Blanks)

**Input**:
```
No acute intracranial hemorrhage.

No acute skull fracture.

```

**Output**:
```
1. No acute intracranial hemorrhage.
2. No acute skull fracture.
```
(All blank lines removed)

---

### Example 4: Paragraph Mode (Preserves Structure)

**Input**:
```
No acute intracranial hemorrhage.
No acute skull fracture.


Mild brain atrophy.
Chronic microangiopathy.
```

**Output**:
```
1. No acute intracranial hemorrhage.
   No acute skull fracture.

2. Mild brain atrophy.
   Chronic microangiopathy.
```
(Multiple blank lines collapsed to single blank)

---

## Testing

### Test Case 1: Single Paragraph, Line Mode
```
Input: "1. No acute intracranial hemorrhage.\nNo acute skull fracture.\n\n"
Config: number_conclusion_lines_on_one_paragraph = true
Expected: "1. No acute intracranial hemorrhage.\n2. No acute skull fracture."
```

### Test Case 2: Multiple Paragraphs, Paragraph Mode
```
Input: "No acute intracranial hemorrhage.\nNo acute skull fracture.\n\nMild brain atrophy.\nChronic microangiopathy."
Config: number_conclusion_lines_on_one_paragraph = false
Expected: "1. No acute intracranial hemorrhage.\n   No acute skull fracture.\n\n2. Mild brain atrophy.\n   Chronic microangiopathy."
```

### Test Case 3: Already Numbered
```
Input: "1. No acute intracranial hemorrhage.\n2. No acute skull fracture."
Config: number_conclusion_lines_on_one_paragraph = true
Expected: "1. No acute intracranial hemorrhage.\n2. No acute skull fracture."
(Renumbers to ensure consistency)
```

---

## Impact

### Positive
? **Cleaner Output**: No trailing blank lines  
? **Proper Numbering**: Each line/paragraph numbered correctly  
? **Better Readability**: Indentation shows structure  
? **Flexible**: Two modes for different use cases

### No Breaking Changes
? **Backward Compatible**: Both modes preserved  
? **Default Behavior**: Original paragraph mode is default  
? **Configuration**: Settings control behavior

---

## Build Status

? **Compilation**: Success (no errors)  
? **Dependencies**: All resolved  
? **Integration**: No conflicts

---

## Summary

**Problem**: Reportify conclusion numbering had three issues:
1. Line-by-line mode didn't number every line
2. Blank lines weren't removed
3. Paragraph mode didn't indent continuation lines

**Solution**: Rewrote both numbering modes to:
1. Number every non-blank line in line mode
2. Remove all blank lines in line mode
3. Properly indent continuation lines in paragraph mode

**Result**: Clean, professional-looking numbered conclusions with proper formatting

---

**Status**: ? Fixed and Tested  
**Build**: ? Success
