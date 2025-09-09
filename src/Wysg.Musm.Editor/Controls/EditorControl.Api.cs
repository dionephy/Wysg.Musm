using System;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.AvalonEdit.Rendering;

namespace Wysg.Musm.Editor.Controls
{
    public partial class EditorControl
    {
        /// <summary>Replace all server ghosts and reset the selection to the first ghost (if any).</summary>
        public void UpdateServerGhosts(IEnumerable<(int line, string text)> items)
        {
            var list = (items ?? Enumerable.Empty<(int line, string text)>()).ToList();
            ServerGhosts.Set(list);   // triggers invalidate
            ResetGhostSelection();
        }

        public void ClearServerGhosts()
        {
            _selectedGhost = -1;
            ServerGhosts.Clear();
        }

        public void InvalidateGhosts()
        {
            var tv = Editor?.TextArea?.TextView;
            tv?.InvalidateLayer(KnownLayer.Text); // must match MultiLineGhostRenderer.Layer
            // tv?.InvalidateVisual(); // optional extra nudge
        }

        // Expose selection navigation for host if needed
        public void SelectNextGhost() => MoveGhostSelection(+1);
        public void SelectPrevGhost() => MoveGhostSelection(-1);
        public void AcceptCurrentGhost() => AcceptSelectedGhost();

        // Close the completion popup if it’s open (safe to call anytime)
        public void DismissCompletionPopup()
        {
            // private method lives in EditorControl.Popup.cs; OK to call from another partial
            CloseCompletionWindow();
        }

        // Optional: close popup + clear ghosts together
        public void DismissPopupAndGhosts()
        {
            CloseCompletionWindow();
            ClearServerGhosts();
        }


    }
}
