// src/Wysg.Musm.Editor/Controls/EditorControl.Api.cs
using System;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.AvalonEdit.Rendering;

namespace Wysg.Musm.Editor.Controls
{
    public partial class EditorControl
    {
        public bool AcceptSelectedServerGhost()
        {
            if (!ServerGhosts.HasItems || ServerGhosts.SelectedIndex < 0) return false;

            var (lineZero, text) = ServerGhosts.Items[ServerGhosts.SelectedIndex];
            var doc = Editor.Document;
            int line = lineZero + 1;
            if (line < 1 || line > doc.LineCount) return false;

            var dl = doc.GetLineByNumber(line);

            // Replace visible line (trim right-side whitespace)
            int start = dl.Offset;
            int end = dl.EndOffset;
            while (end > start && char.IsWhiteSpace(doc.GetCharAt(end - 1))) end--;

            doc.Replace(start, end - start, text);

            // Remove only the accepted ghost, keep others & reselect neighbor
            int next = ServerGhosts.SelectedIndex;
            ServerGhosts.Items.RemoveAt(ServerGhosts.SelectedIndex);
            if (ServerGhosts.Items.Count == 0)
            {
                ClearServerGhosts();
            }
            else
            {
                if (next >= ServerGhosts.Items.Count) next = ServerGhosts.Items.Count - 1;
                ServerGhosts.SelectIndex(next);   // ✅ reselect via method
                                                  // no need to call InvalidateGhosts() here; SelectIndex already invalidated
            }

            return true;
        }




        // Wrappers that forward to the canonical implementations in ServerGhosts.cs
        public void ApplyServerGhostsAbsolute(IEnumerable<(int line, string text)> items)
            => UpdateServerGhosts(items);

        public void ApplyServerGhostsNonEmpty(IEnumerable<(int nonEmptyIndex, string text)> items)
            => UpdateServerGhostsFromNonEmpty(items);

        public void ClearServerGhosts()
        {
            ServerGhosts.Clear();
            InvalidateGhosts();
            ResumeIdleAfterGhosts();
        }

        // EditorControl.Api.cs (or wherever you keep it)
        public void InvalidateGhosts()
        {
            var tv = Editor?.TextArea?.TextView;
            if (tv == null) return;
            tv.EnsureVisualLines();
            tv.InvalidateLayer(ICSharpCode.AvalonEdit.Rendering.KnownLayer.Selection);
            tv.InvalidateVisual();
        }


        public void EnableGhostDebugAnchors(bool on)
        {
            _ghostRenderer?.SetShowAnchors(on);
            InvalidateGhosts();
        }

        public string GetGhostDebugInfo()
        {
            var count = ServerGhosts.Items.Count;
            var lines = string.Join(",", ServerGhosts.Items.Select(i => i.line));
            return $"Ghosts:{count} lines=[{lines}] attached={(_ghostRenderer != null ? "True" : "False")}";
        }

        public void DebugSeedGhosts()
            => UpdateServerGhosts(new[] { (0, "Test ghost: renderer path OK") });

        // Leave this public for Playground to close popup safely
        public void DismissCompletionPopup() => CloseCompletionWindow();

        private void PauseIdleForGhosts()
        {
            _idlePausedForGhosts = true;
            _idleTimer.Stop();
        }

        private void ResumeIdleAfterGhosts()
        {
            _idlePausedForGhosts = false;
            RestartIdle();
        }

        public event EventHandler? ReadOnlyEditAttempted;

        private void OnReadOnlyEditAttempted()
        {
            ReadOnlyEditAttempted?.Invoke(this, EventArgs.Empty);
        }
    }
}
