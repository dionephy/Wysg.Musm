using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Input;

namespace Wysg.Musm.Radium.Services
{
    /// <summary>
    /// Service that monitors global keyboard input and triggers editor autofocus
    /// when configured keys are pressed while a target UI element has focus.
    /// 
    /// Based on legacy MainViewModel.KeyHookTarget pattern but with configurable
    /// bookmark targeting and key type filtering.
    /// 
    /// Performance: Optimized for minimal latency in hook callback.
    /// Logging is disabled by default for speed; set ENABLE_DIAGNOSTIC_LOGGING to true for debugging.
    /// </summary>
    public sealed class EditorAutofocusService : IDisposable
    {
        private readonly IRadiumLocalSettings _settings;
        private readonly Action _focusEditorCallback;
        private IntPtr _hookHandle = IntPtr.Zero;
        private LowLevelKeyboardProc? _hookProc; // Keep delegate alive to prevent GC
        private bool _isDisposed;
        
        // Cached bookmark resolution to avoid expensive FlaUI calls on every keypress
        private IntPtr _cachedBookmarkHwnd = IntPtr.Zero;
        private string? _cachedBookmarkName = null;
        private DateTime _lastBookmarkCacheTime = DateTime.MinValue;
        private static readonly TimeSpan BookmarkCacheExpiry = TimeSpan.FromSeconds(2); // Re-resolve every 2 seconds

        // Autofocus operation queue to prevent race conditions
        private readonly System.Collections.Concurrent.ConcurrentQueue<char> _pendingKeys = new();
        private int _isProcessingQueue = 0; // 0 = idle, 1 = processing

        // Diagnostic logging flag - set to true only when debugging performance issues
        private const bool ENABLE_DIAGNOSTIC_LOGGING = false;

        // Windows hook constants
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;

        // Delegate for low-level keyboard hook
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        /// <summary>
        /// KBDLLHOOKSTRUCT structure for low-level keyboard input
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct KBDLLHOOKSTRUCT
        {
            public uint vkCode;
            public uint scanCode;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        public EditorAutofocusService(IRadiumLocalSettings settings, Action focusEditorCallback)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _focusEditorCallback = focusEditorCallback ?? throw new ArgumentNullException(nameof(focusEditorCallback));
        }

        /// <summary>
        /// Starts monitoring keyboard input for autofocus triggers.
        /// </summary>
        public void Start()
        {
            if (_hookHandle != IntPtr.Zero)
            {
                if (ENABLE_DIAGNOSTIC_LOGGING)
                    Debug.WriteLine("[EditorAutofocusService] Hook already installed");
                return;
            }

            if (!_settings.EditorAutofocusEnabled)
            {
                if (ENABLE_DIAGNOSTIC_LOGGING)
                    Debug.WriteLine("[EditorAutofocusService] Autofocus disabled in settings, not starting hook");
                return;
            }

            try
            {
                // Keep delegate alive to prevent GC collection
                _hookProc = HookCallback;
                
                using var curProcess = System.Diagnostics.Process.GetCurrentProcess();
                using var curModule = curProcess.MainModule;
                
                _hookHandle = SetWindowsHookEx(WH_KEYBOARD_LL, _hookProc, GetModuleHandle(curModule?.ModuleName ?? ""), 0);
                
                if (_hookHandle == IntPtr.Zero)
                {
                    var error = Marshal.GetLastWin32Error();
                    Debug.WriteLine($"[EditorAutofocusService] Failed to install hook. Error: {error}");
                }
                else if (ENABLE_DIAGNOSTIC_LOGGING)
                {
                    Debug.WriteLine("[EditorAutofocusService] Keyboard hook installed successfully");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[EditorAutofocusService] Error starting hook: {ex.Message}");
            }
        }

        /// <summary>
        /// Stops monitoring keyboard input.
        /// </summary>
        public void Stop()
        {
            if (_hookHandle != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookHandle);
                _hookHandle = IntPtr.Zero;
                if (ENABLE_DIAGNOSTIC_LOGGING)
                    Debug.WriteLine("[EditorAutofocusService] Keyboard hook uninstalled");
            }
            
            // Clear cached bookmark
            _cachedBookmarkHwnd = IntPtr.Zero;
            _cachedBookmarkName = null;
        }

