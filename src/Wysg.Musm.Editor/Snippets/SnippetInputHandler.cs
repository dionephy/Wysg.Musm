// src/Wysg.Musm.Editor/Snippets/SnippetInputHandler.cs
using System.Linq;
using System.Windows.Input;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;

namespace Wysg.Musm.Editor.Snippets;

public static class SnippetInputHandler
{
    public static void Start(TextArea area, string expandedText, System.Collections.Generic.List<ExpandedPlaceholder> map)
    {
        // Insert expanded text at caret, remember first placeholder
        var doc = area.Document;
        int insertOffset = area.Caret.Offset;
        doc.Insert(insertOffset, expandedText);

        if (map.Count == 0) return;

        PlaceholderModeManager.Enter();

        // Focus first placeholder by smallest Index
        var ph = map.OrderBy(p => p.Index).First();
        SelectPlaceholder(area, insertOffset, ph);

        area.PreviewKeyDown += OnPreviewKeyDown;
        void OnPreviewKeyDown(object? s, KeyEventArgs e)
        {
            if (!PlaceholderModeManager.IsActive) { area.PreviewKeyDown -= OnPreviewKeyDown; return; }

            // digits choose options if a placeholder is selected
            if (e.Key >= Key.D0 && e.Key <= Key.D9)
            {
                int digit = (int)(e.Key - Key.D0);
                var current = GetCurrentPlaceholder(area, insertOffset, map);
                if (current != null)
                {
                    var opt = current.Options.FirstOrDefault(o => o.Digit == digit);
                    if (opt is not null)
                    {
                        // replace selection with chosen option
                        int oldLen = current.Length;
                        ReplaceSelection(area, opt.Text);
                        int newLen = opt.Text.Length;

                        // update current placeholder size
                        current.Length = newLen;

                        // shift START of all subsequent placeholders by the delta
                        int delta = newLen - oldLen;
                        if (delta != 0)
                        {
                            foreach (var ph in map.Where(p => p.Index > current.Index))
                                ph.Start += delta;
                        }

                        // jump to next placeholder (by Index)
                        var next = map.Where(p => p.Index > current.Index).OrderBy(p => p.Index).FirstOrDefault();
                        if (next is null)
                        {
                            Exit();
                        }
                        else
                        {
                            SelectPlaceholder(area, insertOffset, next);
                        }
                        e.Handled = true;
                    }
                }

            }
            else if (e.Key == Key.Enter)
            {
                Exit();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                // cancel: just exit (leave current text as-is)
                Exit();
                e.Handled = true;
            }
        }

        void Exit()
        {
            PlaceholderModeManager.Exit();
            area.PreviewKeyDown -= OnPreviewKeyDown;
        }
    }

    private static ExpandedPlaceholder? GetCurrentPlaceholder(TextArea area, int insertOffset, System.Collections.Generic.List<ExpandedPlaceholder> map)
    {
        var sel = area.Selection.SurroundingSegment;
        if (sel is null) return null;
        int start = sel.Offset - insertOffset;
        return map.FirstOrDefault(p => p.Start == start);
    }

    private static void SelectPlaceholder(TextArea area, int insertOffset, ExpandedPlaceholder p)
    {
        area.Selection = Selection.Create(area, insertOffset + p.Start, insertOffset + p.Start + p.Length);
        area.Caret.Offset = insertOffset + p.Start + p.Length;
        ShowPopup(area, p);
    }

    private static void ReplaceSelection(TextArea area, string text)
    {
        var sel = area.Selection.SurroundingSegment;
        if (sel is null) return;
        area.Document.Replace(sel.Offset, sel.Length, text);
        area.Selection = Selection.Create(area, sel.Offset, sel.Offset + text.Length);
        area.Caret.Offset = sel.Offset + text.Length;
    }

    private static void ShowPopup(TextArea area, ExpandedPlaceholder p)
    {
        if (p.Options.Count == 0) return;
        var win = new PlaceholderCompletionWindow(area, p.Options.Select(o => new PlaceholderCompletionWindow.Item(o.Digit, o.Text)));
        win.ShowAtCaret();
    }
}
