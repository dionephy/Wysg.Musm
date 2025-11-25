# Feature: Triple-Click Line Selection in TextBoxes (2025-01-30)

## Overview
Implemented triple-click line selection functionality for all TextBox controls in the MainWindow. Users can now triple-click on any TextBox to select an entire line, improving text editing efficiency.

## User Story
**As a** radiologist entering report text  
**I want** to triple-click to select entire lines  
**So that** I can quickly select and manipulate individual lines of text without manual dragging

## Implementation Details

### Line Definition
A line is defined as text delimited by:
- **Single newlines** (`\n` or `\r\n`)
- **Start of text** (beginning of TextBox content)
- **End of text** (end of TextBox content)

### Behavior
1. **Single-click**: Places caret at click position (standard behavior)
2. **Double-click**: Selects word at click position (standard behavior)
3. **Triple-click**: Selects entire line containing the click position (new behavior)

### Technical Implementation

#### Event Handling
- **Handler**: `OnPreviewMouseLeftButtonDown` (global preview handler)
- **Trigger**: `MouseButtonEventArgs.ClickCount == 3`
- **Scope**: All TextBox controls in MainWindow and child controls
- **Detection**: Uses `FindAncestor<TextBox>()` to locate TextBox in visual tree from click source
- **Event Handling**: `e.Handled = true` prevents default triple-click behavior (select all)

#### Selection Logic
```csharp
// Find TextBox ancestor from click source
var textBox = FindAncestor<TextBox>(e.OriginalSource as DependencyObject);
if (textBox == null) return;

// Find line boundaries
int lineStart = FindLineStart(text, caretIndex);
int lineEnd = FindLineEnd(text, caretIndex);

// Select line
textBox.Select(lineStart, lineEnd - lineStart);
```

#### Boundary Detection
**Start Boundary** (`FindLineStart`):
- Searches backwards from caret position
- Stops at any newline character (`\n`)
- Returns position after the newline
- Returns `0` if no boundary found (start of text)

**End Boundary** (`FindLineEnd`):
- Searches forwards from caret position
- Stops at any newline character (`\r` or `\n`)
- Returns position before the newline
- Returns `text.Length` if no boundary found (end of text)

### Affected Components
- **MainWindow.xaml.cs**: 
  - Added `InitializeTripleClickSupport()` method
  - Added `OnPreviewMouseLeftButtonDown()` handler
  - Added `SelectParagraphAtCaret()` method (renamed but selects lines)
  - Added `FindLineStart()` helper
  - Added `FindLineEnd()` helper
  - Added `FindAncestor<T>()` helper for visual tree traversal
  - Kept old `FindParagraphStart()` and `FindParagraphEnd()` for potential future use

### Affected TextBoxes
The feature works on all `TextBox` controls in:
- Report input fields (Chief Complaint, Patient History, etc.)
- JSON editor panels
- Remark fields
- Any other TextBox in the visual tree

**Note**: The feature does NOT affect `EditorControl` instances (which use AvalonEdit and have their own selection behaviors).

## User Benefits
1. **Faster Selection**: One triple-click instead of click-drag for lines
2. **Consistent UX**: Matches behavior of many text editors
3. **Improved Workflow**: Easier to copy/delete/replace entire lines in reports
4. **Reduced Errors**: Less chance of partial selection when moving text

## Testing Scenarios
### Basic Selection
1. Open Radium application
2. Navigate to any report input field (e.g., Findings, Conclusion)
3. Type or paste multi-line text
4. Triple-click anywhere within a line
5. **Expected**: Entire line is selected (excluding the newline character)

### Boundary Cases
**Single Line (No Newlines)**:
- Triple-click selects all text

**First Line**:
- Triple-click selects from start of text to first newline

**Last Line**:
- Triple-click selects from last newline to end of text

**Empty TextBox**:
- Triple-click does nothing (no selection change)

**Empty Line**:
- Triple-click selects empty line (zero-length selection at that position)

### Mixed Line Endings
**Windows (\\r\\n)**:
- Correctly detects line boundaries
- Selection excludes the CRLF

**Unix (\\n)**:
- Correctly detects line boundaries
- Selection excludes the LF

**Mixed**:
- Handles both styles within same text

## Change History

### 2025-01-30 - Changed from Paragraph to Line Selection
**Previous Behavior**: Triple-click selected paragraphs (text delimited by double newlines `\n\n` or `\r\n\r\n`)

**New Behavior**: Triple-click selects single lines (text delimited by single newlines `\n` or `\r\n`)

**Rationale**: User feedback indicated preference for line-level selection over paragraph-level selection for typical radiology report workflows.

**Migration**: No user action required. Existing triple-click functionality now operates on single lines instead of paragraphs.

## Known Limitations
1. **EditorControl Not Supported**: Triple-click in AvalonEdit-based `EditorControl` instances uses AvalonEdit's default behavior (may select all or select line depending on editor configuration)
2. **Newline Character Not Selected**: The newline character itself is excluded from selection (selection ends before `\r` or `\n`)
3. **Non-Textual Elements**: Does not work on `RichTextBox` or other rich text controls

## Troubleshooting

### Issue: Triple-click selects word instead of line
**Symptom**: Triple-clicking behaves like double-click (selects only one word)

**Root Cause**: The event handler wasn't properly detecting TextBox controls due to WPF's internal visual tree structure. The `OriginalSource` is typically a `TextBoxView` or other internal element, not a `Run` or `TextBlock`.

**Fix Applied (2025-01-30)**: Changed detection logic from type checking to visual tree traversal:
```csharp
// Before (incorrect):
if (e.OriginalSource is not Run && e.OriginalSource is not TextBlock) return;
var textBox = FindAncestor<TextBox>(e.OriginalSource as DependencyObject);

// After (correct):
var textBox = FindAncestor<TextBox>(e.OriginalSource as DependencyObject);
if (textBox == null) return;
```

**Verification**: After this fix, triple-clicking in any TextBox should select the entire line.

## Future Enhancements (Not Implemented)
- [ ] Configurable selection mode (line vs paragraph via settings)
- [ ] Visual feedback during selection (highlight line on hover)
- [ ] Extend to `EditorControl` instances with custom line detection
- [ ] Support for selecting line including newline character (optional setting)

## Code Changes
**Files Modified**:
- `apps\Wysg.Musm.Radium\Views\MainWindow.xaml.cs`
  - Added triple-click event handling and line selection logic
  - Fixed event detection to use visual tree traversal (2025-01-30)
  - Changed from paragraph selection to line selection (2025-01-30)

**Files Updated**:
- `apps\Wysg.Musm.Radium\docs\FEATURE_2025-01-30_TripleClickParagraphSelection.md` (renamed to reflect line selection)

## Related Features
- FR-700..FR-709: Editor Phrase-Based Syntax Highlighting
- FR-1100: Foreign Textbox One-Way Sync Feature
- EditorControl: AvalonEdit-based rich text editing with completion and ghost suggestions

## Compatibility
- **WPF Version**: .NET 9 / WPF 9.0
- **OS**: Windows 10+
- **Text Controls**: Standard WPF `TextBox` only

## References
- WPF MouseButtonEventArgs.ClickCount Property
- WPF TextBox.Select Method
- Visual Tree Helper API (FindAncestor pattern)
