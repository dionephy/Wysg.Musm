using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Wysg.Musm.Infrastructure.ViewModels;
using Wysg.Musm.Radium.Services;

namespace Wysg.Musm.Radium.ViewModels
{
    /// <summary>
    /// ViewModel for managing account-scoped hotkeys.
    /// Follows synchronous snapshot pattern: DB -> snapshot -> UI.
    /// </summary>
    public sealed class HotkeysViewModel : BaseViewModel
    {
        private readonly IHotkeyService _hotkeys;
        private readonly ITenantContext _tenant;

        public ObservableCollection<HotkeyRow> Items { get; } = new();

        public HotkeysViewModel(IHotkeyService hotkeys, ITenantContext tenant)
        {
            _hotkeys = hotkeys ?? throw new ArgumentNullException(nameof(hotkeys));
            _tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
            
            RefreshCommand = new DelegateCommand(async _ => await RefreshAsync());
            AddCommand = new DelegateCommand(async _ => await AddAsync(), _ => CanAdd());
            DeleteCommand = new DelegateCommand(async _ => await DeleteAsync(), _ => SelectedItem != null);
            
            _tenant.AccountIdChanged += OnAccountIdChanged;
            _ = RefreshAsync();
        }

        private void OnAccountIdChanged(long oldId, long newId)
        {
            if (newId <= 0)
            {
                // logout -> clear
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

        private string _triggerText = string.Empty;
        public string TriggerText
        {
            get => _triggerText;
            set
            {
                if (SetProperty(ref _triggerText, value))
                    (AddCommand as DelegateCommand)?.RaiseCanExecuteChanged();
            }
        }

        private string _expansionText = string.Empty;
        public string ExpansionText
        {
            get => _expansionText;
            set
            {
                if (SetProperty(ref _expansionText, value))
                    (AddCommand as DelegateCommand)?.RaiseCanExecuteChanged();
            }
        }

        private HotkeyRow? _selectedItem;
        public HotkeyRow? SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (SetProperty(ref _selectedItem, value))
                    (DeleteCommand as DelegateCommand)?.RaiseCanExecuteChanged();
            }
        }

        public ICommand RefreshCommand { get; }
        public ICommand AddCommand { get; }
        public ICommand DeleteCommand { get; }

        private bool CanAdd() =>
            !string.IsNullOrWhiteSpace(TriggerText) &&
            !string.IsNullOrWhiteSpace(ExpansionText);

        public sealed class HotkeyRow : BaseViewModel
        {
            private readonly HotkeysViewModel _owner;
            private bool _suppressNotify;
            private volatile bool _isToggling;

            public HotkeyRow(HotkeysViewModel owner) { _owner = owner; }
            
            public long HotkeyId { get; init; }
            public long AccountId { get; init; }
            public string TriggerText { get; set; } = string.Empty;
            public string ExpansionText { get; set; } = string.Empty;

            private bool _isActive;
            public bool IsActive
            {
                get => _isActive;
                set
                {
                    if (_suppressNotify || _isToggling)
                    {
                        Debug.WriteLine($"[HotkeyRow] IsActive change blocked for id={HotkeyId} - suppressNotify={_suppressNotify}, isToggling={_isToggling}");
                        return;
                    }
                    if (_isActive == value) return;

                    Debug.WriteLine($"[HotkeyRow] IsActive user request for id={HotkeyId}, requestedValue={value}, current={_isActive} -> initiating toggle");

                    // Revert visual state until snapshot applied
                    _suppressNotify = true;
                    OnPropertyChanged(nameof(IsActive));
                    _suppressNotify = false;

                    _ = _owner.OnActiveChangedAsync(this);
                }
            }

            public DateTime UpdatedAt { get; set; }
            public long Rev { get; set; }

            public void InitializeActive(bool value)
            {
                _suppressNotify = true;
                _isActive = value;
                OnPropertyChanged(nameof(IsActive));
                _suppressNotify = false;
            }

            public void SetToggling(bool isToggling)
            {
                Debug.WriteLine($"[HotkeyRow] SetToggling({isToggling}) for id={HotkeyId}");
                _isToggling = isToggling;
            }

