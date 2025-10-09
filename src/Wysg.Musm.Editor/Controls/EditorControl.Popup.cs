// src/Wysk.Musm.Editor/Controls/EditorControl.Popup.cs
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using System;
using System.Linq;
using System.Windows.Input;
using System.Diagnostics;
using Wysg.Musm.Editor.Completion;

namespace Wysg.Musm.Editor.Controls
{
    public partial class EditorControl
    {
        private int _lastWordStart = -1;
        private string _lastWordText = string.Empty;
        private Ui.CurrentWordHighlighter? _wordHi;

        private static (int start, string text) GetCurrentWord(TextDocument doc, int caret)
        {
            if (doc is null) return (caret, string.Empty);
            int start = caret;
            while (start > 0)
            {
                char ch = doc.GetCharAt(start - 1);
                if (!char.IsLetter(ch)) break;
                start--;
            }
            int end = caret;
            int len = end - start;
            string text = len > 0 ? doc.GetText(start, len) : string.Empty;
            return (start, text);
        }

        private void OnTextEntered(object? s, System.Windows.Input.TextCompositionEventArgs e)
        {
            Debug.WriteLine($"[Popup] TextEntered: '{e.Text}' caret={Editor.CaretOffset}");
            if (ServerGhosts.HasItems) ClearServerGhosts();
            RestartIdle();

            if (!AutoSuggestOnTyping || SnippetProvider is null || string.IsNullOrEmpty(e.Text)) return;

            var doc = Editor.Document;
            var caret = Editor.CaretOffset;

            int start = caret;
            while (start > 0 && char.IsLetter(doc.GetCharAt(start - 1))) start--;
            int len = caret - start;
            string word = len > 0 ? doc.GetText(start, len) : string.Empty;

            if (start == _lastWordStart && string.Equals(word, _lastWordText, StringComparison.Ordinal))
                return;

            _lastWordStart = start;
            _lastWordText = word;

            if (word.Length == 0 || word.Length < MinCharsForSuggest) { CloseCompletionWindow(); return; }

            var items = SnippetProvider.GetCompletions(Editor);
            if (items is null || !items.Any()) { CloseCompletionWindow(); return; }

            if (_completionWindow == null)
            {
                _completionWindow = new MusmCompletionWindow(Editor);
                _completionWindow.Closed += (_, __) => _completionWindow = null;
                _completionWindow.Show();
            }
            _completionWindow.StartOffset = start;

            var list = _completionWindow.CompletionList.CompletionData;
            list.Clear();
            foreach (var it in items) list.Add(it);

            _completionWindow.AdjustListBoxHeight();

            // Default: select first item to match common UX; Down moves to second
            _completionWindow.SelectExactOrNone(word);
        }

        private void OnTextEntering(object? s, System.Windows.Input.TextCompositionEventArgs e)
        {
            if (_completionWindow != null && e.Text.Length > 0 && !char.IsLetterOrDigit(e.Text[0]))
                _completionWindow.CompletionList.RequestInsertion(e);
        }

        private void OnTextAreaPreviewKeyDown(object? s, KeyEventArgs e)
        {
            int caret = Editor.CaretOffset;
            int sel = _completionWindow?.CompletionList?.ListBox?.SelectedIndex ?? -2;
            int startOff = _completionWindow?.StartOffset ?? -1;
            int endOff = _completionWindow?.EndOffset ?? -1;
            Debug.WriteLine($"[Popup] PKD key={e.Key} popup={( _completionWindow!=null)} sel={sel} caret={caret} range=[{startOff},{endOff}]");

            if (ServerGhosts.HasItems && (e.Key is Key.Back or Key.Delete or Key.Enter or Key.Return))
                ClearServerGhosts();

            if (e.Key is Key.Enter or Key.Return)
            {
                bool noSelection = _completionWindow?.CompletionList?.ListBox?.SelectedIndex == -1;
                if (_completionWindow == null || noSelection)
                {
                    e.Handled = true;
                    CloseCompletionWindow();
                    var off = Editor.CaretOffset;
                    var nl = Environment.NewLine;
                    Editor.Document.Insert(off, nl);
                    Editor.CaretOffset = off + nl.Length;
                    return;
                }
            }

            if (_completionWindow != null)
            {
                if (e.Key == Key.Home)
                {
                    int rangeStart = _completionWindow.StartOffset;
                    int rangeEnd = _completionWindow.EndOffset;
                    var line = Editor.Document.GetLineByOffset(Editor.CaretOffset);
                    int newOff = line.Offset;
                    e.Handled = true;
                    Editor.CaretOffset = newOff;
                    if (newOff < rangeStart || newOff > rangeEnd) { _lastWordStart = -1; CloseCompletionWindow(); }
                    return;
                }
                if (e.Key == Key.End)
                {
                    int rangeStart = _completionWindow.StartOffset;
                    int rangeEnd = _completionWindow.EndOffset;
                    var line = Editor.Document.GetLineByOffset(Editor.CaretOffset);
                    int newOff = line.EndOffset;
                    e.Handled = true;
                    Editor.CaretOffset = newOff;
                    if (newOff < rangeStart || newOff > rangeEnd) { _lastWordStart = -1; CloseCompletionWindow(); }
                    return;
                }
            }

            if (_completionWindow != null && (e.Key is Key.Left or Key.Right))
            {
                var (start, word) = GetCurrentWord(Editor.Document, Editor.CaretOffset);
                if (string.IsNullOrEmpty(word)) { CloseCompletionWindow(); return; }
                _completionWindow.SelectExactOrNone(word);
                return;
            }
        }

        private void CloseCompletionWindow()
        {
            if (_completionWindow != null)
            {
                _completionWindow.Closed -= (_, __) => _completionWindow = null;
                _completionWindow.Close();
                _completionWindow = null;
            }
        }

        // prevent raw \t insert (tab is reserved)
        partial void DisableTabInsertion()
        {
            Editor.TextArea.PreviewKeyDown += (_, e) =>
            {
                if (e.Key == Key.Tab && (Keyboard.Modifiers == ModifierKeys.None))
                    e.Handled = true;
            };
        }
    }
}
