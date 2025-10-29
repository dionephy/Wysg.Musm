# UI Reference Guide

## Application Window Layout

### Window Properties
- **Title**: "LLM Data Builder"
- **Size**: 1000x700 pixels
- **Position**: Center of screen
- **Resizable**: Yes
- **Theme**: Dark theme (default)
- **Always on Top**: Optional (checkbox in status bar)

## Visual Layout

```
����������������������������������������������������������������������������������������������������������������������������������������������������������������������������������������������
�� LLM Data Builder                                        [_][��][X]��
����������������������������������������������������������������������������������������������������������������������������������������������������������������������������������������������
�� Status Bar (Dark Background #252526)                          �� Always on Top ��
�� ���������������������������������������������������������������������������������������������������������������������������������������������������������������������������������� ��
�� �� Ready                                                         �� ��
�� �� Records: 0                                                    �� ��
�� ���������������������������������������������������������������������������������������������������������������������������������������������������������������������������������� ��
����������������������������������������������������������������������������������������������������������������������������������������������������������������������������������������������
��                                                                   ��
�� ����������������������������������������������������������������������������������������������������������������������������������������������������������������������������������   ��
�� �� LEFT PANEL (Data Entry)    �� RIGHT PANEL (Prompt)         ��   ��
�� ��                            ��                              ��   ��
�� �� Input:                     �� Prompt (prompt.txt):         ��   ��
�� �� �������������������������������������������������������� �� �������������������������������������������������������� ��   ��
�� �� ��                        �� �� ��                          �� ��   ��
�� �� ��  [Input Text Area]     �� �� ��                          �� ��   ��
�� �� ��   (100px height)       �� �� ��   [Prompt Editor]        �� ��   ��
�� �� �������������������������������������������������������� �� ��    (400px height)        �� ��   ��
�� ��                            �� ��     Consolas Font        �� ��   ��
�� �� Output:                    �� ��                          �� ��   ��
�� �� �������������������������������������������������������� �� ��                          �� ��   ��
�� �� ��                        �� �� ��                          �� ��   ��
�� �� ��  [Output Text Area]    �� �� ��                          �� ��   ��
�� �� ��   (100px height)       �� �� ��                          �� ��   ��
�� �� �������������������������������������������������������� �� ��                          �� ��   ��
�� ��                            �� �������������������������������������������������������� ��   ��
�� �� Proto Output:              ��                              ��   ��
�� �� �������������������������������������������������������� �� This content will be saved   ��   ��
�� �� ��                        �� �� to prompt.txt                ��   ��
�� �� ��  [Proto Output Area]   �� ��                              ��   ��
�� �� ��   (100px, read-only,   �� ��                              ��   ��
�� �� ��    darker background)  �� ��                              ��   ��
�� �� �������������������������������������������������������� ��                              ��   ��
�� ��                            ��                              ��   ��
�� �� Applied Prompt Numbers:    ��                              ��   ��
�� �� �������������������������������������������������������� ��                              ��   ��
�� �� �� [e.g., 1,2,3]          �� ��                              ��   ��
�� �� �������������������������������������������������������� ��                              ��   ��
�� �� Enter comma-separated      ��                              ��   ��
�� �� numbers (e.g., 1,2,3)      ��                              ��   ��
�� ����������������������������������������������������������������������������������������������������������������������������������������������������������������������������������   ��
��                                                                   ��
����������������������������������������������������������������������������������������������������������������������������������������������������������������������������������������������
�� Action Buttons (Right-aligned)                                   ��
��                                                                   ��
��              ���������������������������������������� �������������������� ������������������������������������������
��              �� Browse Data      �� �� Cleanup Blank Records �� �� Get Proto Result �� ��  Save  �� �� Clear Data     ����
��              ��   (Blue)        �� ��      (Yellow)          �� ��  (Yellow border) �� ��(Green) �� �� Fields (Red)   ����
��              ���������������������������������������� �������������������� ������������������������������������������
����������������������������������������������������������������������������������������������������������������������������������������������������������������������������������������������
```

## Color Scheme - Dark Theme

