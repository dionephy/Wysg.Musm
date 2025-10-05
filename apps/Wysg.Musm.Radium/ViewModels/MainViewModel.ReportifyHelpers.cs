using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Wysg.Musm.Radium.ViewModels
{
    public partial class MainViewModel
    {
        private sealed class ReportifyConfig
        {
            public bool RemoveExcessiveBlanks { get; init; } = true;
            public bool RemoveExcessiveBlankLines { get; init; } = true;
            public bool CapitalizeSentence { get; init; } = true;
            public bool EnsureTrailingPeriod { get; init; } = true;
            public bool NormalizeArrows { get; init; } = true;
            public bool NormalizeBullets { get; init; } = true;
            public bool SpaceAfterPunctuation { get; init; } = true;
            public bool NormalizeParentheses { get; init; } = true;
            public bool SpaceNumberUnit { get; init; } = true;
            public bool CollapseWhitespace { get; init; } = true;
            public bool NumberConclusionParagraphs { get; init; } = true;
            public bool IndentContinuationLines { get; init; } = true;
            public bool PreserveKnownTokens { get; init; } = true;
            public string Arrow { get; init; } = "-->";
            public string ConclusionNumbering { get; init; } = "1."; // seed only; we auto increment
            public string DetailingPrefix { get; init; } = "-";
        }

        private ReportifyConfig? _reportifyConfig;
        private string? _reportifyConfigJsonApplied;
        private HashSet<string>? _keepCapsFirstTokens; private bool _capsLoaded; // added back (used by dereportify + caps preservation)
        private void EnsureReportifyConfig()
        {
            var json = _tenant.ReportifySettingsJson;
            if (json == null || json == _reportifyConfigJsonApplied) return;
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                var root = doc.RootElement;
                bool B(string name, bool def) => root.TryGetProperty(name, out var el) && el.ValueKind == System.Text.Json.JsonValueKind.True ? true : (el.ValueKind == System.Text.Json.JsonValueKind.False ? false : def);
                string Def(string prop, string def)
                {
                    if (root.TryGetProperty("defaults", out var d) && d.TryGetProperty(prop, out var el) && el.ValueKind == System.Text.Json.JsonValueKind.String) return el.GetString() ?? def;
                    return def;
                }
                _reportifyConfig = new ReportifyConfig
                {
                    RemoveExcessiveBlanks = B("remove_excessive_blanks", true),
                    RemoveExcessiveBlankLines = B("remove_excessive_blank_lines", true),
                    CapitalizeSentence = B("capitalize_sentence", true),
                    EnsureTrailingPeriod = B("ensure_trailing_period", true),
                    NormalizeArrows = B("normalize_arrows", true),
                    NormalizeBullets = B("normalize_bullets", true),
                    SpaceAfterPunctuation = B("space_after_punctuation", true),
                    NormalizeParentheses = B("normalize_parentheses", true),
                    SpaceNumberUnit = B("space_number_unit", true),
                    CollapseWhitespace = B("collapse_whitespace", true),
                    NumberConclusionParagraphs = B("number_conclusion_paragraphs", true),
                    IndentContinuationLines = B("indent_continuation_lines", true),
                    PreserveKnownTokens = B("preserve_known_tokens", true),
                    Arrow = Def("arrow", "-->"),
                    ConclusionNumbering = Def("conclusion_numbering", "1."),
                    DetailingPrefix = Def("detailing_prefix", "-")
                };
                _reportifyConfigJsonApplied = json;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[ReportifyConfig] parse error: " + ex.Message);
                _reportifyConfig = new ReportifyConfig();
                _reportifyConfigJsonApplied = json; // prevent repeated attempts
            }
        }

        private static readonly Regex RxMultiSpaces = new(@" {2,}", RegexOptions.Compiled);
        private static readonly Regex RxBlankLines = new(@"(\r?\n){3,}", RegexOptions.Compiled);
        private static readonly Regex RxArrowAny = new(@"^(--?>|=>)\s*", RegexOptions.Compiled);
        private static readonly Regex RxBullet = new(@"^([*-])\s*", RegexOptions.Compiled);
        private static readonly Regex RxAfterPunct = new(@"([;,:])(?!\s)" , RegexOptions.Compiled);
        private static readonly Regex RxParenTrimL = new(@"\(\s+", RegexOptions.Compiled);
        private static readonly Regex RxParenTrimR = new(@"\s+\)", RegexOptions.Compiled);
        private static readonly Regex RxNumberUnit = new(@"(?<![A-Za-z0-9])(\d+(?:\.\d+)?)(cm|mm|ml)(?![A-Za-z])", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex RxCollapseWs = new(@"\s+", RegexOptions.Compiled);
        private static readonly Regex RxNumbered = new(@"^\d+\.\s", RegexOptions.Compiled);

        private string ApplyReportifyBlock(string input, bool isConclusion)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            EnsureReportifyConfig();
            var cfg = _reportifyConfig ?? new ReportifyConfig();

            // Paragraph numbering for conclusion (before line-level ops) works on blank-line separated paragraphs
            if (isConclusion && cfg.NumberConclusionParagraphs)
            {
                var paras = input.Replace("\r\n", "\n").Split("\n\n", StringSplitOptions.None);
                int num = 1;
                for (int i = 0; i < paras.Length; i++)
                {
                    var t = paras[i].Trim();
                    if (t.Length == 0) continue;
                    paras[i] = $"{num}. {t}"; num++;
                }
                input = string.Join("\n\n", paras);
            }

            // Normalize excessive blank lines
            if (cfg.RemoveExcessiveBlankLines)
            {
                input = RxBlankLines.Replace(input, m => m.Value.StartsWith("\r\n") ? "\r\n\r\n" : "\n\n");
            }

            var lines = input.Replace("\r\n", "\n").Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var original = line;
                string working = line.TrimEnd();
                if (working.Length == 0) { lines[i] = string.Empty; continue; }

                if (cfg.NormalizeArrows)
                {
                    working = RxArrowAny.Replace(working, cfg.Arrow + " ");
                }
                if (cfg.NormalizeBullets)
                {
                    working = RxBullet.Replace(working, "- ");
                }
                if (cfg.RemoveExcessiveBlanks)
                {
                    working = RxMultiSpaces.Replace(working, " ");
                }
                if (cfg.SpaceAfterPunctuation)
                {
                    working = RxAfterPunct.Replace(working, g => g.Groups[1].Value + " ");
                }
                if (cfg.NormalizeParentheses)
                {
                    working = RxParenTrimL.Replace(working, "(");
                    working = RxParenTrimR.Replace(working, ")");
                }
                if (cfg.SpaceNumberUnit)
                {
                    working = RxNumberUnit.Replace(working, m => m.Groups[1].Value + " " + m.Groups[2].Value.ToLowerInvariant());
                }
                if (cfg.CollapseWhitespace)
                {
                    working = RxCollapseWs.Replace(working, " ");
                }
                working = working.Trim();

                if (cfg.CapitalizeSentence && working.Length > 0 && char.IsLetter(working[0]))
                {
                    working = char.ToUpperInvariant(working[0]) + (working.Length > 1 ? working[1..] : string.Empty);
                }
                if (cfg.EnsureTrailingPeriod && working.Length > 0 && char.IsLetterOrDigit(working[^1]) && !working.EndsWith('.'))
                {
                    working += '.';
                }
                lines[i] = working;
            }

            // Indent continuation lines after numbering lines if enabled
            if (cfg.IndentContinuationLines)
            {
                for (int i = 1; i < lines.Length; i++)
                {
                    var prev = lines[i - 1]; var curr = lines[i];
                    if (prev.Length == 0 || curr.Length == 0) continue;
                    if (RxNumbered.IsMatch(prev) && !RxNumbered.IsMatch(curr) && !curr.StartsWith("- ") && !curr.StartsWith(cfg.Arrow))
                    {
                        if (!curr.StartsWith("   ")) lines[i] = "   " + curr;
                    }
                }
            }

            return string.Join("\n", lines);
        }

        // For dereportify we already had logic; keep existing DereportifySingleLine methods.
        private string ApplyReportifyConclusion(string input) => ApplyReportifyBlock(input, true);
        // === Legacy helpers required by other partials (preserved) ===
        private static readonly Regex _rxLineSep = new("(\r\n|\n|\r)", RegexOptions.Compiled);
        private string DereportifyPreserveLines(string? input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            var parts = _rxLineSep.Split(input);
            for (int i = 0; i < parts.Length; i++) if (i % 2 == 0) parts[i] = DereportifySingleLine(parts[i]);
            return string.Concat(parts);
        }
        private string DereportifySingleLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return line.TrimEnd();
            line = Regex.Replace(line, @"^\s*\d+\.\s+", string.Empty);
            line = Regex.Replace(line, @"^ {1,4}", string.Empty);
            line = Regex.Replace(line, @"^\s*--?>\s*", "-->");
            if (line.EndsWith('.') && !line.EndsWith("..")) line = line[..^1];
            if (_keepCapsFirstTokens != null) line = DecapUnlessDictionary(line);
            return line.TrimEnd();
        }
        private string DecapUnlessDictionary(string line)
        {
            if (string.IsNullOrEmpty(line)) return line;
            int idx = 0; while (idx < line.Length && char.IsWhiteSpace(line[idx])) idx++;
            if (idx >= line.Length) return line; char c = line[idx]; if (!char.IsUpper(c)) return line;
            int end = idx; while (end < line.Length && char.IsLetter(line[end])) end++;
            var token = line.Substring(idx, end - idx);
            if (_keepCapsFirstTokens != null && _keepCapsFirstTokens.Contains(token)) return line;
            var lowered = char.ToLower(c) + line[(idx + 1)..];
            return idx == 0 ? lowered : line[..idx] + lowered;
        }
        private async Task EnsureCapsAsync()
        {
            if (_capsLoaded) return;
            try
            {
                var list = await _phrases.GetPhrasesForAccountAsync(_tenant.AccountId).ConfigureAwait(false);
                _keepCapsFirstTokens = new HashSet<string>(list.Select(p => FirstToken(p)).Where(t => t.Length > 0 && char.IsUpper(t[0])), StringComparer.OrdinalIgnoreCase);
            }
            catch { _keepCapsFirstTokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase); }
            finally { _capsLoaded = true; }
        }
        private static string FirstToken(string s)
        { if (string.IsNullOrWhiteSpace(s)) return string.Empty; int i = 0; while (i < s.Length && !char.IsWhiteSpace(s[i])) i++; return s[..i]; }
        private static readonly Regex _rxModality = new(@"\b(CT|MRI|MR|XR|CR|DX|US|PET[- ]?CT|PETCT|PET|MAMMO|MMG|DXA|NM)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private string ExtractModality(string? studyName)
        {
            if (string.IsNullOrWhiteSpace(studyName)) return "UNK";
            var m = _rxModality.Match(studyName); if (!m.Success) return "UNK";
            var v = m.Value.ToUpperInvariant();
            return v switch { "MRI" => "MR", "PET-CT" => "PETCT", "PET CT" => "PETCT", "MMG" => "MAMMO", _ => v };
        }
    }
}
