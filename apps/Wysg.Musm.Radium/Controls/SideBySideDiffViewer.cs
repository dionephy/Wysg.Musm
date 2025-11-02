using System;
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
 /// Side-by-side diff viewer showing original text on left and modified text on right.
 /// Uses DiffPlex SideBySideDiffBuilder for line alignment and a lightweight
 /// single-region character diff for inline highlighting.
 /// </summary>
 public class SideBySideDiffViewer : Grid
 {
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

 // High-contrast highlighting switch
 public static readonly DependencyProperty UseHighContrastDiffProperty =
 DependencyProperty.Register(nameof(UseHighContrastDiff), typeof(bool), typeof(SideBySideDiffViewer),
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

 public SideBySideDiffViewer()
 {
 // Grid: [Left][Splitter][Right]
 ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth =200 });
 ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2) });
 ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth =200 });

 _leftEditor = CreateEditor();
 _leftScroll = new ScrollViewer
 {
 Content = _leftEditor,
 VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
 // Disable horizontal scroll on outer viewer to avoid infinite measure
 HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
 };
 Grid.SetColumn(_leftScroll,0);
 Children.Add(_leftScroll);

 var splitter = new GridSplitter
 {
 HorizontalAlignment = HorizontalAlignment.Stretch,
 Background = new SolidColorBrush(Color.FromRgb(45,45,48)),
 Width =2
 };
 Grid.SetColumn(splitter,1);
 Children.Add(splitter);

 _rightEditor = CreateEditor();
 _rightScroll = new ScrollViewer
 {
 Content = _rightEditor,
 VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
 HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
 };
 Grid.SetColumn(_rightScroll,2);
 Children.Add(_rightScroll);

 // Sync scrolling
 _leftScroll.ScrollChanged += OnLeftScrollChanged;
 _rightScroll.ScrollChanged += OnRightScrollChanged;
 }

 private RichTextBox CreateEditor()
 {
 return new RichTextBox
 {
 IsReadOnly = true,
 Background = new SolidColorBrush(Color.FromRgb(30,30,30)),
 Foreground = Brushes.White,
 BorderThickness = new Thickness(1),
 BorderBrush = new SolidColorBrush(Color.FromRgb(63,63,70)),
 Padding = new Thickness(8),
 FontFamily = new FontFamily("Consolas"),
 FontSize =12,
 // Horizontal scroll lives on the inner editor
 VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
 HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
 };
 }

 private void OnLeftScrollChanged(object? sender, ScrollChangedEventArgs e)
 {
 if (_syncingScroll) return;
 _syncingScroll = true;
 _rightScroll.ScrollToVerticalOffset(e.VerticalOffset);
 _rightScroll.ScrollToHorizontalOffset(e.HorizontalOffset);
 _syncingScroll = false;
 }

 private void OnRightScrollChanged(object? sender, ScrollChangedEventArgs e)
 {
 if (_syncingScroll) return;
 _syncingScroll = true;
 _leftScroll.ScrollToVerticalOffset(e.VerticalOffset);
 _leftScroll.ScrollToHorizontalOffset(e.HorizontalOffset);
 _syncingScroll = false;
 }

 private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
 {
 if (d is SideBySideDiffViewer viewer)
 {
 viewer.UpdateDiff();
 }
 }

 private void UpdateDiff()
 {
 var original = OriginalText ?? string.Empty;
 var modified = ModifiedText ?? string.Empty;

 var differ = new Differ();
 var lineBuilder = new SideBySideDiffBuilder(differ);
 var lineModel = lineBuilder.BuildDiffModel(original, modified, ignoreWhitespace: false);

 // Prepare documents
 var leftDoc = new FlowDocument { PagePadding = new Thickness(0) };
 var rightDoc = new FlowDocument { PagePadding = new Thickness(0) };
 var leftPara = new Paragraph { Margin = new Thickness(0), LineHeight =1 };
 var rightPara = new Paragraph { Margin = new Thickness(0), LineHeight =1 };

 // Iterate by aligned rows
 int rows = Math.Max(lineModel.OldText.Lines.Count, lineModel.NewText.Lines.Count);
 for (int i =0; i < rows; i++)
 {
 var leftLine = i < lineModel.OldText.Lines.Count ? lineModel.OldText.Lines[i] : new DiffPiece(string.Empty, ChangeType.Imaginary, i);
 var rightLine = i < lineModel.NewText.Lines.Count ? lineModel.NewText.Lines[i] : new DiffPiece(string.Empty, ChangeType.Imaginary, i);

 AppendLinePair(leftPara, rightPara, leftLine, rightLine);
 }

 leftDoc.Blocks.Add(leftPara);
 rightDoc.Blocks.Add(rightPara);
 _leftEditor.Document = leftDoc;
 _rightEditor.Document = rightDoc;
 }

 private void AppendLinePair(Paragraph leftPara, Paragraph rightPara, DiffPiece leftLine, DiffPiece rightLine)
 {
 // If both sides have non-imaginary lines and texts differ, render single-region character diff
 if (leftLine.Type != ChangeType.Imaginary && rightLine.Type != ChangeType.Imaginary)
 {
 var leftText = leftLine.Text ?? string.Empty;
 var rightText = rightLine.Text ?? string.Empty;
 if (!string.Equals(leftText, rightText, StringComparison.Ordinal))
 {
 RenderSingleRegionCharDiff(leftPara, rightPara, leftText, rightText);
 leftPara.Inlines.Add(new LineBreak());
 rightPara.Inlines.Add(new LineBreak());
 return;
 }
 }

 // Fallback to line-level rendering (inserts/deletes or identical)
 AppendLine(leftPara, leftLine, isLeft: true);
 AppendLine(rightPara, rightLine, isLeft: false);
 }

 // Simple single-region character diff (prefix/suffix common, middle differs)
 private void RenderSingleRegionCharDiff(Paragraph leftPara, Paragraph rightPara, string a, string b)
 {
 int prefix =0;
 int maxPrefix = Math.Min(a.Length, b.Length);
 while (prefix < maxPrefix && a[prefix] == b[prefix]) prefix++;

 int aTail = a.Length -1;
 int bTail = b.Length -1;
 while (aTail >= prefix && bTail >= prefix && a[aTail] == b[bTail]) { aTail--; bTail--; }

 string aPrefix = a.Substring(0, prefix);
 string aMid = prefix <= aTail ? a.Substring(prefix, aTail - prefix +1) : string.Empty;
 string aSuffix = aTail +1 < a.Length ? a[(aTail +1)..] : string.Empty;

 string bPrefix = b.Substring(0, prefix);
 string bMid = prefix <= bTail ? b.Substring(prefix, bTail - prefix +1) : string.Empty;
 string bSuffix = bTail +1 < b.Length ? b[(bTail +1)..] : string.Empty;

 // Left side (original)
 if (!string.IsNullOrEmpty(aPrefix)) leftPara.Inlines.Add(new Run(aPrefix));
 if (!string.IsNullOrEmpty(aMid))
 {
 var delRun = new Run(aMid)
 {
 Background = GetDeleteBrush(inline: true)
 };
 delRun.TextDecorations = TextDecorations.Strikethrough;
 leftPara.Inlines.Add(delRun);
 }
 if (!string.IsNullOrEmpty(aSuffix)) leftPara.Inlines.Add(new Run(aSuffix));

 // Right side (modified)
 if (!string.IsNullOrEmpty(bPrefix)) rightPara.Inlines.Add(new Run(bPrefix));
 if (!string.IsNullOrEmpty(bMid))
 {
 var insRun = new Run(bMid)
 {
 Background = GetInsertBrush(inline: true)
 };
 if (UseHighContrastDiff) insRun.TextDecorations = TextDecorations.Underline;
 rightPara.Inlines.Add(insRun);
 }
 if (!string.IsNullOrEmpty(bSuffix)) rightPara.Inlines.Add(new Run(bSuffix));
 }

 private void AppendLine(Paragraph para, DiffPiece line, bool isLeft)
 {
 var lineBackground = GetLineBackground(line.Type, isLeft);

 if (line.Type == ChangeType.Imaginary)
 {
 var runEmpty = new Run(" ") { Background = new SolidColorBrush(Color.FromRgb(40,40,40)) };
 para.Inlines.Add(runEmpty);
 para.Inlines.Add(new LineBreak());
 return;
 }

 var hasCharDiffs = line.SubPieces != null && line.SubPieces.Any(p => p.Type != ChangeType.Unchanged && !string.IsNullOrEmpty(p.Text));

 if (hasCharDiffs)
 {
 foreach (var piece in line.SubPieces!)
 {
 if (string.IsNullOrEmpty(piece.Text)) continue;
 var run = new Run(piece.Text);

 switch (piece.Type)
 {
 case ChangeType.Inserted when !isLeft:
 run.Background = GetInsertBrush(inline: true);
 run.Foreground = Brushes.White;
 if (UseHighContrastDiff) run.TextDecorations = TextDecorations.Underline;
 break;
 case ChangeType.Deleted when isLeft:
 run.Background = GetDeleteBrush(inline: true);
 run.Foreground = Brushes.White;
 run.TextDecorations = TextDecorations.Strikethrough;
 break;
 case ChangeType.Modified:
 run.Background = GetModifyBrush(inline: true);
 run.Foreground = Brushes.White;
 if (UseHighContrastDiff) run.FontWeight = FontWeights.SemiBold;
 break;
 default:
 run.Background = Brushes.Transparent;
 break;
 }

 para.Inlines.Add(run);
 }
 }
 else
 {
 var lineText = line.Text ?? string.Empty;
 if (string.IsNullOrEmpty(lineText)) lineText = " ";
 var run = new Run(lineText) { Background = line.Type == ChangeType.Modified ? Brushes.Transparent : lineBackground };
 if (line.Type == ChangeType.Deleted && isLeft)
 {
 run.TextDecorations = TextDecorations.Strikethrough;
 }
 para.Inlines.Add(run);
 }

 para.Inlines.Add(new LineBreak());
 }

 private Brush GetLineBackground(ChangeType type, bool isLeft)
 {
 return type switch
 {
 ChangeType.Inserted when !isLeft => GetInsertBrush(inline: false),
 ChangeType.Deleted when isLeft => GetDeleteBrush(inline: false),
 ChangeType.Modified => GetModifyBrush(inline: false),
 _ => Brushes.Transparent
 };
 }

 private Brush GetInsertBrush(bool inline) =>
 UseHighContrastDiff
 ? new SolidColorBrush(Color.FromArgb(inline ? (byte)200 : (byte)110,0,255,0))
 : new SolidColorBrush(Color.FromArgb(inline ? (byte)140 : (byte)60,0,255,0));

 private Brush GetDeleteBrush(bool inline) =>
 UseHighContrastDiff
 ? new SolidColorBrush(Color.FromArgb(inline ? (byte)200 : (byte)110,255,0,0))
 : new SolidColorBrush(Color.FromArgb(inline ? (byte)140 : (byte)60,255,0,0));

 private Brush GetModifyBrush(bool inline) =>
 UseHighContrastDiff
 ? new SolidColorBrush(Color.FromArgb(inline ? (byte)180 : (byte)100,255,255,0))
 : new SolidColorBrush(Color.FromArgb(inline ? (byte)120 : (byte)70,255,255,0));
 }
}
