# UI Reference Guide

## Application Window Layout

### Window Properties
- **Title**: "LLM Data Builder"
- **Size**: 1000x700 pixels
- **Position**: Center of screen
- **Resizable**: Yes

## Visual Layout

```
��������������������������������������������������������������������������������������������������������������������������������������
�� LLM Data Builder                                        [_][��][X]��
��������������������������������������������������������������������������������������������������������������������������������������
�� Status Bar (Light Gray Background)                               ��
�� ������������������������������������������������������������������������������������������������������������������������������ ��
�� �� Ready                                                         �� ��
�� �� Records: 0                                                    �� ��
�� ������������������������������������������������������������������������������������������������������������������������������ ��
��������������������������������������������������������������������������������������������������������������������������������������
��                                                                   ��
�� ��������������������������������������������������������������������������������������������������������������������������   ��
�� �� LEFT PANEL (Data Entry)    �� RIGHT PANEL (Prompt)         ��   ��
�� ��                            ��                              ��   ��
�� �� Input:                     �� Prompt (prompt.txt):         ��   ��
�� �� ���������������������������������������������������� �� �������������������������������������������������������� ��   ��
�� �� ��                        �� �� ��                          �� ��   ��
�� �� ��  [Input Text Area]     �� �� ��                          �� ��   ��
�� �� ��   (100px height)       �� �� ��   [Prompt Editor]        �� ��   ��
�� �� ���������������������������������������������������� �� ��    (400px height)        �� ��   ��
�� ��                            �� ��     Consolas Font        �� ��   ��
�� �� Output:                    �� ��                          �� ��   ��
�� �� ���������������������������������������������������� �� ��                          �� ��   ��
�� �� ��                        �� �� ��                          �� ��   ��
�� �� ��  [Output Text Area]    �� �� ��                          �� ��   ��
�� �� ��   (100px height)       �� �� ��                          �� ��   ��
�� �� ���������������������������������������������������� �� ��                          �� ��   ��
�� ��                            �� �������������������������������������������������������� ��   ��
�� �� Proto Output:              ��                              ��   ��
�� �� ���������������������������������������������������� �� This content will be saved   ��   ��
�� �� ��                        �� �� to prompt.txt                ��   ��
�� �� ��  [Proto Output Area]   �� ��                              ��   ��
�� �� ��   (100px, read-only,   �� ��                              ��   ��
�� �� ��    gray background)    �� ��                              ��   ��
�� �� ���������������������������������������������������� ��                              ��   ��
�� ��                            ��                              ��   ��
�� �� Applied Prompt Numbers:    ��                              ��   ��
�� �� ���������������������������������������������������� ��                              ��   ��
�� �� �� [e.g., 1,2,3]          �� ��                              ��   ��
�� �� ���������������������������������������������������� ��                              ��   ��
�� �� Enter comma-separated      ��                              ��   ��
�� �� numbers (e.g., 1,2,3)      ��                              ��   ��
�� ��������������������������������������������������������������������������������������������������������������������������   ��
��                                                                   ��
��������������������������������������������������������������������������������������������������������������������������������������
�� Action Buttons (Right-aligned)                                   ��
��                                                                   ��
��              ���������������������������������������� �������������������� ��������������������������������������
��              �� Get Proto Result �� ��  Save  �� �� Clear Data     ����
��              ��  (Yellow tint)   �� ��(Green) �� �� Fields (Red)   ����
��              ���������������������������������������� �������������������� ��������������������������������������
��������������������������������������������������������������������������������������������������������������������������������������
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
����������������������������������������������������������������������
�� Validation Error           [X] ��
����������������������������������������������������������������������
��                                 ��
��  ??  Please enter an input      ��
��      value.                     ��
��                                 ��
��           ��������������������            ��
��           ��   OK   ��            ��
��           ��������������������            ��
����������������������������������������������������������������������
```

### Success Dialog
```
��������������������������������������������������������������������������������
�� Success                         [X] ��
��������������������������������������������������������������������������������
��                                      ��
��  ??  Data saved successfully!        ��
��                                      ��
��      Files saved to:                 ��
��      C:\...\bin\Debug\net9.0-windows\��
��                                      ��
��              ��������������������              ��
��              ��   OK   ��              ��
��              ��������������������              ��
��������������������������������������������������������������������������������
```

### Confirm Clear Dialog
```
������������������������������������������������������������������������������
�� Confirm Clear                  [X] ��
������������������������������������������������������������������������������
��                                     ��
��  ? Clear all data entry fields     ��
��     (excluding prompt)?             ��
��                                     ��
��      ����������������������  ����������������������      ��
��      ��   Yes   ��  ��   No    ��      ��
��      ����������������������  ����������������������      ��
������������������������������������������������������������������������������
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
