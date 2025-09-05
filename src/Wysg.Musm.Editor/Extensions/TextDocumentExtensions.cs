// src/Wysg.Musm.Editor/Extensions/TextDocumentExtensions.cs
using ICSharpCode.AvalonEdit.Document;

namespace Wysg.Musm.Editor.Extensions;

internal static class TextDocumentExtensions
{
    public static (string word, int startOffset) GetWordBeforeOffset(this TextDocument doc, int offset)
    {
        if (offset <= 0) return (string.Empty, offset);
        var line = doc.GetLineByOffset(offset);
        var text = doc.GetText(line);
        int local = Math.Min(text.Length, offset - line.Offset) - 1;
        while (local >= 0 && !char.IsWhiteSpace(text[local])) local--;
        int start = line.Offset + local + 1;
        return (doc.GetText(start, offset - start), start);
    }

    public static string GetLineBeforeOffset(this TextDocument doc, int offset)
    {
        if (offset <= 0) return string.Empty;
        var line = doc.GetLineByOffset(offset);
        return doc.GetText(line.Offset, Math.Max(0, offset - line.Offset));
    }
}
