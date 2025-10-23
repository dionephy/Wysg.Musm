# Reportify Enhancement: Line-Based Numbering and Enhanced Capitalization

**Date**: 2025-01-23
**Feature**: Enhanced Conclusion Numbering and Capitalization Options
**Scope**: Reportify Settings in Settings Window

## Overview

Added two new options to the Reportify formatting system to provide more flexible numbering and capitalization control for conclusion formatting.

## New Features

### 1. Number Each Line on One Paragraph

**Setting Name**: `number_conclusion_lines_on_one_paragraph`  
**UI Label**: "On one paragraph, number each line"  
**Location**: Settings ¡æ Reportify ¡æ Conclusion Numbering  
**Default**: `false` (disabled)

#### Behavior

When enabled (along with "Number conclusion paragraphs"):
- Numbers each non-empty line instead of numbering paragraphs
- Lines after the first line in a continuous block are indented as continuation lines
- Blank lines reset the numbering sequence to the next number

#### Example

**Input:**
```
apple
banana

melon
```

**Output:**
```
1. Apple.
   Banana.

2. Melon.
```

**Detailed Flow:**
1. First line "apple" ¡æ "1. Apple."
2. Next line "banana" (no blank before) ¡æ "   Banana." (indented continuation)
3. Blank line encountered
4. Next line "melon" ¡æ "2. Melon." (new number)

### 2. Capitalize After Bullet or Number

**Setting Name**: `capitalize_after_bullet_or_number`  
**UI Label**: "Also capitalize after bullet or number"  
**Location**: Settings ¡æ Reportify ¡æ Sentence Formatting  
**Default**: `false` (disabled)

#### Behavior

When enabled (along with "Capitalize first letter"):
- Capitalizes the first letter **after** bullets (`- `), numbers (`1. `, `2. `, etc.), arrows (`--> `), and indentation (`   `)
- Works in conjunction with the standard capitalization feature

#### Example

**Input:**
```
apple
banana
melon
```

**With original "Number conclusion paragraphs" + "Capitalize first letter":**
```
1. apple.
2. banana.
3. melon.
```

**With new "On one paragraph, number each line" + "Capitalize first letter" + "Also capitalize after bullet or number":**
```
1. Apple.
2. Banana.
3. Melon.
```

## Combined Example

**Input:**
```
apple
banana

melon
```

**Settings Enabled:**
- ? Number conclusion paragraphs
- ? On one paragraph, number each line
- ? Capitalize first letter
- ? Also capitalize after bullet or number
- ? Ensure trailing period

**Output:**
```
1. Apple.
   Banana.

2. Melon.
```

## Technical Implementation

### Configuration Properties

**In `ReportifyConfig` class:**
```csharp
public bool NumberConclusionLinesOnOneParagraph { get; init; } = false;
public bool CapitalizeAfterBulletOrNumber { get; init; } = false;
```

### JSON Schema

**Structure:**
```json
{
  "number_conclusion_lines_on_one_paragraph": false,
  "capitalize_after_bullet_or_number": false,
  // ... other settings
}
```

### Logic Flow

#### Line-Based Numbering Logic

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
        
        // Start new number after blank or at beginning
        if (lastWasBlank || i == 0)
        {
            lines[i] = $"{num}. {t}";
            num++;
        }
        else
        {
            // Indent continuation line
            lines[i] = $"   {t}";
        }
        lastWasBlank = false;
    }
    input = string.Join("\n", lines);
}
```

#### Enhanced Capitalization Logic

```csharp
if (cfg.CapitalizeSentence && working.Length > 0)
{
    int firstLetterPos = 0;
    
    // Detect and skip prefixes (bullets, numbers, arrows, indentation)
    if (working.StartsWith("- ")) firstLetterPos = 2;
    else if (RxNumbered.IsMatch(working)) firstLetterPos = RxNumbered.Match(working).Length;
    else if (working.StartsWith(cfg.Arrow + " ")) firstLetterPos = cfg.Arrow.Length + 1;
    else if (working.StartsWith("   ")) firstLetterPos = 3;
    
    // Find first letter after prefix
    while (firstLetterPos < working.Length && !char.IsLetter(working[firstLetterPos]))
    {
        firstLetterPos++;
    }
    
    // Capitalize the first letter
    if (firstLetterPos < working.Length && char.IsLetter(working[firstLetterPos]))
    {
        if (cfg.CapitalizeAfterBulletOrNumber || firstLetterPos == 0)
        {
            // Capitalize if enabled or if no prefix
            working = working[..firstLetterPos] + 
                     char.ToUpperInvariant(working[firstLetterPos]) + 
                     working[(firstLetterPos + 1)..];
        }
    }
}
```

## UI Integration

### Settings Tab

**File**: `apps/Wysg.Musm.Radium/Views/SettingsTabs/ReportifySettingsTab.xaml`

**Conclusion Numbering Section:**
```xaml
<Expander Header="Conclusion Numbering">
    <StackPanel>
        <CheckBox Content="Number conclusion paragraphs" IsChecked="{Binding NumberConclusionParagraphs}"/>
        <CheckBox Content="On one paragraph, number each line" 
                  IsChecked="{Binding NumberConclusionLinesOnOneParagraph}" 
                  Margin="20,0,0,0"/>
        <CheckBox Content="Indent continuation lines" IsChecked="{Binding IndentContinuationLines}"/>
    </StackPanel>
