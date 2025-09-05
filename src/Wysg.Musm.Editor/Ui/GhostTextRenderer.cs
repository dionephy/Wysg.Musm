using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Editing;    // TextArea
using ICSharpCode.AvalonEdit.Rendering;  // IBackgroundRenderer, TextView, KnownLayer

namespace Wysg.Musm.Editor.Ui
{
    /// <summary>
    /// Draws inline ghost text starting at the caret's right edge.
    /// A hard clip ensures no ghost pixels render left of the caret.
    /// </summary>
    public sealed class GhostTextRenderer : IBackgroundRenderer, IDisposable
    {
        private readonly TextView _view;
        private readonly TextArea _textArea;
        private readonly Func<string> _getText;
        private readonly Func<Typeface> _getTypeface;
        private readonly Func<double> _getFontSize;
        private readonly Brush _brush;

        public GhostTextRenderer(
            TextView view,
            TextArea textArea,
            Func<string> getText,
            Func<Typeface> getTypeface,
            Func<double> getFontSize,
            Brush? brush = null)
        {
            _view = view;
            _textArea = textArea;
            _getText = getText;
            _getTypeface = getTypeface;
            _getFontSize = getFontSize;
            _brush = brush ?? Brushes.Gray;

            _view.BackgroundRenderers.Add(this);
            _view.VisualLinesChanged += OnVisualLinesChanged;
        }

        public KnownLayer Layer => KnownLayer.Selection; // above text, below caret

        public void Draw(TextView textView, DrawingContext dc)
        {
            var ghost = _getText();
            if (string.IsNullOrEmpty(ghost)) return;
            if (!_view.VisualLinesValid) return;

            // Caret geometry
            var caretRect = _textArea.Caret.CalculateCaretRectangle();
            // Anchor at the true insertion point
            var origin = new Point(caretRect.Right, caretRect.Top);

            // Hard clip: nothing from the ghost is allowed to render left of the caret.
            // Height: use the caret rect height (current line). Width: extend to the right edge.
            // NEW — use ActualWidth; clip only to the right of the caret
            double clipWidth = Math.Max(0, _view.ActualWidth - caretRect.Right);
            double clipHeight = Math.Max(0, caretRect.Height);
            var clip = new Rect(caretRect.Right, caretRect.Top, clipWidth, clipHeight);


            dc.PushClip(new RectangleGeometry(clip));

            // Use editor's live typeface + size (ensures identical metrics)
            var typeface = _getTypeface();
            var fontSize = _getFontSize();

            var ft = new FormattedText(
                ghost,
                CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                typeface,
                fontSize,
                _brush,
                VisualTreeHelper.GetDpi(_view).PixelsPerDip);

            dc.DrawText(ft, origin);

            dc.Pop(); // remove clip
        }

        private void OnVisualLinesChanged(object? s, EventArgs e) => _view.InvalidateVisual();

        public void Dispose()
        {
            _view.BackgroundRenderers.Remove(this);
            _view.VisualLinesChanged -= OnVisualLinesChanged;
        }
    }
}
