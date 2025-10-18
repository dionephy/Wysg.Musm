using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace Wysg.Musm.Radium.Services
{
    /// <summary>
    /// Service for one-way text synchronization from a foreign textbox (e.g., Notepad) to the app.
    /// Uses polling to detect changes from the foreign textbox.
    /// </summary>
    public sealed class TextSyncService : IDisposable
    {
        private readonly Dispatcher _dispatcher;
        private Timer? _pollTimer;
        private bool _isEnabled;
        private string _lastKnownForeignText = string.Empty;
        
        private const int PollIntervalMs = 2000; // Poll every 2000ms (2 seconds)
        
        public event EventHandler<string>? ForeignTextChanged;
        
        public TextSyncService(Dispatcher dispatcher)
        {
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        }
        
        /// <summary>
        /// Enable or disable text synchronization.
        /// When enabled, starts polling timer. When disabled, stops timer and clears state.
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            if (_isEnabled == enabled) return;
            
            _isEnabled = enabled;
            
            if (enabled)
            {
                Debug.WriteLine("[TextSync] Sync enabled - starting poll timer");
                _pollTimer = new Timer(PollCallback, null, PollIntervalMs, PollIntervalMs);
                // Initialize with current foreign text
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var text = await ReadForeignTextAsync();
                        _lastKnownForeignText = text ?? string.Empty;
                        Debug.WriteLine($"[TextSync] Initial foreign text: {_lastKnownForeignText.Length} chars");
                        
                        // Notify immediately with initial text
                        await _dispatcher.InvokeAsync(() =>
                        {
                            ForeignTextChanged?.Invoke(this, _lastKnownForeignText);
                        });
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[TextSync] Initial read error: {ex.Message}");
                    }
                });
            }
            else
            {
                Debug.WriteLine("[TextSync] Sync disabled - stopping poll timer");
                _pollTimer?.Dispose();
                _pollTimer = null;
                _lastKnownForeignText = string.Empty;
            }
        }
        
        private void PollCallback(object? state)
        {
            if (!_isEnabled) return;
            
            _ = Task.Run(async () =>
            {
                try
                {
                    var currentForeignText = await ReadForeignTextAsync();
                    if (currentForeignText == null) return;
                    
                    // Check if foreign text changed
                    if (currentForeignText != _lastKnownForeignText)
                    {
                        Debug.WriteLine($"[TextSync] Foreign text changed: {_lastKnownForeignText.Length} -> {currentForeignText.Length} chars");
                        _lastKnownForeignText = currentForeignText;
                        
                        // Notify on dispatcher thread
                        await _dispatcher.InvokeAsync(() =>
                        {
                            ForeignTextChanged?.Invoke(this, currentForeignText);
                        });
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[TextSync] Poll error: {ex.Message}");
                }
            });
        }
        
        private async Task<string?> ReadForeignTextAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    var (hwnd, element) = UiBookmarks.Resolve(UiBookmarks.KnownControl.ForeignTextbox);
                    if (element == null) return null;
                    
                    // Try ValuePattern first
                    if (element.Patterns.Value.IsSupported)
                    {
                        var valuePattern = element.Patterns.Value.Pattern;
                        if (valuePattern != null)
                        {
                            return valuePattern.Value.ValueOrDefault;
                        }
                    }
                    
                    // Fallback: Try Text pattern
                    if (element.Patterns.Text.IsSupported)
                    {
                        var textPattern = element.Patterns.Text.Pattern;
                        if (textPattern != null)
                        {
                            return textPattern.DocumentRange.GetText(-1);
                        }
                    }
                    
                    // Last resort: try Name property (works for some controls)
                    return element.Name;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[TextSync] Read error: {ex.Message}");
                    return null;
                }
            });
        }
        
        /// <summary>
        /// Write text to foreign textbox using UIA patterns.
        /// </summary>
        /// <param name="avoidFocus">When true, attempts to avoid setting focus (best-effort)</param>
        public async Task<bool> WriteToForeignAsync(string text, bool avoidFocus = true)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var (hwnd, element) = UiBookmarks.Resolve(UiBookmarks.KnownControl.ForeignTextbox);
                    if (element == null)
                    {
                        Debug.WriteLine("[TextSync] Write failed: element not found");
                        return false;
                    }
                    
                    // Try ValuePattern first (most reliable)
                    if (element.Patterns.Value.IsSupported)
                    {
                        var valuePattern = element.Patterns.Value.Pattern;
                        if (valuePattern != null && !valuePattern.IsReadOnly)
                        {
                            // IMPORTANT: We do NOT call SetFocus() here to prevent focus stealing
                            // However, some applications (like Notepad) may still bring themselves
                            // to the foreground when their content changes via UIA ValuePattern.
                            // This is application-specific behavior and cannot be fully prevented.
                            // 
                            // Alternative approaches (not implemented):
                            // 1. SendKeys without focus: unreliable, requires exact focus state
                            // 2. Windows messages: application-specific, not portable
                            // 3. Clipboard + Paste: too invasive, disrupts user clipboard
                            //
                            // Current approach is the least invasive option available via UIA.
                            
                            valuePattern.SetValue(text ?? string.Empty);
                            Debug.WriteLine($"[TextSync] Wrote {text?.Length ?? 0} chars via ValuePattern (focus may have changed)");
                            return true;
                        }
                    }
                    
                    Debug.WriteLine("[TextSync] Write failed: ValuePattern not supported or read-only");
                    return false;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[TextSync] Write error: {ex.Message}");
                    return false;
                }
            });
        }
        
        public void Dispose()
        {
            SetEnabled(false);
        }
    }
}
