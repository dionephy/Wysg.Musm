using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace Wysg.Musm.Editor.Ui;

/// Draws faint boxes over every placeholder; emphasizes the active one.
/// Segments returned by the provider must be ABSOLUTE document offsets.
public sealed class PlaceholderOverlayRenderer : IBackgroundRenderer, IDisposable
{
    private readonly TextView _view;
    private readonly Func<IEnumerable<(int start, int length, bool isActive)>> _segments;

    public PlaceholderOverlayRenderer(TextView view,
                                      Func<IEnumerable<(int start, int length, bool isActive)>> segments)
    {
        _view = view;
        _segments = segments;
        _view.BackgroundRenderers.Add(this);
        _view.VisualLinesChanged += OnChanged;
    }

    public KnownLayer Layer => KnownLayer.Selection;

    public void Draw(TextView textView, DrawingContext dc)
    {
        textView.EnsureVisualLines();
        if (!textView.VisualLinesValid || textView.Document is null) return;

        foreach (var (start, length, isActive) in _segments())
        {
            if (length <= 0) continue;

            var seg = new TextSegment
            {
                StartOffset = Math.Max(0, Math.Min(start, textView.Document.TextLength)),
                Length = Math.Max(0, Math.Min(length, Math.Max(0, textView.Document.TextLength - start)))
            };
            foreach (var r in BackgroundGeometryBuilder.GetRectsForSegment(textView, seg))
            {
                var rect = new Rect(r.Location, new Size(r.Width, textView.DefaultLineHeight));

                // Brighter and more opaque fills for better visibility
                var fill = isActive
                    ? new SolidColorBrush(Color.FromArgb(90, 255, 235, 59))     // active: bright yellow
                    : new SolidColorBrush(Color.FromArgb(60, 33, 150, 243));    // others: vivid blue

                var pen = isActive
                    ? new Pen(new SolidColorBrush(Color.FromArgb(200, 255, 193, 7)), 1.5)
                    : new Pen(new SolidColorBrush(Color.FromArgb(160, 30, 136, 229)), 1.0);

                fill.Freeze(); pen.Freeze();
                dc.DrawRectangle(fill, pen, rect);
            }
        }
    }

    public void Invalidate() => _view.InvalidateLayer(KnownLayer.Selection);

    private void OnChanged(object? s, EventArgs e) => Invalidate();

    public void Dispose()
    {
        _view.BackgroundRenderers.Remove(this);
        _view.VisualLinesChanged -= OnChanged;
    }
}
