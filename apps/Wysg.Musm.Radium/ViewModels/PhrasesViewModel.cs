using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Wysg.Musm.Infrastructure.ViewModels;
using Wysg.Musm.Radium.Services;
using System.Windows;
using System.Diagnostics;
using System.Windows.Threading;

namespace Wysg.Musm.Radium.ViewModels
{
    public sealed class PhrasesViewModel : BaseViewModel
    {
        private readonly IPhraseService _phrases;
        private readonly ITenantContext _tenant;

        public ObservableCollection<PhraseRow> Items { get; } = new();

        public PhrasesViewModel(IPhraseService phrases, ITenantContext tenant)
        {
            _phrases = phrases; _tenant = tenant;
            RefreshCommand = new DelegateCommand(async _ => await RefreshAsync());
            AddCommand = new DelegateCommand(async _ => await AddAsync(), _ => !string.IsNullOrWhiteSpace(NewText));
            _tenant.AccountIdChanged += OnAccountIdChanged;
            _ = RefreshAsync();
        }

        private void OnAccountIdChanged(long oldId, long newId)
        {
            if (newId <= 0)
            {
                // logout -> clear (synchronous)
                if (Application.Current?.Dispatcher.CheckAccess() == true)
                    Items.Clear();
                else
                    Application.Current?.Dispatcher.Invoke(() => Items.Clear());
            }
            else if (oldId <= 0 && newId > 0)
            {
                // first valid login -> reload
                _ = RefreshAsync();
            }
        }

        private string _newText = string.Empty;
        public string NewText { get => _newText; set { if (SetProperty(ref _newText, value)) (AddCommand as DelegateCommand)?.RaiseCanExecuteChanged(); } }

        public ICommand RefreshCommand { get; }
        public ICommand AddCommand { get; }

        public sealed class PhraseRow : BaseViewModel
        {
            private readonly PhrasesViewModel _owner;
            private bool _suppressNotify; // prevents toggle during initial load / programmatic sync
            private volatile bool _isToggling; // Make volatile for thread safety
            
            public PhraseRow(PhrasesViewModel owner) { _owner = owner; }
            public long Id { get; init; }
            public long? AccountId { get; init; } // null => global phrase
            public bool IsGlobal => AccountId == null;
            public string Text { get; set; } = string.Empty;
            
            private bool _active;
            public bool Active
            {
                get => _active;
                set
                {
                    // STRICT synchronous requirement (2025-10-07 refinement):
                    if (_suppressNotify || _isToggling)
                    {
                        Debug.WriteLine($"[PhraseRow] Active change blocked for id={Id} - suppressNotify={_suppressNotify}, isToggling={_isToggling}");
                        return; // skip during initialization or ongoing toggle
                    }
                    if (_active == value)
                        return; // no change

                    Debug.WriteLine($"[PhraseRow] Active user request for id={Id}, requestedValue={value}, current={_active} -> initiating strict synchronous toggle");

                    // Revert any visual optimistic state by raising property changed with the current value
                    // (WPF will set the source first, then we force target back until snapshot applied)
                    _suppressNotify = true; // prevent re-entry
                    OnPropertyChanged(nameof(Active)); // forces UI to reflect current backing field state
                    _suppressNotify = false;

                    _ = _owner.OnActiveChangedAsync(this);
                }
            }
            
            public DateTime UpdatedAt { get; set; }
            public long Rev { get; set; }
            
            public void InitializeActive(bool value)
            {
                _suppressNotify = true;
                _active = value; // set backing directly to avoid SetProperty re-entry logic
                OnPropertyChanged(nameof(Active));
                _suppressNotify = false;
            }
            
            public void SetToggling(bool isToggling)
            {
                Debug.WriteLine($"[PhraseRow] SetToggling({isToggling}) for id={Id}");
                _isToggling = isToggling;
            }
            
            public void UpdateFromSnapshotSynchronous(bool active, DateTime updatedAt, long rev)
            {
                Debug.WriteLine($"[PhraseRow] UpdateFromSnapshotSynchronous for id={Id}: active={active}, rev={rev}");
                _suppressNotify = true;
                _active = active;
                UpdatedAt = updatedAt;
                Rev = rev;
                OnPropertyChanged(nameof(Active));
                OnPropertyChanged(nameof(UpdatedAt));
                OnPropertyChanged(nameof(Rev));
                _suppressNotify = false;
            }
        }

