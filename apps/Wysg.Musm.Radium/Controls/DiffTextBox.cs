using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;

namespace Wysg.Musm.Radium.Controls
{
    /// <summary>
    /// Custom TextBox that displays character-by-character differences between original and modified text.
    /// Green highlighting = additions, Red + strikethrough = deletions.
    /// Uses DiffPlex with word-level tokenization for better granularity.
    /// </summary>
    public class DiffTextBox : RichTextBox
    {
        // Newline sentinel: we represent real line breaks as a dedicated token instead of embedding \n in tokens.
        // Why? DiffPlex consumes strings split by '\n' into logical "lines" for comparison. If we keep actual
        // newline characters inside tokens, those characters can be lost or mis-rendered when we rebuild the FlowDocument,
        // which leads to two visually separate paragraphs being concatenated.
        // Using a sentinel token allows us to:
        //  - preserve exact line-break positions across diffing,
        //  - render them deterministically as WPF LineBreak() elements,
        //  - keep token-level comparisons for words/spaces intact.
        private const string LbToken = "\uE000"; // Private Use Area char unlikely to appear in input

        public static readonly DependencyProperty OriginalTextProperty =
         DependencyProperty.Register(nameof(OriginalText), typeof(string), typeof(DiffTextBox),
         new PropertyMetadata(string.Empty, OnTextChanged));

        public static readonly DependencyProperty ModifiedTextProperty =
      DependencyProperty.Register(nameof(ModifiedText), typeof(string), typeof(DiffTextBox),
       new PropertyMetadata(string.Empty, OnTextChanged));

        // High-contrast highlighting switch
        public static readonly DependencyProperty UseHighContrastDiffProperty =
         DependencyProperty.Register(nameof(UseHighContrastDiff), typeof(bool), typeof(DiffTextBox),
         new PropertyMetadata(true, OnTextChanged));

        public string OriginalText
{
            get => (string)GetValue(OriginalTextProperty);
  set => SetValue(OriginalTextProperty, value);
        }

        public string ModifiedText
        {
      get => (string)GetValue(ModifiedTextProperty);
 set => SetValue(ModifiedTextProperty, value);
        }

        public bool UseHighContrastDiff
        {
      get => (bool)GetValue(UseHighContrastDiffProperty);
 set => SetValue(UseHighContrastDiffProperty, value);
        }

