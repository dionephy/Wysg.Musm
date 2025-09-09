// src/Wysg.Musm.Editor/Controls/MusmEditor.cs
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using System;
using System.Windows;
using System.Windows.Threading;
using Wysg.Musm.Editor.Internal;

namespace Wysg.Musm.Editor.Controls;

/// <summary>
/// Lean, reusable AvalonEdit-based editor with bindable properties.
/// No completion/snippet orchestration lives here.
/// </summary>
public class MusmEditor : TextEditor
{
    private bool _suppressTextSync;

    public MusmEditor()
    {
        // Keep DP mirrors in sync with the underlying editor state
        TextChanged += (_, __) =>
        {
            if (_suppressTextSync) return;
            SetCurrentValue(DocumentTextProperty, base.Text ?? string.Empty);
        };

        // Caret & selection mirrors (bindable)
        TextArea.Caret.PositionChanged += (_, __) =>
        {
            SetCurrentValue(CaretOffsetBindableProperty, base.CaretOffset);
            // Selection often changes with caret moves; update mirrors
            UpdateSelectionMirrors();
        };

        // Many AvalonEdit builds expose SelectionChanged on TextArea; if yours doesn’t,
        // caret change + text change will still keep mirrors accurate enough.
        try
        {
            TextArea.SelectionChanged += (_, __) => UpdateSelectionMirrors();
        }
        catch { /* ignore if not available in your AvalonEdit version */ }
    }

    private void UpdateSelectionMirrors()
    {
        var sel = TextArea.Selection;
        var offset = sel.SurroundingSegment?.Offset ?? base.SelectionStart;
        var length = sel.SurroundingSegment?.Length ?? base.SelectionLength;

        SetCurrentValue(SelectionStartBindableProperty, offset);
        SetCurrentValue(SelectionLengthBindableProperty, length);
        SetCurrentValue(SelectedTextBindableProperty, sel.IsEmpty ? string.Empty : sel.GetText());
    }

    // ========== DocumentText (bindable) ==========
    public static readonly DependencyProperty DocumentTextProperty =
        DependencyProperty.Register(
            nameof(DocumentText),
            typeof(string),
            typeof(MusmEditor),
            new FrameworkPropertyMetadata(string.Empty,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnDocumentTextChanged));

    public string DocumentText
    {
        get => (string)GetValue(DocumentTextProperty);
        set => SetValue(DocumentTextProperty, value);
    }

    private static void OnDocumentTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var editor = (MusmEditor)d;
        var newText = e.NewValue as string ?? string.Empty;
        if (editor.Text == newText) return;

        editor._suppressTextSync = true;
        try
        {
            editor.Text = newText;
        }
        finally
        {
            editor._suppressTextSync = false;
        }
    }

    // ========== Caret/Selection (bindable mirrors) ==========
    public static readonly DependencyProperty CaretOffsetBindableProperty =
        DependencyProperty.Register(nameof(CaretOffsetBindable), typeof(int), typeof(MusmEditor),
            new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public int CaretOffsetBindable
    {
        get => (int)GetValue(CaretOffsetBindableProperty);
        set
        {
            SetValue(CaretOffsetBindableProperty, value);
            // Optional: moving caret from VM
            if (value != base.CaretOffset)
                base.CaretOffset = Math.Max(0, Math.Min(value, Document?.TextLength ?? 0));
        }
    }

    public static readonly DependencyProperty SelectionStartBindableProperty =
        DependencyProperty.Register(nameof(SelectionStartBindable), typeof(int), typeof(MusmEditor),
            new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectionBindableChanged));

    public static readonly DependencyProperty SelectionLengthBindableProperty =
        DependencyProperty.Register(nameof(SelectionLengthBindable), typeof(int), typeof(MusmEditor),
            new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectionBindableChanged));

    public static readonly DependencyProperty SelectedTextBindableProperty =
        DependencyProperty.Register(nameof(SelectedTextBindable), typeof(string), typeof(MusmEditor),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedTextBindableChanged));

    public int SelectionStartBindable
    {
        get => (int)GetValue(SelectionStartBindableProperty);
        set => SetValue(SelectionStartBindableProperty, value);
    }

    public int SelectionLengthBindable
    {
        get => (int)GetValue(SelectionLengthBindableProperty);
        set => SetValue(SelectionLengthBindableProperty, value);
    }

    public string SelectedTextBindable
    {
        get => (string)GetValue(SelectedTextBindableProperty);
        set => SetValue(SelectedTextBindableProperty, value);
    }

    private static void OnSelectionBindableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ed = (MusmEditor)d;
        // When VM moves selection explicitly
        var start = ed.SelectionStartBindable;
        var length = ed.SelectionLengthBindable;
        if (start >= 0 && length >= 0 && ed.Document != null)
        {
            start = Math.Min(start, ed.Document.TextLength);
            length = Math.Min(length, Math.Max(0, ed.Document.TextLength - start));
            ed.Select(start, length);
        }
    }

    private static void OnSelectedTextBindableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not MusmEditor ed || ed.TextArea?.Document is null) return;

        // 1) If snippet/editor is mutating, skip mirror for now.
        if (EditorMutationShield.IsActive(ed.TextArea)) return;

        // 2) prevent loops
        if (ed._updatingFromSelection) return;

        var sel = ed.TextArea.Selection?.SurroundingSegment;
        if (sel is null) return;

        string newText = e.NewValue as string ?? string.Empty;

        var doc = ed.TextArea.Document;
        if (sel.Offset < 0 || sel.Offset + sel.Length > doc.TextLength) return;

        ed._updatingFromSelection = true;
        try
        {
            ed.SafeReplace(doc, sel.Offset, sel.Length, newText);

            // mirror selection to new text (deferred in case we had to queue replace)
            ed.Dispatcher.BeginInvoke(new Action(() =>
            {
                var ddoc = ed.TextArea.Document;
                int newEnd = Math.Min(sel.Offset + newText.Length, ddoc.TextLength);
                using (EditorMutationShield.Begin(ed.TextArea))
                {
                    ed.TextArea.Selection = Selection.Create(ed.TextArea, sel.Offset, newEnd);
                    ed.TextArea.Caret.Offset = newEnd;
                }
            }), DispatcherPriority.Background);
        }
        finally
        {
            ed._updatingFromSelection = false;
        }
    }

    private void SafeReplace(TextDocument doc, int offset, int length, string newText)
    {
        try
        {
            doc.Replace(offset, length, newText);
        }
        catch (InvalidOperationException ex) when (
            ex.Message.IndexOf("undo/redo", StringComparison.OrdinalIgnoreCase) >= 0 ||
            ex.Message.IndexOf("another document change", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            // Defer until the current document change or undo/redo completes
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (offset < 0 || offset > doc.TextLength) return;
                int safeLen = Math.Min(length, Math.Max(0, doc.TextLength - offset));
                if (safeLen < 0) return;
                try { doc.Replace(offset, safeLen, newText); } catch { /* still in flux; skip */ }
            }), DispatcherPriority.Background);
        }
    }

    // add a private field:
    private bool _updatingFromSelection;
}
