// src/Wysg.Musm.Editor/Controls/EditorControl.ServerGhosts.cs
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.AvalonEdit.Document;

namespace Wysg.Musm.Editor.Controls
{
    public partial class EditorControl
    {
        /// <summary>Backstore for server ghosts + selection.</summary>
        // Make it public so it’s not less accessible than the public property
        public sealed class GhostStore
        {
            public List<(int line, string text)> Items { get; private set; } = new();
            public bool HasItems => Items.Count > 0;

            // read-only from the outside; use SelectIndex/MoveSelection to change
            public int SelectedIndex { get; private set; } = -1;

            private readonly Action _invalidate;

            public GhostStore(Action invalidate)
            {
                _invalidate = invalidate ?? throw new ArgumentNullException(nameof(invalidate));
            }

            public void Set(IEnumerable<(int line, string text)> items, int? desiredIndex = null)
            {
                Items = (items ?? Enumerable.Empty<(int, string)>()).ToList();

                if (Items.Count == 0)
                {
                    SelectedIndex = -1;
                    _invalidate();
                    return;
                }

                if (desiredIndex.HasValue)
                    SelectIndex(desiredIndex.Value);
                else if (SelectedIndex < 0)
                    SelectedIndex = 0;

                if (SelectedIndex >= Items.Count) SelectedIndex = Items.Count - 1;

                _invalidate();
            }

            public void Clear()
            {
                if (Items.Count == 0 && SelectedIndex == -1) return;
                Items.Clear();
                SelectedIndex = -1;
                _invalidate();
            }

            public void SelectIndex(int index)
            {
                if (Items.Count == 0) { SelectedIndex = -1; _invalidate(); return; }
                var clamped = Math.Max(0, Math.Min(index, Items.Count - 1));
                if (clamped != SelectedIndex)
                {
                    SelectedIndex = clamped;
                    _invalidate();
                }
            }

            public void MoveSelection(int delta)
            {
                if (Items.Count == 0) { SelectedIndex = -1; _invalidate(); return; }
                if (SelectedIndex < 0) { SelectedIndex = 0; _invalidate(); return; }
                SelectIndex(SelectedIndex + delta);
            }
        }

        public GhostStore ServerGhosts { get; private set; }

        // ===== Update APIs (exactly one implementation each) =====

        // EditorControl.Api.cs (or wherever you keep ghost APIs)
        public void UpdateServerGhosts(IEnumerable<(int line, string text)> items)
        {
            var list = (items ?? Enumerable.Empty<(int line, string text)>()).ToList();
            var doc = Editor?.Document;

            if (doc is null || list.Count == 0)
            {
                ClearServerGhosts();
                return;
            }

            // Guard: out-of-range or obviously-bad shapes (e.g., all same line)
            bool allSame = list.Count > 1 && list.All(t => t.line == list[0].line);
            bool anyOut = list.Any(t => t.line < 0 || t.line >= doc.LineCount);
            if (allSame || anyOut)
            {
                // Fallback to “non-empty mapping” route
                var reindexed = list.Select((t, i) => (i, t.text));
                UpdateServerGhostsFromNonEmpty(reindexed);
                return;
            }

            // Normalize: sort by line and collapse duplicates (last wins)
            var normalized = list
                .OrderBy(t => t.line)
                .GroupBy(t => t.line)
                .Select(g => g.Last())
                .ToList();

            // Preserve previous selection by line number if possible
            int previousSelectedLine = (ServerGhosts.SelectedIndex >= 0 &&
                                        ServerGhosts.SelectedIndex < ServerGhosts.Items.Count)
                                        ? ServerGhosts.Items[ServerGhosts.SelectedIndex].line
                                        : -1;

            int prevLine = (ServerGhosts.SelectedIndex >= 0 && ServerGhosts.SelectedIndex < ServerGhosts.Items.Count)
               ? ServerGhosts.Items[ServerGhosts.SelectedIndex].line
               : -1;

            ServerGhosts.Set(normalized);

            // reselect
            int newSel = normalized.FindIndex(x => x.line == prevLine);
            if (newSel < 0 && normalized.Count > 0) newSel = 0;
            ServerGhosts.SelectIndex(newSel);   // <-- instead of property assignment
                       

            // InvalidateGhosts(); -> not needed?
            PauseIdleForGhosts(); // <- critical: while ghosts visible, idle won’t refresh
        }


        /// <summary>
        /// Map server indices that refer to *non-empty* lines to absolute document lines (0-based).
        /// Use this if your API returns indices after removing blank lines.
        /// </summary>
        public void UpdateServerGhostsFromNonEmpty(IEnumerable<(int nonEmptyIndex, string text)> suggestions)
        {
            var doc = Editor?.Document;
            if (doc is null)
            {
                ClearServerGhosts();
                return;
            }

            var nonEmptyLines = GetNonEmptyLineNumbers(doc); // 0-based doc line indexes of non-empty lines
            var mapped = new List<(int line, string text)>();

            foreach (var (idx, text) in suggestions ?? Enumerable.Empty<(int, string)>())
            {
                if (idx < 0 || idx >= nonEmptyLines.Count) continue;
                mapped.Add((nonEmptyLines[idx], text));
            }

            if (mapped.Count == 0)
            {
                ClearServerGhosts();
                return;
            }

            // Sort & collapse, then apply just like absolute version
            var normalized = mapped
                .OrderBy(t => t.line)
                .GroupBy(t => t.line)
                .Select(g => g.Last())
                .ToList();

            int previousSelectedLine = (ServerGhosts.SelectedIndex >= 0 &&
                                        ServerGhosts.SelectedIndex < ServerGhosts.Items.Count)
                                        ? ServerGhosts.Items[ServerGhosts.SelectedIndex].line
                                        : -1;

            int prevLine = (ServerGhosts.SelectedIndex >= 0 && ServerGhosts.SelectedIndex < ServerGhosts.Items.Count)
               ? ServerGhosts.Items[ServerGhosts.SelectedIndex].line
               : -1;

            ServerGhosts.Set(normalized);

            int newSel = normalized.FindIndex(x => x.line == prevLine);
            if (newSel < 0 && normalized.Count > 0) newSel = 0;
            ServerGhosts.SelectIndex(newSel);   // <-- here too

            //InvalidateGhosts(); <-- not needed?
            PauseIdleForGhosts();
        }

        private static List<int> GetNonEmptyLineNumbers(ICSharpCode.AvalonEdit.Document.TextDocument doc)
        {
            var result = new List<int>(capacity: doc.LineCount);
            for (int i = 1; i <= doc.LineCount; i++)
            {
                var dl = doc.GetLineByNumber(i);
                if (dl.Length == 0) continue;
                // Check if line has at least one non-whitespace char
                bool hasNonWs = false;
                for (int off = dl.Offset; off < dl.EndOffset; off++)
                {
                    if (!char.IsWhiteSpace(doc.GetCharAt(off))) { hasNonWs = true; break; }
                }
                if (hasNonWs) result.Add(i - 1); // convert to 0-based
            }
            return result;
        }


        private static List<int> BuildNonEmptyLineMap(TextDocument doc)
        {
            var list = new List<int>();
            for (int lineNo = 1; lineNo <= doc.LineCount; lineNo++)
            {
                var dl = doc.GetLineByNumber(lineNo);
                if (doc.GetText(dl).Trim().Length > 0)
                    list.Add(lineNo - 1); // zero-based
            }
            return list;
        }

       
    }
}
