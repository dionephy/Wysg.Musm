// src/Wysg.Musm.Editor/Snippets/CompositeSnippetProvider.cs
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.AvalonEdit.CodeCompletion;

namespace Wysg.Musm.Editor.Snippets;


public sealed class CompositeSnippetProvider : ISnippetProvider
{
    private readonly List<string> _tokens = new();
    private readonly List<(string hotkey, string detail)> _hotkeys = new();
    private readonly List<CodeSnippet> _snippets = new();

    public CompositeSnippetProvider AddTokens(params string[] tokens) { _tokens.AddRange(tokens); return this; }
    public CompositeSnippetProvider AddHotkey(string hotkey, string detail) { _hotkeys.Add((hotkey, detail)); return this; }
    public CompositeSnippetProvider AddSnippet(CodeSnippet cs) { _snippets.Add(cs); return this; }

    public IEnumerable<ICompletionData> GetCompletions(ICSharpCode.AvalonEdit.TextEditor editor)
    {
        foreach (var t in _tokens) yield return new MusmCompletionData(t);
        foreach (var h in _hotkeys) yield return new MusmCompletionData(h.hotkey, h.detail);
        foreach (var s in _snippets) yield return new MusmCompletionData(s);
    }
}
