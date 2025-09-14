using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ICSharpCode.AvalonEdit.CodeCompletion;
using Wysg.Musm.Editor.Controls;
using Wysg.Musm.Editor.Snippets;
using Wysg.Musm.Infrastructure.ViewModels;
using Wysg.Musm.Radium.Services;

namespace Wysg.Musm.Radium.ViewModels
{
    public partial class MainViewModel : BaseViewModel
    {
        private readonly IPhraseService _phrases;
        private readonly ITenantContext _tenant;
        private readonly IPhraseCache _cache;

        public MainViewModel(IPhraseService phrases, ITenantContext tenant, IPhraseCache cache)
        {
            _phrases = phrases;
            _tenant = tenant;
            _cache = cache;
        }

        public void InitializeEditor(EditorControl editor)
        {
            editor.MinCharsForSuggest = 1; // open when กร 1 letter
            editor.SnippetProvider = new PhraseSnippetProvider(_phrases, _tenant, _cache);
            editor.DebugSeedGhosts();
            editor.EnableGhostDebugAnchors(false);
            // kick off prefetch ASAP
            _ = Task.Run(async () =>
            {
                var all = await _phrases.GetPhrasesForTenantAsync(_tenant.TenantId);
                _cache.Set(_tenant.TenantId, all);
            });
        }

        private sealed class PhraseSnippetProvider : ISnippetProvider
        {
            private readonly IPhraseService _svc;
            private readonly ITenantContext _ctx;
            private readonly IPhraseCache _cache;
            private volatile bool _prefetching;

            public PhraseSnippetProvider(IPhraseService svc, ITenantContext ctx, IPhraseCache cache)
            { _svc = svc; _ctx = ctx; _cache = cache; }

            public IEnumerable<ICompletionData> GetCompletions(ICSharpCode.AvalonEdit.TextEditor editor)
            {
                var (prefix, start) = GetWordBeforeCaret(editor);
                if (string.IsNullOrEmpty(prefix)) yield break;

                if (!_cache.Has(_ctx.TenantId) && !_prefetching)
                {
                    _prefetching = true;
                    _ = Task.Run(async () =>
                    {
                        var all = await _svc.GetPhrasesForTenantAsync(_ctx.TenantId);
                        _cache.Set(_ctx.TenantId, all);
                        _prefetching = false;
                    });
                    yield break; // no data yet; popup will open next time
                }

                var list = _cache.Get(_ctx.TenantId);
                if (list.Count == 0) yield break;

                var filtered = list.Where(t => t.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                                   .OrderBy(t => t.Length).ThenBy(t => t);

                foreach (var t in filtered)
                    yield return MusmCompletionData.Token(t);
            }

            private static (string word, int startOffset) GetWordBeforeCaret(ICSharpCode.AvalonEdit.TextEditor editor)
            {
                int caret = editor.CaretOffset;
                var line = editor.Document.GetLineByOffset(caret);
                var text = editor.Document.GetText(line.Offset, caret - line.Offset);

                int i = text.Length - 1;
                while (i >= 0)
                {
                    char ch = text[i];
                    if (!char.IsLetter(ch)) break;
                    i--;
                }
                int start = line.Offset + i + 1;
                string word = editor.Document.GetText(start, caret - start);
                return (word, start);
            }
        }
    }
}