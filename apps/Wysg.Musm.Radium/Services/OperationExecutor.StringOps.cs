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
