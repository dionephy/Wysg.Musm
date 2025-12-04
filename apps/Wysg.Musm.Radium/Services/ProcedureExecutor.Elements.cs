using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using FlaUI.Core.AutomationElements;

namespace Wysg.Musm.Radium.Services
{
    internal static partial class ProcedureExecutor
    {
        // CHANGED: Now two separate caches
        // Global cache: Persists across sessions (most bookmarks - for speed)
        private static readonly Dictionary<string, AutomationElement> _globalControlCache = new();
        
        // Session cache: Cleared on each automation run (for dynamic/changing elements)
        private static readonly Dictionary<string, AutomationElement> _sessionControlCache = new();
        
        // Runtime element cache for storing elements from GetSelectedElement (always session-based)
        private static readonly Dictionary<string, AutomationElement> _elementCache = new();
        
        // Session tracking: Track which session the cache belongs to
        // When session ID changes, session caches are cleared (but global cache persists)
        private static string? _currentSessionId = null;
        
        // Cached set of session-based bookmark names (loaded from settings)
        // PERFORMANCE: Cache for 60 seconds to avoid repeated settings file reads
        private static HashSet<string>? _sessionBasedBookmarkNames = null;
        private static DateTime _sessionBasedBookmarkNamesLastLoaded = DateTime.MinValue;
        private const int SessionBasedBookmarkCacheSeconds = 60;
        
        // Element resolution with staleness detection and retry (inspired by legacy PacsService validation pattern)
        // PERFORMANCE: Reduced from 3 to 1 - if element doesn't exist, don't waste time retrying
        private const int ElementResolveMaxAttempts = 3;
        private const int ElementResolveRetryDelayMs = 150;

        // Diagnostic: Track timing for performance analysis
        private static readonly Dictionary<string, (int hitCount, int missCount, long totalResolveMs)> _bookmarkStats = new();

