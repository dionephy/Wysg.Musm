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
            // CHANGED: Granular arrow/bullet spacing
            public bool SpaceBeforeArrows { get; init; } = false;
            public bool SpaceAfterArrows { get; init; } = true;
            public bool SpaceBeforeBullets { get; init; } = false;
            public bool SpaceAfterBullets { get; init; } = true;
            public bool SpaceAfterPunctuation { get; init; } = true;
            public bool NormalizeParentheses { get; init; } = true;
            public bool SpaceNumberUnit { get; init; } = true;
            public bool CollapseWhitespace { get; init; } = true;
            public bool NumberConclusionParagraphs { get; init; } = true;
            public bool IndentContinuationLines { get; init; } = true;
            // NEW: Number each line on one paragraph instead of numbering paragraphs
            public bool NumberConclusionLinesOnOneParagraph { get; init; } = false;
            // NEW: Capitalize first letter after bullet or conclusion number
            public bool CapitalizeAfterBulletOrNumber { get; init; } = false;
            public string Arrow { get; init; } = "-->";
            public string ConclusionNumbering { get; init; } = "1."; // seed only; we auto increment
            public string DetailingPrefix { get; init; } = "-";
        }

        private ReportifyConfig? _reportifyConfig;
        private string? _reportifyConfigJsonApplied;
        // Removed: known token caps preservation
        private void EnsureReportifyConfig()
        {
            var json = _tenant.ReportifySettingsJson;
            
            // CRITICAL FIX: Always reload if JSON changed OR if config is null
            // This ensures settings changes are applied immediately
            if (json != _reportifyConfigJsonApplied || _reportifyConfig == null)
            {
                Debug.WriteLine($"[ReportifyConfig] Reloading - JSON changed or config null");
                Debug.WriteLine($"[ReportifyConfig] Old JSON length: {_reportifyConfigJsonApplied?.Length ?? 0}");
                Debug.WriteLine($"[ReportifyConfig] New JSON length: {json?.Length ?? 0}");
            }
            else
            {
                // Config is up to date
                return;
            }
            
            if (json == null)
            {
                Debug.WriteLine("[ReportifyConfig] JSON is null, using defaults");
                _reportifyConfig = new ReportifyConfig();
                _reportifyConfigJsonApplied = null;
                return;
            }
            
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
                    // CHANGED: Load new granular settings with backward compat
                    SpaceBeforeArrows = B("space_before_arrows", false),
                    SpaceAfterArrows = B("space_after_arrows", B("normalize_arrows", true)), // fallback to old normalize_arrows
                    SpaceBeforeBullets = B("space_before_bullets", false),
                    SpaceAfterBullets = B("space_after_bullets", B("normalize_bullets", true)), // fallback to old normalize_bullets
                    SpaceAfterPunctuation = B("space_after_punctuation", true),
                    NormalizeParentheses = B("normalize_parentheses", true),
                    SpaceNumberUnit = B("space_number_unit", true),
                    CollapseWhitespace = B("collapse_whitespace", true),
                    NumberConclusionParagraphs = B("number_conclusion_paragraphs", true),
                    IndentContinuationLines = B("indent_continuation_lines", true),
                    // NEW: Parse the two new settings
                    NumberConclusionLinesOnOneParagraph = B("number_conclusion_lines_on_one_paragraph", false),
                    CapitalizeAfterBulletOrNumber = B("capitalize_after_bullet_or_number", false),
                    Arrow = Def("arrow", "-->"),
                    ConclusionNumbering = Def("conclusion_numbering", "1."),
                    DetailingPrefix = Def("detailing_prefix", "-")
                };
                
                _reportifyConfigJsonApplied = json;
                
                // Debug log loaded settings
                Debug.WriteLine($"[ReportifyConfig] Loaded settings:");
                Debug.WriteLine($"[ReportifyConfig]   SpaceBeforeArrows: {_reportifyConfig.SpaceBeforeArrows}");
                Debug.WriteLine($"[ReportifyConfig]   SpaceAfterArrows: {_reportifyConfig.SpaceAfterArrows}");
                Debug.WriteLine($"[ReportifyConfig]   SpaceBeforeBullets: {_reportifyConfig.SpaceBeforeBullets}");
                Debug.WriteLine($"[ReportifyConfig]   SpaceAfterBullets: {_reportifyConfig.SpaceAfterBullets}");
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
        // FIXED: Bullet regex now matches - followed by anything (not just space or EOL)
        // Excludes arrow patterns (doesn't match when followed by another -)
        private static readonly Regex RxBullet = new(@"^([*]|-(?!-))(?:\s*)", RegexOptions.Compiled);
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

            // Normalize line endings first
            input = input.Replace("\r\n", "\n").Replace("\r", "\n");
            
            // CRITICAL: Normalize excessive blank lines FIRST (before paragraph processing)
            // This ensures 3+ blank lines become 2 blank lines (one blank line between paragraphs)
            if (cfg.RemoveExcessiveBlankLines)
            {
                input = RxBlankLines.Replace(input, "\n\n");
            }

            // Paragraph numbering for conclusion (after blank line normalization)
            if (isConclusion && cfg.NumberConclusionParagraphs)
            {
                if (cfg.NumberConclusionLinesOnOneParagraph)
                {
                    // LINE MODE: Number each line as a separate point, remove all blank lines
                    var lines = input.Split('\n');
                    var resultLines = new List<string>();
                    int num = 1;
                    
                    foreach (var line in lines)
                    {
                        var trimmed = line.Trim();
                        
                        // Skip completely blank lines
                        if (string.IsNullOrWhiteSpace(trimmed))
                        {
                            continue; // Don't add blank lines at all in line mode
                        }
                        
                        // Check if this line already has a number (from manual entry)
                        bool hasNumber = RxNumbered.IsMatch(trimmed);
                        
                        if (hasNumber)
                        {
                            // Line already numbered - renumber for consistency
                            var match = RxNumbered.Match(trimmed);
                            var content = trimmed.Substring(match.Length);
                            resultLines.Add($"{num}. {content}");
                            num++;
                        }
                        else
                        {
                            // Line not numbered - add number
                            resultLines.Add($"{num}. {trimmed}");
                            num++;
                        }
                    }
                    
                    input = string.Join("\n", resultLines);
                }
                else
                {
                    // PARAGRAPH MODE: Number paragraphs separated by blank lines, preserve blank lines
                    var paras = input.Split("\n\n", StringSplitOptions.None);
                    int num = 1;
                    var resultParas = new List<string>();
                    
                    foreach (var para in paras)
                    {
                        var trimmed = para.Trim();
                        
                        // Empty paragraph (blank line) - preserve it
                        if (string.IsNullOrWhiteSpace(trimmed))
                        {
                            // Keep empty paragraphs to preserve spacing
                            // This will result in double newlines when joined
                            continue; // Skip empty paragraphs in the loop, they're already handled by the split/join
                        }
                        
                        // Split paragraph into lines for proper indenting
                        var paraLines = trimmed.Split('\n');
                        var formattedLines = new List<string>();
                        
                        for (int i = 0; i < paraLines.Length; i++)
                        {
                            var line = paraLines[i].Trim();
                            if (string.IsNullOrWhiteSpace(line)) continue;
                            
                            if (i == 0)
                            {
                                // First line gets the number
                                formattedLines.Add($"{num}. {line}");
                            }
                            else
                            {
                                // Continuation lines get indented
                                formattedLines.Add($"   {line}");
                            }
                        }
                        
                        if (formattedLines.Count > 0)
                        {
                            resultParas.Add(string.Join("\n", formattedLines));
                            num++;
                        }
                    }
                    
                    // Join with double newlines to preserve paragraph separation
                    input = string.Join("\n\n", resultParas);
                }
            }

            // Apply line-level transformations (capitalization, punctuation, etc.)
            var lines2 = input.Split('\n');
            for (int i = 0; i < lines2.Length; i++)
            {
                var line = lines2[i];
                string working = line.TrimEnd();
                
                // Preserve blank lines - don't process them
                if (working.Length == 0) 
                { 
                    lines2[i] = string.Empty; 
                    continue; 
                }

                // Track if we added a leading space (for space before arrows/bullets)
                bool hasLeadingSpace = false;

                // FIXED: Apply granular arrow spacing
                if (cfg.SpaceBeforeArrows || cfg.SpaceAfterArrows)
                {
                    // Match arrow patterns at start of line (with any trailing whitespace)
                    var arrowMatch = RxArrowAny.Match(working);
                    if (arrowMatch.Success)
                    {
                        // Extract the content after the arrow and any whitespace
                        var content = working.Substring(arrowMatch.Length).TrimStart();
                        var arrow = cfg.Arrow;
                        var prefix = cfg.SpaceBeforeArrows ? " " : "";
                        var suffix = cfg.SpaceAfterArrows ? " " : "";
                        working = prefix + arrow + suffix + content;
                        hasLeadingSpace = cfg.SpaceBeforeArrows;
                    }
                }
                
                // FIXED: Apply granular bullet spacing
                if (cfg.SpaceBeforeBullets || cfg.SpaceAfterBullets)
                {
                    // Match bullet patterns at start of line (with any trailing whitespace)
                    var bulletMatch = RxBullet.Match(working);
                    if (bulletMatch.Success)
                    {
                        // Extract the content after the bullet and any whitespace
                        var content = working.Substring(bulletMatch.Length).TrimStart();
                        var bullet = cfg.DetailingPrefix;
                        var prefix = cfg.SpaceBeforeBullets ? " " : "";
                        var suffix = cfg.SpaceAfterBullets ? " " : "";
                        working = prefix + bullet + suffix + content;
                        hasLeadingSpace = cfg.SpaceBeforeBullets;
                    }
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
                
                // CRITICAL FIX: Only trim if we didn't add a leading space
                // If hasLeadingSpace is true, use TrimEnd only to preserve the space before arrow/bullet
                if (hasLeadingSpace)
                {
                    working = working.TrimEnd();
                }
                else
                {
                    working = working.Trim();
                }

                // Enhanced capitalization logic
                if (cfg.CapitalizeSentence && working.Length > 0)
                {
                    // Find the first letter position (skip bullets, numbers, arrows, whitespace)
                    int firstLetterPos = 0;
                    
                    // Skip leading space (from space before arrows/bullets)
                    if (working.StartsWith(" "))
                    {
                        firstLetterPos = 1;
                    }
                    
                    // Skip leading bullet (- )
                    if (working.Length > firstLetterPos + 1 && working.Substring(firstLetterPos).StartsWith("- "))
                    {
                        firstLetterPos += 2;
                    }
                    // Skip leading number (1. 2. etc)
                    else if (RxNumbered.IsMatch(working.Substring(firstLetterPos)))
                    {
                        var match = RxNumbered.Match(working.Substring(firstLetterPos));
                        firstLetterPos += match.Length;
                    }
                    // Skip leading arrow (--> )
                    else if (working.Length > firstLetterPos + cfg.Arrow.Length && working.Substring(firstLetterPos).StartsWith(cfg.Arrow + " "))
                    {
                        firstLetterPos += cfg.Arrow.Length + 1;
                    }
                    // Skip indentation (3 spaces for continuation)
                    else if (working.Length > firstLetterPos + 2 && working.Substring(firstLetterPos).StartsWith("   "))
                    {
                        firstLetterPos += 3;
                    }
                    
                    // Find first letter after the prefix
                    while (firstLetterPos < working.Length && !char.IsLetter(working[firstLetterPos]))
                    {
                        firstLetterPos++;
                    }
                    
                    // Capitalize the first letter
                    if (firstLetterPos < working.Length && char.IsLetter(working[firstLetterPos]))
                    {
                        working = working[..firstLetterPos] + char.ToUpperInvariant(working[firstLetterPos]) + (firstLetterPos + 1 < working.Length ? working[(firstLetterPos + 1)..] : string.Empty);
                    }
                }
                
                if (cfg.EnsureTrailingPeriod && working.Length > 0 && char.IsLetterOrDigit(working[^1]) && !working.EndsWith('.'))
                {
                    working += '.';
                }
                lines2[i] = working;
            }

            return string.Join("\n", lines2);
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
            return line.TrimEnd();
        }
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
