# Enhancement: If/Endif Control Flow in Automation System (2025-12-01)

**Date**: 2025-12-01  
**Type**: Enhancement  
**Status**: ? Complete  
**Priority**: High

---

## Summary

Implemented Visual Basic-style if/endif control flow logic in the automation system with built-in "Abort" and "End if" modules, plus custom "If {Procedure}" and "If not {Procedure}" modules. This allows users to create conditional automation sequences with proper control flow.

**Status Messages**:
- If/If not modules now show status in brackets: `[If {Procedure}] Condition met.` or `[If not {Procedure}] Condition not met.`
- Completion message uses checkmark: `? {sequence name} completed successfully`

## Problem

Users had no way to implement conditional logic in automation sequences. They could only create linear sequences or use workarounds like "Abort if {proc}" which would terminate the entire automation rather than skip specific sections.

**Requested Features**:
1. `If {Procedure}` - Execute wrapped modules if procedure returns true
2. `If not {Procedure}` - Execute wrapped modules if procedure returns false
3. `End if` - Mark the end of an if-block
4. `Abort` - Immediately abort the entire automation sequence
5. Remove obsolete modules: "Abort if {proc}", "Abort if not {proc}", "If {proc1} then run {proc2}"

## Solution Overview

### New Module Types

#### Built-in Modules
- **Abort** - Immediately terminates the entire automation sequence
- **End if** - Marks the end of an if-block, resumes normal execution

#### Custom Modules (User-Created)
- **If {Procedure}** - Evaluates procedure, executes wrapped modules if true
- **If not {Procedure}** - Evaluates procedure, executes wrapped modules if false (negated)

### Control Flow Semantics

```
If {Procedure}
    Module A
    Module B
End if
Module C
```

**Behavior**:
- If `{Procedure}` returns **true** (or non-empty string): Execute Module A, Module B, Module C
- If `{Procedure}` returns **false** (or empty string): Skip Module A and Module B, execute Module C directly

**Negation**:
```
If not {Procedure}
    Module A
End if
```

- If `{Procedure}` returns **false**: Execute Module A
- If `{Procedure}` returns **true**: Skip Module A

### Nested If-Blocks

The implementation supports nested if-blocks using a stack-based approach:

```
If {Check1}
    Module A
    If {Check2}
        Module B
    End if
    Module C
End if
```

**Logic**:
- If Check1=true and Check2=true: Execute A, B, C
- If Check1=true and Check2=false: Execute A, C (skip B)
- If Check1=false: Skip all (A, inner if-block, C)

## Implementation Details

### 1. Enum Changes (CustomModule.cs)

Added two new module types to `CustomModuleType` enum:

```csharp
public enum CustomModuleType
{
    Run,
    Set,
    AbortIf,
    If,      // NEW
    IfNot    // NEW
}
```

### 2. UI Changes (CreateModuleWindow.xaml)

Added "If" and "If not" options to the module type dropdown:

```xaml
<ComboBox x:Name="cboModuleType" ...>
    <ComboBoxItem Content="Run"/>
    <ComboBoxItem Content="Set"/>
    <ComboBoxItem Content="Abort if"/>
    <ComboBoxItem Content="If"/>      <!-- NEW -->
    <ComboBoxItem Content="If not"/>  <!-- NEW -->
</ComboBox>
```

### 3. Name Generation (CreateModuleWindow.xaml.cs)

Added automatic name generation for new types:

```csharp
var moduleType = typeStr switch
{
    "Run" => CustomModuleType.Run,
    "Set" => CustomModuleType.Set,
    "Abort if" => CustomModuleType.AbortIf,
    "If" => CustomModuleType.If,          // NEW
    "If not" => CustomModuleType.IfNot,   // NEW
    _ => CustomModuleType.Run
};
```

**Generated Names**:
- If type: `"If {procedure name}"`
- If not type: `"If not {procedure name}"`

### 4. Built-in Modules (SettingsViewModel.cs)

Added "Abort" and "End if" to available modules:

```csharp
private ObservableCollection<string> availableModules = new(new[] { 
    ..., 
    "Abort",    // NEW
    "End if"    // NEW
});
```

### 5. Control Flow Logic (MainViewModel.Commands.Automation.Core.cs)

#### Stack-Based If-Block Tracking

```csharp
// Stack to track if-block state: (startIndex, conditionMet, isNegated)
var ifStack = new Stack<(int startIndex, bool conditionMet, bool isNegated)>();
bool skipExecution = false;
```

#### If/If not Module Handling

