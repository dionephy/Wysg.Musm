# UI Reference Guide

## Application Window Layout

### Window Properties
- **Title**: "LLM Data Builder"
- **Size**: 1000x700 pixels
- **Position**: Center of screen
- **Resizable**: Yes

## Visual Layout

```
忙式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式忖
弛 LLM Data Builder                                        [_][﹤][X]弛
戍式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式扣
弛 Status Bar (Light Gray Background)                               弛
弛 忙式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式忖 弛
弛 弛 Ready                                                         弛 弛
弛 弛 Records: 0                                                    弛 弛
弛 戌式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式戎 弛
戍式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式扣
弛                                                                   弛
弛 忙式式式式式式式式式式式式式式式式式式式式式式式式式式式式成式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式忖   弛
弛 弛 LEFT PANEL (Data Entry)    弛 RIGHT PANEL (Prompt)         弛   弛
弛 弛                            弛                              弛   弛
弛 弛 Input:                     弛 Prompt (prompt.txt):         弛   弛
弛 弛 忙式式式式式式式式式式式式式式式式式式式式式式式式忖 弛 忙式式式式式式式式式式式式式式式式式式式式式式式式式式忖 弛   弛
弛 弛 弛                        弛 弛 弛                          弛 弛   弛
弛 弛 弛  [Input Text Area]     弛 弛 弛                          弛 弛   弛
弛 弛 弛   (100px height)       弛 弛 弛   [Prompt Editor]        弛 弛   弛
弛 弛 戌式式式式式式式式式式式式式式式式式式式式式式式式戎 弛 弛    (400px height)        弛 弛   弛
弛 弛                            弛 弛     Consolas Font        弛 弛   弛
弛 弛 Output:                    弛 弛                          弛 弛   弛
弛 弛 忙式式式式式式式式式式式式式式式式式式式式式式式式忖 弛 弛                          弛 弛   弛
弛 弛 弛                        弛 弛 弛                          弛 弛   弛
弛 弛 弛  [Output Text Area]    弛 弛 弛                          弛 弛   弛
弛 弛 弛   (100px height)       弛 弛 弛                          弛 弛   弛
弛 弛 戌式式式式式式式式式式式式式式式式式式式式式式式式戎 弛 弛                          弛 弛   弛
弛 弛                            弛 戌式式式式式式式式式式式式式式式式式式式式式式式式式式戎 弛   弛
弛 弛 Proto Output:              弛                              弛   弛
弛 弛 忙式式式式式式式式式式式式式式式式式式式式式式式式忖 弛 This content will be saved   弛   弛
弛 弛 弛                        弛 弛 to prompt.txt                弛   弛
弛 弛 弛  [Proto Output Area]   弛 弛                              弛   弛
弛 弛 弛   (100px, read-only,   弛 弛                              弛   弛
弛 弛 弛    gray background)    弛 弛                              弛   弛
弛 弛 戌式式式式式式式式式式式式式式式式式式式式式式式式戎 弛                              弛   弛
弛 弛                            弛                              弛   弛
弛 弛 Applied Prompt Numbers:    弛                              弛   弛
弛 弛 忙式式式式式式式式式式式式式式式式式式式式式式式式忖 弛                              弛   弛
弛 弛 弛 [e.g., 1,2,3]          弛 弛                              弛   弛
弛 弛 戌式式式式式式式式式式式式式式式式式式式式式式式式戎 弛                              弛   弛
弛 弛 Enter comma-separated      弛                              弛   弛
弛 弛 numbers (e.g., 1,2,3)      弛                              弛   弛
弛 戌式式式式式式式式式式式式式式式式式式式式式式式式式式式式扛式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式戎   弛
弛                                                                   弛
戍式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式扣
弛 Action Buttons (Right-aligned)                                   弛
弛                                                                   弛
弛              忙式式式式式式式式式式式式式式式式式式忖 忙式式式式式式式式忖 忙式式式式式式式式式式式式式式式式忖弛
弛              弛 Get Proto Result 弛 弛  Save  弛 弛 Clear Data     弛弛
弛              弛  (Yellow tint)   弛 弛(Green) 弛 弛 Fields (Red)   弛弛
弛              戌式式式式式式式式式式式式式式式式式式戎 戌式式式式式式式式戎 戌式式式式式式式式式式式式式式式式戎弛
戌式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式戎
```

## Color Scheme

### Status Bar
- **Background**: `#F5F5F5` (Light Gray)
- **Border**: `#DDDDDD` (Medium Gray)
- **Text**: Black (normal), Red (errors)

### Buttons
- **Get Proto Result**
  - Background: `#FFF3E5` (Light Orange)
  - Border: `#FFB74D` (Orange)

- **Save**
  - Background: `#E8F5E9` (Light Green)
  - Border: `#66BB6A` (Green)

- **Clear Data Fields**
  - Background: `#FFEBEE` (Light Red)
  - Border: `#EF5350` (Red)

### Text Boxes
- **Normal**: White background, black text
- **Proto Output (Read-only)**: `#F5F5F5` background, gray text
- **Prompt Editor**: Consolas font, size 12

