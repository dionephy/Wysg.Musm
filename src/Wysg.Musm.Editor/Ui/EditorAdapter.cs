using System;
using ICSharpCode.AvalonEdit;

namespace Wysg.Musm.Editor.Ui;

public sealed class EditorAdapter
{
    private readonly TextEditor _editor;
    public event EventHandler? TextChanged;
    public event EventHandler? CaretMoved;

    public EditorAdapter(TextEditor editor)
    {
        _editor = editor;
        _editor.TextChanged += (_, __) => TextChanged?.Invoke(this, EventArgs.Empty);
        _editor.TextArea.Caret.PositionChanged += (_, __) => CaretMoved?.Invoke(this, EventArgs.Empty);
    }

    public (string left, string right) GetContextWindows(int leftChars, int rightChars)
    {
        var doc = _editor.Document;
        var caret = _editor.CaretOffset;

        var leftStart = Math.Max(0, caret - leftChars);
        var left = doc.GetText(leftStart, caret - leftStart);

        var rightLen = Math.Min(rightChars, doc.TextLength - caret);
        var right = doc.GetText(caret, rightLen);

        return (left, right);
    }
}
