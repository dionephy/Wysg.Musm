using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Wysg.Musm.EditorDataStudio.Services;

namespace Wysg.Musm.EditorDataStudio.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly ITenantLookup _tenants;
        private readonly IPhraseWriter _writer;
        private readonly ILocalSettings _settings;

        [ObservableProperty] private ObservableCollection<TenantDto> tenants = new();
        [ObservableProperty] private TenantDto? selectedTenant;
        [ObservableProperty] private ObservableCollection<PhraseDto> phrases = new();
        [ObservableProperty] private PhraseDto? selectedPhrase;

        [ObservableProperty] private string newPhraseText = string.Empty;
        [ObservableProperty] private bool newPhraseCaseSensitive = false;
        [ObservableProperty] private string newPhraseLang = "en";

        // Tag list UI
        [ObservableProperty] private string newTagText = string.Empty;
        [ObservableProperty] private ObservableCollection<string> newTags = new();

        // SNOMED mapping inputs for adding/mapping
        [ObservableProperty] private string rootConceptId = string.Empty;
        [ObservableProperty] private string? expressionCg;
        [ObservableProperty] private string? edition;
        [ObservableProperty] private string? moduleId;
        [ObservableProperty] private DateTime? effectiveTime;

        [ObservableProperty] private string sctSearchText = string.Empty;
        [ObservableProperty] private ObservableCollection<SctConceptDto> sctResults = new();
        [ObservableProperty] private SctConceptDto? selectedSct;

        // Display of SNOMED mapping for selected phrase
        [ObservableProperty] private PhraseSctMappingDto? selectedPhraseSct;

        public IRelayCommand RefreshTenantsCommand { get; }
        public IRelayCommand AddPhraseCommand { get; }
        public IRelayCommand SearchSctCommand { get; }
        public IRelayCommand MapToSctCommand { get; }
        public IRelayCommand AddTagCommand { get; }
        public IRelayCommand RemoveSelectedTagCommand { get; }

        [ObservableProperty] private string? selectedTag;

        public MainViewModel(ITenantLookup tenants, IPhraseWriter writer, ILocalSettings settings)
        {
            _tenants = tenants; _writer = writer; _settings = settings;
            RefreshTenantsCommand = new AsyncRelayCommand(LoadTenantsAsync);
            AddPhraseCommand = new AsyncRelayCommand(AddPhraseAsync, CanAddPhrase);
            SearchSctCommand = new AsyncRelayCommand(SearchSctAsync);
            MapToSctCommand = new AsyncRelayCommand(MapToSctAsync, CanMapToSct);
            AddTagCommand = new RelayCommand(AddTag, CanAddTag);
            RemoveSelectedTagCommand = new RelayCommand(RemoveSelectedTag, CanRemoveSelectedTag);

            // Defaults
            Edition = _settings.LastEdition ?? "INT 20250901";
            EffectiveTime = ParseEffectiveFromEdition(Edition) ?? DateTime.Today;

            _ = LoadTenantsAsync();
        }

        private DateTime? ParseEffectiveFromEdition(string? ed)
        {
            if (string.IsNullOrWhiteSpace(ed)) return null;
            // expect pattern like "INT 20250901" -> take last token with digits
            var token = ed.Split(' ', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
            if (token != null && token.Length >= 8 && DateTime.TryParseExact(token.Substring(0, 8), "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out var dt))
                return dt;
            return null;
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

        partial void OnRootConceptIdChanged(string value)
        {
            AddPhraseCommand.NotifyCanExecuteChanged();
        }

        partial void OnEditionChanged(string? value)
        {
            // if edition looks like it has a date, update EffectiveTime accordingly
            var parsed = ParseEffectiveFromEdition(value);
            if (parsed.HasValue)
                EffectiveTime = parsed.Value;
        }

        partial void OnSelectedPhraseChanged(PhraseDto? value)
        {
            MapToSctCommand.NotifyCanExecuteChanged();
            _ = LoadSelectedPhraseSctAsync();
        }

        private async Task LoadSelectedPhraseSctAsync()
        {
            if (SelectedPhrase == null) { SelectedPhraseSct = null; return; }
            SelectedPhraseSct = await _writer.GetPhraseSctAsync(SelectedPhrase.Id);
        }

        partial void OnSelectedSctChanged(SctConceptDto? value)
        {
            if (value != null)
            {
                RootConceptId = value.Id;
            }
            MapToSctCommand.NotifyCanExecuteChanged();
            AddPhraseCommand.NotifyCanExecuteChanged();
        }

        private bool CanAddPhrase()
            => SelectedTenant != null
               && !string.IsNullOrWhiteSpace(NewPhraseText)
               && (!string.IsNullOrWhiteSpace(RootConceptId) || SelectedSct != null);

        private async Task AddPhraseAsync()
        {
            if (SelectedTenant == null) return;

            var conceptId = !string.IsNullOrWhiteSpace(RootConceptId) ? RootConceptId : SelectedSct?.Id;
            if (string.IsNullOrWhiteSpace(conceptId)) return;

            string tagsJson = BuildTagsJson();

            _ = await _writer.EnsurePhraseWithSctAsync(
                tenantId: SelectedTenant.Id,
                text: NewPhraseText,
                caseSensitive: NewPhraseCaseSensitive,
                lang: NewPhraseLang,
                rootConceptId: conceptId!,
                expressionCg: ExpressionCg,
                edition: Edition,
                moduleId: ModuleId,
                effectiveTime: EffectiveTime,
                tagsJson: tagsJson
            );

            // Persist last edition for next startup
            if (!string.IsNullOrWhiteSpace(Edition))
                _settings.LastEdition = Edition;

            await LoadPhrasesAsync();
            NewPhraseText = string.Empty;
            RootConceptId = string.Empty;
            ExpressionCg = null; ModuleId = null; // keep Edition and EffectiveTime as next defaults
            NewTags.Clear(); NewTagText = string.Empty;
        }

        private string BuildTagsJson()
        {
            // represent tags as array of strings: ["tag1","tag2"]
            if (NewTags.Count == 0) return "[]";
            return JsonSerializer.Serialize(NewTags);
        }

        private async Task SearchSctAsync()
        {
            if (string.IsNullOrWhiteSpace(SctSearchText)) { SctResults.Clear(); return; }
            var list = await _writer.SearchSctAsync(SctSearchText);
            SctResults = new ObservableCollection<SctConceptDto>(list);
        }

        private bool CanMapToSct() => SelectedPhrase != null && (SelectedSct != null || !string.IsNullOrWhiteSpace(RootConceptId));

        private async Task MapToSctAsync()
        {
            if (SelectedPhrase == null) return;
            var conceptId = !string.IsNullOrWhiteSpace(RootConceptId) ? RootConceptId : SelectedSct?.Id;
            if (string.IsNullOrWhiteSpace(conceptId)) return;

            await _writer.MapPhraseToSctAsync(
                phraseId: SelectedPhrase.Id,
                rootConceptId: conceptId!,
                expressionCg: ExpressionCg,
                edition: Edition,
                moduleId: ModuleId,
                effectiveTime: EffectiveTime
            );

            await LoadSelectedPhraseSctAsync();
        }

        private bool CanAddTag() => !string.IsNullOrWhiteSpace(NewTagText);
        private void AddTag()
        {
            if (!CanAddTag()) return;
            if (!NewTags.Contains(NewTagText)) NewTags.Add(NewTagText);
            NewTagText = string.Empty;
        }

        private bool CanRemoveSelectedTag() => !string.IsNullOrWhiteSpace(SelectedTag) && NewTags.Contains(SelectedTag);
        private void RemoveSelectedTag()
        {
            if (SelectedTag == null) return;
            _ = NewTags.Remove(SelectedTag);
            SelectedTag = null;
        }
    }
}
