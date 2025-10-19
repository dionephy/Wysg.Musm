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

            // Load phrases on initialization
            _ = RefreshPhrasesAsync();
            _ = LoadAllNonGlobalAsync();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
