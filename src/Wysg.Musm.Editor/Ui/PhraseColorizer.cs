using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace Wysg.Musm.Editor.Ui
{
    /// <summary>
    /// Foreground-based phrase colorizer with SNOMED semantic tag support.
    /// - Phrases with SNOMED mappings: colored based on semantic tag (body structure, disorder, etc.)
    /// - Phrases in snapshot without mappings: use existingBrush (default #A0A0A0)
    /// - Numbers (integers and decimals): use existingBrush
    /// - Dates (0000-00-00 format): use existingBrush
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
        
        // Regex patterns for numbers and dates
        private static readonly Regex NumberPattern = new Regex(@"^\d+(\.\d+)?$", RegexOptions.Compiled);
        private static readonly Regex DatePattern = new Regex(@"^\d{4}-\d{2}-\d{2}$", RegexOptions.Compiled);

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
            _findingBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0x99)); // Light Yellow (unused - finding now uses disorder color)
            _disorderBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0xB3, 0xB3)); // Light Red/Pink
            _procedureBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0x99)); // Light Yellow
            _observableEntityBrush = new SolidColorBrush(Color.FromRgb(0xE0, 0xC4, 0xFF)); // Light Purple (unused)
            _substanceBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0xD5, 0x80)); // Light Orange (unused)

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
                }
                else
                {
                    brush = m.ExistsInSnapshot ? _existingBrush : _missingBrush;
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
      "body structure" => _bodyStructureBrush, // Light green
   "intended site" => _bodyStructureBrush, // Light green (same as body structure)
  "finding" => _disorderBrush, // Light pink (same as disorder)
    "morphologic abnormality" => _disorderBrush, // Light pink (same as disorder)
    "disorder" => _disorderBrush, // Light pink
  "procedure" => _procedureBrush, // Light yellow
   "observable entity" => _existingBrush, // Default gray
      "substance" => _existingBrush, // Default gray
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
                // Skip whitespace
                if (char.IsWhiteSpace(text, i)) { i++; continue; }
                
                // Skip standalone punctuation (except hyphen and forward slash which are part of medical terms)
                if (char.IsPunctuation(text[i]) && text[i] != '-' && text[i] != '/') { i++; continue; }

                // Find word boundaries (include hyphens, forward slashes, and periods for phrases like "COVID-19", "N/A", and decimals)
                int wordStart = i;
                while (i < text.Length && !char.IsWhiteSpace(text, i) && (char.IsLetterOrDigit(text, i) || text[i] == '-' || text[i] == '/' || text[i] == '.'))
                    i++;

                if (i <= wordStart) { i++; continue; }

                int bestLen = i - wordStart;
                var currentToken = text.Substring(wordStart, bestLen);
                
                // Strip trailing periods (sentence punctuation) but keep them for matching purposes
                var tokenForMatching = currentToken.TrimEnd('.');
                int trailingPeriodsCount = currentToken.Length - tokenForMatching.Length;
                
                // Check if this token is a number or date - if so, treat it as existing
                bool isNumberOrDate = NumberPattern.IsMatch(tokenForMatching) || DatePattern.IsMatch(tokenForMatching);
                bool bestExists = isNumberOrDate || set.Contains(tokenForMatching);
                int scanPos = i;

                // Only look ahead for multi-word phrases if not a number or date
                if (!isNumberOrDate)
                {
                    // Look ahead up to 9 additional words (total up to 10)
                    for (int ahead = 1; ahead <= 9; ahead++)
                    {
                        // Skip whitespace
                        while (scanPos < text.Length && char.IsWhiteSpace(text, scanPos)) scanPos++;
                        if (scanPos >= text.Length) break;
            
                        // Skip punctuation before next word (except hyphen and forward slash)
                        if (char.IsPunctuation(text[scanPos]) && text[scanPos] != '-' && text[scanPos] != '/') break;

                        // Find next word (include hyphens and forward slashes, but not periods at this stage)
                        int nextStart = scanPos;
                        while (scanPos < text.Length && !char.IsWhiteSpace(text, scanPos) && (char.IsLetterOrDigit(text, scanPos) || text[scanPos] == '-' || text[scanPos] == '/'))
                            scanPos++;
        
                        if (scanPos <= nextStart) break;

                        int phraseLen = scanPos - wordStart;
                        var phrase = text.Substring(wordStart, phraseLen);
                        
                        // Strip trailing periods from multi-word phrases too
                        var phraseForMatching = phrase.TrimEnd('.');
                        if (set.Contains(phraseForMatching))
                        {
                            bestLen = phraseForMatching.Length;
                            bestExists = true;
                            i = wordStart + bestLen; // advance to end of matched phrase (without trailing periods)
                            tokenForMatching = phraseForMatching;
                            trailingPeriodsCount = 0; // We've already adjusted the length
                        }
                    }
                }

                // Use the matched length (excluding trailing periods)
                int matchLen = bestLen - trailingPeriodsCount;
                if (matchLen > 0)
                {
                    yield return new PhraseMatch(wordStart, matchLen, bestExists, tokenForMatching);
                }
            }
        }
    }
}
