using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Wysg.Musm.Radium.Services;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;

namespace Wysg.Musm.Radium.ViewModels
{
    /// <summary>
    /// ViewModel for managing global phrases (account_id IS NULL).
    /// Global phrases are available to all accounts.
    /// Follows synchronous database flow (FR-258..FR-260, FR-273..FR-278).
    /// </summary>
    public sealed class GlobalPhrasesViewModel : INotifyPropertyChanged
    {
        private readonly IPhraseService _phraseService;
        private readonly IPhraseCache _cache;
        private readonly ITenantContext _tenant;
        private string _newPhraseText = string.Empty;
        private bool _isBusy;
        private string _statusMessage = string.Empty;
        private string _searchAccountText = string.Empty;
        private long? _sourceAccountId;

        public ObservableCollection<GlobalPhraseItem> Items { get; } = new();
        public ObservableCollection<AccountPhraseItem> AccountPhrases { get; } = new();

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
                    ((AsyncRelayCommand)AddPhraseCommand).NotifyCanExecuteChanged();
                    ((AsyncRelayCommand)RefreshCommand).NotifyCanExecuteChanged();
                    ((AsyncRelayCommand)SearchAccountPhrasesCommand).NotifyCanExecuteChanged();
                    ((AsyncRelayCommand)ConvertSelectedCommand).NotifyCanExecuteChanged();
                    (SelectAllAccountPhrasesCommand as RelayCommand)?.NotifyCanExecuteChanged();
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

        public IAsyncRelayCommand AddPhraseCommand { get; }
        public IAsyncRelayCommand RefreshCommand { get; }
        public IAsyncRelayCommand SearchAccountPhrasesCommand { get; }
        public IAsyncRelayCommand ConvertSelectedCommand { get; }
        public IRelayCommand SelectAllAccountPhrasesCommand { get; }

        public GlobalPhrasesViewModel(IPhraseService phraseService, IPhraseCache cache, ITenantContext tenant)
        {
            _phraseService = phraseService;
            _cache = cache;
            _tenant = tenant;

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
                () => !IsBusy // enable always; conversion handler checks selection
            );

            SelectAllAccountPhrasesCommand = new RelayCommand(
                SelectAllAccountPhrases,
                () => !IsBusy && AccountPhrases.Count > 0
            );

            // Load phrases on initialization
            _ = RefreshPhrasesAsync();
            _ = LoadAllNonGlobalAsync(); // Default: show all non-global phrases
        }

        private void SelectAllAccountPhrases()
        {
            int n = 0;
            foreach (var p in AccountPhrases)
            {
                if (!p.IsSelected)
                {
                    p.IsSelected = true;
                    n++;
                }
            }
            StatusMessage = n == 0 ? "All rows already selected" : $"Selected {n} phrase(s)";
        }

        private async Task LoadAllNonGlobalAsync()
        {
            try
            {
                IsBusy = true;
                StatusMessage = "Loading all non-global phrases...";
                AccountPhrases.Clear();
                var phrases = await _phraseService.GetAllNonGlobalPhraseMetaAsync(500);
                foreach (var p in phrases.OrderBy(p => p.Text))
                    AccountPhrases.Add(new AccountPhraseItem(p));
                _sourceAccountId = null; // indicates cross-account listing
                StatusMessage = $"Loaded {AccountPhrases.Count} non-global phrases";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading non-global phrases: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
                ((AsyncRelayCommand)ConvertSelectedCommand).NotifyCanExecuteChanged();
                (SelectAllAccountPhrasesCommand as RelayCommand)?.NotifyCanExecuteChanged();
            }
        }

