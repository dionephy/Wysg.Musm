using System.Threading.Tasks;
using Wysg.Musm.Editor.Controls;

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
            editor.SnippetProvider = new PhraseCompletionProvider(_phrases, _tenant, _cache);
            editor.EnableGhostDebugAnchors(false);
            _ = Task.Run(async () =>
            {
                var all = await _phrases.GetPhrasesForAccountAsync(_tenant.AccountId);
                _cache.Set(_tenant.AccountId, all);
                await EnsureCapsAsync();
            });
        }
    }
}
