using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Wysg.Musm.Radium.Services
{
    /// <summary>
    /// In-memory cache for phrase completion lists with automatic version-based invalidation.
    /// 
    /// Purpose:
    /// - Avoids repeated database queries for phrase completion
    /// - Stores filtered phrase lists (global phrases ¡Â3 words, all account phrases)
    /// - Automatically invalidates when filter logic changes via CACHE_VERSION
    /// 
    /// Architecture:
    /// - Key: accountId (long) - Each account has separate cached phrase list
    /// - Value: (version, phrases) tuple - Version ensures cache coherence across code changes
    /// 
    /// Cache Versioning (FR-completion-filter-2025-01-20):
    /// - CACHE_VERSION increments when filtering logic changes
    /// - Old cache entries (wrong version) are automatically ignored
    /// - Forces reload with new filtering rules after app update
    /// 
    /// Thread Safety:
    /// - Uses ConcurrentDictionary for thread-safe operations
    /// - Safe to call from multiple threads simultaneously
    /// </summary>
    public sealed class PhraseCache : IPhraseCache
    {
        /// <summary>
        /// Cache version number for automatic invalidation when filter logic changes.
        /// 
        /// Version History:
        /// - v1: No versioning (implicit), no filtering
        /// - v2: Added 3-word filter for global phrases (FR-completion-filter-2025-01-20)
        /// 
        /// IMPORTANT: Increment this number whenever you change filtering rules in:
        /// - AzureSqlPhraseService.GetGlobalPhrasesAsync()
        /// - AzureSqlPhraseService.GetGlobalPhrasesByPrefixAsync()
        /// - PhraseService.GetGlobalPhrasesAsync() (PostgreSQL)
        /// - PhraseService.GetGlobalPhrasesByPrefixAsync() (PostgreSQL)
        /// </summary>
        private const int CACHE_VERSION = 2;
        
        /// <summary>
        /// Thread-safe cache storage: accountId ¡æ (version, phrases list)
        /// </summary>
        private readonly ConcurrentDictionary<long, (int version, IReadOnlyList<string> phrases)> _map = new();
        
        /// <summary>
        /// Get cached phrases for an account, or empty list if not cached or version mismatch.
        /// </summary>
        public IReadOnlyList<string> Get(long tenantId)
        {
            if (_map.TryGetValue(tenantId, out var entry) && entry.version == CACHE_VERSION)
                return entry.phrases;
            
            // Return empty list if:
            // - Account not in cache
            // - Cached version doesn't match current CACHE_VERSION (stale data)
            return new List<string>();
        }
        
        /// <summary>
        /// Store phrases for an account with current cache version.
        /// </summary>
        public void Set(long tenantId, IReadOnlyList<string> phrases)
        {
            _map[tenantId] = (CACHE_VERSION, phrases);
        }
        
        /// <summary>
        /// Check if account has cached phrases with correct version.
        /// </summary>
        public bool Has(long tenantId)
        {
            return _map.TryGetValue(tenantId, out var entry) && entry.version == CACHE_VERSION;
        }
        
        /// <summary>
        /// Clear cached phrases for specific account.
        /// </summary>
        public void Clear(long tenantId) => _map.TryRemove(tenantId, out _);
        
        /// <summary>
        /// Clear all cached phrases (all accounts).
        /// Called on startup to ensure fresh data after app update.
        /// </summary>
        public void ClearAll() => _map.Clear();
    }
}
