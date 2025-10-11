using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Input;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Rendering;
using Wysg.Musm.Editor.Ui;
using Wysg.Musm.Editor.Internal; // ← mutation shield

namespace Wysg.Musm.Editor.Snippets;

public static class SnippetInputHandler
{
    private sealed class Session : IDisposable
    {
        public readonly TextArea Area;
        public readonly int InsertOffset;
        public readonly List<ExpandedPlaceholder> Map;     // appearance order
        public ExpandedPlaceholder? Current;
        private readonly PlaceholderOverlayRenderer _overlay;

        // Tracks dynamic end of snippet span
        public TextAnchor? EndAnchor { get; set; }

        // Popup for current placeholder
        public PlaceholderCompletionWindow? Popup { get; set; }

        // Mode 3 accumulation buffer and mode 2 selection set
        public readonly StringBuilder ChoiceBuffer = new();
        public readonly HashSet<string> MultiSelected = new(System.StringComparer.OrdinalIgnoreCase);
        
        // Track whether current free-text placeholder was modified
        public bool CurrentPlaceholderModified { get; set; }
        
        // Store original text of current placeholder for comparison
        public string CurrentPlaceholderOriginalText { get; set; } = string.Empty;

        public Session(TextArea area, int insertOffset, List<ExpandedPlaceholder> map)
        {
            Area = area;
            InsertOffset = insertOffset;
            Map = map;
            _overlay = new PlaceholderOverlayRenderer(area.TextView, ProvideSegments);
            Area.Document.Changed += OnDocumentChanged;
            Area.Caret.PositionChanged += OnCaretChanged;
        }

        public ExpandedPlaceholder? NextAfter(ExpandedPlaceholder cur)
        {
            int idx = Map.IndexOf(cur);
            if (idx < 0) return null;
            for (int i = idx + 1; i < Map.Count; i++)
                if (Map[i].Length > 0) return Map[i];
            return null;
        }

        private void OnCaretChanged(object? s, EventArgs e)
        {
            // Keep caret within current placeholder selection bounds
            if (Current is null) { _overlay.Invalidate(); return; }
            var sel = Area.Selection?.SurroundingSegment;
            if (sel is null) { _overlay.Invalidate(); return; }
            if (Area.Caret.Offset < sel.Offset)
            {
                using (EditorMutationShield.Begin(Area)) Area.Caret.Offset = sel.Offset;
            }
            else if (Area.Caret.Offset > sel.EndOffset)
            {
                using (EditorMutationShield.Begin(Area)) Area.Caret.Offset = sel.EndOffset;
            }
            _overlay.Invalidate();
        }

        private IEnumerable<(int start, int length, bool active)> ProvideSegments()
        {
            var selSeg = Area.Selection.SurroundingSegment;
            int activeStart = selSeg?.Offset ?? -1;
            int activeEnd = (selSeg is null) ? -1 : selSeg.Offset + selSeg.Length;

            foreach (var p in Map)
            {
                if (p.Length <= 0) continue;
                int absStart = InsertOffset + p.Start;
                int absEnd = absStart + p.Length;
                bool isActive = (selSeg != null && absStart == activeStart && absEnd == activeEnd);
                yield return (absStart, p.Length, isActive);
            }
        }

        private void OnDocumentChanged(object? s, DocumentChangeEventArgs e)
        {
            int delta = e.InsertionLength - e.RemovalLength;
            
            int pos = e.Offset; // absolute doc offset

            // Check if change is within current placeholder (free text typing)
            if (Current != null && Current.Options.Count == 0)
            {
                int startAbs = InsertOffset + Current.Start;
                int endAbs = startAbs + Current.Length;
                
                if (pos >= startAbs && pos <= endAbs)
                {
                    CurrentPlaceholderModified = true;
                }
            }

            if (delta == 0) return;

            foreach (var ph in Map)
            {
                int startAbs = InsertOffset + ph.Start;
                int endAbs = startAbs + ph.Length;

                if (pos <= startAbs)
                    ph.Start = Math.Max(0, ph.Start + delta);
                else if (pos < endAbs)
                    ph.Length = Math.Max(0, ph.Length + delta);
            }

            _overlay.Invalidate();
        }

