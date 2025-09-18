using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

#if WINDOWS_OCR
using Windows.Globalization;
using Windows.Media.Ocr;
using Windows.Graphics.Imaging;
using System.Runtime.InteropServices.WindowsRuntime;
#endif

namespace Wysg.Musm.MFCUIA;

public static class OcrReader
{
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
    [DllImport("user32.dll")] private static extern IntPtr GetDC(IntPtr hWnd);
    [DllImport("gdi32.dll")] private static extern bool BitBlt(IntPtr hdc, int x, int y, int cx, int cy, IntPtr hdcSrc, int x1, int y1, int rop);
    [DllImport("user32.dll")] private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);
    [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleDC(IntPtr hdc);
    [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);
    [DllImport("gdi32.dll")] private static extern IntPtr SelectObject(IntPtr hdc, IntPtr h);
    [DllImport("gdi32.dll")] private static extern bool DeleteObject(IntPtr hObject);
    [DllImport("gdi32.dll")] private static extern bool DeleteDC(IntPtr hdc);

    private const int SRCCOPY = 0x00CC0020;

    [StructLayout(LayoutKind.Sequential)] private struct RECT { public int Left, Top, Right, Bottom; }

    public static bool TryGetWindowRect(IntPtr hwnd, out Rectangle rect)
    {
        rect = Rectangle.Empty;
        if (hwnd == IntPtr.Zero) return false;
        if (!GetWindowRect(hwnd, out var r)) return false;
        rect = new Rectangle(r.Left, r.Top, Math.Max(0, r.Right - r.Left), Math.Max(0, r.Bottom - r.Top));
        return true;
    }

    public static Bitmap? CaptureTopStrip(IntPtr hwnd, int topStripPx = 160)
    {
        if (!TryGetWindowRect(hwnd, out var r)) return null;
        return CaptureRegion(hwnd, new Rectangle(0, 0, r.Width, Math.Min(topStripPx, r.Height)));
    }

    public static Bitmap? CaptureRegion(IntPtr hwnd, Rectangle crop)
    {
        if (!TryGetWindowRect(hwnd, out var wr)) return null;
        if (crop.Width <= 0 || crop.Height <= 0) return null;
        // Clamp crop to window bounds
        int x = Math.Max(0, crop.X);
        int y = Math.Max(0, crop.Y);
        int w = Math.Min(crop.Width, Math.Max(0, wr.Width - x));
        int h = Math.Min(crop.Height, Math.Max(0, wr.Height - y));
        if (w <= 0 || h <= 0) return null;

        IntPtr screenDc = IntPtr.Zero, memDc = IntPtr.Zero, bmp = IntPtr.Zero, old = IntPtr.Zero;
        try
        {
            screenDc = GetDC(IntPtr.Zero);
            memDc = CreateCompatibleDC(screenDc);
            bmp = CreateCompatibleBitmap(screenDc, w, h);
            old = SelectObject(memDc, bmp);
            _ = BitBlt(memDc, 0, 0, w, h, screenDc, wr.Left + x, wr.Top + y, SRCCOPY);
            var image = Image.FromHbitmap(bmp);
            return new Bitmap(image);
        }
        finally
        {
            if (old != IntPtr.Zero) SelectObject(memDc, old);
            if (bmp != IntPtr.Zero) DeleteObject(bmp);
            if (memDc != IntPtr.Zero) DeleteDC(memDc);
            if (screenDc != IntPtr.Zero) ReleaseDC(IntPtr.Zero, screenDc);
        }
    }

#if WINDOWS_OCR
    private static Bitmap PreprocessForOcr(Bitmap src)
    {
        float scale = 1.5f;
        int w = Math.Max(1, (int)(src.Width * scale));
        int h = Math.Max(1, (int)(src.Height * scale));
        var scaled = new Bitmap(w, h, PixelFormat.Format24bppRgb);
        using (var g = Graphics.FromImage(scaled))
        {
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
            g.DrawImage(src, new Rectangle(0, 0, w, h));
        }

        var gray = new Bitmap(w, h, PixelFormat.Format24bppRgb);
        using (var g = Graphics.FromImage(gray))
        {
            var cm = new ColorMatrix(new float[] []
            {
                new float[] {0.299f, 0.299f, 0.299f, 0f, 0f},
                new float[] {0.587f, 0.587f, 0.587f, 0f, 0f},
                new float[] {0.114f, 0.114f, 0.114f, 0f, 0f},
                new float[] {0f,      0f,      0f,      1f, 0f},
                new float[] {0f,      0f,      0f,      0f, 1f}
            });
            using var ia = new ImageAttributes();
            ia.SetColorMatrix(cm);
            g.DrawImage(scaled, new Rectangle(0, 0, w, h), 0, 0, w, h, GraphicsUnit.Pixel, ia);
        }
        scaled.Dispose();

        var thresh = new Bitmap(w, h, PixelFormat.Format24bppRgb);
        var data = gray.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, gray.PixelFormat);
        var outData = thresh.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, thresh.PixelFormat);
        unsafe
        {
            byte* srcPtr = (byte*)data.Scan0;
            byte* dstPtr = (byte*)outData.Scan0;
            int stride = data.Stride;
            int oStride = outData.Stride;
            byte T = 170;
            for (int y = 0; y < h; y++)
            {
                byte* srow = srcPtr + y * stride;
                byte* drow = dstPtr + y * oStride;
                for (int x = 0; x < w; x++)
                {
                    byte b = srow[x * 3 + 0];
                    byte g2 = srow[x * 3 + 1];
                    byte r = srow[x * 3 + 2];
                    byte lum = (byte)((r + g2 + b) / 3);
                    byte v = lum > T ? (byte)255 : (byte)0;
                    drow[x * 3 + 0] = v; drow[x * 3 + 1] = v; drow[x * 3 + 2] = v;
                }
            }
        }
        gray.UnlockBits(data);
        thresh.UnlockBits(outData);
        gray.Dispose();
        return thresh;
    }

    private static string NormalizeArtifacts(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        // Quotes and punctuation
        s = s.Replace('¡Ç', ':').Replace('£§', '\'')
             .Replace('¡°', '"').Replace('¡±', '"')
             .Replace('¡®', '\'')
             .Replace('£ü', ':').Replace('|', ':');
        // Context-specific: ¡º between letters -> r; otherwise map to parentheses to retain study names
        s = Regex.Replace(s, "([A-Za-z°¡-ÆR])¡º([A-Za-z°¡-ÆR])", "$1r$2");
        s = s.Replace('¡º', '(').Replace('¡»', ')').Replace('¡²', '(').Replace('¡³', ')');
        // Korean 'ÀÌ' between digits -> 2 (time misread)
        s = Regex.Replace(s, @"(?<=\d)ÀÌ(?=\d)", "2");
        // Collapse spaces
        s = Regex.Replace(s, "\\s+", " ");
        // Normalize datetime separators
        s = Regex.Replace(s, @"(\d{4})-(\d{2})-(\d{2})\s+(\d{1,2})\D(\d{2})\D(\d{2})", m =>
            $"{m.Groups[1].Value}-{m.Groups[2].Value}-{m.Groups[3].Value} {m.Groups[4].Value.PadLeft(2,'0')}:{m.Groups[5].Value}:{m.Groups[6].Value}");
        return s.Trim();
    }

    private static async Task<string?> RunOcrAsync(Bitmap bmp)
    {
        using var ms = new MemoryStream();
        bmp.Save(ms, ImageFormat.Png);
        ms.Position = 0;
        var ras = ms.AsRandomAccessStream();
        var decoder = await BitmapDecoder.CreateAsync(ras);
        var swBmp = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);

        OcrEngine? ocr = null;
        try { ocr = OcrEngine.TryCreateFromLanguage(new Language("ko")); } catch { }
        if (ocr == null) { try { ocr = OcrEngine.TryCreateFromLanguage(new Language("en")); } catch { } }
        ocr ??= OcrEngine.TryCreateFromUserProfileLanguages();
        if (ocr == null) return null;

        var result = await ocr.RecognizeAsync(swBmp);
        if (result == null) return null;

        string? best = null; int bestScore = int.MinValue;
        foreach (var line in result.Lines)
        {
            var t = line?.Text?.Trim();
            if (string.IsNullOrEmpty(t)) continue;
            int commas = 0; foreach (var ch in t) if (ch == ',') commas++;
            bool hasDigit = Regex.IsMatch(t, @"\d");
            bool hasLetters = Regex.IsMatch(t, @"[A-Za-z°¡-ÆR]");
            int score = t.Length + commas * 5 + (hasDigit ? 10 : 0) + (hasLetters ? 8 : 0);
            if (score > bestScore) { bestScore = score; best = t; }
        }
        best ??= result.Text?.Trim();
        return NormalizeArtifacts(best ?? string.Empty);
    }
