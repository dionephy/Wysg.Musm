using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Rendering;

namespace Wysg.Musm.Editor.Ui;

public sealed class GhostTextRenderer : IBackgroundRenderer, IDisposable
{
    private readonly TextView _view;
    private readonly Func<string> _getText;
    private readonly Typeface _typeface;
    private bool _disposed;

    public GhostTextRenderer(TextView view, Func<string> getText)
    {
        _view = view ?? throw new ArgumentNullException(nameof(view));
        _getText = getText ?? throw new ArgumentNullException(nameof(getText));

        // Pick font from the owning TextArea if available; fall back to Consolas
        var ta = (TextArea?)_view.Services.GetService(typeof(TextArea));
        var fontFamily = ta?.FontFamily ?? new FontFamily("Consolas");
        _typeface = new Typeface(fontFamily, FontStyles.Italic, FontWeights.Normal, FontStretches.Normal);

        _view.BackgroundRenderers.Add(this);
        _view.VisualLinesChanged += OnVisualLinesChanged;
    }

    private void OnVisualLinesChanged(object? s, EventArgs e) => _view.InvalidateVisual();

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        if (!textView.VisualLinesValid) return;

        var ghost = _getText();
        if (string.IsNullOrEmpty(ghost)) return;

        var ta = (TextArea?)textView.Services.GetService(typeof(TextArea));
        if (ta is null) return;

        var caretPos = textView.GetVisualPosition(ta.Caret.Position, VisualYPosition.TextTop);
        var origin = new Point(caretPos.X, caretPos.Y);

        double fontSize = ta.FontSize;
        double pixelsPerDip = VisualTreeHelper.GetDpi(textView).PixelsPerDip;

        var ft = new FormattedText(
            ghost,
            CultureInfo.CurrentUICulture,
            FlowDirection.LeftToRight,
            _typeface,
            fontSize,
            Brushes.Gray,
            pixelsPerDip);

        drawingContext.DrawText(ft, origin);
    }

    public KnownLayer Layer => KnownLayer.Selection;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try { _view.BackgroundRenderers.Remove(this); } catch { }
        try { _view.VisualLinesChanged -= OnVisualLinesChanged; } catch { }
    }
}
