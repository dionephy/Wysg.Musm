using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Wysg.Musm.SnomedTools.Abstractions;

namespace Wysg.Musm.SnomedTools.Services
{
    /// <summary>
    /// SQLite-based implementation of SNOMED cache service.
    /// Stores candidates locally for user review.
    /// </summary>
    public sealed class SqliteSnomedCacheService : ISnomedCacheService
    {
        private readonly string _dbPath;
        private readonly object _lock = new();
        private readonly IPhraseService _phraseService;

        public SqliteSnomedCacheService(IPhraseService phraseService, string? dbPath = null)
        {
            _phraseService = phraseService ?? throw new ArgumentNullException(nameof(phraseService));
            _dbPath = dbPath ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Wysg.Musm.SnomedTools",
                "snomed_cache.db");

            EnsureDatabase();
        }

        private string ConnectionString => $"Data Source={_dbPath}";

        private void EnsureDatabase()
        {
            var dir = Path.GetDirectoryName(_dbPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            lock (_lock)
            {
                using var con = new SqliteConnection(ConnectionString);
                con.Open();

                const string createTableSql = @"
                    CREATE TABLE IF NOT EXISTS cached_candidates (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        concept_id INTEGER NOT NULL,
                        concept_id_str TEXT NOT NULL,
                        concept_fsn TEXT NOT NULL,
                        concept_pt TEXT,
                        term_text TEXT NOT NULL,
                        term_type TEXT NOT NULL,
                        word_count INTEGER NOT NULL,
                        cached_at TEXT NOT NULL,
                        status INTEGER NOT NULL DEFAULT 0,
                        UNIQUE(concept_id, term_text)
                    );
                    
                    CREATE INDEX IF NOT EXISTS idx_status ON cached_candidates(status);
                    CREATE INDEX IF NOT EXISTS idx_word_count ON cached_candidates(word_count);
                    CREATE INDEX IF NOT EXISTS idx_cached_at ON cached_candidates(cached_at);
                    CREATE INDEX IF NOT EXISTS idx_term_text ON cached_candidates(term_text);

                    CREATE TABLE IF NOT EXISTS fetch_progress (
                        id INTEGER PRIMARY KEY CHECK (id = 1),
                        target_word_count INTEGER NOT NULL,
                        next_search_after TEXT,
                        current_page INTEGER NOT NULL,
                        saved_at TEXT NOT NULL
                    );
                ";

                using var cmd = new SqliteCommand(createTableSql, con);
                cmd.ExecuteNonQuery();
            }

            Debug.WriteLine($"[SqliteSnomedCacheService] Database initialized at {_dbPath}");
        }

        public async Task<bool> CheckPhraseExistsInDatabaseAsync(string phraseText)
        {
            try
            {
                // Get all global phrases from Azure SQL
                var globalPhrases = await _phraseService.GetAllGlobalPhraseMetaAsync().ConfigureAwait(false);
                
                // Check if phrase exists (case-insensitive, trimmed comparison)
                var normalized = phraseText.ToLowerInvariant().Trim();
                var exists = globalPhrases.Any(p => p.Text.ToLowerInvariant().Trim() == normalized);
                
                if (exists)
                {
                    Debug.WriteLine($"[SqliteSnomedCacheService] Phrase '{phraseText}' exists in Azure SQL");
                }
                
                return exists;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SqliteSnomedCacheService] Error checking phrase existence: {ex.Message}");
                return false; // On error, assume it doesn't exist to avoid skipping valid candidates
            }
        }

