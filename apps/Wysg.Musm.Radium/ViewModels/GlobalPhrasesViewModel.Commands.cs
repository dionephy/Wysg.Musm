using System;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Wysg.Musm.Radium.Services;

namespace Wysg.Musm.Radium.ViewModels
{
    /// <summary>
    /// Command handlers for GlobalPhrasesViewModel.
    /// </summary>
    public sealed partial class GlobalPhrasesViewModel
    {
        public IAsyncRelayCommand AddPhraseCommand { get; }
        public IAsyncRelayCommand RefreshCommand { get; }
        public IAsyncRelayCommand SearchAccountPhrasesCommand { get; }
        public IAsyncRelayCommand ConvertSelectedCommand { get; }
        public IRelayCommand SelectAllAccountPhrasesCommand { get; }
        public IAsyncRelayCommand BulkImportCommand { get; }
        public IAsyncRelayCommand SearchSnomedCommand { get; }
        public IAsyncRelayCommand AddPhraseWithSnomedCommand { get; }
        public IAsyncRelayCommand BulkSearchSnomedCommand { get; }
        public IAsyncRelayCommand BulkAddPhrasesWithSnomedCommand { get; }
        public IRelayCommand SelectAllBulkResultsCommand { get; }
        public IRelayCommand PreviousPageCommand { get; }
        public IRelayCommand NextPageCommand { get; }

