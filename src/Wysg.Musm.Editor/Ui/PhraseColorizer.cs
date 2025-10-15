using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace Wysg.Musm.Editor.Ui
{
    /// <summary>
    /// Foreground-based phrase colorizer with SNOMED semantic tag support.
    /// - Phrases with SNOMED mappings: colored based on semantic tag (body structure, disorder, etc.)
    /// - Phrases in snapshot without mappings: use existingBrush (default #4A4A4A)
    /// - Missing phrases: use missingBrush (default Red)
    /// Matching is case-insensitive; supports multi-word phrases up to 10 words.
    /// </summary>
    public sealed class PhraseColorizer : DocumentColorizingTransformer
    {
        private readonly Func<IReadOnlyList<string>> _getSnapshot;
        private readonly Func<IReadOnlyDictionary<string, string?>>? _getSemanticTags;
        private readonly Brush _existingBrush;
        private readonly Brush _missingBrush;
        
        // SNOMED semantic tag color brushes
        private readonly Brush _bodyStructureBrush;
        private readonly Brush _findingBrush;
        private readonly Brush _disorderBrush;
        private readonly Brush _procedureBrush;
        private readonly Brush _observableEntityBrush;
        private readonly Brush _substanceBrush;

        public PhraseColorizer(Func<IReadOnlyList<string>> getSnapshot,
                               Func<IReadOnlyDictionary<string, string?>>? getSemanticTags = null,
                               Brush? existingBrush = null,
                               Brush? missingBrush = null)
        {
            _getSnapshot = getSnapshot ?? throw new ArgumentNullException(nameof(getSnapshot));
            _getSemanticTags = getSemanticTags;

            // Try resolve from Dark theme resources first
            if (existingBrush == null)
            {
                try
                {
                    var res = System.Windows.Application.Current?.TryFindResource("Dark.Brush.ForegroundDim");
                    existingBrush = res as Brush;
                }
                catch { }
            }
            if (missingBrush == null)
            {
                try
                {
                    var res = System.Windows.Application.Current?.TryFindResource("Dark.Brush.PhraseMissing");
                    missingBrush = res as Brush;
                }
                catch { }
            }

            // Fallbacks
            _existingBrush = existingBrush ?? new SolidColorBrush(Color.FromRgb(0xA0, 0xA0, 0xA0)); // #A0A0A0
            _missingBrush = missingBrush ?? Brushes.Red;

            // SNOMED semantic tag colors (matching SettingsWindow.xaml colors)
            _bodyStructureBrush = new SolidColorBrush(Color.FromRgb(0x90, 0xEE, 0x90)); // Light Green
            _findingBrush = new SolidColorBrush(Color.FromRgb(0xAD, 0xD8, 0xE6)); // Light Blue
            _disorderBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0xB3, 0xB3)); // Light Red/Pink
            _procedureBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0x99)); // Light Yellow
            _observableEntityBrush = new SolidColorBrush(Color.FromRgb(0xE0, 0xC4, 0xFF)); // Light Purple
            _substanceBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0xD5, 0x80)); // Light Orange

            // Freeze only brushes we created locally
            if (_existingBrush is SolidColorBrush eb && !eb.IsFrozen && eb.Color == Color.FromRgb(0xA0, 0xA0, 0xA0))
                eb.Freeze();
            if (_missingBrush is SolidColorBrush mb && !mb.IsFrozen && Equals(mb.Color, Colors.Red))
                mb.Freeze();
            
            _bodyStructureBrush.Freeze();
            _findingBrush.Freeze();
            _disorderBrush.Freeze();
            _procedureBrush.Freeze();
            _observableEntityBrush.Freeze();
            _substanceBrush.Freeze();
        }

        protected override void ColorizeLine(DocumentLine line)
        {
            var phrases = _getSnapshot();
            if (phrases == null || phrases.Count == 0) return;

            var doc = CurrentContext?.Document;
            if (doc == null) return;

            int lineStart = line.Offset;
            int length = line.Length;
            if (length <= 0) return;

            var text = doc.GetText(lineStart, length);

            var set = new HashSet<string>(phrases, StringComparer.OrdinalIgnoreCase);
            var semanticTags = _getSemanticTags?.Invoke();

            foreach (var m in FindMatchesInLine(text, set))
            {
                int segStart = lineStart + m.Offset;
                int segEnd = segStart + m.Length;
                
                // Determine brush based on semantic tag or existence
                Brush brush;
                if (m.ExistsInSnapshot && semanticTags != null && semanticTags.TryGetValue(m.PhraseText, out var semanticTag))
                {
                    brush = GetBrushForSemanticTag(semanticTag);
                    System.Diagnostics.Debug.WriteLine($"[PhraseColor] '{m.PhraseText}' ¡æ semantic tag: '{semanticTag}' ¡æ brush type: {brush.GetType().Name}");
                }
                else
                {
                    brush = m.ExistsInSnapshot ? _existingBrush : _missingBrush;
                    if (m.ExistsInSnapshot && semanticTags != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[PhraseColor] '{m.PhraseText}' in snapshot but no semantic tag found (tags count: {semanticTags.Count})");
                    }
                }
                
                ChangeLinePart(segStart, segEnd, (el) =>
                {
                    el.TextRunProperties.SetForegroundBrush(brush);
                });
            }
        }
        
        private Brush GetBrushForSemanticTag(string? semanticTag)
        {
            if (string.IsNullOrWhiteSpace(semanticTag))
                return _existingBrush;
                
            return semanticTag.ToLowerInvariant() switch
            {
                "body structure" => _bodyStructureBrush,
                "finding" => _findingBrush,
                "disorder" => _disorderBrush,
                "procedure" => _procedureBrush,
                "observable entity" => _observableEntityBrush,
                "substance" => _substanceBrush,
                _ => _existingBrush
            };
        }

        private readonly struct PhraseMatch
        {
            public PhraseMatch(int offset, int length, bool exists, string phraseText)
            {
                Offset = offset; Length = length; ExistsInSnapshot = exists; PhraseText = phraseText;
            }
            public int Offset { get; }
            public int Length { get; }
            public bool ExistsInSnapshot { get; }
            public string PhraseText { get; }
        }

        private static IEnumerable<PhraseMatch> FindMatchesInLine(string text, HashSet<string> set)
        {
            int i = 0;
            while (i < text.Length)
            {
                if (char.IsWhiteSpace(text, i)) { i++; continue; }

                int wordStart = i;
                while (i < text.Length && !char.IsWhiteSpace(text, i) && !char.IsPunctuation(text[i])) i++;

                if (i <= wordStart) { i++; continue; }

                int bestLen = i - wordStart;
                bool bestExists = set.Contains(text.Substring(wordStart, bestLen));
                int scanPos = i;

                // Look ahead up to 9 additional words (total up to 10)
                for (int ahead = 1; ahead <= 9; ahead++)
                {
                    // Skip whitespace
                    while (scanPos < text.Length && char.IsWhiteSpace(text, scanPos)) scanPos++;
                    if (scanPos >= text.Length) break;

                    int nextStart = scanPos;
                    while (scanPos < text.Length && !char.IsWhiteSpace(text, scanPos) && !char.IsPunctuation(text[scanPos])) scanPos++;
                    if (scanPos <= nextStart) break;

                    int phraseLen = scanPos - wordStart;
                    var phrase = text.Substring(wordStart, phraseLen);
                    if (set.Contains(phrase))
                    {
                        bestLen = phraseLen;
                        bestExists = true;
                        i = scanPos; // advance to end of phrase
                    }
                }

                var matchedText = text.Substring(wordStart, bestLen);
                yield return new PhraseMatch(wordStart, bestLen, bestExists, matchedText);
            }
        }
    }
}
