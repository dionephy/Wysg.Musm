## Change Log Addition (2025-10-19 ? Foreign Text Merge Caret Preservation and Focus Management)
- **User Request 1**: On sync text OFF, when merging foreign text into Findings editor, preserve the caret position at the same location relative to the existing Findings text (i.e., current_caret_offset + foreign_text_length + newline_length).
- **User Request 2**: When clearing the foreign textbox during merge, prevent it from stealing focus from the Radium application.
- **Solution**: Implemented caret offset adjustment mechanism using ViewModel property and Editor control binding; removed SetFocus() call from WriteToForeignAsync.

### Implementation Details

**Caret Preservation Flow**:
1. When TextSyncEnabled is set to false and ForeignText is not empty:
   - Calculate adjustment: `foreignLength = ForeignText.Length + Environment.NewLine.Length`
   - Merge text: `FindingsText = ForeignText + newline + FindingsText`
   - Set adjustment property: `FindingsCaretOffsetAdjustment = foreignLength`
   - Clear foreign text property and element

2. EditorFindings binding:
   - Bound to `FindingsCaretOffsetAdjustment` via XAML one-way binding
   - Property change flows through EditorControl �� MusmEditor
   - MusmEditor stores adjustment in dependency property

3. Caret adjustment application:
   - OnDocumentTextChanged in MusmEditor detects non-zero adjustment
   - After text update completes (deferred via Dispatcher.BeginInvoke):
     - Calculate new caret position: `newCaret = oldCaret + adjustment`
     - Clamp to document length
     - Set CaretOffset to new position
     - Reset CaretOffsetAdjustment to 0

**Focus Prevention**:
- WriteToForeignAsync method updated to never call SetFocus()
- UIA ValuePattern.SetValue() works without focus in most implementations
- Prevents foreign textbox window from coming to foreground
- User maintains focus in Radium application

### Code Changes Summary
**Files Modified**:
1. `MainViewModel.cs`: Added FindingsCaretOffsetAdjustment property; calculate adjustment during merge
2. `MusmEditor.cs`: Added CaretOffsetAdjustment DP; apply adjustment in OnDocumentTextChanged after text updates
3. `EditorControl.View.cs`: Added CaretOffsetAdjustment DP; forward to inner MusmEditor
4. `CurrentReportEditorPanel.xaml`: Added CaretOffsetAdjustment binding to EditorFindings
5. `TextSyncService.cs`: Removed SetFocus() call from WriteToForeignAsync
6. `Spec.md`: Added FR-1132 through FR-1136
7. `Tasks.md`: To be updated with T1132-T1145

### Related Features
- Extends FR-1123..FR-1131 (Foreign Text Merge on Sync Disable)
- Complements FR-1100..FR-1122 (Foreign Textbox One-Way Sync Feature)
- Uses MusmEditor CaretOffsetBindable infrastructure (existing)