        /// <summary>
        /// Get diagnostic statistics for bookmark resolution performance.
        /// </summary>
        internal static string GetCacheStatistics()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"=== Bookmark Cache Statistics ===");
            sb.AppendLine($"Global cache entries: {_globalControlCache.Count}");
            sb.AppendLine($"Session cache entries: {_sessionControlCache.Count}");
            sb.AppendLine($"Element cache entries: {_elementCache.Count}");
            sb.AppendLine();
            sb.AppendLine("Per-bookmark stats:");
            foreach (var kvp in _bookmarkStats.OrderByDescending(x => x.Value.totalResolveMs))
            {
                var (hits, misses, totalMs) = kvp.Value;
                var avgMs = (hits + misses) > 0 ? totalMs / (hits + misses) : 0;
                sb.AppendLine($"  {kvp.Key}: hits={hits}, misses={misses}, totalMs={totalMs}, avgMs={avgMs}");
            }
            return sb.ToString();
        }

        /// <summary>
        /// Set the current session ID. When this changes, session caches will be cleared (global cache persists).
        /// Call this at the START of an automation sequence, NOT for each procedure within the sequence.
        /// </summary>
        internal static void SetSessionId(string sessionId)
        {
            if (_currentSessionId != sessionId)
            {
                Debug.WriteLine($"[ProcedureExecutor][SetSessionId] Session changed: '{_currentSessionId}' -> '{sessionId}'");
                _currentSessionId = sessionId;
                ClearSessionCaches();
            }
        }

        /// <summary>
        /// Clear session-based caches only. Global cache persists for performance.
        /// </summary>
        internal static void ClearSessionCaches()
        {
            _elementCache.Clear();
            _sessionControlCache.Clear();
            Debug.WriteLine($"[ProcedureExecutor][ClearSessionCaches] Session caches cleared (global cache has {_globalControlCache.Count} entries)");
        }

        /// <summary>
        /// Clear all caches including global cache. Use sparingly (e.g., when PACS process restarts).
        /// </summary>
        internal static void ClearAllCaches()
        {
            _elementCache.Clear();
            _sessionControlCache.Clear();
            _globalControlCache.Clear();
            _bookmarkStats.Clear();
            Debug.WriteLine($"[ProcedureExecutor][ClearAllCaches] All caches cleared (global + session)");
        }

        /// <summary>
        /// Check if a bookmark should use session-based caching (cleared each automation run).
        /// Returns true if bookmark is in the user-configured session-based list.
        /// PERFORMANCE: Caches the settings for 60 seconds to avoid repeated file reads.
        /// </summary>
        private static bool IsSessionBasedBookmark(string bookmarkName)
        {
            // PERFORMANCE: Only reload settings every 60 seconds
            if (_sessionBasedBookmarkNames == null || 
                (DateTime.Now - _sessionBasedBookmarkNamesLastLoaded).TotalSeconds > SessionBasedBookmarkCacheSeconds)
            {
                LoadSessionBasedBookmarkNames();
            }
            
            return _sessionBasedBookmarkNames?.Contains(bookmarkName, StringComparer.OrdinalIgnoreCase) ?? false;
        }

        /// <summary>
        /// Load the list of session-based bookmark names from settings.
        /// </summary>
        private static void LoadSessionBasedBookmarkNames()
        {
            try
            {
                var app = (App)System.Windows.Application.Current;
                var localSettings = app?.Services?.GetService(typeof(IRadiumLocalSettings)) as IRadiumLocalSettings;
                var settingsValue = localSettings?.SessionBasedCacheBookmarks;
                
                if (string.IsNullOrWhiteSpace(settingsValue))
                {
                    _sessionBasedBookmarkNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                }
                else
                {
                    _sessionBasedBookmarkNames = new HashSet<string>(
                        settingsValue.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => s.Trim())
                            .Where(s => !string.IsNullOrWhiteSpace(s)),
                        StringComparer.OrdinalIgnoreCase
                    );
                }
                
                _sessionBasedBookmarkNamesLastLoaded = DateTime.Now;
                Debug.WriteLine($"[ProcedureExecutor][LoadSessionBasedBookmarkNames] Loaded {_sessionBasedBookmarkNames.Count} session-based bookmarks: [{string.Join(", ", _sessionBasedBookmarkNames)}]");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ProcedureExecutor][LoadSessionBasedBookmarkNames] Error: {ex.Message}");
                _sessionBasedBookmarkNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                _sessionBasedBookmarkNamesLastLoaded = DateTime.Now;
            }
        }

        /// <summary>
        /// FAST staleness check - avoids cross-process COM calls when possible.
        /// Uses try-catch to handle COM exceptions gracefully.
        /// </summary>
        private static bool IsElementAliveFast(AutomationElement el)
        {
            try
            {
                // FAST PATH: Try to access a property that FlaUI may have cached locally
                // ControlType is often cached and doesn't require a round-trip
                _ = el.ControlType;
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool IsWindow(IntPtr hWnd);

        private static AutomationElement? GetCached(string bookmarkName, out bool wasHit)
        {
            wasHit = false;
            
            // PERFORMANCE: Compute isSessionBased once and reuse
            var isSessionBased = IsSessionBasedBookmark(bookmarkName);
            var cache = isSessionBased ? _sessionControlCache : _globalControlCache;
            
            if (cache.TryGetValue(bookmarkName, out var el))
            {
                // PERFORMANCE FIX: Use fast staleness check
                if (IsElementAliveFast(el))
                {
                    wasHit = true;
                    Debug.WriteLine($"[ProcedureExecutor][GetCached] *** CACHE HIT *** for '{bookmarkName}' (session-based={isSessionBased}, globalCount={_globalControlCache.Count}, sessionCount={_sessionControlCache.Count})");
                    return el; 
                }
                else
                {
                    cache.Remove(bookmarkName);
                    Debug.WriteLine($"[ProcedureExecutor][GetCached] Cache STALE for '{bookmarkName}', removed");
                }
            }
            else
            {
                Debug.WriteLine($"[ProcedureExecutor][GetCached] *** CACHE MISS *** for '{bookmarkName}' (session-based={isSessionBased}, globalCount={_globalControlCache.Count}, sessionCount={_sessionControlCache.Count})");
            }
            return null;
        }

        private static void StoreCache(string bookmarkName, AutomationElement el) 
        { 
            if (el != null)
            {
                // PERFORMANCE: Compute isSessionBased once
                var isSessionBased = IsSessionBasedBookmark(bookmarkName);
                var cache = isSessionBased ? _sessionControlCache : _globalControlCache;
                cache[bookmarkName] = el;
                Debug.WriteLine($"[ProcedureExecutor][StoreCache] Cached '{bookmarkName}' (session-based={isSessionBased}, globalCount={_globalControlCache.Count}, sessionCount={_sessionControlCache.Count})");
            }
        }

        /// <summary>
        /// Full staleness check with property access - use only when strict validation is needed.
        /// This is slower than IsElementAliveFast but more thorough.
        /// </summary>
        private static bool IsElementAlive(AutomationElement el)
        {
            try
            {
                // First try fast check
                if (!IsElementAliveFast(el))
                    return false;
                
                // For strict validation, also verify we can read Name (cross-process call)
                _ = el.Name;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static AutomationElement? ResolveElement(ProcArg arg, Dictionary<string, string?> vars)
        {
            return ResolveElementInternal(arg, vars, ElementResolveMaxAttempts);
        }

        /// <summary>
        /// Resolve element with only a single attempt (no retries).
        /// Use this for operations where you expect the element might not exist and want to fail fast.
        /// </summary>
        private static AutomationElement? ResolveElementOnce(ProcArg arg, Dictionary<string, string?> vars)
        {
            return ResolveElementInternal(arg, vars, maxAttempts: 1);
        }

        private static AutomationElement? ResolveElementInternal(ProcArg arg, Dictionary<string, string?> vars, int maxAttempts)
        {
            var type = ParseArgKind(arg.Type);
            
            // Handle Element type (bookmark-based resolution)
            if (type == ArgKind.Element)
            {
                var tag = arg.Value ?? string.Empty;
                var sw = Stopwatch.StartNew();
                bool cacheHit = false;
                
                Debug.WriteLine($"[ProcedureExecutor][ResolveElement] START resolving bookmark '{tag}' (maxAttempts={maxAttempts})");
                
                // Simplified: All bookmarks resolved by name (no enum parsing)
                // Strategy: Try cache first, validate it, then resolve fresh with retry on staleness
                for (int attempt = 0; attempt < maxAttempts; attempt++)
                {
                    // Attempt 1: Check cache (uses fast staleness check internally)
                    var cached = GetCached(tag, out cacheHit);
                    if (cached != null)
                    {
                        sw.Stop();
                        
                        // Update stats
                        if (!_bookmarkStats.ContainsKey(tag))
                            _bookmarkStats[tag] = (0, 0, 0);
                        var (h, m, t) = _bookmarkStats[tag];
                        _bookmarkStats[tag] = (h + 1, m, t + sw.ElapsedMilliseconds);
                        
                        Debug.WriteLine($"[ProcedureExecutor][ResolveElement] END '{tag}' - CACHE HIT, total={sw.ElapsedMilliseconds}ms");
                        return cached; // Cache hit, element valid
                    }

                    // Attempt 2: Resolve fresh from bookmark by name
                    try
                    {
                        var resolveSw = Stopwatch.StartNew();
                        Debug.WriteLine($"[ProcedureExecutor][ResolveElement] '{tag}' - calling UiBookmarks.Resolve (attempt {attempt + 1}/{maxAttempts})...");
                        var tuple = UiBookmarks.Resolve(tag);
                        resolveSw.Stop();
                        Debug.WriteLine($"[ProcedureExecutor][ResolveElement] '{tag}' - UiBookmarks.Resolve took {resolveSw.ElapsedMilliseconds}ms, element={tuple.element != null}");
                        
                        if (tuple.element != null)
                        {
                            // Validate the newly resolved element before caching (use fast check)
                            if (IsElementAliveFast(tuple.element))
                            {
                                StoreCache(tag, tuple.element);
                                sw.Stop();
                                
                                // Update stats
                                if (!_bookmarkStats.ContainsKey(tag))
                                    _bookmarkStats[tag] = (0, 0, 0);
                                var (h, m, t) = _bookmarkStats[tag];
                                _bookmarkStats[tag] = (h, m + 1, t + sw.ElapsedMilliseconds);
                                
                                Debug.WriteLine($"[ProcedureExecutor][ResolveElement] END '{tag}' - FRESH RESOLVE, resolve={resolveSw.ElapsedMilliseconds}ms, total={sw.ElapsedMilliseconds}ms");
                                return tuple.element;
                            }
                            Debug.WriteLine($"[ProcedureExecutor][ResolveElement] '{tag}' - newly resolved element NOT alive");

                            // Element resolved but not alive - retry with delay (only if more attempts remain)
                            if (attempt < maxAttempts - 1)
                            {
                                System.Threading.Tasks.Task.Delay(ElementResolveRetryDelayMs).Wait();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[ProcedureExecutor][ResolveElement] '{tag}' - exception on attempt {attempt + 1}: {ex.Message}");
                    }
                }
                
                sw.Stop();
                Debug.WriteLine($"[ProcedureExecutor][ResolveElement] END '{tag}' - FAILED after {maxAttempts} attempts, total={sw.ElapsedMilliseconds}ms");
                
                // All attempts exhausted
                return null;
            }
            
            // Handle Var type (variable containing cached element reference)
            if (type == ArgKind.Var)
            {
                var varName = arg.Value ?? string.Empty;
                Debug.WriteLine($"[ResolveElement][Var] Resolving variable '{varName}'");
                
                // First resolve the variable value from vars dictionary
                if (!vars.TryGetValue(varName, out var varValue) || string.IsNullOrWhiteSpace(varValue))
                {
                    Debug.WriteLine($"[ResolveElement][Var] Variable '{varName}' not found in vars dictionary or is empty");
                    return null;
                }
                
                Debug.WriteLine($"[ResolveElement][Var] Variable '{varName}' resolved to: '{varValue}'");
                
                // Check if this variable contains a cached element reference
                if (_elementCache.TryGetValue(varValue, out var cachedElement))
                {
                    Debug.WriteLine($"[ResolveElement][Var] Found cached element for key '{varValue}'");
                    // Validate element is still alive (use fast check)
                    if (IsElementAliveFast(cachedElement))
                    {
                        Debug.WriteLine($"[ResolveElement][Var] Cached element is still alive");
                        return cachedElement;
                    }
                    else
                    {
                        // Element is stale, remove from cache
                        Debug.WriteLine($"[ResolveElement][Var] Cached element is stale, removing from cache");
                        _elementCache.Remove(varValue);
                        return null;
                    }
                }
                else
                {
                    Debug.WriteLine($"[ResolveElement][Var] No cached element found for key '{varValue}'");
                }
            }
            
            return null;
        }

        private static string ResolveString(ProcArg arg, Dictionary<string, string?> vars)
        {
            var type = ParseArgKind(arg.Type);
            
            if (type == ArgKind.Element)
            {
                // For ArgKind.Element, resolve using the elements dictionary
                var key = arg.Value ?? string.Empty;
                return _elementCache.TryGetValue(key, out var el) && el != null ? el.Name : string.Empty;
            }
            
            if (type == ArgKind.Var)
            {
                // For ArgKind.Var, look up value in vars dictionary
                vars.TryGetValue(arg.Value ?? string.Empty, out var value);
                return value ?? string.Empty;
            }
            
            // For String and Number types, return the value directly
            return arg.Value ?? string.Empty;
        }

        private static ArgKind ParseArgKind(string s)
        {
            return s.Trim() switch
            {
                "Element" => ArgKind.Element,
                "String" => ArgKind.String,
                "Number" => ArgKind.Number,
                "Var" => ArgKind.Var,
                _ => ArgKind.String,
            };
        }
    }
}