        private void InitializeCommands()
        {
            ((AsyncRelayCommand)AddPhraseCommand).NotifyCanExecuteChanged();
            ((AsyncRelayCommand)RefreshCommand).NotifyCanExecuteChanged();
            ((AsyncRelayCommand)SearchAccountPhrasesCommand).NotifyCanExecuteChanged();
            ((AsyncRelayCommand)ConvertSelectedCommand).NotifyCanExecuteChanged();
            (SelectAllAccountPhrasesCommand as RelayCommand)?.NotifyCanExecuteChanged();
            ((AsyncRelayCommand)BulkImportCommand).NotifyCanExecuteChanged();
            ((AsyncRelayCommand)SearchSnomedCommand).NotifyCanExecuteChanged();
            ((AsyncRelayCommand)AddPhraseWithSnomedCommand).NotifyCanExecuteChanged();
            ((AsyncRelayCommand)BulkSearchSnomedCommand).NotifyCanExecuteChanged();
            ((AsyncRelayCommand)BulkAddPhrasesWithSnomedCommand).NotifyCanExecuteChanged();
            (SelectAllBulkResultsCommand as RelayCommand)?.NotifyCanExecuteChanged();
            (PreviousPageCommand as RelayCommand)?.NotifyCanExecuteChanged();
            (NextPageCommand as RelayCommand)?.NotifyCanExecuteChanged();
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

                // Store all active phrases in cache (2025-02-02)
                // No sorting here - alphabetical sort applied in ApplyPhraseFilter()
                _allPhrasesCache = phrases.Where(p => p.Active).ToList();
                PhraseTotalCount = _allPhrasesCache.Count;

                // Reset to first page and apply filter (loads only one page into Items)
                _phraseCurrentPageIndex = 0;
                OnPropertyChanged(nameof(PhraseCurrentPageIndex));
                ApplyPhraseFilter();

                StatusMessage = $"Loaded {_allPhrasesCache.Count} global phrases (showing page 1, sorted A-Z)";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading phrases: {ex.Message}";
                _allPhrasesCache.Clear();
                Items.Clear();
                PhraseTotalCount = 0;
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

        public async Task SaveEditAsync(GlobalPhraseItem item)
        {
            if (IsBusy) return;
            if (string.IsNullOrWhiteSpace(item.EditText)) return;

            IsBusy = true;
            StatusMessage = $"Updating phrase text: {item.Text} ¡æ {item.EditText}...";

            try
            {
                var updated = await _phraseService.UpdatePhraseTextAsync(
                    accountId: null,  // NULL = global phrase
                    phraseId: item.Id,
                    newText: item.EditText.Trim()
                );

                if (updated != null)
                {
                    item.UpdateFrom(updated);
                    StatusMessage = $"Updated phrase text to: {item.Text}";

                    // Clear global cache
                    _cache.Clear(-1); // GLOBAL_KEY
                }
                else
                {
                    StatusMessage = $"Failed to update phrase text";
                    item.IsEditing = false;
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error updating phrase: {ex.Message}";
                item.IsEditing = false;
                // Refresh to recover consistency
                await RefreshPhrasesAsync();
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task DeletePhraseAsync(GlobalPhraseItem item)
        {
            if (IsBusy) return;

            // Confirm deletion
            var result = System.Windows.MessageBox.Show(
                $"Are you sure you want to delete the global phrase:\n\n\"{item.Text}\"?\n\nThis cannot be undone.",
                "Confirm Deletion",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (result != System.Windows.MessageBoxResult.Yes)
            {
                StatusMessage = "Deletion cancelled";
                return;
            }

            IsBusy = true;
            StatusMessage = $"Deleting phrase: {item.Text}...";

            try
            {
                // Toggle phrase to inactive (soft delete)
                var updated = await _phraseService.ToggleActiveAsync(
                    accountId: null,  // NULL = global phrase
                    phraseId: item.Id
                );

                if (updated != null && !updated.Active)
                {
                    // Remove from UI collection
                    Items.Remove(item);
                    StatusMessage = $"Deleted phrase: {item.Text}";

                    // Clear global cache
                    _cache.Clear(-1); // GLOBAL_KEY
                }
                else
                {
                    StatusMessage = $"Failed to delete phrase: {item.Text}";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error deleting phrase: {ex.Message}";
                // Refresh to recover consistency
                await RefreshPhrasesAsync();
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task SearchSnomedAsync()
        {
            if (_snowstormClient == null)
            {
                StatusMessage = "Snowstorm client not available. Check Snowstorm connection settings.";
                return;
            }

            try
            {
                IsBusy = true;
                StatusMessage = $"Searching Snowstorm for '{SnomedSearchText}'...";

                SnomedSearchResults.Clear();
                var results = await _snowstormClient.SearchConceptsAsync(SnomedSearchText, limit: 50);

                foreach (var concept in results)
                {
                    SnomedSearchResults.Add(concept);
                }

                StatusMessage = $"Found {results.Count} SNOMED concepts";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error searching Snowstorm: {ex.Message}";
                System.Windows.MessageBox.Show(
                    $"Failed to search Snowstorm:\n{ex.Message}\n\nMake sure your local Snowstorm instance is running.",
                    "Snowstorm Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task AddPhraseWithSnomedAsync()
        {
            if (SelectedSnomedConcept == null)
            {
                StatusMessage = "No SNOMED concept selected";
                return;
            }

            if (_snomedMapService == null)
            {
                StatusMessage = "SNOMED mapping service not available";
                return;
            }

            var phraseText = SnomedSearchText.Trim();
            if (string.IsNullOrWhiteSpace(phraseText))
            {
                StatusMessage = "Phrase text is empty";
                return;
            }

            try
            {
                IsBusy = true;
                StatusMessage = $"Adding phrase '{phraseText}' with SNOMED mapping...";

                // 1. Create the phrase
                var newPhrase = await _phraseService.UpsertPhraseAsync(
                    accountId: null,  // NULL = global phrase
                    text: phraseText,
                    active: true
                );

                // 2. Cache the SNOMED concept
                await _snomedMapService.CacheConceptAsync(SelectedSnomedConcept);

                // 3. Map phrase to concept
                await _snomedMapService.MapPhraseAsync(
                    newPhrase.Id,
                    accountId: null,  // NULL = global
                    SelectedSnomedConcept.ConceptId,
                    mappingType: "exact",
                    confidence: 1.0m,
                    notes: "Auto-mapped via Snowstorm search"
                );

                StatusMessage = $"Added phrase '{phraseText}' mapped to {SelectedSnomedConcept.ConceptIdStr}";

                // Clear inputs and refresh
                SnomedSearchText = string.Empty;
                SelectedSnomedConcept = null;
                SnomedSearchResults.Clear();

                // Clear cache and refresh
                _cache.Clear(-1); // GLOBAL_KEY
                await RefreshPhrasesAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error adding phrase: {ex.Message}";
                System.Windows.MessageBox.Show(
                    $"Failed to add phrase with SNOMED mapping:\n{ex.Message}",
                    "Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task BulkImportAsync()
        {
            try
            {
                // Open file dialog to select CSV file
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "Select CSV file for bulk import",
                    Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                    CheckFileExists = true
                };

                if (dialog.ShowDialog() != true)
                {
                    StatusMessage = "Bulk import cancelled";
                    return;
                }

                IsBusy = true;
                StatusMessage = $"Importing from {System.IO.Path.GetFileName(dialog.FileName)}...";

                // Get bulk importer service from DI
                if (System.Windows.Application.Current is not App app)
                {
                    StatusMessage = "Error: Application context not available";
                    return;
                }

                var bulkImporter = app.Services.GetRequiredService<IPhraseSnomedBulkImporter>();

                // Show import dialog to get options
                var importResult = await bulkImporter.ImportFromCsvAsync(
                    dialog.FileName,
                    accountId: null, // null = global phrases
                    createPhrasesIfMissing: true // Auto-create phrases
                );

                // Display results
                var summary = $"Import complete: {importResult.SuccessfulMappings}/{importResult.TotalRows} mapped successfully";
                if (importResult.PhrasesCreated > 0)
                    summary += $", {importResult.PhrasesCreated} phrases created";
                if (importResult.ConceptsCached > 0)
                    summary += $", {importResult.ConceptsCached} concepts cached";

                if (importResult.FailedMappings > 0)
                {
                    summary += $"\n{importResult.FailedMappings} failed:";
                    var errorText = string.Join("\n", importResult.Errors.Take(10));
                    if (importResult.Errors.Count > 10)
                        errorText += $"\n... and {importResult.Errors.Count - 10} more errors";

                    System.Windows.MessageBox.Show(
                        $"{summary}\n\n{errorText}",
                        "Bulk Import Results",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Warning);
                }
                else
                {
                    System.Windows.MessageBox.Show(
                        summary,
                        "Bulk Import Success",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information);
                }

                StatusMessage = summary.Split('\n')[0]; // First line only

                // Refresh phrases and clear cache
                _cache.Clear(-1); // Clear global cache
                await RefreshPhrasesAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error during bulk import: {ex.Message}";
                System.Windows.MessageBox.Show(
                    $"Bulk import failed:\n{ex.Message}",
                    "Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