        public void Dispose()
        {
            Area.Document.Changed -= OnDocumentChanged;
            Area.Caret.PositionChanged -= OnCaretChanged;
            _overlay.Dispose();
        }
    }

    public static void Start(TextArea area, string expandedText, List<ExpandedPlaceholder> map)
    {
        var doc = area.Document;
        int insertOffset = area.Caret.Offset;

        using (EditorMutationShield.Begin(area))
        {
            doc.Insert(insertOffset, expandedText);
        }

        if (map.Count == 0) return;

        PlaceholderModeManager.Enter();

        var session = new Session(area, insertOffset, map);
        // Hook mouse handlers to prevent caret jumps outside placeholder
        area.PreviewMouseDown += OnPreviewMouseDown;
        area.PreviewMouseUp += OnPreviewMouseUp;

        // Create anchor at snippet end so it tracks subsequent document changes inside snippet
        var endAnchor = doc.CreateAnchor(insertOffset + expandedText.Length);
        endAnchor.MovementType = AnchorMovementType.AfterInsertion; // extend when text inserted at anchor
        endAnchor.SurviveDeletion = true;
        session.EndAnchor = endAnchor;

        session.Current = session.Map.FirstOrDefault(p => p.Length > 0);
        if (session.Current is null) { Exit(); return; }

        SelectPlaceholder(area, session, session.Current);
        session.Popup = ShowPopup(area, session.Current);
        // Important: We subscribe to popup.CommitRequested instead of relying on synthetic Tab
        // forwarding from the popup. The popup raises this event asynchronously (Dispatcher.BeginInvoke),
        // which avoids re-entrant input routing and crashes that occurred when closing popups while
        // still in their own input handlers. See PlaceholderCompletionWindow (Root cause & fix note).
        if (session.Popup != null)
        {
            session.Popup.CommitRequested += OnPopupCommitRequested;
        }
        session.Popup?.SelectFirst();

        area.PreviewKeyDown += OnPreviewKeyDown;

        void OnPreviewKeyDown(object? s, KeyEventArgs e)
        {
            if (!PlaceholderModeManager.IsActive)
            {
                Cleanup();
                return;
            }

            var cur = session.Current;
            if (cur is null) { Exit(); e.Handled = true; return; }

            // Prevent caret moving out with arrow keys
            if (e.Key is Key.Left or Key.Right or Key.Home or Key.End)
            {
                var sel = area.Selection.SurroundingSegment;
                if (sel != null)
                {
                    if (e.Key is Key.Left or Key.Home)
                    {
                        using (EditorMutationShield.Begin(area)) area.Caret.Offset = sel.Offset;
                    }
                    else
                    {
                        using (EditorMutationShield.Begin(area)) area.Caret.Offset = sel.EndOffset;
                    }
                    e.Handled = true;
                    return;
                }
            }

            // Navigation inside popup
            if (e.Key == Key.Up) { session.Popup?.MoveSelection(-1); e.Handled = true; return; }
            if (e.Key == Key.Down) { session.Popup?.MoveSelection(+1); e.Handled = true; return; }

            // Mode-specific key processing
            if (TryMapKeyToString(e.Key, out var keyStr))
            {
                if (cur.Options.Count > 0)
                {
                    if (cur.Kind == PlaceholderKind.MultiChoice)
                    {
                        // Mode 2: Toggle selection (Space or letter)
                        session.Popup?.SelectByKey(keyStr);
                        session.Popup?.ToggleCurrent();
                        e.Handled = true;
                        return;
                    }
                    else if (cur.Mode == 1)
                    {
                        // Mode 1: Immediate accept on single key match
                        var match = cur.Options.FirstOrDefault(o => o.Key.Equals(keyStr, System.StringComparison.OrdinalIgnoreCase));
                        if (match != null)
                        {
                            AcceptOptionAndComplete(match.Text);
                            e.Handled = true;
                            return;
                        }
                        // If no match, ignore (do not mutate placeholder text)
                        e.Handled = true;
                        return;
                    }
                    else if (cur.Mode == 3)
                    {
                        // Mode 3: Accumulate multi-char key until Tab/Enter
                        session.ChoiceBuffer.Append(keyStr);
                        session.Popup?.SelectByKey(session.ChoiceBuffer.ToString());
                        e.Handled = true;
                        return;
                    }
                }
                else
                {
                    // Free text placeholder: allow normal typing (don't handle, let default behavior)
                    return; // e.Handled stays false
                }
            }

            // For Mode 1 and 3, lock other character keys (no typing allowed)
            if (cur.Options.Count > 0 && (cur.Mode == 1 || cur.Mode == 3))
            {
                // Allow only navigation and our handled keys; block others (including punctuation, letters not mapped, backspace/delete, etc.)
                if (!(e.Key is Key.Tab or Key.Enter or Key.Return or Key.Escape or Key.Up or Key.Down or Key.Left or Key.Right or Key.Home or Key.End))
                {
                    e.Handled = true;
                    return;
                }
            }

            if (e.Key == Key.Space)
            {
                if (cur.Kind == PlaceholderKind.MultiChoice)
                {
                    session.Popup?.ToggleCurrent();
                    e.Handled = true;
                    return;
                }
            }

            if (e.Key == Key.Tab)
            {
                // Tab completes current and moves to next
                if (cur.Options.Count == 0)
                {
                    // Free text: keep typed text; remove highlight and move next
                    MarkCurrentCompleted();
                    JumpToNextOrExit();
                    e.Handled = true;
                    return;
                }

                if (cur.Kind == PlaceholderKind.MultiChoice)
                {
                    var texts = session.Popup?.GetSelectedTexts() ?? new List<string>();
                    if (texts.Count == 0)
                    {
                        var sel = session.Popup?.Selected;
                        if (sel != null) texts.Add(sel.Text);
                    }
                    var output = FormatJoin(texts, cur.Joiner);
                    AcceptOptionAndComplete(output);
                    e.Handled = true;
                    return;
                }
                else if (cur.Mode == 3)
                {
                    // Use accumulated buffer to resolve
                    var key = session.ChoiceBuffer.ToString();
                    var match = cur.Options.FirstOrDefault(o => o.Key.Equals(key, System.StringComparison.OrdinalIgnoreCase))
                               ?? cur.Options.FirstOrDefault(o => o.Key.Equals(session.Popup?.Selected?.Key, System.StringComparison.OrdinalIgnoreCase))
                               ?? cur.Options.FirstOrDefault();
                    AcceptOptionAndComplete(match?.Text ?? string.Empty);
                    e.Handled = true;
                    return;
                }
                else if (cur.Mode == 1)
                {
                    // Use popup selected or default first
                    var selectedKey = session.Popup?.Selected?.Key;
                    var match = cur.Options.FirstOrDefault(o => o.Key.Equals(selectedKey, System.StringComparison.OrdinalIgnoreCase))
                               ?? cur.Options.FirstOrDefault();
                    AcceptOptionAndComplete(match?.Text ?? string.Empty);
                    e.Handled = true;
                    return;
                }
            }

            if (e.Key is Key.Enter)
            {
                // End snippet mode and move caret to next line; apply fallback replacement for ALL remaining placeholders
                ApplyFallbackAndEnd(moveToNextLine: true);
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Escape)
            {
                // End snippet: caret at end of inserted snippet; apply fallback replacement
                ApplyFallbackAndEnd(moveToNextLine: false);
                e.Handled = true; return;
            }
        }

        void MarkCurrentCompleted()
        {
            if (session.Current != null)
            {
                session.Current.Length = 0; // stop highlighting this placeholder
            }
        }

        void AcceptOptionAndComplete(string text)
        {
            if (session.Current is null) { Exit(); return; }
            ReplaceSelection(area, text);
            // Mark the placeholder as completed to remove highlight
            MarkCurrentCompleted();
            JumpToNextOrExit();
        }

        void ApplyFallbackAndEnd(bool moveToNextLine)
        {
            // Build replacements for ALL placeholders that are not completed (Length>0)
            var toReplace = new List<(int absStart, int length, string replacement)>();
            foreach (var ph in session.Map)
            {
                if (ph.Length <= 0) continue; // already completed
                int absStart = session.InsertOffset + ph.Start;
                int length = ph.Length;

                string? replacement = null;

                if (ph.Options.Count == 0)
                {
                    // Free text
                    if (ph == session.Current)
                    {
                        if (!session.CurrentPlaceholderModified)
                            replacement = "[ ]";
                        // else leave typed text (replacement stays null)
                    }
                    else
                    {
                        // Non-current free text was not modified; replace with [ ]
                        replacement = "[ ]";
                    }
                }
                else if (ph.Mode == 1)
                {
                    replacement = ph.Options.FirstOrDefault()?.Text ?? string.Empty; // may be empty
                }
                else if (ph.Mode == 2)
                {
                    var all = ph.Options.Select(o => o.Text).ToList();
                    replacement = FormatJoin(all, ph.Joiner);
                }
                else if (ph.Mode == 3)
                {
                    replacement = ph.Options.FirstOrDefault()?.Text ?? string.Empty;
                }

                if (replacement != null)
                {
                    toReplace.Add((absStart, length, replacement));
                }
            }

            // Apply replacements from right to left to keep offsets valid
            if (toReplace.Count > 0)
            {
                using (EditorMutationShield.Begin(area))
                {
                    foreach (var item in toReplace.OrderByDescending(t => t.absStart))
                    {
                        area.Document.Replace(item.absStart, item.length, item.replacement);
                    }
                }
            }

            if (moveToNextLine)
            {
                // Move caret to the end of snippet, then insert newline
                MoveCaretToEndOfSnippet(area, session);
                InsertNewLineAtCaret(area);
            }
            else
            {
                MoveCaretToEndOfSnippet(area, session);
            }
            Exit();
        }

        void JumpToNextOrExit()
        {
            if (session.Current is null) { Exit(); return; }

            var next = session.NextAfter(session.Current);
            if (next is null || next.Length <= 0)
            {
                // No next placeholder → leave caret at end of inserted snippet
                MoveCaretToEndOfSnippet(area, session);
                Exit();
            }
            else
            {
                session.ChoiceBuffer.Clear();
                session.MultiSelected.Clear();
                session.Current = next;
                SelectPlaceholder(area, session, next);
                if (session.Popup != null)
                {
                    // Unsubscribe before closing to avoid event leaks
                    session.Popup.CommitRequested -= OnPopupCommitRequested;
                    session.Popup.Close();
                }
                session.Popup = ShowPopup(area, next);
                if (session.Popup != null)
                {
                    session.Popup.CommitRequested += OnPopupCommitRequested;
                }
                session.Popup?.SelectFirst();
            }
        }

        void Exit()
        {
            PlaceholderModeManager.Exit();
            Cleanup();
        }

        void Cleanup()
        {
            area.PreviewKeyDown -= OnPreviewKeyDown;
            area.PreviewMouseDown -= OnPreviewMouseDown;
            area.PreviewMouseUp -= OnPreviewMouseUp;
            if (session.Popup != null)
            {
                // Unsubscribe before closing to avoid event leaks
                session.Popup.CommitRequested -= OnPopupCommitRequested;
                session.Popup.Close();
            }
            session.Popup = null;
            session.Dispose();
        }

        void OnPopupCommitRequested(object? s, PlaceholderCompletionWindow.Item? selected)
        {
            if (session.Current is null) return;
            var cur = session.Current;
            if (cur.Options.Count == 0) return; // free text: ignore
            if (cur.Kind == PlaceholderKind.MultiChoice) return; // handled via Tab accumulation flow

            // Mode 1 or 3: accept selected item (fallback to first)
            string text;
            if (selected != null)
            {
                text = selected.Text;
            }
            else
            {
                var first = cur.Options.FirstOrDefault();
                text = first?.Text ?? string.Empty;
            }
            AcceptOptionAndComplete(text);
        }

        void OnPreviewMouseDown(object? s, MouseButtonEventArgs e)
        {
            if (!PlaceholderModeManager.IsActive) return;
            var sel = area.Selection.SurroundingSegment;
            if (sel == null) return;
            var relative = e.GetPosition(area.TextView); // point relative to TextView
            var tvPos = area.TextView.GetPosition(relative);
            if (tvPos == null)
            {
                e.Handled = true; return;
            }
            int off = area.Document.GetOffset(tvPos.Value.Location);
            int start = sel.Offset;
            int end = sel.EndOffset;
            if (off < start || off > end)
            {
                e.Handled = true;
                using (EditorMutationShield.Begin(area)) area.Caret.Offset = (off < start) ? start : end;
            }
        }

        void OnPreviewMouseUp(object? s, MouseButtonEventArgs e)
        {
            if (!PlaceholderModeManager.IsActive) return;
            var sel = area.Selection.SurroundingSegment;
            if (sel == null) return;
            int start = sel.Offset;
            int end = sel.EndOffset;
            if (area.Caret.Offset < start || area.Caret.Offset > end)
            {
                e.Handled = true;
                using (EditorMutationShield.Begin(area)) area.Caret.Offset = end;
            }
        }
    }

