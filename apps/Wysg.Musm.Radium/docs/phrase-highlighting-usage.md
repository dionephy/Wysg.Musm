# Phrase-Based Syntax Highlighting Usage Guide

## Overview
The editor now supports real-time phrase-based syntax highlighting. Words and phrases are highlighted based on whether they exist in the current phrase snapshot:
- **Existing phrases** (in snapshot): Highlighted with `#4A4A4A` (dark gray - `Dark.Color.BorderLight`)
- **Non-existent phrases** (not in snapshot): Highlighted with red

## How to Use

### 1. In XAML
Bind the `PhraseSnapshot` property of the EditorControl to your ViewModel:

```xaml
<editor:EditorControl 
    DocumentText="{Binding CurrentText, Mode=TwoWay}"
    PhraseSnapshot="{Binding CurrentPhraseSnapshot}"
    />
```

### 2. In ViewModel
Populate the phrase snapshot from the PhraseService:

```csharp
using Wysg.Musm.Radium.Services;

public class YourViewModel : INotifyPropertyChanged
{
    private readonly IPhraseService _phraseService;
    private readonly ITenantContext _tenant;
    private IReadOnlyList<string> _currentPhraseSnapshot = Array.Empty<string>();
    
    public IReadOnlyList<string> CurrentPhraseSnapshot
    {
        get => _currentPhraseSnapshot;
        set
        {
            _currentPhraseSnapshot = value;
            OnPropertyChanged(nameof(CurrentPhraseSnapshot));
        }
    }
    
    public async Task LoadPhrasesAsync()
    {
        // Load combined phrases (global + account-specific)
        var accountId = _tenant.AccountId;
        CurrentPhraseSnapshot = await _phraseService.GetCombinedPhrasesAsync(accountId);
        
        // Or just account-specific:
        // CurrentPhraseSnapshot = await _phraseService.GetPhrasesForAccountAsync(accountId);
        
        // Or just global:
        // CurrentPhraseSnapshot = await _phraseService.GetGlobalPhrasesAsync();
    }
    
    // Call this when phrases are updated
    public async Task RefreshPhrasesAsync()
    {
        await _phraseService.RefreshPhrasesAsync(_tenant.AccountId);
        await LoadPhrasesAsync();
    }
}
```

### 3. Loading Phrases on Initialization
In your window or view code-behind:

```csharp
public partial class MainWindow : Window
{
    private readonly YourViewModel _viewModel;
    
    protected override async void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        await _viewModel.LoadPhrasesAsync();
    }
}
```

## Features

### Multi-Word Phrase Detection
The highlighter automatically detects multi-word phrases (up to 5 words). For example:
- "no evidence of" (3 words)
- "no significant abnormality detected" (4 words)

### Case-Insensitive Matching
Phrase matching is case-insensitive for better user experience:
- "Unremarkable" matches "unremarkable" in the snapshot
- "No Evidence" matches "no evidence" in the snapshot

### Real-Time Updates
When the phrase snapshot changes (e.g., after adding new phrases), the highlighting updates automatically without needing to refresh the editor.

### Performance Optimized
- Only visible text regions are processed (no full-document scan)
- Uses HashSet for O(1) phrase lookup
- Lazy evaluation when scrolling

## Future Enhancements

### SNOMED CT Color Coding (FR-709)
In the future, phrase colors will be diversified based on SNOMED CT concepts linked to each phrase:
- Anatomical structures: One color
- Clinical findings: Another color
- Procedures: Another color
- etc.

This will provide visual semantic cues to help radiologists write more structured reports.

## Example Workflow

1. User starts typing a report
2. As they type "unremarkable", it highlights in dark gray (#4A4A4A) - it's in the phrase vocabulary
3. User types "strange finding" - highlights in red because it's not a standard phrase
4. User can then add "strange finding" to the phrase database
5. After refresh, "strange finding" now highlights in dark gray

## Troubleshooting

### Phrases Not Highlighting
- Verify `PhraseSnapshot` is bound correctly in XAML
- Check that ViewModel property is populated with phrases
- Ensure phrases are loaded after account/tenant initialization

### Performance Issues
- Phrase snapshot should contain < 1000 phrases for optimal performance
- If using more, consider filtering or pagination
- Profile the phrase matching logic if issues persist

### Colors Not Matching Theme
- Colors are hardcoded to `#4A4A4A` (existing) and red (missing)
- To customize, modify `PhraseHighlightRenderer` constructor in `src/Wysg.Musm.Editor/Ui/PhraseHighlightRenderer.cs`

## Technical Details

### Architecture
- **Renderer**: `PhraseHighlightRenderer` implements `IBackgroundRenderer`
- **Layer**: `KnownLayer.Background` (behind text for readability)
- **Lifecycle**: Initialized in `EditorControl` constructor, disposed in `OnUnloaded`
- **Thread Safety**: All operations on UI thread via WPF binding system

### Files Modified
- `src/Wysg.Musm.Editor/Ui/PhraseHighlightRenderer.cs` (new)
- `src/Wysg.Musm.Editor/Controls/EditorControl.View.cs` (modified)

### Dependencies
- ICSharpCode.AvalonEdit (for rendering infrastructure)
- System.Collections.Generic (for phrase snapshot)
