using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Diagnostics;
using System.Windows.Media; // added
using System.Windows.Data;  // added for binding
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;

namespace Wysg.Musm.Editor.Completion
{
    public sealed class MusmCompletionWindow : CompletionWindow
    {
        private readonly TextEditor _editor;
        private bool _allowSelectionOnce;
        private bool _handlingSelectionChange;
        private const int MaxVisibleItems = 8; // constant cap requested

        public MusmCompletionWindow(TextEditor editor)
            : base(editor?.TextArea ?? throw new ArgumentNullException(nameof(editor)))
        {
            _editor = editor;

            CloseAutomatically = true;
            CompletionList.IsFiltering = true;
            // Width grows with content; height we manage manually each rebuild
            SizeToContent = SizeToContent.Width;
            MaxHeight = 320; // hard ceiling safeguard
            Width = 360;

            try { CompletionList.ListBox.FontFamily = _editor.FontFamily; } catch { }
            try { CompletionList.ListBox.FontSize = _editor.FontSize; } catch { }
            try { CompletionList.ListBox.SelectedIndex = -1; } catch { }
            try { CompletionList.ListBox.SelectionChanged += OnListSelectionChanged; } catch { }
            try { CompletionList.ListBox.PreviewKeyDown += OnListPreviewKeyDown; } catch { }
            try { CompletionList.ListBox.KeyDown += OnListKeyDown; } catch { }
            try { TextArea.Caret.PositionChanged += OnCaretPositionChanged; } catch { }

            // Intercept Home/End at the window level to prevent underlying controls from handling them
            try { PreviewKeyDown += OnWindowPreviewKeyDown; } catch { }

            // Apply dark + minimal styling
            TryApplyDarkTheme();
        }