        private async Task AddPhraseAsync()
        {
            var text = NewPhraseText?.Trim();
            if (string.IsNullOrWhiteSpace(text)) return;

            IsBusy = true;
            StatusMessage = "Adding global phrase...";

            try
            {
                // Add as global phrase (accountId = null)
                var phraseInfo = await _phraseService.UpsertPhraseAsync(
                    accountId: null,  // NULL = global phrase
                    text: text,
                    active: true
                );

                StatusMessage = $"Added: {phraseInfo.Text}";
                NewPhraseText = string.Empty;

                // Clear global cache
                _cache.Clear(-1); // GLOBAL_KEY

                // Refresh list
                await RefreshPhrasesAsync();
                await LoadAllNonGlobalAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error adding phrase: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task RefreshPhrasesAsync()
        {
            IsBusy = true;
            StatusMessage = "Loading global phrases...";

            try
            {
                var phrases = await _phraseService.GetAllGlobalPhraseMetaAsync();

                Items.Clear();
                foreach (var phrase in phrases.OrderByDescending(p => p.UpdatedAt))
                {
                    Items.Add(new GlobalPhraseItem(phrase, this));
                }

                StatusMessage = $"Loaded {Items.Count} global phrases";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading phrases: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task SearchAccountPhrasesAsync()
        {
            IsBusy = true;
            AccountPhrases.Clear();
            _sourceAccountId = null;

            // Try to parse account ID from search text; empty -> load all non-global phrases
            if (string.IsNullOrWhiteSpace(SearchAccountText))
            {
                await LoadAllNonGlobalAsync();
                return;
            }

            long accountId = 0;
            if (!long.TryParse(SearchAccountText.Trim(), out accountId))
            {
                // If not a number, try current tenant account
                accountId = _tenant.AccountId;
            }

            if (accountId <= 0)
            {
                StatusMessage = "Please enter a valid account ID or ensure you are logged in";
                IsBusy = false;
                return;
            }

            StatusMessage = $"Loading phrases for account {accountId}...";

            try
            {
                var phrases = await _phraseService.GetAllPhraseMetaAsync(accountId);
                _sourceAccountId = accountId;

                foreach (var phrase in phrases.OrderBy(p => p.Text))
                {
                    AccountPhrases.Add(new AccountPhraseItem(phrase));
                }

                StatusMessage = $"Loaded {AccountPhrases.Count} phrases from account {accountId}";
                ((AsyncRelayCommand)ConvertSelectedCommand).NotifyCanExecuteChanged();
                (SelectAllAccountPhrasesCommand as RelayCommand)?.NotifyCanExecuteChanged();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading account phrases: {ex.Message}";
                _sourceAccountId = null;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ConvertSelectedToGlobalAsync()
        {
            var selected = AccountPhrases.Where(p => p.IsSelected).ToList();
            if (selected.Count == 0)
            {
                StatusMessage = "No phrases selected";
                return;
            }

            IsBusy = true;
            StatusMessage = $"Converting {selected.Count} phrases to global...";

            try
            {
                int totalConverted = 0;
                int totalRemoved = 0;

                if (_sourceAccountId.HasValue && _sourceAccountId.Value > 0)
                {
                    // Simple path: selected from a single account
                    var (converted, removed) = await _phraseService.ConvertToGlobalPhrasesAsync(
                        _sourceAccountId.Value, selected.Select(s => s.Id)
                    );
                    totalConverted += converted; totalRemoved += removed;
                }
                else
                {
                    // Mixed accounts: group by AccountId and convert per account
                    var byAccount = selected
                        .GroupBy(s => s.AccountId)
                        .Where(g => g.Key.HasValue && g.Key.Value > 0);
                    foreach (var g in byAccount)
                    {
                        var (c, r) = await _phraseService.ConvertToGlobalPhrasesAsync(g.Key!.Value, g.Select(s => s.Id));
                        totalConverted += c; totalRemoved += r;
                    }
                }

                StatusMessage = $"Converted {totalConverted} phrases to global, removed {totalRemoved} duplicates";

                // Refresh lists
                await RefreshPhrasesAsync();
                await SearchAccountPhrasesAsync(); // reloads all non-global or account view
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error converting phrases: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task ToggleActiveAsync(GlobalPhraseItem item)
        {
            if (IsBusy) return;

            IsBusy = true;
            StatusMessage = $"Toggling phrase: {item.Text}...";

            try
            {
                var updated = await _phraseService.ToggleActiveAsync(
                    accountId: null,  // NULL = global phrase
                    phraseId: item.Id
                );

                if (updated != null)
                {
                    item.UpdateFrom(updated);
                    StatusMessage = $"Updated: {item.Text} (Active: {item.Active})";

                    // Clear global cache
                    _cache.Clear(-1); // GLOBAL_KEY
                }
                else
                {
                    StatusMessage = $"Failed to toggle phrase: {item.Text}";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error toggling phrase: {ex.Message}";
                // Refresh to recover consistency
                await RefreshPhrasesAsync();
            }
            finally
            {
                IsBusy = false;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Represents a single global phrase in the UI.
    /// </summary>
    public sealed class GlobalPhraseItem : INotifyPropertyChanged
    {
        private readonly GlobalPhrasesViewModel _parent;
        private bool _active;
        private DateTime _updatedAt;
        private long _rev;

        public long Id { get; }
        public string Text { get; }

        public bool Active
        {
            get => _active;
            private set
            {
                if (_active != value)
                {
                    _active = value;
                    OnPropertyChanged();
                }
            }
        }

        public DateTime UpdatedAt
        {
            get => _updatedAt;
            private set
            {
                if (_updatedAt != value)
                {
                    _updatedAt = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(UpdatedAtDisplay));
                }
            }
        }

        public long Rev
        {
            get => _rev;
            private set
            {
                if (_rev != value)
                {
                    _rev = value;
                    OnPropertyChanged();
                }
            }
        }

        public string UpdatedAtDisplay => UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss");

        public IAsyncRelayCommand ToggleCommand { get; }

        public GlobalPhraseItem(PhraseInfo info, GlobalPhrasesViewModel parent)
        {
            _parent = parent;
            Id = info.Id;
            Text = info.Text;
            _active = info.Active;
            _updatedAt = info.UpdatedAt;
            _rev = info.Rev;

            ToggleCommand = new AsyncRelayCommand(
                async () => await _parent.ToggleActiveAsync(this),
                () => !_parent.IsBusy
            );
        }

        public void UpdateFrom(PhraseInfo info)
        {
            Active = info.Active;
            UpdatedAt = info.UpdatedAt;
            Rev = info.Rev;
            ToggleCommand.NotifyCanExecuteChanged();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Represents an account-specific phrase that can be converted to global.
    /// </summary>
    public sealed class AccountPhraseItem : INotifyPropertyChanged
    {
        private bool _isSelected;

        public long Id { get; }
        public long? AccountId { get; }
        public string Text { get; }
        public bool Active { get; }

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

        public AccountPhraseItem(PhraseInfo info)
        {
            Id = info.Id;
            AccountId = info.AccountId;
            Text = info.Text;
            Active = info.Active;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
