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
            if (string.IsNullOrEmpty(prefix)) yield break;

            long accountId = _ctx.AccountId; // alias for former tenant id

            // If combined list is not known to be loaded (or TTL expired), start a background prefetch to combined list.
            if (!_combinedReadyAt.TryGetValue(accountId, out var when) || (DateTime.UtcNow - when) > RefreshTtl)
            {
                TryStartPrefetch(accountId, allowRetryForEmpty: true);
            }

            // Ensure cache seeded asynchronously (single flight) if we have no cache yet
            if (!_cache.Has(accountId))
            {
                TryStartPrefetch(accountId);
                yield break; // not yet
            }

            var list = _cache.Get(accountId);
            if (list.Count == 0)
            {
                // Empty cached list likely from early snapshot call; re-fetch later.
                TryStartPrefetch(accountId, allowRetryForEmpty: true);
                yield break;
            }

            // Simple prefix filter with stable ordering (shorter first, then lexicographic)
            foreach (var t in list.Where(t => t.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                                  .OrderBy(t => t.Length).ThenBy(t => t))
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
                    // Use combined phrases (global + account-specific) for completion
                    var all = await _svc.GetCombinedPhrasesAsync(accountId);
                    if (all.Count > 0)
                    {
                        _cache.Set(accountId, all);
                        _combinedReadyAt[accountId] = DateTime.UtcNow;
                        Debug.WriteLine($"[PhraseCompletion] Prefetch loaded {all.Count} combined phrases (global + account) for account={accountId}");
                    }
                    else if (allowRetryForEmpty)
                    {
                        // simple delayed retry
                        await Task.Delay(750);
                        var again = await _svc.GetCombinedPhrasesAsync(accountId);
                        if (again.Count > 0)
                        {
                            _cache.Set(accountId, again);
                            _combinedReadyAt[accountId] = DateTime.UtcNow;
                            Debug.WriteLine($"[PhraseCompletion] Retry loaded {again.Count} combined phrases for account={accountId}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[PhraseCompletion] Prefetch error: {ex.Message}");
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