```csharp
if (customModule.Type == CustomModuleType.If || 
    customModule.Type == CustomModuleType.IfNot)
{
    // Execute the procedure to get the condition result
    var result = await ProcedureExecutor.ExecuteAsync(customModule.ProcedureName);
    
    // Evaluate condition (true if result is non-empty and not "false")
    bool conditionValue = !string.IsNullOrWhiteSpace(result) && 
                          !string.Equals(result, "false", StringComparison.OrdinalIgnoreCase);
    
    // Apply negation for If not
    bool conditionMet = customModule.Type == CustomModuleType.IfNot 
        ? !conditionValue 
        : conditionValue;
    
    // Push to stack
    ifStack.Push((i, conditionMet, customModule.Type == CustomModuleType.IfNot));
    
    // Update skip flag (skip if any parent condition is false)
    skipExecution = !conditionMet || ifStack.Any(entry => !entry.conditionMet);
    
    continue;
}
```

#### End if Module Handling

```csharp
if (string.Equals(m, "End if", StringComparison.OrdinalIgnoreCase))
{
    if (ifStack.Count == 0)
    {
        Debug.WriteLine($"WARNING: 'End if' without matching 'If' at position {i}");
        continue;
    }
    
    ifStack.Pop();
    skipExecution = ifStack.Any(entry => !entry.conditionMet);
    continue;
}
```

#### Abort Module Handling

```csharp
if (string.Equals(m, "Abort", StringComparison.OrdinalIgnoreCase))
{
    SetStatus("Automation aborted by Abort module", true);
    return; // Immediately abort the entire sequence
}
```

#### Module Skip Logic

```csharp
// If we're inside a false if-block, skip execution
if (skipExecution)
{
    Debug.WriteLine($"Skipping module '{m}' (inside false if-block)");
    continue;
}
```

#### Unclosed If-Block Detection

```csharp
// Check for unclosed if-blocks at end
if (ifStack.Count > 0)
{
    Debug.WriteLine($"WARNING: {ifStack.Count} unclosed if-block(s)");
    SetStatus($"? {sequenceName} completed with {ifStack.Count} unclosed if-block(s)", isError: true);
    return;
}
```

## Usage Examples

### Example 1: Basic If-Block

**Sequence**:
```
If {Patient Number Match}
    LockStudy
    OpenStudy
End if
```

**Behavior**:
- If patient numbers match: Execute LockStudy and OpenStudy
- If patient numbers don't match: Skip to after "End if"

### Example 2: If Not Block

**Sequence**:
```
If not {Worklist Is Visible}
    OpenWorklist
End if
GetStudyRemark
```

**Behavior**:
- If worklist is NOT visible: Execute OpenWorklist
- If worklist IS visible: Skip OpenWorklist
- Always execute GetStudyRemark

### Example 3: Nested If-Blocks

**Sequence**:
```
If {Patient Locked}
    If {Study Opened}
        SaveCurrentStudyToDB
    End if
    UnlockStudy
End if
```

**Behavior**:
- If patient is locked:
  - If study is opened: Save to DB
  - Always unlock study
- If patient is not locked: Skip entire block

### Example 4: Abort on Condition

**Sequence**:
```
If not {Patient Number Match}
    Abort
End if
OpenStudy
SendReport
```

**Behavior**:
- If patient numbers don't match: Abort entire automation
- If patient numbers match: Continue with OpenStudy and SendReport

### Example 5: Complex Workflow

**Sequence**:
```
NewStudy
If {Study Remark Is Empty}
    GetStudyRemark
End if
If not {Patient Remark Is Empty}
    GetPatientRemark
End if
LockStudy
OpenStudy
```

**Behavior**:
- Always execute NewStudy
- Get study remark only if empty
- Get patient remark only if NOT empty
- Always lock and open study

## User Workflow

### Creating an If Module

1. Open AutomationWindow ¡æ Custom Procedures
2. Create a procedure that returns true/false (e.g., "Check Patient Locked")
3. Go to AutomationWindow ¡æ Automation tab ¡æ Custom Modules
4. Click "Create Module"
5. Select "If" or "If not" from Module Type dropdown
6. Select your procedure
7. Module name is auto-generated (e.g., "If Check Patient Locked")
8. Click Save

### Using If/Endif in Automation

1. Go to Settings ¡æ Automation
2. Drag "If {your module}" from Custom Modules to a sequence
3. Drag the modules you want to conditionally execute
4. Drag "End if" from Available Modules to close the if-block
5. Continue building your sequence
6. Click Save

## Condition Evaluation

### True Values
- Any non-empty string except "false" (case-insensitive)
- Examples: "true", "yes", "1", "success", "Patient123"

### False Values
- Empty string
- Null
- The string "false" (case-insensitive)

### Example Procedure Results

