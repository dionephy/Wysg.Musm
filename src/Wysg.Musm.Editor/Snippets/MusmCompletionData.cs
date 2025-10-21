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
    public double Priority { get; }

    // ----- Factories -----
    public static MusmCompletionData Token(string token, string? description = null) =>
        new(token, isHotkey: false, isSnippet: false,
            replacement: token, preview: token, desc: null, snippet: null,
            content: token, priority: 0.0); // Lowest priority - phrases go last

    public static MusmCompletionData Hotkey(string display, string expanded, string? description = null) =>
        new(display, isHotkey: true, isSnippet: false,
            replacement: expanded, preview: expanded, desc: null, snippet: null,
            content: display, priority: 2.0); // Higher priority - hotkeys go second

    public static MusmCompletionData Snippet(CodeSnippet snippet)
    {
        var content = $"{snippet.Shortcut} → {snippet.Description}";
        return new(
            text: content, // filtering matches on trigger prefix since content starts with trigger
            isHotkey: false, isSnippet: true,
            replacement: string.Empty, preview: snippet.PreviewText(),
            desc: null, snippet: snippet,
            content: content, priority: 3.0); // Highest priority - snippets go first
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
        string content,
        double priority = 0.0)
    {
        Text = text;
        IsHotkey = isHotkey;
        IsSnippet = isSnippet;
        Replacement = replacement;
        Preview = preview;
        Description = desc!; // may be null → completion window shows no tooltip
        _snippet = snippet;
        _content = content;
        Priority = priority;
    }

    public override string ToString() => _content; // ensure ToString shows "{trigger} → {description}"

    public void Complete(TextArea area, ISegment completionSegment, EventArgs insertionRequestEventArgs)
    {
        var doc = area.Document;
        // Guard against stale completionSegment when document changed during popup
        int baseOffset = completionSegment.Offset;
        if (baseOffset < 0) baseOffset = 0;
        if (baseOffset > doc.TextLength) baseOffset = doc.TextLength;
        int replaceLength = completionSegment.Length;
        if (replaceLength < 0) replaceLength = 0;
        if (baseOffset + replaceLength > doc.TextLength) replaceLength = Math.Max(0, doc.TextLength - baseOffset);

        if (IsSnippet)
        {
            var (expanded, map) = _snippet!.Expand();
            doc.Replace(baseOffset, replaceLength, string.Empty);
            area.Caret.Offset = Math.Min(baseOffset, doc.TextLength);
            SnippetInputHandler.Start(area, expanded, map);
            return;
        }

        // Insert replacement
        doc.Replace(baseOffset, replaceLength, Replacement);
        int newCaret = baseOffset + Replacement.Length;
        // If triggered by Space, append trailing space after the inserted token
        if (insertionRequestEventArgs is System.Windows.Input.KeyEventArgs ke && ke.Key == System.Windows.Input.Key.Space)
        {
            try { doc.Insert(newCaret, " "); newCaret += 1; } catch { }
        }
        if (newCaret > doc.TextLength) newCaret = doc.TextLength;
        area.Caret.Offset = newCaret;
    }
}
