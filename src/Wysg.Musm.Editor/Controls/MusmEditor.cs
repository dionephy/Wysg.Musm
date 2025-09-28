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
/// MusmEditor
///
/// Root cause of the earlier "reverse (right→left) selection collapse" bug:
/// ---------------------------------------------------------------
/// We expose several bindable dependency properties that mirror the editor selection:
///   - SelectionStartBindable
///   - SelectionLengthBindable
///   - SelectedTextBindable
/// These are updated on every caret / selection change (view → DP), but the DP change handlers
/// also push changes back into AvalonEdit (DP → view) by calling Select(...).
///
/// During a native mouse drag (especially right→left) AvalonEdit continually adjusts the caret
/// and the selection anchor. While the left button is held we received a rapid sequence:
///   1. AvalonEdit updates internal selection (anchor stays at initial down position).
///   2. We mirror to the DP (view → DP) (OK).
///   3. DP property change triggers OnSelectionBindableChanged (DP → view) which calls Select(...)
///      using the mirrored offsets. That explicit Select resets AvalonEdit's notion of the anchor,
///      breaking the reverse expansion and often collapsing / flickering the selection.
///
/// Net effect: right→left drags never visually extend because we kept overwriting the in‑progress
/// native selection with our mirrored copy; the anchor kept jumping.
///
/// Fix implemented:
/// 1. Introduced _userSelecting flag (true while mouse button is held) so we know a live drag is happening.
/// 2. Introduced _suppressSelectionMirror to guard against recursive DP updates while we copy view → DP.
/// 3. While _userSelecting is true we DO mirror view → DP (so any one‑way bindings still see movement) but
///    we IGNORE DP → view callbacks (OnSelectionBindableChanged / OnSelectedTextBindableChanged) to avoid
///    calling Select(...) mid‑drag. This preserves AvalonEdit's internal anchor and native behavior.
/// 4. After mouse up, _userSelecting becomes false so external programmatic selection changes (e.g. VM)
///    again flow both ways.
/// 5. Added the safety guard _suppressSelectedTextMirror for the existing text mirror pathway, plus
///    _updatingFromSelection (existing) to prevent loops on text replacement.
///
/// Key invariants now:
///   - No DP → view selection mutation while the user is dragging.
///   - Programmatic selection (from VM) still works when not actively dragging.
///   - Reverse selection direction (caret positioned at selection start) is preserved.
///
/// If future features need to programmatically alter selection during a drag, they must either:
///   a) Temporarily clear _userSelecting (NOT recommended) or
///   b) Provide a distinct API that defers changes until mouse up.
/// </summary>
public class MusmEditor : TextEditor
{
    private bool _suppressTextSync;
    private bool _suppressSelectedTextMirror; // Prevent feedback when mirroring SelectedText → DP
    private bool _suppressSelectionMirror;    // Guard: true only while copying view → DP (block DP → view)
    private bool _userSelecting;              // True while mouse button held; suppresses DP-driven selection changes
    private bool _updatingFromSelection;      // Existing loop-prevention for SelectedText updates

    public MusmEditor()
    {
        // Keep DP mirrors in sync with the underlying editor state
        TextChanged += (_, __) =>
        {
            if (_suppressTextSync) return;
            SetCurrentValue(DocumentTextProperty, base.Text ?? string.Empty);
        };

        // Caret & selection mirrors
        TextArea.Caret.PositionChanged += (_, __) =>
        {
            SetCurrentValue(CaretOffsetBindableProperty, base.CaretOffset);
            UpdateSelectionMirrors();
        };

        // SelectionChanged (if available)
        try { TextArea.SelectionChanged += (_, __) => UpdateSelectionMirrors(); } catch { }

        // Track mouse-based selection lifecycle (suppresses DP → view writes while true)
        TextArea.PreviewMouseLeftButtonDown += (_, __) => _userSelecting = true;
        TextArea.PreviewMouseLeftButtonUp += (_, __) => _userSelecting = false;
    }

    /// <summary>
    /// Copies current selection (view → DP) while blocking the reverse path. Safe during drag.
    /// </summary>
    private void UpdateSelectionMirrors()
    {
        var sel = TextArea.Selection;
        var offset = sel.SurroundingSegment?.Offset ?? base.SelectionStart;
        var length = sel.SurroundingSegment?.Length ?? base.SelectionLength;

        _suppressSelectionMirror = true;   // Block OnSelectionBindableChanged re-entry
        _suppressSelectedTextMirror = true; // Block SelectedText mirror handler
        try
        {
            SetCurrentValue(SelectionStartBindableProperty, offset);
            SetCurrentValue(SelectionLengthBindableProperty, length);
            SetCurrentValue(SelectedTextBindableProperty, sel.IsEmpty ? string.Empty : sel.GetText());
        }
        finally
        {
            _suppressSelectedTextMirror = false;
            _suppressSelectionMirror = false;
        }
    }

