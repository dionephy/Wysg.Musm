# ? **COMPLETE: Editor Autofocus Feature**

### What Was the Problem?

Four issues discovered during testing:

**Issue #1**: Initial optimization (removed debug logging, consolidated Dispatcher calls, replaced SendKeys)  
**Impact**: Improved but still slow

**Issue #2**: **CRITICAL** - `UiBookmarks.Resolve()` called on EVERY keypress  
**Impact**: 
- 50ms per keypress (FlaUI UI tree walking)
- 12-20 FlaUI exceptions per keypress
- Keys dropped when typing rapidly
- Even when typing IN the editor!

**Issue #3**: **TIMING ISSUE** - Manual key re-send causes first key and rapid keys to be dropped
**Impact**:
- First keypress after autofocus trigger was always ignored
- Multiple rapid keypresses were dropped
- SendInput timing didn't synchronize with editor focus completion
- Legacy code worked because it let Windows handle key delivery naturally

**Issue #4**: **CHILD ELEMENT TRIGGERING** - Child controls of bookmarked window also triggered autofocus
**Impact**:
- Typing in PACS search boxes triggered autofocus ?
- Typing in PACS filter dialogs triggered autofocus ?
- Typing in any child window/control triggered autofocus ?
- Feature unusable in practice - users couldn't interact with PACS UI

### The Fix

**Four-Part Solution**:

1. **Short-Circuit for Radium Window Focus**
```csharp
// NEW: Check if Radium already has focus BEFORE expensive bookmark check
var radiumHwnd = new WindowInteropHelper(Application.Current?.MainWindow).Handle;
if (GetForegroundWindow() == radiumHwnd)
    return false; // Skip autofocus - already in Radium!
```

2. **Bookmark HWND Caching**
```csharp
// Cache bookmark HWND for 2 seconds to avoid repeated FlaUI calls
private IntPtr _cachedBookmarkHwnd = IntPtr.Zero;
private DateTime _lastBookmarkCacheTime = DateTime.MinValue;

// Only re-resolve if cache expired or bookmark changed
if ((DateTime.Now - _lastBookmarkCacheTime) > TimeSpan.FromSeconds(2))
{
    // Expensive FlaUI call (once per 2 seconds)
    _cachedBookmarkHwnd = UiBookmarks.Resolve(knownControl).hwnd;
    _lastBookmarkCacheTime = DateTime.Now;
}

// Fast HWND comparison using cache
return GetForegroundWindow() == _cachedBookmarkHwnd;
```

3. **Synchronous Key Delivery with SendKeys** *(Latest Fix)*
```csharp
if (ShouldTriggerAutofocus(key))
{
    char keyChar = GetCharFromKey(key);
    
    // Synchronous: focus completes before SendKeys executes
    Dispatcher.Invoke(() =>
    {
        _focusEditorCallback();
        
        if (keyChar != '\0')
        {
            string keyString = keyChar switch
            {
                '+' => "{+}", '^' => "{^}", '%' => "{%}",
                '~' => "{~}", '(' => "{(}", ')' => "{)}",
                '{' => "{{}}", '}' => "{}}", '[' => "{[}", ']' => "{]}",
                _ => keyChar.ToString()
            };
            
            System.Windows.Forms.SendKeys.SendWait(keyString);
        }
    }, DispatcherPriority.Send);
    
    // CRITICAL: Return 1 to CONSUME key (blocks PACS shortcuts)
    return (IntPtr)1;
}

return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
```

**Why This Works**:
- `Dispatcher.Invoke` (synchronous) - blocks until focus completes
- `SendKeys.SendWait` - not blocked by hooks (unlike SendInput)
- Key consumed (`return 1`) - blocks PACS shortcuts
- Exact timing - focus guaranteed before input

4. **Exact HWND Match (Child Element Prevention)** *(Critical Fix)*
```csharp
private bool IsForegroundWindowTargetBookmark()
{
    var foregroundHwnd = GetForegroundWindow();
    
    // Get the ACTUAL focused element (not just top-level window)
    var focusedHwnd = GetFocusedWindowHandle(foregroundHwnd);
    
    // Only trigger if EXACT match (not child elements)
    return _cachedBookmarkHwnd == focusedHwnd;
}

private static IntPtr GetFocusedWindowHandle(IntPtr foregroundHwnd)
{
    uint threadId = GetWindowThreadProcessId(foregroundHwnd, IntPtr.Zero);
    
    GUITHREADINFO info = new GUITHREADINFO();
    info.cbSize = (uint)Marshal.SizeOf(info);
    
    if (GetGUIThreadInfo(threadId, ref info))
    {
        // Priority 1: Actual focused control (e.g., textbox)
        if (info.hwndFocus != IntPtr.Zero)
            return info.hwndFocus;
        
        // Priority 2: Active window in thread
        if (info.hwndActive != IntPtr.Zero)
            return info.hwndActive;
    }
    
    // Priority 3: Foreground window
    return foregroundHwnd;
}
```

**Why This Works**:
- `GetGUIThreadInfo()` - gets actual focused control HWND
- **Exact HWND comparison** - only bookmarked element triggers
- Child controls (textboxes, buttons, dialogs) have different HWNDs
- Prevents false positives from descendant elements
