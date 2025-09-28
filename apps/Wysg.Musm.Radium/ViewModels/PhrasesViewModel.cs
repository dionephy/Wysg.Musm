using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Wysg.Musm.Infrastructure.ViewModels;
using Wysg.Musm.Radium.Services;
using System.Windows;
using System.Diagnostics;

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
            _ = RefreshAsync();
        }

        private string _newText = string.Empty;
        public string NewText { get => _newText; set { if (SetProperty(ref _newText, value)) (AddCommand as DelegateCommand)?.RaiseCanExecuteChanged(); } }

        public ICommand RefreshCommand { get; }
        public ICommand AddCommand { get; }

        public sealed class PhraseRow : BaseViewModel
        {
            private readonly PhrasesViewModel _owner;
            public PhraseRow(PhrasesViewModel owner) { _owner = owner; }
            public long Id { get; init; }
            public string Text { get; set; } = string.Empty;
            private bool _active;
            public bool Active
            {
                get => _active;
                set
                {
                    if (SetProperty(ref _active, value))
                        _ = _owner.OnActiveChangedAsync(this); // fire & forget
                }
            }
            public DateTime UpdatedAt { get; set; }
            public long Rev { get; set; }
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
            var accountId = await ResolveAccountIdAsync();
            if (accountId == null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Items.Clear();
                    var row = new PhraseRow(this) { Id = 0, Text = "<no phrases yet>", Active = false, UpdatedAt = DateTime.UtcNow, Rev = 0 };
                    Items.Add(row);
                });
                return;
            }
            var meta = await _phrases.GetAllPhraseMetaAsync(accountId.Value).ConfigureAwait(false);
            Application.Current.Dispatcher.Invoke(() =>
            {
                Items.Clear();
                foreach (var m in meta)
                {
                    var row = new PhraseRow(this) { Id = m.Id, Text = m.Text, Active = m.Active, UpdatedAt = m.UpdatedAt, Rev = m.Rev };
                    Items.Add(row);
                }
            });
        }

        private async Task AddAsync()
        {
            var accountId = await ResolveAccountIdAsync();
            if (accountId == null) return; // no account
            var text = NewText.Trim();
            if (text.Length == 0) return;
            _ = await _phrases.UpsertPhraseAsync(accountId.Value, text, true);
            NewText = string.Empty;
            await RefreshAsync();
        }

        private async Task OnActiveChangedAsync(PhraseRow row)
        {
            if (row.Id == 0) return; // placeholder row
            var accountId = await ResolveAccountIdAsync();
            if (accountId == null) return;
            // Toggle service simply flips; ensure desired end state matches
            var updated = await _phrases.ToggleActiveAsync(accountId.Value, row.Id).ConfigureAwait(false);
            if (updated == null) return;
            // If flip mismatched user intention (because they clicked while stale), adjust again
            if (updated.Active != row.Active)
            {
                updated = await _phrases.ToggleActiveAsync(accountId.Value, row.Id).ConfigureAwait(false);
                if (updated == null) return;
            }
            Application.Current.Dispatcher.Invoke(() =>
            {
                row.Active = updated.Active; // may trigger re-entry; guard by comparing
                row.UpdatedAt = updated.UpdatedAt;
                row.Rev = updated.Rev;
            });
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
                    disp.BeginInvoke(new Action(() => CanExecuteChanged?.Invoke(this, EventArgs.Empty)));
            }
        }
    }
}
