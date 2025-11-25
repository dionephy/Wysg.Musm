using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.CodeCompletion;
using Wysg.Musm.Editor.Snippets;
using Wysg.Musm.Editor.Completion; // Added for WordBoundaryHelper
using Wysg.Musm.Radium.Services;
using System.Collections.Concurrent;

namespace Wysg.Musm.Radium.ViewModels
{
    /// <summary>
    /// Supplies completion items (tokens) from the central phrase database.
    /// Implements ISnippetProvider to plug into EditorControl per spec_editor.md.
    /// Now uses combined phrases (global + account-specific) for completion (FR-274, T385).
    /// 
    /// IMPORTANT: This provider queries by prefix on-demand (not caching all phrases)
    /// to ensure the 15-item limit is enforced at the service layer.
    /// </summary>
    internal sealed class PhraseCompletionProvider : ISnippetProvider
    {
        private readonly IPhraseService _svc;
        private readonly ITenantContext _ctx;

        public PhraseCompletionProvider(IPhraseService svc, ITenantContext ctx)
        { 
            _svc = svc; 
            _ctx = ctx;
        }

        public IEnumerable<ICompletionData> GetCompletions(TextEditor editor)
        {
            var (prefix, _) = GetWordBeforeCaret(editor);
            
            if (string.IsNullOrEmpty(prefix)) yield break;

            long accountId = _ctx.AccountId;

            // CRITICAL FIX: Query by prefix directly instead of caching all phrases
            // This ensures we get exactly 15 results from the service layer
            var task = _svc.GetCombinedPhrasesByPrefixAsync(accountId, prefix, limit: 15);
            
            // Block and wait for results (this is called from UI thread during typing)
            // Note: This is acceptable because the API adapter caches all phrases in memory,
            // so the query is fast (just in-memory filtering)
            task.Wait();
            var matches = task.Result;
            
            foreach (var t in matches)
            {
                yield return MusmCompletionData.Token(t);
            }
        }

        private static (string word, int startOffset) GetWordBeforeCaret(TextEditor editor)
        {
            int caret = editor.CaretOffset;
            var line = editor.Document.GetLineByOffset(caret);
            string lineText = editor.Document.GetText(line);
            int local = Math.Clamp(caret - line.Offset, 0, lineText.Length);
            
            // Use ComputePrefixBeforeCaret to only get text from break to caret (not beyond)
            var (startLocal, endLocal) = WordBoundaryHelper.ComputePrefixBeforeCaret(lineText, local);
            int start = line.Offset + startLocal;
            string word = endLocal > startLocal ? lineText.Substring(startLocal, endLocal - startLocal) : string.Empty;
            
            return (word, start);
        }
    }
}
