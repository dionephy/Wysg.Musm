// src/Wysk.Musm.Editor/Controls/EditorControl.Popup.cs
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using System;
using System.Linq;
using System.Windows.Input;
using Wysg.Musm.Editor.Completion;
using Wysg.Musm.Editor.Snippets; // added

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
            var line = doc.GetLineByOffset(caret);
            string lineText = doc.GetText(line);
            int local = Math.Clamp(caret - line.Offset, 0, lineText.Length);
            var (startLocal, endLocal) = WordBoundaryHelper.ComputePrefixBeforeCaret(lineText, local);
            int start = line.Offset + startLocal;
            string text = endLocal > startLocal ? lineText.Substring(startLocal, endLocal - startLocal) : string.Empty;
            return (start, text);
        }

        private void OnTextEntered(object? s, System.Windows.Input.TextCompositionEventArgs e)
        {
            if (ServerGhosts.HasItems) ClearServerGhosts();
            RestartIdle();

            if (!AutoSuggestOnTyping || SnippetProvider is null || string.IsNullOrEmpty(e.Text)) return;

            var doc = Editor.Document;
            var caret = Editor.CaretOffset;

            // Use ComputePrefixBeforeCaret to only get text from break to caret (not beyond)
            var line = doc.GetLineByOffset(caret);
            string lineText = doc.GetText(line);
            int local = Math.Clamp(caret - line.Offset, 0, lineText.Length);
            var (startLocal, endLocal) = WordBoundaryHelper.ComputePrefixBeforeCaret(lineText, local);
            int start = line.Offset + startLocal;
            string word = endLocal > startLocal ? lineText.Substring(startLocal, endLocal - startLocal) : string.Empty;

            if (start == _lastWordStart && string.Equals(word, _lastWordText, StringComparison.Ordinal))
                return;

            _lastWordStart = start;
            _lastWordText = word;

            if (word.Length == 0 || word.Length < MinCharsForSuggest)
            {
                CloseCompletionWindow();
                return;
            }

            if (word.All(char.IsDigit))
            {
                CloseCompletionWindow();
                return;
            }

            var items = SnippetProvider.GetCompletions(Editor);
            if (items is null || !items.Any())
            { 
                CloseCompletionWindow(); 
                return; 
            }

            var itemsList = items.ToList();

            if (_completionWindow == null)
            {
                _completionWindow = new MusmCompletionWindow(Editor);
                _completionWindow.Closed += (_, __) => _completionWindow = null;
                _completionWindow.Show();
            }
            
            _completionWindow.StartOffset = start;

            var list = _completionWindow.CompletionList.CompletionData;
            list.Clear();

            // Adjust priorities BEFORE adding to list, then sort by priority (desc) and text (asc)
            foreach (var it in itemsList)
            {
                if (it is MusmCompletionData mcd)
                {
                    mcd.AdjustPriorityForInput(word);
                }
            }

            // Sort by priority (descending) then by text (ascending) to ensure proper ordering
            var sortedItems = itemsList
                .OrderByDescending(it => it is MusmCompletionData mcd ? mcd.Priority : 0.0)
                .ThenBy(it => it.Text, StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var it in sortedItems)
            {
                list.Add(it);
            }

            _completionWindow.AdjustListBoxHeight();

            // Default: select first item to match common UX; Down moves to second
            _completionWindow.SelectExactOrNone(word);
        }

        private void OnTextEntering(object? s, System.Windows.Input.TextCompositionEventArgs e)
        {
            // FIXED: Delete selection before inserting character (normal text editor behavior)
            if (!string.IsNullOrEmpty(e.Text))
            {
                var selection = Editor.TextArea?.Selection;
                if (selection != null && !selection.IsEmpty)
                {
                    var segment = selection.SurroundingSegment;
                    if (segment != null && segment.Length > 0)
                    {
                        // Delete selected text
                        Editor.Document.Remove(segment.Offset, segment.Length);
                        Editor.CaretOffset = segment.Offset;
                        // Clear selection
                        Editor.TextArea.ClearSelection();
                    }
                }
            }

            if (_completionWindow != null && e.Text.Length > 0)
            {
                char firstChar = e.Text[0];
                if (firstChar == '/' || firstChar == '-')
                {
                    return; // do not commit completion on slash or dash
                }
                if (!char.IsLetterOrDigit(firstChar))
                {
                    bool isWhitespace = char.IsWhiteSpace(firstChar);
                    string trailingText = e.Text;

                    _completionWindow.CompletionList.RequestInsertion(e);
                    e.Handled = true; // guarantee the non-alnum char is handled by us

                    if (!isWhitespace && Editor.Document != null && trailingText.Length > 0)
                    {
                        // Preserve punctuation (.,;: etc.) for any completion commit
                        int caret = Editor.CaretOffset;
                        Editor.Document.Insert(caret, trailingText);
                        Editor.CaretOffset = caret + trailingText.Length;
                    }

                    return;
                }
            }
        }

        private void OnTextAreaPreviewKeyDown(object? s, KeyEventArgs e)
        {
            // Allow Alt+Arrow keys to pass through for editor-level navigation
            if (e.KeyboardDevice.Modifiers == ModifierKeys.Alt &&
                (e.Key == Key.Up || e.Key == Key.Down || e.Key == Key.Left || e.Key == Key.Right ||
                 e.SystemKey == Key.Up || e.SystemKey == Key.Down || e.SystemKey == Key.Left || e.SystemKey == Key.Right))
            {
                // Do NOT handle the event - let it bubble to editor navigation handlers
                return;
            }

            if (ServerGhosts.HasItems && (e.Key is Key.Back or Key.Delete or Key.Enter or Key.Return))
                ClearServerGhosts();

            // Commit completion on Enter when popup is open and an item is selected (cancel raw newline)
            if (e.Key is Key.Enter or Key.Return)
            {
                bool hasSelection = _completionWindow?.CompletionList?.ListBox?.SelectedIndex >= 0;
                if (_completionWindow != null && hasSelection)
                {
                    e.Handled = true;
                    _completionWindow.CompletionList.RequestInsertion(e);
                    return;
                }

                bool noSelection = _completionWindow?.CompletionList?.ListBox?.SelectedIndex == -1;
                if (_completionWindow == null || noSelection)
                {
                    // If snippet mode is active, allow SnippetInputHandler to process Enter
                    if (PlaceholderModeManager.IsActive)
                    {
                        return; // do not handle here
                    }
                    e.Handled = true;
                    CloseCompletionWindow();
                    
                    // FIXED: Check if there's a selection and delete it before inserting newline
                    var selection = Editor.TextArea?.Selection;
                    int insertOffset = Editor.CaretOffset;
                    
                    if (selection != null && !selection.IsEmpty)
                    {
                        var segment = selection.SurroundingSegment;
                        if (segment != null && segment.Length > 0)
                        {
                            // Delete selected text
                            Editor.Document.Remove(segment.Offset, segment.Length);
                            insertOffset = segment.Offset;
                            // Clear selection
                            Editor.TextArea.ClearSelection();
                        }
                    }
                    
                    // Insert newline at the correct position
                    var nl = Environment.NewLine;
                    Editor.Document.Insert(insertOffset, nl);
                    Editor.CaretOffset = insertOffset + nl.Length;
                    return;
                }
            }

            // Space commits selection (and cancels the space), otherwise inserts a space if popup has no selection
            if (e.Key == Key.Space)
            {
                if (_completionWindow != null)
                {
                    e.Handled = true;
                    bool hasSelection = _completionWindow.CompletionList?.ListBox?.SelectedIndex >= 0;
                    if (hasSelection)
                    {
                        _completionWindow.CompletionList.RequestInsertion(e);
                    }
                    else
                    {
                        // no selection: insert literal space and close popup
                        CloseCompletionWindow();
                        
                        // FIXED: Check if there's a selection and delete it before inserting space
                        var selection = Editor.TextArea?.Selection;
                        int insertOffset = Editor.CaretOffset;
                        
                        if (selection != null && !selection.IsEmpty)
                        {
                            var segment = selection.SurroundingSegment;
                            if (segment != null && segment.Length > 0)
                            {
                                // Delete selected text
                                Editor.Document.Remove(segment.Offset, segment.Length);
                                insertOffset = segment.Offset;
                                // Clear selection
                                Editor.TextArea.ClearSelection();
                            }
                        }
                        
                        // Insert space at the correct position
                        Editor.Document.Insert(insertOffset, " ");
                        Editor.CaretOffset = insertOffset + 1;
                    }
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
                // Note: cannot reliably remove the exact anonymous handler; just close and null
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
                {
                    // When snippet mode is active, let SnippetInputHandler process Tab
                    if (PlaceholderModeManager.IsActive)
                        return; // do not mark handled
                    e.Handled = true; // otherwise block raw tab insertion
                }
            };
        }
    }
}
