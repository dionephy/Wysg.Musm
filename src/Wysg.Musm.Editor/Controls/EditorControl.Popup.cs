using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Wysg.Musm.Editor.Completion;
using Wysg.Musm.Editor.Ui;

namespace Wysg.Musm.Editor.Controls
{
    public partial class EditorControl
    {
        private ISegment? _completionReplaceSegment;
        private WordHighlightRenderer _wordHighlightRenderer = null!;
        private CompletionGhostRenderer _completionGhostRenderer = null!;
        private string _completionGhostText = string.Empty;
        private bool _squelchServerIdleWhilePopupSelected; // gate for server-ghost timer

        // ===== Popup init/cleanup =====
        private void InitPopup()
        {
            // nothing special; bindings are wired in View ctor
            // existing init...
            this.InputBindings.Add(new KeyBinding(
                new RoutedUICommand("ForcePopup", "ForcePopup", typeof(EditorControl)),
                new KeyGesture(Key.F2)));

            this.CommandBindings.Add(new CommandBinding(
                ApplicationCommands.NotACommand,
                (s, e) => ShowCompletionForCurrentWord(),
                (s, e) => { e.CanExecute = true; }));

            Editor.TextChanged += OnEditorTextChangedForPopup;

            _wordHighlightRenderer = new WordHighlightRenderer(
                Editor.TextArea.TextView,
                () => _completionReplaceSegment);

            _completionGhostRenderer = new CompletionGhostRenderer(
                Editor.TextArea.TextView,
                () => (_completionReplaceSegment, _completionGhostText),
                () => new Typeface(Editor.FontFamily, Editor.FontStyle, Editor.FontWeight, Editor.FontStretch),
                () => Editor.FontSize);

            // keep the highlight segment fresh while popup is open
            Editor.TextArea.Caret.PositionChanged += (_, __) => { if (_completionWindow != null) UpdateReplaceSegmentAndInvalidate(); };
            Editor.TextChanged += (_, __) => { if (_completionWindow != null) UpdateReplaceSegmentAndInvalidate(); };

        }
        private void CleanupPopup()
        {
            if (_completionWindow != null)
                ClosePopupCore();
            Editor.TextChanged -= OnEditorTextChangedForPopup;
        }

        private void OnEditorTextChangedForPopup(object? s, EventArgs e)
        {
            // If the popup is open and user shortened the word, this will close it
            EvaluateAutoPopupVisibility();
        }

        // ===== Text input hooks =====
        private void OnTextEntered(object? s, TextCompositionEventArgs e)
        {
            bool imeEnabled = InputMethod.GetIsInputMethodEnabled(Editor);
            bool imeOn = InputMethod.Current?.ImeState == InputMethodState.On;
            if (imeEnabled && imeOn) return;

            if (e.Text == ";" || e.Text == ":")
            {
                ShowCompletionForCurrentWord();
                return;
            }

            if (AutoSuggestOnTyping && e.Text.Length == 1 && char.IsLetterOrDigit(e.Text[0]))
                TryOpenAutoPopupIfThresholdMet();
        }

        private void OnTextEntering(object? s, TextCompositionEventArgs e)
        {
            if (_completionWindow != null && e.Text.Length > 0 && !char.IsLetterOrDigit(e.Text[0]))
                _completionWindow.CompletionList.RequestInsertion(e);
        }

        // ===== Open/close/evaluate =====
        private void TryOpenAutoPopupIfThresholdMet()
        {
            if (_completionWindow != null) return;
            if (GetCurrentWordPrefixLength() < Math.Max(1, MinCharsForSuggest)) return;

            var items = SnippetProvider.GetCompletions(Editor);
            if (!items.Any()) return;

            items = SnippetProvider.GetCompletions(Editor);
            OpenPopupWithNoSelection(items);
        }

        private void ShowCompletionForCurrentWord()
        {
            if (_completionWindow != null)
            {
                _completionWindow.PreviewKeyDown -= OnCompletionWindowPreviewKeyDown;
                _completionWindow.Closed -= OnCompletionClosed;
                _completionWindow.Close();
                _completionWindow = null;
            }

            var items = SnippetProvider.GetCompletions(Editor);
            if (!items.Any()) return;

            items = SnippetProvider.GetCompletions(Editor);
            OpenPopupWithNoSelection(items);
        }

