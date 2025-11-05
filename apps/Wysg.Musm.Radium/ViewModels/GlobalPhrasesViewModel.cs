using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Wysg.Musm.Radium.Services;
using CommunityToolkit.Mvvm.Input;

namespace Wysg.Musm.Radium.ViewModels
{
    /// <summary>
    /// ViewModel for managing global phrases (account_id IS NULL).
    /// Global phrases are available to all accounts.
    /// Follows synchronous database flow (FR-258..FR-260, FR-273..FR-278).
    /// 
    /// Split into partial classes for maintainability:
    /// - GlobalPhrasesViewModel.cs (this file): Core properties and constructor
    /// - GlobalPhrasesViewModel.Commands.cs: Command definitions and handlers  
    /// - GlobalPhrasesViewModel.BulkSnomed.cs: Bulk SNOMED search functionality
    /// 
    /// Performance optimizations (2025-02-02):
    /// - Search/filter functionality to reduce visible items
    /// - Pagination support to limit items loaded at once (separate from Bulk SNOMED pagination)
    /// - Deferred SNOMED mapping loading for large datasets
    /// </summary>
    public sealed partial class GlobalPhrasesViewModel : INotifyPropertyChanged
    {
        private readonly IPhraseService _phraseService;
        private readonly IPhraseCache _cache;
      private readonly ITenantContext _tenant;
  private readonly ISnomedMapService? _snomedMapService;
        private readonly ISnowstormClient? _snowstormClient;
        
        private string _newPhraseText = string.Empty;
        private bool _isBusy;
        private string _statusMessage = string.Empty;
        private string _searchAccountText = string.Empty;
      private long? _sourceAccountId;
      private string _snomedSearchText = string.Empty;
        private SnomedConcept? _selectedSnomedConcept;
        
    // Performance optimization fields (2025-02-02)
        // Prefix with "Phrase" to avoid naming conflicts with BulkSnomed partial class pagination
        private string _phraseSearchFilter = string.Empty;
        private int _phrasePageSize = 100;
        private int _phraseCurrentPageIndex = 0;
        private int _phraseTotalCount = 0;
    private System.Collections.Generic.List<PhraseInfo> _allPhrasesCache = new();

        public ObservableCollection<GlobalPhraseItem> Items { get; } = new();
   public ObservableCollection<AccountPhraseItem> AccountPhrases { get; } = new();
        public ObservableCollection<SnomedConcept> SnomedSearchResults { get; } = new();
        public ObservableCollection<SelectableSnomedConcept> BulkSnomedSearchResults { get; } = new();

        public string NewPhraseText
        {
    get => _newPhraseText;
            set
         {
                if (_newPhraseText != value)
     {
    _newPhraseText = value;
        OnPropertyChanged();
     ((AsyncRelayCommand)AddPhraseCommand).NotifyCanExecuteChanged();
          }
 }
        }