        private void OnWindowPreviewKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key is Key.Home or Key.End)
            {
                // Ensure only editor processes Home/End
                e.Handled = true;
                var line = _editor.Document.GetLineByOffset(_editor.CaretOffset);
                _editor.CaretOffset = (e.Key == Key.Home) ? line.Offset : line.EndOffset;
                if (_editor.CaretOffset < StartOffset || _editor.CaretOffset > EndOffset) Close();
                _editor.TextArea.Focus();
            }
        }

        private void TryApplyDarkTheme()
        {
            try
            {
                // Remove standard window chrome which can show a light top strip
                try
                {
                    WindowStyle = WindowStyle.None;
                    AllowsTransparency = true; // enable full client-area control
                    ShowInTaskbar = false;
                }
                catch { }

                // We'll keep outer window transparent and draw our own background
                Background = Brushes.Transparent;
                BorderThickness = new Thickness(0);

                // Wrap existing content (normally a Grid) so we can guarantee no stray top padding/border
                if (Content is UIElement existing && existing is not Border)
                {
                    var wrapper = new Border
                    {
                        Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                        Padding = new Thickness(0),
                        BorderThickness = new Thickness(1), // visible border
                        BorderBrush = new SolidColorBrush(Color.FromRgb(65, 65, 65)),
                        CornerRadius = new CornerRadius(4),
                        SnapsToDevicePixels = true
                    };
                    Content = null; // detach first
                    wrapper.Child = existing;
                    Content = wrapper;
                }
                else if (Content is Border bdr)
                {
                    bdr.Background = new SolidColorBrush(Color.FromRgb(30, 30, 30));
                    bdr.Padding = new Thickness(0);
                    bdr.BorderThickness = new Thickness(1);
                    bdr.BorderBrush = new SolidColorBrush(Color.FromRgb(65, 65, 65));
                    if (bdr.CornerRadius == default) bdr.CornerRadius = new CornerRadius(4);
                }

                if (CompletionList?.ListBox is ListBox lb)
                {
                    lb.Background = new SolidColorBrush(Color.FromRgb(30, 30, 30));
                    lb.BorderThickness = new Thickness(0);
                    lb.Padding = new Thickness(0);
                    lb.Margin = new Thickness(0);
                    lb.FocusVisualStyle = null;
                    lb.Foreground = new SolidColorBrush(Color.FromRgb(220, 220, 220));
                    ScrollViewer.SetHorizontalScrollBarVisibility(lb, ScrollBarVisibility.Disabled);
                    ScrollViewer.SetVerticalScrollBarVisibility(lb, ScrollBarVisibility.Auto);

                    // Item template: bind to Content to show "{trigger} → {description}" for snippets
                    var dt = new DataTemplate();
                    var f = new FrameworkElementFactory(typeof(TextBlock));
                    f.SetBinding(TextBlock.TextProperty, new Binding("Content")); // ✅ Changed from "Text" to "Content"
                    f.SetValue(TextBlock.MarginProperty, new Thickness(0));
                    f.SetValue(TextBlock.PaddingProperty, new Thickness(0));
                    f.SetValue(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis);
                    f.SetValue(TextBlock.ForegroundProperty, new SolidColorBrush(Color.FromRgb(220,220,220)));
                    dt.VisualTree = f;
                    lb.ItemTemplate = dt;

                    // Item container style (selection visuals)
                    var itemStyle = new Style(typeof(ListBoxItem));
                    itemStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(6, 2, 6, 2)));
                    itemStyle.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
                    itemStyle.Setters.Add(new Setter(Control.ForegroundProperty, new SolidColorBrush(Color.FromRgb(220, 220, 220))));
                    itemStyle.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0)));
                    itemStyle.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Left));
                    itemStyle.Setters.Add(new Setter(FrameworkElement.FocusVisualStyleProperty, null));

                    var selTrigger = new Trigger { Property = ListBoxItem.IsSelectedProperty, Value = true };
                    selTrigger.Setters.Add(new Setter(Control.BackgroundProperty, new SolidColorBrush(Color.FromRgb(50, 50, 50))));
                    selTrigger.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.White));
                    itemStyle.Triggers.Add(selTrigger);

                    var hoverTrigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
                    hoverTrigger.Setters.Add(new Setter(Control.BackgroundProperty, new SolidColorBrush(Color.FromRgb(45, 45, 45))));
                    itemStyle.Triggers.Add(hoverTrigger);

                    lb.ItemContainerStyle = itemStyle;
                }

                // If a Grid is still directly the content (unlikely after wrap), ensure no padding
                if (Content is Grid g)
                {
                    g.Background = Brushes.Transparent; // wrapper border supplies bg
                    g.Margin = new Thickness(0);
                }
            }
            catch { /* non-fatal */ }
        }

        public void AdjustListBoxHeight()
        {
            if (CompletionList?.ListBox is not { } lb) return;

            // Force generation of first container so we can measure actual item height
            if (lb.Items.Count > 0)
            {
                // Apply a quick layout pass
                lb.UpdateLayout();
            }

            double itemHeight = 0;
            if (lb.Items.Count > 0)
            {
                var container = lb.ItemContainerGenerator.ContainerFromIndex(0) as ListBoxItem;
                if (container != null)
                {
                    container.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                    itemHeight = container.DesiredSize.Height;
                }
            }
            if (itemHeight <= 0) itemHeight = lb.FontSize * 1.35; // heuristic fallback
            if (itemHeight < 14) itemHeight = 14; // lower bound
            if (itemHeight > 40) itemHeight = 40; // safety upper bound

            int count = lb.Items.Count;
            int visible = Math.Min(count, MaxVisibleItems);
            double listHeight = visible * itemHeight + 4; // padding for borders

            // Apply fixed (not just MaxHeight) so window shrinks when item count smaller
            lb.Height = listHeight;
            lb.MaxHeight = listHeight; // lock exact height (scrollbar appears if more items than visible)

            // Derive window chrome overhead (empirical small constant)
            double chrome = 12; // reduced for minimal chrome
            double desiredWindowHeight = listHeight + chrome;
            if (desiredWindowHeight > MaxHeight) desiredWindowHeight = MaxHeight;

            // Only set if different to avoid layout churn
            if (Math.Abs(Height - desiredWindowHeight) > 0.5)
            {
                Height = desiredWindowHeight;
            }
        }

        private void OnListSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_handlingSelectionChange) return;
            if (_allowSelectionOnce)
            {
                _allowSelectionOnce = false;
                return;
            }
            if (CompletionList?.ListBox is { } lb && (lb.IsFocused || lb.IsKeyboardFocusWithin))
        {
         return;
      }
            if (IsFocused || CompletionList?.IsFocused == true)
            {
 return;
     }
       if (CompletionList?.ListBox is { } clb && clb.SelectedIndex == -1)
            {
    return;
  }
            if (e.AddedItems?.Count > 0)
     {
       return;
      }
     if (CompletionList?.ListBox is { } clearListBox && clearListBox.SelectedIndex != -1)
            {
      _handlingSelectionChange = true;
         try { clearListBox.SelectedIndex = -1; }
     finally { _handlingSelectionChange = false; }
            }
        }

 private void OnListPreviewKeyDown(object? sender, KeyEventArgs e)
        {
    if (CompletionList?.ListBox is null) return;

            if (e.Key == Key.Down)
            {
                e.Handled = true;
       var lb = CompletionList.ListBox;
     int count = lb.Items.Count;
   if (count == 0) return;
      int idx = lb.SelectedIndex;
                if (idx < 0) idx = -1; // first Down selects index 0
        int newIdx = Math.Min(idx + 1, count - 1);
    _allowSelectionOnce = true;
       lb.SelectedIndex = newIdx;
            lb.ScrollIntoView(lb.SelectedItem);
                return;
            }
            if (e.Key == Key.Up)
       {
          e.Handled = true;
       var lb = CompletionList.ListBox;
     int count = lb.Items.Count;
      if (count == 0) return;
  int idx = lb.SelectedIndex;
       if (idx < 0) idx = count; // first Up selects last index
  int newIdx = Math.Max(idx - 1, 0);
       _allowSelectionOnce = true;
          lb.SelectedIndex = newIdx;
 lb.ScrollIntoView(lb.SelectedItem);
  return;
            }

        if (e.Key is Key.Enter or Key.Return)
     {
     if (CompletionList.ListBox.SelectedIndex == -1)
     {
          e.Handled = true;
       Close();
  var off = _editor.CaretOffset;
     var nl = Environment.NewLine;
          _editor.Document.Insert(off, nl);
 _editor.CaretOffset = off + nl.Length;
                }
      else
       {
 e.Handled = true;
        CompletionList.RequestInsertion(e);
           }
          return;
}
            if (e.Key == Key.Space)
       {
                // Cancel space so it does not insert into the document; treat like Enter if selection exists
          e.Handled = true;
 if (CompletionList.ListBox.SelectedIndex >= 0)
            {
    // Request insertion, then append a space at caret
        CompletionList.RequestInsertion(e);
     try
     {
     var off = _editor.CaretOffset;
      _editor.Document.Insert(off, " ");
_editor.CaretOffset = off + 1;
           }
      catch { }
     }
     return;
   }
     if (e.Key == Key.Home)
     {
     e.Handled = true;
         var line = _editor.Document.GetLineByOffset(_editor.CaretOffset);
                _editor.CaretOffset = line.Offset;
           if (_editor.CaretOffset < StartOffset || _editor.CaretOffset > EndOffset) Close();
       _editor.TextArea.Focus();
           return;
       }
            if (e.Key == Key.End)
          {
        e.Handled = true;
         var line = _editor.Document.GetLineByOffset(_editor.CaretOffset);
       _editor.CaretOffset = line.EndOffset;
          if (_editor.CaretOffset < StartOffset || _editor.CaretOffset > EndOffset) Close();
    _editor.TextArea.Focus();
 return;
            }
        }

    private void OnListKeyDown(object? sender, KeyEventArgs e)
 {
            // Safety: ensure ListBox doesn't process Home/End even if Preview was bypassed
            if (e.Key == Key.Home)
            {
          e.Handled = true;
         var line = _editor.Document.GetLineByOffset(_editor.CaretOffset);
      _editor.CaretOffset = line.Offset;
    if (_editor.CaretOffset < StartOffset || _editor.CaretOffset > EndOffset) Close();
                _editor.TextArea.Focus();
            }
     else if (e.Key == Key.End)
          {
      e.Handled = true;
      var line = _editor.Document.GetLineByOffset(_editor.CaretOffset);
       _editor.CaretOffset = line.EndOffset;
 if (_editor.CaretOffset < StartOffset || _editor.CaretOffset > EndOffset) Close();
       _editor.TextArea.Focus();
      }
        }

     private void OnCaretPositionChanged(object? sender, EventArgs e)
 {
            int caret = TextArea.Caret.Offset;
   if (caret < StartOffset || caret > EndOffset)
   {
                if (CloseAutomatically) Close();
      return;
            }
    var (word, ok) = TryGetWordAtCaret();
        if (!ok || string.IsNullOrEmpty(word))
            {
    if (CloseAutomatically) Close();
          }
      }

        private (string word, bool ok) TryGetWordAtCaret()
      {
    var doc = _editor.Document;
            if (doc is null) return (string.Empty, false);
int caret = _editor.CaretOffset;
            var line = doc.GetLineByOffset(caret);
            string lineText = doc.GetText(line);
   int local = Math.Clamp(caret - line.Offset, 0, lineText.Length);
     var (startLocal, endLocal) = WordBoundaryHelper.ComputeWordSpan(lineText, local);
         if (endLocal <= startLocal) return (string.Empty, true);
            var word = lineText.Substring(startLocal, endLocal - startLocal);
      return (word, true);
        }

        /// <summary>Compute StartOffset/EndOffset from the word around caret.</summary>
        public void ComputeReplaceRegionFromCaret()
        {
      var doc = _editor.Document;
    int caret = _editor.CaretOffset;

        var line = doc.GetLineByOffset(caret);
       string lineText = doc.GetText(line);
            int local = Math.Clamp(caret - line.Offset, 0, lineText.Length);

    var (startLocal, endLocal) = WordBoundaryHelper.ComputeWordSpan(lineText, local);
  StartOffset = line.Offset + startLocal;
            EndOffset = line.Offset + endLocal;
        }

        /// <summary>One-liner to show a completion window for current word.</summary>
        public static MusmCompletionWindow ShowForCurrentWord(TextEditor editor, IEnumerable<ICompletionData> items)
        {
     var w = new MusmCompletionWindow(editor);
  var target = w.CompletionList.CompletionData;
         foreach (var item in items) target.Add(item);
            w.ComputeReplaceRegionFromCaret();
     w.Show();
            // Auto-select first item by default (new behavior FR-264)
   w.EnsureFirstItemSelected();
            return w;
        }

        /// <summary>
    /// Select the first item by default to keep navigation predictable; do not auto-select exact matches.
     /// </summary>
 public void SelectExactOrNone(string word)
      {
     if (CompletionList?.ListBox is null) return;
 var lb = CompletionList.ListBox;
          if (lb.Items.Count == 0) return;
  if (lb.SelectedIndex == -1)
            {
           _allowSelectionOnce = true;
     lb.SelectedIndex = 0;
       lb.ScrollIntoView(lb.SelectedItem);
      }
      AdjustListBoxHeight();
        }

        public void EnsureFirstItemSelected()
        {
   if (CompletionList?.ListBox is not { } lb) return;
            if (lb.Items.Count == 0) return;
      if (lb.SelectedIndex == -1)
            {
     _allowSelectionOnce = true;
          lb.SelectedIndex = 0;
      lb.ScrollIntoView(lb.SelectedItem);
  }
   }

    /// <summary>
        /// Allow one selection change (e.g., caused by Up/Down).
  /// </summary>
     public void AllowSelectionByKeyboardOnce() => _allowSelectionOnce = true;

        /// <summary>
        /// Update the ListBox selection without triggering the guard logic.
     /// </summary>
   public void SetSelectionSilently(int index)
        {
       if (CompletionList?.ListBox is not { } lb) return;
     _handlingSelectionChange = true;
 try
          {
    if (index >= 0 && index < lb.Items.Count)
   {
     lb.SelectedIndex = index;
        lb.ScrollIntoView(lb.SelectedItem);
  }
     else lb.SelectedIndex = -1;
    }
         finally { _handlingSelectionChange = false; }
    }
    }
}