        private void OpenPopupWithNoSelection(IEnumerable<ICompletionData> items)
        {
            _completionWindow = MusmCompletionWindow.ShowForCurrentWord(Editor, items);

            // hook selection-changed to drive completion ghost
            _completionWindow.CompletionList.SelectionChanged += OnCompletionSelectionChanged;

            // No initial selection unless exact match (and not a snippet)
            ClearPopupSelection();
            TryAutoSelectExactMatch();

            // we highlight the replacement token whenever popup is open
            UpdateReplaceSegmentAndInvalidate();

            _completionGhostText = string.Empty;
            _completionWindow.PreviewKeyDown += OnCompletionWindowPreviewKeyDown;
            _completionWindow.Closed += OnCompletionClosed;

            // while popup has a selection → pause idle server ghosts
            _squelchServerIdleWhilePopupSelected = (_completionWindow.CompletionList.SelectedItem != null);
            if (_squelchServerIdleWhilePopupSelected) _idleTimer.Stop();
        }


        private void OnCompletionClosed(object? sender, EventArgs e)
        {
            if (_completionWindow != null)
            {
                _completionWindow.CompletionList.SelectionChanged -= OnCompletionSelectionChanged;
                _completionWindow.PreviewKeyDown -= OnCompletionWindowPreviewKeyDown;
                _completionWindow.Closed -= OnCompletionClosed;
                _completionWindow = null;
            }

            _completionGhostText = string.Empty;
            _completionReplaceSegment = null;
            Editor.TextArea.TextView.InvalidateLayer(ICSharpCode.AvalonEdit.Rendering.KnownLayer.Selection);

            _squelchServerIdleWhilePopupSelected = false;
            ResetIdleTimer(); // allow server ghosts countdown to start

        }

        // Controls/EditorControl.Popup.cs
        private void EvaluateAutoPopupVisibility()
        {
            if (_completionWindow == null) return;

            var doc = Editor.Document;
            int caret = Editor.CaretOffset;
            var line = doc.GetLineByOffset(caret);
            string lineText = doc.GetText(line);
            int local = Math.Clamp(caret - line.Offset, 0, lineText.Length);

            bool atWhitespace = local > 0 && char.IsWhiteSpace(lineText[local - 1]);
            int prefixLen = GetCurrentWordPrefixLength();

            // CLOSE if: caret is at whitespace OR the word got shorter than threshold
            if (atWhitespace || prefixLen < Math.Max(1, MinCharsForSuggest))
                ClosePopupCore();
        }



        private int GetCurrentWordPrefixLength()
        {
            var doc = Editor.Document;
            int caret = Editor.CaretOffset;
            var line = doc.GetLineByOffset(caret);
            string lineText = doc.GetText(line);
            int local = Math.Clamp(caret - line.Offset, 0, lineText.Length);
            var (startLocal, _) = Completion.WordBoundaryHelper.ComputeWordSpan(lineText, local);
            return local - startLocal;
        }

        // ===== Key routing (TextArea authority) =====
        private void OnTextAreaPreviewKeyDown(object? sender, KeyEventArgs e)
        {
            EvaluateAutoPopupVisibility();

            if (_completionWindow == null) return;

            // Up/Down navigate list
            if (e.Key == Key.Up) { MovePopupSelection(-1); e.Handled = true; return; }
            if (e.Key == Key.Down) { MovePopupSelection(+1); e.Handled = true; return; }

            // Tab: leave for ghosts or your own handler; no default
            if (e.Key == Key.Enter)
            {
                var item = _completionWindow.CompletionList.SelectedItem as ICompletionData;
                if (item != null)
                {
                    e.Handled = true;
                    AcceptSelectedFromPopup(addTrailingSpace: false);
                }
            }
            if (e.Key == Key.Space)
            {
                var item = _completionWindow.CompletionList.SelectedItem as ICompletionData;
                if (item != null)
                {
                    e.Handled = true;
                    AcceptSelectedFromPopup(addTrailingSpace: true);
                }
            }
        }