            public void UpdateFromSnapshotSynchronous(bool isActive, DateTime updatedAt, long rev)
            {
                Debug.WriteLine($"[HotkeyRow] UpdateFromSnapshotSynchronous for id={HotkeyId}: active={isActive}, rev={rev}");
                _suppressNotify = true;
                _isActive = isActive;
                UpdatedAt = updatedAt;
                Rev = rev;
                OnPropertyChanged(nameof(IsActive));
                OnPropertyChanged(nameof(UpdatedAt));
                OnPropertyChanged(nameof(Rev));
                _suppressNotify = false;
            }
        }

        private async Task RefreshAsync()
        {
            Debug.WriteLine($"[HotkeysVM] RefreshAsync called - AccountId={_tenant.AccountId}");
            var accountId = _tenant.AccountId;
            if (accountId <= 0)
            {
                Debug.WriteLine($"[HotkeysVM] AccountId is {accountId}, clearing items");
                if (Application.Current?.Dispatcher.CheckAccess() == true)
                    Items.Clear();
                else
                    Application.Current?.Dispatcher.Invoke(() => Items.Clear());
                return;
            }

            Debug.WriteLine($"[HotkeysVM] Refreshing hotkeys from snapshot for account={accountId}");

            try
            {
                var meta = await _hotkeys.GetAllHotkeyMetaAsync(accountId).ConfigureAwait(false);

                void UpdateUI()
                {
                    Items.Clear();
                    foreach (var m in meta)
                    {
                        var row = new HotkeyRow(this)
                        {
                            HotkeyId = m.HotkeyId,
                            AccountId = m.AccountId,
                            TriggerText = m.TriggerText,
                            ExpansionText = m.ExpansionText,
                            UpdatedAt = m.UpdatedAt,
                            Rev = m.Rev
                        };
                        row.InitializeActive(m.IsActive);
                        Items.Add(row);
                    }
                    Debug.WriteLine($"[HotkeysVM] Loaded {Items.Count} hotkeys to UI");
                }

                if (Application.Current?.Dispatcher.CheckAccess() == true)
                    UpdateUI();
                else
                    Application.Current?.Dispatcher.Invoke(UpdateUI);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[HotkeysVM] Refresh failed: {ex.Message}");
            }
        }

