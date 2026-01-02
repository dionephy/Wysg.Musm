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
        private readonly Func<string?> _getTextboxBookmarkName;
        private const string DefaultBookmarkName = "ForeignTextbox";
        private Timer? _pollTimer;
        private bool _isEnabled;
        private string _lastKnownForeignText = string.Empty;
        
        private const int PollIntervalMs = 2000; // Poll every 2000ms (2 seconds)
        
        public event EventHandler<string>? ForeignTextChanged;
        
        public TextSyncService(Dispatcher dispatcher, Func<string?>? getTextboxBookmarkName = null)
        {
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            _getTextboxBookmarkName = getTextboxBookmarkName ?? (() => DefaultBookmarkName);
        }

        private string ResolveBookmarkName()
        {
            try
            {
                var name = _getTextboxBookmarkName();
                if (string.IsNullOrWhiteSpace(name)) return DefaultBookmarkName;
                return name;
            }
            catch
            {
                return DefaultBookmarkName;
            }
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
                    var bookmarkName = ResolveBookmarkName();
                    var (_, element) = UiBookmarks.Resolve(bookmarkName);
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
        /// <param name="text">Text to write to foreign textbox</param>
        /// <param name="avoidFocus">When true, attempts to avoid setting focus (best-effort)</param>
        /// <param name="afterFocusCallback">Optional callback to invoke after foreign element receives focus (on dispatcher thread)</param>
        public async Task<bool> WriteToForeignAsync(string text, bool avoidFocus = true, Action? afterFocusCallback = null)
        {
            var result = await Task.Run(() =>
            {
                try
                {
                    var bookmarkName = ResolveBookmarkName();
                    var (_, element) = UiBookmarks.Resolve(bookmarkName);
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
            
            // Invoke callback on dispatcher thread after write completes (if successful and callback provided)
            if (result && afterFocusCallback != null)
            {
                // Delay callback slightly to ensure foreign element has finished processing focus change
                await Task.Delay(150);
                await _dispatcher.InvokeAsync(afterFocusCallback);
            }
            
            return result;
        }
        
        public void Dispose()
        {
            SetEnabled(false);
        }
    }
}