        public async Task<bool> CacheCandidateAsync(
            long conceptId,
            string conceptIdStr,
            string conceptFsn,
            string? conceptPt,
            string termText,
            string termType,
            int wordCount)
        {
            const string insertSql = @"
                INSERT OR IGNORE INTO cached_candidates 
                (concept_id, concept_id_str, concept_fsn, concept_pt, term_text, term_type, word_count, cached_at, status)
                VALUES (@conceptId, @conceptIdStr, @conceptFsn, @conceptPt, @termText, @termType, @wordCount, @cachedAt, @status)
            ";

            await using var con = new SqliteConnection(ConnectionString);
            await con.OpenAsync().ConfigureAwait(false);
            await using var cmd = new SqliteCommand(insertSql, con);
            
            cmd.Parameters.AddWithValue("@conceptId", conceptId);
            cmd.Parameters.AddWithValue("@conceptIdStr", conceptIdStr);
            cmd.Parameters.AddWithValue("@conceptFsn", conceptFsn);
            cmd.Parameters.AddWithValue("@conceptPt", conceptPt ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@termText", termText);
            cmd.Parameters.AddWithValue("@termType", termType);
            cmd.Parameters.AddWithValue("@wordCount", wordCount);
            cmd.Parameters.AddWithValue("@cachedAt", DateTime.UtcNow.ToString("O"));
            cmd.Parameters.AddWithValue("@status", (int)CandidateStatus.Pending);

            var rowsAffected = await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            var cached = rowsAffected > 0;
            
            if (cached)
            {
                Debug.WriteLine($"[SqliteSnomedCacheService] Cached: {termText} ({wordCount} words) -> {conceptIdStr}");
            }
            
            return cached;
        }

        public async Task SaveFetchProgressAsync(int targetWordCount, string? nextSearchAfter, int currentPage)
        {
            const string upsertSql = @"
                INSERT INTO fetch_progress (id, target_word_count, next_search_after, current_page, saved_at)
                VALUES (1, @targetWordCount, @nextSearchAfter, @currentPage, @savedAt)
                ON CONFLICT(id) DO UPDATE SET
                    target_word_count = @targetWordCount,
                    next_search_after = @nextSearchAfter,
                    current_page = @currentPage,
                    saved_at = @savedAt
            ";

            await using var con = new SqliteConnection(ConnectionString);
            await con.OpenAsync().ConfigureAwait(false);
            await using var cmd = new SqliteCommand(upsertSql, con);
            
            cmd.Parameters.AddWithValue("@targetWordCount", targetWordCount);
            cmd.Parameters.AddWithValue("@nextSearchAfter", nextSearchAfter ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@currentPage", currentPage);
            cmd.Parameters.AddWithValue("@savedAt", DateTime.UtcNow.ToString("O"));

            await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            Debug.WriteLine($"[SqliteSnomedCacheService] Saved fetch progress: page={currentPage}, wordCount={targetWordCount}");
        }

        public async Task<FetchProgress?> LoadFetchProgressAsync()
        {
            const string selectSql = @"
                SELECT target_word_count, next_search_after, current_page, saved_at
                FROM fetch_progress
                WHERE id = 1
            ";

            await using var con = new SqliteConnection(ConnectionString);
            await con.OpenAsync().ConfigureAwait(false);
            await using var cmd = new SqliteCommand(selectSql, con);

            await using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
            if (await reader.ReadAsync().ConfigureAwait(false))
            {
                var progress = new FetchProgress(
                    TargetWordCount: reader.GetInt32(0),
                    NextSearchAfter: reader.IsDBNull(1) ? null : reader.GetString(1),
                    CurrentPage: reader.GetInt32(2),
                    SavedAt: DateTime.Parse(reader.GetString(3))
                );
                
                Debug.WriteLine($"[SqliteSnomedCacheService] Loaded fetch progress: page={progress.CurrentPage}, wordCount={progress.TargetWordCount}");
                return progress;
            }

            return null;
        }

        public async Task ClearFetchProgressAsync()
        {
            const string deleteSql = "DELETE FROM fetch_progress WHERE id = 1";
            
            await using var con = new SqliteConnection(ConnectionString);
            await con.OpenAsync().ConfigureAwait(false);
            await using var cmd = new SqliteCommand(deleteSql, con);
            
            await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            Debug.WriteLine("[SqliteSnomedCacheService] Cleared fetch progress");
        }

        public async Task<IReadOnlyList<CachedCandidate>> GetPendingCandidatesAsync(int limit = 100)
        {
            const string selectSql = @"
                SELECT id, concept_id, concept_id_str, concept_fsn, concept_pt, 
                       term_text, term_type, word_count, cached_at, status
                FROM cached_candidates
                WHERE status = @status
                ORDER BY cached_at ASC
                LIMIT @limit
            ";

            var results = new List<CachedCandidate>();
            
            await using var con = new SqliteConnection(ConnectionString);
            await con.OpenAsync().ConfigureAwait(false);
            await using var cmd = new SqliteCommand(selectSql, con);
            
            cmd.Parameters.AddWithValue("@status", (int)CandidateStatus.Pending);
            cmd.Parameters.AddWithValue("@limit", limit);

            await using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                results.Add(new CachedCandidate(
                    Id: reader.GetInt64(0),
                    ConceptId: reader.GetInt64(1),
                    ConceptIdStr: reader.GetString(2),
                    ConceptFsn: reader.GetString(3),
                    ConceptPt: reader.IsDBNull(4) ? null : reader.GetString(4),
                    TermText: reader.GetString(5),
                    TermType: reader.GetString(6),
                    WordCount: reader.GetInt32(7),
                    CachedAt: DateTime.Parse(reader.GetString(8)),
                    Status: (CandidateStatus)reader.GetInt32(9)
                ));
            }

            return results;
        }

        public async Task<int> GetPendingCountAsync()
        {
            const string countSql = "SELECT COUNT(*) FROM cached_candidates WHERE status = @status";
            
            await using var con = new SqliteConnection(ConnectionString);
            await con.OpenAsync().ConfigureAwait(false);
            await using var cmd = new SqliteCommand(countSql, con);
            
            cmd.Parameters.AddWithValue("@status", (int)CandidateStatus.Pending);
            
            var result = await cmd.ExecuteScalarAsync().ConfigureAwait(false);
            return Convert.ToInt32(result);
        }

        public async Task MarkAcceptedAsync(long candidateId)
        {
            await UpdateStatusAsync(candidateId, CandidateStatus.Accepted).ConfigureAwait(false);
        }

        public async Task MarkRejectedAsync(long candidateId)
        {
            await UpdateStatusAsync(candidateId, CandidateStatus.Rejected).ConfigureAwait(false);
        }

        public async Task MarkSavedAsync(long candidateId)
        {
            await UpdateStatusAsync(candidateId, CandidateStatus.Saved).ConfigureAwait(false);
        }

        private async Task UpdateStatusAsync(long candidateId, CandidateStatus status)
        {
            const string updateSql = "UPDATE cached_candidates SET status = @status WHERE id = @id";
            
            await using var con = new SqliteConnection(ConnectionString);
            await con.OpenAsync().ConfigureAwait(false);
            await using var cmd = new SqliteCommand(updateSql, con);
            
            cmd.Parameters.AddWithValue("@status", (int)status);
            cmd.Parameters.AddWithValue("@id", candidateId);
            
            await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            Debug.WriteLine($"[SqliteSnomedCacheService] Marked candidate {candidateId} as {status}");
        }

        public async Task DeleteOldCandidatesAsync(int daysOld = 30)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-daysOld).ToString("O");
            const string deleteSql = "DELETE FROM cached_candidates WHERE cached_at < @cutoffDate";
            
