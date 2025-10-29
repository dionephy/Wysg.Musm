using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.Input;
using Wysg.Musm.SnomedTools.Abstractions;

namespace Wysg.Musm.SnomedTools.ViewModels
{
    /// <summary>
    /// ViewModel for reviewing cached SNOMED candidates with background fetching.
    /// Allows user to accept (save) or reject (ignore) cached synonyms.
    /// </summary>
    public sealed class CacheReviewViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly ISnomedCacheService _cacheService;
        private readonly IPhraseService _phraseService;
        private readonly ISnomedMapService _snomedMapService;
        private readonly Services.BackgroundSnomedFetcher _backgroundFetcher;
        private readonly Dispatcher _dispatcher;

        private bool _isBusy;
        private string _statusMessage = "Set word count and click Start to begin fetching";
        private int _pendingCount;
        private int _acceptedCount;
        private int _rejectedCount;
        private int _savedCount;
        private CachedCandidate? _currentCandidate;
        private int _backgroundFetchedCount;
        private int _backgroundCachedCount;
        private int _backgroundSkippedCount;
        private int _backgroundCurrentPage;
        private bool _isBackgroundRunning;
        private int _targetWordCount = 1;

        public ObservableCollection<CachedCandidate> PendingCandidates { get; } = new();

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (_isBusy != value)
                {
                    _isBusy = value;
                    OnPropertyChanged();
                    ((AsyncRelayCommand)AcceptCommand).NotifyCanExecuteChanged();
                    ((AsyncRelayCommand)RejectCommand).NotifyCanExecuteChanged();
                    ((AsyncRelayCommand)SaveAllAcceptedCommand).NotifyCanExecuteChanged();
                }
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            private set
            {
                if (_statusMessage != value)
                {
                    _statusMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        public int TargetWordCount
        {
            get => _targetWordCount;
            set
            {
                if (_targetWordCount != value && value >= 1 && value <= 10)
                {
                    _targetWordCount = value;
                    OnPropertyChanged();
                    _backgroundFetcher.SetTargetWordCount(value);
                }
            }
        }

        public int PendingCount
        {
            get => _pendingCount;
            private set
            {
                if (_pendingCount != value)
                {
                    _pendingCount = value;
                    OnPropertyChanged();
                }
            }
        }

        public int AcceptedCount
        {
            get => _acceptedCount;
            private set
            {
                if (_acceptedCount != value)
                {
                    _acceptedCount = value;
                    OnPropertyChanged();
                }
            }
        }

        public int RejectedCount
        {
            get => _rejectedCount;
            private set
            {
                if (_rejectedCount != value)
                {
                    _rejectedCount = value;
                    OnPropertyChanged();
                }
            }
        }

        public int SavedCount
        {
            get => _savedCount;
            private set
            {
                if (_savedCount != value)
                {
                    _savedCount = value;
                    OnPropertyChanged();
                }
            }
        }

        public CachedCandidate? CurrentCandidate
        {
            get => _currentCandidate;
            private set
            {
                if (_currentCandidate != value)
                {
                    _currentCandidate = value;
                    OnPropertyChanged();
                }
            }
        }

        public int BackgroundFetchedCount
        {
            get => _backgroundFetchedCount;
            private set
            {
                if (_backgroundFetchedCount != value)
                {
                    _backgroundFetchedCount = value;
                    OnPropertyChanged();
                }
            }
        }

        public int BackgroundCachedCount
        {
            get => _backgroundCachedCount;
            private set
            {
                if (_backgroundCachedCount != value)
                {
                    _backgroundCachedCount = value;
                    OnPropertyChanged();
                }
            }
        }

        public int BackgroundSkippedCount
        {
            get => _backgroundSkippedCount;
            private set
            {
                if (_backgroundSkippedCount != value)
                {
                    _backgroundSkippedCount = value;
                    OnPropertyChanged();
                }
            }
        }

        public int BackgroundCurrentPage
        {
            get => _backgroundCurrentPage;
            private set
            {
                if (_backgroundCurrentPage != value)
                {
                    _backgroundCurrentPage = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsBackgroundRunning
        {
            get => _isBackgroundRunning;
            private set
            {
                if (_isBackgroundRunning != value)
                {
                    _isBackgroundRunning = value;
                    OnPropertyChanged();
                    ((RelayCommand)StartFetchCommand).NotifyCanExecuteChanged();
                    ((RelayCommand)PauseFetchCommand).NotifyCanExecuteChanged();
                }
            }
        }

        public IAsyncRelayCommand RefreshCommand { get; }
        public IAsyncRelayCommand AcceptCommand { get; }
        public IAsyncRelayCommand RejectCommand { get; }
        public IAsyncRelayCommand SaveAllAcceptedCommand { get; }
        public IRelayCommand StartFetchCommand { get; }
        public IRelayCommand PauseFetchCommand { get; }
        public IAsyncRelayCommand ResetProgressCommand { get; }

        public CacheReviewViewModel(
            ISnomedCacheService cacheService,
            IPhraseService phraseService,
            ISnomedMapService snomedMapService,
            Services.BackgroundSnomedFetcher backgroundFetcher)
        {
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _phraseService = phraseService ?? throw new ArgumentNullException(nameof(phraseService));
            _snomedMapService = snomedMapService ?? throw new ArgumentNullException(nameof(snomedMapService));
            _backgroundFetcher = backgroundFetcher ?? throw new ArgumentNullException(nameof(backgroundFetcher));
            _dispatcher = Dispatcher.CurrentDispatcher;

            RefreshCommand = new AsyncRelayCommand(RefreshAsync);
            AcceptCommand = new AsyncRelayCommand(AcceptCurrentAsync, () => !IsBusy && CurrentCandidate != null);
            RejectCommand = new AsyncRelayCommand(RejectCurrentAsync, () => !IsBusy && CurrentCandidate != null);
            SaveAllAcceptedCommand = new AsyncRelayCommand(SaveAllAcceptedAsync, () => !IsBusy);
            StartFetchCommand = new RelayCommand(StartFetch, () => !IsBackgroundRunning);
            PauseFetchCommand = new RelayCommand(PauseFetch, () => IsBackgroundRunning);
            ResetProgressCommand = new AsyncRelayCommand(ResetProgressAsync);

            // Subscribe to background fetcher events
            _backgroundFetcher.ProgressUpdated += OnBackgroundProgressUpdated;
            _backgroundFetcher.CandidateCached += OnCandidateCached;

            // Initialize with current background state
            BackgroundFetchedCount = _backgroundFetcher.TotalFetched;
            BackgroundCachedCount = _backgroundFetcher.TotalCached;
            BackgroundSkippedCount = _backgroundFetcher.TotalSkipped;
            IsBackgroundRunning = !_backgroundFetcher.IsPaused;

            // Load initial data
            _ = RefreshAsync();
        }

        private void StartFetch()
        {
            _backgroundFetcher.Start();
            IsBackgroundRunning = true;
            StatusMessage = $"Background fetching started for {TargetWordCount}-word terms";
        }

        private void PauseFetch()
        {
            _backgroundFetcher.Pause();
            IsBackgroundRunning = false;
            StatusMessage = "Background fetching paused";
        }

        private void OnBackgroundProgressUpdated(object? sender, Services.FetchProgressEventArgs e)
        {
            _dispatcher.InvokeAsync(() =>
            {
                BackgroundFetchedCount = e.TotalFetched;
                BackgroundCachedCount = e.TotalCached;
                BackgroundSkippedCount = e.TotalSkipped;
                BackgroundCurrentPage = e.CurrentPage;
                IsBackgroundRunning = !_backgroundFetcher.IsPaused;

                if (e.CachedThisPage > 0 || e.SkippedThisPage > 0)
                {
                    StatusMessage = $"Background: Page {e.CurrentPage}, cached {e.CachedThisPage} new, skipped {e.SkippedThisPage} existing";
                }
            });
        }

        private void OnCandidateCached(object? sender, Services.CandidateCachedEventArgs e)
        {
            _dispatcher.InvokeAsync(async () =>
            {
                var previousPendingCount = PendingCount;
                
                // Refresh pending count when new candidates arrive
                PendingCount = await _cacheService.GetPendingCountAsync().ConfigureAwait(false);
                
                // If pending count increased AND there's no current candidate, auto-refresh to show it
                if (PendingCount > previousPendingCount && CurrentCandidate == null)
                {
                    Debug.WriteLine("[CacheReviewViewModel] New candidate cached and no current candidate - auto-refreshing");
                    await LoadNextCandidateAsync().ConfigureAwait(false);
                }
            });
        }

        private async Task RefreshAsync()
        {
            try
            {
                IsBusy = true;
                StatusMessage = "Loading pending candidates...";

                // Load pending candidates
                var pending = await _cacheService.GetPendingCandidatesAsync(100).ConfigureAwait(false);
                
                PendingCandidates.Clear();
                foreach (var candidate in pending)
                {
                    PendingCandidates.Add(candidate);
                }

                PendingCount = PendingCandidates.Count;
                CurrentCandidate = PendingCandidates.FirstOrDefault();

                // Update statistics
                var accepted = await _cacheService.GetAcceptedCandidatesAsync().ConfigureAwait(false);
                AcceptedCount = accepted.Count;

                StatusMessage = $"Loaded {PendingCount} pending candidates";
                
                ((AsyncRelayCommand)AcceptCommand).NotifyCanExecuteChanged();
                ((AsyncRelayCommand)RejectCommand).NotifyCanExecuteChanged();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading candidates: {ex.Message}";
                Debug.WriteLine($"[CacheReviewViewModel] Error refreshing: {ex}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadNextCandidateAsync()
        {
            // Load fresh pending candidates
            var pending = await _cacheService.GetPendingCandidatesAsync(100).ConfigureAwait(false);
            
            PendingCandidates.Clear();
            foreach (var candidate in pending)
            {
                PendingCandidates.Add(candidate);
            }

            PendingCount = PendingCandidates.Count;
            CurrentCandidate = PendingCandidates.FirstOrDefault();
            
            ((AsyncRelayCommand)AcceptCommand).NotifyCanExecuteChanged();
            ((AsyncRelayCommand)RejectCommand).NotifyCanExecuteChanged();
        }

        private async Task AcceptCurrentAsync()
        {
            if (CurrentCandidate == null)
                return;

            try
            {
                IsBusy = true;
                var candidate = CurrentCandidate;
                StatusMessage = $"Accepting '{candidate.TermText}'...";

                // Mark as accepted
                await _cacheService.MarkAcceptedAsync(candidate.Id).ConfigureAwait(false);
                AcceptedCount++;

                // Remove from pending list
                PendingCandidates.Remove(candidate);
                PendingCount = PendingCandidates.Count;

                StatusMessage = $"Accepted '{candidate.TermText}' (ready to save)";
                
                // Auto-refresh to show next candidate
                await LoadNextCandidateAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error accepting candidate: {ex.Message}";
                Debug.WriteLine($"[CacheReviewViewModel] Error accepting: {ex}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task RejectCurrentAsync()
        {
            if (CurrentCandidate == null)
                return;

            try
            {
                IsBusy = true;
                var candidate = CurrentCandidate;
                StatusMessage = $"Rejecting '{candidate.TermText}'...";

                // Mark as rejected
                await _cacheService.MarkRejectedAsync(candidate.Id).ConfigureAwait(false);
                RejectedCount++;

                // Remove from pending list
                PendingCandidates.Remove(candidate);
                PendingCount = PendingCandidates.Count;

                StatusMessage = $"Rejected '{candidate.TermText}'";
                
                // Auto-refresh to show next candidate
                await LoadNextCandidateAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error rejecting candidate: {ex.Message}";
                Debug.WriteLine($"[CacheReviewViewModel] Error rejecting: {ex}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task SaveAllAcceptedAsync()
        {
            try
            {
                IsBusy = true;
                StatusMessage = "Saving all accepted candidates to database...";

                var accepted = await _cacheService.GetAcceptedCandidatesAsync().ConfigureAwait(false);
                
                if (accepted.Count == 0)
                {
                    StatusMessage = "No accepted candidates to save";
                    return;
                }

                var savedThisRound = 0;
                foreach (var candidate in accepted)
                {
                    try
                    {
                        // Create phrase (active)
                        var phrase = await _phraseService.UpsertPhraseAsync(
                            accountId: null,  // Global phrase
                            text: candidate.TermText.ToLowerInvariant().Trim(),
                            active: true
                        ).ConfigureAwait(false);

                        // Cache SNOMED concept
                        var concept = new SnomedConcept(
                            candidate.ConceptId,
                            candidate.ConceptIdStr,
                            candidate.ConceptFsn,
                            candidate.ConceptPt,
                            true,
                            DateTime.UtcNow
                        );
                        await _snomedMapService.CacheConceptAsync(concept).ConfigureAwait(false);

                        // Map phrase to concept
                        await _snomedMapService.MapPhraseAsync(
                            phrase.Id,
                            accountId: null,
                            candidate.ConceptId,
                            mappingType: "exact",
                            confidence: 1.0m,
                            notes: $"Imported via Cache Review ({candidate.WordCount}-word)"
                        ).ConfigureAwait(false);

                        // Mark as saved
                        await _cacheService.MarkSavedAsync(candidate.Id).ConfigureAwait(false);
                        
                        savedThisRound++;
                        SavedCount++;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[CacheReviewViewModel] Error saving candidate {candidate.Id}: {ex.Message}");
                    }
                }

                AcceptedCount = 0; // All accepted have been processed
                StatusMessage = $"Successfully saved {savedThisRound} candidates to database";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error saving candidates: {ex.Message}";
                Debug.WriteLine($"[CacheReviewViewModel] Error saving all: {ex}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ResetProgressAsync()
        {
            try
            {
                // Pause fetching first
                if (IsBackgroundRunning)
                {
                    _backgroundFetcher.Pause();
                }

                // Clear progress
                await _backgroundFetcher.ClearProgressAsync().ConfigureAwait(false);

                // Reset counters
                BackgroundFetchedCount = 0;
                BackgroundCachedCount = 0;
                BackgroundSkippedCount = 0;
                BackgroundCurrentPage = 0;

                StatusMessage = "Progress reset. Set word count and click Start to begin from the beginning.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error resetting progress: {ex.Message}";
                Debug.WriteLine($"[CacheReviewViewModel] Error resetting progress: {ex}");
            }
        }

        public void Dispose()
        {
            if (_backgroundFetcher != null)
            {
                _backgroundFetcher.ProgressUpdated -= OnBackgroundProgressUpdated;
                _backgroundFetcher.CandidateCached -= OnCandidateCached;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