        /// <summary>
        /// Low-level keyboard hook callback.
        /// </summary>
        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            try
            {
                // Only process if code is HC_ACTION (0)
                if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
                {
                    var hookStruct = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
                    var key = KeyInterop.KeyFromVirtualKey((int)hookStruct.vkCode);

                    if (ENABLE_DIAGNOSTIC_LOGGING)
                        Debug.WriteLine($"[EditorAutofocus] Key pressed: {key}");

                    // Check if autofocus should trigger
                    if (ShouldTriggerAutofocus(key))
                    {
                        if (ENABLE_DIAGNOSTIC_LOGGING)
                            Debug.WriteLine($"[EditorAutofocus] ShouldTriggerAutofocus returned TRUE for key: {key}");
                        
                        // Get character before any async operations
                        char keyChar = GetCharFromKey(key);
                        
                        // STRATEGY: Consume original key, focus editor, then send the key
                        // This ensures exactly one character is inserted (the sent key)
                        // We use BeginInvoke to avoid blocking the hook callback
                        System.Windows.Application.Current?.Dispatcher.BeginInvoke(
                            System.Windows.Threading.DispatcherPriority.Send,
                            new Action(() =>
                            {
                                try
                                {
                                    // Focus editor first
                                    _focusEditorCallback();
                                    
                                    // Small delay to ensure focus transfer completes
                                    // This is critical for consistent single-character insertion
                                    System.Threading.Thread.Sleep(10);
                                    
                                    // Send the key (only once, since we consumed the original)
                                    if (keyChar != '\0')
                                    {
                                        string keyString = keyChar switch
                                        {
                                            '+' => "{+}",
                                            '^' => "{^}",
                                            '%' => "{%}",
                                            '~' => "{~}",
                                            '(' => "{(}",
                                            ')' => "{)}",
                                            '{' => "{{}",
                                            '}' => "{}}",
                                            '[' => "{[}",
                                            ']' => "{]}",
                                            _ => keyChar.ToString()
                                        };
                                        
                                        System.Windows.Forms.SendKeys.SendWait(keyString);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    if (ENABLE_DIAGNOSTIC_LOGGING)
                                        Debug.WriteLine($"[EditorAutofocus] Error in dispatcher callback: {ex.Message}");
                                }
                            }));
                        
                        // CONSUME the original key to prevent it from reaching any window
                        // This ensures only our sent key arrives at the editor (single character)
                        return (IntPtr)1;
                    }
                    else
                    {
                        if (ENABLE_DIAGNOSTIC_LOGGING)
                            Debug.WriteLine($"[EditorAutofocus] ShouldTriggerAutofocus returned FALSE for key: {key}");
                    }
                }
            }
            catch (Exception ex)
            {
                if (ENABLE_DIAGNOSTIC_LOGGING)
                    Debug.WriteLine($"[EditorAutofocus] Exception in HookCallback: {ex.Message}");
            }

            // Pass through to next hook for non-autofocus keys
            return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
        }

