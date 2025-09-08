// src/Wysg.Musm.Editor/Snippets/MusmCompletionData.cs
using System;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;

namespace Wysg.Musm.Editor.Snippets;

public sealed class MusmCompletionData : ICompletionData
{
    public string Text { get; }
    public object Content => Text;
    public object Description { get; }
    public ImageSource? Image { get; init; }

    public bool IsSnippet { get; }
    private readonly CodeSnippet? _snippet; // when IsSnippet == true

    // Token/hotkey
    public MusmCompletionData(string tokenOrHotkey, string? detail = null)
    {
        Text = tokenOrHotkey;
        Description = detail ?? tokenOrHotkey;
        IsSnippet = false;
    }

    // Snippet
    public MusmCompletionData(CodeSnippet snippet)
    {
        _snippet = snippet;
        IsSnippet = true;
        Text = snippet.Shortcut;
        Description = snippet.Description + "  —  " + snippet.PreviewText();
    }

    public double Priority => 0.0;

    public void Complete(TextArea area, ISegment completionSegment, EventArgs insertionRequestEventArgs)
    {
        if (!IsSnippet)
        {
            // token/hotkey: replace the word
            area.Document.Replace(completionSegment, Text);
            area.Caret.Offset = completionSegment.Offset + Text.Length;
            return;
        }

        // Snippet: expand template and enter placeholder mode
        var (expanded, map) = _snippet!.Expand();

        // replace the word first (so snippet sits where the token is)
        area.Document.Replace(completionSegment, string.Empty);
        area.Caret.Offset = completionSegment.Offset;

        SnippetInputHandler.Start(area, expanded, map);
    }
}
