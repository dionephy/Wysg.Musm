using System;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;

namespace Wysg.Musm.Editor.Playground
{
    public sealed class WordListCompletionProvider : Wysg.Musm.Editor.Snippets.ISnippetProvider
    {
        private readonly string[] _words;
        public WordListCompletionProvider(IEnumerable<string> words) => _words = words.ToArray();

        public IEnumerable<ICompletionData> GetCompletions(TextEditor editor)
        {
            var (seg, word) = GetCurrentWord(editor);
            if (seg is null || string.IsNullOrWhiteSpace(word) || word.Length < 2)
                return Enumerable.Empty<ICompletionData>();

            return _words
                .Where(w => w.StartsWith(word, StringComparison.OrdinalIgnoreCase))
                .Select(w => new PlainCompletionData(w));
        }

        // Minimal completion item
        private sealed class PlainCompletionData : ICompletionData
        {
            private readonly string _insert;
            public PlainCompletionData(string text) { Text = text; _insert = text; }
            public System.Windows.Media.ImageSource? Image => null;
            public string Text { get; }
            public object Content => Text;
            public object Description => "word list";
            public double Priority => 0;

            public void Complete(TextArea area, ISegment completionSegment, EventArgs insertionRequestEventArgs)
            {
                area.Document.Replace(completionSegment, _insert);
            }
        }

        // Local current-word finder (letters/digits only)
        private static (ISegment? segment, string word) GetCurrentWord(TextEditor editor)
        {
            var doc = editor.Document;
            if (doc is null) return (null, string.Empty);

            int caret = editor.CaretOffset;
            int start = caret, end = caret;

            while (start > 0)
            {
                char ch = doc.GetCharAt(start - 1);
                if (char.IsLetterOrDigit(ch)) start--; else break;
            }
            while (end < doc.TextLength)
            {
                char ch = doc.GetCharAt(end);
                if (char.IsLetterOrDigit(ch)) end++; else break;
            }

            int len = end - start;
            if (len <= 0) return (null, string.Empty);

            var seg = new TextSegment { StartOffset = start, Length = len };
            string word = doc.GetText(start, len);
            return (seg, word);
        }
    }
}
