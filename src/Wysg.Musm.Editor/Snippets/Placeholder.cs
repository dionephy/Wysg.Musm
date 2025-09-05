using System.Collections.Generic;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Snippets;

namespace Wysg.Musm.Editor.Snippets;

/// <summary>
/// Runtime info for one ${...} placeholder inside a snippet.
/// Segment/Element map to the inserted text in the editor during an active snippet session.
/// </summary>
public sealed class Placeholder
{
    /// <summary>How the placeholder should behave.</summary>
    public PlaceholderType Type { get; set; } = PlaceholderType.FreeText;

    /// <summary>Human-readable initial text/title that appears in the editor before user edits.</summary>
    public string InitialDescription { get; set; } = string.Empty;

    /// <summary>Key->Value options parsed from the placeholder definition (for choice/select types).</summary>
    public IDictionary<string, string> Options { get; init; } = new Dictionary<string, string>();

    /// <summary>Optional extra metadata captured from the placeholder header (after the title).</summary>
    public string? Metadata { get; set; }

    /// <summary>Text segment of the current placeholder inside the editor document.</summary>
    public TextSegment Segment { get; set; } = new TextSegment();

    /// <summary>The snippet element that is replaceable for this placeholder.</summary>
    public SnippetReplaceableTextElement? Element { get; set; }

    /// <summary>Index of the element within the created Snippet's Elements list (handy for navigation).</summary>
    public int SnippetTextElementIndex { get; set; }
}
