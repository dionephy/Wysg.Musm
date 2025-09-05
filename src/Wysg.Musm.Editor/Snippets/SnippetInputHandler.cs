using System.Linq;
using System.Windows.Input;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;

namespace Wysg.Musm.Editor.Snippets;

public sealed class SnippetInputHandler : TextAreaInputHandler
{
    private readonly PlaceholderSession _session;

    public SnippetInputHandler(TextArea textArea, System.Collections.Generic.IList<Placeholder> placeholders)
        : base(textArea)
    {
        _session = new PlaceholderSession(placeholders);
        _session.Enter();
    }

    public override void Attach()
    {
        base.Attach();
        TextArea.PreviewKeyDown += OnPreviewKeyDown;
    }

    public override void Detach()
    {
        TextArea.PreviewKeyDown -= OnPreviewKeyDown;
        base.Detach();
    }

    private void OnPreviewKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Tab) return;

        _session.FocusAtOffset(TextArea.Caret.Offset);
        var ph = _session.Current;
        if (ph == null) return;

        // Normalize laterality if the selection covers the placeholder text
        var sel = TextArea.Selection;
        if (!sel.IsEmpty && sel.Length <= 512)
        {
            var raw = sel.GetText();
            if (raw.Contains("left") || raw.Contains("right") || raw.Contains("bilateral"))
            {
                var normalized = LateralityCombiner.CombineFromText(raw);
                if (!string.IsNullOrWhiteSpace(normalized) && normalized != raw)
                {
                    using (TextArea.Document.RunUpdate())
                    {
                        TextArea.Document.Replace(sel.SurroundingSegment, normalized);
                    }
                    // Let Tab continue to next placeholder
                }
            }
        }

        // If the placeholder has options, show the picker
        if (ph.Options != null && ph.Options.Count > 0)
        {
            var multi = ph.Type == PlaceholderType.MultiSelect;

            void Pick(string value)
            {
                var seg = ph.Segment as TextSegment;
                if (seg == null) return;

                string current = TextArea.Selection.IsEmpty
                    ? TextArea.Document.GetText(seg)
                    : TextArea.Selection.GetText();

                string next = multi ? AppendUnique(current, value) : value;

                using (TextArea.Document.RunUpdate())
                {
                    if (TextArea.Selection.IsEmpty)
                        TextArea.Document.Replace(seg, next);
                    else
                        TextArea.Document.Replace(TextArea.Selection.SurroundingSegment, next);
                }

                var newLen = next.Length;
                ph.Segment.StartOffset = seg.StartOffset;
                ph.Segment.EndOffset = seg.StartOffset + newLen;
                TextArea.Caret.Offset = ph.Segment.EndOffset;
            }

            var items = ph.Options.Select(kv => (kv.Key, kv.Value));
            var window = new PlaceholderCompletionWindow(
                textArea: TextArea,
                options: items,
                multiSelect: multi,
                onPick: Pick);

            // Show the picker over the placeholder segment
            if (ph.Segment is TextSegment s)
            {
                window.StartOffset = s.StartOffset;
                window.EndOffset = s.EndOffset;
            }
            window.Show();

            if (!multi) e.Handled = true; // single choice: avoid tab jumping past the placeholder
            return;
        }

        // No options → allow default Tab navigation across snippet fields.
    }

    private static string AppendUnique(string current, string value)
    {
        var parts = current.Split(new[] { ',', ' ' }, System.StringSplitOptions.RemoveEmptyEntries).ToList();
        if (!parts.Contains(value))
            parts.Add(value);
        var joined = string.Join(", ", parts);
        return LateralityCombiner.CombineFromText(joined);
    }
}