        /// <summary>
        /// Sends a keypress using SendInput (faster and more reliable than SendKeys).
        /// </summary>
        private static void SendKeyPress(char ch)
        {
            try
            {
                if (ENABLE_DIAGNOSTIC_LOGGING)
                    Debug.WriteLine($"[EditorAutofocus] SendKeyPress called for '{ch}'");
                
                // Convert character to virtual key code
                short vk = VkKeyScan(ch);
                if (vk == -1) 
                {
                    if (ENABLE_DIAGNOSTIC_LOGGING)
                        Debug.WriteLine($"[EditorAutofocus] VkKeyScan failed for '{ch}'");
                    return; // Character cannot be mapped
                }
                
                byte virtualKey = (byte)(vk & 0xFF);
                byte shiftState = (byte)((vk >> 8) & 0xFF);
                bool needShift = (shiftState & 1) != 0;
                
                if (ENABLE_DIAGNOSTIC_LOGGING)
                    Debug.WriteLine($"[EditorAutofocus] VK={virtualKey}, NeedShift={needShift}");
                
                INPUT[] inputs = needShift ? new INPUT[4] : new INPUT[2];
                int inputIndex = 0;
                
                // Press Shift if needed
                if (needShift)
                {
                    inputs[inputIndex++] = new INPUT
                    {
                        type = INPUT_KEYBOARD,
                        ki = new KEYBDINPUT { wVk = VK_SHIFT, dwFlags = 0 }
                    };
                }
                
                // Press key
                inputs[inputIndex++] = new INPUT
                {
                    type = INPUT_KEYBOARD,
                    ki = new KEYBDINPUT { wVk = virtualKey, dwFlags = 0 }
                };
                
                // Release key
                inputs[inputIndex++] = new INPUT
                {
                    type = INPUT_KEYBOARD,
                    ki = new KEYBDINPUT { wVk = virtualKey, dwFlags = KEYEVENTF_KEYUP }
                };
                
                // Release Shift if needed
                if (needShift)
                {
                    inputs[inputIndex] = new INPUT
                    {
                        type = INPUT_KEYBOARD,
                        ki = new KEYBDINPUT { wVk = VK_SHIFT, dwFlags = KEYEVENTF_KEYUP }
                    };
                }
                
                uint result = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
                
                if (ENABLE_DIAGNOSTIC_LOGGING)
                    Debug.WriteLine($"[EditorAutofocus] SendInput returned {result} (expected {inputs.Length})");
            }
            catch (Exception ex) 
            { 
                if (ENABLE_DIAGNOSTIC_LOGGING)
                    Debug.WriteLine($"[EditorAutofocus] SendKeyPress exception: {ex.Message}");
            }
        }

        // P/Invoke declarations for SendInput
        private const int INPUT_KEYBOARD = 1;
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const byte VK_SHIFT = 0x10;

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public int type;
            public KEYBDINPUT ki;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [DllImport("user32.dll")]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        private static extern short VkKeyScan(char ch);

        /// <summary>
        /// Converts a WPF Key to a character for SendKeys.
        /// </summary>
        private static char GetCharFromKey(Key key)
        {
            // Alphabet keys
            if (key >= Key.A && key <= Key.Z)
                return (char)('a' + (key - Key.A));
            
            // Number keys (main keyboard)
            if (key >= Key.D0 && key <= Key.D9)
                return (char)('0' + (key - Key.D0));
            
            // Numpad keys
            if (key >= Key.NumPad0 && key <= Key.NumPad9)
                return (char)('0' + (key - Key.NumPad0));
            
            // Space
            if (key == Key.Space)
                return ' ';
            
            // Tab
            if (key == Key.Tab)
                return '\t';
            
            // Common symbols (OEM keys)
            return key switch
            {
                Key.OemPeriod => '.',
                Key.OemComma => ',',
                Key.OemQuestion => '/',
                Key.OemSemicolon => ';',
                Key.OemQuotes => '\'',
                Key.OemOpenBrackets => '[',
                Key.OemCloseBrackets => ']',
                Key.OemPipe => '\\',
                Key.OemPlus => '=',
                Key.OemMinus => '-',
                Key.OemTilde => '`',
                _ => '\0' // Null character for unsupported keys
            };
        }

        /// <summary>
        /// Determines if the given key should trigger autofocus based on current settings.
        /// </summary>
        private bool ShouldTriggerAutofocus(Key key)
        {
            if (ENABLE_DIAGNOSTIC_LOGGING)
                Debug.WriteLine($"[EditorAutofocus] ShouldTriggerAutofocus called for key: {key}");
            
            // Quick checks first (fastest to slowest)
            if (!_settings.EditorAutofocusEnabled)
            {
                if (ENABLE_DIAGNOSTIC_LOGGING)
                    Debug.WriteLine($"[EditorAutofocus] Feature disabled in settings");
                return false;
            }

            // Check for modifier keys - only trigger on plain keys
            if (Keyboard.Modifiers != ModifierKeys.None)
            {
                if (ENABLE_DIAGNOSTIC_LOGGING)
                    Debug.WriteLine($"[EditorAutofocus] Modifier keys pressed: {Keyboard.Modifiers}");
                return false;
            }

            // Check if key matches configured key types
            if (!IsKeyTypeEnabled(key))
            {
                if (ENABLE_DIAGNOSTIC_LOGGING)
                    Debug.WriteLine($"[EditorAutofocus] Key type not enabled for key: {key}");
                return false;
            }

            // SHORT-CIRCUIT: If Radium window already has focus, don't trigger autofocus
            // This prevents the expensive foreground window check when typing in the editor
            try
            {
                var currentForeground = GetForegroundWindow();
                var radiumHwnd = new System.Windows.Interop.WindowInteropHelper(
                    System.Windows.Application.Current?.MainWindow).Handle;
                
                if (currentForeground == radiumHwnd)
                {
                    if (ENABLE_DIAGNOSTIC_LOGGING)
                        Debug.WriteLine($"[EditorAutofocus] Radium already has focus, skipping");
                    return false;
                }
            }
            catch { /* Fall through to normal check */ }

            // Most expensive check last - foreground window matches bookmark
            bool matches = IsForegroundWindowTargetBookmark();
            
            if (ENABLE_DIAGNOSTIC_LOGGING)
                Debug.WriteLine($"[EditorAutofocus] IsForegroundWindowTargetBookmark returned: {matches}");
            
            return matches;
        }

