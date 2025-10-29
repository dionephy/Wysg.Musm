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
    /// Separates candidates into three categories: Organism, Substance, and Others.
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
        
        // Three separate current candidates
        private CachedCandidate? _currentOrganismCandidate;
        private CachedCandidate? _currentSubstanceCandidate;
        private CachedCandidate? _currentOtherCandidate;
        
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
                    UpdateCommandStates();
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

        public CachedCandidate? CurrentOrganismCandidate
        {
            get => _currentOrganismCandidate;
            private set
            {
                if (_currentOrganismCandidate != value)
                {
                    _currentOrganismCandidate = value;
                    OnPropertyChanged();
                }
            }
        }

        public CachedCandidate? CurrentSubstanceCandidate
        {
            get => _currentSubstanceCandidate;
            private set
            {
                if (_currentSubstanceCandidate != value)
                {
                    _currentSubstanceCandidate = value;
                    OnPropertyChanged();
                }
            }
        }

        public CachedCandidate? CurrentOtherCandidate
        {
            get => _currentOtherCandidate;
            private set
            {
                if (_currentOtherCandidate != value)
                {
                    _currentOtherCandidate = value;
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
        
        // Organism commands
        public IAsyncRelayCommand AcceptOrganismCommand { get; }
        public IAsyncRelayCommand RejectOrganismCommand { get; }
        
        // Substance commands
        public IAsyncRelayCommand AcceptSubstanceCommand { get; }
        public IAsyncRelayCommand RejectSubstanceCommand { get; }
        
        // Other commands
        public IAsyncRelayCommand AcceptOtherCommand { get; }
        public IAsyncRelayCommand RejectOtherCommand { get; }
        
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
            
            // Organism commands
            AcceptOrganismCommand = new AsyncRelayCommand(() => AcceptCandidateAsync(CurrentOrganismCandidate, CandidateCategory.Organism), 
                () => !IsBusy && CurrentOrganismCandidate != null);
            RejectOrganismCommand = new AsyncRelayCommand(() => RejectCandidateAsync(CurrentOrganismCandidate, CandidateCategory.Organism), 
                () => !IsBusy && CurrentOrganismCandidate != null);
            
            // Substance commands
            AcceptSubstanceCommand = new AsyncRelayCommand(() => AcceptCandidateAsync(CurrentSubstanceCandidate, CandidateCategory.Substance), 
                () => !IsBusy && CurrentSubstanceCandidate != null);
            RejectSubstanceCommand = new AsyncRelayCommand(() => RejectCandidateAsync(CurrentSubstanceCandidate, CandidateCategory.Substance), 
                () => !IsBusy && CurrentSubstanceCandidate != null);
            
            // Other commands
            AcceptOtherCommand = new AsyncRelayCommand(() => AcceptCandidateAsync(CurrentOtherCandidate, CandidateCategory.Other), 
                () => !IsBusy && CurrentOtherCandidate != null);
            RejectOtherCommand = new AsyncRelayCommand(() => RejectCandidateAsync(CurrentOtherCandidate, CandidateCategory.Other), 
                () => !IsBusy && CurrentOtherCandidate != null);
            
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

        private enum CandidateCategory
        {
            Organism,
            Substance,
            Other
        }

        private CandidateCategory GetCandidateCategory(CachedCandidate candidate)
        {
            var semanticTag = ExtractSemanticTag(candidate.ConceptFsn);
            
            if (semanticTag != null)
            {
                if (semanticTag.IndexOf("organism", StringComparison.OrdinalIgnoreCase) >= 0)
                    return CandidateCategory.Organism;
                if (semanticTag.IndexOf("substance", StringComparison.OrdinalIgnoreCase) >= 0)
                    return CandidateCategory.Substance;
            }
            
            return CandidateCategory.Other;
        }

        private static string? ExtractSemanticTag(string? fsn)
        {
            if (string.IsNullOrWhiteSpace(fsn)) return null;
            var lastOpenParen = fsn.LastIndexOf('(');
            var lastCloseParen = fsn.LastIndexOf(')');
            if (lastOpenParen >= 0 && lastCloseParen > lastOpenParen)
                return fsn.Substring(lastOpenParen + 1, lastCloseParen - lastOpenParen - 1).Trim();
            return null;
        }

        private void UpdateCommandStates()
        {
            ((AsyncRelayCommand)AcceptOrganismCommand).NotifyCanExecuteChanged();
            ((AsyncRelayCommand)RejectOrganismCommand).NotifyCanExecuteChanged();
            ((AsyncRelayCommand)AcceptSubstanceCommand).NotifyCanExecuteChanged();
            ((AsyncRelayCommand)RejectSubstanceCommand).NotifyCanExecuteChanged();
            ((AsyncRelayCommand)AcceptOtherCommand).NotifyCanExecuteChanged();
            ((AsyncRelayCommand)RejectOtherCommand).NotifyCanExecuteChanged();
            ((AsyncRelayCommand)SaveAllAcceptedCommand).NotifyCanExecuteChanged();
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
                
                // If pending count increased AND there's an empty slot, auto-refresh to show it
                if (PendingCount > previousPendingCount && 
                    (CurrentOrganismCandidate == null || CurrentSubstanceCandidate == null || CurrentOtherCandidate == null))
                {
                    Debug.WriteLine("[CacheReviewViewModel] New candidate cached and empty slots available - auto-refreshing");
                    await LoadNextCandidatesAsync().ConfigureAwait(false);
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

                // Separate into three categories
                await LoadNextCandidatesAsync().ConfigureAwait(false);

                // Update statistics
                var accepted = await _cacheService.GetAcceptedCandidatesAsync().ConfigureAwait(false);
                AcceptedCount = accepted.Count;

                StatusMessage = $"Loaded {PendingCount} pending candidates";
                
                UpdateCommandStates();
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

        private async Task LoadNextCandidatesAsync()
        {
            // Load fresh pending candidates
            var pending = await _cacheService.GetPendingCandidatesAsync(100).ConfigureAwait(false);
            
            PendingCandidates.Clear();
            foreach (var candidate in pending)
            {
                PendingCandidates.Add(candidate);
            }

            PendingCount = PendingCandidates.Count;

            // Separate candidates into three categories
            var organisms = PendingCandidates.Where(c => GetCandidateCategory(c) == CandidateCategory.Organism).ToList();
            var substances = PendingCandidates.Where(c => GetCandidateCategory(c) == CandidateCategory.Substance).ToList();
            var others = PendingCandidates.Where(c => GetCandidateCategory(c) == CandidateCategory.Other).ToList();

            // Set current candidates for each category (only if currently null)
            if (CurrentOrganismCandidate == null)
                CurrentOrganismCandidate = organisms.FirstOrDefault();
            
            if (CurrentSubstanceCandidate == null)
                CurrentSubstanceCandidate = substances.FirstOrDefault();
            
            if (CurrentOtherCandidate == null)
                CurrentOtherCandidate = others.FirstOrDefault();
            
            UpdateCommandStates();
        }

        private async Task AcceptCandidateAsync(CachedCandidate? candidate, CandidateCategory category)
        {
            if (candidate == null)
                return;

            try
            {
                IsBusy = true;
                StatusMessage = $"Accepting '{candidate.TermText}' ({category})...";

                // Mark as accepted
                await _cacheService.MarkAcceptedAsync(candidate.Id).ConfigureAwait(false);
                AcceptedCount++;

                // Remove from pending list
                PendingCandidates.Remove(candidate);
                PendingCount = PendingCandidates.Count;

                StatusMessage = $"Accepted '{candidate.TermText}' ({category}) - ready to save";
                
                // Load next candidate for this category
                await LoadNextCandidateForCategoryAsync(category).ConfigureAwait(false);
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

        private async Task RejectCandidateAsync(CachedCandidate? candidate, CandidateCategory category)
        {
            if (candidate == null)
                return;

            try
            {
                IsBusy = true;
                StatusMessage = $"Rejecting '{candidate.TermText}' ({category})...";

                // Mark as rejected
                await _cacheService.MarkRejectedAsync(candidate.Id).ConfigureAwait(false);
                RejectedCount++;

                // Remove from pending list
                PendingCandidates.Remove(candidate);
                PendingCount = PendingCandidates.Count;

                StatusMessage = $"Rejected '{candidate.TermText}' ({category})";
                
                // Load next candidate for this category
                await LoadNextCandidateForCategoryAsync(category).ConfigureAwait(false);
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

        private async Task LoadNextCandidateForCategoryAsync(CandidateCategory category)
        {
            // Reload pending candidates
            var pending = await _cacheService.GetPendingCandidatesAsync(100).ConfigureAwait(false);
            
            PendingCandidates.Clear();
            foreach (var candidate in pending)
            {
                PendingCandidates.Add(candidate);
            }

            PendingCount = PendingCandidates.Count;

            // Find next candidate for the specific category
            switch (category)
            {
                case CandidateCategory.Organism:
                    CurrentOrganismCandidate = PendingCandidates.FirstOrDefault(c => GetCandidateCategory(c) == CandidateCategory.Organism);
                    break;
                case CandidateCategory.Substance:
                    CurrentSubstanceCandidate = PendingCandidates.FirstOrDefault(c => GetCandidateCategory(c) == CandidateCategory.Substance);
                    break;
                case CandidateCategory.Other:
                    CurrentOtherCandidate = PendingCandidates.FirstOrDefault(c => GetCandidateCategory(c) == CandidateCategory.Other);
                    break;
            }
            
            UpdateCommandStates();
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
                    await _dispatcher.InvokeAsync(() =>
                    {
                        StatusMessage = "No accepted candidates to save";
                    });
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
                        
                        // Update UI counter on dispatcher thread
                        await _dispatcher.InvokeAsync(() =>
                        {
                            SavedCount++;
                        });
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[CacheReviewViewModel] Error saving candidate {candidate.Id}: {ex.Message}");
                    }
                }

                // Update UI on dispatcher thread
                await _dispatcher.InvokeAsync(() =>
                {
                    AcceptedCount = 0; // All accepted have been processed
                    StatusMessage = $"Successfully saved {savedThisRound} candidates to database";
                });
            }
            catch (Exception ex)
            {
                await _dispatcher.InvokeAsync(() =>
                {
                    StatusMessage = $"Error saving candidates: {ex.Message}";
                });
                Debug.WriteLine($"[CacheReviewViewModel] Error saving all: {ex}");
            }
            finally
            {
                await _dispatcher.InvokeAsync(() =>
                {
                    IsBusy = false;
                });
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
