using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;

namespace Wysg.Musm.Radium.Controls
{
 public class SideBySideDiffViewer : Grid
 {
  // We use the same newline sentinel approach as DiffTextBox. See that file for detailed rationale.
  private const string LbToken = "\uE000"; // Private Use char to represent a visual LineBreak
  private readonly RichTextBox _leftEditor;
  private readonly RichTextBox _rightEditor;
  private readonly ScrollViewer _leftScroll;
  private readonly ScrollViewer _rightScroll;
  private bool _syncingScroll;

  public static readonly DependencyProperty OriginalTextProperty =
  DependencyProperty.Register(nameof(OriginalText), typeof(string), typeof(SideBySideDiffViewer),
  new PropertyMetadata(string.Empty, OnTextChanged));

  public static readonly DependencyProperty ModifiedTextProperty =
  DependencyProperty.Register(nameof(ModifiedText), typeof(string), typeof(SideBySideDiffViewer),
  new PropertyMetadata(string.Empty, OnTextChanged));

  public static readonly DependencyProperty UseHighContrastDiffProperty =
  DependencyProperty.Register(nameof(UseHighContrastDiff), typeof(bool), typeof(SideBySideDiffViewer),
  new PropertyMetadata(true, OnTextChanged));

  public string OriginalText { get => (string)GetValue(OriginalTextProperty); set => SetValue(OriginalTextProperty, value); }
  public string ModifiedText { get => (string)GetValue(ModifiedTextProperty); set => SetValue(ModifiedTextProperty, value); }
  public bool UseHighContrastDiff { get => (bool)GetValue(UseHighContrastDiffProperty); set => SetValue(UseHighContrastDiffProperty, value); }

