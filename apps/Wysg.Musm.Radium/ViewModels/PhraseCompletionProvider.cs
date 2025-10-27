using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.CodeCompletion;
using Wysg.Musm.Editor.Snippets;
using Wysg.Musm.Radium.Services;
using System.Collections.Concurrent;

namespace Wysg.Musm.Radium.ViewModels
{
    /// <summary>
    /// Supplies completion items (tokens) from the central phrase database.
    /// Implements ISnippetProvider to plug into EditorControl per spec_editor.md.
    /// Only letters are treated as word characters (editor behavior already enforces this).
    /// Now uses combined phrases (global + account-specific) for completion (FR-274, T385).
    /// </summary>
    internal sealed class PhraseCompletionProvider : ISnippetProvider
    {
        private readonly IPhraseService _svc;
        private readonly ITenantContext _ctx;
        private readonly IPhraseCache _cache;
        private volatile bool _prefetching;

        // Tracks whether we've already loaded the combined (global+account) list into cache for a given account.
        private readonly ConcurrentDictionary<long, DateTime> _combinedReadyAt = new();
        private static readonly TimeSpan RefreshTtl = TimeSpan.FromMinutes(2); // periodic background refresh

        public PhraseCompletionProvider(IPhraseService svc, ITenantContext ctx, IPhraseCache cache)
        { _svc = svc; _ctx = ctx; _cache = cache; }

        public IEnumerable<ICompletionData> GetCompletions(TextEditor editor)
        {
            var (prefix, _) = GetWordBeforeCaret(editor);
            
            Debug.WriteLine($"[PhraseCompletion][GetCompletions] prefix='{prefix}', length={prefix?.Length ?? 0}");
            
            if (string.IsNullOrEmpty(prefix)) 
            {
                Debug.WriteLine("[PhraseCompletion][GetCompletions] Empty prefix, yielding break");
                yield break;
            }

            long accountId = _ctx.AccountId; // alias for former tenant id

            // If combined list is not known to be loaded (or TTL expired), start a background prefetch to combined list.
            if (!_combinedReadyAt.TryGetValue(accountId, out var when) || (DateTime.UtcNow - when) > RefreshTtl)
            {
                Debug.WriteLine($"[PhraseCompletion][GetCompletions] Cache expired or not loaded, starting prefetch");
                TryStartPrefetch(accountId, allowRetryForEmpty: true);
            }

            // Ensure cache seeded asynchronously (single flight) if we have no cache yet
            if (!_cache.Has(accountId))
            {
                Debug.WriteLine($"[PhraseCompletion][GetCompletions] Cache not ready for account={accountId}, starting prefetch and yielding");
                TryStartPrefetch(accountId);
                yield break; // not yet
            }

            var list = _cache.Get(accountId);
            Debug.WriteLine($"[PhraseCompletion][GetCompletions] Cache has {list.Count} phrases for account={accountId}");
            
            if (list.Count == 0)
            {
                // Empty cached list likely from early snapshot call; re-fetch later.
                Debug.WriteLine("[PhraseCompletion][GetCompletions] Empty cache, retrying prefetch");
                TryStartPrefetch(accountId, allowRetryForEmpty: true);
                yield break;
            }

            // Debug: Check if "vein of calf" is in the list
            var hasVeinOfCalf = list.Any(p => p.Contains("vein", StringComparison.OrdinalIgnoreCase) && p.Contains("calf", StringComparison.OrdinalIgnoreCase));
            if (hasVeinOfCalf)
            {
                var veinPhrases = list.Where(p => p.Contains("vein", StringComparison.OrdinalIgnoreCase)).Take(10).ToList();
                Debug.WriteLine($"[PhraseCompletion][GetCompletions] ? Found 'vein' phrases in cache: {string.Join(", ", veinPhrases.Select(p => $"\"{p}\""))}");
            }
            else
            {
                Debug.WriteLine("[PhraseCompletion][GetCompletions] ? 'vein of calf' NOT found in cache");
            }

