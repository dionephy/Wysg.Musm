using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Wysg.Musm.Editor.Snippets;

namespace Wysg.Musm.Editor.Controls
{
    public partial class EditorControl
    {
        // Idle hooks implemented in View partial
        partial void IdlePause();
        partial void IdleResume();

        // Called from View ctor
        private void InitPopup()
        {
            // no-op; event hooks are already wired in View ctor
        }

        private void CleanupPopup()
        {
            CloseCompletionWindow();
        }

        // Disable TAB inserting \t; we’ll use Tab for accept (ghost/snippet)
        partial void DisableTabInsertion()
        {
            Editor.TextArea.PreviewKeyDown += (s, e) =>
            {
                if (e.Key == Key.Tab && !e.Handled)
                {
                    // Don’t let TextArea insert \t
                    e.Handled = true;
                }
            };
        }

        // ===== Completion open/close =====
        private void ShowCompletionWindowForCurrentWord()
        {
            var seg = GetCurrentWordSegment();
            if (seg == null || seg.Length < MinCharsForSuggest)
            {
                CloseCompletionWindow();
                return;
            }

            // fetch items from provider(s)
            var items = SnippetProvider?.GetCompletions(Editor) ?? Enumerable.Empty<ICompletionData>();
            if (!items.Any())
            {
                CloseCompletionWindow();
                return;
            }

            // open window
            if (_completionWindow == null)
            {
                _completionWindow = new CompletionWindow(Editor.TextArea);
                _completionWindow.Closed += (_, __) =>
                {
                    _completionWindow = null;
                    IdleResume(); // resume idle once popup closes
                };
                IdlePause(); // pause idle while popup open
            }

            var data = _completionWindow.CompletionList.CompletionData;
            data.Clear();
            foreach (var it in items) data.Add(it);

            // do NOT auto-select first item; user chooses
            _completionWindow.CompletionList.SelectedItem = null;

            // where to insert: the segment we computed
            _completionWindow.StartOffset = seg.Offset;
            _completionWindow.EndOffset = seg.EndOffset;

            _completionWindow.Show();
        }

        private void CloseCompletionWindow()
        {
            if (_completionWindow != null)
            {
                _completionWindow.Closed -= (_, __) => { };
                _completionWindow.Close();
                _completionWindow = null;
                IdleResume();
            }
        }

        // ===== Editor events =====
        private void OnTextEntered(object? s, TextCompositionEventArgs e)
        {
            // Ignore during IME composition
            bool imeEnabled = InputMethod.GetIsInputMethodEnabled(Editor);
            var ime = InputMethod.Current;
            if (imeEnabled && ime != null && ime.ImeState == InputMethodState.On)
                return;

            if (!AutoSuggestOnTyping) return;

            // Only open on letters/digits; else close
            if (e.Text.Length == 1 && char.IsLetterOrDigit(e.Text[0]))
                ShowCompletionWindowForCurrentWord();
            else
                CloseCompletionWindow();
        }

        private void OnTextEntering(object? s, TextCompositionEventArgs e)
        {
            // If popup open and a non-alnum arrives, insert selected (if any)
            if (_completionWindow != null && e.Text.Length > 0 && !char.IsLetterOrDigit(e.Text[0]))
                _completionWindow.CompletionList.RequestInsertion(e);
        }

        // Master key handler (ghost nav + popup close/open)
        private void OnTextAreaPreviewKeyDown(object? sender, KeyEventArgs e)
        {
            // 1) Ghost navigation keys (when ghosts are showing and no popup/snippet)
            bool ghostsVisible = ServerGhosts.Items.Count > 0;
            bool popupOpen = _completionWindow?.IsVisible == true;
            bool inPlaceholder = PlaceholderModeManager.IsActive;

            if (ghostsVisible && !popupOpen && !inPlaceholder)
            {
                if (e.Key == Key.Up)
                {
                    MoveGhostSelection(-1);
                    e.Handled = true; return;
                }
                if (e.Key == Key.Down)
                {
                    MoveGhostSelection(+1);
                    e.Handled = true; return;
                }
                if (e.Key == Key.Tab)
                {
                    AcceptSelectedGhost();
                    e.Handled = true; return;
                }
                if (e.Key == Key.Escape)
                {
                    ClearServerGhosts();
                    e.Handled = true; return;
                }
            }

            // 2) Completion popup close rule: if word length < MinChars → close
            if (_completionWindow != null)
            {
                var seg = GetCurrentWordSegment();
                if (seg == null || seg.Length < MinCharsForSuggest)
                    CloseCompletionWindow();
            }

            // Let Home/End, arrows, etc. fall through to the editor normally.
        }

        // ===== Helpers =====
        private ISegment? GetCurrentWordSegment()
        {
            var doc = Editor.Document;
            int caret = Editor.CaretOffset;
            if (doc == null || caret < 0 || caret > doc.TextLength) return null;

            // Scan left to first non-alnum/underscore
            int start = caret;
            while (start > 0)
            {
                char ch = doc.GetCharAt(start - 1);
                if (!(char.IsLetterOrDigit(ch) || ch == '_')) break;
                start--;
            }
            // Scan right
            int end = caret;
            while (end < doc.TextLength)
            {
                char ch = doc.GetCharAt(end);
                if (!(char.IsLetterOrDigit(ch) || ch == '_')) break;
                end++;
            }

            int len = end - start;
            if (len <= 0) return null;
            return new SimpleSegment(start, len);
        }

        private sealed class SimpleSegment : ISegment
        {
            public SimpleSegment(int offset, int length) { Offset = offset; Length = length; }
            public int Offset { get; }
            public int Length { get; }
            public int EndOffset => Offset + Length;
        }
    }
}
