# Feature Specifications: Foreign Text Sync (2025-Q1)

**Archive Period**: January 2025  
**Feature Domain**: Foreign Text Synchronization & Caret Management  
**Requirement Range**: FR-1100 through FR-1136  
**Status**: Implemented and Active  

[¡ç Back to Archive Index](../README.md) | [¡æ See Implementation Plan](Plan-2025-Q1-foreign-text-sync.md)

---

## Overview

This archive documents the Foreign Text Sync feature, which enables bidirectional text synchronization between the Radium Findings editor and external textbox applications (such as Notepad). The feature includes merge-on-disable functionality and caret position preservation to maintain user workflow continuity.

## Feature Requirements

### FR-1100: Foreign Textbox Bookmark
Add `ForeignTextbox` to `UiBookmarks.KnownControl` enum to allow users to map an external textbox control for synchronization.

**Rationale**: Provides flexible targeting of any external textbox application without hardcoding specific applications.

---

### FR-1101: Text Sync Toggle Button
Add "Sync Text" toggle button in MainWindow toolbar area to enable/disable text synchronization.

**Behavior**:
- Default state: OFF (sync disabled)
- Located near "Study locked" and "Study opened" toggles
- Bound to `MainViewModel.TextSyncEnabled` property

---

### FR-1102: TextSyncService - Core Sync Engine
Implement `TextSyncService` class to manage bidirectional text synchronization using UI Automation.

**Key Features**:
- Polling-based change detection (2000ms interval)
- UIA ValuePattern for read/write operations
- Event-based change notification to dispatcher thread
- Re-entry prevention during sync operations

---

### FR-1103: Polling-Based Change Detection
Use timer-based polling to detect changes in the foreign textbox.

**Implementation**:
- Poll interval: 2000ms (2 seconds)
- Compare current text with last known value
- Only raise event when actual change detected
- Dispose timer when sync disabled

---

### FR-1104: ForeignText Property in MainViewModel
Add `ForeignText` property (read-only, string) to display synced content from foreign textbox.

**Binding**: One-way from service to ViewModel property.

---

### FR-1105: EditorForeignText in CurrentReportEditorPanel
Add read-only `EditorForeignText` control above `EditorFindings` in the same ScrollViewer.

**Behavior**:
- Visibility: Collapsed when sync OFF
- Height: Auto when sync ON
- IsReadOnly: True (display only)
- Shares vertical scrollbar with Findings editor

---

### FR-1106: FindingsText Hook for Foreign Sync
When `TextSyncEnabled` is true and user edits `FindingsText`, write changes to foreign textbox via `TextSyncService.WriteToForeignAsync()`.

---

### FR-1107: Initial Sync on Enable
When sync is enabled, copy current `FindingsText` content to foreign textbox immediately.

**Purpose**: Ensures foreign textbox reflects current editor state before bidirectional sync begins.

---

### FR-1108: UIA ValuePattern Write Method
Primary write method using `IValuePattern.SetValue()` for reliability.

**Features**:
- Checks read-only state before write
- No focus requirement
- Logs write operations
- Returns bool indicating success

---

### FR-1109: UIA ValuePattern Read Method
Primary read method using `IValuePattern.Value` property.

**Fallbacks** (in order):
1. TextPattern.DocumentRange.GetText()
2. Element.Name property

---

### FR-1110: Write Operation Re-entry Prevention
Use `_isSyncing` flag to prevent recursive sync operations.

**Protection**: Blocks `WriteToForeignAsync()` while already syncing to avoid infinite loops.

---

### FR-1111: System.Windows.Forms Reference
Add `System.Windows.Forms` assembly reference for SendKeys support (not currently used, reserved for future clipboard fallback).

---

### FR-1112: Event-Based Change Notification
Raise `ForeignTextChanged` event on dispatcher thread when foreign text changes.

**Thread Safety**: Ensures UI updates happen on correct thread via `Dispatcher.InvokeAsync()`.

---

### FR-1113: Status Messages for Sync Operations
Display user-friendly status messages for sync state changes:
- "Text sync enabled" when activated
- "Text sync disabled" when deactivated
- "Text sync disabled - foreign text merged into findings" when merge occurs

---

### FR-1114: ForeignText Property Cleared on Disable
When sync is disabled, set `ForeignText` property to empty string to clear the displayed content.

---

### FR-1115: Foreign Textbox Element Cleared on Disable
When sync is disabled, call `WriteToForeignAsync("")` to clear the foreign textbox content.

---

### FR-1116: OnForeignTextChanged Event Handler
Handle `TextSyncService.ForeignTextChanged` event to update `MainViewModel.ForeignText` property.

**Execution Context**: Dispatcher thread (ensured by service).

---

### FR-1117: TextSyncService Dispose Pattern
Implement `IDisposable` to properly clean up timer and event subscriptions.

**Disposal**: Called automatically when ViewModel is disposed.

---

### FR-1118: Debug Logging for Sync Operations
Log all sync operations to Debug output for troubleshooting:
- Read operations: text length
- Write operations: text length and success/failure
- Errors: exception messages
- State changes: enable/disable events

---

### FR-1119: Poll Callback Error Handling
Handle exceptions in poll callback gracefully without crashing the application.

