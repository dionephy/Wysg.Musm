using System;
using System.Collections.Generic;
using FlaUI.Core.AutomationElements;

namespace Wysg.Musm.Radium.Services
{
    internal static partial class ProcedureExecutor
    {
        // Changed from Dictionary<KnownControl, AutomationElement> to Dictionary<string, AutomationElement>
        // All bookmarks now resolved by name (string key)
        private static readonly Dictionary<string, AutomationElement> _controlCache = new();
        
        // Runtime element cache for storing elements from GetSelectedElement
        private static readonly Dictionary<string, AutomationElement> _elementCache = new();
        
        // Element resolution with staleness detection and retry (inspired by legacy PacsService validation pattern)
        private const int ElementResolveMaxAttempts = 3;
        private const int ElementResolveRetryDelayMs = 150;

        private static AutomationElement? GetCached(string bookmarkName)
        {
            if (_controlCache.TryGetValue(bookmarkName, out var el))
            {
                try { _ = el.Name; return el; } catch { _controlCache.Remove(bookmarkName); }
            }
            return null;
        }

        private static void StoreCache(string bookmarkName, AutomationElement el) 
        { 
            if (el != null) _controlCache[bookmarkName] = el; 
        }

        private static bool IsElementAlive(AutomationElement el)
        {
            try
            {
                // Test if element is still accessible by checking Name property
                _ = el.Name;
                
                // Additional validation: check if element has valid bounds
                var rect = el.BoundingRectangle;
                return true; // Element is accessible
            }
            catch
            {
                return false; // Element is stale or not accessible
            }
        }

        private static AutomationElement? ResolveElement(ProcArg arg, Dictionary<string, string?> vars)
        {
            var type = ParseArgKind(arg.Type);
            
            // Handle Element type (bookmark-based resolution)
            if (type == ArgKind.Element)
            {
                var tag = arg.Value ?? string.Empty;
                
                // Simplified: All bookmarks resolved by name (no enum parsing)
                // Strategy: Try cache first, validate it, then resolve fresh with retry on staleness
                for (int attempt = 0; attempt < ElementResolveMaxAttempts; attempt++)
                {
                    // Attempt 1: Check cache
                    var cached = GetCached(tag);
                    if (cached != null)
                    {
                        if (IsElementAlive(cached))
                        {
                            return cached; // Cache hit, element valid
                        }
                        else
                        {
                            // Stale element in cache, remove it
                            _controlCache.Remove(tag);
                        }
                    }

                    // Attempt 2: Resolve fresh from bookmark by name
                    try
                    {
                        var tuple = UiBookmarks.Resolve(tag);
                        if (tuple.element != null)
                        {
                            // Validate the newly resolved element before caching
                            if (IsElementAlive(tuple.element))
                            {
                                StoreCache(tag, tuple.element);
                                return tuple.element;
                            }

                            // Element resolved but not alive - retry with delay
                            System.Threading.Tasks.Task.Delay(ElementResolveRetryDelayMs).Wait();
                        }
                    }
                    catch { }
                }
                
                // All attempts exhausted
                return null;
            }
            
            // Handle Var type (variable containing cached element reference)
            if (type == ArgKind.Var)
            {
                var varName = arg.Value ?? string.Empty;
                System.Diagnostics.Debug.WriteLine($"[ResolveElement][Var] Resolving variable '{varName}'");
                
                // First resolve the variable value from vars dictionary
                if (!vars.TryGetValue(varName, out var varValue) || string.IsNullOrWhiteSpace(varValue))
                {
                    System.Diagnostics.Debug.WriteLine($"[ResolveElement][Var] Variable '{varName}' not found in vars dictionary or is empty");
                    return null;
                }
                
                System.Diagnostics.Debug.WriteLine($"[ResolveElement][Var] Variable '{varName}' resolved to: '{varValue}'");
                
                // Check if this variable contains a cached element reference
                if (_elementCache.TryGetValue(varValue, out var cachedElement))
                {
                    System.Diagnostics.Debug.WriteLine($"[ResolveElement][Var] Found cached element for key '{varValue}'");
                    // Validate element is still alive
                    if (IsElementAlive(cachedElement))
                    {
                        System.Diagnostics.Debug.WriteLine($"[ResolveElement][Var] Cached element is still alive");
                        return cachedElement;
                    }
                    else
                    {
                        // Element is stale, remove from cache
                        System.Diagnostics.Debug.WriteLine($"[ResolveElement][Var] Cached element is stale, removing from cache");
                        _elementCache.Remove(varValue);
                        return null;
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[ResolveElement][Var] No cached element found for key '{varValue}'");
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
