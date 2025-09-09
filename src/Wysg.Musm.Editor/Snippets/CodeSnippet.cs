// src/Wysg.Musm.Editor/Snippets/CodeSnippet.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ICSharpCode.AvalonEdit.Document;  // ISegment
using ICSharpCode.AvalonEdit.Editing;   // TextArea

namespace Wysg.Musm.Editor.Snippets;

public enum PlaceholderKind
{
    FreeText,
    SingleChoice,
    MultiChoice,
    MacroNumber,
    MacroDate
}

public sealed record SnippetOption(string Key, string Text);

public sealed class ExpandedPlaceholder
{
    public int Index { get; init; }
    public string Label { get; init; }
    public int Start { get; set; }
    public int Length { get; set; }
    public IReadOnlyList<SnippetOption> Options { get; init; }
    public PlaceholderKind Kind { get; init; }
    public string? Joiner { get; init; }  // for MultiChoice (e.g., "or")

    public ExpandedPlaceholder(int index, string label, int start, int length,
                               IReadOnlyList<SnippetOption> options,
                               PlaceholderKind kind,
                               string? joiner = null)
    {
        Index = index;
        Label = label;
        Start = start;
        Length = length;
        Options = options;
        Kind = kind;
        Joiner = joiner;
    }
}

/// Hotkey: plain text without placeholders
/// Snippet: supports:
///   - ${label}              -> FreeText
///   - ${date} / ${number}   -> macros
///   - ${1^label=a^A|b^B}    -> SingleChoice (letter/number keys)
///   - ${2^label^or=a^A|b^B} -> MultiChoice with joiner "or"
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

    // Combined placeholder regex:
    // 1) Choice: ${<idx>^<label>(^<joiner>)?=<opts>}
    //    where <opts> is key^text | key^text | ...
    // 2) Free/Macros: ${<free>}
    private static readonly Regex PlaceholderRx = new(
        @"\$\{(?:(?<idx>\d+)\^(?<label>[^}=]+?)(?:\^(?<join>[^}=]+))?=(?<opts>[^}]*)|(?<free>[^}]+))\}",
        RegexOptions.Compiled);

    public bool IsSnippet => PlaceholderRx.IsMatch(Template);

    /// Display preview for the completion ghost
    public string PreviewText()
    {
        if (!IsSnippet) return Template;
        return PlaceholderRx.Replace(Template, m =>
        {
            if (m.Groups["idx"].Success)
            {
                // choice: show label → first option
                var label = m.Groups["label"].Value;
                var opts = ParseOptions(m.Groups["opts"].Value);
                var first = opts.FirstOrDefault()?.Text ?? "";
                return $"{label} → {first}";
            }
            // free / macro
            var free = m.Groups["free"].Value.Trim();
            if (free.Equals("date", StringComparison.OrdinalIgnoreCase))
                return DateTime.Today.ToString("yyyy-MM-dd");
            if (free.Equals("number", StringComparison.OrdinalIgnoreCase))
                return "0";
            return free;
        });
    }

    /// Expand to text + placeholder map
    public (string expanded, List<ExpandedPlaceholder> map) Expand()
    {
        if (!IsSnippet) return (Template, new List<ExpandedPlaceholder>());

        var map = new List<ExpandedPlaceholder>();
        var sb = new System.Text.StringBuilder();
        int cursor = 0;

        int autoIndex = 1000; // for free/macro when no explicit index is provided

        foreach (Match m in PlaceholderRx.Matches(Template))
        {
            // append plain text between placeholders
            int plainLen = m.Index - cursor;
            if (plainLen > 0) sb.Append(Template.AsSpan(cursor, plainLen));

            if (m.Groups["idx"].Success)
            {
                // Choice (single or multi)
                int idx = int.Parse(m.Groups["idx"].Value);
                string label = m.Groups["label"].Value;
                string? joiner = m.Groups["join"].Success ? m.Groups["join"].Value : null;
                var options = ParseOptions(m.Groups["opts"].Value);

                int start = sb.Length;
                // Insert the label as the initial visible token
                sb.Append(label);
                int length = label.Length;

                var kind = string.IsNullOrEmpty(joiner) ? PlaceholderKind.SingleChoice
                                                        : PlaceholderKind.MultiChoice;
                map.Add(new ExpandedPlaceholder(idx, label, start, length, options, kind, joiner));
            }
            else
            {
                // Free / Macro
                string label = m.Groups["free"].Value.Trim();
                int start = sb.Length;
                string initial;
                var kind = PlaceholderKind.FreeText;

                if (label.Equals("date", StringComparison.OrdinalIgnoreCase))
                {
                    initial = DateTime.Today.ToString("yyyy-MM-dd");
                    kind = PlaceholderKind.MacroDate;
                }
                else if (label.Equals("number", StringComparison.OrdinalIgnoreCase))
                {
                    initial = "0";
                    kind = PlaceholderKind.MacroNumber;
                }
                else
                {
                    initial = label; // show label as hint; user will type over it
                    kind = PlaceholderKind.FreeText;
                }

                sb.Append(initial);
                int length = initial.Length;

                map.Add(new ExpandedPlaceholder(autoIndex++, label, start, length,
                                                Array.Empty<SnippetOption>(), kind, null));
            }

            cursor = m.Index + m.Length;
        }
        // tail
        if (cursor < Template.Length) sb.Append(Template.AsSpan(cursor));

        return (sb.ToString(), map);
    }

    private static List<SnippetOption> ParseOptions(string raw)
    {
        var list = new List<SnippetOption>();
        if (string.IsNullOrWhiteSpace(raw)) return list;

        foreach (var tok in raw.Split('|'))
        {
            var idx = tok.IndexOf('^');
            if (idx <= 0 || idx >= tok.Length - 1) continue;
            string key = tok.Substring(0, idx).Trim();    // can be letter or digit (e.g., "a", "3")
            string text = tok.Substring(idx + 1).Trim();
            if (key.Length > 0 && text.Length > 0)
                list.Add(new SnippetOption(key, text));
        }
        return list;
    }

    // ---- Legacy compatibility shim (keeps existing callers compiling) ----

    public string Text => Template;

    public void Insert(TextArea area, ISegment replaceSegment)
    {
        if (!IsSnippet)
        {
            area.Document.Replace(replaceSegment, Template);
            area.Caret.Offset = replaceSegment.Offset + Template.Length;
            return;
        }

        var (expanded, map) = Expand();
        area.Document.Replace(replaceSegment, string.Empty);
        area.Caret.Offset = replaceSegment.Offset;
        SnippetInputHandler.Start(area, expanded, map);
    }

    public void Insert(TextArea area, ISegment replaceSegment, EventArgs _)
        => Insert(area, replaceSegment);
}
