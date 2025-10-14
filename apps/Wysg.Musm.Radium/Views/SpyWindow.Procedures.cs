using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FlaUI.Core.AutomationElements;
using Wysg.Musm.Radium.Services;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using Ude;
using System.Net;

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

        private enum ArgKind { Element, String, Number, Var }

        private sealed class ProcArg : INotifyPropertyChanged
        {
            private string _type = nameof(ArgKind.String);
            private string? _value;
            public string Type { get => _type; set => SetField(ref _type, value); }
            public string? Value { get => _value; set => SetField(ref _value, value); }
            public event PropertyChangedEventHandler? PropertyChanged;
            private void OnPropertyChanged([CallerMemberName] string? n = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
            private bool SetField<T>(ref T f, T v, [CallerMemberName] string? n = null)
            { if (EqualityComparer<T>.Default.Equals(f, v)) return false; f = v; OnPropertyChanged(n); return true; }
        }

        private sealed class ProcOpRow : INotifyPropertyChanged
        {
            private string _op = string.Empty;
            private ProcArg _arg1 = new();
            private ProcArg _arg2 = new();
            private ProcArg _arg3 = new();
            private bool _arg1Enabled = true;
            private bool _arg2Enabled = true;
            private bool _arg3Enabled = false;
            private string? _outputVar;
            private string? _outputPreview;
            public string Op { get => _op; set => SetField(ref _op, value); }
            public ProcArg Arg1 { get => _arg1; set => SetField(ref _arg1, value); }
            public ProcArg Arg2 { get => _arg2; set => SetField(ref _arg2, value); }
            public ProcArg Arg3 { get => _arg3; set => SetField(ref _arg3, value); }
            public bool Arg1Enabled { get => _arg1Enabled; set => SetField(ref _arg1Enabled, value); }
            public bool Arg2Enabled { get => _arg2Enabled; set => SetField(ref _arg2Enabled, value); }
            public bool Arg3Enabled { get => _arg3Enabled; set => SetField(ref _arg3Enabled, value); }
            public string? OutputVar { get => _outputVar; set => SetField(ref _outputVar, value); }
            public string? OutputPreview { get => _outputPreview; set => SetField(ref _outputPreview, value); }
            public event PropertyChangedEventHandler? PropertyChanged;
            private void OnPropertyChanged([CallerMemberName] string? n = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
            private bool SetField<T>(ref T f, T v, [CallerMemberName] string? n = null)
            { if (EqualityComparer<T>.Default.Equals(f, v)) return false; f = v; OnPropertyChanged(n); return true; }
        }

        private sealed class ProcStore { public Dictionary<string, List<ProcOpRow>> Methods { get; set; } = new(); }

        private static readonly HttpClient _http = CreateHttp();
        private static HttpClient CreateHttp()
        {
            TryRegisterCodePages(); // Ensure early registration
            var handler = new SocketsHttpHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli
            };
            var client = new HttpClient(handler, disposeHandler: true);
            try
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/123.0 Safari/537.36");
            }
            catch { }
            return client;
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

        private static string DecodeMixedUtf8Cp949(byte[] bytes)
        {
            var sb = new StringBuilder(bytes.Length);
            Encoding cp949;
            try { cp949 = Encoding.GetEncoding("cp949"); }
            catch { cp949 = Encoding.GetEncoding(949); }
            int i = 0;
            while (i < bytes.Length)
            {
                byte b0 = bytes[i];
                if (b0 < 0x80)
                {
                    sb.Append((char)b0);
                    i++;
                    continue;
                }

                if (i + 2 < bytes.Length && (b0 & 0xF0) == 0xE0)
                {
                    byte b1 = bytes[i + 1], b2 = bytes[i + 2];
                    if ((b1 & 0xC0) == 0x80 && (b2 & 0xC0) == 0x80)
                    {
                        int codepoint = ((b0 & 0x0F) << 12) | ((b1 & 0x3F) << 6) | (b2 & 0x3F);
                        // Prefer when it yields Hangul/Jamo
                        if ((codepoint >= 0xAC00 && codepoint <= 0xD7A3) || (codepoint >= 0x3130 && codepoint <= 0x318F))
                        {
                            sb.Append((char)codepoint);
                            i += 3;
                            continue;
                        }
                    }
                }
                if (i + 1 < bytes.Length && (b0 & 0xE0) == 0xC0)
                {
                    byte b1 = bytes[i + 1];
                    if ((b1 & 0xC0) == 0x80)
                    {
                        int codepoint = ((b0 & 0x1F) << 6) | (b1 & 0x3F);
                        if (codepoint >= 0x80)
                        {
                            sb.Append((char)codepoint);
                            i += 2;
                            continue;
                        }
                    }
                }

                if (i + 1 < bytes.Length)
                {
                    var span2 = new byte[] { bytes[i], bytes[i + 1] };
                    var chars2 = cp949.GetChars(span2);
                    if (chars2.Length > 0 && chars2[0] != '?' && chars2[0] != '\uFFFD')
                    {
                        sb.Append(chars2[0]);
                        i += 2;
                        continue;
                    }
                }
                var ch1 = cp949.GetChars(new byte[] { bytes[i] });
                sb.Append(ch1.Length > 0 ? ch1[0] : (char)bytes[i]);
                i++;
            }
            return sb.ToString();
        }

        private static bool IndicatesKr(string? charset)
        {
            if (string.IsNullOrWhiteSpace(charset)) return false;
            var s = charset.ToLowerInvariant();
            return s.Contains("949") || s.Contains("euc-kr") || s.Contains("ks_c_5601");
        }

        private static async Task<string> HttpGetHtmlSmartAsync(string url)
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            using var res = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
            res.EnsureSuccessStatusCode();
            var bytes = await res.Content.ReadAsByteArrayAsync().ConfigureAwait(false);

            var hexDump = string.Join(" ", bytes.Take(512).Select(b => b.ToString("X2")));
            Debug.WriteLine($"[HttpGetHtmlSmartAsync] First 512 bytes (hex): {hexDump}");
            Debug.WriteLine($"[HttpGetHtmlSmartAsync] Content-Type: {res.Content.Headers.ContentType}");

            if (HasUtf8Bom(bytes)) return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
            if (HasUtf16LeBom(bytes)) return Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2);
            if (HasUtf16BeBom(bytes)) return Encoding.BigEndianUnicode.GetString(bytes, 2, bytes.Length - 2);

            var candidates = new List<Encoding>();

            var headerCs = res.Content.Headers.ContentType?.CharSet;
            var headerEnc = TryResolveEncoding(headerCs);
            if (headerEnc != null) candidates.Add(headerEnc);

            var headLen = Math.Min(bytes.Length, 262144);
            bool metaEucKr = false;
            if (headLen > 0)
            {
                var headAscii = Encoding.ASCII.GetString(bytes, 0, headLen);
                foreach (Match m in Regex.Matches(headAscii, "charset\\s*=\\s*([A-Za-z0-9_\\-]+)", RegexOptions.IgnoreCase))
                {
                    var val = m.Groups[1].Value;
                    Debug.WriteLine($"[HttpGetHtmlSmartAsync] Found meta charset: {val}");
                    if (val.Equals("euc-kr", StringComparison.OrdinalIgnoreCase) || val.Equals("ks_c_5601-1987", StringComparison.OrdinalIgnoreCase))
                        metaEucKr = true;
                    var metaEnc = TryResolveEncoding(val);
                    if (metaEnc != null) candidates.Add(metaEnc);
                }
            }

            try
            {
                var det = new CharsetDetector();
                det.Feed(bytes, 0, bytes.Length);
                det.DataEnd();
                if (!string.IsNullOrWhiteSpace(det.Charset))
                {
                    Debug.WriteLine($"[HttpGetHtmlSmartAsync] Ude detected: {det.Charset}");
                    // Ignore UDE if it says windows-1252 but meta says euc-kr
                    if (!metaEucKr || !det.Charset.Equals("windows-1252", StringComparison.OrdinalIgnoreCase))
                    {
                        var udeEnc = TryResolveEncoding(det.Charset);
                        if (udeEnc != null) candidates.Add(udeEnc);
                    }
                }
            }
            catch { }

            // Ensure CP949/EUC-KR is in candidates
            foreach (var name in new[] { "cp949", "euc-kr", "ks_c_5601-1987", "x-windows-949", "ms949" })
            {
                var e = TryResolveEncoding(name);
                if (e != null) candidates.Add(e);
            }
            candidates.Add(Encoding.UTF8);
            candidates.Add(Encoding.GetEncoding("iso-8859-1"));

            var distinct = new List<Encoding>();
            foreach (var enc in candidates)
                if (!distinct.Any(x => string.Equals(x.WebName, enc.WebName, StringComparison.OrdinalIgnoreCase)))
                    distinct.Add(enc);

            var best = DecodeBest(bytes, distinct);
            Debug.WriteLine($"[HttpGetHtmlSmartAsync] DecodeBest selected: {distinct.IndexOf(distinct.First(x => DecodeBest(bytes, new List<Encoding> { x }) == best))} with score={ScoreText(best).rep}rep/{ScoreText(best).han}han");

            if (metaEucKr || IndicatesKr(headerCs))
            {
                string candBest = best; var scoreBest = ScoreText(candBest);
                string candCp949 = string.Empty, candUtf8 = string.Empty, candMixed = string.Empty;
                try { candCp949 = Encoding.GetEncoding(949).GetString(bytes); } catch { }
                try { candUtf8 = Encoding.UTF8.GetString(bytes); } catch { }
                try { candMixed = DecodeMixedUtf8Cp949(bytes); } catch { }

                Debug.WriteLine($"[HttpGetHtmlSmartAsync] Scoring: best={scoreBest.rep}rep/{scoreBest.han}han, cp949={ScoreText(candCp949).rep}rep/{ScoreText(candCp949).han}han, utf8={ScoreText(candUtf8).rep}rep/{ScoreText(candUtf8).han}han, mixed={ScoreText(candMixed).rep}rep/{ScoreText(candMixed).han}han");

                void Consider(ref string cur, ref (int rep, int han) curScore, string cand)
                {
                    if (string.IsNullOrEmpty(cand)) return;
                    var s = ScoreText(cand);
                    if (s.rep < curScore.rep || (s.rep == curScore.rep && s.han > curScore.han))
                    { cur = cand; curScore = s; }
                }

                Consider(ref candBest, ref scoreBest, candCp949);
                Consider(ref candBest, ref scoreBest, candUtf8);
                Consider(ref candBest, ref scoreBest, candMixed);
                best = candBest;
                Debug.WriteLine($"[HttpGetHtmlSmartAsync] Winner: score={scoreBest.rep}rep/{scoreBest.han}han");
            }

            best = RepairLatin1Runs(best);

            if (LooksLatin1Mojibake(best))
            {
                string improved = best;
                int bestHan = CountHangul(best);
                int bestRep = CountReplacement(best);
                void Consider(string? s)
                {
                    if (string.IsNullOrEmpty(s)) return;
                    int han = CountHangul(s); int rep = CountReplacement(s);
                    if (rep < bestRep || (rep == bestRep && han > bestHan)) { improved = s; bestHan = han; bestRep = rep; }
                }
                Consider(TryRepairLatin1ToUtf8(best));
                Consider(TryRepairCp1252ToUtf8(best));
                Consider(TryRepairLatin1ToCp949(best));
                improved = RepairLatin1Runs(improved);
                best = improved;
            }

            return best;
        }

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

        private static string DecodeBest(byte[] bytes, List<Encoding> encs)
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

        private void OnAddProcRow(object sender, RoutedEventArgs e)
        {
            if (FindName("gridProcSteps") is System.Windows.Controls.DataGrid procGrid) returnToList(procGrid, list => list.Add(new ProcOpRow()));
        }
        private void OnRemoveProcRow(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button b && b.Tag is ProcOpRow row && FindName("gridProcSteps") is System.Windows.Controls.DataGrid procGrid)
            {
                returnToList(procGrid, list => list.Remove(row));
            }
        }
        private async void OnSetProcRow(object sender, RoutedEventArgs e)
        {
            if (FindName("gridProcSteps") is not System.Windows.Controls.DataGrid procGrid) return;
            try { procGrid.CommitEdit(DataGridEditingUnit.Cell, true); procGrid.CommitEdit(DataGridEditingUnit.Row, true); } catch { }
            var list = procGrid.Items.OfType<ProcOpRow>().ToList();
            if (sender is System.Windows.Controls.Button b && b.Tag is ProcOpRow row)
            {
                var index = list.IndexOf(row); if (index < 0) return;
                var vars = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < index; i++) vars[$"var{i + 1}"] = list[i].OutputVar != null ? list[i].OutputPreview : null;
                var varName = $"var{index + 1}";
                var result = NeedsAsync(row.Op)
                    ? await ExecuteSingleAsync(row, vars)
                    : ExecuteSingle(row, vars);
                row.OutputVar = varName;
                row.OutputPreview = result.preview;
                if (!ProcedureVars.Contains(varName)) ProcedureVars.Add(varName);
                procGrid.ItemsSource = null; procGrid.ItemsSource = list;
            }
        }

        private static bool NeedsAsync(string? op)
        {
            return string.Equals(op, "GetTextOCR", StringComparison.OrdinalIgnoreCase)
                || string.Equals(op, "GetHTML", StringComparison.OrdinalIgnoreCase);
        }

        private bool _handlingProcOpChange;
        private void OnProcOpChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_handlingProcOpChange) return;
            if (sender is System.Windows.Controls.ComboBox cb && cb.DataContext is ProcOpRow row)
            {
                try
                {
                    _handlingProcOpChange = true;
                    switch (row.Op)
                    {
                        case "GetText":
                        case "GetTextOCR":
                        case "GetName":
                        case "Invoke":
                            row.Arg1.Type = nameof(ArgKind.Element); row.Arg1Enabled = true;
                            row.Arg2.Type = nameof(ArgKind.String); row.Arg2Enabled = false; row.Arg2.Value = string.Empty;
                            row.Arg3.Type = nameof(ArgKind.Number); row.Arg3Enabled = false; row.Arg3.Value = string.Empty;
                            break;
                        case "Split":
                            row.Arg1.Type = nameof(ArgKind.Var); row.Arg1Enabled = true;
                            row.Arg2.Type = nameof(ArgKind.String); row.Arg2Enabled = true; if (string.IsNullOrWhiteSpace(row.Arg2.Value)) row.Arg2.Value = ",";
                            row.Arg3.Type = nameof(ArgKind.Number); row.Arg3Enabled = true; if (string.IsNullOrWhiteSpace(row.Arg3.Value)) row.Arg3.Value = "0";
                            break;
                        case "Replace":
                            row.Arg1.Type = nameof(ArgKind.Var); row.Arg1Enabled = true;
                            row.Arg2.Type = nameof(ArgKind.String); row.Arg2Enabled = true;
                            row.Arg3.Type = nameof(ArgKind.String); row.Arg3Enabled = true;
                            break;
                        case "GetHTML":
                            row.Arg1.Type = nameof(ArgKind.Var); row.Arg1Enabled = true;
                            row.Arg2.Type = nameof(ArgKind.String); row.Arg2Enabled = false; row.Arg2.Value = string.Empty;
                            row.Arg3.Type = nameof(ArgKind.String); row.Arg3Enabled = false; row.Arg3.Value = string.Empty;
                            break;
                        case "MouseClick":
                            // Arg1: Number (X), Arg2: Number (Y)
                            row.Arg1.Type = nameof(ArgKind.Number); row.Arg1Enabled = true; if (string.IsNullOrWhiteSpace(row.Arg1.Value)) row.Arg1.Value = "0";
                            row.Arg2.Type = nameof(ArgKind.Number); row.Arg2Enabled = true; if (string.IsNullOrWhiteSpace(row.Arg2.Value)) row.Arg2.Value = "0";
                            row.Arg3.Type = nameof(ArgKind.String); row.Arg3Enabled = false; row.Arg3.Value = string.Empty;
                            break;
                        case "TakeLast":
                        case "Trim":
                        case "ToDateTime":
                            row.Arg1.Type = nameof(ArgKind.Var); row.Arg1Enabled = true;
                            row.Arg2.Type = nameof(ArgKind.String); row.Arg2Enabled = false; row.Arg2.Value = string.Empty;
                            row.Arg3.Type = nameof(ArgKind.Number); row.Arg3Enabled = false; row.Arg3.Value = string.Empty;
                            break;
                        case "GetValueFromSelection":
                            row.Arg1.Type = nameof(ArgKind.Element); row.Arg1Enabled = true;
                            row.Arg2.Type = nameof(ArgKind.String); row.Arg2Enabled = true; if (string.IsNullOrWhiteSpace(row.Arg2.Value)) row.Arg2.Value = "ID";
                            row.Arg3.Type = nameof(ArgKind.Number); row.Arg3Enabled = false; row.Arg3.Value = string.Empty;
                            break;
                        default:
                            row.Arg3Enabled = false; row.Arg3.Value = string.Empty;
                            break;
                    }
                }
                finally { _handlingProcOpChange = false; }
            }
        }

        private void returnToList(System.Windows.Controls.DataGrid grid, Action<List<ProcOpRow>> mutator)
        {
            var list = grid.Items.OfType<ProcOpRow>().ToList();
            mutator(list);
            grid.ItemsSource = null; grid.ItemsSource = list;
            UpdateProcedureVarsFrom(list);
        }

        private static string UnescapeUserText(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            try { return Regex.Unescape(s); } catch { return s; }
        }

        private (string preview, string? value) ExecuteSingle(ProcOpRow row, Dictionary<string, string?> vars)
        {
            string? valueToStore = null; string preview;
            switch (row.Op)
            {
                case "Split":
                    var input = ResolveString(row.Arg1, vars);
                    var sepRaw = ResolveString(row.Arg2, vars) ?? string.Empty;
                    var indexStr = ResolveString(row.Arg3, vars);
                    if (input == null) { preview = "(null)"; break; }

                    string[] parts;
                    // Regex mode: prefix with re: or regex:
                    if (sepRaw.StartsWith("re:", StringComparison.OrdinalIgnoreCase) || sepRaw.StartsWith("regex:", StringComparison.OrdinalIgnoreCase))
                    {
                        var pattern = sepRaw.StartsWith("re:", StringComparison.OrdinalIgnoreCase) ? sepRaw.Substring(3) : sepRaw.Substring(6);
                        if (string.IsNullOrEmpty(pattern)) { preview = "(empty pattern)"; break; }
                        try { parts = Regex.Split(input, pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase); }
                        catch (Exception ex) { preview = $"(regex error: {ex.Message})"; break; }
                    }
                    else
                    {
                        // Support C#-style escapes like \n, \r\n, \t in separator
                        var sep = UnescapeUserText(sepRaw);
                        parts = input.Split(new[] { sep }, StringSplitOptions.None);
                        // If user pasted multi-line HTML with Windows line breaks, also try CRLF when sep contained only LF
                        if (parts.Length == 1 && sep.Contains('\n') && !sep.Contains("\r\n"))
                        {
                            var crlfSep = sep.Replace("\n", "\r\n");
                            parts = input.Split(new[] { crlfSep }, StringSplitOptions.None);
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(indexStr) && int.TryParse(indexStr.Trim(), out var idx))
                    {
                        if (idx >= 0 && idx < parts.Length)
                        { valueToStore = parts[idx]; preview = valueToStore ?? string.Empty; }
                        else { preview = $"(index out of range {parts.Length})"; }
                    }
                    else { valueToStore = string.Join("\u001F", parts); preview = $"{parts.Length} parts"; }
                    break;
                case "Replace":
                    var input2 = ResolveString(row.Arg1, vars);
                    var searchRaw = ResolveString(row.Arg2, vars) ?? string.Empty;
                    var replRaw = ResolveString(row.Arg3, vars) ?? string.Empty;
                    if (input2 == null) { preview = "(null)"; break; }
                    // Support escapes for Replace too
                    var search = UnescapeUserText(searchRaw);
                    var repl = UnescapeUserText(replRaw);
                    if (string.IsNullOrEmpty(search)) { valueToStore = input2; preview = input2; break; }
                    valueToStore = input2.Replace(search, repl);
                    preview = valueToStore;
                    break;
                case "GetText":
                    var el = ResolveElement(row.Arg1);
                    if (el == null) { preview = "(no element)"; break; }
                    try
                    {
                        var name = el.Name;
                        var val = el.Patterns.Value.PatternOrDefault?.Value ?? string.Empty;
                        var legacy = el.Patterns.LegacyIAccessible.PatternOrDefault?.Name ?? string.Empty;
                        var raw = !string.IsNullOrEmpty(val) ? val : (!string.IsNullOrEmpty(name) ? name : legacy);
                        valueToStore = NormalizeKoreanMojibake(raw);
                        preview = valueToStore ?? "(null)";
                    }
                    catch { preview = "(error)"; }
                    break;
                case "GetName":
                    var el2 = ResolveElement(row.Arg1);
                    if (el2 == null) { preview = "(no element)"; break; }
                    try { var raw = el2.Name; valueToStore = NormalizeKoreanMojibake(raw); preview = string.IsNullOrEmpty(valueToStore) ? "(empty)" : valueToStore; }
                    catch { preview = "(error)"; }
                    break;
                case "GetTextOCR":
                    var el3 = ResolveElement(row.Arg1);
                    if (el3 == null) { preview = "(no element)"; break; }
                    try
                    {
                        var r = el3.BoundingRectangle; if (r.Width <= 0 || r.Height <= 0) { preview = "(no bounds)"; break; }
                        var hwnd = new IntPtr(el3.Properties.NativeWindowHandle.Value); if (hwnd == IntPtr.Zero) { preview = "(no hwnd)"; break; }
                        var (engine, text) = Wysg.Musm.MFCUIA.OcrReader.OcrTryReadRegionDetailedAsync(hwnd, new System.Drawing.Rectangle(0, 0, (int)r.Width, (int)r.Height)).ConfigureAwait(false).GetAwaiter().GetResult();
                        if (!engine) preview = "(ocr unavailable)"; else { valueToStore = text; preview = string.IsNullOrWhiteSpace(text) ? "(empty)" : text!; }
                    }
                    catch { preview = "(error)"; }
                    break;
                case "Invoke":
                    var el4 = ResolveElement(row.Arg1); if (el4 == null) { preview = "(no element)"; break; }
                    try { var inv = el4.Patterns.Invoke.PatternOrDefault; if (inv != null) inv.Invoke(); else el4.Patterns.Toggle.PatternOrDefault?.Toggle(); preview = "(invoked)"; }
                    catch { preview = "(error)"; }
                    break;
                case "TakeLast":
                    var combined = ResolveString(row.Arg1, vars) ?? string.Empty;
                    var arr = combined.Split('\u001F');
                    valueToStore = arr.Length > 0 ? arr[^1] : string.Empty;
                    preview = valueToStore ?? "(null)"; break;
                case "Trim":
                    var s = ResolveString(row.Arg1, vars);
                    valueToStore = s?.Trim();
                    preview = valueToStore ?? "(null)"; break;
                case "GetValueFromSelection":
                    var el5 = ResolveElement(row.Arg1); var headerWanted = row.Arg2?.Value ?? "ID"; if (string.IsNullOrWhiteSpace(headerWanted)) headerWanted = "ID";
                    if (el5 == null) { preview = "(no element)"; break; }
                    try
                    {
                        var selection = el5.Patterns.Selection.PatternOrDefault;
                        var selected = selection?.Selection?.Value ?? Array.Empty<AutomationElement>();
                        if (selected.Length == 0)
                        {
                            selected = el5.FindAllDescendants().Where(a =>
                            {
                                try { return a.Patterns.SelectionItem.IsSupported && a.Patterns.SelectionItem.PatternOrDefault?.IsSelected == true; }
                                catch { return false; }
                            }).ToArray();
                        }
                        if (selected.Length == 0) { preview = "(no selection)"; break; }
                        var rowEl = selected[0];
                        var headers = GetHeaderTexts(el5);
                        var cells = GetRowCellValues(rowEl).Select(NormalizeKoreanMojibake).ToList();
                        if (headers.Count < cells.Count) for (int j = headers.Count; j < cells.Count; j++) headers.Add($"Col{j + 1}");
                        else if (headers.Count > cells.Count) for (int j = cells.Count; j < headers.Count; j++) cells.Add(string.Empty);
                        string? matched = null;
                        for (int j = 0; j < headers.Count; j++)
                        {
                            var hNorm = NormalizeHeader(headers[j]);
                            if (string.Equals(hNorm, headerWanted, StringComparison.OrdinalIgnoreCase)) { matched = cells[j]; break; }
                        }
                        if (matched == null)
                        {
                            for (int j = 0; j < headers.Count; j++)
                            {
                                var hNorm = NormalizeHeader(headers[j]);
                                if (hNorm.IndexOf(headerWanted, StringComparison.OrdinalIgnoreCase) >= 0) { matched = cells[j]; break; }
                            }
                        }
                        if (matched == null) { preview = $"({headerWanted} not found)"; }
                        else { valueToStore = matched; preview = matched; }
                    }
                    catch { preview = "(error)"; }
                    break;
                case "ToDateTime":
                    var s2 = ResolveString(row.Arg1, vars);
                    if (string.IsNullOrWhiteSpace(s2)) { preview = "(null)"; break; }
                    if (TryParseYmdOrYmdHms(s2.Trim(), out var dt)) { valueToStore = dt.ToString("o"); preview = dt.ToString("yyyy-MM-dd HH:mm:ss"); }
                    else { preview = "(parse fail)"; }
                    break;
                case "MouseClick":
                    // Perform a mouse click at screen coordinates (X,Y)
                    var xStr = ResolveString(row.Arg1, vars);
                    var yStr = ResolveString(row.Arg2, vars);
                    if (!int.TryParse(xStr, out var px) || !int.TryParse(yStr, out var py)) { preview = "(invalid coords)"; break; }
                    try
                    {
                        NativeMouseHelper.ClickScreen(px, py);
                        preview = $"(clicked {px},{py})";
                    }
                    catch { preview = "(error)"; }
                    break;
                default: preview = "(unsupported)"; break;
            }
            return (preview, valueToStore);
        }

        private async Task<(string preview, string? value)> ExecuteSingleAsync(ProcOpRow row, Dictionary<string, string?> vars)
        {
            if (string.Equals(row.Op, "GetTextOCR", StringComparison.OrdinalIgnoreCase))
            {
                var el = ResolveElement(row.Arg1);
                if (el == null) return ("(no element)", null);
                try
                {
                    var r = el.BoundingRectangle;
                    if (r.Width <= 0 || r.Height <= 0) return ("(no bounds)", null);
                    var hwnd = new IntPtr(el.Properties.NativeWindowHandle.Value);
                    if (hwnd == IntPtr.Zero) return ("(no hwnd)", null);
                    var (engine, text) = await Wysg.Musm.MFCUIA.OcrReader.OcrTryReadRegionDetailedAsync(hwnd, new System.Drawing.Rectangle(0, 0, (int)r.Width, (int)r.Height));
                    if (!engine) return ("(ocr unavailable)", null);
                    return (string.IsNullOrWhiteSpace(text) ? "(empty)" : text!, text);
                }
                catch { return ("(error)", null); }
            }
            if (string.Equals(row.Op, "GetHTML", StringComparison.OrdinalIgnoreCase))
            {
                var url = ResolveString(row.Arg1, vars);
                if (string.IsNullOrWhiteSpace(url)) return ("(null)", null);
                try
                {
                    var html = await HttpGetHtmlSmartAsync(url);
                    return (html ?? string.Empty, html);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[SpyWindow] GetHTML error: {ex.Message}");
                    return ("(error)", null);
                }
            }

            return ExecuteSingle(row, vars);
        }

        private void UpdateProcedureVarsFrom(List<ProcOpRow> rows)
        {
            ProcedureVars.Clear();
            foreach (var r in rows)
                if (!string.IsNullOrWhiteSpace(r.OutputVar) && !ProcedureVars.Contains(r.OutputVar)) ProcedureVars.Add(r.OutputVar);
        }

        private static string GetProcPath()
        {
            try
            {
                // Prefer per-PACS directory based on UiBookmarks store override (same folder)
                if (UiBookmarks.GetStorePathOverride != null)
                {
                    var bookmarkPath = UiBookmarks.GetStorePathOverride.Invoke();
                    if (!string.IsNullOrWhiteSpace(bookmarkPath))
                    {
                        var baseDir = System.IO.Path.GetDirectoryName(bookmarkPath);
                        if (!string.IsNullOrEmpty(baseDir))
                        {
                            System.IO.Directory.CreateDirectory(baseDir);
                            return System.IO.Path.Combine(baseDir, "ui-procedures.json");
                        }
                    }
                }

                // Next, attempt to resolve using current PACS key from tenant context
                if (System.Windows.Application.Current is Wysg.Musm.Radium.App app)
                {
                    try
                    {
                        var tenant = (Wysg.Musm.Radium.Services.ITenantContext?)app.Services.GetService(typeof(Wysg.Musm.Radium.Services.ITenantContext));
                        var pacsKey = string.IsNullOrWhiteSpace(tenant?.CurrentPacsKey) ? "default_pacs" : tenant!.CurrentPacsKey;
                        var dir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Wysg.Musm", "Radium", "Pacs", SanitizeFileName(pacsKey));
                        System.IO.Directory.CreateDirectory(dir);
                        return System.IO.Path.Combine(dir, "ui-procedures.json");
                    }
                    catch { }
                }
            }
            catch { }

            // Legacy fallback (non-PACS-scoped)
            var legacyDir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Wysg.Musm", "Radium");
            System.IO.Directory.CreateDirectory(legacyDir);
            return System.IO.Path.Combine(legacyDir, "ui-procedures.json");
        }
        private static ProcStore LoadProcStore()
        {
            try
            {
                var p = GetProcPath();
                if (!System.IO.File.Exists(p)) return new ProcStore();
                return System.Text.Json.JsonSerializer.Deserialize<ProcStore>(System.IO.File.ReadAllText(p), new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web) { WriteIndented = true }) ?? new ProcStore();
            }
            catch { return new ProcStore(); }
        }
        private static void SaveProcStore(ProcStore s)
        {
            try
            {
                var p = GetProcPath();
                System.IO.File.WriteAllText(p, System.Text.Json.JsonSerializer.Serialize(s, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web) { WriteIndented = true }));
            }
            catch { }
        }
        private static void SaveProcedureForMethod(string methodTag, List<ProcOpRow> steps)
        { var s = LoadProcStore(); s.Methods[methodTag] = steps; SaveProcStore(s); }
        private static List<ProcOpRow> LoadProcedureForMethod(string methodTag)
        { var s = LoadProcStore(); return s.Methods.TryGetValue(methodTag, out var steps) ? steps : new List<ProcOpRow>(); }

        private void OnProcMethodChanged(object? sender, SelectionChangedEventArgs e)
        {
            var cmb = (System.Windows.Controls.ComboBox?)FindName("cmbProcMethod");
            var tag = ((cmb?.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Tag as string) ?? ((cmb?.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content as string);
            if (FindName("gridProcSteps") is not System.Windows.Controls.DataGrid procGrid || string.IsNullOrWhiteSpace(tag)) return;
            var steps = LoadProcedureForMethod(tag).ToList();
            procGrid.ItemsSource = steps;
            UpdateProcedureVarsFrom(steps);
        }
        private void OnSaveProcedure(object sender, RoutedEventArgs e)
        {
            var cmb = (System.Windows.Controls.ComboBox?)FindName("cmbProcMethod");
            var procGrid = (System.Windows.Controls.DataGrid?)FindName("gridProcSteps");
            var tag = ((cmb?.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Tag as string) ?? ((cmb?.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content as string);
            if (string.IsNullOrWhiteSpace(tag)) { txtStatus.Text = "Select PACS method"; return; }
            if (procGrid == null) { txtStatus.Text = "No steps"; return; }
            try { procGrid.CommitEdit(DataGridEditingUnit.Cell, true); procGrid.CommitEdit(DataGridEditingUnit.Row, true); } catch { }
            var steps = procGrid.Items.OfType<ProcOpRow>().Where(s => !string.IsNullOrWhiteSpace(s.Op)).ToList();
            SaveProcedureForMethod(tag, steps);
            txtStatus.Text = $"Saved procedure for {tag}";
        }
        private async void OnRunProcedure(object sender, RoutedEventArgs e)
        {
            var cmb = (System.Windows.Controls.ComboBox?)FindName("cmbProcMethod");
            var procGrid = (System.Windows.Controls.DataGrid?)FindName("gridProcSteps");
            var tag = ((cmb?.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Tag as string) ?? ((cmb?.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content as string);
            if (string.IsNullOrWhiteSpace(tag)) { txtStatus.Text = "Select PACS method"; return; }
            if (procGrid == null) { txtStatus.Text = "No steps"; return; }
            try { procGrid.CommitEdit(DataGridEditingUnit.Cell, true); procGrid.CommitEdit(DataGridEditingUnit.Row, true); } catch { }
            var steps = procGrid.Items.OfType<ProcOpRow>().Where(s => !string.IsNullOrWhiteSpace(s.Op)).ToList();
            var (result, annotated) = await RunProcedureAsync(steps);
            procGrid.ItemsSource = null; procGrid.ItemsSource = annotated; UpdateProcedureVarsFrom(annotated);
            txtStatus.Text = result ?? "(null)";
        }
        private async Task<(string? result, List<ProcOpRow> annotated)> RunProcedureAsync(List<ProcOpRow> steps)
        {
            var vars = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            string? last = null; var annotated = new List<ProcOpRow>();
            for (int i = 0; i < steps.Count; i++)
            {
                var row = steps[i]; string varName = $"var{i + 1}"; row.OutputVar = varName;
                var res = NeedsAsync(row.Op)
                    ? await ExecuteSingleAsync(row, vars)
                    : ExecuteSingle(row, vars);
                vars[varName] = res.value; if (res.value != null) last = res.value; row.OutputPreview = res.preview; annotated.Add(row);
            }
            return (last, annotated);
        }

        private AutomationElement? ResolveElement(ProcArg arg)
        {
            var type = ParseArgKind(arg.Type); if (type != ArgKind.Element) return null;
            var tag = arg.Value ?? string.Empty; if (!Enum.TryParse<UiBookmarks.KnownControl>(tag, out var key)) return null;
            var tuple = UiBookmarks.Resolve(key); return tuple.element;
        }
        private static string? ResolveString(ProcArg arg, Dictionary<string, string?> vars)
        {
            var type = ParseArgKind(arg.Type);
            return type switch
            {
                ArgKind.Var => (arg.Value != null && vars.TryGetValue(arg.Value, out var v)) ? v : null,
                ArgKind.String => arg.Value,
                ArgKind.Number => arg.Value,
                _ => null
            };
        }
        private static ArgKind ParseArgKind(string? s)
        {
            if (Enum.TryParse<ArgKind>(s, true, out var k)) return k;
            return s?.Equals("Var", StringComparison.OrdinalIgnoreCase) == true ? ArgKind.Var : ArgKind.String;
        }

        private void OnOpComboPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (sender is System.Windows.Controls.ComboBox cb && !cb.IsDropDownOpen)
                {
                    e.Handled = true;
                    var cell = FindParent<System.Windows.Controls.DataGridCell>(cb);
                    var grid = FindParent<System.Windows.Controls.DataGrid>(cell) ?? (System.Windows.Controls.DataGrid?)FindName("gridProcSteps");
                    try { grid?.BeginEdit(); } catch { }
                    cb.Focus(); cb.IsDropDownOpen = true;
                }
            }
            catch { }
        }
        private void OnOpComboPreviewKeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (sender is System.Windows.Controls.ComboBox cb)
                {
                    if (e.Key == Key.F4 || (e.Key == Key.Down && (Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt) || e.Key == Key.Space)
                    {
                        var cell = FindParent<System.Windows.Controls.DataGridCell>(cb);
                        var grid = FindParent<System.Windows.Controls.DataGrid>(cell) ?? (System.Windows.Controls.DataGrid?)FindName("gridProcSteps");
                        try { grid?.BeginEdit(); } catch { }
                        cb.Focus(); cb.IsDropDownOpen = !cb.IsDropDownOpen; e.Handled = true;
                    }
                }
            }
            catch { }
        }
    }
}
