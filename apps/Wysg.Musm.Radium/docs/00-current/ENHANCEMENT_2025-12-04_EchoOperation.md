# Enhancement: Echo Operation for Property Pass-Through

**Date**: 2025-12-04  
**Status**: ? Completed  
**Build**: ? Success

---

## Summary

Added an `Echo` operation that serves as a simple pass-through, returning the input value unchanged. This is useful for capturing built-in properties into procedure variables without any transformation.

---

## Problem Statement

When using built-in properties (like "Current Patient Name", "Study Locked", etc.) in procedures, there was no simple way to capture their values into a procedure variable. Users had to use workarounds like:

- `Trim` for string properties (which works but is semantically incorrect)
- `IsMatch` for boolean properties (which requires comparing to "true")
- `Not` twice for booleans (awkward)

---

## Solution

Added a new `Echo` operation that:
- Accepts any value (String or Var type)
- Returns the value unchanged
- Works for both string and boolean property types
- Provides clear semantic meaning: "capture this value"

---

## Usage Examples

### Capture a String Property

```
Operation: Echo
Arg1 Type: Var
Arg1 Value: Current Patient Name
Output: var1 ¡æ "John Doe"
```

### Capture a Boolean Property

```
Operation: Echo
Arg1 Type: Var
Arg1 Value: Study Locked
Output: var1 ¡æ "true" or "false"
```

### Capture Editor Text

```
Operation: Echo
Arg1 Type: Var
Arg1 Value: Current Findings
Output: var1 ¡æ (current findings text)
```

### Pass Through a Literal Value

```
Operation: Echo
Arg1 Type: String
Arg1 Value: Hello World
Output: var1 ¡æ "Hello World"
```

---

## Implementation Details

### Files Modified

1. **`apps/Wysg.Musm.Radium/Views/AutomationWindow.OperationItems.xaml`**
   - Added `Echo` to operations dropdown

2. **`apps/Wysg.Musm.Radium/Services/OperationExecutor.cs`**
   - Added routing for `Echo` operation

3. **`apps/Wysg.Musm.Radium/Services/OperationExecutor.StringOps.cs`**
   - Added `ExecuteEcho()` implementation

4. **`apps/Wysg.Musm.Radium/Views/AutomationWindow.Procedures.Exec.cs`**
   - Added argument configuration for `Echo` operation

---

## Operation Specification

| Property | Value |
|----------|-------|
| **Name** | Echo |
| **Category** | String Operations |
| **Arg1** | String or Var (the value to pass through) |
| **Arg2** | Disabled |
| **Arg3** | Disabled |
| **Output** | The input value, unchanged |
| **Preview** | Truncated value (max 50 chars) or "(empty)" |

---

## Use Cases

### 1. Capture Built-in Properties for Later Use

```
Echo(Study Locked) ¡æ var1           // Capture current lock state
... other operations ...
IsMatch(var1, "true") ¡æ var5        // Check if was locked earlier
```

### 2. Initialize Variables with Default Values

```
Echo("default value") ¡æ var1        // Set initial value
... conditional logic ...
```

### 3. Pass Properties Between Procedures

When you need a property value available at a specific step:

```
Echo(Current Patient Number) ¡æ var1
Echo(Current Study Datetime) ¡æ var2
... use var1 and var2 in later operations ...
```

### 4. Debug/Inspect Property Values

```
Echo(Current Reportified) ¡æ var1    // See the current state in OutputPreview
```

---

## Comparison with Alternatives

| Scenario | Before (Workaround) | After (Echo) |
|----------|---------------------|--------------|
| Capture string property | `Trim(Var)` | `Echo(Var)` |
| Capture boolean property | `IsMatch(Var, "true")` | `Echo(Var)` |
| Pass through literal | `Trim("value")` | `Echo("value")` |

**Benefits of Echo:**
- ? Clear semantic meaning
- ? Works for all types
- ? No unintended side effects
- ? Single operation, no workarounds

---

## Related Features

- **Built-in Property Read Support** (2025-12-04): Properties can now be selected in Var dropdown
- **CustomModuleProperties.AllReadableProperties**: List of available properties
- **Trim/IsMatch/Not**: Alternative operations (less semantic)

---

## Testing

### Test Cases

1. **String property pass-through**
   - Echo with "Current Patient Name" ¡æ returns patient name

2. **Boolean property pass-through**
   - Echo with "Study Locked" ¡æ returns "true" or "false"

3. **Literal string pass-through**
   - Echo with String "test" ¡æ returns "test"

4. **Empty value handling**
   - Echo with empty value ¡æ returns empty string, preview shows "(empty)"

5. **Long value truncation in preview**
   - Echo with 100-char string ¡æ preview shows first 47 chars + "..."

---

## Build Status

? Build successful with no errors
