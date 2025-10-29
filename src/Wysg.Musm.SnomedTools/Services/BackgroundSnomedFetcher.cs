using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Wysg.Musm.SnomedTools.Abstractions;

namespace Wysg.Musm.SnomedTools.Services
{
    /// <summary>
    /// Background service that continuously fetches SNOMED concepts and caches relevant synonyms.
    /// Runs until explicitly stopped or application closes.
    /// </summary>
    public sealed class BackgroundSnomedFetcher : IDisposable
    {
        private readonly ISnowstormClient _snowstormClient;
        private readonly ISnomedCacheService _cacheService;
        private readonly CancellationTokenSource _cts;
        private readonly Task _backgroundTask;
        private readonly object _stateLock = new();
        
        private int _targetWordCount;
        private string? _nextSearchAfter;
        private int _currentPage;
        private bool _hasMoreConcepts = true;
        private bool _isRunning;
        private bool _isPaused = true; // Start paused by default
        private int _totalFetched;
        private int _totalCached;
        private int _totalSkipped;

        public event EventHandler<CandidateCachedEventArgs>? CandidateCached;
        public event EventHandler<FetchProgressEventArgs>? ProgressUpdated;

        public bool IsRunning
        {
            get { lock (_stateLock) return _isRunning; }
            private set { lock (_stateLock) _isRunning = value; }
        }

        public bool IsPaused
        {
            get { lock (_stateLock) return _isPaused; }
        }

        public int TotalFetched
        {
            get { lock (_stateLock) return _totalFetched; }
            private set { lock (_stateLock) _totalFetched = value; }
        }

        public int TotalCached
        {
            get { lock (_stateLock) return _totalCached; }
            private set { lock (_stateLock) _totalCached = value; }
        }

        public int TotalSkipped
        {
            get { lock (_stateLock) return _totalSkipped; }
            private set { lock (_stateLock) _totalSkipped = value; }
        }

        public BackgroundSnomedFetcher(
            ISnowstormClient snowstormClient,
            ISnomedCacheService cacheService,
            int targetWordCount = 1)
        {
            _snowstormClient = snowstormClient ?? throw new ArgumentNullException(nameof(snowstormClient));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _targetWordCount = targetWordCount;
            _cts = new CancellationTokenSource();
            
            // Start background task (but paused)
            _backgroundTask = Task.Run(() => RunBackgroundFetchAsync(_cts.Token));
        }

        /// <summary>
        /// Restore fetch progress from last session.
        /// </summary>
        public async Task<bool> RestoreProgressAsync()
        {
            var progress = await _cacheService.LoadFetchProgressAsync().ConfigureAwait(false);
            
            if (progress == null)
            {
                Debug.WriteLine("[BackgroundSnomedFetcher] No saved progress found");
                return false;
            }

            lock (_stateLock)
            {
                _targetWordCount = progress.TargetWordCount;
                _nextSearchAfter = progress.NextSearchAfter;
                _currentPage = progress.CurrentPage;
                _hasMoreConcepts = true;
            }

            Debug.WriteLine($"[BackgroundSnomedFetcher] Restored progress: page={progress.CurrentPage}, wordCount={progress.TargetWordCount}, saved={progress.SavedAt:g}");
            return true;
        }

        /// <summary>
        /// Clear saved progress.
        /// </summary>
        public async Task ClearProgressAsync()
        {
            await _cacheService.ClearFetchProgressAsync().ConfigureAwait(false);
            
            lock (_stateLock)
            {
                _nextSearchAfter = null;
                _currentPage = 0;
                _hasMoreConcepts = true;
            }
            
            Debug.WriteLine("[BackgroundSnomedFetcher] Progress cleared");
        }

        public void Start()
        {
            lock (_stateLock)
            {
                if (!_isPaused)
                    return; // Already running
                    
                _isPaused = false;
                Debug.WriteLine("[BackgroundSnomedFetcher] Starting fetch");
            }
        }

