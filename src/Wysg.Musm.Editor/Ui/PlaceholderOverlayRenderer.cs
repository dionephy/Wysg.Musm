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

                var fill = isActive
                    ? new SolidColorBrush(Color.FromArgb(50, 255, 215, 0))     // active (gold-ish)
                    : new SolidColorBrush(Color.FromArgb(28, 64, 156, 255));   // others (soft blue)

                var pen = isActive
                    ? new Pen(new SolidColorBrush(Color.FromArgb(160, 255, 215, 0)), 1)
                    : new Pen(new SolidColorBrush(Color.FromArgb(120, 64, 156, 255)), 1);

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
