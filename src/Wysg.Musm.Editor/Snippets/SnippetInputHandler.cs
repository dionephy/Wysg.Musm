using System;
using System.Linq;
using System.Collections.Generic;
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

        private void OnCaretChanged(object? s, EventArgs e) => _overlay.Invalidate();

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
            if (delta == 0) return;

            int pos = e.Offset; // absolute doc offset

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

        PlaceholderCompletionWindow? popup = null;

        session.Current = session.Map.FirstOrDefault(p => p.Length > 0);
        if (session.Current is null) { Exit(); return; }

        SelectPlaceholder(area, session, session.Current);
        popup = ShowPopup(area, session.Current);
        popup?.SelectFirst();

        area.PreviewKeyDown += OnPreviewKeyDown;

        void OnPreviewKeyDown(object? s, KeyEventArgs e)
        {
            if (!PlaceholderModeManager.IsActive)
            {
                Cleanup();
                return;
            }

            var cur = session.Current;

            if (e.Key == Key.Up) { popup?.MoveSelection(-1); e.Handled = true; return; }
            if (e.Key == Key.Down) { popup?.MoveSelection(+1); e.Handled = true; return; }

            if (TryMapKeyToString(e.Key, out var keyStr))
            {
                if (cur != null && cur.Options.Count > 0)
                {
                    popup?.SelectByKey(keyStr);

                    if (cur.Kind == PlaceholderKind.MultiChoice)
                    {
                        popup?.ToggleCurrent();
                        e.Handled = true;
                        return;
                    }
                    else
                    {
                        var sel = popup?.Selected;
                        if (sel is not null)
                        {
                            AcceptOption(sel.Text);
                            e.Handled = true;
                            return;
                        }
                    }
                }
            }

            if (e.Key == Key.Space)
            {
                if (cur != null && cur.Kind == PlaceholderKind.MultiChoice)
                {
                    popup?.ToggleCurrent();
                    e.Handled = true;
                    return;
                }
            }

            if (e.Key == Key.Enter || e.Key == Key.Tab)
            {
                if (cur == null) { Exit(); e.Handled = true; return; }

                if (cur.Options.Count == 0)
                {
                    JumpToNextOrExit();
                    e.Handled = true;
                    return;
                }

                if (cur.Kind == PlaceholderKind.MultiChoice)
                {
                    var texts = popup?.GetSelectedTexts() ?? new List<string>();
                    if (texts.Count == 0)
                    {
                        var sel = popup?.Selected;
                        if (sel != null) texts.Add(sel.Text);
                    }
                    string joiner = string.IsNullOrWhiteSpace(cur.Joiner) ? " " : $" {cur.Joiner.Trim()} ";
                    AcceptOption(string.Join(joiner, texts));
                    e.Handled = true;
                    return;
                }
                else
                {
                    var sel = popup?.Selected;
                    if (sel != null)
                    {
                        AcceptOption(sel.Text);
                    }
                    else
                    {
                        popup?.SelectFirst();
                        sel = popup?.Selected;
                        if (sel != null) AcceptOption(sel.Text);
                        else Exit();
                    }
                    e.Handled = true;
                    return;
                }
            }

            if (e.Key == Key.Escape)
            {
                Exit(); e.Handled = true; return;
            }
        }

        void AcceptOption(string text)
        {
            if (session.Current is null) { Exit(); return; }

            ReplaceSelection(area, text);
            JumpToNextOrExit();
        }

        void JumpToNextOrExit()
        {
            if (session.Current is null) { Exit(); return; }

            var next = session.NextAfter(session.Current);
            if (next is null || next.Length <= 0)
            {
                using (EditorMutationShield.Begin(area))
                    area.Selection = Selection.Create(area, area.Caret.Offset, area.Caret.Offset);
                Exit();
            }
            else
            {
                session.Current = next;
                SelectPlaceholder(area, session, next);
                popup?.Close();
                popup = ShowPopup(area, next);
                popup?.SelectFirst();
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
            popup?.Close();
            popup = null;
            session.Dispose();
        }
    }

    private static bool TryMapKeyToString(Key key, out string s)
    {
        if (key >= Key.A && key <= Key.Z) { s = ((char)('a' + (key - Key.A))).ToString(); return true; }
        if (key >= Key.D0 && key <= Key.D9) { s = ((char)('0' + (key - Key.D0))).ToString(); return true; }
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