        public void Pause()
        {
            lock (_stateLock)
            {
                if (_isPaused)
                    return; // Already paused
                    
                _isPaused = true;
                Debug.WriteLine("[BackgroundSnomedFetcher] Pausing fetch");
            }
        }

        public void SetTargetWordCount(int wordCount)
        {
            if (wordCount < 1 || wordCount > 10)
                throw new ArgumentOutOfRangeException(nameof(wordCount), "Word count must be between 1 and 10");
                
            lock (_stateLock)
            {
                if (_targetWordCount != wordCount)
                {
                    _targetWordCount = wordCount;
                    // Reset pagination state when word count changes
                    _nextSearchAfter = null;
                    _currentPage = 0;
                    _hasMoreConcepts = true;
                    Debug.WriteLine($"[BackgroundSnomedFetcher] Target word count changed to {wordCount}, resetting search");
                }
            }
        }

        private async Task RunBackgroundFetchAsync(CancellationToken cancellationToken)
        {
            IsRunning = true;
            Debug.WriteLine("[BackgroundSnomedFetcher] Background task started (paused)");
            
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    // Check if paused
                    bool paused;
                    lock (_stateLock)
                    {
                        paused = _isPaused;
                    }

                    if (paused)
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken).ConfigureAwait(false);
                        continue;
                    }

                    bool hasMore;
                    lock (_stateLock)
                    {
                        hasMore = _hasMoreConcepts;
                    }

                    if (!hasMore)
                    {
                        // All concepts fetched, wait before checking again
                        Debug.WriteLine("[BackgroundSnomedFetcher] All concepts processed, waiting 60s before restart");
                        await Task.Delay(TimeSpan.FromSeconds(60), cancellationToken).ConfigureAwait(false);
                        
                        // Reset to start over
                        lock (_stateLock)
                        {
                            _nextSearchAfter = null;
                            _currentPage = 0;
                            _hasMoreConcepts = true;
                        }
                        continue;
                    }

