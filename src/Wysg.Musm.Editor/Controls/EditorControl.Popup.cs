// src/Wysg.Musm.Editor/Controls/EditorControl.Popup.cs
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using System;
using System.Linq;
using System.Windows.Input;
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
            // typing cancels ghosts and restarts 2s idle
            if (ServerGhosts.HasItems) ClearServerGhosts(); // also resumes idle internally
            RestartIdle(); // schedules the 2s timer; does NOT close popup now



            if (!AutoSuggestOnTyping) return;
            if (SnippetProvider is null) return;
            if (string.IsNullOrEmpty(e.Text)) return;

            var doc = Editor.Document;
            var caret = Editor.CaretOffset;

            // find current [a–zA–Z]+ to the left of the caret
            int start = caret;
            while (start > 0 && char.IsLetter(doc.GetCharAt(start - 1))) start--;
            int len = caret - start;
            string word = len > 0 ? doc.GetText(start, len) : string.Empty;

            // (1) do not rebuild if word didn’t change
            if (start == _lastWordStart && string.Equals(word, _lastWordText, StringComparison.Ordinal))
                return;

            _lastWordStart = start;
            _lastWordText = word;

            // (2) enforce min length
            if (word.Length < MinCharsForSuggest) { CloseCompletionWindow(); return; }

            // (3) fetch items (filter if needed)
            var items = SnippetProvider.GetCompletions(Editor);
            if (items is null || !items.Any()) { CloseCompletionWindow(); return; }

            // (4) create/reuse window and set REPLACE RANGE
            if (_completionWindow == null)
            {
                _completionWindow = new MusmCompletionWindow(Editor);
                _completionWindow.Closed += (_, __) => _completionWindow = null;
                _completionWindow.Show();
            }
            // ⬇️ THIS IS THE IMPORTANT LINE
            _completionWindow.StartOffset = start;

            // repopulate items
            var list = _completionWindow.CompletionList.CompletionData;
            list.Clear();
            foreach (var it in items) list.Add(it);

        }

        private void OnTextEntering(object? s, System.Windows.Input.TextCompositionEventArgs e)
        {
            if (_completionWindow != null && e.Text.Length > 0 && !char.IsLetterOrDigit(e.Text[0]))
                _completionWindow.CompletionList.RequestInsertion(e);
        }

        private void OnTextAreaPreviewKeyDown(object? s, KeyEventArgs e)
        {
            // editing keys remove ghosts
            if (ServerGhosts.HasItems && (e.Key is Key.Back or Key.Delete or Key.Enter))
                ClearServerGhosts();

            // With popup closed, arrows navigate ghosts; Tab accepts
            if (_completionWindow is null && ServerGhosts.HasItems)
            {
                if (e.Key == Key.Down)
                {
                    ServerGhosts.MoveSelection(+1);
                    InvalidateGhosts();
                    e.Handled = true;
                    return;
                }
                if (e.Key == Key.Up)
                {
                    ServerGhosts.MoveSelection(-1);
                    InvalidateGhosts();
                    e.Handled = true;
                    return;
                }
                if (e.Key == Key.Tab)
                {
                    if (AcceptSelectedServerGhost())
                        e.Handled = true;
                    return;
                }
            }

            // let Home/End/Left/Right go to the editor even if popup is open
            if (_completionWindow != null && (e.Key is Key.Home or Key.End or Key.Left or Key.Right))
                return;
        }


        private void CloseCompletionWindow()
        {
            if (_completionWindow != null)
            {
                _completionWindow.Closed -= (_, __) => _completionWindow = null; // remove anonymous
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
