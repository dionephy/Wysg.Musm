using System.Collections.Generic;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.CodeCompletion;
using Wysg.Musm.Editor.Snippets;

namespace Wysg.Musm.Editor.Playground.Completion;

public sealed class CompositeSnippetProvider : ISnippetProvider
{
    private readonly ISnippetProvider[] _providers;

    public CompositeSnippetProvider(params ISnippetProvider[] providers)
        => _providers = providers ?? [];

    public IEnumerable<ICompletionData> GetCompletions(TextEditor editor)
    {
        foreach (var p in _providers)
            foreach (var d in p.GetCompletions(editor))
                yield return d;
    }
}
