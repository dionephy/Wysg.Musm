// src/Wysg.Musm.Editor/Snippets/PlaceholderModeManager.cs
using System;

namespace Wysg.Musm.Editor.Snippets;

public static class PlaceholderModeManager
{
    public static bool IsActive { get; private set; }
    public static event EventHandler? PlaceholderModeEntered;
    public static event EventHandler? PlaceholderModeExited;

    internal static void Enter()
    {
        IsActive = true;
        PlaceholderModeEntered?.Invoke(null, EventArgs.Empty);
    }
    internal static void Exit()
    {
        IsActive = false;
        PlaceholderModeExited?.Invoke(null, EventArgs.Empty);
    }
}
