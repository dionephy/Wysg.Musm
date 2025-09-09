// src/Wysg.Musm.Editor/Ui/CompletionGhostRenderer.cs
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace Wysg.Musm.Editor.Ui;

public sealed class CompletionGhostRenderer : IBackgroundRenderer, IDisposable
{
    private readonly TextView _view;
    private readonly Func<(ISegment? seg, string text)> _get;
    private readonly Func<Typeface> _getTypeface;
    private readonly Func<double> _getFontSize;

    public CompletionGhostRenderer(TextView view,
                                   Func<(ISegment? seg, string text)> get,
                                   Func<Typeface> getTypeface,
                                   Func<double> getFontSize)
    {
        _view = view;
        _get = get;
        _getTypeface = getTypeface;
        _getFontSize = getFontSize;

        _view.BackgroundRenderers.Add(this);
        _view.VisualLinesChanged += OnVisualLinesChanged;
    }

    public KnownLayer Layer => KnownLayer.Selection;

    public void Draw(TextView textView, DrawingContext dc)
    {
        textView.EnsureVisualLines();
        if (!textView.VisualLinesValid) return;

        var (seg, text) = _get();
        if (seg is null || string.IsNullOrWhiteSpace(text)) return;

        var doc = textView.Document;
        if (doc is null) return;

        // --- Clamp the segment to the current document to avoid OOR during edits ---
        int start = Math.Max(0, Math.Min(seg.Offset, doc.TextLength));
        int end = Math.Max(start, Math.Min(seg.EndOffset, doc.TextLength));
        if (end <= start) return;

        // Anchor at end of the (clamped) highlighted word
        var loc = doc.GetLocation(end);
        var pos = textView.GetVisualPosition(new TextViewPosition(loc.Line, loc.Column), VisualYPosition.TextTop);
        if (double.IsNaN(pos.X) || double.IsNaN(pos.Y)) return;

        var origin = new Point(pos.X, pos.Y);
        var ft = new FormattedText(
            " " + text.Trim(),
            CultureInfo.CurrentUICulture,
            FlowDirection.LeftToRight,
            _getTypeface(),
            _getFontSize(),
            Brushes.Gray,
            VisualTreeHelper.GetDpi(textView).PixelsPerDip);

        // Clip to line end
        double clipWidth = Math.Max(0, textView.ActualWidth - origin.X);
        var clip = new Rect(origin.X, origin.Y, clipWidth, textView.DefaultLineHeight);
        dc.PushClip(new RectangleGeometry(clip));
        dc.DrawText(ft, origin);
        dc.Pop();
    }

    private void OnVisualLinesChanged(object? s, EventArgs e)
        => _view.InvalidateLayer(KnownLayer.Selection);

    public void Dispose()
    {
        _view.BackgroundRenderers.Remove(this);
        _view.VisualLinesChanged -= OnVisualLinesChanged;
    }
}