### Main Colors
- **Background**: `#1E1E1E` (Dark Gray)
- **Surface**: `#252526` (Slightly Lighter Gray)
- **Surface Highlight**: `#2D2D30` (Medium Gray)
- **Border**: `#3E3E42` (Light Gray)
- **Text**: `#CCCCCC` (Light Gray)
- **Text Secondary**: `#9E9E9E` (Medium Gray)

### Accent Colors
- **Accent**: `#007ACC` (Blue)
- **Error**: `#F48771` (Red)
- **Success**: `#73C991` (Green)
- **Warning**: `#CCA700` (Yellow)

### Component Colors

#### Status Bar
- **Background**: `#252526` (Surface)
- **Border**: `#3E3E42` (Border)
- **Text**: `#CCCCCC` (normal), `#F48771` (errors)
- **Secondary Text**: `#9E9E9E`

#### Buttons
- **Browse Data**
  - Background: `#2D2D30`
  - Border: `#007ACC` (Accent/Blue)
  - Text: `#CCCCCC`

- **Cleanup Blank Records**
  - Background: `#2D2D30`
  - Border: `#CCA700` (Warning/Yellow)
  - Text: `#CCCCCC`
  - Tooltip: "Remove all records with empty Input or Output fields (creates backup first)"

- **Get Proto Result**
  - Background: `#2D2D30`
  - Border: `#CCA700` (Warning/Yellow)
  - Text: `#CCCCCC`

- **Save**
  - Background: `#2D2D30`
  - Border: `#73C991` (Success/Green)
  - Text: `#CCCCCC`

- **Clear Data Fields**
  - Background: `#2D2D30`
  - Border: `#F48771` (Error/Red)
  - Text: `#CCCCCC`

#### Button States
- **Hover**: `#3E3E42`
- **Pressed**: `#007ACC`

#### Text Boxes
- **Background**: `#252526` (Surface)
- **Text**: `#CCCCCC`
- **Border**: `#3E3E42`
- **Caret**: `#CCCCCC`
- **Prompt Editor**: Consolas font, size 12

#### Checkbox
- **Text**: `#CCCCCC`
- **Accent Color**: `#007ACC` (when checked)

## Component Sizes

### Text Boxes
- **txtInput**: Height 100px, multi-line, scrollable
- **txtOutput**: Height 100px, multi-line, scrollable
- **txtProtoOutput**: Height 100px, read-only, darker background
- **txtAppliedPromptNumbers**: Height 40px, single-line
- **txtPrompt**: Height 400px, multi-line, scrollable, Consolas font

### Buttons
- **Minimum Width**: 120px
- **Padding**: 15px horizontal, 8px vertical
- **Margin**: 5px between buttons
- **Corner Radius**: 2px

### Spacing
- **Margin around window**: 15px
- **Margin between components**: 5px
- **Label margin top**: 10px

## New Feature: Always on Top

### Location
- Status bar, top-right corner
- Next to the status text and record count

### Behavior
- **Unchecked (default)**: Window behaves normally
- **Checked**: Window stays on top of all other windows
- **Status Message**: Displays confirmation in status bar when toggled

### Use Cases
- Referencing other applications while entering data
- Keeping the application visible during multi-tasking
- Working with multiple monitors

## Interactions

### Button States

**Normal State**
- Background: `#2D2D30`
- Border colored by function
- Text: `#CCCCCC`
- Cursor changes to hand

**Hover State**
- Background: `#3E3E42`
- Border maintains color
- Text: `#CCCCCC`

**Pressed State**
- Background: `#007ACC`
- Visual feedback
- Executes command

### Text Box Behaviors

**Input Fields (Editable)**
- Dark background (`#252526`)
- Light text (`#CCCCCC`)
- Cursor visible
- Accept typing
- Scrollable
- Accept multi-line input

