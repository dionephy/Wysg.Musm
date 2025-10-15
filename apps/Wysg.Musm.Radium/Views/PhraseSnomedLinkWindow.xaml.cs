using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Wysg.Musm.Radium.Services;

namespace Wysg.Musm.Radium.Views
{
    public partial class PhraseSnomedLinkWindow : Window
    {
        public PhraseSnomedLinkWindowViewModel VM { get; }
        public PhraseSnomedLinkWindow(long phraseId, string phraseText, long? accountId, ISnomedMapService mapSvc, ISnowstormClient snow)
        {
            InitializeComponent();
            VM = new PhraseSnomedLinkWindowViewModel(phraseId, phraseText, accountId, mapSvc, snow);
            DataContext = VM;
        }
    }

    public sealed partial class PhraseSnomedLinkWindowViewModel : ObservableObject
    {
        private readonly long _phraseId;
        private readonly long? _accountId;
        private readonly ISnomedMapService _map;
        private readonly ISnowstormClient _snow;

        [ObservableProperty] private string phraseText = string.Empty;
        [ObservableProperty] private string scopeText = string.Empty;
        [ObservableProperty] private string searchText = string.Empty;
        public ObservableCollection<SnomedConcept> Results { get; } = new();
        
        private SnomedConcept? _selectedConcept;
        public SnomedConcept? SelectedConcept
        {
            get => _selectedConcept;
            set
            {
                if (SetProperty(ref _selectedConcept, value))
                {
                    MapCommand.NotifyCanExecuteChanged();
                }
            }
        }
        
        [ObservableProperty] private string mappingType = "exact";
        [ObservableProperty] private string? confidence;
        [ObservableProperty] private string? notes;

        public IRelayCommand SearchCommand { get; }
        public IRelayCommand MapCommand { get; }

        public PhraseSnomedLinkWindowViewModel(long phraseId, string phraseText, long? accountId, ISnomedMapService map, ISnowstormClient snow)
        {
            _phraseId = phraseId; _accountId = accountId; _map = map; _snow = snow;
            PhraseText = phraseText;
            SearchText = phraseText; // Pre-fill search box with phrase text
            ScopeText = accountId == null ? "Global" : $"Account {accountId}";
            SearchCommand = new AsyncRelayCommand(OnSearchAsync);
            MapCommand = new AsyncRelayCommand(OnMapAsync, CanMap);
        }

        private bool CanMap() => SelectedConcept != null;

        private async Task OnSearchAsync()
        {
            Results.Clear();
            var list = await _snow.SearchConceptsAsync(SearchText ?? string.Empty, 50);
            foreach (var c in list) Results.Add(c);
        }

        private async Task OnMapAsync()
        {
            if (SelectedConcept == null) return;
            
            try
            {
                // First, cache the concept if it's not already cached
                await _map.CacheConceptAsync(SelectedConcept);
                
                // Parse confidence
                decimal? conf = null;
                if (!string.IsNullOrWhiteSpace(Confidence) && decimal.TryParse(Confidence, out var d)) conf = d;
                
                // Map phrase to SNOMED concept
                await _map.MapPhraseAsync(_phraseId, _accountId, SelectedConcept.ConceptId, MappingType, conf, Notes);
                
                MessageBox.Show("Mapped successfully.", "SNOMED", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to map phrase:\n{ex.Message}", "SNOMED", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
