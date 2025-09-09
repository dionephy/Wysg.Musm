// src/Wysg.Musm.Editor/Snippets/MusmCompletionData.cs
using System;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;

namespace Wysg.Musm.Editor.Snippets;

public sealed class MusmCompletionData : ICompletionData
{
    // Display text shown in the list (e.g., "dba")
    public string Text { get; }

    // Tooltip text
    public object Description { get; }

    // Optional icon
    public ImageSource? Image { get; init; }

    // Kinds
    public bool IsSnippet { get; }
    public bool IsHotkey { get; }

    // Actual text to insert for non-snippets
    public string Replacement { get; }

    // Text shown by the completion-ghost preview
    public string Preview { get; }

    public object Content => Text;
    public double Priority => 0.0;

    // ----- Factories -----
    public static MusmCompletionData Token(string token, string? description = null) =>
        new(token, isHotkey: false, isSnippet: false,
            replacement: token, preview: token, description ?? token, snippet: null);

    public static MusmCompletionData Hotkey(string shortcut, string expanded, string? description = null) =>
        new(shortcut, isHotkey: true, isSnippet: false,
            replacement: expanded, preview: expanded, description ?? expanded, snippet: null);

    public static MusmCompletionData Snippet(CodeSnippet snippet) =>
        new(snippet.Shortcut, isHotkey: false, isSnippet: true,
            replacement: string.Empty, preview: snippet.PreviewText(),
            description: $"{snippet.Description}  —  {snippet.PreviewText()}",
            snippet: snippet);

    // ----- impl -----
    private readonly CodeSnippet? _snippet;

    private MusmCompletionData(
        string text,
        bool isHotkey,
        bool isSnippet,
        string replacement,
        string preview,
        string description,
        CodeSnippet? snippet)
    {
        Text = text;
        IsHotkey = isHotkey;
        IsSnippet = isSnippet;
        Replacement = replacement;
        Preview = preview;
        Description = description;
        _snippet = snippet;
    }

    // Single required method per ICompletionData
    public void Complete(TextArea area, ISegment completionSegment, EventArgs insertionRequestEventArgs)
    {
        if (IsSnippet)
        {
            // Expand snippet at the segment position
            var (expanded, map) = _snippet!.Expand();
            area.Document.Replace(completionSegment, string.Empty);
            area.Caret.Offset = completionSegment.Offset;
            SnippetInputHandler.Start(area, expanded, map);
            return;
        }

        // Token / Hotkey: insert Replacement (not Text)
        area.Document.Replace(completionSegment, Replacement);
        area.Caret.Offset = completionSegment.Offset + Replacement.Length;
    }
}