        private void OnCompletionWindowPreviewKeyDown(object? sender, KeyEventArgs e)
        {
            if (_completionWindow == null) return;

            if (e.Key == Key.Home || e.Key == Key.End)
            {
                e.Handled = true;
                bool extend = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
                bool wholeDoc = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
                MoveCaretHomeEndOnEditor(e.Key == Key.End, extend, wholeDoc);
            }
        }

        private void AcceptSelectedFromPopup(bool addTrailingSpace)
        {
            if (_completionWindow == null) return;
            var list = _completionWindow.CompletionList;
            var item = list.SelectedItem as ICompletionData;
            if (item == null) return;

            // prefer our computed segment; fall back to the window’s range
            // AcceptSelectedFromPopup(...)
            ISegment seg = _completionReplaceSegment ??
                           new TextSegment
                           {
                               StartOffset = _completionWindow.StartOffset,
                               Length = Math.Max(0, _completionWindow.EndOffset - _completionWindow.StartOffset)
                           };
            item.Complete(Editor.TextArea, seg, EventArgs.Empty);


            item.Complete(Editor.TextArea, seg, EventArgs.Empty);
            if (addTrailingSpace)
                Editor.Document.Insert(Editor.TextArea.Caret.Offset, " ");
        }


        private void MovePopupSelection(int delta)
        {
            if (_completionWindow == null) return;
            var list = _completionWindow.CompletionList;
            var data = list.CompletionData;
            if (data.Count == 0) return;

            int idx = -1;
            if (list.SelectedItem is ICompletionData sel)
            {
                for (int i = 0; i < data.Count; i++)
                    if (ReferenceEquals(data[i], sel)) { idx = i; break; }
            }

            idx = idx < 0 ? (delta > 0 ? 0 : data.Count - 1)
                          : Math.Clamp(idx + delta, 0, data.Count - 1);

            list.SelectedItem = data[idx];
            try { list.ListBox?.ScrollIntoView(list.SelectedItem); } catch { }
        }

        partial void DisableTabInsertion()
        {
            var toRemove = Editor.TextArea.InputBindings
                .OfType<KeyBinding>()
                .Where(k => k.Key == Key.Tab)
                .ToList();
            foreach (var kb in toRemove)
                Editor.TextArea.InputBindings.Remove(kb);

            void Exec(object s, ExecutedRoutedEventArgs e) { e.Handled = true; }
            void Can(object s, CanExecuteRoutedEventArgs e) { e.CanExecute = true; e.Handled = true; }

            Editor.TextArea.CommandBindings.Add(new CommandBinding(EditingCommands.TabForward, Exec, Can));
            Editor.TextArea.CommandBindings.Add(new CommandBinding(EditingCommands.TabBackward, Exec, Can));
        }

        private void MoveCaretHomeEndOnEditor(bool toEnd, bool extend, bool wholeDoc)
        {
            var doc = Editor.Document;
            int caret = Editor.CaretOffset;
            int target = wholeDoc
                ? (toEnd ? doc.TextLength : 0)
                : (toEnd ? doc.GetLineByOffset(caret).EndOffset : doc.GetLineByOffset(caret).Offset);
            ApplyCaretMoveWithSelection(target, extend);
        }

        private void ApplyCaretMoveWithSelection(int targetOffset, bool extend)
        {
            var ta = Editor.TextArea;
            int caret = ta.Caret.Offset;

            if (!extend)
            {
                Editor.SelectionLength = 0;
                Editor.CaretOffset = targetOffset;
                return;
            }

            var sel = ta.Selection;
            int anchor;
            if (sel.IsEmpty) anchor = caret;
            else
            {
                var seg = sel.SurroundingSegment;
                int segStart = seg.Offset;
                int segEnd = seg.Offset + seg.Length;
                anchor = (caret == segEnd) ? segStart : segEnd;
            }

            int start = Math.Min(anchor, targetOffset);
            int len = Math.Abs(targetOffset - anchor);
            Editor.Select(start, len);
            Editor.CaretOffset = targetOffset;
        }