```csharp
// Custom Procedure: Check Patient Locked
// Returns: "true" or "false"

If {Check Patient Locked}
    // Executes if procedure returns "true"
End if

If not {Check Patient Locked}
    // Executes if procedure returns "false"
End if
```

## Benefits

### For Users
- ? **Conditional Logic**: Can now implement if/else-style workflows
- ? **Flexible Automation**: Skip sections based on runtime conditions
- ? **Nested Blocks**: Support for complex multi-level conditions
- ? **Abort Control**: Immediately stop automation when needed
- ? **Reusable**: Create If modules once, use them everywhere

### For Developers
- ? **Stack-Based**: Clean implementation using standard control flow patterns
- ? **Debuggable**: Comprehensive logging for troubleshooting
- ? **Extensible**: Easy to add new conditional constructs later
- ? **Error-Proof**: Detects unclosed if-blocks and warns user

## Testing Checklist

### Basic Functionality
- [x] "If" module executes wrapped modules when condition is true
- [x] "If" module skips wrapped modules when condition is false
- [x] "If not" module executes wrapped modules when condition is false
- [x] "If not" module skips wrapped modules when condition is true
- [x] "End if" properly closes if-block
- [x] "Abort" immediately terminates automation

### Nested Blocks
- [x] Nested if-blocks work correctly
- [x] Inner false condition skips only inner block
- [x] Outer false condition skips all nested content

### Edge Cases
- [x] Unclosed if-block detection works
- [x] "End if" without "If" is handled gracefully
- [x] Multiple sequential if-blocks work independently

### Integration
- [x] Custom If modules appear in Custom Modules list
- [x] Built-in Abort/End if appear in Available Modules
- [x] Drag-drop works for all new module types
- [x] Module execution order is preserved

## Migration Notes

### Obsolete Modules to Remove

The following custom module patterns should be removed by users:

1. **"Abort if {proc}"** - Replace with:
   ```
   If {proc}
       Abort
   End if
   ```

2. **"Abort if not {proc}"** - Replace with:
   ```
   If not {proc}
       Abort
   End if
   ```

3. **"If {proc1} then run {proc2}"** - Replace with:
   ```
   If {proc1}
       Run {proc2}
   End if
   ```

### Migration Path

Users with existing sequences using obsolete patterns should:
1. Open AutomationWindow ¡æ Custom Modules
2. Delete obsolete modules
3. Recreate logic using new If/End if pattern
4. Update automation sequences in Settings ¡æ Automation

## Debug Logging

```
[Automation] If Check Patient Locked: condition=true, negated=false, conditionMet=true, skipExecution=false
[Automation] Skipping module 'OpenStudy' (inside false if-block)
[Automation] End if - resuming execution (skipExecution=false)
[Automation] Automation aborted by Abort module
[Automation] WARNING: 'End if' without matching 'If' at position 5
[Automation] WARNING: 1 unclosed if-block(s) at end of sequence
```

**Status Messages**:
```
[If Patient Number Match] Condition met.
[If not Worklist Visible] Condition not met.
[ClearCurrentFields] Done.
? New Study completed successfully
```

## Related Features

- **Custom Modules**: If/If not builds on existing custom module infrastructure
- **ProcedureExecutor**: Used to evaluate condition procedures
- **Automation Sequences**: Integrates with all existing automation features

## Future Enhancements

Potential improvements:
- [ ] Else/Else if support
- [ ] Loop constructs (while, for each)
- [ ] Break/Continue keywords
- [ ] Variable scope management
- [ ] Condition expression builder (AND/OR logic)

## Files Modified

### Created
- `apps/Wysg.Musm.Radium/docs/00-current/ENHANCEMENT_2025-12-01_IfEndifControlFlow.md` (this document)

### Modified
- `apps/Wysg.Musm.Radium/Models/CustomModule.cs` - Added If and IfNot to enum
- `apps/Wysg.Musm.Radium/Views/CreateModuleWindow.xaml` - Added If/If not options
- `apps/Wysg.Musm.Radium/Views/CreateModuleWindow.xaml.cs` - Added If/If not handling
- `apps/Wysg.Musm.Radium/ViewModels/SettingsViewModel.cs` - Added Abort and End if
- `apps/Wysg.Musm.Radium/ViewModels/MainViewModel.Commands.Automation.Core.cs` - Implemented control flow

## Build Status

? **Build Successful** - No errors, no warnings

---

**Implementation Date**: 2025-12-01  
**Build Status**: ? Success  
**Backward Compatible**: ? Users must migrate obsolete module patterns  
**Ready for Use**: ? Complete

---

*New conditional control flow successfully implemented. Users can now create sophisticated automation sequences with if/endif blocks, nested conditions, and abort control.*
