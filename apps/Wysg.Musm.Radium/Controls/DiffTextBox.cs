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
    /// Uses DiffPlex for inline diffs.
    /// </summary>
    public class DiffTextBox : RichTextBox
    {
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

  // Use DiffPlex for efficient diff computation
            var differ = new Differ();
    var builder = new InlineDiffBuilder(differ);
   var diff = builder.BuildDiffModel(original, modified, ignoreWhitespace: false);

    // Build FlowDocument with inline formatting
            var doc = new FlowDocument();
var para = new Paragraph();
     para.Margin = new Thickness(0);

            foreach (var line in diff.Lines)
          {
      if (line.Type == ChangeType.Inserted)
        {
       // Brighter green background for insertions
   var run = new Run(line.Text);
 run.Background = GetInsertBrush(inline: true);
 if (UseHighContrastDiff) run.TextDecorations = TextDecorations.Underline;
  para.Inlines.Add(run);
           }
         else if (line.Type == ChangeType.Deleted)
    {
      // Brighter red background + strikethrough for deletions
  var run = new Run(line.Text);
             run.Background = GetDeleteBrush(inline: true);
run.TextDecorations = TextDecorations.Strikethrough;
        para.Inlines.Add(run);
     }
      else if (line.Type == ChangeType.Modified)
           {
           // For modified lines, show character-level diffs
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
    // Fallback: show entire line as modified (yellow tint)
      var run = new Run(line.Text);
       run.Background = GetModifyBrush(inline: true);
       if (UseHighContrastDiff) run.FontWeight = FontWeights.SemiBold;
  para.Inlines.Add(run);
 }
         }
       else // Unchanged
     {
        para.Inlines.Add(new Run(line.Text));
       }
          
  // Add newline if not the last line
    if (line != diff.Lines.Last())
       {
      para.Inlines.Add(new LineBreak());
      }
        }

   doc.Blocks.Add(para);
    Document = doc;
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
