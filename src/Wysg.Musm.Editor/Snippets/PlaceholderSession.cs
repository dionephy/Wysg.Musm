using System;
using System.Collections.Generic;
using ICSharpCode.AvalonEdit.Document;

namespace Wysg.Musm.Editor.Snippets;

/// <summary>
/// Per-editor placeholder session; replaces the old global static manager.
/// Tracks the active placeholder and exposes simple navigation.
/// </summary>
public sealed class PlaceholderSession
{
    private readonly IList<Placeholder> _placeholders;

    public PlaceholderSession(IList<Placeholder> placeholders)
    {
        _placeholders = placeholders ?? Array.Empty<Placeholder>();
    }

    public bool IsActive { get; private set; }
    public int CurrentIndex { get; private set; } = 0;

    public event EventHandler? Entered;
    public event EventHandler? Exited;
    public event EventHandler? Changed;

    public void Enter()
    {
        if (IsActive) return;
        IsActive = true;
        CurrentIndex = 0;
        Entered?.Invoke(this, EventArgs.Empty);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Exit()
    {
        if (!IsActive) return;
        IsActive = false;
        Exited?.Invoke(this, EventArgs.Empty);
    }

    public Placeholder? Current =>
        (IsActive && CurrentIndex >= 0 && CurrentIndex < _placeholders.Count)
            ? _placeholders[CurrentIndex]
            : null;

    public bool MoveNext()
    {
        if (!IsActive) return false;
        if (CurrentIndex + 1 < _placeholders.Count)
        {
            CurrentIndex++;
            Changed?.Invoke(this, EventArgs.Empty);
            return true;
        }
        Exit();
        return false;
    }

    public bool MovePrev()
    {
        if (!IsActive) return false;
        if (CurrentIndex - 1 >= 0)
        {
            CurrentIndex--;
            Changed?.Invoke(this, EventArgs.Empty);
            return true;
        }
        return false;
    }

    /// <summary>Finds the placeholder that contains the given offset (if any) and makes it current.</summary>
    public void FocusAtOffset(int offset)
    {
        if (_placeholders.Count == 0) return;
        for (int i = 0; i < _placeholders.Count; i++)
        {
            var seg = _placeholders[i].Segment;
            if (seg is TextSegment ts && offset >= ts.StartOffset && offset <= ts.EndOffset)
            {
                CurrentIndex = i;
                if (!IsActive) Enter(); else Changed?.Invoke(this, EventArgs.Empty);
                return;
            }
        }
    }
}
