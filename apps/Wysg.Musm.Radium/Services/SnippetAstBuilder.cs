using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Wysg.Musm.Radium.Services
{
    /// <summary>
    /// Builds a simple AST JSON for snippet_text based on placeholder syntax described in docs/snippet_logic.md.
    /// This AST is intended for fast runtime parsing in the editor.
    /// Structure:
    /// {
    ///   "version": 1,
    ///   "placeholders": [
    ///      { "mode": 0|1|2|3, "label": "fruit", "tabstop": 1, "options": [{"key":"a","text":"apple"}], "joiner":"or","bilateral":false }
    ///   ]
    /// }
    /// </summary>
    public static class SnippetAstBuilder
    {
        private static readonly Regex PlaceholderRegex = new Regex(@"\$\{([^}]*)\}", RegexOptions.Compiled);

        public static string BuildJson(string snippetText)
        {
            if (string.IsNullOrWhiteSpace(snippetText))
            {
                return JsonSerializer.Serialize(new SnippetAst { Version = 1, Placeholders = new() });
            }

            var result = new SnippetAst { Version = 1, Placeholders = new() };
            foreach (Match m in PlaceholderRegex.Matches(snippetText))
            {
                var raw = m.Groups[1].Value.Trim();
                if (string.IsNullOrEmpty(raw)) continue;

                var node = ParsePlaceholder(raw);
                if (node != null) result.Placeholders.Add(node);
            }

            var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = false });
            return json;
        }

        private static Placeholder? ParsePlaceholder(string raw)
        {
            int mode = 0; // 0=free,1=single,2=multi,3=replace
            int? tabstop = null;
            string rest = raw;

            if (raw.Length >= 2 && char.IsDigit(raw[0]) && raw[1] == '^')
            {
                mode = (int)char.GetNumericValue(raw[0]);
                tabstop = mode; // tabstop follows mode number in examples
                rest = raw.Substring(2);
            }

            if (mode == 0)
            {
                // Free text: ${friend}
                return new Placeholder { Mode = 0, Label = rest, Tabstop = null };
            }

            // Expect label (+ optional opts for mode 2) then '=' then choices
            var eqIdx = rest.IndexOf('=');
            if (eqIdx < 0)
            {
                // No '=' present; treat as free label fallback
                return new Placeholder { Mode = mode, Label = rest, Tabstop = tabstop };
            }

            var left = rest.Substring(0, eqIdx);
            var right = rest.Substring(eqIdx + 1);

            string label;
            var joiner = "and"; // default for mode 2
            bool bilateral = false;

            if (mode == 2)
            {
                // mode 2: label^opt1^opt2
                var parts = left.Split('^', StringSplitOptions.RemoveEmptyEntries);
                label = (parts.Length > 0) ? parts[0] : string.Empty;
                for (int i = 1; i < parts.Length; i++)
                {
                    var opt = parts[i].Trim().ToLowerInvariant();
                    if (opt == "or") joiner = "or";
                    if (opt == "bilateral") bilateral = true;
                }
            }
            else
            {
                // mode 1 or 3: label only on left
                label = left;
            }

            var options = ParseOptions(right);
            return new Placeholder
            {
                Mode = mode,
                Label = label,
                Tabstop = tabstop,
                Options = options,
                Joiner = joiner,
                Bilateral = bilateral
            };
        }

        private static List<Choice>? ParseOptions(string right)
        {
            // right like: a^apple|b^banana|3^pear
            var list = new List<Choice>();
            foreach (var token in right.Split('|', StringSplitOptions.RemoveEmptyEntries))
            {
                var kv = token.Split('^');
                if (kv.Length == 1)
                {
                    list.Add(new Choice { Key = kv[0], Text = kv[0] });
                }
                else
                {
                    list.Add(new Choice { Key = kv[0], Text = string.Join('^', kv.Skip(1)) });
                }
            }
            return list.Count > 0 ? list : null;
        }

        private sealed class SnippetAst
        {
            public int Version { get; set; }
            public List<Placeholder> Placeholders { get; set; } = new();
        }

        private sealed class Placeholder
        {
            public int Mode { get; set; }
            public string Label { get; set; } = string.Empty;
            public int? Tabstop { get; set; }
            public List<Choice>? Options { get; set; }
            public string? Joiner { get; set; }
            public bool? Bilateral { get; set; }
        }

        private sealed class Choice
        {
            public string Key { get; set; } = string.Empty;
            public string Text { get; set; } = string.Empty;
        }
    }
}
