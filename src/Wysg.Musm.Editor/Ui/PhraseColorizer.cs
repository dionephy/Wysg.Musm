using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace Wysg.Musm.Editor.Ui
{
    /// <summary>
    /// Foreground-based phrase colorizer. Applies text foreground color for phrases.
    /// - Existing phrases (in snapshot) use existingBrush (default #4A4A4A)
    /// - Missing phrases use missingBrush (default Red)
    /// Matching is case-insensitive; supports multi-word phrases up to 5 words.
    /// </summary>
    public sealed class PhraseColorizer : DocumentColorizingTransformer
    {
        private readonly Func<IReadOnlyList<string>> _getSnapshot;
        private readonly Brush _existingBrush;
        private readonly Brush _missingBrush;

        public PhraseColorizer(Func<IReadOnlyList<string>> getSnapshot,
                               Brush? existingBrush = null,
                               Brush? missingBrush = null)
        {
            _getSnapshot = getSnapshot ?? throw new ArgumentNullException(nameof(getSnapshot));

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
            _missingBrush = missingBrush ?? Brushes.Red; // roll back to Red as requested

            // Freeze only brushes we created locally (avoid freezing app resource instances)
            if (_existingBrush is SolidColorBrush eb && !eb.IsFrozen && eb.Color == Color.FromRgb(0xA0, 0xA0, 0xA0))
                eb.Freeze();
            if (_missingBrush is SolidColorBrush mb && !mb.IsFrozen && Equals(mb.Color, Colors.Red))
                mb.Freeze();
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

            foreach (var m in FindMatchesInLine(text, set))
            {
                int segStart = lineStart + m.Offset;
                int segEnd = segStart + m.Length;
                var brush = m.ExistsInSnapshot ? _existingBrush : _missingBrush;
                ChangeLinePart(segStart, segEnd, (el) =>
                {
                    el.TextRunProperties.SetForegroundBrush(brush);
                });
            }
        }

        private readonly struct PhraseMatch
        {
            public PhraseMatch(int offset, int length, bool exists)
            {
                Offset = offset; Length = length; ExistsInSnapshot = exists;
            }
            public int Offset { get; }
            public int Length { get; }
            public bool ExistsInSnapshot { get; }
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

                yield return new PhraseMatch(wordStart, bestLen, bestExists);
            }
        }
    }
}