#endif

    // Legacy wrapper (kept for compatibility)
    public static async Task<string?> OcrTopStripAsync(IntPtr hwnd, int topStripPx = 160)
    {
        var (engineAvailable, text) = await OcrTryReadTopStripDetailedAsync(hwnd, topStripPx);
        return engineAvailable ? text : null;
    }

    public static async Task<(bool engineAvailable, string? text)> OcrTryReadTopStripDetailedAsync(IntPtr hwnd, int topStripPx = 160)
    {
#if WINDOWS_OCR
        using var raw = CaptureTopStrip(hwnd, topStripPx);
        if (raw == null) return (true, null);
        using var bmp = PreprocessForOcr(raw);
        var text = await RunOcrAsync(bmp);
        return (text != null, text);
#else
        await Task.CompletedTask;
        return (false, null);
#endif
    }

    public static async Task<(bool engineAvailable, string? text)> OcrTryReadRegionDetailedAsync(IntPtr hwnd, Rectangle crop)
    {
#if WINDOWS_OCR
        using var raw = CaptureRegion(hwnd, crop);
        if (raw == null) return (true, null);
        using var bmp = PreprocessForOcr(raw);
        var text = await RunOcrAsync(bmp);
        return (text != null, text);
#else
        await Task.CompletedTask;
        return (false, null);
#endif
    }
}