        private async Task RefreshAsync()
        {
            Debug.WriteLine($"[PhrasesVM] RefreshAsync called - AccountId={_tenant.AccountId}");
            var accountId = _tenant.AccountId; 
            if (accountId <= 0) 
            { 
                Debug.WriteLine($"[PhrasesVM] AccountId is {accountId}, clearing items");
                if (Application.Current?.Dispatcher.CheckAccess() == true)
                    Items.Clear();
                else
                    Application.Current?.Dispatcher.Invoke(() => Items.Clear());
                return; 
            }
            
            Debug.WriteLine($"[PhrasesVM] Refreshing phrases from snapshot for account={accountId}");
            
            // Load account-specific and global metadata
            var loadAccountTask = _phrases.GetAllPhraseMetaAsync(accountId);
            var loadGlobalTask = _phrases.GetAllGlobalPhraseMetaAsync();
            await Task.WhenAll(loadAccountTask, loadGlobalTask).ConfigureAwait(false);
            var accountMeta = loadAccountTask.Result;
            var globalMeta = loadGlobalTask.Result;

            // Synchronous UI update
            void UpdateUI()
            {
                Items.Clear();
                // Account phrases first (stable previous behavior)
                foreach (var m in accountMeta)
                {
                    var rowAcc = new PhraseRow(this) { Id = m.Id, AccountId = m.AccountId, Text = m.Text, UpdatedAt = m.UpdatedAt, Rev = m.Rev };
                    rowAcc.InitializeActive(m.Active);
                    Items.Add(rowAcc);
                }
                // Then global phrases
                foreach (var g in globalMeta)
                {
                    var rowG = new PhraseRow(this) { Id = g.Id, AccountId = g.AccountId, Text = g.Text, UpdatedAt = g.UpdatedAt, Rev = g.Rev };
                    rowG.InitializeActive(g.Active);
                    Items.Add(rowG);
                }
                Debug.WriteLine($"[PhrasesVM] Loaded {Items.Count} items (account + global) to UI");
            }

            if (Application.Current?.Dispatcher.CheckAccess() == true)
                UpdateUI();
            else
                Application.Current?.Dispatcher.Invoke(UpdateUI);
        }

        private async Task AddAsync()
        {
            var accountId = _tenant.AccountId; if (accountId <= 0) return;
            var text = NewText.Trim();
            if (text.Length == 0) return;
            
            try
            {
                Debug.WriteLine($"[PhrasesVM] Adding phrase: '{text}'");
                IsBusy = true;
                
                // Synchronous flow: database update -> snapshot update -> UI update from snapshot
                var newPhrase = await _phrases.UpsertPhraseAsync(accountId, text, true);
                
                Debug.WriteLine($"[PhrasesVM] Phrase added successfully: id={newPhrase.Id}");
                NewText = string.Empty;
                
                void AddToUI()
                {
                    var row = new PhraseRow(this) 
                    { 
                        Id = newPhrase.Id,
                        AccountId = newPhrase.AccountId,
                        Text = newPhrase.Text, 
                        UpdatedAt = newPhrase.UpdatedAt, 
                        Rev = newPhrase.Rev 
                    };
                    row.InitializeActive(newPhrase.Active);
                    Items.Add(row);
                    Debug.WriteLine($"[PhrasesVM] Added new phrase id={newPhrase.Id} to UI from snapshot");
                }

                if (Application.Current?.Dispatcher.CheckAccess() == true)
                    AddToUI();
                else
                    Application.Current?.Dispatcher.Invoke(AddToUI);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PhrasesVM] Add failed: {ex.Message}");
                await RefreshAsync();
            }
            finally
            {
                IsBusy = false;
            }
        }

        internal async Task OnActiveChangedAsync(PhraseRow row)
        {
            if (row.Id == 0) return; 
            long? accountIdForToggle = row.AccountId; // null => global phrase
            if (!accountIdForToggle.HasValue)
            {
                Debug.WriteLine($"[PhrasesVM] Toggle target is GLOBAL phrase id={row.Id}");
            }
            else if (accountIdForToggle.Value <= 0)
            {
                Debug.WriteLine($"[PhrasesVM] Invalid account id for toggle: {accountIdForToggle}");
                return;
            }
            
            Debug.WriteLine($"[PhrasesVM] Synchronous toggle requested for phrase id={row.Id} text='{row.Text}' (scope={(accountIdForToggle.HasValue ? "account" : "global")})");
            
            row.SetToggling(true);
            
            try
            {
                var updated = await _phrases.ToggleActiveAsync(accountIdForToggle, row.Id).ConfigureAwait(false);
                if (updated == null) 
                {
                    Debug.WriteLine($"[PhrasesVM] Toggle failed for id={row.Id}, refreshing from snapshot");
                    await RefreshAsync();
                    return;
                }
                
                void UpdateUI()
                {
                    row.UpdateFromSnapshotSynchronous(updated.Active, updated.UpdatedAt, updated.Rev);
                    Debug.WriteLine($"[PhrasesVM] UI updated strictly from snapshot for id={row.Id}, active={updated.Active}");
                }

                if (Application.Current?.Dispatcher.CheckAccess() == true)
                    UpdateUI();
                else
                    Application.Current?.Dispatcher.Invoke(UpdateUI);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PhrasesVM] Toggle exception for id={row.Id}: {ex.Message}");
                await RefreshAsync();
            }
            finally
            {
                row.SetToggling(false);
                Debug.WriteLine($"[PhrasesVM] Toggle lock released for id={row.Id}");
            }
        }

        private sealed class DelegateCommand : ICommand
        {
            private readonly Func<object?, Task> _execAsync;
            private readonly Predicate<object?>? _can;
            public DelegateCommand(Func<object?, Task> execAsync, Predicate<object?>? _can = null) { _execAsync = execAsync; this._can = _can; }
            public bool CanExecute(object? parameter) => _can?.Invoke(parameter) ?? true;
            public async void Execute(object? parameter) => await _execAsync(parameter);
            public event EventHandler? CanExecuteChanged;
            public void RaiseCanExecuteChanged()
            {
                var disp = Application.Current?.Dispatcher;
                if (disp == null || disp.CheckAccess())
                    CanExecuteChanged?.Invoke(this, EventArgs.Empty);
                else
                    disp.Invoke(new Action(() => CanExecuteChanged?.Invoke(this, EventArgs.Empty))); // Synchronous invoke
            }
        }
    }
}
