# Green Automation Completion - Visual Reference

## Status Log Examples

### Successful Automation (Green Completion)
```
Study locked
Study remark captured (42 chars)
Patient remark captured (156 chars)
Previous study added
Open study invoked
? Shortcut: Open study (new) completed successfully   �� GREEN (#90EE90)
```

### Failed Automation (Red Error, No Green Completion)
```
Study locked
Study remark captured (42 chars)
Module 'GetPatientRemark' failed - procedure aborted   �� RED (#FF5A5A)
```
*No green completion message because sequence was aborted*

### Mixed Log (All Colors)
```
New study initialized (unlocked, all toggles off, JSON cleared)   �� GRAY
Study locked   �� GRAY
Study remark captured (42 chars)   �� GRAY
Patient remark captured (156 chars)   �� GRAY
Previous study added   �� GRAY
? New Study completed successfully   �� GREEN
```

### Context-Sensitive Shortcut Names
```
# When not locked
? Shortcut: Open study (new) completed successfully   �� GREEN

# When locked but not opened
? Shortcut: Open study (add) completed successfully   �� GREEN

# When already opened
? Shortcut: Open study (after open) completed successfully   �� GREEN

# Send report shortcuts
? Shortcut: Send report (preview) completed successfully   �� GREEN
? Shortcut: Send report (reportified) completed successfully   �� GREEN
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

**Control**: `StatusPanel` �� `richStatusBox` (RichTextBox)

**Behavior**:
- Auto-scrolls to latest message
- Maintains last 50 lines in rolling buffer
- Multi-line display with color-coded Run elements

## Code Flow
```
Automation Button Click
  ��
RunModulesSequentially(modules, "New Study")
  ��
Execute each module in sequence
  ��
If exception �� SetStatus(error, isError=true) �� RED, ABORT
  ��
All modules succeed �� SetStatus("? New Study completed successfully", isError=false) �� GREEN
  ��
StatusPanel.UpdateStatusText()
  ��
Detect completion line �� Apply green color (#90EE90)
```

## Quick Reference
| Message Type | Color | Hex | Trigger |
|--------------|-------|-----|---------|
| Completion | Light Green | #90EE90 | "completed successfully" or "?" |
| Error | Red | #FF5A5A | "error", "failed", "exception" |
| Regular | Gray | #D0D0D0 | Default |
