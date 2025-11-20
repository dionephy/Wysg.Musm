# Quick Reference: Alt+Arrow Navigation Map

**Date**: 2025-02-11  
**Component**: ReportInputsAndJsonPanel, CenterEditingArea

**Last Updated**: 2025-02-11 - Added editor copy-to-caret behavior

---

## Navigation Map (Visual)

```
忙式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式忖
弛                    REPORT INPUTS PANEL                       弛
戍式式式式式式式式式式式式式式式式式式式式式式成式式式式式式式式式式式式式式成式式式式式式式式式式式式式式式式式式式式式式式式扣
弛   Study Remark       弛              弛  Chief Complaint       弛
弛   txtStudyRemark     弛              弛  txtChiefComplaint     弛
弛                      弛    RIGHT     弛                        弛
弛                      弛   (copy) 式式式式⊥                        弛
弛                      弛    LEFT      弛                        弛
弛                      弛   ∠式式式式式式式式式式弛                        弛
弛         弛            弛              弛         弛              弛
弛         弛 DOWN       弛              弛         弛 DOWN         弛
弛         ⊿            弛              弛         ⊿              弛
弛                      弛              弛  Patient History       弛
弛   Findings           弛              弛  txtPatientHistory     弛
弛   txtFindings        弛              弛                        弛
弛                      弛    RIGHT     弛         ∟              弛
弛                      弛   (copy) 式式式式⊥         弛 UP           弛
弛                      弛    LEFT      弛         弛              弛
弛   txtFindingsProofread ∠式式式式式式式式式式式式弛  Patient Remark       弛
弛                      弛              弛  txtPatientRemark      弛
弛         弛            弛              弛                        弛
弛         弛 DOWN       弛              弛    LEFT (copy)         弛
弛         ⊿            弛              弛   ∠式式式式式式式式式式式式式式式式式式  弛
弛                      弛              弛                        弛
弛   Conclusion         弛              弛         弛              弛
弛   txtConclusion      弛              弛         弛 DOWN (to     弛
弛                      弛    RIGHT     弛         ⊿  editors)    弛
弛                      弛   式式式式式式式式式⊥ 弛                        弛
弛                      弛    LEFT      弛                        弛
弛   txtConclusionProofread ∠式式式式式式式式式式弛                        弛
弛                      弛              弛                        弛
戌式式式式式式式式式式式式式式式式式式式式式式扛式式式式式式式式式式式式式式扛式式式式式式式式式式式式式式式式式式式式式式式式戎

忙式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式忖
弛               EDITOR NAVIGATION (CenterEditingArea)          弛
戍式式式式式式式式式式式式式式式式式式式式式式成式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式扣
弛   editorFindings     弛   editorPreviousFindings              弛
弛   (Current)          弛   (Previous)                          弛
弛                      弛        LEFT (copy to caret)           弛
弛                      弛   ∠式式式式式式式式式式式式式式式式式式式               弛
弛                      弛        RIGHT (focus only)             弛
弛                      弛   式式式式式式式式式式式式式式式式式式式式式⊥             弛
弛                      弛                                       弛
弛   editorConclusion   弛   editorPreviousConclusion            弛
弛   (Current)          弛   (Previous)                          弛
弛                      弛        LEFT (copy to caret)           弛
弛                      弛   ∠式式式式式式式式式式式式式式式式式式式               弛
弛                      弛                                       弛
弛                      弛   editorPreviousHeader                弛
弛                      弛   (Previous)                          弛
弛                      弛        LEFT (copy to caret)           弛
弛                      弛   ∠式式式式式式式式式式式式式式式式式式式               弛
戌式式式式式式式式式式式式式式式式式式式式式式扛式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式戎
```

---

## Complete Navigation Matrix

