using System;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.AvalonEdit.Document;

namespace Wysg.Musm.Editor.Controls
{
    public partial class EditorControl
    {
        // Simple in-control store for server ghosts
        public sealed class ServerGhostStore
        {
            private readonly EditorControl _owner;
            private List<(int line, string text)> _items = new();

            public ServerGhostStore(EditorControl owner) => _owner = owner;

            public IReadOnlyList<(int line, string text)> Items => _items;

            public void Set(IEnumerable<(int line, string text)> items)
            {
                _items = (items ?? Array.Empty<(int, string)>()).ToList();
                _owner.InvalidateGhosts();
            }

            public void Clear()
            {
                if (_items.Count == 0) return;
                _items.Clear();
                _owner.InvalidateGhosts();
            }
        }

        // Store instance
        public ServerGhostStore ServerGhosts { get; private set; } = null!;

        private int _selectedGhost = -1; // index into ServerGhosts.Items (not line number)

        private void InitServerGhosts()
        {
            ServerGhosts = new ServerGhostStore(this);
            _selectedGhost = -1;
        }

        private void CleanupServerGhosts()
        {
            _selectedGhost = -1;
            ServerGhosts.Clear();
        }

        private int GetSelectedGhostIndex() => _selectedGhost;

        private void ResetGhostSelection()
        {
            _selectedGhost = ServerGhosts.Items.Count > 0 ? 0 : -1;
            InvalidateGhosts();
        }

        private void MoveGhostSelection(int delta)
        {
            if (ServerGhosts.Items.Count == 0) return;
            if (_selectedGhost < 0) _selectedGhost = 0;
            _selectedGhost = Math.Clamp(_selectedGhost + delta, 0, ServerGhosts.Items.Count - 1);
            InvalidateGhosts();
        }

        private void AcceptSelectedGhost()
        {
            if (_selectedGhost < 0 || _selectedGhost >= ServerGhosts.Items.Count) return;

            var (lineZero, text) = ServerGhosts.Items[_selectedGhost];
            var doc = Editor.Document;
            if (doc is null) return;

            int lineNo = lineZero + 1;
            if (lineNo < 1 || lineNo > doc.LineCount) return;

            DocumentLine line = doc.GetLineByNumber(lineNo);

            int start = line.Offset;
            int end = line.EndOffset;
            // trim trailing whitespace at EOL
            while (end > start && char.IsWhiteSpace(doc.GetCharAt(end - 1))) end--;

            doc.BeginUpdate();
            try
            {
                doc.Replace(start, end - start, text);
            }
            finally { doc.EndUpdate(); }

            // remove accepted ghost, reselect next
            var list = ServerGhosts.Items.ToList();
            list.RemoveAt(_selectedGhost);
            ServerGhosts.Set(list);

            if (list.Count == 0) _selectedGhost = -1;
            else _selectedGhost = Math.Min(_selectedGhost, list.Count - 1);
        }
    }
}
