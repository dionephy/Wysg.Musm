// src/Wysg.Musm.Editor/Ui/MultiLineGhostRenderer.cs
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit;             // TextViewPosition
using ICSharpCode.AvalonEdit.Document;    // DocumentLine
using ICSharpCode.AvalonEdit.Editing;     // TextArea
using ICSharpCode.AvalonEdit.Rendering;   // IBackgroundRenderer, TextView, KnownLayer

namespace Wysg.Musm.Editor.Ui;

public sealed class MultiLineGhostRenderer : IBackgroundRenderer, IDisposable
{
    private readonly TextView _view;
    private readonly TextArea _area;
    private readonly Func<IReadOnlyList<(int lineNumber, string text)>> _getItems;
    private readonly Func<int> _getSelectedIndex;
    private readonly Typeface _typeface;              // <— ensure this exists
    private readonly Func<double> _getFontSize;

    public MultiLineGhostRenderer(
        TextView view,
        TextArea area,
        Func<IReadOnlyList<(int lineNumber, string text)>> getItems,
        Func<int> getSelectedIndex,
        Typeface typeface,
        Func<double> getFontSize)
    {
        _view = view;
        _area = area;
        _getItems = getItems;
        _getSelectedIndex = getSelectedIndex;
        _typeface = typeface;                         // <— set it here
        _getFontSize = getFontSize;

        _view.BackgroundRenderers.Add(this);
        _view.VisualLinesChanged += (_, __) => _view.InvalidateVisual();
    }

    public KnownLayer Layer => KnownLayer.Selection;

    public void Draw(TextView textView, DrawingContext dc)
    {
        // Realize lines first so idle renders without a click
        textView.EnsureVisualLines();
        if (!textView.VisualLinesValid) return;

        var items = _getItems() ?? Array.Empty<(int lineNumber, string text)>();
        if (items.Count == 0) return;

        var selectedIndex = _getSelectedIndex();

        for (int i = 0; i < items.Count; i++)
        {
            var (ln, ghostText) = items[i];
            if (_area.Document == null || ln < 1 || ln > _area.Document.LineCount) continue;

            var line = _area.Document.GetLineByNumber(ln);

            var tvp = new TextViewPosition(ln, int.MaxValue);
            var pos = textView.GetVisualPosition(tvp, VisualYPosition.TextTop);
            if (double.IsNaN(pos.X) || double.IsNaN(pos.Y)) continue;

            var origin = new Point(pos.X, pos.Y);

            if (i == selectedIndex)
            {
                var rect = new Rect(origin.X, origin.Y,
                                    Math.Max(60, textView.ActualWidth - origin.X),
                                    textView.DefaultLineHeight);
                dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(28, 0, 128, 255)), null, rect);
            }

            var ft = new FormattedText(
                ghostText ?? string.Empty,
                CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                _typeface,                                  // <— use the field here
                _getFontSize(),
                i == selectedIndex ? Brushes.DeepSkyBlue : Brushes.Gray,
                VisualTreeHelper.GetDpi(textView).PixelsPerDip);

            double clipWidth = Math.Max(0, textView.ActualWidth - origin.X);
            var clip = new Rect(origin.X, origin.Y, clipWidth, textView.DefaultLineHeight);
            dc.PushClip(new RectangleGeometry(clip));
            dc.DrawText(ft, origin);
            dc.Pop();
        }
    }

    public void Dispose()
    {
        _view.BackgroundRenderers.Remove(this);
        _view.VisualLinesChanged -= (_, __) => _view.InvalidateVisual();
    }
}