    // ===== DocumentText (bindable) =====
    public static readonly DependencyProperty DocumentTextProperty =
        DependencyProperty.Register(nameof(DocumentText), typeof(string), typeof(MusmEditor),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnDocumentTextChanged));

    public string DocumentText { get => (string)GetValue(DocumentTextProperty); set => SetValue(DocumentTextProperty, value); }

    private static void OnDocumentTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var editor = (MusmEditor)d;
        var newText = e.NewValue as string ?? string.Empty;
        if (editor.Text == newText) return;
        editor._suppressTextSync = true;
        try { editor.Text = newText; } finally { editor._suppressTextSync = false; }
    }

    // ===== Caret / Selection bindable mirrors =====
    public static readonly DependencyProperty CaretOffsetBindableProperty =
        DependencyProperty.Register(nameof(CaretOffsetBindable), typeof(int), typeof(MusmEditor),
            new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public int CaretOffsetBindable
    {
        get => (int)GetValue(CaretOffsetBindableProperty);
        set
        {
            SetValue(CaretOffsetBindableProperty, value);
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

    public int SelectionStartBindable { get => (int)GetValue(SelectionStartBindableProperty); set => SetValue(SelectionStartBindableProperty, value); }
    public int SelectionLengthBindable { get => (int)GetValue(SelectionLengthBindableProperty); set => SetValue(SelectionLengthBindableProperty, value); }
    public string SelectedTextBindable { get => (string)GetValue(SelectedTextBindableProperty); set => SetValue(SelectedTextBindableProperty, value); }

    /// <summary>
    /// DP → view path for selection start/length. Suppressed while dragging or mirroring.
    /// </summary>
    private static void OnSelectionBindableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ed = (MusmEditor)d;
        if (ed._suppressSelectionMirror || ed._userSelecting) return; // critical suppression to preserve reverse drag

        var start = ed.SelectionStartBindable;
        var length = ed.SelectionLengthBindable;
        if (start < 0 || length < 0 || ed.Document == null) return;

        start = Math.Min(start, ed.Document.TextLength);
        length = Math.Min(length, Math.Max(0, ed.Document.TextLength - start));
        ed.Select(start, length);
    }

    /// <summary>
    /// DP → view path for SelectedText (replacement). Blocked while user drags selection.
    /// </summary>
    private static void OnSelectedTextBindableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not MusmEditor ed || ed.TextArea?.Document is null) return;
        if (ed._suppressSelectedTextMirror) return;
        if (EditorMutationShield.IsActive(ed.TextArea)) return;
        if (ed._updatingFromSelection) return;
        if (ed._userSelecting) return; // critical: don't mutate doc mid-selection drag

        var sel = ed.TextArea.Selection?.SurroundingSegment;
        if (sel is null) return;

        string newText = e.NewValue as string ?? string.Empty;
        var doc = ed.TextArea.Document;
        if (sel.Offset < 0 || sel.Offset + sel.Length > doc.TextLength) return;

        ed._updatingFromSelection = true;
        try
        {
            ed.SafeReplace(doc, sel.Offset, sel.Length, newText);
            ed.Dispatcher.BeginInvoke(new Action(() =>
            {
                var ddoc = ed.TextArea.Document;
                int newEnd = Math.Min(sel.Offset + newText.Length, ddoc.TextLength);
                using (EditorMutationShield.Begin(ed.TextArea))
                {
                    ed.TextArea.Selection = Selection.Create(ed.TextArea, newEnd, newEnd);
                    ed.TextArea.Caret.Offset = newEnd;
                }
            }), DispatcherPriority.Background);
        }
        finally { ed._updatingFromSelection = false; }
    }

    private void SafeReplace(TextDocument doc, int offset, int length, string newText)
    {
        try { doc.Replace(offset, length, newText); }
        catch (InvalidOperationException ex) when (
            ex.Message.IndexOf("undo/redo", StringComparison.OrdinalIgnoreCase) >= 0 ||
            ex.Message.IndexOf("another document change", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (offset < 0 || offset > doc.TextLength) return;
                int safeLen = Math.Min(length, Math.Max(0, doc.TextLength - offset));
                if (safeLen < 0) return;
                try { doc.Replace(offset, safeLen, newText); } catch { }
            }), DispatcherPriority.Background);
        }
    }
}
