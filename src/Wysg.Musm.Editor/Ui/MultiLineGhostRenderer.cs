using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Rendering;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace Wysg.Musm.Editor.Ui
{
    /// <summary>
    /// Draw server ghosts at the end of specified lines (server 0-based -> document 1-based).
    /// For debugging, we render on KnownLayer.Caret so it’s on top of everything.
    /// NOTE: This renderer does NOT self-register. Call Attach() from the control.
    /// </summary>
    public sealed class MultiLineGhostRenderer : IBackgroundRenderer, IDisposable
    {
        private readonly TextView _view;
        private readonly TextArea _area;
        private readonly Func<IReadOnlyList<(int line, string text)>> _getItems;
        private readonly Func<int> _getSelectedIndex; // -1 if none
        private readonly Typeface _typeface;
        private readonly Func<double> _getFontSize;
        private readonly bool _showAnchors;

        public MultiLineGhostRenderer(
            TextView view,
            TextArea area,
            Func<IReadOnlyList<(int line, string text)>> getItems,
            Func<int> getSelectedIndex,
            Typeface typeface,
            Func<double> getFontSize,
            bool showAnchors = false)
        {
            _view = view;
            _area = area;
            _getItems = getItems;
            _getSelectedIndex = getSelectedIndex;
            _typeface = typeface;
            _getFontSize = getFontSize;
            _showAnchors = showAnchors;
        }

        public void Attach()
        {
            if (!_view.BackgroundRenderers.OfType<MultiLineGhostRenderer>().Any())
            {
                _view.BackgroundRenderers.Add(this);
                //_view.VisualLinesChanged += OnVisualLinesChanged;
                _view.VisualLinesChanged += (_, __) => _view.InvalidateLayer(KnownLayer.Text);

                //Debug.WriteLine("[GhostRenderer] Attached to TextView.");
            }
        }

        public void Detach()
        {
            //_view.VisualLinesChanged -= OnVisualLinesChanged;
            _view.VisualLinesChanged += (_, __) => _view.InvalidateLayer(KnownLayer.Text);

            _view.BackgroundRenderers.Remove(this);
            //Debug.WriteLine("[GhostRenderer] Detached from TextView.");
        }

        private void OnVisualLinesChanged(object? s, EventArgs e)
        {
            _view.InvalidateLayer(Layer); // match our layer
            Debug.WriteLine("[GhostRenderer] VisualLinesChanged → invalidate.");
        }

        // Put the test as high as possible for visibility
        //public KnownLayer Layer => KnownLayer.Caret;
        public KnownLayer Layer => KnownLayer.Text;


        public void Draw(TextView textView, DrawingContext dc)
        {
            var doc = textView.Document;
            if (doc is null) return;

            textView.EnsureVisualLines();

            var items = _getItems();
            int count = items?.Count ?? 0;
            Debug.WriteLine($"[GhostRenderer] Draw start. items={count}, docLines={doc.LineCount}");

            if (items is null || count == 0) return;

            int selectedZeroBased =
                (_getSelectedIndex() is int idx && idx >= 0 && idx < items.Count)
                ? items[idx].line
                : -1;

            foreach (var (zeroBased, rawGhost) in items)
            {
                var ghost = rawGhost?.Trim();
                if (string.IsNullOrEmpty(ghost)) continue;

                int lineNo = zeroBased + 1; // 0-based -> 1-based
                if (lineNo < 1 || lineNo > doc.LineCount) continue;

                DocumentLine line = doc.GetLineByNumber(lineNo);

                // Only draw when the line is realized (visible)
                var vline = textView.VisualLines.FirstOrDefault(v =>
                    v.FirstDocumentLine.LineNumber <= lineNo &&
                    v.LastDocumentLine.LineNumber >= lineNo);
                if (vline is null)
                {
                    Debug.WriteLine($"[GhostRenderer] line {lineNo} not realized ⇒ skip draw.");
                    continue;
                }

                // Anchor at trimmed end-of-line
                int end = line.EndOffset;
                while (end > line.Offset)
                {
                    char ch = doc.GetCharAt(end - 1);
                    if (!char.IsWhiteSpace(ch)) break;
                    end--;
                }
                end = Math.Max(line.Offset, Math.Min(end, doc.TextLength));

                var loc = doc.GetLocation(end);
                var pos = textView.GetVisualPosition(new TextViewPosition(loc), VisualYPosition.TextTop);
                if (double.IsNaN(pos.X) || double.IsNaN(pos.Y))
                {
                    Debug.WriteLine($"[GhostRenderer] NaN pos for line {lineNo} ⇒ skip.");
                    continue;
                }

                bool isSelected = (selectedZeroBased == zeroBased);

                if (_showAnchors)
                {
                    var anchor = new Rect(pos.X - 2, pos.Y + textView.DefaultLineHeight - 4, 4, 4);
                    dc.DrawRectangle(Brushes.OrangeRed, null, anchor);
                }

                // Optional faint highlight for the selected ghost (for debug)
                if (isSelected)
                {
                    var hiRect = new Rect(pos.X, pos.Y,
                        Math.Max(1, textView.ActualWidth - pos.X),
                        textView.DefaultLineHeight);
                    dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(28, 100, 149, 237)), null, hiRect);
                }

                var ft = new FormattedText(
                    " " + ghost,
                    CultureInfo.CurrentUICulture,
                    FlowDirection.LeftToRight,
                    _typeface,
                    _getFontSize(),
                    Brushes.Gray,
                    VisualTreeHelper.GetDpi(textView).PixelsPerDip);

                // Clip to line
                double clipWidth = Math.Max(0, textView.ActualWidth - pos.X);
                var clip = new Rect(pos.X, pos.Y, clipWidth, textView.DefaultLineHeight);
                dc.PushClip(new RectangleGeometry(clip));
                dc.DrawText(ft, new Point(pos.X, pos.Y));
                dc.Pop();

                Debug.WriteLine($"[GhostRenderer] Drew ghost on line {lineNo} at ({pos.X:0.0},{pos.Y:0.0}).");
            }
        }

        public void Dispose() => Detach();
    }
}
