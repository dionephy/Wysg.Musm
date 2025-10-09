using System.Threading.Tasks;
using Wysg.Musm.Editor.Controls;
using Wysg.Musm.Editor.Snippets;
using Wysg.Musm.Radium.Services;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Wysg.Musm.Radium.ViewModels
{
    /// <summary>
    /// Partial: Editor initialization (snippet provider, cache warmup).
    /// Extracted so UI layer (MainWindow) can still call InitializeEditor after split.
    /// </summary>
    public partial class MainViewModel
    {
        public void InitializeEditor(EditorControl editor)
        {
            editor.MinCharsForSuggest = 1;
            // Build a composite provider that merges phrases (tokens; combined scope) with active hotkeys and snippets
            editor.SnippetProvider = new CompositeProvider(_phrases, _tenant, _cache, _hotkeys, _snippets);
            editor.EnableGhostDebugAnchors(false);
            _ = Task.Run(async () =>
            {
                var accountId = _tenant.AccountId;
                // Seed cache with COMBINED phrases (global + account) so completion shows both
                var combined = await _phrases.GetCombinedPhrasesAsync(accountId);
                _cache.Set(accountId, combined);
                // Preload hotkeys snapshot
                await _hotkeys.PreloadAsync(accountId);
                // Preload snippets snapshot
                await _snippets.PreloadAsync(accountId);
                await EnsureCapsAsync();
            });
        }

        private sealed class CompositeProvider : ISnippetProvider
        {
            private readonly IPhraseService _phrases;
            private readonly ITenantContext _tenant;
            private readonly IPhraseCache _cache;
            private readonly IHotkeyService _hotkeys;
            private readonly ISnippetService _snippets;
            private volatile bool _prefetching;

            public CompositeProvider(IPhraseService phrases, ITenantContext tenant, IPhraseCache cache, IHotkeyService hotkeys, ISnippetService snippets)
            { _phrases = phrases; _tenant = tenant; _cache = cache; _hotkeys = hotkeys; _snippets = snippets; }

            public IEnumerable<ICSharpCode.AvalonEdit.CodeCompletion.ICompletionData> GetCompletions(ICSharpCode.AvalonEdit.TextEditor editor)
            {
                var (prefix, _) = GetWordBeforeCaret(editor);
                if (string.IsNullOrEmpty(prefix)) yield break;

                long accountId = _tenant.AccountId;

                // Ensure combined phrases are available in cache (background prefetch if missing/empty)
                if (!_cache.Has(accountId) || _cache.Get(accountId).Count == 0)
                {
                    TryStartPrefetchCombined(accountId);
                    // continue (we can still show hotkeys/snippets if available)
                }

                // 1) Phrases (tokens)
                if (_cache.Has(accountId))
                {
                    var list = _cache.Get(accountId);
                    foreach (var t in list.Where(t => t.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                                          .OrderBy(t => t.Length).ThenBy(t => t))
                    {
                        yield return MusmCompletionData.Token(t);
                    }
                }

                // 2) Hotkeys
                var metaTask = _hotkeys.GetAllHotkeyMetaAsync(accountId);
                if (!metaTask.IsCompleted) metaTask.Wait(50);
                var meta = metaTask.IsCompletedSuccessfully ? metaTask.Result : Array.Empty<HotkeyInfo>();
                foreach (var hk in meta.Where(h => h.IsActive))
                {
                    if (!hk.TriggerText.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;
                    var desc = string.IsNullOrWhiteSpace(hk.Description) ? GetFirstLine(hk.ExpansionText) : hk.Description;
                    var display = $"{hk.TriggerText} ¡æ {desc}";
                    yield return MusmCompletionData.Hotkey(display, hk.ExpansionText, description: null);
                }

                // 3) Snippets (database-driven)
                var snTask = _snippets.GetActiveSnippetsAsync(accountId);
                if (!snTask.IsCompleted) snTask.Wait(50);
                var snDict = snTask.IsCompletedSuccessfully ? snTask.Result : new Dictionary<string, (string text, string ast)>(StringComparer.OrdinalIgnoreCase);
                foreach (var kv in snDict)
                {
                    var trigger = kv.Key;
                    if (!trigger.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;
                    var (text, _ast) = kv.Value;
                    var snippet = new CodeSnippet(trigger, GetFirstLine(text), text);
                    yield return MusmCompletionData.Snippet(snippet);
                }
            }

            private void TryStartPrefetchCombined(long accountId)
            {
                if (_prefetching) return;
                _prefetching = true;
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var combined = await _phrases.GetCombinedPhrasesAsync(accountId);
                        if (combined.Count > 0)
                            _cache.Set(accountId, combined);
                    }
                    catch { }
                    finally { _prefetching = false; }
                });
            }

            private static string GetFirstLine(string s)
            {
                if (string.IsNullOrEmpty(s)) return string.Empty;
                s = s.Replace("\r\n", "\n");
                int idx = s.IndexOf('\n');
                return idx >= 0 ? s.Substring(0, idx) : s;
            }

            private static (string word, int startOffset) GetWordBeforeCaret(ICSharpCode.AvalonEdit.TextEditor editor)
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
}
