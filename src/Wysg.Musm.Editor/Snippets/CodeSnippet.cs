// src/Wysg.Musm.Editor/Snippets/CodeSnippet.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ICSharpCode.AvalonEdit.Document;  // for ISegment
using ICSharpCode.AvalonEdit.Editing;   // for TextArea

namespace Wysg.Musm.Editor.Snippets;

/// Hotkey: text without placeholders
/// Snippet: supports ${index^label=1^optA|3^optB} style placeholders
public sealed class CodeSnippet
{
    public string Shortcut { get; }
    public string Description { get; }      // for UI list
    public string Template { get; }         // raw template with ${...}

    public CodeSnippet(string shortcut, string description, string template)
    {
        Shortcut = shortcut;
        Description = description;
        Template = template ?? string.Empty;
    }

    // Very small parser: find ${...} segments and extract options keyed by digits
    private static readonly Regex PlaceholderRx =
        new(@"\$\{(\d+)\^([^\}=]+)=(.*?)\}", RegexOptions.Compiled); // ${1^laterality=1^right|3^left}

    public bool IsSnippet => PlaceholderRx.IsMatch(Template);

    public IEnumerable<SnippetPlaceholder> EnumeratePlaceholders()
    {
        foreach (Match m in PlaceholderRx.Matches(Template))
        {
            var index = int.Parse(m.Groups[1].Value);
            var label = m.Groups[2].Value;
            var optionsRaw = m.Groups[3].Value; // "1^right|3^left"
            var options = new List<SnippetOption>();
            foreach (var token in optionsRaw.Split('|'))
            {
                var parts = token.Split('^');
                if (parts.Length >= 2 && int.TryParse(parts[0], out var digit))
                    options.Add(new SnippetOption(digit, parts[1]));
            }
            yield return new SnippetPlaceholder(index, label, m.Index, m.Length, options);
        }
    }

    /// Returns a display preview (for completion ghost) without mutating document.
    public string PreviewText()
    {
        if (!IsSnippet) return Template;
        // Replace ${...} with label (first option hint)
        return PlaceholderRx.Replace(Template, m =>
        {
            var label = m.Groups[2].Value;
            var options = m.Groups[3].Value.Split('|');
            var first = options.FirstOrDefault()?.Split('^').LastOrDefault() ?? "";
            return label + " → " + first;
        });
    }

    /// Expand into a text with placeholders removed but positions recorded.
    public (string expanded, List<ExpandedPlaceholder> map) Expand()
    {
        if (!IsSnippet) return (Template, new List<ExpandedPlaceholder>());
        var map = new List<ExpandedPlaceholder>();
        int cursor = 0;
        var text = Template;
        var result = new System.Text.StringBuilder();

        foreach (Match m in PlaceholderRx.Matches(text))
        {
            // Copy text before placeholder
            int plainLen = m.Index - cursor;
            if (plainLen > 0) result.Append(text.AsSpan(cursor, plainLen));

            var index = int.Parse(m.Groups[1].Value);
            var label = m.Groups[2].Value;
            var options = m.Groups[3].Value.Split('|')
                             .Select(t => t.Split('^')).Where(p => p.Length >= 2)
                             .Select(p => new SnippetOption(int.Parse(p[0]), p[1])).ToList();

            int start = result.Length;
            result.Append(label); // insert label initially as visible token
            int length = label.Length;

            map.Add(new ExpandedPlaceholder(index, label, start, length, options));
            cursor = m.Index + m.Length;
        }
        // Tail
        if (cursor < text.Length) result.Append(text.AsSpan(cursor));
        return (result.ToString(), map);
    }

    // ---- Legacy compatibility shim (for EditorCompletionData.cs etc.) ----

    /// Legacy callers expect the snippet/hotkey body on .Text
    public string Text => Template;

    /// Legacy callers use snippet.Insert(...) to perform insertion.
    /// - Hotkey/token: replaces the target segment with Template.
    /// - Snippet: expands placeholders and enters placeholder mode.
    public void Insert(TextArea area, ISegment replaceSegment)
    {
        if (!IsSnippet)
        {
            // Hotkey / token: just replace the word
            area.Document.Replace(replaceSegment, Template);
            area.Caret.Offset = replaceSegment.Offset + Template.Length;
            return;
        }

        // Snippet: expand and start placeholder workflow
        var (expanded, map) = Expand();

        // Replace the token so the snippet lands at the same spot
        area.Document.Replace(replaceSegment, string.Empty);
        area.Caret.Offset = replaceSegment.Offset;

        SnippetInputHandler.Start(area, expanded, map);
    }

    /// Some legacy sites pass an EventArgs; accept and forward.
    public void Insert(TextArea area, ISegment replaceSegment, EventArgs _)
        => Insert(area, replaceSegment);
}

public sealed record SnippetOption(int Digit, string Text);
public sealed record SnippetPlaceholder(int Index, string Label, int TemplateOffset, int TemplateLength, IReadOnlyList<SnippetOption> Options);

public sealed class ExpandedPlaceholder
{
    public int Index { get; init; }
    public string Label { get; init; }
    public int Start { get; set; }    // mutable: shifting after replacements
    public int Length { get; set; }   // mutable: becomes chosen option length
    public IReadOnlyList<SnippetOption> Options { get; init; }

    public ExpandedPlaceholder(int index, string label, int start, int length, IReadOnlyList<SnippetOption> options)
    {
        Index = index;
        Label = label;
        Start = start;
        Length = length;
        Options = options;
    }
}