     public DiffTextBox()
      {
         IsReadOnly = true;
       Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)); // Dark background
   Foreground = Brushes.White;
            BorderThickness = new Thickness(1);
            BorderBrush = new SolidColorBrush(Color.FromRgb(63, 63, 70));
            Padding = new Thickness(4);
   FontSize = 12;
       AcceptsReturn = true;
      VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
          HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
}

        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
       if (d is DiffTextBox diffBox)
        {
    diffBox.UpdateDiff();
         }
        }

        private void UpdateDiff()
        {
            var original = OriginalText ?? string.Empty;
            var modified = ModifiedText ?? string.Empty;

            // Word-level tokenization (keeps spaces/punct as separate tokens) + newline sentinels
            var originalTokens = TokenizeText(original);
            var modifiedTokens = TokenizeText(modified);

            // Join tokens with real '\n' to feed DiffPlex. Newline sentinels are kept as tokens.
            var originalForDiff = string.Join("\n", originalTokens);
            var modifiedForDiff = string.Join("\n", modifiedTokens);

            var differ = new Differ();
            var builder = new InlineDiffBuilder(differ);
            var diff = builder.BuildDiffModel(originalForDiff, modifiedForDiff, ignoreWhitespace: false);

            // Build FlowDocument with inline formatting
            var doc = new FlowDocument();
            var para = new Paragraph { Margin = new Thickness(0) };

            foreach (var line in diff.Lines)
            {
                // Each "line" is a token produced by our tokenizer above
                var token = line.Text ?? string.Empty;

                // Render newline sentinel as LineBreak and continue
                if (token == LbToken)
                {
                    para.Inlines.Add(new LineBreak());
                    continue;
                }
                
                if (line.Type == ChangeType.Inserted)
                {
                    var run = new Run(token) { Background = GetInsertBrush(inline: true) };
                    if (UseHighContrastDiff) run.TextDecorations = TextDecorations.Underline;
                    para.Inlines.Add(run);
                }
                else if (line.Type == ChangeType.Deleted)
                {
                    var run = new Run(token) { Background = GetDeleteBrush(inline: true), TextDecorations = TextDecorations.Strikethrough };
                    para.Inlines.Add(run);
                }
                else if (line.Type == ChangeType.Modified)
                {
                    // Show character-level diffs when available
                    if (line.SubPieces != null && line.SubPieces.Count > 0)
                    {
                        foreach (var piece in line.SubPieces)
                        {
                            var run = new Run(piece.Text);
                            if (piece.Type == ChangeType.Inserted)
                            {
                                run.Background = GetInsertBrush(inline: true);
                                if (UseHighContrastDiff) run.TextDecorations = TextDecorations.Underline;
                            }
                            else if (piece.Type == ChangeType.Deleted)
                            {
                                run.Background = GetDeleteBrush(inline: true);
                                run.TextDecorations = TextDecorations.Strikethrough;
                            }
                            else if (piece.Type == ChangeType.Modified)
                            {
                                run.Background = GetModifyBrush(inline: true);
                                if (UseHighContrastDiff) run.FontWeight = FontWeights.SemiBold;
                            }
                            para.Inlines.Add(run);
                        }
                    }
                    else
                    {
                        // Fallback: color the whole token
                        var run = new Run(token) { Background = GetModifyBrush(inline: true) };
                        if (UseHighContrastDiff) run.FontWeight = FontWeights.SemiBold;
                        para.Inlines.Add(run);
                    }
                }
                else // Unchanged
                {
                    para.Inlines.Add(new Run(token));
                }
            }

            doc.Blocks.Add(para);
            Document = doc;
        }

        /// <summary>
        /// Tokenizes text into words/spaces/punct for word-level diff and emits a newline sentinel token per real line break.
        /// - CRLF/CR/LF are all normalized to the same sentinel token during tokenization to preserve positions.
        /// - This prevents DiffPlex from losing the visual break when we rebuild the FlowDocument.
        /// </summary>
        private List<string> TokenizeText(string text)
        {
            var tokens = new List<string>();
            if (string.IsNullOrEmpty(text)) return tokens;

            var currentToken = new System.Text.StringBuilder();
            bool inWord = false;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                // Hard line breaks -> sentinel token
                if (c == '\r' || c == '\n')
                {
                    if (currentToken.Length > 0)
                    {
                        tokens.Add(currentToken.ToString());
                        currentToken.Clear();
                    }
                    // Consume LF when CRLF
                    if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++;
                    tokens.Add(LbToken);
                    inWord = false;
                    continue;
                }

                bool isWordChar = char.IsLetterOrDigit(c) || c == '-' || c == '\'';

                if (isWordChar)
                {
                    if (!inWord && currentToken.Length > 0)
                    {
                        tokens.Add(currentToken.ToString());
                        currentToken.Clear();
                    }
                    currentToken.Append(c);
                    inWord = true;
                }
                else
                {
                    if (inWord && currentToken.Length > 0)
                    {
                        tokens.Add(currentToken.ToString());
                        currentToken.Clear();
                    }
                    currentToken.Append(c);
                    inWord = false;
                }
            }

            if (currentToken.Length > 0)
            {
                tokens.Add(currentToken.ToString());
            }

            return tokens;
        }

 private Brush GetInsertBrush(bool inline) =>
 UseHighContrastDiff
 ? new SolidColorBrush(Color.FromArgb(inline ? (byte)200 : (byte)110,0,255,0))
 : new SolidColorBrush(Color.FromArgb(inline ? (byte)140 : (byte)80,0,255,0));

 private Brush GetDeleteBrush(bool inline) =>
 UseHighContrastDiff
 ? new SolidColorBrush(Color.FromArgb(inline ? (byte)200 : (byte)110,255,0,0))
 : new SolidColorBrush(Color.FromArgb(inline ? (byte)140 : (byte)80,255,0,0));

 private Brush GetModifyBrush(bool inline) =>
 UseHighContrastDiff
 ? new SolidColorBrush(Color.FromArgb(inline ? (byte)180 : (byte)100,255,255,0))
 : new SolidColorBrush(Color.FromArgb(inline ? (byte)120 : (byte)70,255,255,0));
    }
}