| From                 | Direction | To                  | Copyable | Paste Location | Notes                        |
|----------------------|-----------|---------------------|----------|----------------|------------------------------|
| txtStudyRemark       | Right     | txtChiefComplaint   | ? Yes   | End            | Main workflow path           |
| txtStudyRemark       | Down      | txtFindings         | ? No    | N/A            | Skip to main report content  |
| txtChiefComplaint    | Left      | txtStudyRemark      | ? No    | N/A            | Return to source             |
| txtChiefComplaint    | Right     | txtPatientRemark    | ? No    | N/A            | Access JSON column           |
| txtChiefComplaint    | Down      | txtPatientHistory   | ? No    | N/A            | Sequential field navigation  |
| txtPatientRemark     | Left      | txtPatientHistory   | ? Yes   | End            | Copy notes to history        |
| txtPatientRemark     | Down      | editorFindings      | ? No    | N/A            | Jump to main editor          |
| txtPatientHistory    | Up        | txtChiefComplaint   | ? No    | N/A            | Return to header             |
| txtPatientHistory    | Down      | txtFindingsProofread| ? No    | N/A            | Sequential editing           |
| txtPatientHistory    | Left      | txtStudyRemark      | ? No    | N/A            | Cross-column navigation      |
| txtPatientHistory    | Right     | txtPatientRemark    | ? No    | N/A            | Access JSON column           |
| txtFindings          | Up        | txtStudyRemark      | ? No    | N/A            | Jump back to top             |
| txtFindings          | Down      | txtConclusion       | ? No    | N/A            | Sequential report sections   |
| txtFindings          | Right     | txtFindingsProofread| ? Yes   | End            | Copy to proofread field      |
| txtFindingsProofread | Up        | txtPatientHistory   | ? No    | N/A            | Return to header section     |
| txtFindingsProofread | Down      | txtConclusionProofread | ? No | N/A            | Sequential proofread editing |
| txtFindingsProofread | Left      | txtFindings         | ? No    | N/A            | Return to original           |
| txtFindingsProofread | Right     | txtPatientRemark    | ? No    | N/A            | Access JSON column           |
| txtConclusion        | Up        | txtFindings         | ? No    | N/A            | Return to findings           |
| txtConclusion        | Down      | editorFindings      | ? No    | N/A            | Jump to main editor          |
| txtConclusion        | Right     | txtConclusionProofread | ? No | N/A            | Move to proofread version    |
| txtConclusionProofread | Up      | txtFindingsProofread| ? No    | N/A            | Sequential proofread editing |
| txtConclusionProofread | Down    | editorFindings      | ? No    | N/A            | Jump to main editor          |
| txtConclusionProofread | Left    | txtConclusion       | ? No    | N/A            | Return to original           |
| txtConclusionProofread | Right   | txtPatientRemark    | ? No    | N/A            | Access JSON column           |
| **editorPreviousFindings** | **Left** | **editorFindings** | **? Yes** | **Caret** | **Copy previous findings at caret** |
| **editorPreviousConclusion** | **Left** | **editorFindings** | **? Yes** | **Caret** | **Copy previous conclusion at caret** |
| **editorPreviousHeader** | **Left** | **editorFindings** | **? Yes** | **Caret** | **Copy previous header at caret** |
| editorFindings       | Right     | editorPreviousFindings | ? No | N/A            | Navigate to previous findings |
| editorFindings       | Up/Down   | editorConclusion    | ? Yes   | End            | Vertical current report navigation |

---

## Key Navigation Patterns

### Copyable Transitions (with selection)
These transitions will copy selected text to the target field when text is selected:

**TextBox ⊥ TextBox (append at end):**
- **Study Remark ⊥ Chief Complaint** (Alt+Right) - Copy clinical info
- **Patient Remark ⊥ Patient History** (Alt+Left) - Copy notes to structured field
- **Findings ⊥ Findings (PR)** (Alt+Right) - Copy for proofreading

**Editor ⊥ Editor (insert at caret) ??:**
- **editorPreviousFindings ⊥ editorFindings** (Alt+Left) - Insert previous findings at cursor
- **editorPreviousConclusion ⊥ editorFindings** (Alt+Left) - Insert previous conclusion at cursor
- **editorPreviousHeader ⊥ editorFindings** (Alt+Left) - Insert previous header at cursor

### Non-Copyable Transitions (focus only)
All other transitions move focus without copying text, even if text is selected.

