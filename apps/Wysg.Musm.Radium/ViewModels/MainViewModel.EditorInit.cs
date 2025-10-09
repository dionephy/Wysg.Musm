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
            // Build a composite provider that merges phrases (tokens; combined scope) with active hotkeys
            editor.SnippetProvider = new CompositeProvider(_phrases, _tenant, _cache, _hotkeys);
            editor.EnableGhostDebugAnchors(false);
            _ = Task.Run(async () =>
            {
                var accountId = _tenant.AccountId;
                // Seed cache with COMBINED phrases (global + account) so completion shows both
                var combined = await _phrases.GetCombinedPhrasesAsync(accountId);
                _cache.Set(accountId, combined);
                // Preload hotkeys snapshot
                await _hotkeys.PreloadAsync(accountId);
                await EnsureCapsAsync();
            });
        }

        private sealed class CompositeProvider : ISnippetProvider
        {
            private readonly IPhraseService _phrases;
            private readonly ITenantContext _tenant;
            private readonly IPhraseCache _cache;
            private readonly IHotkeyService _hotkeys;
            private volatile bool _prefetching;

            public CompositeProvider(IPhraseService phrases, ITenantContext tenant, IPhraseCache cache, IHotkeyService hotkeys)
            { _phrases = phrases; _tenant = tenant; _cache = cache; _hotkeys = hotkeys; }

            public IEnumerable<ICSharpCode.AvalonEdit.CodeCompletion.ICompletionData> GetCompletions(ICSharpCode.AvalonEdit.TextEditor editor)
            {
                var (prefix, _) = GetWordBeforeCaret(editor);
                if (string.IsNullOrEmpty(prefix)) yield break;

                long accountId = _tenant.AccountId;

                // Ensure combined phrases are available in cache (background prefetch if missing/empty)
                if (!_cache.Has(accountId) || _cache.Get(accountId).Count == 0)
                {
                    TryStartPrefetchCombined(accountId);
                    yield break;
                }

                var list = _cache.Get(accountId);

                foreach (var t in list.Where(t => t.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                                      .OrderBy(t => t.Length).ThenBy(t => t))
                {
                    // No tooltip: factories set Description to empty
                    yield return MusmCompletionData.Token(t);
                }

                // Hotkeys: list active ones as "{trigger} ¡æ {first line}..."; insert full expansion.
                var mapTask = _hotkeys.GetActiveHotkeysAsync(accountId);
                if (!mapTask.IsCompleted) mapTask.Wait(50);
                var map = mapTask.IsCompletedSuccessfully ? mapTask.Result : new Dictionary<string, string>();

                foreach (var kv in map)
                {
                    var trigger = kv.Key;
                    if (!trigger.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;
                    var expansion = kv.Value ?? string.Empty;
                    var firstLine = GetFirstLine(expansion);
                    var display = expansion.IndexOf('\n') >= 0 || expansion.IndexOf('\r') >= 0
                        ? $"{trigger} ¡æ {firstLine}..."
                        : $"{trigger} ¡æ {firstLine}";
                    yield return MusmCompletionData.Hotkey(display, expansion, description: null);
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
