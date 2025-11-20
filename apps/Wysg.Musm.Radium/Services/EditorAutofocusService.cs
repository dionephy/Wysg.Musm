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

                    // Check if autofocus should trigger
                    if (ShouldTriggerAutofocus(key))
                    {
                        // Get character before any async operations
                        char keyChar = GetCharFromKey(key);
                        
                        // Simple synchronous approach: focus then send immediately
                        System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                        {
                            try
                            {
                                // Focus editor
                                _focusEditorCallback();
                                
                                // Send key immediately (synchronously)
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
                            catch { /* Silently fail */ }
                        }, System.Windows.Threading.DispatcherPriority.Send);
                        
                        // CRITICAL: Return 1 to CONSUME the key
                        return (IntPtr)1;
                    }
                }
            }
            catch { /* Silently fail - performance critical path */ }

            // Pass through only if autofocus NOT triggered
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
            // Quick checks first (fastest to slowest)
            if (!_settings.EditorAutofocusEnabled)
                return false;

            // Check for modifier keys - only trigger on plain keys
            if (Keyboard.Modifiers != ModifierKeys.None)
                return false;

            // Check if key matches configured key types
            if (!IsKeyTypeEnabled(key))
                return false;

            // SHORT-CIRCUIT: If Radium window already has focus, don't trigger autofocus
            // This prevents the expensive foreground window check when typing in the editor
            try
            {
                var currentForeground = GetForegroundWindow();
                var radiumHwnd = new System.Windows.Interop.WindowInteropHelper(
                    System.Windows.Application.Current?.MainWindow).Handle;
                
                if (currentForeground == radiumHwnd)
                {
                    // Radium already has focus - no need to autofocus
                    return false;
                }
            }
            catch { /* Fall through to normal check */ }

            // Most expensive check last - foreground window matches bookmark
            if (!IsForegroundWindowTargetBookmark())
                return false;

            return true;
        }

        /// <summary>
        /// Checks if the foreground window matches the configured bookmark.
        /// Uses cached bookmark HWND to avoid expensive FlaUI resolution on every keypress.
        /// </summary>
        private bool IsForegroundWindowTargetBookmark()
        {
            var bookmarkName = _settings.EditorAutofocusBookmark;
            var windowTitle = _settings.EditorAutofocusWindowTitle;
            
            // If window title is configured, use title-based detection (legacy pattern)
            if (!string.IsNullOrWhiteSpace(windowTitle))
            {
                try
                {
                    var foregroundHwnd = GetForegroundWindow();
                    if (foregroundHwnd == IntPtr.Zero)
                        return false;
                    
                    var title = GetWindowTitle(foregroundHwnd);
                    return !string.IsNullOrWhiteSpace(title) && title.Contains(windowTitle, StringComparison.OrdinalIgnoreCase);
                }
                catch
                {
                    return false;
                }
            }
            
            // Otherwise use bookmark-based detection
            if (string.IsNullOrWhiteSpace(bookmarkName))
                return false;

            try
            {
                var foregroundHwnd = GetForegroundWindow();
                if (foregroundHwnd == IntPtr.Zero)
                    return false;

                // Check if we need to refresh the cached bookmark HWND
                bool needRefresh = _cachedBookmarkName != bookmarkName ||
                                   _cachedBookmarkHwnd == IntPtr.Zero ||
                                   (DateTime.Now - _lastBookmarkCacheTime) > BookmarkCacheExpiry;

                if (needRefresh)
                {
                    // Resolve bookmark (expensive FlaUI call)
                    if (!Enum.TryParse<UiBookmarks.KnownControl>(bookmarkName, out var knownControl))
                        return false;

                    var (bookmarkHwnd, _) = UiBookmarks.Resolve(knownControl);
                    
                    // Cache the result
                    _cachedBookmarkHwnd = bookmarkHwnd;
                    _cachedBookmarkName = bookmarkName;
                    _lastBookmarkCacheTime = DateTime.Now;
                }

                // Fast HWND comparison using cached value
                return _cachedBookmarkHwnd == foregroundHwnd;
            }
            catch
            {
                // Clear cache on error
                _cachedBookmarkHwnd = IntPtr.Zero;
                _cachedBookmarkName = null;
                return false;
            }
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