    private static string FormatJoin(IReadOnlyList<string> items, string? joiner)
    {
        if (items == null || items.Count == 0) return string.Empty;
        if (items.Count == 1) return items[0];
        string conj = (string.Equals(joiner, "or", StringComparison.OrdinalIgnoreCase)) ? "or"
                    : (string.Equals(joiner, "and", StringComparison.OrdinalIgnoreCase)) ? "and"
                    : "and"; // default
        if (items.Count == 2) return string.Join($" {conj} ", items);
        // Oxford comma style: a, b, and c
        var head = string.Join(", ", items.Take(items.Count - 1));
        return $"{head}, {conj} {items[^1]}";
    }

    private static void MoveCaretToLineEnd(TextArea area)
    {
        var line = area.Document.GetLineByOffset(area.Caret.Offset);
        using (EditorMutationShield.Begin(area))
        {
            area.Caret.Offset = line.EndOffset;
            area.Document.Insert(line.EndOffset, Environment.NewLine);
            area.Caret.Offset = line.EndOffset + Environment.NewLine.Length;
        }
    }

    private static void InsertNewLineAtCaret(TextArea area)
    {
        using (EditorMutationShield.Begin(area))
        {
            var off = area.Caret.Offset;
            area.Document.Insert(off, Environment.NewLine);
            area.Caret.Offset = off + Environment.NewLine.Length;
        }
    }

