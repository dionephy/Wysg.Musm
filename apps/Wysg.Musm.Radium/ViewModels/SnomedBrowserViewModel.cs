using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Wysg.Musm.Radium.Services;

namespace Wysg.Musm.Radium.ViewModels
{
    /// <summary>
    /// ViewModel for the SNOMED CT Browser Window.
    /// Allows browsing all SNOMED concepts by domain (semantic tag) with pagination.
    /// Uses token caching for efficient "Next" navigation.
    /// </summary>
    public sealed class SnomedBrowserViewModel : INotifyPropertyChanged
    {
        private readonly ISnowstormClient _snowstormClient;
        private readonly IPhraseService _phraseService;
        private readonly ISnomedMapService _snomedMapService;

        private string _selectedDomain = "body structure";
        private int _currentPage = 1;
        private int _totalPages = 1;
        private int _jumpToPage = 1;
        private bool _isBusy;
        private string _statusMessage = string.Empty;
        private const int ConceptsPerPage = 10;

        // Token cache: Maps page number to searchAfter token for that page
        // Key: page number, Value: searchAfter token to use when loading that page
        private readonly Dictionary<int, string> _pageTokenCache = new();
        private string? _lastSearchAfterToken = null; // Token for the NEXT page after current

        public ObservableCollection<SnomedConceptViewModel> Concepts { get; } = new();

        public string[] AvailableDomains { get; } = new[]
        {
            "all",
            "body structure",
            "finding",
            "disorder",
            "procedure",
            "observable entity",
            "substance"
        };

        public string SelectedDomain
        {
            get => _selectedDomain;
            set
            {
                if (_selectedDomain != value)
                {
                    _selectedDomain = value;
                    OnPropertyChanged();
                    CurrentPage = 1; // Reset to page 1 on domain change
                    _pageTokenCache.Clear(); // Clear token cache when domain changes
                    _lastSearchAfterToken = null;
                    _ = LoadConceptsAsync();
                }
            }
        }

        public int CurrentPage
        {
            get => _currentPage;
            private set
            {
                if (_currentPage != value)
                {
                    _currentPage = value;
                    _jumpToPage = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(JumpToPage));
                    OnPropertyChanged(nameof(PageInfo));
                    OnPropertyChanged(nameof(CanGoToPreviousPage));
                    OnPropertyChanged(nameof(CanGoToNextPage));
                }
            }
        }

        public int JumpToPage
        {
            get => _jumpToPage;
            set
            {
                if (_jumpToPage != value)
                {
                    _jumpToPage = value;
                    OnPropertyChanged();
                }
            }
        }