        /// <summary>
        /// Checks if the foreground window matches the configured bookmark.
        /// Uses cached bookmark HWND to avoid expensive FlaUI resolution on every keypress.
        /// IMPORTANT: Only triggers if the EXACT bookmark element has focus, not its children/descendants.
        /// 
        /// Two-stage matching for performance:
        /// 1. Fast: Check window title (rejects non-PACS apps immediately)
        /// 2. Precise: Check bookmark HWND (rejects child elements within PACS)
        /// </summary>
        private bool IsForegroundWindowTargetBookmark()
        {
            var bookmarkName = _settings.EditorAutofocusBookmark;
            var windowTitle = _settings.EditorAutofocusWindowTitle;
            
            if (ENABLE_DIAGNOSTIC_LOGGING)
            {
                Debug.WriteLine($"[EditorAutofocus] IsForegroundWindowTargetBookmark called:");
                Debug.WriteLine($"  BookmarkName: '{bookmarkName}'");
                Debug.WriteLine($"  WindowTitle: '{windowTitle}'");
            }
            
            var foregroundHwnd = GetForegroundWindow();
            if (foregroundHwnd == IntPtr.Zero)
                return false;
            
            // STAGE 1: Fast window title check (avoids expensive bookmark resolve for non-PACS apps)
            // If window title is configured, check it FIRST as a gate
            if (!string.IsNullOrWhiteSpace(windowTitle))
            {
                var title = GetWindowTitle(foregroundHwnd);
                
                if (ENABLE_DIAGNOSTIC_LOGGING)
                    Debug.WriteLine($"[EditorAutofocus] Stage 1 - Window title check: '{title}'");
                
                // If title doesn't match, reject immediately (not PACS app)
                if (string.IsNullOrWhiteSpace(title) || !title.Contains(windowTitle, StringComparison.OrdinalIgnoreCase))
                {
                    if (ENABLE_DIAGNOSTIC_LOGGING)
                        Debug.WriteLine($"[EditorAutofocus] Stage 1 REJECT - Title mismatch");
                    return false;
                }
                
                if (ENABLE_DIAGNOSTIC_LOGGING)
                    Debug.WriteLine($"[EditorAutofocus] Stage 1 PASS - Title matches, proceeding to Stage 2");
            }
            else
            {
                if (ENABLE_DIAGNOSTIC_LOGGING)
                    Debug.WriteLine($"[EditorAutofocus] Stage 1 SKIP - No window title configured");
            }
            
            // STAGE 2: Precise bookmark HWND check (filters out child elements within PACS)
            // If bookmark is configured, use exact HWND matching to exclude child controls
            if (string.IsNullOrWhiteSpace(bookmarkName))
            {
                if (ENABLE_DIAGNOSTIC_LOGGING)
                    Debug.WriteLine($"[EditorAutofocus] Stage 2 SKIP - No bookmark configured, accepting based on title match");
                
                // If only title is configured (no bookmark), accept based on title alone
                // This is legacy behavior but won't filter child elements
                return !string.IsNullOrWhiteSpace(windowTitle);
            }

            try
            {
                if (ENABLE_DIAGNOSTIC_LOGGING)
                    Debug.WriteLine($"[EditorAutofocus] Stage 2 - Bookmark HWND check");
                
                // Check if we need to refresh the cached bookmark HWND
                bool needRefresh = _cachedBookmarkName != bookmarkName ||
                                   _cachedBookmarkHwnd == IntPtr.Zero ||
                                   (DateTime.Now - _lastBookmarkCacheTime) > BookmarkCacheExpiry;

                if (needRefresh)
                {
                    // Resolve bookmark by name (simplified - no enum parsing)
                    var (bookmarkHwnd, _) = UiBookmarks.Resolve(bookmarkName);
                    
                    // Cache the result
                    _cachedBookmarkHwnd = bookmarkHwnd;
                    _cachedBookmarkName = bookmarkName;
                    _lastBookmarkCacheTime = DateTime.Now;
                    
                    if (ENABLE_DIAGNOSTIC_LOGGING)
                        Debug.WriteLine($"[EditorAutofocus] Bookmark '{bookmarkName}' resolved to HWND: 0x{bookmarkHwnd:X}");
                }

                // Get the actual focused element (not just top-level window)
                var focusedHwnd = GetFocusedWindowHandle(foregroundHwnd);
                
                if (ENABLE_DIAGNOSTIC_LOGGING)
                {
                    Debug.WriteLine($"[EditorAutofocus] Stage 2 - HWND comparison:");
                    Debug.WriteLine($"  Foreground HWND: 0x{foregroundHwnd:X}");
                    Debug.WriteLine($"  Focused HWND:    0x{focusedHwnd:X}");
                    Debug.WriteLine($"  Bookmark HWND:   0x{_cachedBookmarkHwnd:X}");
                }
                
                // Exact HWND match: only the bookmarked element itself triggers
                bool matches = _cachedBookmarkHwnd == focusedHwnd;
                
                if (ENABLE_DIAGNOSTIC_LOGGING)
                    Debug.WriteLine($"[EditorAutofocus] Stage 2 result: {(matches ? "ACCEPT" : "REJECT")} - HWND exact match: {matches}");
                
                return matches;
            }
            catch (Exception ex)
            {
                if (ENABLE_DIAGNOSTIC_LOGGING)
                    Debug.WriteLine($"[EditorAutofocus] Stage 2 EXCEPTION: {ex.Message}");
                
                // Clear cache on error
                _cachedBookmarkHwnd = IntPtr.Zero;
                _cachedBookmarkName = null;
                return false;
            }
        }
        
