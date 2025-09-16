using Wysg.Musm.MFCUIA.Abstractions;
using Wysg.Musm.MFCUIA.Core.Discovery;

namespace Wysg.Musm.MFCUIA.Selectors;

public static class By
{
    public static ISelector Class(string cls) => new ClassSelector(cls);
    public static ISelector TitleContains(string part) => new TitleContainsSelector(part);
    public static ISelector Nth(int index0) => new NthSelector(index0);
    public static ISelector ClassNth(string cls, int index0) => new ClassNthSelector(cls, index0);
}

internal sealed class ClassSelector(string cls) : ISelector
{
    public bool Matches(IntPtr hwnd, WindowSnapshot s) => string.Equals(s.ClassName, cls, StringComparison.OrdinalIgnoreCase);
}

internal sealed class TitleContainsSelector(string part) : ISelector
{
    public bool Matches(IntPtr hwnd, WindowSnapshot s) => !string.IsNullOrEmpty(s.Title) && s.Title!.Contains(part, StringComparison.OrdinalIgnoreCase);
}

internal sealed class NthSelector(int index0) : ISelector
{
    private int _i = -1;
    public bool Matches(IntPtr hwnd, WindowSnapshot s)
    {
        _i++;
        return _i == index0;
    }
}

// Increments index only when the class matches; returns true when reaching the Nth match
internal sealed class ClassNthSelector(string cls, int index0) : ISelector
{
    private int _i = -1;
    public bool Matches(IntPtr hwnd, WindowSnapshot s)
    {
        if (!string.Equals(s.ClassName, cls, StringComparison.OrdinalIgnoreCase)) return false;
        _i++;
        return _i == index0;
    }
}