**Behavior**: Log error and continue polling on next interval.

---

### FR-1120: UIA Element Resolution via Bookmark
Resolve foreign textbox element using `UiBookmarks.Resolve(KnownControl.ForeignTextbox)`.

**Null Handling**: Return null text when element not found; log warning but don't crash.

---

### FR-1123: Merge Foreign Text into Findings on Sync Disable
When sync is disabled, automatically merge foreign text into findings text with newline separator.

**Formula**: `FindingsText = ForeignText + Environment.NewLine + FindingsText`

**Conditions**: Only merge if `ForeignText` is not empty.

---

### FR-1124: Status Message for Merge Operation
Display specific status message when merge occurs: "Text sync disabled - foreign text merged into findings"

---

### FR-1125: Status Message for Non-Merge Disable
Display generic status message when no merge needed: "Text sync disabled"

---

### FR-1126: Clear ForeignText Property After Merge
After merging, set `ForeignText = string.Empty` to clear the property and hide the EditorForeignText control.

---

### FR-1127: WriteToForeignAsync Method
Implement `WriteToForeignAsync(string text)` method in `TextSyncService` to write text to foreign textbox.

**Implementation**: Use UIA ValuePattern.SetValue(); return bool indicating success.

---

### FR-1128: Foreign Element Clear After Merge
Call `WriteToForeignAsync("")` to clear foreign textbox element after merge completes.

**Pattern**: Fire-and-forget async call (no await).

---

### FR-1129: Merge Before SetEnabled(false)
Perform merge operation in `TextSyncEnabled` setter before calling `SetEnabled(false)` to stop the timer.

**Ordering**: Ensures merge completes before sync infrastructure is torn down.

---

### FR-1130: CurrentReportJson Update After Merge
Merged `FindingsText` automatically updates `CurrentReportJson` via existing property setter logic.

**Integration**: No special handling needed; property change notification propagates update.

---

### FR-1131: EditorForeignText Height Auto on Sync Enable
When sync is enabled, EditorForeignText `Height` changes from `0` to `Auto` via `DataTrigger`.

**XAML**: Style with triggers based on `TextSyncEnabled` property.

---

### FR-1132: Caret Position Preservation on Foreign Text Merge
When text sync is disabled and foreign text is merged into Findings editor, preserve the caret position at the same location relative to the existing Findings text.

**Formula**: `new_caret_offset = old_caret_offset + foreign_text_length + newline_length`

**Implementation**: Use `FindingsCaretOffsetAdjustment` property to communicate adjustment value from ViewModel to Editor control.

---

### FR-1133: Prevent Focus Stealing on Foreign Textbox Clear (Best Effort)
When text sync is disabled, clearing the foreign textbox should avoid stealing focus from the Radium application where possible.

**Behavior**:
- Clearing foreign textbox via UIA ValuePattern without calling SetFocus()
- **Note**: Some applications (e.g., Notepad) may still bring themselves to foreground when content changes programmatically
- This is application-specific behavior controlled by the target application, not by UIA
- Application focus preservation is best-effort; complete prevention is not possible with all applications

**Implementation**: WriteToForeignAsync uses ValuePattern.SetValue without preceding SetFocus() call.

**Alternatives Considered**:
- SendKeys without focus: unreliable, requires exact focus state
- Windows messages (WM_SETTEXT): application-specific, not portable
- Clipboard + Ctrl+V: too invasive, disrupts user clipboard
- Current UIA approach is the least invasive option available

---

### FR-1134: FindingsCaretOffsetAdjustment Property in MainViewModel
New property to communicate caret position adjustment from merge operation to Findings editor.

**Type**: `int` (number of characters to add to current caret position)  
**Default**: 0 (no adjustment)  
**Usage**: Set to foreign text length + newline length during merge; reset to 0 after applying

---

### FR-1135: CaretOffsetAdjustment Property in MusmEditor
New dependency property on MusmEditor control to receive caret adjustment instructions.

**Type**: `int` dependency property  
**Behavior**: When value changes to non-zero, adjust caret offset after next text update, then reset to zero  
**Implementation**: Applied in OnDocumentTextChanged after text replacement completes

---

### FR-1136: CaretOffsetAdjustment Property in EditorControl
New dependency property on EditorControl wrapper to flow caret adjustment from ViewModel bindings to inner MusmEditor.

**Type**: `int` dependency property with two-way binding  
**Behavior**: Forwards value changes to inner MusmEditor.CaretOffsetAdjustment  
**Binding**: Bound to MainViewModel.FindingsCaretOffsetAdjustment in CurrentReportEditorPanel.xaml

---

## Related Documentation

### Implementation Plans
- [Plan-2025-Q1-foreign-text-sync.md](Plan-2025-Q1-foreign-text-sync.md) - Detailed implementation approach and test plans

### Task Tracking
- Tasks T1100-T1148 cover implementation and verification
- All tasks marked complete as of 2025-01-19

### Cross-References
- UiBookmarks system: FR-516..FR-524 (archived in 2024-Q4)
- Editor infrastructure: FR-700..FR-709
- MainViewModel Editor partial: Core integration point

---

*Archived: 2025-01-19*  
*Status: Feature complete and deployed*
