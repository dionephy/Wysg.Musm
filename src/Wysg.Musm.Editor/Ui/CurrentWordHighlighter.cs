using System.Windows.Media;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.Document;

namespace Wysg.Musm.Editor.Ui
{
    internal sealed class CurrentWordHighlighter : IBackgroundRenderer
    {
        private readonly TextEditor _editor;
        private readonly System.Func<ISegment?> _getSeg;
        public CurrentWordHighlighter(TextEditor editor, System.Func<ISegment?> getSeg)
        {
            _editor = editor;
            _getSeg = getSeg;
            _editor.TextArea.TextView.BackgroundRenderers.Add(this);
        }
        public KnownLayer Layer => KnownLayer.Selection;

        public void Draw(TextView textView, DrawingContext dc)
        {
            var seg = _getSeg();
            if (seg is null || seg.Length <= 0) return;
            textView.EnsureVisualLines();

            var bg = new SolidColorBrush(Color.FromArgb(48, 80, 160, 240));
            var pen = new Pen(new SolidColorBrush(Color.FromArgb(96, 80, 160, 240)), 1);

            foreach (var r in BackgroundGeometryBuilder.GetRectsForSegment(textView, seg))
                dc.DrawRectangle(bg, pen, new System.Windows.Rect(r.Location, r.Size));
        }
    }
}
