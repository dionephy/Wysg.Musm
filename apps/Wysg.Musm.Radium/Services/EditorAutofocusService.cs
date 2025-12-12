using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Input;

namespace Wysg.Musm.Radium.Services
{
    /// <summary>
    /// Service that monitors global keyboard input and triggers editor autofocus
    /// when configured keys are pressed while a target PACS window has focus.
    /// 
    /// <para><b>How It Works:</b></para>
    /// <list type="number">
    ///   <item>Installs a low-level keyboard hook (WH_KEYBOARD_LL) to intercept all keypresses</item>
    ///   <item>When a key is pressed, checks if the foreground window title matches the configured PACS title</item>
    ///   <item>Uses Win32 GetClassName() to detect if the focused element is a text input control</item>
    ///   <item>If NOT a text input ¡æ blocks the key, focuses Radium editor, and replays the key via SendKeys</item>
    ///   <item>If IS a text input ¡æ passes the key through normally (user is typing in worklist, search box, etc.)</item>
    /// </list>
    /// 
    /// <para><b>Why Class Name Detection:</b></para>
    /// Previous approaches using FlaUI bookmark HWND comparison failed because:
    /// <list type="bullet">
    ///   <item>FlaUI calls are too slow (5-50ms) for hook callbacks which must return immediately</item>
    ///   <item>WPF/MFC container elements often share the same native HWND with their children</item>
    /// </list>
    /// Win32 class names (Edit, ComboBox, SysListView32, etc.) are unique per control type and
    /// can be retrieved in microseconds, making them ideal for hook callback detection.
    /// 
    /// <para><b>Key Technologies:</b></para>
    /// <list type="bullet">
    ///   <item><c>SetWindowsHookEx(WH_KEYBOARD_LL)</c> - Global keyboard hook</item>
    ///   <item><c>GetClassName()</c> - Fast Win32 control type detection</item>
    ///   <item><c>GetGUIThreadInfo()</c> - Get the actual focused window handle</item>
    ///   <item><c>SendKeys.SendWait()</c> - Replay keys (bypasses SendInput hook restrictions)</item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// <para><b>CRITICAL:</b> The hook callback must return immediately. Any delay allows Windows
    /// to dispatch the key to the target application before it can be blocked.</para>
    /// 
    /// <para><b>Configuration:</b></para>
    /// <list type="bullet">
    ///   <item><c>EditorAutofocusEnabled</c> - Master enable/disable switch</item>
    ///   <item><c>EditorAutofocusWindowTitle</c> - Partial window title to match (e.g., "INFINITT PACS")</item>
    ///   <item><c>EditorAutofocusKeyTypes</c> - Comma-separated key types: Alphabet, Numbers, Space, Tab, Symbols</item>
    /// </list>
    /// </remarks>
    public sealed class EditorAutofocusService : IDisposable
    {
        private readonly IRadiumLocalSettings _settings;
        private readonly Action _focusEditorCallback;
        private IntPtr _hookHandle = IntPtr.Zero;
        private LowLevelKeyboardProc? _hookProc;
        private bool _isDisposed;

        /// <summary>
        /// Queue for async key processing. Keys are captured in the hook callback (sync)
        /// and processed on the UI thread (async) to avoid blocking the hook.
        /// </summary>
        private readonly ConcurrentQueue<char> _pendingKeys = new();
        private int _isProcessing = 0;

        /// <summary>
        /// Cached window title from settings. Read once at Start() to avoid
        /// accessing settings on every keypress.
        /// </summary>
        private string? _cachedWindowTitle = null;

        // Diagnostic logging - set to false for production, true for debugging
        private const bool ENABLE_DIAGNOSTIC_LOGGING = false;

        // Windows hook constants
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;

        // Modifier virtual keys (used to detect Ctrl, Alt, Win which should NOT trigger autofocus)
        private const int VK_LSHIFT = 0xA0;
        private const int VK_RSHIFT = 0xA1;
        private const int VK_LCONTROL = 0xA2;
        private const int VK_RCONTROL = 0xA3;
        private const int VK_CONTROL = 0x11;
        private const int VK_LMENU = 0xA4;
        private const int VK_RMENU = 0xA5;
        private const int VK_MENU = 0x12;
        private const int VK_LWIN = 0x5B;
        private const int VK_RWIN = 0x5C;

