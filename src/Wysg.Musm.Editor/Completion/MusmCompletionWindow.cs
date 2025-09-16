using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Diagnostics;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;

namespace Wysg.Musm.Editor.Completion
{
    /// <summary>
    /// Completion window that replaces the whole "word around caret"
    /// (prefix and tail on the current line). Filtering on by default.
    /// </summary>
    public sealed class MusmCompletionWindow : CompletionWindow
    {
        private readonly TextEditor _editor;
        private bool _allowSelectionOnce;

        public MusmCompletionWindow(TextEditor editor)
            : base(editor?.TextArea ?? throw new ArgumentNullException(nameof(editor)))
        {
            _editor = editor;

            CloseAutomatically = true;
            CompletionList.IsFiltering = true;
            SizeToContent = SizeToContent.Height;
            MaxHeight = 320;
            Width = 360;

            // Optional: match editor font
            try { CompletionList.ListBox.FontFamily = _editor.FontFamily; } catch { }
            try { CompletionList.ListBox.FontSize = _editor.FontSize; } catch { }

            // Never preselect by default
            try { CompletionList.ListBox.SelectedIndex = -1; } catch { }

            // Guard selection: only allow when explicitly permitted
            try { CompletionList.ListBox.SelectionChanged += OnListSelectionChanged; } catch { }

            // Handle keys when the list has focus (Enter/Home/End)
            try { CompletionList.ListBox.PreviewKeyDown += OnListPreviewKeyDown; } catch { }

            // Track caret changes to adjust/close; caret moves should not auto-select
            try { TextArea.Caret.PositionChanged += OnCaretPositionChanged; } catch { }
        }

        private void OnListSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_allowSelectionOnce)
            {
                _allowSelectionOnce = false; // consume permit
                return;
            }
            // otherwise, enforce exact-match-only by clearing selection
            if (CompletionList?.ListBox is { } lb)
            {
                Debug.WriteLine("[CW] clear selection (guard)");
                lb.SelectedIndex = -1;
            }
        }

        private void OnListPreviewKeyDown(object? sender, KeyEventArgs e)
        {
            Debug.WriteLine($"[CW] PKD key={e.Key} sel={CompletionList?.ListBox?.SelectedIndex}");
            if (CompletionList?.ListBox is null) return;

            if (e.Key is Key.Enter or Key.Return)
            {
                if (CompletionList.ListBox.SelectedIndex == -1)
                {
                    Debug.WriteLine("[CW] ENTER no selection → close + newline");
                    e.Handled = true;
                    Close();
                    var off = _editor.CaretOffset;
                    var nl = Environment.NewLine;
                    _editor.Document.Insert(off, nl);
                    _editor.CaretOffset = off + nl.Length;
                }
                return;
            }

            if (e.Key == Key.Home)
            {
                Debug.WriteLine("[CW] HOME move + maybe close");
                e.Handled = true;
                var line = _editor.Document.GetLineByOffset(_editor.CaretOffset);
                _editor.CaretOffset = line.Offset;
                if (_editor.CaretOffset < StartOffset || _editor.CaretOffset > EndOffset)
                    Close();
                return;
            }

            if (e.Key == Key.End)
            {
                Debug.WriteLine("[CW] END move + maybe close");
                e.Handled = true;
                var line = _editor.Document.GetLineByOffset(_editor.CaretOffset);
                _editor.CaretOffset = line.EndOffset;
                if (_editor.CaretOffset < StartOffset || _editor.CaretOffset > EndOffset)
                    Close();
                return;
            }
        }

        private void OnCaretPositionChanged(object? sender, EventArgs e)
        {
            int caret = TextArea.Caret.Offset;
            Debug.WriteLine($"[CW] CaretChanged caret={caret} range=[{StartOffset},{EndOffset}]");
            if (caret < StartOffset || caret > EndOffset)
            {
                Debug.WriteLine("[CW] Caret outside → close");
                if (CloseAutomatically) Close();
                return;
            }

            var (word, ok) = TryGetWordAtCaret();
            Debug.WriteLine($"[CW] word='{word}' ok={ok}");
            if (!ok || string.IsNullOrEmpty(word))
            {
                Debug.WriteLine("[CW] empty word → close");
                if (CloseAutomatically) Close();
                return;
            }
        }

        private (string word, bool ok) TryGetWordAtCaret()
        {
            var doc = _editor.Document;
            if (doc is null) return (string.Empty, false);
            int caret = _editor.CaretOffset;
            var line = doc.GetLineByOffset(caret);
            string lineText = doc.GetText(line);
            int local = Math.Clamp(caret - line.Offset, 0, lineText.Length);
            var (startLocal, endLocal) = WordBoundaryHelper.ComputeWordSpan(lineText, local);
            if (endLocal <= startLocal) return (string.Empty, true);
            var word = lineText.Substring(startLocal, endLocal - startLocal);
            return (word, true);
        }

        /// <summary>Compute StartOffset/EndOffset from the word around caret.</summary>
        public void ComputeReplaceRegionFromCaret()
        {
            var doc = _editor.Document;
            int caret = _editor.CaretOffset;

            var line = doc.GetLineByOffset(caret);
            string lineText = doc.GetText(line);
            int local = Math.Clamp(caret - line.Offset, 0, lineText.Length);

            var (startLocal, endLocal) = WordBoundaryHelper.ComputeWordSpan(lineText, local);
            StartOffset = line.Offset + startLocal;
            EndOffset = line.Offset + endLocal;
            Debug.WriteLine($"[CW] ComputeReplaceRegion Start={StartOffset} End={EndOffset}");
        }

        /// <summary>One-liner to show a completion window for current word.</summary>
        public static MusmCompletionWindow ShowForCurrentWord(TextEditor editor, IEnumerable<ICompletionData> items)
        {
            var w = new MusmCompletionWindow(editor);
            var target = w.CompletionList.CompletionData;
            foreach (var item in items) target.Add(item);
            w.ComputeReplaceRegionFromCaret();
            w.Show();
            Debug.WriteLine("[CW] ShowForCurrentWord opened");
            return w;
        }

        /// <summary>
        /// Select exact match by item.Text; otherwise keep selection cleared.
        /// </summary>
        public void SelectExactOrNone(string word)
        {
            if (CompletionList?.ListBox is null) return;
            var data = CompletionList.CompletionData?.OfType<ICompletionData>() ?? Enumerable.Empty<ICompletionData>();
            var match = data.FirstOrDefault(d => string.Equals(d.Text, word, StringComparison.Ordinal));
            Debug.WriteLine($"[CW] SelectExactOrNone word='{word}' match={(match!=null)}");
            if (match != null)
            {
                CompletionList.ListBox.SelectedItem = match;
                CompletionList.ListBox.ScrollIntoView(match);
            }
            else
            {
                CompletionList.ListBox.SelectedIndex = -1;
            }
        }

        /// <summary>
        /// Allow one selection change (e.g., caused by Up/Down).
        /// </summary>
        public void AllowSelectionByKeyboardOnce() => _allowSelectionOnce = true;
    }
}
