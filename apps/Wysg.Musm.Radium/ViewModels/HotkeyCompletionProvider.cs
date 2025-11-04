using System;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.CodeCompletion;
using Wysg.Musm.Editor.Snippets;
using Wysg.Musm.Editor.Completion;
using Wysg.Musm.Radium.Services;

namespace Wysg.Musm.Radium.ViewModels
{
    /// <summary>
    /// Provides hotkey completion items from the hotkey service.
    /// Filters based on trigger text prefix matching, including support for digits in triggers.
    /// </summary>
    internal sealed class HotkeyCompletionProvider : ISnippetProvider
    {
        private readonly IHotkeyService _hotkeyService;
        private readonly ITenantContext _ctx;

        public HotkeyCompletionProvider(IHotkeyService hotkeyService, ITenantContext ctx)
        {
            _hotkeyService = hotkeyService ?? throw new ArgumentNullException(nameof(hotkeyService));
            _ctx = ctx ?? throw new ArgumentNullException(nameof(ctx));
        }

        public IEnumerable<ICompletionData> GetCompletions(TextEditor editor)
        {
            var (prefix, _) = GetWordBeforeCaret(editor);
            
            if (string.IsNullOrEmpty(prefix)) yield break;

            long accountId = _ctx.AccountId;
            if (accountId <= 0) yield break;

            // Get active hotkeys synchronously (this should be fast - cached in memory)
            var hotkeys = _hotkeyService.GetActiveHotkeysAsync(accountId).GetAwaiter().GetResult();

            // Filter hotkeys by prefix match on trigger text
            var matches = hotkeys
                .Where(kvp => kvp.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .OrderBy(kvp => kvp.Key.Length)
                .ThenBy(kvp => kvp.Key)
                .ToList();

            foreach (var kvp in matches)
            {
                // Create hotkey completion data with "trigger ¡æ expansion" display
                yield return MusmCompletionData.Hotkey(kvp.Key, kvp.Value, description: null);
            }
        }

        private static (string word, int startOffset) GetWordBeforeCaret(TextEditor editor)
        {
            int caret = editor.CaretOffset;
            var line = editor.Document.GetLineByOffset(caret);
            string lineText = editor.Document.GetText(line);
            int local = Math.Clamp(caret - line.Offset, 0, lineText.Length);
            
            // Use WordBoundaryHelper to include digits, hyphens, and underscores in word
            var (startLocal, endLocal) = WordBoundaryHelper.ComputeWordSpan(lineText, local);
            int start = line.Offset + startLocal;
            string word = endLocal > startLocal ? lineText.Substring(startLocal, endLocal - startLocal) : string.Empty;
            
            return (word, start);
        }
    }
}
