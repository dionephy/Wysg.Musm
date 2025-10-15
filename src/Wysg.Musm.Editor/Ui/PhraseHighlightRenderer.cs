using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace Wysg.Musm.Editor.Ui;

/// <summary>
/// Background renderer that highlights phrases based on a snapshot list with SNOMED semantic tag colors.
/// - Phrases with SNOMED mappings: colored based on semantic tag (body structure, disorder, etc.)
/// - Phrases in snapshot without mappings: show as #4A4A4A (BorderLight color)
/// - Phrases not in snapshot: show as red
/// </summary>
public sealed class PhraseHighlightRenderer : IBackgroundRenderer, IDisposable
{
    private readonly TextView _view;
    private readonly Func<IReadOnlyList<string>> _getSnapshotPhrases;
    private readonly Func<IReadOnlyDictionary<string, string?>>? _getSemanticTags;
    private readonly Brush _existingPhraseBrush;
    private readonly Brush _missingPhraseBrush;
    
    // SNOMED semantic tag color brushes
    private readonly Brush _bodyStructureBrush;
    private readonly Brush _findingBrush;
    private readonly Brush _disorderBrush;
    private readonly Brush _procedureBrush;
    private readonly Brush _observableEntityBrush;
    private readonly Brush _substanceBrush;