**Proto Output (Read-only)**
- Same dark background as other fields
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
Successfully saved! Total records: 5  (Light Gray text)
Records: 5
```

**On Error**
```
Error: Input cannot be empty  (Red text #F48771)
Records: 4
```

**On Clear**
```
Data fields cleared  (Light Gray text)
Records: 4
```

**On Always on Top Toggle**
```
Window is now always on top  (Light Gray text)
or
Window is no longer always on top  (Light Gray text)
```

## Dialog Boxes

### Validation Error Dialog
```
��������������������������������������������������������������������������������������������
�� Validation Error           [X] ��
��������������������������������������������������������������������������������������������
��                                 ��
��  ?  Please enter an input      ��
��      value.                     ��
��                                 ��
��           ������������������������            ��
��           ��   OK   ��            ��
��           ������������������������            ��
��������������������������������������������������������������������������������������������
```

### Success Dialog
```
������������������������������������������������������������������������������������������������������������
�� Success                         [X] ��
������������������������������������������������������������������������������������������������������������
��                                      ��
��  ?  Data saved successfully!        ��
��                                      ��
��      Files saved to:                 ��
��      C:\...\bin\Debug\net9.0-windows\��
��                                      ��
��              ������������������������              ��
��              ��   OK   ��              ��
��              ������������������������              ��
������������������������������������������������������������������������������������������������������������
```

### Confirm Clear Dialog
```
����������������������������������������������������������������������������������������������������
�� Confirm Clear                  [X] ��
����������������������������������������������������������������������������������������������������
��                                     ��
��  ? Clear all data entry fields     ��
��     (excluding prompt)?             ��
��                                     ��
��      ������������������������  ������������������������      ��
��      ��   Yes   ��  ��   No    ��      ��
��      ������������������������  ������������������������      ��
����������������������������������������������������������������������������������������������������
```

## Keyboard Navigation

### Tab Order
1. txtInput
2. txtOutput
3. txtProtoOutput (skipped - read-only)
4. txtAppliedPromptNumbers
5. txtPrompt
6. btnBrowseData
7. btnGetProtoResult
8. btnSave
9. btnClear
10. chkAlwaysOnTop

### Shortcuts (Future Enhancement)
- **Ctrl+B**: Browse Data
- **Ctrl+S**: Save
- **Ctrl+N**: Clear fields
- **F5**: Get Proto Result
- **Ctrl+T**: Toggle Always on Top
- **Ctrl+,**: Focus on Prompt

## Responsive Behavior

### Window Resize
- Text boxes expand/contract with window
- Buttons remain fixed size, right-aligned
- Minimum window size prevents UI breaking
- Scroll bars appear as needed
- Always on Top checkbox remains in top-right

### Content Overflow
- All text boxes show scroll bars when content exceeds visible area
- Status bar text truncates with ellipsis if too long
- Dialog boxes expand to fit content

## Accessibility Features

### Current
- Dark theme reduces eye strain
- High contrast text for readability
- Clear visual hierarchy
- Keyboard navigation support
- Readable fonts
- Color-coded borders for button functions

### Future Enhancements
- Light theme toggle
- Screen reader support
- Adjustable font sizes
- Additional keyboard shortcuts
- High contrast mode

## Visual States Summary

| Element | Normal | Hover | Active | Disabled | Error |
|---------|--------|-------|--------|----------|-------|
| Input Box | Dark BG | Dark BG | Dark BG | Darker | Red Border |
| Save Button | Dark BG + Green Border | Hover BG | Accent BG | Gray | - |
| Status Text | Light Gray | - | - | - | Red |
| Proto Output | Dark BG | Dark BG | - | Dark BG | - |
| Checkbox | Unchecked | Hover | Checked (Blue) | Gray | - |

## Best Practices for Use

### Visual Feedback
- ? Always check status bar after actions
- ? Read dialog messages carefully
- ? Observe record count changes
- ? Notice button border color coding (green=safe, red=destructive, yellow=caution)
- ? Use Always on Top when multitasking

### Color Coding
- **Green Border**: Positive actions (Save)
- **Red Border**: Destructive actions (Clear)
- **Yellow Border**: Caution/future feature (Get Proto Result)
- **Blue**: Accent color for interactive elements

### Dark Theme Benefits
- Reduced eye strain during extended use
- Better visibility in low-light environments
- Modern, professional appearance
- Consistent with modern development tools

---

**This UI design follows modern WPF best practices with a contemporary dark theme and provides clear visual feedback for all user actions.**
