# Fix: SetFocusSearchResultsList Element Chaining Issue (2025-01-20)

## Problem

The `SetFocusSearchResultsList` procedure worked correctly in the SpyWindow interactive execution but failed when run as an automation module. The procedure used two operations:

1. `GetSelectedElement(SearchResultsList)` ¡æ stores selected element in cache and returns cache key to `var1`
2. `ClickElementAndStay(var1)` ¡æ should click the cached element

**Error Symptom:**
```
[ProcedureExecutor][ExecuteInternal] Step 1 result: preview='(element: (no name), automationId: ListViewItem-104)', value='SelectedElement:'
[ProcedureExecutor][ExecuteInternal] Step 2 result: preview='(no element)', value=''
```

Step 2 failed to find the element even though Step 1 successfully cached it.

## Root Cause

The `ResolveElement` method in `ProcedureExecutor.cs` (headless execution) didn't have access to the `vars` dictionary when resolving variable-type arguments. The method signature was:

```csharp
private static AutomationElement? ResolveElement(ProcArg arg)
```

When `ClickElementAndStay` tried to resolve a `Var` type argument:
1. `arg.Value` contained the variable name (`"var1"`)
2. The method couldn't resolve `var1` to its actual value (`"SelectedElement:MRI Brain"`)  
3. Without the actual cache key, it couldn't look up the element in `_elementCache`

The same issue existed in SpyWindow's `ResolveElement` method, but the user had tested it in a scenario where it happened to work.

## Solution

### Changed Files

**1. apps\Wysg.Musm.Radium\Services\ProcedureExecutor.cs**
- Updated `ResolveElement` signature to accept `vars` dictionary:
  ```csharp
  private static AutomationElement? ResolveElement(ProcArg arg, Dictionary<string, string?> vars)
  ```
- Added variable resolution logic for `ArgKind.Var` type:
  ```csharp
  if (type == ArgKind.Var)
  {
      var varName = arg.Value ?? string.Empty;
      // First resolve the variable value from vars dictionary
      if (!vars.TryGetValue(varName, out var varValue) || string.IsNullOrWhiteSpace(varValue))
          return null;
      
      // Then look up the element in cache using the resolved value
      if (_elementCache.TryGetValue(varValue, out var cachedElement))
      {
          if (IsElementAlive(cachedElement))
              return cachedElement;
      }
  }
  ```
- Updated all calls to `ResolveElement` to pass `vars` dictionary

**2. apps\Wysg.Musm.Radium\Views\SpyWindow.Procedures.Exec.cs**
- Applied identical fix to SpyWindow's `ResolveElement` method for consistency

### Debug Logging Added

Added comprehensive debug output for troubleshooting variable resolution:
```csharp
Debug.WriteLine($"[ResolveElement][Var] Resolving variable '{varName}'");
Debug.WriteLine($"[ResolveElement][Var] Variable '{varName}' resolved to: '{varValue}'");
Debug.WriteLine($"[ResolveElement][Var] Found cached element for key '{varValue}'");
```

## Verification

After the fix, the procedure should work correctly:

```
[ProcedureExecutor][ExecuteInternal] Step 1/2: Op='GetSelectedElement'
[ProcedureExecutor][ExecuteInternal] Step 1 result: preview='(element: MRI Brain, automationId: ListViewItem-104)', value='SelectedElement:MRI Brain'
[ProcedureExecutor][ExecuteInternal] Stored to variable 'var1': 'SelectedElement:MRI Brain'

[ProcedureExecutor][ExecuteInternal] Step 2/2: Op='ClickElementAndStay'
[ResolveElement][Var] Resolving variable 'var1'
[ResolveElement][Var] Variable 'var1' resolved to: 'SelectedElement:MRI Brain'
[ResolveElement][Var] Found cached element for key 'SelectedElement:MRI Brain'
[ResolveElement][Var] Cached element is still alive
[ProcedureExecutor][ExecuteInternal] Step 2 result: preview='(clicked and stayed at 450,320)', value=''
```

## Impact

This fix enables:
- **Operation Chaining**: `GetSelectedElement` + `ClickElement`/`ClickElementAndStay`/`MouseMoveToElement` workflows
- **Dynamic Automation**: Click on currently selected items without hardcoded bookmarks
- **Flexible Procedures**: Build procedures that adapt to runtime UI state

## Related Features

- **FR-1173**: Custom Procedure Operation ? GetSelectedElement
- **FR-1174**: ClickElement Operation ? Accept Var Type for Element Chaining  
- **FR-1175**: Runtime Element Cache for Operation Chaining
- **FR-1101**: PACS Method ? Set focus search results list

## Testing Recommendations

Test the following scenarios:
1. Run `SetFocusSearchResultsList` from automation module (should now work)
2. Verify SpyWindow interactive execution still works  
3. Test multi-step procedures with element chaining
4. Verify element staleness detection (change selection between steps)
5. Test with different list controls (SearchResultsList, RelatedStudiesList, etc.)
