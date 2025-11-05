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
/// <summary>
    /// ViewModel for managing account-specific phrases.
    /// 
    /// Performance optimizations (2025-02-02):
    /// - Search/filter functionality to reduce visible items
    /// - Pagination support to limit items loaded at once
 /// - Virtualization-friendly UI binding
    /// </summary>
    public sealed class PhrasesViewModel : BaseViewModel
    {
        private readonly IPhraseService _phrases;
        private readonly ITenantContext _tenant;

  public ObservableCollection<PhraseRow> Items { get; } = new();
     
        // Hidden full list for filtering/pagination
  private System.Collections.Generic.List<PhraseInfo> _allPhrases = new();

        public PhrasesViewModel(IPhraseService phrases, ITenantContext tenant)
        {
            _phrases = phrases; _tenant = tenant;
  RefreshCommand = new DelegateCommand(async _ => await RefreshAsync());
            AddCommand = new DelegateCommand(async _ => await AddAsync(), _ => !string.IsNullOrWhiteSpace(NewText));
            
    // Pagination commands
 SearchCommand = new DelegateCommand(_ => { _currentPage = 0; OnPropertyChanged(nameof(CurrentPage)); ApplyFilter(); return Task.CompletedTask; }, _ => !IsBusy);
 ClearFilterCommand = new DelegateCommand(_ => { SearchFilter = string.Empty; return Task.CompletedTask; }, _ => !IsBusy && !string.IsNullOrWhiteSpace(SearchFilter));
        FirstPageCommand = new DelegateCommand(_ => { _currentPage = 0; OnPropertyChanged(nameof(CurrentPage)); ApplyFilter(); return Task.CompletedTask; }, _ => !IsBusy && _currentPage > 0);
  LastPageCommand = new DelegateCommand(_ => { _currentPage = Math.Max(0, (int)Math.Ceiling((double)TotalPhraseCount / PageSize) - 1); OnPropertyChanged(nameof(CurrentPage)); ApplyFilter(); return Task.CompletedTask; }, _ => !IsBusy && CanGoToNextPage);
      PreviousPageCommand = new DelegateCommand(_ => { if (CanGoToPreviousPage) { _currentPage--; OnPropertyChanged(nameof(CurrentPage)); ApplyFilter(); } return Task.CompletedTask; }, _ => !IsBusy && CanGoToPreviousPage);
      NextPageCommand = new DelegateCommand(_ => { if (CanGoToNextPage) { _currentPage++; OnPropertyChanged(nameof(CurrentPage)); ApplyFilter(); } return Task.CompletedTask; }, _ => !IsBusy && CanGoToNextPage);
 
        _tenant.AccountIdChanged += OnAccountIdChanged;
      _ = RefreshAsync();
        }

    private void OnAccountIdChanged(long oldId, long newId)
        {
     if (newId <= 0)
       {
         // logout -> clear (synchronous)
     if (Application.Current?.Dispatcher.CheckAccess() == true)
        {
        Items.Clear();
        _allPhrases.Clear();
     TotalPhraseCount = 0;
                }
 else
  {
    Application.Current?.Dispatcher.Invoke(() =>
        {
         Items.Clear();
    _allPhrases.Clear();
         TotalPhraseCount = 0;
            });
          }
         }
     else if (oldId <= 0 && newId > 0)
       {
        // first valid login -> reload
        _ = RefreshAsync();
            }
     }

     // Search/filter properties
     private string _searchFilter = string.Empty;
        private int _pageSize = 100;
        private int _currentPage = 0;
        private int _totalPhraseCount = 0;

        public string SearchFilter
     {
            get => _searchFilter;
          set
    {
            if (SetProperty(ref _searchFilter, value))
                {
        _currentPage = 0;
             OnPropertyChanged(nameof(CurrentPage));
              OnPropertyChanged(nameof(PageInfo));
   ApplyFilter();
         }
  }
        }

        public int PageSize
  {
       get => _pageSize;
   set
   {
   var clamped = Math.Max(10, Math.Min(500, value));
      if (SetProperty(ref _pageSize, clamped))
 {
    _currentPage = 0;
      OnPropertyChanged(nameof(CurrentPage));
       OnPropertyChanged(nameof(PageInfo));
             OnPropertyChanged(nameof(CanGoToNextPage));
         OnPropertyChanged(nameof(CanGoToPreviousPage));
             ApplyFilter();
        }
            }
        }

        public int CurrentPage
        {
         get => _currentPage;
   private set
            {
                if (SetProperty(ref _currentPage, value))
         {
           OnPropertyChanged(nameof(PageInfo));
     OnPropertyChanged(nameof(CanGoToNextPage));
            OnPropertyChanged(nameof(CanGoToPreviousPage));
      }
            }
        }

    public int TotalPhraseCount
        {
     get => _totalPhraseCount;
     private set
        {
if (SetProperty(ref _totalPhraseCount, value))
       {
      OnPropertyChanged(nameof(PageInfo));
    OnPropertyChanged(nameof(CanGoToNextPage));
      }
            }
        }

      public string PageInfo => $"Page {_currentPage + 1} of {Math.Max(1, (int)Math.Ceiling((double)TotalPhraseCount / PageSize))} ({TotalPhraseCount} total)";
     public bool CanGoToNextPage => (_currentPage + 1) * PageSize < TotalPhraseCount;
        public bool CanGoToPreviousPage => _currentPage > 0;

        private string _newText = string.Empty;
        public string NewText { get => _newText; set { if (SetProperty(ref _newText, value)) (AddCommand as DelegateCommand)?.RaiseCanExecuteChanged(); } }

    public ICommand RefreshCommand { get; }
        public ICommand AddCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand ClearFilterCommand { get; }
        public ICommand FirstPageCommand { get; }
  public ICommand LastPageCommand { get; }
        public ICommand PreviousPageCommand { get; }
        public ICommand NextPageCommand { get; }

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
                {
            Items.Clear();
  _allPhrases.Clear();
       TotalPhraseCount = 0;
     }
     else
      {
               Application.Current?.Dispatcher.Invoke(() =>
            {
       Items.Clear();
          _allPhrases.Clear();
       TotalPhraseCount = 0;
       });
                }
          return; 
            }
       
            Debug.WriteLine($"[PhrasesVM] Refreshing phrases from snapshot for account={accountId}");
            
          // Load account-specific and global metadata
    var loadAccountTask = _phrases.GetAllPhraseMetaAsync(accountId);
         var loadGlobalTask = _phrases.GetAllGlobalPhraseMetaAsync();
  await Task.WhenAll(loadAccountTask, loadGlobalTask).ConfigureAwait(false);
            var accountMeta = loadAccountTask.Result;
         var globalMeta = loadGlobalTask.Result;

     // Store all phrases for filtering/pagination
     _allPhrases = accountMeta.Concat(globalMeta).OrderByDescending(m => m.UpdatedAt).ToList();
          TotalPhraseCount = _allPhrases.Count;
      _currentPage = 0;

     // Apply filter to load first page
            void UpdateUI()
       {
          OnPropertyChanged(nameof(CurrentPage));
              ApplyFilter();
      Debug.WriteLine($"[PhrasesVM] Loaded {_allPhrases.Count} total phrases (account + global), showing page 1");
    }

            if (Application.Current?.Dispatcher.CheckAccess() == true)
                UpdateUI();
     else
   Application.Current?.Dispatcher.Invoke(UpdateUI);
        }

     private void ApplyFilter()
        {
   if (_allPhrases == null || _allPhrases.Count == 0)
    {
              Items.Clear();
      TotalPhraseCount = 0;
       return;
            }

          // Filter by search text
            var filtered = _allPhrases.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(_searchFilter))
         {
     var search = _searchFilter.Trim().ToLowerInvariant();
                filtered = filtered.Where(p => p.Text.ToLowerInvariant().Contains(search));
          }

            var filteredList = filtered.ToList();
       TotalPhraseCount = filteredList.Count;

       // Apply pagination