    public PhraseHighlightRenderer(
        TextView view,
        Func<IReadOnlyList<string>> getSnapshotPhrases,
        Func<IReadOnlyDictionary<string, string?>>? getSemanticTags = null,
        Color? existingColor = null,
        Color? missingColor = null)
    {
        _view = view ?? throw new ArgumentNullException(nameof(view));
        _getSnapshotPhrases = getSnapshotPhrases ?? throw new ArgumentNullException(nameof(getSnapshotPhrases));
        _getSemanticTags = getSemanticTags;
        
        // Default colors from DarkTheme.xaml
        _existingPhraseBrush = new SolidColorBrush(existingColor ?? Color.FromRgb(0x4A, 0x4A, 0x4A)); // Dark.Color.BorderLight
        _missingPhraseBrush = new SolidColorBrush(missingColor ?? Colors.Red);
        
        // SNOMED semantic tag colors (matching SettingsWindow.xaml colors)
        _bodyStructureBrush = new SolidColorBrush(Color.FromRgb(0x90, 0xEE, 0x90)); // Light Green
        _findingBrush = new SolidColorBrush(Color.FromRgb(0xAD, 0xD8, 0xE6)); // Light Blue
        _disorderBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0xB3, 0xB3)); // Light Red/Pink
        _procedureBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0x99)); // Light Yellow
        _observableEntityBrush = new SolidColorBrush(Color.FromRgb(0xE0, 0xC4, 0xFF)); // Light Purple
        _substanceBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0xD5, 0x80)); // Light Orange
        
        _existingPhraseBrush.Freeze();
        _missingPhraseBrush.Freeze();
        _bodyStructureBrush.Freeze();
        _findingBrush.Freeze();
        _disorderBrush.Freeze();
        _procedureBrush.Freeze();
        _observableEntityBrush.Freeze();
        _substanceBrush.Freeze();

        _view.BackgroundRenderers.Add(this);
        _view.VisualLinesChanged += OnVisualLinesChanged;
    }

    public KnownLayer Layer => KnownLayer.Background; // Behind text so text remains readable

    public void Draw(TextView textView, DrawingContext dc)
    {
        textView.EnsureVisualLines();
        if (!textView.VisualLinesValid || textView.Document == null) return;

        var phrases = _getSnapshotPhrases();
        if (phrases == null || phrases.Count == 0) return;

        var doc = textView.Document;
        var text = doc.Text;
        
        // Build phrase lookup (case-insensitive for better UX)
        var phraseSet = new HashSet<string>(phrases, StringComparer.OrdinalIgnoreCase);
        
        // Get semantic tags if available
        var semanticTags = _getSemanticTags?.Invoke();
        
        // Find all visible lines
        var firstLine = textView.VisualLines.FirstOrDefault();
        var lastLine = textView.VisualLines.LastOrDefault();
        if (firstLine == null || lastLine == null) return;
        
        int startOffset = firstLine.FirstDocumentLine.Offset;
        int endOffset = lastLine.LastDocumentLine.EndOffset;
        
        // Extract visible text
        if (startOffset >= text.Length) return;
        int length = Math.Min(endOffset - startOffset, text.Length - startOffset);
        if (length <= 0) return;
        
        var visibleText = text.Substring(startOffset, length);
        
        // Tokenize visible text into words
        var matches = FindPhraseMatches(visibleText, phraseSet);
        
        // Draw highlights for each match
        foreach (var match in matches)
        {
            int absoluteOffset = startOffset + match.Offset;
            if (absoluteOffset + match.Length > doc.TextLength) continue;
            
            var segment = new TextSegment { StartOffset = absoluteOffset, Length = match.Length };
            
            // Determine brush based on semantic tag or existence
            Brush brush;
            if (match.ExistsInSnapshot && semanticTags != null && semanticTags.TryGetValue(match.PhraseText, out var semanticTag))
            {
                brush = GetBrushForSemanticTag(semanticTag);
            }
            else
            {
                brush = match.ExistsInSnapshot ? _existingPhraseBrush : _missingPhraseBrush;
            }
            
            foreach (var rect in BackgroundGeometryBuilder.GetRectsForSegment(textView, segment))
            {
                // Draw semi-transparent background
                var fillRect = new Rect(rect.Location, new Size(rect.Width, textView.DefaultLineHeight));
                dc.DrawRectangle(brush, null, fillRect);
            }
        }
    }
    
    private Brush GetBrushForSemanticTag(string? semanticTag)
    {
        if (string.IsNullOrWhiteSpace(semanticTag))
            return _existingPhraseBrush;
            
        return semanticTag.ToLowerInvariant() switch
        {
            "body structure" => _bodyStructureBrush,
            "finding" => _findingBrush,
            "disorder" => _disorderBrush,
            "procedure" => _procedureBrush,
            "observable entity" => _observableEntityBrush,
            "substance" => _substanceBrush,
            _ => _existingPhraseBrush
        };
    }

    private sealed class PhraseMatch
    {
        public int Offset { get; set; }
        public int Length { get; set; }
        public bool ExistsInSnapshot { get; set; }
        public string PhraseText { get; set; } = string.Empty;
    }

    private static List<PhraseMatch> FindPhraseMatches(string text, HashSet<string> phraseSet)
    {
        var matches = new List<PhraseMatch>();
        if (string.IsNullOrEmpty(text)) return matches;
        
        int i = 0;
        while (i < text.Length)
        {
            // Skip whitespace
            if (char.IsWhiteSpace(text[i]))
            {
                i++;
                continue;
            }
            
            // Find word boundaries
            int wordStart = i;
            while (i < text.Length && !char.IsWhiteSpace(text[i]) && !char.IsPunctuation(text[i]))
            {
                i++;
            }
            
            if (i > wordStart)
            {
                int wordLength = i - wordStart;
                var word = text.Substring(wordStart, wordLength);
                
                // Check if this word (or phrase starting with this word) exists in snapshot
                bool exists = phraseSet.Contains(word);
                
                // Try to match longer phrases by looking ahead
                int longestMatch = wordLength;
                bool longestExists = exists;
                
                // Look ahead for multi-word phrases (up to 5 words)
                for (int ahead = 1; ahead <= 4 && i < text.Length; ahead++)
                {
                    // Skip whitespace
                    int tempI = i;
                    while (tempI < text.Length && char.IsWhiteSpace(text[tempI]))
                        tempI++;
                    
                    if (tempI >= text.Length) break;
                    
                    // Find next word
                    int nextWordStart = tempI;
                    while (tempI < text.Length && !char.IsWhiteSpace(text[tempI]) && !char.IsPunctuation(text[tempI]))
                        tempI++;
                    
                    if (tempI <= nextWordStart) break;
                    
                    int phraseLength = tempI - wordStart;
                    var phrase = text.Substring(wordStart, phraseLength);
                    
                    if (phraseSet.Contains(phrase))
                    {
                        longestMatch = phraseLength;
                        longestExists = true;
                        i = tempI; // Advance to end of matched phrase
                    }
                }
                
                var matchedText = text.Substring(wordStart, longestMatch);
                matches.Add(new PhraseMatch
                {
                    Offset = wordStart,
                    Length = longestMatch,
                    ExistsInSnapshot = longestExists,
                    PhraseText = matchedText
                });
            }
        }
        
        return matches;
    }

    private void OnVisualLinesChanged(object? s, EventArgs e) => _view.InvalidateLayer(KnownLayer.Background);

    public void Dispose()
    {
        _view.BackgroundRenderers.Remove(this);
        _view.VisualLinesChanged -= OnVisualLinesChanged;
    }
}
