using System;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using Wysg.Musm.Editor.Completion;

namespace Wysg.Musm.Editor.Snippets;

/// <summary>
/// Simple in-memory snippet source. Start here, swap to JSON/DB later.
/// </summary>
public sealed class InMemorySnippetProvider : ISnippetProvider
{
    private readonly List<CodeSnippet> _snippets;

    public InMemorySnippetProvider()
    {
        _snippets = new List<CodeSnippet>
        {
            new("nq", "Normal brain MRI",
                "Impression: ${0^No acute intracranial hemorrhage, territorial infarct, or mass effect.}"),

            new("cta-n", "CTA head/neck (normal)",
                "CTA head/neck: ${1^Result=a^No flow-limiting stenosis or aneurysm identified.}"),

            new("dwi-p", "DWI positive (acute infarct)",
                "Restricted diffusion in the ${2^Site=li^left insula|ri^right insula|th^thalami} compatible with acute infarction."),

            new("impr-n", "Impression (no acute hemorrhage)",
                "Impression: ${3^No acute intracranial hemorrhage identified.}")
        };
    }

    public InMemorySnippetProvider(IEnumerable<CodeSnippet> snippets)
        => _snippets = snippets?.ToList() ?? new();

    public IEnumerable<ICompletionData> GetCompletions(TextEditor editor)
    {
        if (editor is null) yield break;

        var (prefix, wordStart) = GetWordBeforeCaret(editor);

        foreach (var sn in _snippets
                 .Where(s => s.Shortcut.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                 .OrderBy(s => s.Shortcut.Length)
                 .ThenBy(s => s.Shortcut))
        {
            // Let AvalonEdit compute the completionSegment; snippet.Insert will use it.
            yield return EditorCompletionData.ForSnippet(sn, iconKey: "CodeSnippetIcon");
        }
    }

    private static (string word, int startOffset) GetWordBeforeCaret(TextEditor editor)
    {
        int caret = editor.CaretOffset;
        if (caret <= 0) return (string.Empty, caret);

        var line = editor.Document.GetLineByOffset(caret);
        var text = editor.Document.GetText(line.Offset, line.Length);
        int local = Math.Min(text.Length, caret - line.Offset) - 1;

        while (local >= 0 && char.IsLetterOrDigit(text[local])) local--;
        int start = line.Offset + local + 1;

        return (editor.Document.GetText(start, caret - start), start);
    }
}