            await using var con = new SqliteConnection(ConnectionString);
            await con.OpenAsync().ConfigureAwait(false);
            await using var cmd = new SqliteCommand(deleteSql, con);
            
            cmd.Parameters.AddWithValue("@cutoffDate", cutoffDate);
            
            var rowsDeleted = await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            Debug.WriteLine($"[SqliteSnomedCacheService] Deleted {rowsDeleted} old candidates (older than {daysOld} days)");
        }

        public async Task<IReadOnlyList<CachedCandidate>> GetAcceptedCandidatesAsync()
        {
            const string selectSql = @"
                SELECT id, concept_id, concept_id_str, concept_fsn, concept_pt, 
                       term_text, term_type, word_count, cached_at, status
                FROM cached_candidates
                WHERE status = @status
                ORDER BY cached_at ASC
            ";

            var results = new List<CachedCandidate>();
            
            await using var con = new SqliteConnection(ConnectionString);
            await con.OpenAsync().ConfigureAwait(false);
            await using var cmd = new SqliteCommand(selectSql, con);
            
            cmd.Parameters.AddWithValue("@status", (int)CandidateStatus.Accepted);

            await using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                results.Add(new CachedCandidate(
                    Id: reader.GetInt64(0),
                    ConceptId: reader.GetInt64(1),
                    ConceptIdStr: reader.GetString(2),
                    ConceptFsn: reader.GetString(3),
                    ConceptPt: reader.IsDBNull(4) ? null : reader.GetString(4),
                    TermText: reader.GetString(5),
                    TermType: reader.GetString(6),
                    WordCount: reader.GetInt32(7),
                    CachedAt: DateTime.Parse(reader.GetString(8)),
                    Status: (CandidateStatus)reader.GetInt32(9)
                ));
            }

            return results;
        }

        public async Task ClearAllCandidatesAsync()
        {
            const string deleteSql = "DELETE FROM cached_candidates";
            
            await using var con = new SqliteConnection(ConnectionString);
            await con.OpenAsync().ConfigureAwait(false);
            await using var cmd = new SqliteCommand(deleteSql, con);
            
            var rowsDeleted = await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            Debug.WriteLine($"[SqliteSnomedCacheService] Cleared all {rowsDeleted} candidates");
        }
    }
}
