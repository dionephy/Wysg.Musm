using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Wysg.Musm.Radium.Services
{
    /// <summary>
    /// Snippet information record (account-scoped)
    /// </summary>
    public record SnippetInfo(
        long SnippetId,
        long AccountId,
        string TriggerText,
        string SnippetText,
        string SnippetAst,
        string Description,
        bool IsActive,
        DateTime UpdatedAt,
        long Rev);

    /// <summary>
    /// Service for managing account-scoped text snippets with placeholder AST.
    /// Follows synchronous snapshot pattern: DB update -> snapshot update -> UI display from snapshot.
    /// </summary>
    public interface ISnippetService
    {
        Task PreloadAsync(long accountId);
        Task<IReadOnlyList<SnippetInfo>> GetAllSnippetMetaAsync(long accountId);
        Task<IReadOnlyDictionary<string, (string text, string ast, string description)>> GetActiveSnippetsAsync(long accountId);
        Task<SnippetInfo> UpsertSnippetAsync(long accountId, string triggerText, string snippetText, string snippetAst, bool isActive = true, string? description = null);
        Task<SnippetInfo?> ToggleActiveAsync(long accountId, long snippetId);
        Task<bool> DeleteSnippetAsync(long accountId, long snippetId);
        Task RefreshSnippetsAsync(long accountId);
    }
}