  public SideBySideDiffViewer()
  {
   // Layout omitted for brevity...
   ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth =200 });
   ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2) });
   ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth =200 });

   _leftEditor = CreateEditor();
   _leftScroll = new ScrollViewer { Content = _leftEditor, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
   Grid.SetColumn(_leftScroll,0);
   Children.Add(_leftScroll);

   var splitter = new GridSplitter { HorizontalAlignment = HorizontalAlignment.Stretch, Background = new SolidColorBrush(Color.FromRgb(45,45,48)), Width =2 };
   Grid.SetColumn(splitter,1);
   Children.Add(splitter);

   _rightEditor = CreateEditor();
   _rightScroll = new ScrollViewer { Content = _rightEditor, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
   Grid.SetColumn(_rightScroll,2);
   Children.Add(_rightScroll);

   // Sync scrolling
   _leftScroll.ScrollChanged += OnLeftScrollChanged;
   _rightScroll.ScrollChanged += OnRightScrollChanged;
  }

  private RichTextBox CreateEditor() => new RichTextBox { IsReadOnly = true, Background = new SolidColorBrush(Color.FromRgb(30,30,30)), Foreground = Brushes.White, BorderThickness = new Thickness(1), BorderBrush = new SolidColorBrush(Color.FromRgb(63,63,70)), Padding = new Thickness(8), FontFamily = new FontFamily("Consolas"), FontSize =12, VerticalScrollBarVisibility = ScrollBarVisibility.Hidden, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto };

  private void OnLeftScrollChanged(object? sender, ScrollChangedEventArgs e) { if (_syncingScroll) return; _syncingScroll = true; _rightScroll.ScrollToVerticalOffset(e.VerticalOffset); _rightScroll.ScrollToHorizontalOffset(e.HorizontalOffset); _syncingScroll = false; }
  private void OnRightScrollChanged(object? sender, ScrollChangedEventArgs e) { if (_syncingScroll) return; _syncingScroll = true; _leftScroll.ScrollToVerticalOffset(e.VerticalOffset); _leftScroll.ScrollToHorizontalOffset(e.HorizontalOffset); _syncingScroll = false; }

  private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) { if (d is SideBySideDiffViewer viewer) viewer.UpdateDiff(); }

  private void UpdateDiff()
  {
   var original = OriginalText ?? string.Empty;
   var modified = ModifiedText ?? string.Empty;

   // Word-level tokenization with newline sentinels
   var originalTokens = TokenizeText(original);
   var modifiedTokens = TokenizeText(modified);

   // DiffPlex operates on strings joined by real '\n'. We keep the LbToken inside tokens.
   var originalForDiff = string.Join("\n", originalTokens);
   var modifiedForDiff = string.Join("\n", modifiedTokens);

   var differ = new Differ();
   var builder = new SideBySideDiffBuilder(differ);
   var diff = builder.BuildDiffModel(originalForDiff, modifiedForDiff, ignoreWhitespace: false);

   var leftDoc = new FlowDocument { PagePadding = new Thickness(0) };
   var rightDoc = new FlowDocument { PagePadding = new Thickness(0) };
   var leftPara = new Paragraph { Margin = new Thickness(0), LineHeight = 1 };
   var rightPara = new Paragraph { Margin = new Thickness(0), LineHeight = 1 };

   int maxLines = Math.Max(diff.OldText.Lines.Count, diff.NewText.Lines.Count);
   for (int i = 0; i < maxLines; i++)
   {
    var leftLine = i < diff.OldText.Lines.Count ? diff.OldText.Lines[i] : null;
    var rightLine = i < diff.NewText.Lines.Count ? diff.NewText.Lines[i] : null;

    var leftText = leftLine?.Text ?? string.Empty;
    var rightText = rightLine?.Text ?? string.Empty;

    // Explicitly render sentinel tokens as LineBreaks on each side
    bool leftIsLb = leftText == LbToken;
    bool rightIsLb = rightText == LbToken;
    if (leftIsLb) leftPara.Inlines.Add(new LineBreak());
    if (rightIsLb) rightPara.Inlines.Add(new LineBreak());

    // If both sides are just a line break token, skip adding runs
    if (leftIsLb && rightIsLb) continue;

    if (leftLine != null && !leftIsLb) AppendToken(leftPara, leftLine, true);
    if (rightLine != null && !rightIsLb) AppendToken(rightPara, rightLine, false);
   }

   leftDoc.Blocks.Add(leftPara);
   rightDoc.Blocks.Add(rightPara);
   _leftEditor.Document = leftDoc;
   _rightEditor.Document = rightDoc;
  }

  private void AppendToken(Paragraph para, DiffPiece token, bool isLeft)
  {
   var text = token.Text ?? string.Empty;
   if (token.Type == ChangeType.Imaginary) return;

   Run run;
   if (token.Type == ChangeType.Inserted && !isLeft)
   { run = new Run(text); run.Background = GetInsertBrush(true); if (UseHighContrastDiff) run.TextDecorations = TextDecorations.Underline; para.Inlines.Add(run); }
   else if (token.Type == ChangeType.Deleted && isLeft)
   { run = new Run(text); run.Background = GetDeleteBrush(true); run.TextDecorations = TextDecorations.Strikethrough; para.Inlines.Add(run); }
   else if (token.Type == ChangeType.Modified)
   { run = new Run(text); run.Background = GetModifyBrush(true); if (UseHighContrastDiff) run.FontWeight = FontWeights.SemiBold; para.Inlines.Add(run); }
   else
   { para.Inlines.Add(new Run(text)); }
  }

  /// <summary>
  /// Tokenize into words/spaces/punct and emit a sentinel token per real line break.
  /// Normalizes CRLF/CR/LF into a single sentinel so rendering inserts consistent LineBreaks.
  /// </summary>
  private List<string> TokenizeText(string text)
  {
   var tokens = new List<string>();
   if (string.IsNullOrEmpty(text)) return tokens;
   var sb = new System.Text.StringBuilder();
   bool inWord = false;
   for (int i = 0; i < text.Length; i++)
   {
    char c = text[i];
    if (c == '\r' || c == '\n')
    {
     if (sb.Length > 0) { tokens.Add(sb.ToString()); sb.Clear(); }
     if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++; // consume LF in CRLF
     tokens.Add(LbToken);
     inWord = false;
     continue;
    }
    bool isWord = char.IsLetterOrDigit(c) || c == '-' || c == '\'';
    if (isWord)
    {
     if (!inWord && sb.Length > 0) { tokens.Add(sb.ToString()); sb.Clear(); }
     sb.Append(c); inWord = true;
    }
    else
    {
     if (inWord && sb.Length > 0) { tokens.Add(sb.ToString()); sb.Clear(); }
     sb.Append(c); inWord = false;
    }
   }
   if (sb.Length > 0) tokens.Add(sb.ToString());
   return tokens;
  }

  private Brush GetInsertBrush(bool inline) => UseHighContrastDiff ? new SolidColorBrush(Color.FromArgb(inline ? (byte)200 : (byte)110,0,255,0)) : new SolidColorBrush(Color.FromArgb(inline ? (byte)140 : (byte)60,0,255,0));
  private Brush GetDeleteBrush(bool inline) => UseHighContrastDiff ? new SolidColorBrush(Color.FromArgb(inline ? (byte)200 : (byte)110,255,0,0)) : new SolidColorBrush(Color.FromArgb(inline ? (byte)140 : (byte)60,255,0,0));
  private Brush GetModifyBrush(bool inline) => UseHighContrastDiff ? new SolidColorBrush(Color.FromArgb(inline ? (byte)180 : (byte)100,255,255,0)) : new SolidColorBrush(Color.FromArgb(inline ? (byte)120 : (byte)70,255,255,0));
 }
}
