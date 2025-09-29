// src/Wysg.Musm.Editor/Controls/EditorControl.Popup.cs
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
        private bool _hasUserNavigated = false; // Track if user has used Up/Down navigation

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

            Debug.WriteLine($"[Popup] word='{word}' start={start} lastStart={_lastWordStart} lastWord='{_lastWordText}'");

            // (1) do not rebuild if word didn't change
            if (start == _lastWordStart && string.Equals(word, _lastWordText, StringComparison.Ordinal))
                return;

            _lastWordStart = start;
            _lastWordText = word;

            // (2) enforce min length and close when empty
            if (word.Length == 0) { Debug.WriteLine("[Popup] close: empty word"); CloseCompletionWindow(); return; }
            if (word.Length < MinCharsForSuggest) { Debug.WriteLine("[Popup] close: below MinCharsForSuggest"); CloseCompletionWindow(); return; }

            // (3) fetch items (filter if needed)
            var items = SnippetProvider.GetCompletions(Editor);
            if (items is null || !items.Any()) { Debug.WriteLine("[Popup] close: no items"); CloseCompletionWindow(); return; }

            // (4) create/reuse window and set REPLACE RANGE
            if (_completionWindow == null)
            {
                _completionWindow = new MusmCompletionWindow(Editor);
                _completionWindow.Closed += (_, __) => _completionWindow = null;
                _completionWindow.Show();
                _hasUserNavigated = false; // Reset navigation flag for new window
                Debug.WriteLine("[Popup] open completion window");
            }
            _completionWindow.StartOffset = start;
            Debug.WriteLine($"[Popup] set StartOffset={start}");

            // repopulate items
            var list = _completionWindow.CompletionList.CompletionData;
            list.Clear();
            foreach (var it in items) list.Add(it);
            Debug.WriteLine($"[Popup] items count={list.Count}");

            // exact-match-only selection
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
            int sel = _completionWindow?.CompletionList?.ListBox?.SelectedIndex ?? -2; // -2 means no window
            int startOff = _completionWindow?.StartOffset ?? -1;
            int endOff = _completionWindow?.EndOffset ?? -1;
            Debug.WriteLine($"[Popup] PKD key={e.Key} popup={( _completionWindow!=null)} sel={sel} caret={caret} range=[{startOff},{endOff}]");

            // editing keys remove ghosts
            if (ServerGhosts.HasItems && (e.Key is Key.Back or Key.Delete or Key.Enter or Key.Return))
                ClearServerGhosts();

            // ENTER: guarantee single-press newline when no selection or popup closed
            if (e.Key is Key.Enter or Key.Return)
            {
                bool noSelection = _completionWindow?.CompletionList?.ListBox?.SelectedIndex == -1;
                if (_completionWindow == null || noSelection)
                {
                    Debug.WriteLine("[Popup] ENTER: force newline");
                    e.Handled = true;
                    CloseCompletionWindow();
                    var off = Editor.CaretOffset;
                    var nl = Environment.NewLine;
                    Editor.Document.Insert(off, nl);
                    Editor.CaretOffset = off + nl.Length;
                    return;
                }
            }

            // If completion is open, handle Home/End by moving caret and closing based on range
            if (_completionWindow != null)
            {
                if (e.Key == Key.Home)
                {
                    int rangeStart = _completionWindow.StartOffset;
                    int rangeEnd = _completionWindow.EndOffset;
                    var line = Editor.Document.GetLineByOffset(Editor.CaretOffset);
                    int newOff = line.Offset;
                    Debug.WriteLine($"[Popup] HOME move caret -> {newOff}, range=[{rangeStart},{rangeEnd}]");
                    e.Handled = true;
                    Editor.CaretOffset = newOff;
                    if (newOff < rangeStart || newOff > rangeEnd)
                    {
                        Debug.WriteLine("[Popup] HOME outside range -> close");
                        _lastWordStart = -1; // clear highlight
                        CloseCompletionWindow();
                    }
                    return;
                }
                if (e.Key == Key.End)
                {
                    int rangeStart = _completionWindow.StartOffset;
                    int rangeEnd = _completionWindow.EndOffset;
                    var line = Editor.Document.GetLineByOffset(Editor.CaretOffset);
                    int newOff = line.EndOffset;
                    Debug.WriteLine($"[Popup] END move caret -> {newOff}, range=[{rangeStart},{rangeEnd}]");
                    e.Handled = true;
                    Editor.CaretOffset = newOff;
                    if (newOff < rangeStart || newOff > rangeEnd)
                    {
                        Debug.WriteLine("[Popup] END outside range -> close");
                        _lastWordStart = -1; // clear highlight
                        CloseCompletionWindow();
                    }
                    return;
                }
            }

            // Close completion when current word becomes empty (e.g., after Backspace/Space/Delete)
            if (_completionWindow != null && (e.Key is Key.Back or Key.Delete or Key.Space))
            {
                var (start, word) = GetCurrentWord(Editor.Document, Editor.CaretOffset);
                Debug.WriteLine($"[Popup] after key word='{word}' start={start}");
                if (string.IsNullOrEmpty(word)) { Debug.WriteLine("[Popup] close due to empty after key"); CloseCompletionWindow(); return; }
            }

            // Ghost navigation when popup closed
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

            if (_completionWindow != null && (e.Key is Key.Left or Key.Right))
            {
                var (start, word) = GetCurrentWord(Editor.Document, Editor.CaretOffset);
                Debug.WriteLine($"[Popup] Left/Right word='{word}' start={start}");
                if (string.IsNullOrEmpty(word)) { Debug.WriteLine("[Popup] close due to empty on LR"); CloseCompletionWindow(); return; }
                _completionWindow.SelectExactOrNone(word);
                return;
            }

            if (_completionWindow != null && (e.Key is Key.Up or Key.Down))
            {
                var lb = _completionWindow.CompletionList?.ListBox;
                if (lb != null)
                {
                    Debug.WriteLine($"[Popup] Up/Down navigation: current selection={lb.SelectedIndex}, items={lb.Items.Count}, hasUserNavigated={_hasUserNavigated}");
                    
                    if (!_hasUserNavigated)
                    {
                        // first navigation → select first (Down) or last (Up) immediately
                        Debug.WriteLine("[Popup] First navigation: allowing selection");
                        _completionWindow.AllowSelectionByKeyboardOnce();
                        _hasUserNavigated = true; // Mark that user has now navigated
                        
                        // Give the ListBox focus to ensure keyboard events work properly
                        if (!lb.IsFocused)
                        {
                            lb.Focus();
                            Debug.WriteLine("[Popup] Giving ListBox focus");
                        }
                        
                        if (e.Key == Key.Down) 
                            lb.SelectedIndex = 0; 
                        else 
                            lb.SelectedIndex = lb.Items.Count - 1;
                        e.Handled = true; // prevent caret move
                        Debug.WriteLine($"[Popup] First navigation: set selection to {lb.SelectedIndex}");
                        return;
                    }
                    
                    // subsequent navigation: allow one selection change per key and let ListBox handle it
                    Debug.WriteLine("[Popup] Subsequent navigation: allowing selection change");
                    _completionWindow.AllowSelectionByKeyboardOnce();
                    
                    // Ensure ListBox has focus
                    if (!lb.IsFocused)
                    {
                        lb.Focus();
                        Debug.WriteLine("[Popup] Giving ListBox focus for navigation");
                    }
                    
                    // Let the ListBox handle the navigation naturally
                    if (e.Key == Key.Down && lb.SelectedIndex < lb.Items.Count - 1)
                    {
                        lb.SelectedIndex++;
                        e.Handled = true;
                        Debug.WriteLine($"[Popup] Down navigation: moved to {lb.SelectedIndex}");
                    }
                    else if (e.Key == Key.Up && lb.SelectedIndex > 0)
                    {
                        lb.SelectedIndex--;
                        e.Handled = true;
                        Debug.WriteLine($"[Popup] Up navigation: moved to {lb.SelectedIndex}");
                    }
                    else
                    {
                        e.Handled = true; // Prevent further processing even if at boundaries
                        Debug.WriteLine($"[Popup] Navigation at boundary: {e.Key}, index={lb.SelectedIndex}");
                    }
                }
                return;
            }
        }

        private void CloseCompletionWindow()
        {
            if (_completionWindow != null)
            {
                Debug.WriteLine("[Popup] close window");
                _completionWindow.Closed -= (_, __) => _completionWindow = null; // remove anonymous
                _completionWindow.Close();
                _completionWindow = null;
                _hasUserNavigated = false; // Reset navigation flag when closing
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