        /// <summary>
        /// Window class names that indicate text input controls.
        /// When the focused element has one of these class names, autofocus is NOT triggered
        /// because the user is typing in that control (worklist search, patient ID field, etc.).
        /// </summary>
        private static readonly HashSet<string> TextInputClassNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "Edit",           // Standard Windows edit control
            "RichEdit",       // Rich text edit
            "RichEdit20A",    // Rich text edit ANSI
            "RichEdit20W",    // Rich text edit Unicode
            "RichEdit50W",    // Rich text edit v5
            "RICHEDIT50W",    // Rich text edit v5 (uppercase)
            "TextBox",        // WPF TextBox
            "ComboBox",       // ComboBox with edit
            "ComboBoxEx32",   // Extended ComboBox
            "SysListView32",  // ListView (worklist - may have inline edit)
            "SysTreeView32",  // TreeView (may have inline edit)
            "Scintilla",      // Scintilla editor control
        };

        #region P/Invoke Declarations

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

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        private static extern int ToUnicode(uint wVirtKey, uint wScanCode, byte[] lpKeyState, StringBuilder pwszBuff, int cchBuff, uint wFlags);

        [DllImport("user32.dll")]
        private static extern bool GetKeyboardState(byte[] lpKeyState);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr processId);

        [DllImport("user32.dll")]
        private static extern bool GetGUIThreadInfo(uint idThread, ref GUITHREADINFO lpgui);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [StructLayout(LayoutKind.Sequential)]
        private struct KBDLLHOOKSTRUCT
        {
            public uint vkCode;
            public uint scanCode;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

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

        #endregion

        /// <summary>
        /// Creates a new instance of the EditorAutofocusService.
        /// </summary>
        /// <param name="settings">Settings provider for configuration values.</param>
        /// <param name="focusEditorCallback">Callback to focus the Radium editor when autofocus triggers.</param>
        public EditorAutofocusService(IRadiumLocalSettings settings, Action focusEditorCallback)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _focusEditorCallback = focusEditorCallback ?? throw new ArgumentNullException(nameof(focusEditorCallback));
        }

        /// <summary>
        /// Starts monitoring keyboard input for autofocus triggers.
        /// Installs a global low-level keyboard hook.
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
                    Debug.WriteLine("[EditorAutofocusService] Autofocus disabled in settings");
                return;
            }

            // Cache window title setting (read once to avoid accessing settings on every keypress)
            _cachedWindowTitle = _settings.EditorAutofocusWindowTitle;

            if (string.IsNullOrWhiteSpace(_cachedWindowTitle))
            {
                if (ENABLE_DIAGNOSTIC_LOGGING)
                    Debug.WriteLine("[EditorAutofocusService] No window title configured, not starting");
                return;
            }

            try
            {
                // Keep delegate alive to prevent GC collection
                _hookProc = HookCallback;

                using var curProcess = Process.GetCurrentProcess();
                using var curModule = curProcess.MainModule;

                _hookHandle = SetWindowsHookEx(WH_KEYBOARD_LL, _hookProc, GetModuleHandle(curModule?.ModuleName ?? ""), 0);

                if (_hookHandle == IntPtr.Zero)
                {
                    var error = Marshal.GetLastWin32Error();
                    Debug.WriteLine($"[EditorAutofocusService] Failed to install hook. Error: {error}");
                }
                else if (ENABLE_DIAGNOSTIC_LOGGING)
                {
                    Debug.WriteLine($"[EditorAutofocusService] Keyboard hook installed. Monitoring for window title: '{_cachedWindowTitle}'");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[EditorAutofocusService] Error starting hook: {ex.Message}");
            }
        }

        /// <summary>
        /// Stops monitoring keyboard input and removes the hook.
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

            // Clear any pending keys
            while (_pendingKeys.TryDequeue(out _)) { }
        }

        /// <summary>
        /// Low-level keyboard hook callback.
        /// <para><b>CRITICAL:</b> This must be extremely fast. Any delay allows Windows to
        /// dispatch the key to the target application before we can block it.</para>
        /// </summary>
        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            try
            {
                if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
                {
                    var hookStruct = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
                    var key = KeyInterop.KeyFromVirtualKey((int)hookStruct.vkCode);

                    // FAST check - native Win32 only, no FlaUI, no slow operations
                    if (ShouldTriggerAutofocusFast(key))
                    {
                        // Don't trigger for Ctrl+, Alt+, Win+ combinations
                        if (HasDisallowedModifier())
                            return CallNextHookEx(_hookHandle, nCode, wParam, lParam);

                        // Get character while keyboard state is valid
                        char keyChar = GetCharFromKey((int)hookStruct.vkCode, (int)hookStruct.scanCode);

                        if (ENABLE_DIAGNOSTIC_LOGGING)
                            Debug.WriteLine($"[EditorAutofocus] BLOCKING key: {key}, char: '{keyChar}'");

                        // Queue for async processing on UI thread
                        if (keyChar != '\0')
                        {
                            _pendingKeys.Enqueue(keyChar);
                            TriggerAsyncProcessing();
                        }

                        // BLOCK the key immediately - return non-zero to consume the key
                        return (IntPtr)1;
                    }
                }
            }
            catch (Exception ex)
            {
                if (ENABLE_DIAGNOSTIC_LOGGING)
                    Debug.WriteLine($"[EditorAutofocus] Hook exception: {ex.Message}");
            }

            // Pass through to next hook
            return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
        }

        /// <summary>
        /// Fast autofocus check using only native Win32 APIs.
        /// <para>Triggers autofocus when:</para>
        /// <list type="number">
        ///   <item>Key type is enabled in settings</item>
        ///   <item>Radium doesn't already have focus</item>
        ///   <item>Window title matches configured PACS title</item>
        ///   <item>Focused control is NOT a text input (Edit, ComboBox, ListView, etc.)</item>
        /// </list>
        /// </summary>
        private bool ShouldTriggerAutofocusFast(Key key)
        {
            // Check if feature is enabled
            if (!_settings.EditorAutofocusEnabled)
                return false;

            // Check if key type is enabled
            if (!IsKeyTypeEnabled(key))
                return false;

            // Get foreground window
            var foregroundHwnd = GetForegroundWindow();
            if (foregroundHwnd == IntPtr.Zero)
                return false;

            // Check if Radium already has focus (don't intercept our own keys)
            try
            {
                var radiumHwnd = new System.Windows.Interop.WindowInteropHelper(
                    System.Windows.Application.Current?.MainWindow).Handle;
                if (foregroundHwnd == radiumHwnd)
                    return false;
            }
            catch { }

            // Check window title matches configured PACS title
            if (string.IsNullOrWhiteSpace(_cachedWindowTitle))
                return false;

            var title = GetWindowTitleFast(foregroundHwnd);
            if (string.IsNullOrEmpty(title) || !title.Contains(_cachedWindowTitle, StringComparison.OrdinalIgnoreCase))
            {
                // Window title doesn't match - not the target PACS app
                return false;
            }

            // Check if focused element is a text input control
            // If it is, DON'T trigger autofocus (user is typing in worklist, search box, etc.)
            var focusedHwnd = GetFocusedHwndFast(foregroundHwnd);
            if (focusedHwnd != IntPtr.Zero)
            {
                var className = GetClassNameFast(focusedHwnd);
                
                if (ENABLE_DIAGNOSTIC_LOGGING)
                    Debug.WriteLine($"[EditorAutofocus] Focused HWND=0x{focusedHwnd:X}, ClassName='{className}'");

                if (IsTextInputControl(className))
                {
                    if (ENABLE_DIAGNOSTIC_LOGGING)
                        Debug.WriteLine($"[EditorAutofocus] Text input control detected ('{className}'), not triggering");
                    return false;
                }
            }

            // All checks passed - trigger autofocus
            if (ENABLE_DIAGNOSTIC_LOGGING)
                Debug.WriteLine($"[EditorAutofocus] Triggering autofocus (title='{title}', focused class not a text input)");

            return true;
        }

        /// <summary>
        /// Check if class name indicates a text input control where the user might be typing.
        /// </summary>
        private static bool IsTextInputControl(string? className)
        {
            if (string.IsNullOrEmpty(className))
                return false;

            // Check exact match against known text input class names
            if (TextInputClassNames.Contains(className))
                return true;

            // Check partial matches for common patterns (e.g., "MyAppEdit", "CustomTextBox")
            if (className.Contains("Edit", StringComparison.OrdinalIgnoreCase))
                return true;
            if (className.Contains("TextBox", StringComparison.OrdinalIgnoreCase))
                return true;
            if (className.Contains("ComboBox", StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }

        /// <summary>
        /// Fast window title retrieval using native Win32 GetWindowText.
        /// </summary>
        private static string GetWindowTitleFast(IntPtr hWnd)
        {
            var sb = new StringBuilder(256);
            GetWindowText(hWnd, sb, sb.Capacity);
            return sb.ToString();
        }

        /// <summary>
        /// Fast class name retrieval using native Win32 GetClassName.
        /// </summary>
        private static string GetClassNameFast(IntPtr hWnd)
        {
            var sb = new StringBuilder(256);
            GetClassName(hWnd, sb, sb.Capacity);
            return sb.ToString();
        }

        /// <summary>
        /// Get the HWND of the actually focused control using GetGUIThreadInfo.
        /// This returns the focused child control, not just the top-level window.
        /// </summary>
        private static IntPtr GetFocusedHwndFast(IntPtr foregroundHwnd)
        {
            uint threadId = GetWindowThreadProcessId(foregroundHwnd, IntPtr.Zero);
            if (threadId == 0)
                return foregroundHwnd;

            var info = new GUITHREADINFO { cbSize = (uint)Marshal.SizeOf<GUITHREADINFO>() };
            if (GetGUIThreadInfo(threadId, ref info))
            {
                if (info.hwndFocus != IntPtr.Zero)
                    return info.hwndFocus;
                if (info.hwndActive != IntPtr.Zero)
                    return info.hwndActive;
            }

            return foregroundHwnd;
        }

        /// <summary>
        /// Triggers async processing of pending keys on the UI thread.
        /// Uses compare-exchange to ensure only one processing task runs at a time.
        /// </summary>
        private void TriggerAsyncProcessing()
        {
            if (Interlocked.CompareExchange(ref _isProcessing, 1, 0) != 0)
                return;

            System.Windows.Application.Current?.Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Input,
                new Action(ProcessPendingKeys));
        }

        /// <summary>
        /// Processes pending keys on the UI thread.
        /// Focuses the editor first, then replays each queued key via SendKeys.
        /// </summary>
        private void ProcessPendingKeys()
        {
            try
            {
                bool focusedEditor = false;

                while (_pendingKeys.TryDequeue(out char keyChar))
                {
                    if (!focusedEditor)
                    {
                        _focusEditorCallback();
                        Thread.Sleep(30); // Wait for focus to be established
                        focusedEditor = true;
                    }

                    if (keyChar != '\0')
                    {
                        string keyString = EscapeForSendKeys(keyChar);
                        if (ENABLE_DIAGNOSTIC_LOGGING)
                            Debug.WriteLine($"[EditorAutofocus] Sending: '{keyString}'");

                        // SendKeys.SendWait bypasses SendInput restrictions from hook contexts
                        System.Windows.Forms.SendKeys.SendWait(keyString);
                    }
                }
            }
            catch (Exception ex)
            {
                if (ENABLE_DIAGNOSTIC_LOGGING)
                    Debug.WriteLine($"[EditorAutofocus] ProcessPendingKeys error: {ex.Message}");
            }
            finally
            {
                Interlocked.Exchange(ref _isProcessing, 0);

                // Check if more keys arrived while processing
                if (!_pendingKeys.IsEmpty)
                    TriggerAsyncProcessing();
            }
        }

        /// <summary>
        /// Escapes special characters for SendKeys format.
        /// Characters like +, ^, %, ~, {, } have special meanings in SendKeys.
        /// </summary>
        private static string EscapeForSendKeys(char keyChar)
        {
            return keyChar switch
            {
                '+' => "{+}",   // Shift modifier
                '^' => "{^}",   // Ctrl modifier
                '%' => "{%}",   // Alt modifier
                '~' => "{~}",   // Enter key
                '(' => "{(}",
                ')' => "{)}",
                '{' => "{{}",
                '}' => "{}}",
                '[' => "{[}",
                ']' => "{]}",
                _ => keyChar.ToString()
            };
        }

        /// <summary>
        /// Converts a virtual key code to a character using the current keyboard state.
        /// </summary>
        private char GetCharFromKey(int virtualKey, int scanCode)
        {
            try
            {
                var keyState = new byte[256];
                if (!GetKeyboardState(keyState))
                    return '\0';

                var buffer = new StringBuilder(8);
                int result = ToUnicode((uint)virtualKey, (uint)scanCode, keyState, buffer, buffer.Capacity, 0);

                if (result > 0)
                    return buffer[0];
            }
            catch { }

            return '\0';
        }

        /// <summary>
        /// Checks if a disallowed modifier key (Ctrl, Alt, Win) is pressed.
        /// We don't want to intercept keyboard shortcuts like Ctrl+C.
        /// </summary>
        private static bool HasDisallowedModifier()
        {
            return IsVirtualKeyPressed(VK_CONTROL) ||
                   IsVirtualKeyPressed(VK_LCONTROL) ||
                   IsVirtualKeyPressed(VK_RCONTROL) ||
                   IsVirtualKeyPressed(VK_MENU) ||
                   IsVirtualKeyPressed(VK_LMENU) ||
                   IsVirtualKeyPressed(VK_RMENU) ||
                   IsVirtualKeyPressed(VK_LWIN) ||
                   IsVirtualKeyPressed(VK_RWIN);
        }

        /// <summary>
        /// Checks if a virtual key is currently pressed using GetAsyncKeyState.
        /// </summary>
        private static bool IsVirtualKeyPressed(int virtualKey)
        {
            return (GetAsyncKeyState(virtualKey) & 0x8000) != 0;
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

            if (enabledTypes.Contains("Alphabet") && key >= Key.A && key <= Key.Z)
                return true;
            if (enabledTypes.Contains("Numbers") && ((key >= Key.D0 && key <= Key.D9) || (key >= Key.NumPad0 && key <= Key.NumPad9)))
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
        /// Checks if the key is a symbol/punctuation key.
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

        /// <summary>
        /// Disposes the service and removes the keyboard hook.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
                return;

            Stop();
            _isDisposed = true;
        }
    }
}
