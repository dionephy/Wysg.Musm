using System;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Wysg.Musm.Radium.Services
{
    /// <summary>
    /// OperationExecutor partial class: String manipulation operations.
    /// Contains operations for text processing, splitting, trimming, replacing, etc.
    /// </summary>
    internal static partial class OperationExecutor
    {
        #region String Operations

        private static (string preview, string? value) ExecuteSplit(string? input, string? sepRaw, string? indexStr)
        {
            sepRaw ??= string.Empty;

            Debug.WriteLine($"[Split] Input length: {input?.Length ?? 0}");
            Debug.WriteLine($"[Split] SepRaw: '{sepRaw}' (length: {sepRaw.Length})");

            if (input == null) { return ("(null)", null); }

            string[] parts;
            if (sepRaw.StartsWith("re:", StringComparison.OrdinalIgnoreCase) || sepRaw.StartsWith("regex:", StringComparison.OrdinalIgnoreCase))
            {
                var pattern = sepRaw.StartsWith("re:", StringComparison.OrdinalIgnoreCase) ? sepRaw.Substring(3) : sepRaw.Substring(6);
                if (string.IsNullOrEmpty(pattern)) { return ("(empty pattern)", null); }
                try { parts = Regex.Split(input, pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase); }
                catch (Exception ex) { return ($"(regex error: {ex.Message})", null); }
            }
            else
            {
                var sep = UnescapeUserText(sepRaw);
                parts = input.Split(new[] { sep }, StringSplitOptions.None);

                // Try CRLF if LF alone didn't work
                if (parts.Length == 1 && sep.Contains('\n') && !sep.Contains("\r\n"))
                {
                    var crlfSep = sep.Replace("\n", "\r\n");
                    parts = input.Split(new[] { crlfSep }, StringSplitOptions.None);
                }
            }

            if (!string.IsNullOrWhiteSpace(indexStr) && int.TryParse(indexStr.Trim(), out var idx))
            {
                if (idx >= 0 && idx < parts.Length)
                {
                    var value = parts[idx];
                    return (value ?? string.Empty, value);
                }
                else
                {
                    return ($"(index out of range {parts.Length})", null);
                }
            }
            else
            {
                var value = string.Join("\u001F", parts);
                return (parts.Length + " parts", value);
            }
        }

        private static (string preview, string? value) ExecuteIsMatch(string? value1, string? value2)
        {
            value1 ??= string.Empty;
            value2 ??= string.Empty;

            bool match = string.Equals(value1, value2, StringComparison.Ordinal);
            string result = match ? "true" : "false";

            return ($"{result} ('{value1}' vs '{value2}')", result);
        }

        private static (string preview, string? value) ExecuteIsAlmostMatch(string? value1, string? value2)
        {
            value1 ??= string.Empty;
            value2 ??= string.Empty;

            // Exact match check
            if (string.Equals(value1, value2, StringComparison.Ordinal))
            {
                return ("true (exact match)", "true");
            }

            // Normalize both strings for similarity comparison
            string norm1 = NormalizeForComparison(value1);
            string norm2 = NormalizeForComparison(value2);

            // Check normalized match
            if (string.Equals(norm1, norm2, StringComparison.Ordinal))
            {
                return ("true (normalized match)", "true");
            }

            // Check datetime pattern similarity (e.g., "2025-10-24 0456126" vs "2025-10-24 04:56:26")
            if (IsDateTimeSimilar(value1, value2))
            {
                return ("true (datetime similar)", "true");
            }

            return ("false", "false");
        }

        private static (string preview, string? value) ExecuteAnd(string? value1, string? value2)
        {
            value1 ??= string.Empty;
            value2 ??= string.Empty;

            // Check if both values are "true" (case-insensitive)
            bool isTrue1 = string.Equals(value1, "true", StringComparison.OrdinalIgnoreCase);
            bool isTrue2 = string.Equals(value2, "true", StringComparison.OrdinalIgnoreCase);

            bool result = isTrue1 && isTrue2;
            string resultStr = result ? "true" : "false";

            return ($"{resultStr} ({value1} AND {value2})", resultStr);
        }

        private static (string preview, string? value) ExecuteNot(string? value1)
        {
            value1 ??= string.Empty;

            // Check if value is "true" (case-insensitive)
            bool isTrue = string.Equals(value1, "true", StringComparison.OrdinalIgnoreCase);

            // Invert the boolean value
            bool result = !isTrue;
            string resultStr = result ? "true" : "false";

            return ($"{resultStr} (NOT {value1})", resultStr);
        }

        /// <summary>
        /// Compares two strings and returns the longer one.
        /// If both strings are equal in length, returns the first one.
        /// </summary>
        private static (string preview, string? value) ExecuteGetLongerText(string? text1, string? text2)
        {
            text1 ??= string.Empty;
            text2 ??= string.Empty;

            string longerText = text1.Length >= text2.Length ? text1 : text2;
            int len1 = text1.Length;
            int len2 = text2.Length;

            string preview = $"{longerText.Length} chars (text1: {len1}, text2: {len2})";
            return (preview, longerText);
        }

        /// <summary>
        /// Normalizes a string for comparison by removing whitespace, punctuation, and converting to uppercase.
        /// </summary>
        private static string NormalizeForComparison(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            
            // Remove all non-alphanumeric characters and convert to uppercase
            return System.Text.RegularExpressions.Regex.Replace(input, @"[^A-Za-z0-9]", "").ToUpperInvariant();
        }

        /// <summary>
        /// Checks if two strings represent similar datetimes (handles OCR errors like colons read as digits).
        /// </summary>
        private static bool IsDateTimeSimilar(string value1, string value2)
        {
            // Pattern: YYYY-MM-DD followed by 6-7 digits (time with possible separators as digits)
            // Example: "2025-10-24 0456126" should match "2025-10-24 04:56:26"
            
            var dateTimePattern = new System.Text.RegularExpressions.Regex(
                @"(\d{4}-\d{2}-\d{2})\s*(\d{1,2})[\D:lI1]?(\d{2})[\D:lI1]?(\d{2})",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            var match1 = dateTimePattern.Match(value1);
            var match2 = dateTimePattern.Match(value2);

            if (!match1.Success || !match2.Success) return false;

            // Compare date part (YYYY-MM-DD)
            if (match1.Groups[1].Value != match2.Groups[1].Value) return false;

            // Compare time components (HH, MM, SS) - normalize to 2 digits
            string time1 = $"{match1.Groups[2].Value.PadLeft(2, '0')}{match1.Groups[3].Value}{match1.Groups[4].Value}";
            string time2 = $"{match2.Groups[2].Value.PadLeft(2, '0')}{match2.Groups[3].Value}{match2.Groups[4].Value}";

            return time1 == time2;
        }

        private static (string preview, string? value) ExecuteTrimString(string? sourceString, string? trimString)
        {
            sourceString ??= string.Empty;
            trimString ??= string.Empty;

            if (string.IsNullOrEmpty(trimString))
            {
                return (sourceString, sourceString);
            }

            var result = sourceString;

            // Trim from start
            while (result.StartsWith(trimString, StringComparison.Ordinal))
            {
                result = result.Substring(trimString.Length);
            }

            // Trim from end
            while (result.EndsWith(trimString, StringComparison.Ordinal))
            {
                result = result.Substring(0, result.Length - trimString.Length);
            }

            return (result, result);
        }

        private static (string preview, string? value) ExecuteReplace(string? input, string? searchRaw, string? replRaw)
        {
            input ??= string.Empty;
            searchRaw ??= string.Empty;
            replRaw ??= string.Empty;

            var search = UnescapeUserText(searchRaw);
            var repl = UnescapeUserText(replRaw);

            if (string.IsNullOrEmpty(search))
            {
                return (input, input);
            }

            var value = input.Replace(search, repl);
            return (value, value);
        }

        private static (string preview, string? value) ExecuteMerge(string? input1, string? input2, string? separator)
        {
            input1 ??= string.Empty;
            input2 ??= string.Empty;
            separator ??= string.Empty;

            string merged;
            if (string.IsNullOrEmpty(separator))
            {
                merged = input1 + input2;
            }
            else
            {
                merged = input1 + separator + input2;
            }

            return (merged, merged);
        }

        private static (string preview, string? value) ExecuteTakeLast(string? combined)
        {
            combined ??= string.Empty;
            var arr = combined.Split('\u001F');
            var value = arr.Length > 0 ? arr[^1] : string.Empty;
            return (value, value);
        }

        private static (string preview, string? value) ExecuteTrim(string? s)
        {
            var value = s?.Trim();
            return (value ?? "(null)", value);
        }

        private static (string preview, string? value) ExecuteToDateTime(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) { return ("(null)", null); }

            if (DateTime.TryParse(s.Trim(), out var dt))
            {
                var iso = dt.ToString("o");
                return (dt.ToString("yyyy-MM-dd HH:mm:ss"), iso);
            }

            return ("(parse fail)", null);
        }

        private static string UnescapeUserText(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            try { return Regex.Unescape(s); } catch { return s; }
        }

        #endregion
    }
}