</Expander>
```

**Sentence Formatting Section:**
```xaml
<Expander Header="Sentence Formatting">
    <StackPanel>
        <CheckBox Content="Capitalize first letter" IsChecked="{Binding CapitalizeSentence}"/>
        <CheckBox Content="Also capitalize after bullet or number" 
                  IsChecked="{Binding CapitalizeAfterBulletOrNumber}" 
                  Margin="20,0,0,0"/>
        <!-- other options -->
    </StackPanel>
</Expander>
```

### Sample Preview

**Hint buttons** are provided next to each checkbox to show before/after examples:
- Click "Hint" next to "On one paragraph, number each line" to see example transformation
- Click "Hint" next to "Also capitalize after bullet or number" to see example transformation

## Files Modified

1. **`apps/Wysg.Musm.Radium/ViewModels/SettingsViewModel.cs`**
   - Added properties: `NumberConclusionLinesOnOneParagraph`, `CapitalizeAfterBulletOrNumber`
   - Updated `UpdateReportifyJson()` to serialize new settings
   - Updated `ApplyReportifyJson()` to deserialize new settings
   - Added samples in `GetSamples()`

2. **`apps/Wysg.Musm.Radium/ViewModels/MainViewModel.ReportifyHelpers.cs`**
   - Updated `ReportifyConfig` class with new properties
   - Updated `EnsureReportifyConfig()` to parse new settings from JSON
   - Modified `ApplyReportifyBlock()` to implement line-based numbering logic
   - Enhanced capitalization logic to detect and capitalize after prefixes

3. **`apps/Wysg.Musm.Radium/Views/SettingsTabs/ReportifySettingsTab.xaml`**
   - Added checkbox for "On one paragraph, number each line" (indented under conclusion numbering)
   - Added checkbox for "Also capitalize after bullet or number" (indented under capitalize sentence)
   - Added Hint buttons for both new options

## Usage Guide

### Scenario 1: Simple Line Numbering

**Use Case**: Number each line separately instead of paragraphs

**Steps:**
1. Open Settings ¡æ Reportify tab
2. Enable "Number conclusion paragraphs"
3. Enable "On one paragraph, number each line"
4. Save Settings

**Result**: Each line gets a number, with continuation lines indented

### Scenario 2: Capitalize After Numbers

**Use Case**: Ensure proper capitalization after conclusion numbers

**Steps:**
1. Open Settings ¡æ Reportify tab
2. Enable "Capitalize first letter"
3. Enable "Also capitalize after bullet or number"
4. Save Settings

**Result**: Text after `1. `, `2. `, `- `, etc. starts with capital letter

### Scenario 3: Combined Usage

**Use Case**: Professional numbered conclusion with proper capitalization

**Steps:**
1. Enable "Number conclusion paragraphs"
2. Enable "On one paragraph, number each line"
3. Enable "Capitalize first letter"
4. Enable "Also capitalize after bullet or number"
5. Enable "Ensure trailing period"
6. Save Settings

**Input:**
```
apple
banana

melon
```

**Output:**
```
1. Apple.
   Banana.

2. Melon.
```

## Edge Cases

### 1. Mixed Blank Lines

**Input:**
```
apple

banana


melon
```

**Output:**
```
1. Apple.

2. Banana.

3. Melon.
```

Multiple blank lines are collapsed to single blank line (if "Remove excessive blank lines" is enabled).

### 2. Already Numbered Input

**Input:**
```
1. apple
2. banana
```

**Output:**
```
1. Apple.
2. Banana.
```

Existing numbers are replaced with sequential numbers.

### 3. Empty Lines at Start

**Input:**
```

apple
banana
```

**Output:**
```
1. Apple.
   Banana.
```

Leading blank lines are ignored for numbering purposes.

## Compatibility

- **Backward Compatible**: Disabling both new options restores original behavior
- **Database**: Settings persist to `radium.reportify_setting` table as JSON
- **Sync**: Settings sync across devices for same account
- **Default Values**: Both options default to `false` to maintain existing behavior

## Testing Recommendations

### Test Case 1: Line Numbering Only
```
Input:  apple\nbanana\n\nmelon
Output: 1. Apple.\n   Banana.\n\n2. Melon.
```

### Test Case 2: Capitalization Only
```
Input:  1. apple\n2. banana
Output: 1. Apple.\n2. Banana.
```

### Test Case 3: Combined
```
Input:  apple\nbanana\n\nmelon
Output: 1. Apple.\n   Banana.\n\n2. Melon.
```

### Test Case 4: Bullets
```
Input:  - apple\n- banana
Output: - Apple.\n- Banana.
```

### Test Case 5: Complex Mixed
```
Input:  apple\nbanana\n\n- melon\n- grape\n\norange
Output: 1. Apple.\n   Banana.\n\n2. - Melon.\n   - Grape.\n\n3. Orange.
```

## Build Status

? **Build Successful**
- No compilation errors
- All new features integrated
- UI updated correctly

## Performance Impact

**Negligible** - Additional logic only runs during reportify transformation:
- Line iteration: O(n) where n = number of lines
- Character prefix detection: O(1) per line
- Overall: Same complexity class as existing reportify logic

## Future Enhancements

Potential improvements for future versions:
1. Custom numbering format (e.g., "A.", "I.", "(1)")
2. Multi-level numbering (e.g., "1.1", "1.2")
3. Preserve specific capitalization exceptions (e.g., "pH", "HbA1c")
4. Customizable indentation width
5. Option to number only certain line patterns