        // no-op here; InlineGhost partial overrides
        private void OnDebounceTick(object? s, EventArgs e) { }
        private void ClearPopupSelection()
        {
            if (_completionWindow == null) return;
            var list = _completionWindow.CompletionList;
            list.SelectedItem = null;
            try { list.ListBox?.UnselectAll(); } catch { }
        }

        private void ClosePopupCore()
        {
            if (_completionWindow == null) return;
            _completionWindow.PreviewKeyDown -= OnCompletionWindowPreviewKeyDown;
            _completionWindow.Closed -= OnCompletionClosed;
            _completionWindow.Close();
            _completionWindow = null;
        }

        private void UpdateReplaceSegmentAndInvalidate()
        {
            _completionReplaceSegment = GetCurrentWordSegment();
            Editor.TextArea.TextView.InvalidateLayer(ICSharpCode.AvalonEdit.Rendering.KnownLayer.Selection);
        }

        private ISegment? GetCurrentWordSegment()
        {
            var doc = Editor.Document;
            int caret = Editor.CaretOffset;
            var line = doc.GetLineByOffset(caret);
            string lineText = doc.GetText(line);
            int local = Math.Clamp(caret - line.Offset, 0, lineText.Length);

            var (startLocal, endLocal) = Completion.WordBoundaryHelper.ComputeWordSpan(lineText, local);
            int start = line.Offset + startLocal;
            int length = Math.Max(0, endLocal - startLocal);
            if (length == 0) return null;
            return new TextSegment { StartOffset = start, Length = length };
        }


        private string GetCurrentWordText()
        {
            var seg = GetCurrentWordSegment();
            return seg == null ? string.Empty : Editor.Document.GetText(seg);
        }

        private void TryAutoSelectExactMatch()
        {
            if (_completionWindow == null) return;

            string word = GetCurrentWordText();
            if (string.IsNullOrWhiteSpace(word)) return;

            var list = _completionWindow.CompletionList;
            var data = list.CompletionData;
            for (int i = 0; i < data.Count; i++)
            {
                var item = data[i];
                string text = item.Text ?? item.Content?.ToString() ?? string.Empty;

                // TryAutoSelectExactMatch()
                bool isSnippet = (item is Wysg.Musm.Editor.Snippets.MusmCompletionData mm && mm.IsSnippet);
                if (isSnippet) continue;

                if (string.Equals(text, word, StringComparison.OrdinalIgnoreCase))
                {
                    list.SelectedItem = item; // auto-select
                    try { list.ListBox?.ScrollIntoView(item); } catch { }
                    _squelchServerIdleWhilePopupSelected = true;
                    _idleTimer.Stop();
                    OnCompletionSelectionChanged(list, EventArgs.Empty); // update ghost preview
                    return;
                }
            }
        }

        private void OnCompletionSelectionChanged(object? sender, EventArgs e)
        {
            if (_completionWindow == null) return;

            var sel = _completionWindow.CompletionList.SelectedItem as ICompletionData;
            _squelchServerIdleWhilePopupSelected = (sel != null);

            if (sel == null)
            {
                _completionGhostText = string.Empty;
                // no selection → allow idle timer to run for server ghosts
                ResetIdleTimer();
            }
            else
            {
                // selected → show completion ghost
                // tokens/hotkeys: preview = item.Text ; snippets: preview = item.Description (detail)
                // OnCompletionSelectionChanged(...)
                if (sel is Wysg.Musm.Editor.Snippets.MusmCompletionData md && md.IsSnippet)
                    _completionGhostText = (md.Description?.ToString() ?? "").ToString();
                else
                    _completionGhostText = sel.Text ?? sel.Content?.ToString() ?? string.Empty;


                _idleTimer.Stop(); // selected item blocks idle server ghosts
            }

            Editor.TextArea.TextView.InvalidateLayer(ICSharpCode.AvalonEdit.Rendering.KnownLayer.Selection);
        }



    }
}
