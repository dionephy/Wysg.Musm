using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Wysg.Musm.Radium.ViewModels
{
    /// <summary>
    /// Partial: Text normalization, reportify/dereportify helper methods & regex utilities.
    /// Extracted for clarity since these are pure string manipulation helpers reused across sections.
    /// </summary>
    public partial class MainViewModel
    {
        // Dictionary influenced casing preservation
        private System.Collections.Generic.HashSet<string>? _keepCapsFirstTokens; private bool _capsLoaded;
        private async Task EnsureCapsAsync()
        {
            if (_capsLoaded) return;
            try
            {
                var list = await _phrases.GetPhrasesForAccountAsync(_tenant.AccountId).ConfigureAwait(false);
                _keepCapsFirstTokens = new System.Collections.Generic.HashSet<string>(list.Select(p => FirstToken(p)).Where(t => t.Length > 0 && char.IsUpper(t[0])), System.StringComparer.OrdinalIgnoreCase);
            }
            catch { _keepCapsFirstTokens = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase); }
            finally { _capsLoaded = true; }
        }
        private static string FirstToken(string s)
        { if (string.IsNullOrWhiteSpace(s)) return string.Empty; int i = 0; while (i < s.Length && !char.IsWhiteSpace(s[i])) i++; return s[..i]; }

        // Reportify / dereportify placeholders (advanced formatting can be implemented later)
        private string ReportifyBlock(string input, bool isConclusion) => input;
        private string DereportifyBlock(string input, bool isConclusion) => input;
        private string ReportifySentence(string sentence) => sentence;
        private string ReportifyLineExt(string str) => str;
        private string PurifySentence(string sentence, bool reportify) => sentence;
        private string GetBody(string input, bool onlyNumber) => input;

        // Dereportify line-level utilities (moved from original file)
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

        // Regex utilities (retained from original for future formatting expansions)
        private static readonly Regex _rxArrow = new(@"^(--?>)", RegexOptions.Compiled);
        private static readonly Regex _rxBullet = new(@"^-(?!\->)|\*?(?<!-)", RegexOptions.Compiled);
        private static readonly Regex _rxAfterPunct = new(@"([;,:](?<!\d:))(?!\s)", RegexOptions.Compiled);
        private static readonly Regex _rxParensSpace = new(@"(?<=\S)\((?=\S)(?!\s*s\s*\))|(?<=\S)\)(?=[^\.,\s])(?!:)", RegexOptions.Compiled);
        private static readonly Regex _rxNumberUnit = new(@"(\d+(\.\d+)?)(cm|mm|ml)", RegexOptions.Compiled);
        private static readonly Regex _rxDot = new(@"(?<=\D)\.(?=[^\.\)]|$)", RegexOptions.Compiled);
        private static readonly Regex _rxSpaces = new(@"\s+", RegexOptions.Compiled);
        private static readonly Regex _rxLParen = new(@"\(\s+", RegexOptions.Compiled);
        private static readonly Regex _rxRParen = new(@"\s+\)", RegexOptions.Compiled);
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
