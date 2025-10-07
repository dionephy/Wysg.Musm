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
            public string Text { get; set; } = string.Empty;
            
            private bool _active;
            public bool Active
            {
                get => _active;
                set
                {
                    // STRICT synchronous requirement (2025-10-07 refinement):
                    // Do NOT apply the optimistic UI change here. We only perform the DB toggle and then
                    // update the backing field from the refreshed snapshot (UpdateFromSnapshotSynchronous).
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

                    // Kick off the strict toggle flow (DB -> snapshot -> UI). Fire & forget is fine because
                    // UI does NOT show the new state until snapshot update method runs.
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

        private async Task<long?> ResolveAccountIdAsync()
        {
            if (_tenant.TenantId > 0) return _tenant.TenantId;
            var any = await _phrases.GetAnyAccountIdAsync();
            if (any.HasValue)
            {
                _tenant.TenantId = any.Value; // adopt for session
                Debug.WriteLine($"[PhrasesVM] Adopted fallback account id={any.Value}");
            }
            return any;
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
            
            // Get the current snapshot state (this will load if needed)
            var meta = await _phrases.GetAllPhraseMetaAsync(accountId).ConfigureAwait(false);
            Debug.WriteLine($"[PhrasesVM] GetAllPhraseMetaAsync returned {meta.Count} phrases");
            
            // Synchronous UI update
            void UpdateUI()
            {
                Items.Clear();
                foreach (var m in meta)
                {
                    var row = new PhraseRow(this) { Id = m.Id, Text = m.Text, UpdatedAt = m.UpdatedAt, Rev = m.Rev };
                    row.InitializeActive(m.Active); // prevents accidental toggle
                    Items.Add(row);
                }
                Debug.WriteLine($"[PhrasesVM] Loaded {Items.Count} items from snapshot to UI");
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
                
                // Synchronous flow: database update ¡æ snapshot update ¡æ UI update from snapshot
                var newPhrase = await _phrases.UpsertPhraseAsync(accountId, text, true);
                
                Debug.WriteLine($"[PhrasesVM] Phrase added successfully: id={newPhrase.Id}");
                NewText = string.Empty;
                
                // Synchronous UI update - Add new phrase to UI displaying exactly what's in the snapshot
                void AddToUI()
                {
                    var row = new PhraseRow(this) 
                    { 
                        Id = newPhrase.Id, 
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
                // On any error, refresh from snapshot to ensure consistency
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
            var accountId = _tenant.AccountId; 
            if (accountId <= 0) return;
            
            Debug.WriteLine($"[PhrasesVM] Synchronous toggle requested for phrase id={row.Id} text='{row.Text}' (no optimistic UI state)");
            
            // Mark as toggling to prevent further UI changes during operation
            row.SetToggling(true);
            
            try
            {
                // STRICT flow: user action already captured ¡æ database update ¡æ snapshot ¡æ UI
                var updated = await _phrases.ToggleActiveAsync(accountId, row.Id).ConfigureAwait(false);
                
                if (updated == null) 
                {
                    Debug.WriteLine($"[PhrasesVM] Toggle failed for id={row.Id}, refreshing from snapshot");
                    // Failed - refresh from snapshot to ensure consistency
                    await RefreshAsync();
                    return;
                }
                
                Debug.WriteLine($"[PhrasesVM] Toggle completed for id={row.Id}, snapshot state={updated.Active}");
                
                void UpdateUI()
                {
                    row.UpdateFromSnapshotSynchronous(updated.Active, updated.UpdatedAt, updated.Rev);
                    Debug.WriteLine($"[PhrasesVM] UI updated strictly from snapshot for id={row.Id}, active={updated.Active}");
                }

                if (Application.Current?.Dispatcher.CheckAccess() == true)
                    UpdateUI();
                else
                    Application.Current?.Dispatcher.Invoke(UpdateUI); // Synchronous invoke
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
            public DelegateCommand(Func<object?, Task> execAsync, Predicate<object?>? can = null) { _execAsync = execAsync; _can = can; }
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