        /// <summary>
        /// Gets the HWND of the currently focused element within the foreground window.
        /// Uses GetGUIThreadInfo to get the actual focused control, not just the top-level window.
        /// This allows us to distinguish between the parent window and its child controls.
        /// </summary>
        private static IntPtr GetFocusedWindowHandle(IntPtr foregroundHwnd)
        {
            try
            {
                // Get the thread ID of the foreground window
                uint threadId = GetWindowThreadProcessId(foregroundHwnd, IntPtr.Zero);
                if (threadId == 0)
                {
                    if (ENABLE_DIAGNOSTIC_LOGGING)
                        Debug.WriteLine($"[EditorAutofocus] GetWindowThreadProcessId failed, using foreground HWND");
                    return foregroundHwnd;
                }
                
                if (ENABLE_DIAGNOSTIC_LOGGING)
                    Debug.WriteLine($"[EditorAutofocus] Thread ID: {threadId}");
                
                // Get GUI thread info to find the actual focused window
                GUITHREADINFO info = new GUITHREADINFO();
                info.cbSize = (uint)Marshal.SizeOf(info);
                
                if (GetGUIThreadInfo(threadId, ref info))
                {
                    if (ENABLE_DIAGNOSTIC_LOGGING)
                    {
                        Debug.WriteLine($"[EditorAutofocus] GUI Thread Info:");
                        Debug.WriteLine($"  hwndActive:  0x{info.hwndActive:X}");
                        Debug.WriteLine($"  hwndFocus:   0x{info.hwndFocus:X}");
                        Debug.WriteLine($"  hwndCapture: 0x{info.hwndCapture:X}");
                    }
                    
                    // Priority 1: hwndFocus (actual focused control)
                    if (info.hwndFocus != IntPtr.Zero)
                    {
                        if (ENABLE_DIAGNOSTIC_LOGGING)
                            Debug.WriteLine($"[EditorAutofocus] Using hwndFocus: 0x{info.hwndFocus:X}");
                        return info.hwndFocus;
                    }
                    
                    // Priority 2: hwndActive (active window in thread)
                    if (info.hwndActive != IntPtr.Zero)
                    {
                        if (ENABLE_DIAGNOSTIC_LOGGING)
                            Debug.WriteLine($"[EditorAutofocus] Using hwndActive: 0x{info.hwndActive:X}");
                        return info.hwndActive;
                    }
                }
                else
                {
                    if (ENABLE_DIAGNOSTIC_LOGGING)
                        Debug.WriteLine($"[EditorAutofocus] GetGUIThreadInfo failed");
                }
                
                // Fallback: return the foreground window if we can't get focus info
                if (ENABLE_DIAGNOSTIC_LOGGING)
                    Debug.WriteLine($"[EditorAutofocus] Using foreground HWND (fallback): 0x{foregroundHwnd:X}");
                return foregroundHwnd;
            }
            catch (Exception ex)
            {
                if (ENABLE_DIAGNOSTIC_LOGGING)
                    Debug.WriteLine($"[EditorAutofocus] Exception in GetFocusedWindowHandle: {ex.Message}");
                return foregroundHwnd;
            }
        }
        
        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr processId);
        
