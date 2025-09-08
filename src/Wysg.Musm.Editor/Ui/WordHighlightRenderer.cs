using System;
using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace Wysg.Musm.Editor.Ui;

public sealed class WordHighlightRenderer : IBackgroundRenderer, IDisposable
{
    private readonly TextView _view;
    private readonly Func<ISegment?> _getSegment;

    public WordHighlightRenderer(TextView view, Func<ISegment?> getSegment)
    {
        _view = view;
        _getSegment = getSegment;
        _view.BackgroundRenderers.Add(this);
        _view.VisualLinesChanged += OnVisualLinesChanged;
    }

    public KnownLayer Layer => KnownLayer.Selection; // same layer as ghosts is fine

    public void Draw(TextView textView, DrawingContext dc)
    {
        textView.EnsureVisualLines();
        if (!textView.VisualLinesValid) return;

        var seg = _getSegment();
        if (seg == null || seg.Length <= 0) return;

        foreach (var r in BackgroundGeometryBuilder.GetRectsForSegment(textView, seg))
        {
            var rect = new Rect(r.Location, new Size(r.Width, textView.DefaultLineHeight));
            dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(40, 255, 215, 0)), // gold-ish
                             new Pen(new SolidColorBrush(Color.FromArgb(120, 255, 215, 0)), 1),
                             rect);
        }
    }

    private void OnVisualLinesChanged(object? s, EventArgs e) => _view.InvalidateLayer(KnownLayer.Selection);
    public void Dispose()
    {
        _view.BackgroundRenderers.Remove(this);
        _view.VisualLinesChanged -= OnVisualLinesChanged;
    }
}