var page = filteredList.Skip(_currentPage * _pageSize).Take(_pageSize).ToList();

            // Update UI
         Items.Clear();
    foreach (var m in page)
            {
        var row = m.AccountId.HasValue 
     ? new PhraseRow(this) { Id = m.Id, AccountId = m.AccountId, Text = m.Text, UpdatedAt = m.UpdatedAt, Rev = m.Rev }
        : new PhraseRow(this) { Id = m.Id, AccountId = null, Text = m.Text, UpdatedAt = m.UpdatedAt, Rev = m.Rev };
           row.InitializeActive(m.Active);
 Items.Add(row);
       }

       // Update command states
            (FirstPageCommand as DelegateCommand)?.RaiseCanExecuteChanged();
            (LastPageCommand as DelegateCommand)?.RaiseCanExecuteChanged();
            (PreviousPageCommand as DelegateCommand)?.RaiseCanExecuteChanged();
   (NextPageCommand as DelegateCommand)?.RaiseCanExecuteChanged();
   (ClearFilterCommand as DelegateCommand)?.RaiseCanExecuteChanged();
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
   
           // Add to full list and refresh view
                _allPhrases.Insert(0, newPhrase);
  TotalPhraseCount = _allPhrases.Count;
           _currentPage = 0;
     
       void AddToUI()
            {
     OnPropertyChanged(nameof(CurrentPage));
ApplyFilter();
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
   
        // Update in full list too
       var existing = _allPhrases.FirstOrDefault(p => p.Id == row.Id);
         if (existing != null)
         {
  var index = _allPhrases.IndexOf(existing);
      _allPhrases[index] = updated;
 }
      
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