                    try
                    {
                        await FetchNextPageAsync(cancellationToken).ConfigureAwait(false);
                        
                        // Small delay between fetches to avoid overwhelming the server
                        await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        throw; // Propagate cancellation
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[BackgroundSnomedFetcher] Error during fetch: {ex.Message}");
                        // Wait before retrying
                        await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("[BackgroundSnomedFetcher] Background fetch cancelled");
            }
            finally
            {
                IsRunning = false;
                Debug.WriteLine("[BackgroundSnomedFetcher] Background fetch stopped");
            }
        }

        private async Task FetchNextPageAsync(CancellationToken cancellationToken)
        {
            const int pageSize = 50;
            int offset;
            string? searchAfter;
            int targetWords;
            
            lock (_stateLock)
            {
                offset = _currentPage * pageSize;
                searchAfter = _nextSearchAfter;
                targetWords = _targetWordCount;
            }

            Debug.WriteLine($"[BackgroundSnomedFetcher] Fetching page {_currentPage + 1} for {targetWords}-word terms");
            
            var (concepts, nextSearchAfter) = await _snowstormClient.BrowseBySemanticTagAsync(
                "all",
                offset,
                pageSize,
                searchAfter
            ).ConfigureAwait(false);

            if (cancellationToken.IsCancellationRequested)
                return;

            lock (_stateLock)
            {
                _nextSearchAfter = nextSearchAfter;
                _currentPage++;
                
                if (concepts.Count == 0)
                {
                    _hasMoreConcepts = false;
                    Debug.WriteLine("[BackgroundSnomedFetcher] No more concepts available");
                }
            }

            TotalFetched += concepts.Count;

            // Save progress after successful page fetch
            await _cacheService.SaveFetchProgressAsync(_targetWordCount, _nextSearchAfter, _currentPage).ConfigureAwait(false);

            // Process concepts and cache candidates
            var cachedCount = 0;
            var skippedCount = 0;
            
            foreach (var concept in concepts)
            {
                if (cancellationToken.IsCancellationRequested)
                    return;

                foreach (var term in concept.AllTerms)
                {
                    // Only consider synonyms
                    if (!string.Equals(term.Type, "Synonym", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var wordCount = CountWords(term.Term);
                    if (wordCount == targetWords)
                    {
                        var termText = term.Term.ToLowerInvariant().Trim();
                        
                        // FIRST: Check if term already exists in Azure SQL phrase table
                        var existsInAzure = await _cacheService.CheckPhraseExistsInDatabaseAsync(termText).ConfigureAwait(false);
                        
                        if (existsInAzure)
                        {
                            skippedCount++;
                            TotalSkipped++;
                            Debug.WriteLine($"[BackgroundSnomedFetcher] Skipped '{termText}' - already exists in Azure SQL");
                            continue; // Skip this term entirely
                        }
                        
                        // SECOND: Try to cache locally (will skip if already in local cache)
                        var cached = await _cacheService.CacheCandidateAsync(
                            concept.ConceptId,
                            concept.ConceptIdStr,
                            concept.Fsn,
                            concept.Pt,
                            term.Term,
                            term.Type,
                            targetWords
                        ).ConfigureAwait(false);

                        if (cached)
                        {
                            cachedCount++;
                            TotalCached++;
                            
                            // Notify listeners
                            CandidateCached?.Invoke(this, new CandidateCachedEventArgs(
                                concept.ConceptIdStr,
                                term.Term,
                                concept.Fsn
                            ));
                        }
                        else
                        {
                            skippedCount++;
                            TotalSkipped++;
                            Debug.WriteLine($"[BackgroundSnomedFetcher] Skipped '{termText}' - already in local cache");
                        }
                    }
                }
            }

            // Notify progress
            ProgressUpdated?.Invoke(this, new FetchProgressEventArgs(
                TotalFetched,
                TotalCached,
                TotalSkipped,
                _currentPage,
                cachedCount,
                skippedCount
            ));

            Debug.WriteLine($"[BackgroundSnomedFetcher] Page {_currentPage}: fetched {concepts.Count} concepts, cached {cachedCount} candidates, skipped {skippedCount} duplicates");
        }

        private static int CountWords(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return 0;
            return text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length;
        }

        public void Dispose()
        {
            if (!_cts.IsCancellationRequested)
            {
                Debug.WriteLine("[BackgroundSnomedFetcher] Stopping background fetch...");
                _cts.Cancel();
                
                try
                {
                    _backgroundTask.Wait(TimeSpan.FromSeconds(5));
                }
                catch (AggregateException ex) when (ex.InnerException is OperationCanceledException)
                {
                    // Expected
                }
            }
            
            _cts.Dispose();
        }
    }

    public sealed class CandidateCachedEventArgs : EventArgs
    {
        public string ConceptId { get; }
        public string TermText { get; }
        public string ConceptFsn { get; }

        public CandidateCachedEventArgs(string conceptId, string termText, string conceptFsn)
        {
            ConceptId = conceptId;
            TermText = termText;
            ConceptFsn = conceptFsn;
        }
    }

    public sealed class FetchProgressEventArgs : EventArgs
    {
        public int TotalFetched { get; }
        public int TotalCached { get; }
        public int TotalSkipped { get; }
        public int CurrentPage { get; }
        public int CachedThisPage { get; }
        public int SkippedThisPage { get; }

        public FetchProgressEventArgs(int totalFetched, int totalCached, int totalSkipped, int currentPage, int cachedThisPage, int skippedThisPage)
        {
            TotalFetched = totalFetched;
            TotalCached = totalCached;
            TotalSkipped = totalSkipped;
            CurrentPage = currentPage;
            CachedThisPage = cachedThisPage;
            SkippedThisPage = skippedThisPage;
        }
    }
}
