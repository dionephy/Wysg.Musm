using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Wysg.Musm.Radium.Services
{
    /// <summary>
    /// Hotkey information record (account-scoped, no global hotkeys)
    /// </summary>
    public record HotkeyInfo(long HotkeyId, long AccountId, string TriggerText, string ExpansionText, bool IsActive, DateTime UpdatedAt, long Rev);

    /// <summary>
    /// Service for managing account-scoped text expansion hotkeys.
    /// Follows synchronous snapshot pattern: DB update -> snapshot update -> UI display from snapshot.
    /// </summary>
    public interface IHotkeyService
    {
        /// <summary>
        /// Explicit one-time preload (fills in-memory snapshot; safe to call multiple times)
        /// </summary>
        Task PreloadAsync(long accountId);

        /// <summary>
        /// Get all hotkeys metadata for a specific account from snapshot
        /// </summary>
        Task<IReadOnlyList<HotkeyInfo>> GetAllHotkeyMetaAsync(long accountId);

        /// <summary>
        /// Get active hotkeys for completion/expansion (trigger -> expansion mapping)
        /// </summary>
        Task<IReadOnlyDictionary<string, string>> GetActiveHotkeysAsync(long accountId);

        /// <summary>
        /// Upsert hotkey (insert new or update existing by trigger_text).
        /// Returns updated/created hotkey info from snapshot after DB commit.
        /// </summary>
        Task<HotkeyInfo> UpsertHotkeyAsync(long accountId, string triggerText, string expansionText, bool isActive = true);

        /// <summary>
        /// Toggle is_active flag for a specific hotkey.
        /// Returns updated hotkey info from snapshot, or null on failure.
        /// </summary>
        Task<HotkeyInfo?> ToggleActiveAsync(long accountId, long hotkeyId);

        /// <summary>
        /// Delete a specific hotkey.
        /// Returns true if deleted, false if not found or error.
        /// </summary>
        Task<bool> DeleteHotkeyAsync(long accountId, long hotkeyId);

        /// <summary>
        /// Refresh hotkeys from database and update snapshot for the given account
        /// </summary>
        Task RefreshHotkeysAsync(long accountId);
    }
}
