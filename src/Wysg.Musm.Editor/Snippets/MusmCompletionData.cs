// src/Wysg.Musm.Editor/Snippets/MusmCompletionData.cs
using System;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;

namespace Wysg.Musm.Editor.Snippets;

public sealed class MusmCompletionData : ICompletionData
{
    /// <summary>
    /// Text property used for filtering by AvalonEdit's CompletionList.
    /// IMPORTANT: This should contain ONLY the trigger/shortcut text, NOT the description.
    /// For hotkeys: just the trigger text (e.g., "noaa")
    /// For snippets: just the shortcut (e.g., "ngi")
    /// For phrases: just the phrase text
    /// </summary>
    public string Text { get; }

    // Tooltip text (null to suppress)
    public object Description { get; }

    public ImageSource? Image { get; init; }

    public bool IsSnippet { get; }
    public bool IsHotkey { get; }

    public string Replacement { get; }

    public string Preview { get; }

    private readonly string _content; // what shows in the popup list (can include description)
    public object Content => _content;
    public double Priority { get; private set; }

    // ----- Factories -----
    public static MusmCompletionData Token(string token, string? description = null) =>
        new(token, isHotkey: false, isSnippet: false,
            replacement: token, preview: token, desc: null, snippet: null,
            content: token, priority: 0.0); // Lowest priority - phrases go last

    public static MusmCompletionData Hotkey(string trigger, string expanded, string? description = null)
    {
        // Display shows trigger text only (no description in list)
        // Text (for filtering) is just the trigger
        var content = trigger; // Keep simple: just show trigger text
        return new(
            text: trigger, // Filter by trigger text only
            isHotkey: true,
            isSnippet: false,
            replacement: expanded,
            preview: expanded,
            desc: description, // Show in tooltip if available
            snippet: null,
            content: content,
            priority: 2.0); // Higher priority - hotkeys go second
    }

    public static MusmCompletionData Snippet(CodeSnippet snippet)
    {
        // Display shows "trigger → description" for clarity
        // But Text (for filtering) is just the shortcut/trigger
        var content = $"{snippet.Shortcut} → {snippet.Description}";
        return new(
            text: snippet.Shortcut, // ✅ FIX: Filter by shortcut only, not full content string
            isHotkey: false, isSnippet: true,
            replacement: string.Empty, preview: snippet.PreviewText(),
            desc: null, snippet: snippet,
            content: content, // Display shows full "trigger → description"
            priority: 3.0); // Highest priority - snippets go first
    }

    /// <summary>
    /// Adjust priority based on match quality with current input.
    /// Called by completion window to prioritize shorter/exact matches.
    /// </summary>
    public void AdjustPriorityForInput(string input)
    {
        if (string.IsNullOrEmpty(input))
            return;

        // Get the key text to match against (display for hotkeys, trigger for snippets)
        string matchKey = IsHotkey ? Text : (IsSnippet ? _snippet?.Shortcut ?? "" : Text);

        if (string.IsNullOrEmpty(matchKey))
            return;

        // Calculate match quality bonus (within same category)
        double bonus = 0;

        // Exact match gets highest bonus (0.9)
        if (matchKey.Equals(input, StringComparison.OrdinalIgnoreCase))
        {
            bonus = 0.9;
        }
        // Shorter items get priority over longer items
        // For "ngi" input: "ngi" (length 3) should rank higher than "ngio" (length 4)
        else
        {
            // Inverse length bonus: shorter items get higher bonus
            // Max bonus 0.5 for very short items, decreasing as length increases
            bonus = Math.Max(0, 0.5 - (matchKey.Length * 0.02));
        }

        Priority += bonus;
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
