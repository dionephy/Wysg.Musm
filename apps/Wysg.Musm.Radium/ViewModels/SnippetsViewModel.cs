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
    /// ViewModel for managing account-scoped snippets (template + AST).
    /// Synchronous snapshot pattern: DB -> snapshot -> UI.
    /// </summary>
    public sealed class SnippetsViewModel : BaseViewModel
    {
        private readonly ISnippetService _snippets;
        private readonly ITenantContext _tenant;

        public ObservableCollection<SnippetRow> Items { get; } = new();

        public SnippetsViewModel(ISnippetService snippets, ITenantContext tenant)
        {
            _snippets = snippets ?? throw new ArgumentNullException(nameof(snippets));
            _tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));

            RefreshCommand = new DelegateCommand(async _ => await RefreshAsync());
            AddCommand = new DelegateCommand(async _ => await AddOrUpdateAsync(), _ => CanAdd());
            EditCommand = new DelegateCommand(_ => { StartEdit(); return Task.CompletedTask; }, _ => SelectedItem != null && !IsEditMode);
            CancelEditCommand = new DelegateCommand(_ => { CancelEdit(); return Task.CompletedTask; }, _ => IsEditMode);
            DeleteCommand = new DelegateCommand(async _ => await DeleteAsync(), _ => SelectedItem != null && !IsEditMode);

            _tenant.AccountIdChanged += OnAccountIdChanged;
            _ = RefreshAsync();
        }

        private void OnAccountIdChanged(long oldId, long newId)
        {
            if (newId <= 0)
            {
                if (Application.Current?.Dispatcher.CheckAccess() == true) Items.Clear();
                else Application.Current?.Dispatcher.Invoke(() => Items.Clear());
            }
            else if (oldId <= 0 && newId > 0)
            {
                _ = RefreshAsync();
            }
        }

        private string _triggerText = string.Empty;
        public string TriggerText { get => _triggerText; set { if (SetProperty(ref _triggerText, value)) (AddCommand as DelegateCommand)?.RaiseCanExecuteChanged(); } }

        private string _snippetText = string.Empty;
        public string SnippetText { get => _snippetText; set { if (SetProperty(ref _snippetText, value)) (AddCommand as DelegateCommand)?.RaiseCanExecuteChanged(); } }

        private string _snippetAstText = string.Empty;
        public string SnippetAstText { get => _snippetAstText; set { if (SetProperty(ref _snippetAstText, value)) (AddCommand as DelegateCommand)?.RaiseCanExecuteChanged(); } }

        private string _descriptionText = string.Empty;
        public string DescriptionText { get => _descriptionText; set { if (SetProperty(ref _descriptionText, value)) (AddCommand as DelegateCommand)?.RaiseCanExecuteChanged(); } }

        private SnippetRow? _selectedItem;
        public SnippetRow? SelectedItem 
        { 
            get => _selectedItem; 
            set 
            { 
                if (SetProperty(ref _selectedItem, value))
                {
                    (DeleteCommand as DelegateCommand)?.RaiseCanExecuteChanged();
                    (EditCommand as DelegateCommand)?.RaiseCanExecuteChanged();
                }
            } 
        }

        private bool _isEditMode;
        public bool IsEditMode
        {
            get => _isEditMode;
            set
            {
                if (SetProperty(ref _isEditMode, value))
                {
                    (AddCommand as DelegateCommand)?.RaiseCanExecuteChanged();
                    (EditCommand as DelegateCommand)?.RaiseCanExecuteChanged();
                    (CancelEditCommand as DelegateCommand)?.RaiseCanExecuteChanged();
                    (DeleteCommand as DelegateCommand)?.RaiseCanExecuteChanged();
                    OnPropertyChanged(nameof(AddButtonText));
                }
            }
        }

        private SnippetRow? _editingItem;

        public string AddButtonText => IsEditMode ? "Update" : "Add";

        public ICommand RefreshCommand { get; }
        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand CancelEditCommand { get; }
        public ICommand DeleteCommand { get; }

        private bool CanAdd() => !string.IsNullOrWhiteSpace(TriggerText) && !string.IsNullOrWhiteSpace(SnippetText);

        private void StartEdit()
        {
            var item = SelectedItem;
            if (item == null) return;

            _editingItem = item;
            TriggerText = item.TriggerText;
            SnippetText = item.SnippetText;
            SnippetAstText = item.SnippetAst;
            DescriptionText = item.Description;
            IsEditMode = true;

            Debug.WriteLine($"[SnippetsVM] Started editing snippet id={item.SnippetId}, trigger='{item.TriggerText}'");
        }

        private void CancelEdit()
        {
            _editingItem = null;
            TriggerText = string.Empty;
            SnippetText = string.Empty;
            SnippetAstText = string.Empty;
            DescriptionText = string.Empty;
            IsEditMode = false;

            Debug.WriteLine($"[SnippetsVM] Cancelled editing");
        }

        public sealed class SnippetRow : BaseViewModel
        {
            private readonly SnippetsViewModel _owner;
            private bool _suppressNotify;
            private volatile bool _isToggling;

            public SnippetRow(SnippetsViewModel owner) { _owner = owner; }

            public long SnippetId { get; init; }
            public long AccountId { get; init; }
            public string TriggerText { get; set; } = string.Empty;
            public string SnippetText { get; set; } = string.Empty;
            public string SnippetAst { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;

            private bool _isActive;
            public bool IsActive
            {
                get => _isActive;
                set
                {
                    if (_suppressNotify || _isToggling) return;
                    if (_isActive == value) return;
                    _suppressNotify = true; OnPropertyChanged(nameof(IsActive)); _suppressNotify = false;
                    _ = _owner.OnActiveChangedAsync(this);
                }
            }

            public DateTime UpdatedAt { get; set; }
            public long Rev { get; set; }

            public void InitializeActive(bool v) { _suppressNotify = true; _isActive = v; OnPropertyChanged(nameof(IsActive)); _suppressNotify = false; }
            public void SetToggling(bool v) { _isToggling = v; }
            public void UpdateFromSnapshotSynchronous(bool isActive, DateTime updatedAt, long rev)
            {
                _suppressNotify = true; _isActive = isActive; UpdatedAt = updatedAt; Rev = rev;
                OnPropertyChanged(nameof(IsActive)); OnPropertyChanged(nameof(UpdatedAt)); OnPropertyChanged(nameof(Rev)); _suppressNotify = false;
            }
        }

        private async Task RefreshAsync()
        {
            var aid = _tenant.AccountId; if (aid <= 0) { if (Application.Current?.Dispatcher.CheckAccess() == true) Items.Clear(); else Application.Current?.Dispatcher.Invoke(() => Items.Clear()); return; }
            try
            {
                var meta = await _snippets.GetAllSnippetMetaAsync(aid).ConfigureAwait(false);
                void UpdateUI()
                {
                    Items.Clear();
                    foreach (var m in meta)
                    {
                        var row = new SnippetRow(this)
                        {
                            SnippetId = m.SnippetId,
                            AccountId = m.AccountId,
                            TriggerText = m.TriggerText,
                            SnippetText = m.SnippetText,
                            SnippetAst = m.SnippetAst,
                            Description = m.Description,
                            UpdatedAt = m.UpdatedAt,
                            Rev = m.Rev
                        };
                        row.InitializeActive(m.IsActive);
                        Items.Add(row);
                    }
                }
                if (Application.Current?.Dispatcher.CheckAccess() == true) UpdateUI(); else Application.Current?.Dispatcher.Invoke(UpdateUI);
            }
            catch (Exception ex) { Debug.WriteLine($"[SnippetsVM] Refresh failed: {ex.Message}"); }
        }

        private async Task AddOrUpdateAsync()
        {
            var aid = _tenant.AccountId; if (aid <= 0) return;
            var trigger = TriggerText.Trim(); var text = SnippetText.Trim(); var desc = string.IsNullOrWhiteSpace(DescriptionText) ? null : DescriptionText.Trim();
            if (string.IsNullOrEmpty(trigger) || string.IsNullOrEmpty(text)) return;

            try
            {
                IsBusy = true;
                // Auto-build AST from snippet text if not provided (always build to keep consistent)
                var builtAst = SnippetAstBuilder.BuildJson(text);
                SnippetAstText = builtAst; // reflect in UI for preview

                Debug.WriteLine($"[SnippetsVM] {(IsEditMode ? "Updating" : "Adding")} snippet: trigger='{trigger}'");

                var info = await _snippets.UpsertSnippetAsync(aid, trigger, text, builtAst, true, desc);

                Debug.WriteLine($"[SnippetsVM] Snippet {(IsEditMode ? "updated" : "added")} successfully: id={info.SnippetId}");

                TriggerText = string.Empty; SnippetText = string.Empty; SnippetAstText = string.Empty; DescriptionText = string.Empty;
                IsEditMode = false;
                _editingItem = null;

                // Refresh the entire list to ensure proper ordering by UpdatedAt
                await RefreshAsync();
            }
            catch (Exception ex) { Debug.WriteLine($"[SnippetsVM] Add/Update failed: {ex.Message}"); await RefreshAsync(); }
            finally { IsBusy = false; }
        }

        private async Task DeleteAsync()
        {
            var item = SelectedItem; if (item == null) return; var aid = _tenant.AccountId; if (aid <= 0) return;
            try
            {
                IsBusy = true; var ok = await _snippets.DeleteSnippetAsync(aid, item.SnippetId);
                if (ok)
                {
                    void RemoveUI() { Items.Remove(item); SelectedItem = null; }
                    if (Application.Current?.Dispatcher.CheckAccess() == true) RemoveUI(); else Application.Current?.Dispatcher.Invoke(RemoveUI);
                }
                else { await RefreshAsync(); }
            }
            catch (Exception ex) { Debug.WriteLine($"[SnippetsVM] Delete exception: {ex.Message}"); await RefreshAsync(); }
            finally { IsBusy = false; }
        }

        internal async Task OnActiveChangedAsync(SnippetRow row)
        {
            if (row.SnippetId == 0 || row.AccountId <= 0) return; row.SetToggling(true);
            try
            {
                var updated = await _snippets.ToggleActiveAsync(row.AccountId, row.SnippetId).ConfigureAwait(false);
                if (updated == null) { await RefreshAsync(); return; }
                void UpdateUI() { row.UpdateFromSnapshotSynchronous(updated.IsActive, updated.UpdatedAt, updated.Rev); }
                if (Application.Current?.Dispatcher.CheckAccess() == true) UpdateUI(); else Application.Current?.Dispatcher.Invoke(UpdateUI);
            }
            catch (Exception ex) { Debug.WriteLine($"[SnippetsVM] Toggle exception: {ex.Message}"); await RefreshAsync(); }
            finally { row.SetToggling(false); }
        }

        private sealed class DelegateCommand : ICommand
        {
            private readonly Func<object?, Task> _execAsync; private readonly Predicate<object?>? _can;
            public DelegateCommand(Func<object?, Task> execAsync, Predicate<object?>? can = null) { _execAsync = execAsync; _can = can; }
            public bool CanExecute(object? parameter) => _can?.Invoke(parameter) ?? true;
            public async void Execute(object? parameter) => await _execAsync(parameter);
            public event EventHandler? CanExecuteChanged;
            public void RaiseCanExecuteChanged() { var disp = Application.Current?.Dispatcher; if (disp == null || disp.CheckAccess()) CanExecuteChanged?.Invoke(this, EventArgs.Empty); else disp.Invoke(new Action(() => CanExecuteChanged?.Invoke(this, EventArgs.Empty))); }
        }
    }
}