### Column-Spanning Navigation
- **Patient History** ㏒ **Study Remark** (Alt+Left/Right) - Cross main columns
- **Patient History** ㏒ **Patient Remark** (Alt+Left/Right) - Main to JSON column
- **Findings/Conclusion (PR)** ⊥ **Patient Remark** (Alt+Right) - Proofread to JSON

---

## Workflow Examples

### Example 1: Basic Report Entry
```
1. User fills txtStudyRemark with "chest pain"
2. Alt+Right (copies "chest pain" to txtChiefComplaint)
3. Alt+Down to txtPatientHistory
4. Fill patient history
5. Alt+Down to txtFindingsProofread
6. (User switches to Findings column)
7. Alt+Left to txtFindings
8. Alt+Down to txtConclusion
```

### Example 2: Proofread Workflow
```
1. User in txtFindings with selected text
2. Alt+Right (copies selection to txtFindingsProofread)
3. Edit proofread version
4. Alt+Down to txtConclusionProofread
5. Alt+Left to txtConclusion
6. Continue editing
```

### Example 3: Copy Previous Report Content at Specific Location ??
```
1. User in editorFindings, positions caret in middle of paragraph: "There is mild atrophy. |More findings..."
2. Alt+Right to editorPreviousFindings
3. Select relevant text: "No acute hemorrhage."
4. Alt+Left (text inserted at caret): "There is mild atrophy. No acute hemorrhage.|More findings..."
5. Caret positioned after inserted text for continued typing
```

### Example 4: Cross-Column Navigation
```
1. User in txtPatientHistory
2. Alt+Right to txtPatientRemark (JSON column)
3. Review/edit JSON
4. Alt+Left back to txtPatientHistory
5. Alt+Left to txtStudyRemark (cross main columns)
```

---

## Troubleshooting

### Navigation Not Working
- **Check Alt Key**: Hold Alt then press arrow key (both must be pressed together)
- **SystemKey Issue**: If Alt alone triggers (KeyEventArgs.Key == Key.System), the arrow key is in SystemKey property
- **Focus Lost**: Ensure source textbox has focus before pressing Alt+Arrow

### Copy Not Working
- **Selection Required**: For copyable transitions, text must be selected first
- **TextBox Targets**: Selected text is appended at end with newline separator
- **Editor Targets**: Selected text is inserted at current caret position (no newline added automatically)

### Unexpected Behavior
- **Wrong Target**: Check navigation map - some paths are intentionally one-way
- **Caret Position**: 
  - TextBox targets: Caret always at end of text
  - Editor targets: Caret positioned after inserted text
- **JSON Column**: Patient Remark navigation differs from other fields (isolated column)
- **Paste Location**: 
  - TextBox ⊥ TextBox: Pastes at end (legacy behavior)
  - Editor ⊥ Editor: Pastes at caret (new behavior) ??

---

## Technical Notes

### Implementation Details
- **KeyDown Handler**: Uses `PreviewKeyDown` event for Alt+Arrow detection
- **SystemKey Check**: `var actualKey = e.Key == Key.System ? e.SystemKey : e.Key;`
- **Copy Logic**: 
  - TextBox navigation: `HandleAltArrowNavigation(source, target, copyText)` - appends at end
  - Editor navigation: `HandleEditorNavigation(source, target, copyText)` - inserts at caret
- **Insertion Method**: AvalonEdit `Document.Insert(offset, text)` for atomic caret-position insertion

### Performance Considerations
- **Event Handlers**: Attached once in `Loaded` event, not per-navigation
- **No Polling**: Event-driven, no background threads
- **Minimal Overhead**: Only processes events when Alt+Arrow actually pressed
- **Atomic Insertion**: Single `Document.Insert()` call, preserves undo/redo history

---

## See Also

- **Full Specification**: `ENHANCEMENT_2025-02-11_AltArrowNavigationDirectionChanges.md`
- **Caret Position Enhancement**: `ENHANCEMENT_2025-02-11_AltLeftCopyToCaretPosition.md`
- **Implementation**: 
  - `apps\Wysg.Musm.Radium\Controls\ReportInputsAndJsonPanel.xaml.cs` - TextBox navigation
  - `apps\Wysg.Musm.Radium\Controls\CenterEditingArea.xaml.cs` - Editor navigation
