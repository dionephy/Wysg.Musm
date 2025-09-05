using System.Collections.Generic;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.CodeCompletion;

namespace Wysg.Musm.Editor.Snippets;

/// <summary>
/// Supplies snippet completion items for the current editor context.
/// Implementations can source from memory, JSON, or a database.
/// </summary>
public interface ISnippetProvider
{
    IEnumerable<ICompletionData> GetCompletions(TextEditor editor);
}