            // Simple prefix filter with stable ordering (shorter first, then lexicographic)
            var matches = list.Where(t => t.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                              .OrderBy(t => t.Length).ThenBy(t => t)
                              .ToList();
            
            Debug.WriteLine($"[PhraseCompletion][GetCompletions] Returning {matches.Count} matches for prefix '{prefix}'");
            
            if (matches.Count > 0 && matches.Count <= 5)
            {
                Debug.WriteLine($"[PhraseCompletion][GetCompletions] Matches: {string.Join(", ", matches.Select(m => $"\"{m}\""))}");
            }
            
            foreach (var t in matches)
            {
                yield return MusmCompletionData.Token(t);
            }
        }

        private void TryStartPrefetch(long accountId, bool allowRetryForEmpty = false)
        {
            if (_prefetching) return;
            _prefetching = true;
            _ = Task.Run(async () =>
            {
                try
                {
                    Debug.WriteLine($"[PhraseCompletion][Prefetch] Starting prefetch for account={accountId}");
                    
                    // Use combined phrases (global + account-specific) for completion
                    var all = await _svc.GetCombinedPhrasesAsync(accountId);
                    
                    Debug.WriteLine($"[PhraseCompletion][Prefetch] Received {all.Count} combined phrases");
                    
                    if (all.Count > 0)
                    {
                        // Debug: Check if "vein of calf" is in the loaded phrases
                        var hasVeinOfCalf = all.Any(p => p.Contains("vein", StringComparison.OrdinalIgnoreCase) && p.Contains("calf", StringComparison.OrdinalIgnoreCase));
                        if (hasVeinOfCalf)
                        {
                            var veinPhrases = all.Where(p => p.Contains("vein", StringComparison.OrdinalIgnoreCase)).Take(10).ToList();
                            Debug.WriteLine($"[PhraseCompletion][Prefetch] ? 'vein' phrases found: {string.Join(", ", veinPhrases.Select(p => $"\"{p}\""))}");
                        }
                        else
                        {
                            Debug.WriteLine("[PhraseCompletion][Prefetch] ? 'vein of calf' NOT in loaded phrases");
                        }
                        
                        _cache.Set(accountId, all);
                        _combinedReadyAt[accountId] = DateTime.UtcNow;
                        Debug.WriteLine($"[PhraseCompletion][Prefetch] Cached {all.Count} combined phrases (global + account) for account={accountId}");
                    }
                    else if (allowRetryForEmpty)
                    {
                        Debug.WriteLine("[PhraseCompletion][Prefetch] Empty result, retrying after delay");
                        // simple delayed retry
                        await Task.Delay(750);
                        var again = await _svc.GetCombinedPhrasesAsync(accountId);
                        Debug.WriteLine($"[PhraseCompletion][Prefetch] Retry received {again.Count} phrases");
                        
                        if (again.Count > 0)
                        {
                            // Debug: Check retry results
                            var hasVeinOfCalf2 = again.Any(p => p.Contains("vein", StringComparison.OrdinalIgnoreCase) && p.Contains("calf", StringComparison.OrdinalIgnoreCase));
                            if (hasVeinOfCalf2)
                            {
                                var veinPhrases2 = again.Where(p => p.Contains("vein", StringComparison.OrdinalIgnoreCase)).Take(10).ToList();
                                Debug.WriteLine($"[PhraseCompletion][Prefetch][Retry] ? 'vein' phrases found: {string.Join(", ", veinPhrases2.Select(p => $"\"{p}\""))}");
                            }
                            else
                            {
                                Debug.WriteLine("[PhraseCompletion][Prefetch][Retry] ? 'vein of calf' NOT in retry results");
                            }
                            
                            _cache.Set(accountId, again);
                            _combinedReadyAt[accountId] = DateTime.UtcNow;
                            Debug.WriteLine($"[PhraseCompletion][Prefetch] Retry cached {again.Count} combined phrases for account={accountId}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[PhraseCompletion][Prefetch] Error: {ex.Message}");
                    Debug.WriteLine($"[PhraseCompletion][Prefetch] Stack: {ex.StackTrace}");
                }
                finally { _prefetching = false; }
            });
        }

        private static (string word, int startOffset) GetWordBeforeCaret(TextEditor editor)
        {
            int caret = editor.CaretOffset;
            var line = editor.Document.GetLineByOffset(caret);
            var text = editor.Document.GetText(line.Offset, caret - line.Offset);

            int i = text.Length - 1;
            while (i >= 0 && char.IsLetter(text[i])) i--;
            int start = line.Offset + i + 1;
            string word = editor.Document.GetText(start, caret - start);
            return (word, start);
        }
    }
}
