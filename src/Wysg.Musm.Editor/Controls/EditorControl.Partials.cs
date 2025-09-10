// src/Wysg.Musm.Editor/Controls/EditorControl.Partials.cs
namespace Wysg.Musm.Editor.Controls
{
    public partial class EditorControl
    {
        // declarations only
        partial void InitPopup();
        partial void CleanupPopup();

        partial void InitServerGhosts();
        partial void CleanupServerGhosts();

        partial void DisableTabInsertion();
        partial void InitInlineGhost();
        partial void CleanupInlineGhost();
    }
}
