using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Wysg.Musm.EditorDataStudio.Services;

namespace Wysg.Musm.EditorDataStudio.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly ITenantLookup _tenants;
        private readonly IPhraseWriter _writer;

        [ObservableProperty] private ObservableCollection<TenantDto> tenants = new();
        [ObservableProperty] private TenantDto? selectedTenant;
        [ObservableProperty] private ObservableCollection<PhraseDto> phrases = new();
        [ObservableProperty] private PhraseDto? selectedPhrase;

        [ObservableProperty] private string newPhraseText = string.Empty;
        [ObservableProperty] private bool newPhraseCaseSensitive = false;
        [ObservableProperty] private string newPhraseLang = "en";

        [ObservableProperty] private string sctSearchText = string.Empty;
        [ObservableProperty] private ObservableCollection<SctConceptDto> sctResults = new();
        [ObservableProperty] private SctConceptDto? selectedSct;

        public IRelayCommand RefreshTenantsCommand { get; }
        public IRelayCommand AddPhraseCommand { get; }
        public IRelayCommand SearchSctCommand { get; }
        public IRelayCommand MapToSctCommand { get; }

        public MainViewModel(ITenantLookup tenants, IPhraseWriter writer)
        {
            _tenants = tenants; _writer = writer;
            RefreshTenantsCommand = new AsyncRelayCommand(LoadTenantsAsync);
            AddPhraseCommand = new AsyncRelayCommand(AddPhraseAsync, CanAddPhrase);
            SearchSctCommand = new AsyncRelayCommand(SearchSctAsync);
            MapToSctCommand = new AsyncRelayCommand(MapToSctAsync, CanMapToSct);
            _ = LoadTenantsAsync();
        }

        private async Task LoadTenantsAsync()
        {
            var list = await _tenants.GetAllAsync();
            Tenants = new ObservableCollection<TenantDto>(list);
            if (SelectedTenant == null && Tenants.Count > 0) SelectedTenant = Tenants[0];
            await LoadPhrasesAsync();
        }

        private async Task LoadPhrasesAsync()
        {
            if (SelectedTenant == null) { Phrases.Clear(); return; }
            var list = await _writer.ListAsync(SelectedTenant.Id);
            Phrases = new ObservableCollection<PhraseDto>(list);
        }

        partial void OnSelectedTenantChanged(TenantDto? value)
        {
            _ = LoadPhrasesAsync();
            AddPhraseCommand.NotifyCanExecuteChanged();
        }

        partial void OnNewPhraseTextChanged(string value)
        {
            AddPhraseCommand.NotifyCanExecuteChanged();
        }

        partial void OnSelectedPhraseChanged(PhraseDto? value)
        {
            MapToSctCommand.NotifyCanExecuteChanged();
        }

        partial void OnSelectedSctChanged(SctConceptDto? value)
        {
            MapToSctCommand.NotifyCanExecuteChanged();
        }

        private bool CanAddPhrase() => SelectedTenant != null && !string.IsNullOrWhiteSpace(NewPhraseText);

        private async Task AddPhraseAsync()
        {
            if (SelectedTenant == null) return;
            // Pass tenant CODE, not name: content.ensure_phrase calls app.ensure_tenant(p_code)
            var id = await _writer.EnsurePhraseAsync(SelectedTenant.Code, NewPhraseText, NewPhraseCaseSensitive, NewPhraseLang, "{}");
            // refresh list
            await LoadPhrasesAsync();
            NewPhraseText = string.Empty;
        }

        private async Task SearchSctAsync()
        {
            if (string.IsNullOrWhiteSpace(SctSearchText)) { SctResults.Clear(); return; }
            var list = await _writer.SearchSctAsync(SctSearchText);
            SctResults = new ObservableCollection<SctConceptDto>(list);
        }

        private bool CanMapToSct() => SelectedPhrase != null && SelectedSct != null;

        private async Task MapToSctAsync()
        {
            if (!CanMapToSct()) return;
            await _writer.MapPhraseToSctAsync(SelectedPhrase!.Id, SelectedSct!.Id);
            // Optionally refresh phrases if mapping affects list data
            await LoadPhrasesAsync();
        }
    }
}
