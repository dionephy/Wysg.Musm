﻿// src/Wysk.Musm.Editor/Controls/EditorControl.Popup.cs
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using System;
using System.Linq;
using System.Windows.Input;
using System.Diagnostics;
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

        // Adjust priorities BEFORE adding to list, then sort by priority (desc) and text (asc)
        var itemsList = items.ToList();
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
            if (_completionWindow != null && e.Text.Length > 0 && !char.IsLetterOrDigit(e.Text[0]))
            {
                _completionWindow.CompletionList.RequestInsertion(e);
                e.Handled = true; // guarantee the non-alnum char (e.g., space) is canceled
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
         var off = Editor.CaretOffset;
         var nl = Environment.NewLine;
 Editor.Document.Insert(off, nl);
  Editor.CaretOffset = off + nl.Length;
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
           var off = Editor.CaretOffset;
      Editor.Document.Insert(off, " ");
   Editor.CaretOffset = off + 1;
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
