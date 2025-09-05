using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using Wysg.Musm.Editor.Completion; // WordBoundaryHelper
using Wysg.Musm.Editor.Snippets;

namespace Wysg.Musm.Editor.Playground.Completion;

public sealed class WordListCompletionProvider : ISnippetProvider
{
    private readonly IReadOnlyList<string> _words;
    private readonly ImageSource? _icon;

    public WordListCompletionProvider(IEnumerable<string> words, ImageSource? icon = null)
    {
        _words = words.Distinct().OrderBy(w => w).ToArray();
        _icon = icon;
    }

    public IEnumerable<ICompletionData> GetCompletions(TextEditor editor)
    {
        var doc = editor.Document;
        int caret = editor.CaretOffset;
        var line = doc.GetLineByOffset(caret);
        string lineText = doc.GetText(line);
        int local = System.Math.Clamp(caret - line.Offset, 0, lineText.Length);

        var (startLocal, _) = WordBoundaryHelper.ComputeWordSpan(lineText, local);
        string prefix = lineText.Substring(startLocal, local - startLocal);

        if (prefix.Length < 2) yield break; // don’t show noise

        // simple case-insensitive prefix match
        var q = prefix.ToLowerInvariant();
        foreach (var w in _words.Where(w => w.ToLowerInvariant().StartsWith(q)))
            yield return new SimpleCompletionData(w, _icon);
    }

    private sealed class SimpleCompletionData : ICompletionData
    {
        public SimpleCompletionData(string text, ImageSource? icon)
        {
            Text = text;
            Image = icon;
            Content = text;
            Description = text;
        }

        public ImageSource? Image { get; }
        public string Text { get; }
        public object Content { get; }
        public object Description { get; }
        public double Priority => 0;

        public void Complete(TextArea textArea, ISegment completionSegment, System.EventArgs insertionRequestEventArgs)
        {
            // Replace the segment chosen by MusmCompletionWindow (current word region)
            textArea.Document.Replace(completionSegment, Text);
        }
    }
}