        [DllImport("user32.dll")]
        private static extern bool GetGUIThreadInfo(uint idThread, ref GUITHREADINFO lpgui);
        
        [StructLayout(LayoutKind.Sequential)]
        private struct GUITHREADINFO
        {
            public uint cbSize;
            public uint flags;
            public IntPtr hwndActive;
            public IntPtr hwndFocus;
            public IntPtr hwndCapture;
            public IntPtr hwndMenuOwner;
            public IntPtr hwndMoveSize;
            public IntPtr hwndCaret;
            public System.Drawing.Rectangle rcCaret;
        }

        /// <summary>
        /// Gets the window title for the given window handle.
        /// </summary>
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
        
        private static string GetWindowTitle(IntPtr hWnd)
        {
            const int nMaxCount = 512;
            var windowText = new StringBuilder(nMaxCount);
            
            if (GetWindowText(hWnd, windowText, nMaxCount) > 0)
            {
                return windowText.ToString();
            }
            return string.Empty;
        }

        /// <summary>
        /// Checks if the given key type is enabled in settings.
        /// </summary>
        private bool IsKeyTypeEnabled(Key key)
        {
            var keyTypesStr = _settings.EditorAutofocusKeyTypes ?? string.Empty;
            if (string.IsNullOrWhiteSpace(keyTypesStr))
                return false;

            var enabledTypes = new HashSet<string>(
                keyTypesStr.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                StringComparer.OrdinalIgnoreCase);

            if (enabledTypes.Contains("Alphabet") && IsAlphabetKey(key))
                return true;

            if (enabledTypes.Contains("Numbers") && IsNumberKey(key))
                return true;

            if (enabledTypes.Contains("Space") && key == Key.Space)
                return true;

            if (enabledTypes.Contains("Tab") && key == Key.Tab)
                return true;

            if (enabledTypes.Contains("Symbols") && IsSymbolKey(key))
                return true;

            return false;
        }

        /// <summary>
        /// Checks if key is an alphabet key (A-Z).
        /// </summary>
        private static bool IsAlphabetKey(Key key) => key >= Key.A && key <= Key.Z;

        /// <summary>
        /// Checks if key is a number key (0-9, including numpad).
        /// </summary>
        private static bool IsNumberKey(Key key) =>
            (key >= Key.D0 && key <= Key.D9) ||
            (key >= Key.NumPad0 && key <= Key.NumPad9);

        /// <summary>
        /// Checks if key is a symbol/punctuation key.
        /// </summary>
        private static bool IsSymbolKey(Key key)
        {
            return key switch
            {
                Key.OemPeriod or Key.OemComma or Key.OemQuestion or Key.OemSemicolon or
                Key.OemQuotes or Key.OemOpenBrackets or Key.OemCloseBrackets or Key.OemPipe or
                Key.OemPlus or Key.OemMinus or Key.OemTilde or Key.Multiply or
                Key.Add or Key.Subtract or Key.Divide or Key.Decimal => true,
                _ => false
            };
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            Stop();
            _isDisposed = true;
            Debug.WriteLine("[EditorAutofocusService] Disposed");
        }
    }
}
