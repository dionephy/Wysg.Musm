// src/Wysg.Musm.Editor/Extensions/TextEditorExtensions.cs
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;

namespace Wysg.Musm.Editor.Extensions;

internal static class TextEditorExtensions
{
    public static (string word, int startOffset) GetWordBeforeCaret(this TextEditor editor)
        => editor.Document.GetWordBeforeOffset(editor.CaretOffset);

    public static string GetLineBeforeCaret(this TextEditor editor)
        => editor.Document.GetLineBeforeOffset(editor.CaretOffset);
}