        private async Task AddAsync()
        {
            var accountId = _tenant.AccountId;
            if (accountId <= 0) return;
            
            var trigger = TriggerText.Trim();
            var expansion = ExpansionText.Trim();
            
            if (string.IsNullOrEmpty(trigger) || string.IsNullOrEmpty(expansion)) return;

            try
            {
                Debug.WriteLine($"[HotkeysVM] Adding hotkey: trigger='{trigger}'");
                IsBusy = true;

                // Synchronous flow: DB -> snapshot -> UI
                var newHotkey = await _hotkeys.UpsertHotkeyAsync(accountId, trigger, expansion, true);

                Debug.WriteLine($"[HotkeysVM] Hotkey added successfully: id={newHotkey.HotkeyId}");
                
                TriggerText = string.Empty;
                ExpansionText = string.Empty;

                void AddToUI()
                {
                    // Check if already exists (upsert case)
                    var existing = Items.FirstOrDefault(r => r.HotkeyId == newHotkey.HotkeyId);
                    if (existing != null)
                    {
                        existing.TriggerText = newHotkey.TriggerText;
                        existing.ExpansionText = newHotkey.ExpansionText;
                        existing.UpdateFromSnapshotSynchronous(newHotkey.IsActive, newHotkey.UpdatedAt, newHotkey.Rev);
                    }
                    else
                    {
                        var row = new HotkeyRow(this)
                        {
                            HotkeyId = newHotkey.HotkeyId,
                            AccountId = newHotkey.AccountId,
                            TriggerText = newHotkey.TriggerText,
                            ExpansionText = newHotkey.ExpansionText,
                            UpdatedAt = newHotkey.UpdatedAt,
                            Rev = newHotkey.Rev
                        };
                        row.InitializeActive(newHotkey.IsActive);
                        Items.Insert(0, row); // Add at top
                    }
                    Debug.WriteLine($"[HotkeysVM] Added/Updated hotkey id={newHotkey.HotkeyId} in UI from snapshot");
                }

                if (Application.Current?.Dispatcher.CheckAccess() == true)
                    AddToUI();
                else
                    Application.Current?.Dispatcher.Invoke(AddToUI);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[HotkeysVM] Add failed: {ex.Message}");
                await RefreshAsync();
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task DeleteAsync()
        {
            var item = SelectedItem;
            if (item == null) return;

            var accountId = _tenant.AccountId;
            if (accountId <= 0) return;

            try
            {
                Debug.WriteLine($"[HotkeysVM] Deleting hotkey id={item.HotkeyId}");
                IsBusy = true;

                var deleted = await _hotkeys.DeleteHotkeyAsync(accountId, item.HotkeyId);

                if (deleted)
                {
                    Debug.WriteLine($"[HotkeysVM] Hotkey deleted successfully: id={item.HotkeyId}");

                    void RemoveFromUI()
                    {
                        Items.Remove(item);
                        SelectedItem = null;
                        Debug.WriteLine($"[HotkeysVM] Removed hotkey id={item.HotkeyId} from UI");
                    }

                    if (Application.Current?.Dispatcher.CheckAccess() == true)
                        RemoveFromUI();
                    else
                        Application.Current?.Dispatcher.Invoke(RemoveFromUI);
                }
                else
                {
                    Debug.WriteLine($"[HotkeysVM] Delete failed for id={item.HotkeyId}");
                    await RefreshAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[HotkeysVM] Delete exception: {ex.Message}");
                await RefreshAsync();
            }
            finally
            {
                IsBusy = false;
            }
        }

        internal async Task OnActiveChangedAsync(HotkeyRow row)
        {
            if (row.HotkeyId == 0) return;
            if (row.AccountId <= 0) return;

            Debug.WriteLine($"[HotkeysVM] Synchronous toggle requested for hotkey id={row.HotkeyId} trigger='{row.TriggerText}'");

            row.SetToggling(true);

            try
            {
                var updated = await _hotkeys.ToggleActiveAsync(row.AccountId, row.HotkeyId).ConfigureAwait(false);
                if (updated == null)
                {
                    Debug.WriteLine($"[HotkeysVM] Toggle failed for id={row.HotkeyId}, refreshing from snapshot");
                    await RefreshAsync();
                    return;
                }

                void UpdateUI()
                {
                    row.UpdateFromSnapshotSynchronous(updated.IsActive, updated.UpdatedAt, updated.Rev);
                    Debug.WriteLine($"[HotkeysVM] UI updated from snapshot for id={row.HotkeyId}, active={updated.IsActive}");
                }

                if (Application.Current?.Dispatcher.CheckAccess() == true)
                    UpdateUI();
                else
                    Application.Current?.Dispatcher.Invoke(UpdateUI);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[HotkeysVM] Toggle exception for id={row.HotkeyId}: {ex.Message}");
                await RefreshAsync();
            }
            finally
            {
                row.SetToggling(false);
                Debug.WriteLine($"[HotkeysVM] Toggle lock released for id={row.HotkeyId}");
            }
        }

        private sealed class DelegateCommand : ICommand
        {
            private readonly Func<object?, Task> _execAsync;
            private readonly Predicate<object?>? _can;
            
            public DelegateCommand(Func<object?, Task> execAsync, Predicate<object?>? can = null)
            {
                _execAsync = execAsync;
                _can = can;
            }

            public bool CanExecute(object? parameter) => _can?.Invoke(parameter) ?? true;
            public async void Execute(object? parameter) => await _execAsync(parameter);
            public event EventHandler? CanExecuteChanged;
            
            public void RaiseCanExecuteChanged()
            {
                var disp = Application.Current?.Dispatcher;
                if (disp == null || disp.CheckAccess())
                    CanExecuteChanged?.Invoke(this, EventArgs.Empty);
                else
                    disp.Invoke(new Action(() => CanExecuteChanged?.Invoke(this, EventArgs.Empty)));
            }
        }
    }
}