        public int TotalPages
        {
            get => _totalPages;
            private set
            {
                if (_totalPages != value)
                {
                    _totalPages = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(PageInfo));
                }
            }
        }

        public string PageInfo => $"Page {CurrentPage} of {TotalPages}";

        public bool CanGoToPreviousPage => CurrentPage > 1 && !IsBusy;
        public bool CanGoToNextPage => CurrentPage < TotalPages && !IsBusy;

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (_isBusy != value)
                {
                    _isBusy = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CanGoToPreviousPage));
                    OnPropertyChanged(nameof(CanGoToNextPage));
                    ((AsyncRelayCommand)LoadPageCommand).NotifyCanExecuteChanged();
                    ((RelayCommand)PreviousPageCommand).NotifyCanExecuteChanged();
                    ((RelayCommand)NextPageCommand).NotifyCanExecuteChanged();
                    ((RelayCommand)JumpToPageCommand).NotifyCanExecuteChanged();
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

        public IAsyncRelayCommand LoadPageCommand { get; }
        public IRelayCommand PreviousPageCommand { get; }
        public IRelayCommand NextPageCommand { get; }
        public IRelayCommand JumpToPageCommand { get; }

        public SnomedBrowserViewModel(
            ISnowstormClient snowstormClient,
            IPhraseService phraseService,
            ISnomedMapService snomedMapService)
        {
            _snowstormClient = snowstormClient;
            _phraseService = phraseService;
            _snomedMapService = snomedMapService;

            LoadPageCommand = new AsyncRelayCommand(LoadConceptsAsync, () => !IsBusy);
            PreviousPageCommand = new RelayCommand(GoToPreviousPage, () => CanGoToPreviousPage);
            NextPageCommand = new RelayCommand(GoToNextPage, () => CanGoToNextPage);
            JumpToPageCommand = new RelayCommand(
                () => { CurrentPage = Math.Max(1, Math.Min(JumpToPage, TotalPages)); _ = LoadConceptsAsync(); },
                () => !IsBusy && JumpToPage >= 1 && JumpToPage <= TotalPages
            );

            // Estimate total pages (we'll refine this as we browse)
            TotalPages = 100; // Initial estimate

            // Load first page
            _ = LoadConceptsAsync();
        }

        /// <summary>
        /// Check if a phrase already exists in global phrases for the given concept.
        /// Only considers ACTIVE phrases (soft-deleted phrases are ignored).
        /// </summary>
        public async Task<bool> IsPhraseExistsAsync(string phraseText, long conceptId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[SnomedBrowserVM.IsPhraseExists] Checking: term='{phraseText}', conceptId={conceptId}");
                
                // Get all global phrase metadata
                var globalPhrases = await _phraseService.GetAllGlobalPhraseMetaAsync();
                
                System.Diagnostics.Debug.WriteLine($"[SnomedBrowserVM.IsPhraseExists] Loaded {globalPhrases.Count} global phrases");
                
                // Normalize the phrase text for comparison (phrases are saved as lowercase)
                var normalizedText = phraseText.Trim().ToLowerInvariant();
                
                System.Diagnostics.Debug.WriteLine($"[SnomedBrowserVM.IsPhraseExists] Normalized text: '{normalizedText}'");
                
                // Check if an ACTIVE phrase with this text exists (ignore soft-deleted/inactive phrases)
                var existingPhrase = globalPhrases.FirstOrDefault(p => 
                    p.Active && // IMPORTANT: Only check active phrases
                    p.Text.Trim().ToLowerInvariant() == normalizedText);
                
                if (existingPhrase == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[SnomedBrowserVM.IsPhraseExists] No matching phrase found");
                    return false;
                }
                
                System.Diagnostics.Debug.WriteLine($"[SnomedBrowserVM.IsPhraseExists] Found matching phrase: id={existingPhrase.Id}, text='{existingPhrase.Text}'");
                
                // Phrase exists and is active - now check if it's mapped to this concept
                var mapping = await _snomedMapService.GetMappingAsync(existingPhrase.Id);
                
                if (mapping == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[SnomedBrowserVM.IsPhraseExists] Phrase found but NO mapping exists");
                    return false;
                }
                
                System.Diagnostics.Debug.WriteLine($"[SnomedBrowserVM.IsPhraseExists] Mapping found: conceptId={mapping.ConceptId}");
                
                // If mapping exists and matches this concept, return true
                var matches = mapping.ConceptId == conceptId;
                System.Diagnostics.Debug.WriteLine($"[SnomedBrowserVM.IsPhraseExists] Concept ID match: {matches} (expected={conceptId}, actual={mapping.ConceptId})");
                
                return matches;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SnomedBrowserVM.IsPhraseExists] Error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[SnomedBrowserVM.IsPhraseExists] Stack trace: {ex.StackTrace}");
                return false; // On error, assume phrase doesn't exist
            }
        }

        private async Task LoadConceptsAsync()
        {
            try
            {
                IsBusy = true;
                var offset = (CurrentPage - 1) * ConceptsPerPage;
                
                // Check if we have a cached token for this page
                string? searchAfterToken = null;
                if (_pageTokenCache.TryGetValue(CurrentPage, out var cachedToken))
                {
                    searchAfterToken = cachedToken;
                    StatusMessage = $"Loading {SelectedDomain} concepts (page {CurrentPage}, using cached token)...";
                    System.Diagnostics.Debug.WriteLine($"[SnomedBrowserVM] Using cached token for page {CurrentPage}: {searchAfterToken}");
                }
                else
                {
                    StatusMessage = $"Loading {SelectedDomain} concepts (page {CurrentPage}, offset {offset}, limit {ConceptsPerPage})...";
                    System.Diagnostics.Debug.WriteLine($"[SnomedBrowserVM] No cached token for page {CurrentPage}, will paginate from beginning");
                }

                Concepts.Clear();

                var (concepts, nextSearchAfter) = await _snowstormClient.BrowseBySemanticTagAsync(
                    SelectedDomain, 
                    offset, 
                    ConceptsPerPage, 
                    searchAfterToken);

                System.Diagnostics.Debug.WriteLine($"[SnomedBrowserVM] Received {concepts.Count} concepts from Snowstorm");
                System.Diagnostics.Debug.WriteLine($"[SnomedBrowserVM] Next searchAfter token: {nextSearchAfter ?? "(null)"}");

                // Cache the token for the NEXT page if available
                if (!string.IsNullOrEmpty(nextSearchAfter))
                {
                    var nextPage = CurrentPage + 1;
                    _pageTokenCache[nextPage] = nextSearchAfter;
                    _lastSearchAfterToken = nextSearchAfter;
                    System.Diagnostics.Debug.WriteLine($"[SnomedBrowserVM] Cached token for page {nextPage}");
                }
                else
                {
                    _lastSearchAfterToken = null;
                    System.Diagnostics.Debug.WriteLine($"[SnomedBrowserVM] No token to cache (end of results or error)");
                }

                if (concepts.Count == 0 && CurrentPage > 1)
                {
                    // We've reached the end
                    TotalPages = CurrentPage - 1;
                    CurrentPage = TotalPages;
                    StatusMessage = "Reached end of results - no concepts on this page";
                    return;
                }

                var totalTerms = 0;
                foreach (var concept in concepts)
                {
                    System.Diagnostics.Debug.WriteLine($"[SnomedBrowserVM] Concept {concept.ConceptIdStr}: {concept.Fsn} has {concept.AllTerms.Count} terms");
                    foreach (var term in concept.AllTerms)
                    {
                        System.Diagnostics.Debug.WriteLine($"  - {term.Type}: {term.Term}");
                    }
                    
                    var vm = new SnomedConceptViewModel(concept, this);
                    totalTerms += vm.Terms.Count;
                    Concepts.Add(vm);
                }

                System.Diagnostics.Debug.WriteLine($"[SnomedBrowserVM] Created {Concepts.Count} concept VMs with {totalTerms} total terms");

                // If we got a full page and have a next token, there might be more
                if (concepts.Count == ConceptsPerPage && !string.IsNullOrEmpty(nextSearchAfter) && CurrentPage >= TotalPages)
                {
                    TotalPages = CurrentPage + 1;
                }

                StatusMessage = $"Loaded {Concepts.Count} concepts ({totalTerms} terms) on page {CurrentPage}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading concepts: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"[SnomedBrowserVM] Error: {ex}");
                System.Windows.MessageBox.Show(
                    $"Failed to load SNOMED concepts:\n{ex.Message}\n\nMake sure Snowstorm is running.",
                    "Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void GoToPreviousPage()
        {
            if (CanGoToPreviousPage)
            {
                CurrentPage--;
                _ = LoadConceptsAsync();
            }
        }

        private void GoToNextPage()
        {
            if (CanGoToNextPage)
            {
                CurrentPage++;
                _ = LoadConceptsAsync();
            }
        }

        public async Task AddTermAsPhraseAsync(string term, SnomedConcept concept)
        {
            try
            {
                var phraseText = term.ToLowerInvariant().Trim();

                // 1. Create the phrase
                var newPhrase = await _phraseService.UpsertPhraseAsync(
                    accountId: null,  // NULL = global phrase
                    text: phraseText,
                    active: true
                );

                // 2. Cache the SNOMED concept
                await _snomedMapService.CacheConceptAsync(concept);

                // 3. Map phrase to concept
                await _snomedMapService.MapPhraseAsync(
                    newPhrase.Id,
                    accountId: null,
                    concept.ConceptId,
                    mappingType: "exact",
                    confidence: 1.0m,
                    notes: "Added via SNOMED Browser"
                );

                StatusMessage = $"Added phrase: {phraseText}";

                // Refresh the concept to show it's been added
                var conceptVm = Concepts.FirstOrDefault(c => c.Concept.ConceptId == concept.ConceptId);
                if (conceptVm != null)
                {
                    await conceptVm.RefreshTermStatesAsync();
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error adding phrase: {ex.Message}";
                System.Windows.MessageBox.Show(
                    $"Failed to add phrase:\n{ex.Message}",
                    "Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Represents a single SNOMED concept with all its terms.
    /// Each concept displays FSN and PT as separate term rows.
    /// </summary>
    public sealed class SnomedConceptViewModel : INotifyPropertyChanged
    {
        private readonly SnomedBrowserViewModel _parent;
        private bool _hasExistingPhrases;

        public SnomedConcept Concept { get; }
        public ObservableCollection<SnomedTermViewModel> Terms { get; } = new();

        public string ConceptIdDisplay => $"[{Concept.ConceptIdStr}]";
        public string SemanticTag { get; }

        /// <summary>
        /// Indicates if any term in this concept already exists as a global phrase with this concept mapping.
        /// Used to highlight the entire concept panel in dark red.
        /// </summary>
        public bool HasExistingPhrases
        {
            get => _hasExistingPhrases;
            private set
            {
                if (_hasExistingPhrases != value)
                {
                    _hasExistingPhrases = value;
                    OnPropertyChanged();
                }
            }
        }

        public SnomedConceptViewModel(SnomedConceptWithTerms concept, SnomedBrowserViewModel parent)
        {
            // Convert to base SnomedConcept for phrase mapping
            Concept = new SnomedConcept(concept.ConceptId, concept.ConceptIdStr, concept.Fsn, concept.Pt, concept.Active, concept.CachedAt);
            _parent = parent;

            // Extract semantic tag from FSN
            SemanticTag = ExtractSemanticTag(concept.Fsn) ?? "unknown";

            // Add ALL terms from the concept - NO filtering, NO deduplication
            // Show every single description exactly as Snowstorm returns it
            foreach (var term in concept.AllTerms)
            {
                var termVm = new SnomedTermViewModel(term.Term, term.Type, Concept, parent);
                // Subscribe to term's IsAdded changes to update concept-level flag
                termVm.PropertyChanged += OnTermPropertyChanged;
                Terms.Add(termVm);
            }
        }

        private void OnTermPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SnomedTermViewModel.IsAdded))
            {
                // Update concept-level flag whenever any term's IsAdded changes
                UpdateHasExistingPhrases();
            }
        }

        private void UpdateHasExistingPhrases()
        {
            // Concept has existing phrases if ANY term is marked as added
            HasExistingPhrases = Terms.Any(t => t.IsAdded);
        }

        public async Task RefreshTermStatesAsync()
        {
            // Refresh the existence state for all terms in this concept
            foreach (var term in Terms)
            {
                await term.CheckExistenceAsync();
            }
            
            // Update concept-level flag after refreshing all terms
            UpdateHasExistingPhrases();
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

        private static string ExtractTermFromFsn(string? fsn)
        {
            if (string.IsNullOrWhiteSpace(fsn)) return string.Empty;
            var lastOpenParen = fsn.LastIndexOf('(');
            if (lastOpenParen > 0)
                return fsn.Substring(0, lastOpenParen).Trim();
            return fsn.Trim();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Represents a single term (FSN, PT, or synonym) for a SNOMED concept.
    /// </summary>
    public sealed class SnomedTermViewModel : INotifyPropertyChanged
    {
        private readonly SnomedBrowserViewModel _parent;
        private bool _isAdded;
        private bool _isChecking;

        public string Term { get; }
        public string TermType { get; }
        public SnomedConcept Concept { get; }

        public bool IsAdded
        {
            get => _isAdded;
            set
            {
                if (_isAdded != value)
                {
                    _isAdded = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(AddButtonText));
                    ((AsyncRelayCommand)AddCommand).NotifyCanExecuteChanged();
                }
            }
        }

        public bool IsChecking
        {
            get => _isChecking;
            set
            {
                if (_isChecking != value)
                {
                    _isChecking = value;
                    OnPropertyChanged();
                    ((AsyncRelayCommand)AddCommand).NotifyCanExecuteChanged();
                }
            }
        }

        public string AddButtonText => IsAdded ? "? Added" : "Add";

        public IAsyncRelayCommand AddCommand { get; }

        public SnomedTermViewModel(string term, string termType, SnomedConcept concept, SnomedBrowserViewModel parent)
        {
            Term = term;
            TermType = termType;
            Concept = concept;
            _parent = parent;

            AddCommand = new AsyncRelayCommand(
                async () =>
                {
                    await _parent.AddTermAsPhraseAsync(Term, Concept);
                    IsAdded = true;
                },
                () => !IsAdded && !IsChecking
            );

            // Check if this phrase already exists
            _ = CheckExistenceAsync();
        }

        public async Task CheckExistenceAsync()
        {
            try
            {
                IsChecking = true;
                
                System.Diagnostics.Debug.WriteLine($"[SnomedTermVM] Checking existence for term: '{Term}' with concept: {Concept.ConceptId}");
                
                var exists = await _parent.IsPhraseExistsAsync(Term, Concept.ConceptId);
                
                System.Diagnostics.Debug.WriteLine($"[SnomedTermVM] Existence check result for '{Term}': {exists}");
                
                if (exists)
                {
                    IsAdded = true; // Mark as already added
                    System.Diagnostics.Debug.WriteLine($"[SnomedTermVM] Marked '{Term}' as added (button disabled)");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SnomedTermVM] Error checking existence: {ex.Message}");
            }
            finally
            {
                IsChecking = false;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