      public bool IsBusy
     {
      get => _isBusy;
            private set
            {
                if (_isBusy != value)
    {
             _isBusy = value;
        OnPropertyChanged();
  InitializeCommands();
          
        // Notify all item commands that depend on parent IsBusy
              foreach (var item in Items)
           {
  item.NotifyCommandsCanExecuteChanged();
            }
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

        public string SearchAccountText
        {
   get => _searchAccountText;
       set
            {
             if (_searchAccountText != value)
            {
        _searchAccountText = value;
      OnPropertyChanged();
            ((AsyncRelayCommand)SearchAccountPhrasesCommand).NotifyCanExecuteChanged();
 }
 }
        }

        public string SnomedSearchText
        {
          get => _snomedSearchText;
            set
            {
   if (_snomedSearchText != value)
    {
    _snomedSearchText = value;
   OnPropertyChanged();
   ((AsyncRelayCommand)SearchSnomedCommand).NotifyCanExecuteChanged();
   }
  }
        }

        public SnomedConcept? SelectedSnomedConcept
   {
            get => _selectedSnomedConcept;
       set
   {
          if (_selectedSnomedConcept != value)
         {
           _selectedSnomedConcept = value;
        OnPropertyChanged();
           OnPropertyChanged(nameof(HasSelectedConcept));
          OnPropertyChanged(nameof(SelectedConceptDisplay));
    ((AsyncRelayCommand)AddPhraseWithSnomedCommand).NotifyCanExecuteChanged();
        }
 }
        }

        public bool HasSelectedConcept => SelectedSnomedConcept != null;

        public string SelectedConceptDisplay =>
            SelectedSnomedConcept != null
      ? $"{SelectedSnomedConcept.ConceptIdStr} | {SelectedSnomedConcept.Fsn}"
       : string.Empty;

        // Performance optimization properties (2025-02-02)
      // Phrase list search/filter (separate from Bulk SNOMED search)
  public string PhraseSearchFilter
        {
            get => _phraseSearchFilter;
            set
  {
                if (_phraseSearchFilter != value)
                {
        _phraseSearchFilter = value;
   OnPropertyChanged();
           _phraseCurrentPageIndex = 0; // Reset to first page on search
    OnPropertyChanged(nameof(PhraseCurrentPageIndex));
 OnPropertyChanged(nameof(PhrasePageInfo));
             ApplyPhraseFilter();
 }
            }
      }

        public int PhrasePageSize
        {
   get => _phrasePageSize;
       set
  {
            var clamped = Math.Max(10, Math.Min(500, value)); // Clamp 10-500
       if (_phrasePageSize != clamped)
         {
            _phrasePageSize = clamped;
      OnPropertyChanged();
                    _phraseCurrentPageIndex = 0; // Reset to first page
          OnPropertyChanged(nameof(PhraseCurrentPageIndex));
     OnPropertyChanged(nameof(PhrasePageInfo));
      OnPropertyChanged(nameof(CanGoToPhraseNextPage));
        OnPropertyChanged(nameof(CanGoToPhrasePreviousPage));
     ApplyPhraseFilter();
      }
            }
        }

        public int PhraseCurrentPageIndex
   {
 get => _phraseCurrentPageIndex;
      private set
            {
        if (_phraseCurrentPageIndex != value)
       {
          _phraseCurrentPageIndex = value;
             OnPropertyChanged();
        OnPropertyChanged(nameof(PhrasePageInfo));
         OnPropertyChanged(nameof(CanGoToPhraseNextPage));
            OnPropertyChanged(nameof(CanGoToPhrasePreviousPage));
    }
   }
        }

      public int PhraseTotalCount
        {
 get => _phraseTotalCount;
            private set
     {
           if (_phraseTotalCount != value)
              {
         _phraseTotalCount = value;
             OnPropertyChanged();
           OnPropertyChanged(nameof(PhrasePageInfo));
          OnPropertyChanged(nameof(CanGoToPhraseNextPage));
           }
       }
   }

        public string PhrasePageInfo
        {
   get
            {
      var totalPages = Math.Max(1, (int)Math.Ceiling((double)PhraseTotalCount / PhrasePageSize));
          var currentPage = _phraseCurrentPageIndex + 1;
return $"Page {currentPage} of {totalPages} ({PhraseTotalCount} total)";
   }
        }

        public bool CanGoToPhraseNextPage => (_phraseCurrentPageIndex + 1) * PhrasePageSize < PhraseTotalCount;
public bool CanGoToPhrasePreviousPage => _phraseCurrentPageIndex > 0;

        // Command properties for phrase list pagination (2025-02-02)
public IRelayCommand PhraseSearchCommand { get; }
        public IRelayCommand PhraseClearFilterCommand { get; }
    public IRelayCommand PhraseFirstPageCommand { get; }
        public IRelayCommand PhraseLastPageCommand { get; }
        public IRelayCommand PhrasePreviousPageCommand { get; }
      public IRelayCommand PhraseNextPageCommand { get; }

        public GlobalPhrasesViewModel(
         IPhraseService phraseService, 
      IPhraseCache cache, 
  ITenantContext tenant, 
       ISnomedMapService? snomedMapService = null,
            ISnowstormClient? snowstormClient = null)
        {
            _phraseService = phraseService;
       _cache = cache;
       _tenant = tenant;
            _snomedMapService = snomedMapService;
     _snowstormClient = snowstormClient;

            // Initialize commands (defined in GlobalPhrasesViewModel.Commands.cs)
    AddPhraseCommand = new AsyncRelayCommand(
         AddPhraseAsync,
    () => !IsBusy && !string.IsNullOrWhiteSpace(NewPhraseText)
            );

            RefreshCommand = new AsyncRelayCommand(
     RefreshPhrasesAsync,
   () => !IsBusy
            );

          SearchAccountPhrasesCommand = new AsyncRelayCommand(
             SearchAccountPhrasesAsync,
             () => !IsBusy
   );

      ConvertSelectedCommand = new AsyncRelayCommand(
      ConvertSelectedToGlobalAsync,
                () => !IsBusy
 );

            SelectAllAccountPhrasesCommand = new RelayCommand(
         SelectAllAccountPhrases,
         () => !IsBusy && AccountPhrases.Count > 0
   );

            BulkImportCommand = new AsyncRelayCommand(
                BulkImportAsync,
            () => !IsBusy
      );

  SearchSnomedCommand = new AsyncRelayCommand(
                SearchSnomedAsync,
      () => !IsBusy && !string.IsNullOrWhiteSpace(SnomedSearchText)
            );

     AddPhraseWithSnomedCommand = new AsyncRelayCommand(
                AddPhraseWithSnomedAsync,
           () => !IsBusy && SelectedSnomedConcept != null && !string.IsNullOrWhiteSpace(SnomedSearchText)
            );

            // Bulk SNOMED commands (defined in GlobalPhrasesViewModel.BulkSnomed.cs)
            BulkSearchSnomedCommand = new AsyncRelayCommand(
    BulkSearchSnomedAsync,
                () => !IsBusy && !string.IsNullOrWhiteSpace(BulkSnomedSearchText)
            );

   BulkAddPhrasesWithSnomedCommand = new AsyncRelayCommand(
              BulkAddPhrasesWithSnomedAsync,
          () => !IsBusy && BulkSnomedSearchResults.Any(c => c.IsSelected)
            );

      SelectAllBulkResultsCommand = new RelayCommand(
    SelectAllBulkResults,
     () => !IsBusy && BulkSnomedSearchResults.Count > 0
            );

       PreviousPageCommand = new RelayCommand(
      GoToPreviousPage,
          () => !IsBusy && CanGoToPreviousPage
       );

 NextPageCommand = new RelayCommand(
           GoToNextPage,
      () => !IsBusy && CanGoToNextPage
            );

            // Phrase list pagination commands (2025-02-02)
            PhraseSearchCommand = new RelayCommand(
     () => { _phraseCurrentPageIndex = 0; ApplyPhraseFilter(); },
  () => !IsBusy
            );

       PhraseClearFilterCommand = new RelayCommand(
      () => { PhraseSearchFilter = string.Empty; },
        () => !IsBusy && !string.IsNullOrWhiteSpace(PhraseSearchFilter)
      );

          PhraseFirstPageCommand = new RelayCommand(
         () => { PhraseCurrentPageIndex = 0; ApplyPhraseFilter(); },
       () => !IsBusy && _phraseCurrentPageIndex > 0
       );

    PhraseLastPageCommand = new RelayCommand(
       () =>
         {
              var lastPage = Math.Max(0, (int)Math.Ceiling((double)PhraseTotalCount / PhrasePageSize) - 1);
                    PhraseCurrentPageIndex = lastPage;
   ApplyPhraseFilter();
    },
  () => !IsBusy && CanGoToPhraseNextPage
     );

        PhrasePreviousPageCommand = new RelayCommand(
                () =>
     {
          if (CanGoToPhrasePreviousPage)
          {
    PhraseCurrentPageIndex--;
    ApplyPhraseFilter();
         }
     },
       () => !IsBusy && CanGoToPhrasePreviousPage
    );

      PhraseNextPageCommand = new RelayCommand(
    () =>
  {
         if (CanGoToPhraseNextPage)
     {
          PhraseCurrentPageIndex++;
    ApplyPhraseFilter();
 }
    },
      () => !IsBusy && CanGoToPhraseNextPage
            );

       // Load phrases on initialization
_ = RefreshPhrasesAsync();
            _ = LoadAllNonGlobalAsync();
        }

    /// <summary>
        /// Apply search filter and pagination to the phrase list.
        /// Only loads the current page into the UI-bound Items collection.
      /// Phrases are sorted alphabetically (case-insensitive).
/// </summary>
   private async void ApplyPhraseFilter()
   {
   if (_allPhrasesCache == null || _allPhrasesCache.Count == 0)
          {
  Items.Clear();
    PhraseTotalCount = 0;
          return;
       }

 // Filter by search text
 var filtered = _allPhrasesCache.AsEnumerable();
  if (!string.IsNullOrWhiteSpace(_phraseSearchFilter))
  {
          var search = _phraseSearchFilter.Trim().ToLowerInvariant();
        filtered = filtered.Where(p => p.Text.ToLowerInvariant().Contains(search));
 }

       // Sort alphabetically (case-insensitive) - 2025-02-02
   var filteredList = filtered.OrderBy(p => p.Text, StringComparer.OrdinalIgnoreCase).ToList();
          PhraseTotalCount = filteredList.Count;

     // Apply pagination - only load one page
  var page = filteredList
          .Skip(_phraseCurrentPageIndex * _phrasePageSize)
    .Take(_phrasePageSize)
      .ToList();

   // Update UI collection
   Items.Clear();
       
      // Load SNOMED mappings for visible items (batch query for performance) - 2025-02-02
        System.Collections.Generic.Dictionary<long, PhraseSnomedMapping>? mappings = null;
  if (_snomedMapService != null && page.Count > 0)
        {
          try
      {
      var phraseIds = page.Select(p => p.Id).ToList();
             var mappingDict = await _snomedMapService.GetMappingsBatchAsync(phraseIds);
      mappings = new System.Collections.Generic.Dictionary<long, PhraseSnomedMapping>(mappingDict);
            }
   catch
   {
    // Silently fail if SNOMED service unavailable
          mappings = null;
      }
        }

        foreach (var phrase in page)
     {
      var item = new GlobalPhraseItem(phrase, this);

    // Apply SNOMED mapping if available (2025-02-02)
    if (mappings != null && mappings.TryGetValue(phrase.Id, out var mapping))
            {
var semanticTag = mapping.GetSemanticTag();
   item.SnomedSemanticTag = semanticTag;
    // Include FSN for better visibility - 2025-02-02
    item.SnomedMappingText = $"{mapping.Fsn} (SNOMED {mapping.ConceptIdStr})";
    }

    Items.Add(item);
 }

      StatusMessage = string.IsNullOrWhiteSpace(_phraseSearchFilter)
     ? $"Showing page {_phraseCurrentPageIndex + 1} ({PhraseTotalCount} total phrases, sorted A-Z)"
     : $"Found {PhraseTotalCount} phrases matching '{_phraseSearchFilter}' (page {_phraseCurrentPageIndex + 1}, sorted A-Z)";

  // Notify command state changes
 ((RelayCommand)PhraseFirstPageCommand).NotifyCanExecuteChanged();
          ((RelayCommand)PhraseLastPageCommand).NotifyCanExecuteChanged();
     ((RelayCommand)PhrasePreviousPageCommand).NotifyCanExecuteChanged();
   ((RelayCommand)PhraseNextPageCommand).NotifyCanExecuteChanged();
       ((RelayCommand)PhraseClearFilterCommand).NotifyCanExecuteChanged();
    }

      public event PropertyChangedEventHandler? PropertyChanged;

      private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
   }
    }
}
