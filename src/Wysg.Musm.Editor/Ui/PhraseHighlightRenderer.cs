using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace Wysg.Musm.Editor.Ui;

/// <summary>
/// Background renderer that highlights phrases based on a snapshot list.
/// - Phrases in the snapshot: show as #4A4A4A (BorderLight color)
/// - Phrases not in snapshot: show as red
/// </summary>
public sealed class PhraseHighlightRenderer : IBackgroundRenderer, IDisposable
{
    private readonly TextView _view;
    private readonly Func<IReadOnlyList<string>> _getSnapshotPhrases;
    private readonly Brush _existingPhraseBrush;
    private readonly Brush _missingPhraseBrush;

    public PhraseHighlightRenderer(
        TextView view,
        Func<IReadOnlyList<string>> getSnapshotPhrases,
        Color? existingColor = null,
        Color? missingColor = null)
    {
        _view = view ?? throw new ArgumentNullException(nameof(view));
        _getSnapshotPhrases = getSnapshotPhrases ?? throw new ArgumentNullException(nameof(getSnapshotPhrases));
        
        // Default colors from DarkTheme.xaml
        _existingPhraseBrush = new SolidColorBrush(existingColor ?? Color.FromRgb(0x4A, 0x4A, 0x4A)); // Dark.Color.BorderLight
        _missingPhraseBrush = new SolidColorBrush(missingColor ?? Colors.Red);
        
        _existingPhraseBrush.Freeze();
        _missingPhraseBrush.Freeze();

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
            var brush = match.ExistsInSnapshot ? _existingPhraseBrush : _missingPhraseBrush;
            
            foreach (var rect in BackgroundGeometryBuilder.GetRectsForSegment(textView, segment))
            {
                // Draw semi-transparent background
                var fillRect = new Rect(rect.Location, new Size(rect.Width, textView.DefaultLineHeight));
                dc.DrawRectangle(brush, null, fillRect);
            }
        }
    }

    private sealed class PhraseMatch
    {
        public int Offset { get; set; }
        public int Length { get; set; }
        public bool ExistsInSnapshot { get; set; }
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
                
                matches.Add(new PhraseMatch
                {
                    Offset = wordStart,
                    Length = longestMatch,
                    ExistsInSnapshot = longestExists
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
