using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Ude;

namespace Wysg.Musm.Radium.Views
{
    public partial class AutomationWindow
    {
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

        private static bool IndicatesKr(string? charset)
        {
            if (string.IsNullOrWhiteSpace(charset)) return false;
            var s = charset.ToLowerInvariant();
            return s.Contains("949") || s.Contains("euc-kr") || s.Contains("ks_c_5601");
        }

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
    }
}
