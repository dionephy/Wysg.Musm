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
    public int Mode { get; init; } // 0,1,2,3 based on snippet header
    public string Label { get; init; }
    public int Start { get; set; }
    public int Length { get; set; }
    public IReadOnlyList<SnippetOption> Options { get; init; }
    public PlaceholderKind Kind { get; init; }
    public string? Joiner { get; init; }  // for MultiChoice (e.g., "or" or "and")
    public bool Bilateral { get; init; }  // for mode 2 option processing

    public ExpandedPlaceholder(int index, int mode, string label, int start, int length,
                               IReadOnlyList<SnippetOption> options,
                               PlaceholderKind kind,
                               string? joiner = null,
                               bool bilateral = false)
    {
        Index = index;
        Mode = mode;
        Label = label;
        Start = start;
        Length = length;
        Options = options;
        Kind = kind;
        Joiner = joiner;
        Bilateral = bilateral;
    }
}

/// Hotkey: plain text without placeholders
/// Snippet: supports:
///   - ${label}                              -> FreeText
///   - ${date} / ${number}                   -> macros
///   - ${1^label=a^A|b^B}                    -> Mode 1 SingleChoice (immediate)
///   - ${2^label^or^bilateral=a^A|b^B|...}  -> Mode 2 MultiChoice with options (joiner=or/and, bilateral)
///   - ${3^label=aa^A|bb^B}                  -> Mode 3 SingleChoice (multi-char keys, accept on Tab)
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
    // 1) Header: ${<idx>^<left>=<opts>} where <left> is label[^opt...]
    // 2) Free/Macros: ${<free>}
    private static readonly Regex PlaceholderRx = new(
        @"\$\{(?:(?<idx>\d+)\^(?<left>[^}=]+)=(?<opts>[^}]*)|(?<free>[^}]+))\}",
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
                var left = m.Groups["left"].Value;
                var (mode, label, joiner, bilateral) = ParseHeader(left);
                var opts = ParseOptions(m.Groups["opts"].Value);
                var first = opts.FirstOrDefault()?.Text ?? "";
                return mode == 2
                    ? $"{label} → {first}"
                    : $"{label} → {first}";
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
                int idx = int.Parse(m.Groups["idx"].Value);
                var left = m.Groups["left"].Value;
                var (mode, label, joiner, bilateral) = ParseHeader(left);
                var options = ParseOptions(m.Groups["opts"].Value);

                int start = sb.Length;
                // show label as initial visible token
                sb.Append(label);
                int length = label.Length;

                var kind = mode == 2 ? PlaceholderKind.MultiChoice : PlaceholderKind.SingleChoice;

                map.Add(new ExpandedPlaceholder(index: idx, mode: mode, label: label,
                    start: start, length: length, options: options, kind: kind,
                    joiner: joiner, bilateral: bilateral));
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
                    initial = label; // hint text
                    kind = PlaceholderKind.FreeText;
                }

                sb.Append(initial);
                int length = initial.Length;

                map.Add(new ExpandedPlaceholder(index: autoIndex++, mode: 0, label: label,
                    start: start, length: length, options: Array.Empty<SnippetOption>(),
                    kind: kind, joiner: null, bilateral: false));
            }

            cursor = m.Index + m.Length;
        }
        // tail
        if (cursor < Template.Length) sb.Append(Template.AsSpan(cursor));

        return (sb.ToString(), map);
    }

    private static (int mode, string label, string? joiner, bool bilateral) ParseHeader(string left)
    {
        // left is "label" or "label^opt1^opt2"; mode determined by caller (idx)
        // For mode 2, options can include "or" or "and" and "bilateral".
        var parts = left.Split('^', StringSplitOptions.RemoveEmptyEntries);
        string label = (parts.Length > 0) ? parts[0] : string.Empty;
        string? joiner = null;
        bool bilateral = false;
        if (parts.Length > 1)
        {
            for (int i = 1; i < parts.Length; i++)
            {
                var opt = parts[i].Trim().ToLowerInvariant();
                if (opt == "or" || opt == "and") joiner = opt;
                if (opt == "bilateral") bilateral = true;
            }
        }
        // The actual mode is set by idx in Expand; here we return a placeholder 0 for mode (unused)
        return (0, label, joiner, bilateral);
    }

    private static List<SnippetOption> ParseOptions(string raw)
    {
        var list = new List<SnippetOption>();
        if (string.IsNullOrWhiteSpace(raw)) return list;

        foreach (var tok in raw.Split('|'))
        {
            var idx = tok.IndexOf('^');
            if (idx <= 0 || idx >= tok.Length - 1) continue;
            string key = tok.Substring(0, idx).Trim();    // can be letter/digit or multi-char (e.g., "aa")
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
