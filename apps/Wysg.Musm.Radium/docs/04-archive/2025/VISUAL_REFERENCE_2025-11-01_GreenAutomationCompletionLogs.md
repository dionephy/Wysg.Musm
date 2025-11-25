# Green Automation Completion - Visual Reference

## Status Log Examples

### Successful Automation (Green Completion)
```
Study locked
Study remark captured (42 chars)
Patient remark captured (156 chars)
Previous study added
Open study invoked
? Shortcut: Open study (new) completed successfully   ก็ GREEN (#90EE90)
```

### Failed Automation (Red Error, No Green Completion)
```
Study locked
Study remark captured (42 chars)
Module 'GetPatientRemark' failed - procedure aborted   ก็ RED (#FF5A5A)
```
*No green completion message because sequence was aborted*

### Mixed Log (All Colors)
```
New study initialized (unlocked, all toggles off, JSON cleared)   ก็ GRAY
Study locked   ก็ GRAY
Study remark captured (42 chars)   ก็ GRAY
Patient remark captured (156 chars)   ก็ GRAY
Previous study added   ก็ GRAY
? New Study completed successfully   ก็ GREEN
```

### Context-Sensitive Shortcut Names
```
# When not locked
? Shortcut: Open study (new) completed successfully   ก็ GREEN

# When locked but not opened
? Shortcut: Open study (add) completed successfully   ก็ GREEN

# When already opened
? Shortcut: Open study (after open) completed successfully   ก็ GREEN

# Send report shortcuts
? Shortcut: Send report (preview) completed successfully   ก็ GREEN
? Shortcut: Send report (reportified) completed successfully   ก็ GREEN
```

## Detection Keywords

### Green (Completion)
- Contains: `completed successfully`
- Starts with: `?` (checkmark)

### Red (Error)
- Contains: `error`, `failed`, `exception`, `validation failed`
- Does NOT contain: `completed successfully` (completion takes priority)

### Gray (Regular)
- Everything else (default color)

## UI Element
**Location**: Status bar at bottom of MainWindow

**Control**: `StatusPanel` กๆ `richStatusBox` (RichTextBox)

**Behavior**:
- Auto-scrolls to latest message
- Maintains last 50 lines in rolling buffer
- Multi-line display with color-coded Run elements

## Code Flow
```
Automation Button Click
  ก้
RunModulesSequentially(modules, "New Study")
  ก้
Execute each module in sequence
  ก้
If exception กๆ SetStatus(error, isError=true) กๆ RED, ABORT
  ก้
All modules succeed กๆ SetStatus("? New Study completed successfully", isError=false) กๆ GREEN
  ก้
StatusPanel.UpdateStatusText()
  ก้
Detect completion line กๆ Apply green color (#90EE90)
```

## Quick Reference
| Message Type | Color | Hex | Trigger |
|--------------|-------|-----|---------|
| Completion | Light Green | #90EE90 | "completed successfully" or "?" |
| Error | Red | #FF5A5A | "error", "failed", "exception" |
| Regular | Gray | #D0D0D0 | Default |
