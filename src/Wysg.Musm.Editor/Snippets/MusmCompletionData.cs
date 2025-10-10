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

    // Tooltip text (null to suppress)
    public object Description { get; }

    public ImageSource? Image { get; init; }

    public bool IsSnippet { get; }
    public bool IsHotkey { get; }

    public string Replacement { get; }

    public string Preview { get; }

    private readonly string _content; // what shows in the popup list
    public object Content => _content;
    public double Priority => 0.0;

    // ----- Factories -----
    public static MusmCompletionData Token(string token, string? description = null) =>
        new(token, isHotkey: false, isSnippet: false,
            replacement: token, preview: token, desc: null, snippet: null,
            content: token);

    public static MusmCompletionData Hotkey(string display, string expanded, string? description = null) =>
        new(display, isHotkey: true, isSnippet: false,
            replacement: expanded, preview: expanded, desc: null, snippet: null,
            content: display);

    public static MusmCompletionData Snippet(CodeSnippet snippet)
    {
        var content = $"{snippet.Shortcut} → {snippet.Description}";
        return new(
            text: content, // filtering matches on trigger prefix since content starts with trigger
            isHotkey: false, isSnippet: true,
            replacement: string.Empty, preview: snippet.PreviewText(),
            desc: null, snippet: snippet,
            content: content);
    }

    // ----- impl -----
    private readonly CodeSnippet? _snippet;

    private MusmCompletionData(
        string text,
        bool isHotkey,
        bool isSnippet,
        string replacement,
        string preview,
        object? desc,
        CodeSnippet? snippet,
        string content)
    {
        Text = text;
        IsHotkey = isHotkey;
        IsSnippet = isSnippet;
        Replacement = replacement;
        Preview = preview;
        Description = desc!; // may be null → completion window shows no tooltip
        _snippet = snippet;
        _content = content;
    }

    public override string ToString() => _content; // ensure ToString shows "{trigger} → {description}"

    public void Complete(TextArea area, ISegment completionSegment, EventArgs insertionRequestEventArgs)
    {
        if (IsSnippet)
        {
            var (expanded, map) = _snippet!.Expand();
            area.Document.Replace(completionSegment, string.Empty);
            area.Caret.Offset = completionSegment.Offset;
            SnippetInputHandler.Start(area, expanded, map);
            return;
        }

        area.Document.Replace(completionSegment, Replacement);
        area.Caret.Offset = completionSegment.Offset + Replacement.Length;
    }
}
