using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Input;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using Wysg.Musm.Editor.Snippets;

namespace Wysg.Musm.Editor.Completion;

/// <summary>
/// Themed completion item that can execute any insertion action (snippet/text/etc.).
/// Images are pulled from Themes/Generic.xaml by resource key.
/// </summary>
public sealed class EditorCompletionData : ICompletionData
{
    private readonly string _text;                    // filter text (what user typed against)
    private readonly object _content;                 // shown in the list
    private readonly object _description;             // tooltip / detail
    private readonly string? _iconResourceKey;
    private readonly Action<TextArea, ISegment> _onComplete;
    private ImageSource? _cachedImage;

    private EditorCompletionData(
        string text,
        object content,
        object description,
        string? iconResourceKey,
        Action<TextArea, ISegment> onComplete,
        double priority = 0)
    {
        _text = text;
        _content = content;
        _description = description;
        _iconResourceKey = iconResourceKey;
        _onComplete = onComplete;
        Priority = priority;
    }

    /// <summary>Create a completion item that expands a CodeSnippet.</summary>
    public static EditorCompletionData ForSnippet(CodeSnippet snippet, string? iconKey = "CodeSnippetIcon", double priority = 0)
        => new(
            text: snippet.Shortcut,
            content: $"{snippet.Shortcut} → {snippet.Description}",
            description: snippet.Text, // keep full template in tooltip
            iconResourceKey: iconKey,
            onComplete: (textArea, completionSegment) => snippet.Insert(textArea, completionSegment),
            priority: priority);

    /// <summary>Create a completion item that inserts a literal string (quick canned text).</summary>
    public static EditorCompletionData ForLiteral(string display, string toInsert, string? iconKey = null, double priority = 0)
        => new(
            text: display,
            content: display,
            description: toInsert,
            iconResourceKey: iconKey,
            onComplete: (textArea, completionSegment) =>
            {
                // Replace the completion segment with the literal.
                var doc = textArea.Document;
                doc.Replace(completionSegment, toInsert);
                // Move caret after inserted text:
                textArea.Caret.Offset = completionSegment.Offset + toInsert.Length;
            },
            priority: priority);

    // ICompletionData
    public ImageSource? Image
    {
        get
        {
            if (_cachedImage != null || _iconResourceKey == null) return _cachedImage;
            // Try to resolve from Generic.xaml (or app resources)
            if (Application.Current?.TryFindResource(_iconResourceKey) is ImageSource img)
                _cachedImage = img;
            return _cachedImage;
        }
    }

    /// <summary>Used for filtering in the completion list.</summary>
    public string Text => _text;

    /// <summary>Shown in the popup list (string or a small visual).</summary>
    public object Content => _content;

    /// <summary>Tooltip when item is selected.</summary>
    public object Description => _description;

    /// <summary>Optional sort/priority; higher shows earlier (AvalonEdit treats greater as earlier).</summary>
    public double Priority { get; }

    public override string ToString() => _content?.ToString() ?? base.ToString();

    public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
    {
        _onComplete(textArea, completionSegment);
        // If the insertion was triggered by Space key, append a trailing space for smoother typing
        if (insertionRequestEventArgs is KeyEventArgs ke && ke.Key == Key.Space)
        {
            var doc = textArea.Document;
            int off = textArea.Caret.Offset;
            try { doc.Insert(off, " "); textArea.Caret.Offset = off + 1; } catch { }
        }
    }
}
