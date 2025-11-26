using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Ude;

namespace Wysg.Musm.Radium.Views
{
    public partial class SpyWindow
    {
        private static string SanitizeFileName(string name)
        {
            var invalid = System.IO.Path.GetInvalidFileNameChars();
            return string.Join("_", name.Split(invalid, StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.');
        }
        static SpyWindow()
        {
            TryRegisterCodePages();
        }

        private static void TryRegisterCodePages()
        {
            try
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            }
            catch
            {
                try
                {
                    var provType = Type.GetType("System.Text.CodePagesEncodingProvider, System.Text.Encoding.CodePages");
                    if (provType != null)
                    {
                        var instanceProp = provType.GetProperty("Instance");
                        var instance = instanceProp?.GetValue(null);
                        var register = typeof(Encoding).GetMethod("RegisterProvider", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                        if (instance != null && register != null)
                        {
                            register.Invoke(null, new[] { instance });
                        }
                    }
                }
                catch { }
            }
        }

        private static bool LooksLatin1Mojibake(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            int latin1High = s.Count(ch => ch >= '\u00A0' && ch <= '\u00FF');
            int han = CountHangul(s);
            return latin1High >= 6 && han < 3;
        }
        private static string? TryRepairLatin1ToUtf8(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;
            if (text.Any(c => c > 0xFF)) return null;
            var buf = new byte[text.Length];
            for (int i = 0; i < text.Length; i++) buf[i] = (byte)(text[i] & 0xFF);
            try { return Encoding.UTF8.GetString(buf); } catch { return null; }
        }
        private static string? TryRepairCp1252ToUtf8(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;
            try
            {
                var bytes = Encoding.GetEncoding(1252).GetBytes(text);
                return Encoding.UTF8.GetString(bytes);
            }
            catch { return null; }
        }
        private static string? TryRepairLatin1ToCp949(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;
            if (text.Any(c => c > 0xFF)) return null;
            var buf = new byte[text.Length];
            for (int i = 0; i < text.Length; i++) buf[i] = (byte)(text[i] & 0xFF);
            try { return Encoding.GetEncoding("cp949").GetString(buf); } catch { return null; }
        }
        private static string RepairLatin1Runs(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            var rx = new Regex("[\u00A0-\u00FF\u20AC]+", RegexOptions.Compiled);
            var sb = new StringBuilder();
            int last = 0;
            foreach (Match m in rx.Matches(s))
            {
                if (m.Index > last) sb.Append(s, last, m.Index - last);
                var chunk = m.Value;
                string best = chunk;
                int bestHan = CountHangul(chunk); int bestRep = CountReplacement(chunk);
                void Consider(string? c)
                {
                    if (string.IsNullOrEmpty(c)) return;
                    int han = CountHangul(c); int rep = CountReplacement(c);
                    if (rep < bestRep || (rep == bestRep && han > bestHan)) { best = c; bestHan = han; bestRep = rep; }
                }
                Consider(TryRepairLatin1ToUtf8(chunk));
                Consider(TryRepairCp1252ToUtf8(chunk));
                Consider(TryRepairLatin1ToCp949(chunk));
                sb.Append(best);
                last = m.Index + m.Length;
            }
            if (last < s.Length) sb.Append(s, last, s.Length - last);
            return sb.ToString();
        }

        private static string NormalizeKoreanMojibake(string? s)
        {
            if (string.IsNullOrEmpty(s)) return s ?? string.Empty;
            var repairedRuns = RepairLatin1Runs(s);
            if (!LooksLatin1Mojibake(repairedRuns)) return repairedRuns;
            string best = repairedRuns; int bestHan = CountHangul(repairedRuns); int bestRep = CountReplacement(repairedRuns);
            void Consider(string? c)
            {
                if (string.IsNullOrEmpty(c)) return;
                int han = CountHangul(c); int rep = CountReplacement(c);
                if (rep < bestRep || (rep == bestRep && han > bestHan)) { best = c; bestHan = han; bestRep = rep; }
            }
            Consider(TryRepairLatin1ToUtf8(repairedRuns));
            Consider(TryRepairCp1252ToUtf8(repairedRuns));
            Consider(TryRepairLatin1ToCp949(repairedRuns));
            return best;
        }

        private static int CountReplacement(string s)
        {
            int n = 0; foreach (var ch in s) if (ch == '\uFFFD') n++; return n;
        }
        private static int CountHangul(string s)
        {
            int c = 0; foreach (var ch in s) { if ((ch >= '\uAC00' && ch <= '\uD7A3') || (ch >= '\u3130' && ch <= '\u318F')) c++; } return c;
        }

        private static (int rep, int han) ScoreText(string s) => (CountReplacement(s), CountHangul(s));

        private static bool HasUtf8Bom(byte[] b) => b.Length >= 3 && b[0] == 0xEF && b[1] == 0xBB && b[2] == 0xBF;
        private static bool HasUtf16LeBom(byte[] b) => b.Length >= 2 && b[0] == 0xFF && b[1] == 0xFE;
        private static bool HasUtf16BeBom(byte[] b) => b.Length >= 2 && b[0] == 0xFE && b[1] == 0xFF;
        private static Encoding? TryResolveEncoding(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            name = name.Trim().Trim('\'', '"');
            if (name.Equals("euc-kr", StringComparison.OrdinalIgnoreCase) || name.Equals("ks_c_5601-1987", StringComparison.OrdinalIgnoreCase))
                name = "cp949";
            try { return Encoding.GetEncoding(name); }
            catch (ArgumentException)
            {
                if (name.Equals("cp949", StringComparison.OrdinalIgnoreCase))
                {
                    try { return Encoding.GetEncoding(949); } catch { }
                }
                return null;
            }
            catch { return null; }
        }

        private static string DecodeBest(byte[] bytes, System.Collections.Generic.List<Encoding> encs)
        {
            string? bestText = null; int bestRep = int.MaxValue; int bestHangul = -1; int bestIdx = -1; int idx = 0;
            foreach (var enc in encs)
            {
                string text;
                try { text = enc.GetString(bytes); } catch { idx++; continue; }
                int rep = CountReplacement(text);
                int han = CountHangul(text);
                bool better = rep < bestRep || (rep == bestRep && han > bestHangul) || (rep == bestRep && han == bestHangul && bestIdx == -1);
                if (better) { bestRep = rep; bestHangul = han; bestText = text; bestIdx = idx; }
                idx++;
            }
            return bestText ?? Encoding.UTF8.GetString(bytes);
        }
    }
}
