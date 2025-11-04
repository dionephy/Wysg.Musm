using System;
using System.Collections.Generic;
using System.Globalization;
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
    /// Now supports AutoHeight: the control grows/shrinks to fit its content up to MaxAutoHeight.
    /// MinHeight is constrained to a single line of text (including padding/border) to keep the control compact.
    /// </summary>
    public class DiffTextBox : RichTextBox
    {
        private const string LbToken = "\uE000"; // Private Use Area char unlikely to appear in input

        public static readonly DependencyProperty OriginalTextProperty =
         DependencyProperty.Register(nameof(OriginalText), typeof(string), typeof(DiffTextBox),
         new PropertyMetadata(string.Empty, OnTextChanged));

        public static readonly DependencyProperty ModifiedTextProperty =
      DependencyProperty.Register(nameof(ModifiedText), typeof(string), typeof(DiffTextBox),
       new PropertyMetadata(string.Empty, OnTextChanged));

        public static readonly DependencyProperty UseHighContrastDiffProperty =
         DependencyProperty.Register(nameof(UseHighContrastDiff), typeof(bool), typeof(DiffTextBox),
         new PropertyMetadata(true, OnTextChanged));

        public static readonly DependencyProperty AutoHeightProperty =
            DependencyProperty.Register(nameof(AutoHeight), typeof(bool), typeof(DiffTextBox), new PropertyMetadata(true, OnAutoHeightChanged));

        public static readonly DependencyProperty MaxAutoHeightProperty =
            DependencyProperty.Register(nameof(MaxAutoHeight), typeof(double), typeof(DiffTextBox), new PropertyMetadata(600d, OnAutoHeightChanged));

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

        public bool AutoHeight
        {
            get => (bool)GetValue(AutoHeightProperty);
            set => SetValue(AutoHeightProperty, value);
        }

        public double MaxAutoHeight
        {
            get => (double)GetValue(MaxAutoHeightProperty);
            set => SetValue(MaxAutoHeightProperty, value);
        }

        private bool _updatingHeight;
        private int _renderedLineCount = 1; // computed during UpdateDiff (LineBreaks + 1)

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
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled; // allow auto height by default
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;

            Loaded += (s, e) =>
            {
                // Ensure a sensible minimum equal to one line of text
                MinHeight = GetSingleLineDesiredHeight();
                TryUpdateAutoHeight();
            };

            // Update height when layout changes (document reflows) or font affects line height
            LayoutUpdated += (s, e) =>
            {
                if (AutoHeight)
                {
                    MinHeight = GetSingleLineDesiredHeight();
                    TryUpdateAutoHeight();
                }
            };
        }

        private static void OnAutoHeightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DiffTextBox box)
            {
                // Toggle vertical scrollbar depending on AutoHeight
                box.VerticalScrollBarVisibility = box.AutoHeight ? ScrollBarVisibility.Disabled : ScrollBarVisibility.Auto;
                box.MinHeight = box.GetSingleLineDesiredHeight();
                box.TryUpdateAutoHeight();
            }
        }

        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DiffTextBox diffBox)
            {
                diffBox.UpdateDiff();
                if (diffBox.AutoHeight)
                {
                    diffBox.MinHeight = diffBox.GetSingleLineDesiredHeight();
                    diffBox.TryUpdateAutoHeight();
                }
            }
        }

        private void UpdateDiff()
        {
            var original = OriginalText ?? string.Empty;
            var modified = ModifiedText ?? string.Empty;

            var originalTokens = TokenizeText(original);
            var modifiedTokens = TokenizeText(modified);

            var originalForDiff = string.Join("\n", originalTokens);
            var modifiedForDiff = string.Join("\n", modifiedTokens);

            var differ = new Differ();
            var builder = new InlineDiffBuilder(differ);
            var diff = builder.BuildDiffModel(originalForDiff, modifiedForDiff, ignoreWhitespace: false);

            var doc = new FlowDocument { PagePadding = new Thickness(0) }; // remove default 5px padding
            var para = new Paragraph { Margin = new Thickness(0) };

            int lbCount = 0;
            foreach (var line in diff.Lines)
            {
                var token = line.Text ?? string.Empty;

                if (token == LbToken)
                {
                    para.Inlines.Add(new LineBreak());
                    lbCount++;
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
                        var run = new Run(token) { Background = GetModifyBrush(inline: true) };
                        if (UseHighContrastDiff) run.FontWeight = FontWeights.SemiBold;
                        para.Inlines.Add(run);
                    }
                }
                else
                {
                    para.Inlines.Add(new Run(token));
                }
            }

            _renderedLineCount = Math.Max(1, lbCount + 1); // at least one line

            Document = new FlowDocument(para) { PagePadding = new Thickness(0) };
        }

        /// <summary>
        /// Compute and apply Height to fit content (capped by MaxAutoHeight) and not smaller than one line.
        /// </summary>
        private void TryUpdateAutoHeight()
        {
            if (!AutoHeight || _updatingHeight) return;

            try
            {
                _updatingHeight = true;

                // Compute height from line count instead of ScrollViewer extent (which equals viewport when content is small)
                var (line, chrome) = GetLineAndChrome();
                double desired = _renderedLineCount * line + chrome;

                double minOneLine = line + chrome;
                double capped = (double.IsNaN(MaxAutoHeight) || double.IsInfinity(MaxAutoHeight) || MaxAutoHeight <= 0)
                    ? desired
                    : Math.Min(desired, MaxAutoHeight);

                double final = Math.Max(minOneLine, capped);
                MinHeight = minOneLine;
                Height = final;
            }
            finally
            {
                _updatingHeight = false;
            }
        }

        private (double line, double chrome) GetLineAndChrome()
        {
            var typeface = new Typeface(FontFamily, FontStyle, FontWeight, FontStretch);
            double p = 1.0;
            try { p = VisualTreeHelper.GetDpi(this).PixelsPerDip; } catch { }
            var ft = new FormattedText("Mg", CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, typeface, FontSize, Brushes.Transparent, p);
            double chrome = Padding.Top + Padding.Bottom + BorderThickness.Top + BorderThickness.Bottom;
            return (ft.Height, chrome);
        }

        private double GetSingleLineDesiredHeight()
        {
            var (line, chrome) = GetLineAndChrome();
            return line + chrome;
        }

        private List<string> TokenizeText(string text)
        {
            var tokens = new List<string>();
            if (string.IsNullOrEmpty(text)) return tokens;

            var currentToken = new System.Text.StringBuilder();
            bool inWord = false;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                if (c == '\r' || c == '\n')
                {
                    if (currentToken.Length > 0)
                    {
                        tokens.Add(currentToken.ToString());
                        currentToken.Clear();
                    }
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
         ? new SolidColorBrush(Color.FromArgb(inline ? (byte)200 : (byte)110, 0, 255, 0))
         : new SolidColorBrush(Color.FromArgb(inline ? (byte)140 : (byte)80, 0, 255, 0));

        private Brush GetDeleteBrush(bool inline) =>
         UseHighContrastDiff
         ? new SolidColorBrush(Color.FromArgb(inline ? (byte)200 : (byte)110, 255, 0, 0))
         : new SolidColorBrush(Color.FromArgb(inline ? (byte)140 : (byte)80, 255, 0, 0));

        private Brush GetModifyBrush(bool inline) =>
         UseHighContrastDiff
         ? new SolidColorBrush(Color.FromArgb(inline ? (byte)180 : (byte)100, 255, 255, 0))
         : new SolidColorBrush(Color.FromArgb(inline ? (byte)120 : (byte)70, 255, 255, 0));
    }
}
