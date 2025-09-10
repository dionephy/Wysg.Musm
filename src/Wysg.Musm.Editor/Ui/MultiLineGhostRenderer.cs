using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Rendering;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace Wysg.Musm.Editor.Ui
{
    /// <summary>
    /// Draws multi-line ghost suggestions at the trimmed end of each target line.
    /// Server indices are 0-based; AvalonEdit lines are 1-based.
    /// </summary>
    public sealed class MultiLineGhostRenderer : IBackgroundRenderer, IDisposable
    {
        private readonly TextView _view;
        private readonly TextArea _area;
        private readonly Func<IReadOnlyList<(int line, string text)>> _getItems;
        private readonly Func<int>? _getSelectedIndex;   // optional; if null, no selection highlight
        private readonly Typeface _typeface;
        private readonly Func<double> _getFontSize;
        private bool _showAnchors;

        private EventHandler? _visualChangedHandler;
        private bool _attached;

        public MultiLineGhostRenderer(
            TextView view,
            TextArea area,
            Func<IReadOnlyList<(int line, string text)>> getItems,
            Func<int>? getSelectedIndex,
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

        /// <summary>Attach to TextView (idempotent).</summary>
        public void Attach()
        {
            if (_attached) return;
            if (!_view.BackgroundRenderers.OfType<MultiLineGhostRenderer>().Any())
                _view.BackgroundRenderers.Add(this);

            _visualChangedHandler = (_, __) => _view.InvalidateLayer(Layer);
            _view.VisualLinesChanged += _visualChangedHandler;
            _attached = true;
        }

        /// <summary>Detach from TextView (idempotent).</summary>
        public void Detach()
        {
            if (!_attached) return;
            if (_visualChangedHandler is not null)
                _view.VisualLinesChanged -= _visualChangedHandler;
            _visualChangedHandler = null;

            _view.BackgroundRenderers.Remove(this);
            _attached = false;
        }

        public void SetShowAnchors(bool on)
        {
            _showAnchors = on;
            _view.InvalidateLayer(Layer);
        }

        /// <summary>
        /// Render above background/selection; caret/adorners will still be on top.
        /// </summary>
        public KnownLayer Layer => KnownLayer.Selection;

        public void Draw(TextView textView, DrawingContext dc)
        {
            var doc = textView.Document;
            if (doc is null) return;

            textView.EnsureVisualLines();
            if (!textView.VisualLinesValid) return;

            var items = _getItems();
            if (items is null || items.Count == 0) return;

            int selectedItemIndex = _getSelectedIndex?.Invoke() ?? -1;
            int selectedLineZero =
                (selectedItemIndex >= 0 && selectedItemIndex < items.Count)
                ? items[selectedItemIndex].line
                : -1;

            foreach (var (zeroBased, raw) in items)
            {
                var ghost = raw?.Trim();
                if (string.IsNullOrEmpty(ghost)) continue;

                int lineNo = zeroBased + 1; // AvalonEdit uses 1-based line numbers
                if (lineNo < 1 || lineNo > doc.LineCount) continue;

                // Make sure the visual line exists (must be in viewport)
                var dl = doc.GetLineByNumber(lineNo);
                var vl = textView.GetOrConstructVisualLine(dl);
                if (vl is null) continue; // not visible -> skip

                // Logical end-of-text (trim trailing whitespace)
                int end = dl.EndOffset;
                while (end > dl.Offset && char.IsWhiteSpace(doc.GetCharAt(end - 1))) end--;

                // Let AvalonEdit compute the text view position from the document location
                var loc = doc.GetLocation(Math.Min(end, doc.TextLength));
                var tvp = new ICSharpCode.AvalonEdit.TextViewPosition(loc);

                // Map to visual coordinates; if invalid, fall back to "after end of line"
                var pos = textView.GetVisualPosition(tvp, ICSharpCode.AvalonEdit.Rendering.VisualYPosition.TextTop);
                if (double.IsNaN(pos.X) || double.IsNaN(pos.Y))
                {
                    tvp = new ICSharpCode.AvalonEdit.TextViewPosition(lineNo, int.MaxValue);
                    pos = textView.GetVisualPosition(tvp, ICSharpCode.AvalonEdit.Rendering.VisualYPosition.TextTop);
                    if (double.IsNaN(pos.X) || double.IsNaN(pos.Y)) continue;
                }

                var origin = new Point(pos.X, vl.VisualTop);
                bool isSelected = (selectedLineZero == zeroBased);

                if (_showAnchors)
                {
                    var anchor = new Rect(origin.X - 2, origin.Y + vl.Height - 4, 4, 4);
                    dc.DrawRectangle(Brushes.OrangeRed, null, anchor);
                }

                if (isSelected)
                {
                    var hi = new Rect(origin.X, origin.Y, Math.Max(1, textView.ActualWidth - origin.X), vl.Height);
                    dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(64, 100, 149, 237)), null, hi);
                }

                var ft = new FormattedText(
                    " " + ghost, // leading space for separation
                    System.Globalization.CultureInfo.CurrentUICulture,
                    FlowDirection.LeftToRight,
                    _typeface,
                    _getFontSize(),
                    Brushes.Gray,
                    VisualTreeHelper.GetDpi(textView).PixelsPerDip);

                // Clip to the visual line bounds
                double clipWidth = Math.Max(0, textView.ActualWidth - origin.X);
                var clip = new Rect(origin.X, vl.VisualTop, clipWidth, vl.Height);
                dc.PushClip(new RectangleGeometry(clip));
                dc.DrawText(ft, origin);
                dc.Pop();
            }
        }





        public void Dispose() => Detach();
    }
}
