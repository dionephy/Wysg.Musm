# DEBUG GUIDE: Orientation-Aware Alt+Up Navigation

**Date**: 2025-01-30  
**Component**: CenterEditingArea, MainWindow  
**Issue**: Alt+Up navigation from EditorFindings not routing correctly

---

## Debug Logging Added

Added comprehensive debug logging to diagnose orientation detection and panel selection issues.

---

## How to Debug

### 1. Enable Debug Output

In Visual Studio:
1. Open **Output** window (View ¡æ Output or Ctrl+Alt+O)
2. Select **Debug** from the "Show output from:" dropdown
3. Run the application in Debug mode (F5)

### 2. Trigger the Navigation

1. Focus **EditorFindings** in the main editing area
2. Press **Alt+Up**
3. Observe the debug output in the Output window

### 3. Analyze the Debug Output

The debug log will show:

```
[CenterEditingArea] HandleOrientationAwareUpNavigation:
  - isLandscape: True/False
  - Landscape/Portrait branch: txtPatientHistory=True/False
  - Focusing Patient History (landscape/portrait) OR Patient History textbox not found

[MainWindow] GetPatientHistoryTextBox called:
  - Window dimensions: 1400x850
  - isLandscape: True/False
  - gridSide.ActualWidth: 600 (or -1 if null)
  - gridTop.ActualHeight: 200 (or -1 if null)
  - Landscape/Portrait: Checking gridSideTop/gridTopChild
  - gridSideTop/gridTopChild.FindName result: True/False
  - Found in gridSideTop/gridTopChild: IsVisible=True, ActualWidth=300
  - Final result: FOUND or NULL
```

---

## Diagnostic Checklist

### Problem: Navigation goes to wrong panel

**Check these values in the debug output:**

1. **Is `isLandscape` correct?**
   - ? Expected: `True` when Width ¡Ã Height, `False` otherwise
   - ? If wrong: Orientation detection logic is broken

2. **Is `gridSide.ActualWidth` > 0 in landscape?**
   - ? Expected: > 0 in landscape mode
   - ? If 0 or -1: `gridSide` is not being rendered/sized

3. **Is `gridTop.ActualHeight` > 0 in portrait?**
   - ? Expected: > 0 in portrait mode
   - ? If 0 or -1: `gridTop` is not being rendered/sized

4. **Is `FindName("txtPatientHistory")` finding the control?**
   - ? Expected: `True` in active panel
   - ? If `False`: Control not in visual tree yet

5. **Is `txtPatientHistory.IsVisible` = True?**
   - ? Expected: `True` when found
   - ? If `False`: Control is hidden or not rendered

6. **Is `txtPatientHistory.ActualWidth` > 0?**
   - ? Expected: > 0 when visible
   - ? If 0: Control not laid out yet

---

## Common Issues & Solutions

### Issue 1: `gridSide.ActualWidth` is 0 in landscape

**Symptom**: Landscape mode detected, but `gridSide.ActualWidth=0`

**Cause**: `gridSide` Width binding hasn't evaluated yet

**Solution**: Check XAML binding:
```xml
<Grid Name="gridSide" Height="{Binding ElementName=gridCenter, Path=Height}">
    <Grid.Width>
      <MultiBinding Converter="{StaticResource WidthSubtract}">
          <Binding RelativeSource="{RelativeSource AncestorType=Window}" Path="ActualWidth"/>
      <Binding ElementName="gridCenter" Path="ActualWidth"/>
        </MultiBinding>
    </Grid.Width>
</Grid>
```

**Workaround**: Add small delay before navigation to allow layout to complete.

---

### Issue 2: `FindName` returns `null`

**Symptom**: `gridSideTop.FindName("txtPatientHistory")` returns `null`

**Cause**: Control not in visual tree (may be inside collapsed template)

**Solution**: Ensure `txtPatientHistory` has `x:Name` in XAML:
```xml
<TextBox x:Name="txtPatientHistory" ... />
```

**Debug**: Check if control exists:
```csharp
var allControls = FindVisualChildren<TextBox>(gridSideTop);
foreach (var tb in allControls)
{
    Debug.WriteLine($"Found TextBox: {tb.Name}");
}
```

---

### Issue 3: Control found but `IsVisible=False`

**Symptom**: `txtPatientHistory != null` but `IsVisible=False`

**Cause**: Parent container is collapsed or hidden

**Solution**: Check parent visibility chain:
```csharp
var parent = txtPatientHistory;
while (parent != null)
{
    Debug.WriteLine($"{parent.GetType().Name}.Visibility={((FrameworkElement)parent).Visibility}");
    parent = VisualTreeHelper.GetParent(parent);
}
```

---

### Issue 4: Both `gridSide` and `gridTop` have ActualWidth/Height > 0

**Symptom**: Both panels are rendered simultaneously

**Cause**: Layout is positioning both panels, not just the active one

**Expected Behavior**: 
- **Landscape**: `gridSide` positioned beside `gridCenter`, `gridTop` may still have height but positioned off-screen
- **Portrait**: `gridTop` positioned above `gridCenter`, `gridSide` may still have width but positioned off-screen

**Solution**: Use visibility or actual rendered position instead of dimensions:
```csharp
// Check if panel is actually visible on screen
var bounds = gridSide.TransformToAncestor(this).TransformBounds(new Rect(0, 0, gridSide.ActualWidth, gridSide.ActualHeight));
bool isOnScreen = bounds.IntersectsWith(new Rect(0, 0, ActualWidth, ActualHeight));
```

---

## Next Steps

Based on debug output, you can:

1. **Verify orientation detection**: Check if `ActualWidth >= ActualHeight` matches visual layout
2. **Verify panel selection**: Check if correct panel has `ActualWidth/Height > 0`
3. **Verify control location**: Check if `FindName` finds control in expected panel
4. **Verify visibility**: Check if control is actually visible and laid out

---

## Removing Debug Logging

Once issue is resolved, search for these debug statements and remove/comment out:

**In MainWindow.xaml.cs**:
- Search for: `System.Diagnostics.Debug.WriteLine($"[MainWindow] GetPatientHistoryTextBox`

**In CenterEditingArea.xaml.cs**:
- Search for: `System.Diagnostics.Debug.WriteLine($"[CenterEditingArea] HandleOrientationAwareUpNavigation`

Or use conditional compilation:
```csharp
#if DEBUG
    System.Diagnostics.Debug.WriteLine(...);
#endif
```

---

*Debug guide created on 2025-01-30*