    private static void MoveCaretToEndOfSnippet(TextArea area, Session session)
    {
        // Prefer dynamic anchor when available
        int end = session.EndAnchor?.Offset ?? (session.InsertOffset + session.Map.Select(p => p.Start + p.Length).DefaultIfEmpty(0).Max());
        using (EditorMutationShield.Begin(area))
        {
            area.Selection = Selection.Create(area, end, end);
            area.Caret.Offset = end;
        }
    }

    private static bool TryMapKeyToString(Key key, out string s)
    {
        if (key >= Key.A && key <= Key.Z) { s = ((char)('a' + (key - Key.A))).ToString(); return true; }
        if (key >= Key.D0 && key <= Key.D9) { s = ((char)('0' + (key - Key.D0))).ToString(); return true; }
        if (key >= Key.NumPad0 && key <= Key.NumPad9) { s = ((char)('0' + (key - Key.NumPad0))).ToString(); return true; }
        s = string.Empty; return false;
    }

    private static void SelectPlaceholder(TextArea area, Session session, ExpandedPlaceholder p)
    {
        using (EditorMutationShield.Begin(area))
        {
            session.Current = p;
            int absStart = session.InsertOffset + p.Start;
            area.Selection = Selection.Create(area, absStart, absStart + p.Length);
            area.Caret.Offset = absStart + p.Length;
            area.TextView.InvalidateLayer(KnownLayer.Selection);
            
            // Record original text and reset modification flag
            session.CurrentPlaceholderOriginalText = area.Document.GetText(absStart, p.Length);
            session.CurrentPlaceholderModified = false;
        }
    }

    private static void ReplaceSelection(TextArea area, string text)
    {
        using (EditorMutationShield.Begin(area))
        {
            var sel = area.Selection.SurroundingSegment;
            if (sel is null) return;
            area.Document.Replace(sel.Offset, sel.Length, text);
            area.Selection = Selection.Create(area, sel.Offset, sel.Offset + text.Length);
            area.Caret.Offset = sel.Offset + text.Length;
            area.TextView.InvalidateLayer(KnownLayer.Selection);
        }
    }

    private static PlaceholderCompletionWindow? ShowPopup(TextArea area, ExpandedPlaceholder p)
    {
        if (p.Options.Count == 0) return null;
        var items = p.Options.Select(o => new PlaceholderCompletionWindow.Item(o.Key, o.Text));
        var win = new PlaceholderCompletionWindow(area, items, p.Kind == PlaceholderKind.MultiChoice);
        win.ShowAtCaret();
        return win;
    }
}
