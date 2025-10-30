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
    /// Wrapper for CachedCandidate with selection support.
    /// </summary>
    public sealed class SelectableCachedCandidate : INotifyPropertyChanged
    {
        private bool _isSelected;

        public CachedCandidate Candidate { get; }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        public SelectableCachedCandidate(CachedCandidate candidate)
        {
            Candidate = candidate;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// ViewModel for reviewing cached SNOMED candidates with background fetching.
    /// Allows user to accept (save) or reject (ignore) cached synonyms.
    /// Shows lists of candidates by category with multi-select for bulk operations.
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
        
        private int _backgroundFetchedCount;
        private int _backgroundCachedCount;
        private int _backgroundSkippedCount;
        private int _backgroundCurrentPage;
        private bool _isBackgroundRunning;
        private int _targetWordCount = 1;

        // Three observable collections for each category
        public ObservableCollection<SelectableCachedCandidate> OrganismCandidates { get; } = new();
        public ObservableCollection<SelectableCachedCandidate> SubstanceCandidates { get; } = new();
        public ObservableCollection<SelectableCachedCandidate> OtherCandidates { get; } = new();

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
        
        // Bulk operation commands
        public IAsyncRelayCommand AcceptSelectedOrganismsCommand { get; }
        public IAsyncRelayCommand RejectSelectedOrganismsCommand { get; }
        public IAsyncRelayCommand AcceptSelectedSubstancesCommand { get; }
        public IAsyncRelayCommand RejectSelectedSubstancesCommand { get; }
        public IAsyncRelayCommand AcceptSelectedOthersCommand { get; }
        public IAsyncRelayCommand RejectSelectedOthersCommand { get; }
        
        // Select all commands
        public IRelayCommand SelectAllOrganismsCommand { get; }
        public IRelayCommand DeselectAllOrganismsCommand { get; }
        public IRelayCommand SelectAllSubstancesCommand { get; }
        public IRelayCommand DeselectAllSubstancesCommand { get; }
        public IRelayCommand SelectAllOthersCommand { get; }
        public IRelayCommand DeselectAllOthersCommand { get; }
        
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
            
            // Bulk operations
            AcceptSelectedOrganismsCommand = new AsyncRelayCommand(() => AcceptSelectedAsync(OrganismCandidates), 
                () => !IsBusy && OrganismCandidates.Any(c => c.IsSelected));
            RejectSelectedOrganismsCommand = new AsyncRelayCommand(() => RejectSelectedAsync(OrganismCandidates), 
                () => !IsBusy && OrganismCandidates.Any(c => c.IsSelected));
            
            AcceptSelectedSubstancesCommand = new AsyncRelayCommand(() => AcceptSelectedAsync(SubstanceCandidates), 
                () => !IsBusy && SubstanceCandidates.Any(c => c.IsSelected));
            RejectSelectedSubstancesCommand = new AsyncRelayCommand(() => RejectSelectedAsync(SubstanceCandidates), 
                () => !IsBusy && SubstanceCandidates.Any(c => c.IsSelected));
            
            AcceptSelectedOthersCommand = new AsyncRelayCommand(() => AcceptSelectedAsync(OtherCandidates), 
                () => !IsBusy && OtherCandidates.Any(c => c.IsSelected));
            RejectSelectedOthersCommand = new AsyncRelayCommand(() => RejectSelectedAsync(OtherCandidates), 
                () => !IsBusy && OtherCandidates.Any(c => c.IsSelected));
            
            // Select all commands
            SelectAllOrganismsCommand = new RelayCommand(() => SelectAll(OrganismCandidates));
            DeselectAllOrganismsCommand = new RelayCommand(() => DeselectAll(OrganismCandidates));
            SelectAllSubstancesCommand = new RelayCommand(() => SelectAll(SubstanceCandidates));
            DeselectAllSubstancesCommand = new RelayCommand(() => DeselectAll(SubstanceCandidates));
            SelectAllOthersCommand = new RelayCommand(() => SelectAll(OtherCandidates));
            DeselectAllOthersCommand = new RelayCommand(() => DeselectAll(OtherCandidates));
            
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

        private void SelectAll(ObservableCollection<SelectableCachedCandidate> collection)
        {
            foreach (var item in collection)
                item.IsSelected = true;
            UpdateCommandStates();
        }

        private void DeselectAll(ObservableCollection<SelectableCachedCandidate> collection)
        {
            foreach (var item in collection)
                item.IsSelected = false;
            UpdateCommandStates();
        }

        private void UpdateCommandStates()
        {
            // Ensure we're on the UI thread
            if (!_dispatcher.CheckAccess())
            {
                _dispatcher.InvokeAsync(() => UpdateCommandStates());
                return;
            }

            ((AsyncRelayCommand)AcceptSelectedOrganismsCommand).NotifyCanExecuteChanged();
            ((AsyncRelayCommand)RejectSelectedOrganismsCommand).NotifyCanExecuteChanged();
            ((AsyncRelayCommand)AcceptSelectedSubstancesCommand).NotifyCanExecuteChanged();
            ((AsyncRelayCommand)RejectSelectedSubstancesCommand).NotifyCanExecuteChanged();
            ((AsyncRelayCommand)AcceptSelectedOthersCommand).NotifyCanExecuteChanged();
            ((AsyncRelayCommand)RejectSelectedOthersCommand).NotifyCanExecuteChanged();
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
                
                // If pending count increased, refresh lists
                if (PendingCount > previousPendingCount)
                {
                    Debug.WriteLine("[CacheReviewViewModel] New candidates cached - refreshing lists");
                    await RefreshListsAsync().ConfigureAwait(false);
                }
            });
        }

        private async Task RefreshAsync()
        {
            try
            {
                IsBusy = true;
                StatusMessage = "Loading pending candidates...";

                await RefreshListsAsync().ConfigureAwait(false);

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

        private async Task RefreshListsAsync()
        {
            // Load all pending candidates (on background thread - OK)
            var pending = await _cacheService.GetPendingCandidatesAsync(100).ConfigureAwait(false);
            
            PendingCount = pending.Count;

            // Categorize candidates (on background thread - OK)
            var organisms = pending.Where(c => GetCandidateCategory(c) == CandidateCategory.Organism).ToList();
            var substances = pending.Where(c => GetCandidateCategory(c) == CandidateCategory.Substance).ToList();
            var others = pending.Where(c => GetCandidateCategory(c) == CandidateCategory.Other).ToList();

            // ALL collection updates MUST happen on UI thread
            await _dispatcher.InvokeAsync(() =>
            {
                // Unsubscribe from old items before clearing
                foreach (var item in OrganismCandidates)
                    item.PropertyChanged -= OnSelectablePropertyChanged;
                foreach (var item in SubstanceCandidates)
                    item.PropertyChanged -= OnSelectablePropertyChanged;
                foreach (var item in OtherCandidates)
                    item.PropertyChanged -= OnSelectablePropertyChanged;

                OrganismCandidates.Clear();
                foreach (var candidate in organisms)
                {
                    var selectable = new SelectableCachedCandidate(candidate);
                    selectable.PropertyChanged += OnSelectablePropertyChanged;
                    OrganismCandidates.Add(selectable);
                }

                SubstanceCandidates.Clear();
                foreach (var candidate in substances)
                {
                    var selectable = new SelectableCachedCandidate(candidate);
                    selectable.PropertyChanged += OnSelectablePropertyChanged;
                    SubstanceCandidates.Add(selectable);
                }

                OtherCandidates.Clear();
                foreach (var candidate in others)
                {
                    var selectable = new SelectableCachedCandidate(candidate);
                    selectable.PropertyChanged += OnSelectablePropertyChanged;
                    OtherCandidates.Add(selectable);
                }

                // Update command states (now guaranteed to be on UI thread)
                UpdateCommandStates();
            });
        }

        private void OnSelectablePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SelectableCachedCandidate.IsSelected))
            {
                UpdateCommandStates();
            }
        }

        private async Task AcceptSelectedAsync(ObservableCollection<SelectableCachedCandidate> collection)
        {
            var selected = collection.Where(c => c.IsSelected).ToList();
            
            if (selected.Count == 0)
                return;

            try
            {
                IsBusy = true;
                StatusMessage = $"Accepting {selected.Count} candidates...";

                foreach (var item in selected)
                {
                    await _cacheService.MarkAcceptedAsync(item.Candidate.Id).ConfigureAwait(false);
                    AcceptedCount++;
                }

                // Remove from collection
                await _dispatcher.InvokeAsync(() =>
                {
                    foreach (var item in selected)
                    {
                        collection.Remove(item);
                    }
                });

                PendingCount = await _cacheService.GetPendingCountAsync().ConfigureAwait(false);
                StatusMessage = $"Accepted {selected.Count} candidates (ready to save)";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error accepting candidates: {ex.Message}";
                Debug.WriteLine($"[CacheReviewViewModel] Error accepting: {ex}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task RejectSelectedAsync(ObservableCollection<SelectableCachedCandidate> collection)
        {
            var selected = collection.Where(c => c.IsSelected).ToList();
            
            if (selected.Count == 0)
                return;

            try
            {
                IsBusy = true;
                StatusMessage = $"Rejecting {selected.Count} candidates...";

                foreach (var item in selected)
                {
                    await _cacheService.MarkRejectedAsync(item.Candidate.Id).ConfigureAwait(false);
                    RejectedCount++;
                }

                // Remove from collection
                await _dispatcher.InvokeAsync(() =>
                {
                    foreach (var item in selected)
                    {
                        collection.Remove(item);
                    }
                });

                PendingCount = await _cacheService.GetPendingCountAsync().ConfigureAwait(false);
                StatusMessage = $"Rejected {selected.Count} candidates";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error rejecting candidates: {ex.Message}";
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
            
            // Unsubscribe from selectable items
            foreach (var item in OrganismCandidates)
                item.PropertyChanged -= OnSelectablePropertyChanged;
            foreach (var item in SubstanceCandidates)
                item.PropertyChanged -= OnSelectablePropertyChanged;
            foreach (var item in OtherCandidates)
                item.PropertyChanged -= OnSelectablePropertyChanged;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
