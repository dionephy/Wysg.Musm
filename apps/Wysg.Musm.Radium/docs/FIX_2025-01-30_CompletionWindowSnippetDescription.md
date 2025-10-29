# FIX: Completion Window Snippet Description Display (2025-01-30)

## Issue
Snippet descriptions were not appearing in the completion dropdown list. Users only saw the trigger text (e.g., "ngi") without the associated description, making it difficult to identify the purpose of each snippet.

## Root Cause
The `MusmCompletionWindow` was binding the ListBox item template to the `Text` property instead of the `Content` property:

```csharp
// BEFORE (incorrect)
f.SetBinding(TextBlock.TextProperty, new Binding("Text"));
```

This caused the completion window to display:
- **Snippets**: Only trigger text (e.g., "ngi")
- **Hotkeys**: Only trigger text (e.g., "noaa")  
- **Phrases**: Phrase text (correct)

However, `MusmCompletionData.Snippet()` factory sets `Content` to the full display string `"{trigger} ¡æ {description}"` (e.g., "ngi ¡æ no gross ischemia"), while `Text` contains only the trigger for filtering purposes.

## Solution
Changed the item template binding from `Text` to `Content`:

```csharp
// AFTER (correct)
f.SetBinding(TextBlock.TextProperty, new Binding("Content"));
```

## Result
The completion window now displays:
- **Snippets**: `"{trigger} ¡æ {description}"` (e.g., "ngi ¡æ no gross ischemia")
- **Hotkeys**: Trigger text only (unchanged, as `Content` = trigger)
- **Phrases**: Phrase text (unchanged)

## Implementation Details

### Files Modified
- `src\Wysg.Musm.Editor\Completion\MusmCompletionWindow.cs` (line ~108)

### Code Change
```csharp
// TryApplyDarkTheme() method
var dt = new DataTemplate();
var f = new FrameworkElementFactory(typeof(TextBlock));
f.SetBinding(TextBlock.TextProperty, new Binding("Content")); // ? Changed from "Text"
f.SetValue(TextBlock.MarginProperty, new Thickness(0));
f.SetValue(TextBlock.PaddingProperty, new Thickness(0));
f.SetValue(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis);
f.SetValue(TextBlock.ForegroundProperty, new SolidColorBrush(Color.FromRgb(220,220,220)));
dt.VisualTree = f;
lb.ItemTemplate = dt;
```

### How It Works
1. **MusmCompletionData.Snippet()** creates completion items with:
   - `Text = shortcut` (for filtering, e.g., "ngi")
   - `Content = "{shortcut} ¡æ {description}"` (for display, e.g., "ngi ¡æ no gross ischemia")

2. **MusmCompletionWindow** binds to `Content` to show the full display string

3. **CompletionList filtering** still uses `Text` property (internal AvalonEdit behavior)

## Testing
- [x] Build succeeded with no errors
- [x] Snippet completion shows trigger ¡æ description format
- [x] Filtering still works by trigger text only
- [x] Hotkey completion unchanged (Content = Text for hotkeys)
- [x] Phrase completion unchanged (Content = Text for phrases)

## Related
- **Feature**: Snippet system with placeholder expansion
- **Design**: Two-property approach (Text for filtering, Content for display)
- **User Request**: Display description in dropdown for snippet identification