## Component Sizes

### Text Boxes
- **txtInput**: Height 100px, multi-line, scrollable
- **txtOutput**: Height 100px, multi-line, scrollable
- **txtProtoOutput**: Height 100px, read-only, gray background
- **txtAppliedPromptNumbers**: Height 40px, single-line
- **txtPrompt**: Height 400px, multi-line, scrollable, Consolas font

### Buttons
- **Minimum Width**: 120px
- **Padding**: 15px horizontal, 8px vertical
- **Margin**: 5px between buttons

### Spacing
- **Margin around window**: 15px
- **Margin between components**: 5px
- **Label margin top**: 10px

## Interactions

### Button States

**Normal State**
- Full color as defined
- Cursor changes to hand

**Hover State**
- Slight brightness increase
- Tooltip may appear (future enhancement)

**Disabled State**
- Grayed out
- No interaction

**Pressed State**
- Visual feedback (slight depression)
- Executes command

### Text Box Behaviors

**Input Fields (Editable)**
- White background
- Black text
- Cursor visible
- Accept typing
- Scrollable
- Accept multi-line input

**Proto Output (Read-only)**
- Gray background (`#F5F5F5`)
- Cannot type or edit
- Can select and copy text
- Scrollable if content exceeds height

### Status Bar Updates

**On Startup**
```
Ready
Records: 0
```

**On Save Success**
```
Successfully saved! Total records: 5  (Black text)
Records: 5
```

**On Error**
```
Error: Input cannot be empty  (Red text)
Records: 4
```

**On Clear**
```
Data fields cleared  (Black text)
Records: 4
```

## Dialog Boxes

### Validation Error Dialog
```
忙式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式忖
弛 Validation Error           [X] 弛
戍式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式扣
弛                                 弛
弛  ??  Please enter an input      弛
弛      value.                     弛
弛                                 弛
弛           忙式式式式式式式式忖            弛
弛           弛   OK   弛            弛
弛           戌式式式式式式式式戎            弛
戌式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式戎
```

### Success Dialog
```
忙式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式忖
弛 Success                         [X] 弛
戍式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式扣
弛                                      弛
弛  ??  Data saved successfully!        弛
弛                                      弛
弛      Files saved to:                 弛
弛      C:\...\bin\Debug\net9.0-windows\弛
弛                                      弛
弛              忙式式式式式式式式忖              弛
弛              弛   OK   弛              弛
弛              戌式式式式式式式式戎              弛
戌式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式戎
```

### Confirm Clear Dialog
```
忙式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式忖
弛 Confirm Clear                  [X] 弛
戍式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式扣
弛                                     弛
弛  ? Clear all data entry fields     弛
弛     (excluding prompt)?             弛
弛                                     弛
弛      忙式式式式式式式式式忖  忙式式式式式式式式式忖      弛
弛      弛   Yes   弛  弛   No    弛      弛
弛      戌式式式式式式式式式戎  戌式式式式式式式式式戎      弛
戌式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式戎
```

## Keyboard Navigation

### Tab Order
1. txtInput
2. txtOutput
3. txtProtoOutput (skipped - read-only)
4. txtAppliedPromptNumbers
5. txtPrompt
6. btnGetProtoResult
7. btnSave
8. btnClear

### Shortcuts (Future Enhancement)
- **Ctrl+S**: Save
- **Ctrl+N**: Clear fields
- **F5**: Get Proto Result
- **Ctrl+,**: Focus on Prompt

## Responsive Behavior

### Window Resize
- Text boxes expand/contract with window
- Buttons remain fixed size, right-aligned
- Minimum window size prevents UI breaking
- Scroll bars appear as needed

### Content Overflow
- All text boxes show scroll bars when content exceeds visible area
- Status bar text truncates with ellipsis if too long
- Dialog boxes expand to fit content

## Accessibility Features

### Current
- High contrast text
- Clear visual hierarchy
- Keyboard navigation support
- Readable fonts

### Future Enhancements
- Screen reader support
- Adjustable font sizes
- Keyboard shortcuts
- High contrast mode

## Visual States Summary

| Element | Normal | Hover | Active | Disabled | Error |
|---------|--------|-------|--------|----------|-------|
| Input Box | White | White | White | Gray | Red Border |
| Save Button | Green Tint | Lighter | Pressed | Gray | - |
| Status Text | Black | - | - | - | Red |
| Proto Output | Gray BG | Gray BG | - | Gray BG | - |

## Best Practices for Use

### Visual Feedback
- ? Always check status bar after actions
- ? Read dialog messages carefully
- ? Observe record count changes
- ? Notice button color coding (green=safe, red=destructive, yellow=caution)

### Color Coding
- **Green**: Positive actions (Save)
- **Red**: Destructive actions (Clear)
- **Yellow/Orange**: Caution/future feature (Get Proto Result)
- **Gray**: Read-only or inactive

---

**This UI design follows modern WPF best practices and provides clear visual feedback for all user actions.**
