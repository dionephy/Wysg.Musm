using System;
using System.Collections.Generic;
using System.Windows;
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
        }

        /// <summary>One-liner to show a completion window for current word.</summary>
        public static MusmCompletionWindow ShowForCurrentWord(TextEditor editor, IEnumerable<ICompletionData> items)
        {
            var w = new MusmCompletionWindow(editor);
            var target = w.CompletionList.CompletionData;
            foreach (var item in items) target.Add(item);
            w.ComputeReplaceRegionFromCaret();
            w.Show();
            return w;
        }
    }

    
  
}
