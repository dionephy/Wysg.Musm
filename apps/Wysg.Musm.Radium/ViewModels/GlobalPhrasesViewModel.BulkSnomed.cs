using System;
using System.Linq;
using System.Threading.Tasks;

namespace Wysg.Musm.Radium.ViewModels
{
    /// <summary>
    /// Bulk SNOMED search and multi-select add functionality for GlobalPhrasesViewModel.
    /// </summary>
    public sealed partial class GlobalPhrasesViewModel
    {
        private string _bulkSnomedSearchText = string.Empty;
        private bool _filterBodyStructure;
        private bool _filterFinding;
        private bool _filterDisorder;
        private bool _filterProcedure;
        private bool _filterActiveOnly = true;
        private int _currentPage = 1;
        private int _pageSize = 20;
        private int _totalResults = 0;
        private string _lastSearchTerm = string.Empty;

        public string BulkSnomedSearchText
        {
            get => _bulkSnomedSearchText;
            set
            {
                if (_bulkSnomedSearchText != value)
                {
                    _bulkSnomedSearchText = value;
                    OnPropertyChanged();
                    BulkSearchSnomedCommand.NotifyCanExecuteChanged();
                }
            }
        }

        public bool FilterBodyStructure
        {
            get => _filterBodyStructure;
            set
            {
                if (_filterBodyStructure != value)
                {
                    _filterBodyStructure = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool FilterFinding
        {
            get => _filterFinding;
            set
            {
                if (_filterFinding != value)
                {
                    _filterFinding = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool FilterDisorder
        {
            get => _filterDisorder;
            set
            {
                if (_filterDisorder != value)
                {
                    _filterDisorder = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool FilterProcedure
        {
            get => _filterProcedure;
            set
            {
                if (_filterProcedure != value)
                {
                    _filterProcedure = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool FilterActiveOnly
        {
            get => _filterActiveOnly;
            set
            {
                if (_filterActiveOnly != value)
                {
                    _filterActiveOnly = value;
                    OnPropertyChanged();
                }
            }
        }

        public int CurrentPage
        {
            get => _currentPage;
            private set
            {
                if (_currentPage != value)
                {
                    _currentPage = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(PageInfo));
                }
            }
        }

        public int PageSize
        {
            get => _pageSize;
            set
            {
                if (_pageSize != value && value > 0)
                {
                    _pageSize = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(PageInfo));
                    // Reset to page 1 when page size changes
                    if (!string.IsNullOrWhiteSpace(_lastSearchTerm))
                    {
                        CurrentPage = 1;
                        _ = BulkSearchSnomedAsync();
                    }
                }
            }
        }

        public string PageInfo => _totalResults > 0 
            ? $"Showing {(CurrentPage - 1) * PageSize + 1}-{Math.Min(CurrentPage * PageSize, _totalResults)} of {_totalResults}"
            : "No results";

        public bool CanGoToPreviousPage => CurrentPage > 1;
        public bool CanGoToNextPage => CurrentPage * PageSize < _totalResults;

        public string BulkSelectionSummary =>
            $"Selected: {BulkSnomedSearchResults.Count(c => c.IsSelected)} of {BulkSnomedSearchResults.Count}";

        private async Task BulkSearchSnomedAsync()
        {
            if (_snowstormClient == null)
            {
                StatusMessage = "Snowstorm client not available. Check Snowstorm connection settings.";
                return;
            }

            try
            {
                IsBusy = true;
                
                // Check if this is a new search or pagination
                bool isNewSearch = !_lastSearchTerm.Equals(BulkSnomedSearchText, StringComparison.OrdinalIgnoreCase);
                if (isNewSearch)
                {
                    CurrentPage = 1;
                    _lastSearchTerm = BulkSnomedSearchText;
                }

                StatusMessage = $"Searching Snowstorm for '{BulkSnomedSearchText}' (page {CurrentPage})...";

                BulkSnomedSearchResults.Clear();
                
                // Fetch more results than needed (e.g., 200) to support pagination
                // Snowstorm API limit is typically 50-100, so we'll fetch 200 and cache locally
                var allResults = await _snowstormClient.SearchConceptsAsync(BulkSnomedSearchText, limit: 200);
                _totalResults = allResults.Count;

                // Apply semantic tag filters if any are selected
                var hasFilters = FilterBodyStructure || FilterFinding || FilterDisorder || FilterProcedure;
                
                var filteredResults = allResults.AsEnumerable();
                
                if (hasFilters)
                {
                    filteredResults = filteredResults.Where(concept =>
                    {
                        var semanticTag = ExtractSemanticTag(concept.Fsn);
                        
                        if (FilterBodyStructure && semanticTag?.Contains("body structure", StringComparison.OrdinalIgnoreCase) == true)
                            return true;
                        if (FilterFinding && semanticTag?.Contains("finding", StringComparison.OrdinalIgnoreCase) == true)
                            return true;
                        if (FilterDisorder && semanticTag?.Contains("disorder", StringComparison.OrdinalIgnoreCase) == true)
                            return true;
                        if (FilterProcedure && semanticTag?.Contains("procedure", StringComparison.OrdinalIgnoreCase) == true)
                            return true;

                        return false;
                    }).ToList();
                    
                    _totalResults = filteredResults.Count();
                }

                // Apply pagination
                var pagedResults = filteredResults
                    .Skip((CurrentPage - 1) * PageSize)
                    .Take(PageSize)
                    .ToList();

                foreach (var concept in pagedResults)
                {
                    BulkSnomedSearchResults.Add(new SelectableSnomedConcept(concept));
                }

                StatusMessage = $"Found {_totalResults} SNOMED concepts" +
                               (hasFilters ? " (filtered)" : "") +
                               $" - Page {CurrentPage}";
                
                OnPropertyChanged(nameof(BulkSelectionSummary));
                OnPropertyChanged(nameof(PageInfo));
                OnPropertyChanged(nameof(CanGoToPreviousPage));
                OnPropertyChanged(nameof(CanGoToNextPage));
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

        private void GoToPreviousPage()
        {
            if (CanGoToPreviousPage)
            {
                CurrentPage--;
                _ = BulkSearchSnomedAsync();
            }
        }

        private void GoToNextPage()
        {
            if (CanGoToNextPage)
            {
                CurrentPage++;
                _ = BulkSearchSnomedAsync();
            }
        }

        private void SelectAllBulkResults()
        {
            bool allSelected = BulkSnomedSearchResults.All(c => c.IsSelected);
            
            foreach (var concept in BulkSnomedSearchResults)
            {
                concept.IsSelected = !allSelected; // Toggle all
            }

            OnPropertyChanged(nameof(BulkSelectionSummary));
            BulkAddPhrasesWithSnomedCommand.NotifyCanExecuteChanged();
        }

        private async Task BulkAddPhrasesWithSnomedAsync()
        {
            var selected = BulkSnomedSearchResults.Where(c => c.IsSelected).ToList();
            if (selected.Count == 0)
            {
                StatusMessage = "No concepts selected";
                return;
            }

            if (_snomedMapService == null)
            {
                StatusMessage = "SNOMED mapping service not available";
                return;
            }

            try
            {
                IsBusy = true;
                StatusMessage = $"Adding {selected.Count} phrases with SNOMED mappings...";

                int successCount = 0;
                int errorCount = 0;
                var errors = new System.Collections.Generic.List<string>();

                foreach (var selectable in selected)
                {
                    var concept = selectable.Concept;
                    
                    try
                    {
                        // Extract terms without semantic tags
                        var fsnWithoutTag = ExtractTermFromFsn(concept.Fsn);
                        var ptWithoutTag = !string.IsNullOrWhiteSpace(concept.Pt) 
                            ? concept.Pt.Trim() 
                            : null;

                        // A: Find the shortest term (comparing FSN and PT)
                        string shortestTerm;
                        if (!string.IsNullOrWhiteSpace(ptWithoutTag))
                        {
                            shortestTerm = fsnWithoutTag.Length <= ptWithoutTag.Length 
                                ? fsnWithoutTag 
                                : ptWithoutTag;
                        }
                        else
                        {
                            shortestTerm = fsnWithoutTag;
                        }

                        // B: Find the shortest PT (if different from A)
                        string? shortestPt = null;
                        if (!string.IsNullOrWhiteSpace(ptWithoutTag) && 
                            !ptWithoutTag.Equals(shortestTerm, StringComparison.OrdinalIgnoreCase))
                        {
                            shortestPt = ptWithoutTag;
                        }

                        // Cache the SNOMED concept once
                        await _snomedMapService.CacheConceptAsync(concept);

                        // Save phrase A (shortest term overall)
                        var phraseA = await _phraseService.UpsertPhraseAsync(
                            accountId: null,  // NULL = global phrase
                            text: shortestTerm,
                            active: true
                        );

                        // Map phrase A to concept
                        await _snomedMapService.MapPhraseAsync(
                            phraseA.Id,
                            accountId: null,  // NULL = global
                            concept.ConceptId,
                            mappingType: "exact",
                            confidence: 1.0m,
                            notes: "Bulk-added (shortest term) via Snowstorm search"
                        );

                        successCount++;

                        // Save phrase B (shortest PT if different from A)
                        if (!string.IsNullOrWhiteSpace(shortestPt))
                        {
                            var phraseB = await _phraseService.UpsertPhraseAsync(
                                accountId: null,  // NULL = global phrase
                                text: shortestPt,
                                active: true
                            );

                            // Map phrase B to concept
                            await _snomedMapService.MapPhraseAsync(
                                phraseB.Id,
                                accountId: null,  // NULL = global
                                concept.ConceptId,
                                mappingType: "exact",
                                confidence: 1.0m,
                                notes: "Bulk-added (shortest PT) via Snowstorm search"
                            );

                            successCount++; // Count both phrases
                        }
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        errors.Add($"{concept.ConceptIdStr}: {ex.Message}");
                    }
                }

                // Clear inputs and refresh
                BulkSnomedSearchText = string.Empty;
                BulkSnomedSearchResults.Clear();
                OnPropertyChanged(nameof(BulkSelectionSummary));

                // Clear cache and refresh
                _cache.Clear(-1); // GLOBAL_KEY
                await RefreshPhrasesAsync();

                // Show results
                var summary = $"Added {successCount} phrases from {selected.Count} concepts";
                if (errorCount > 0)
                {
                    summary += $"\n{errorCount} concepts had errors:";
                    var errorText = string.Join("\n", errors.Take(10));
                    if (errors.Count > 10)
                        errorText += $"\n... and {errors.Count - 10} more errors";

                    System.Windows.MessageBox.Show(
                        $"{summary}\n\n{errorText}",
                        "Bulk Add Results",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Warning);
                    StatusMessage = $"Added {successCount} phrases with {errorCount} errors";
                }
                else
                {
                    System.Windows.MessageBox.Show(
                        summary,
                        "Bulk Add Success",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information);
                    StatusMessage = summary;
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error during bulk add: {ex.Message}";
                System.Windows.MessageBox.Show(
                    $"Bulk add failed:\n{ex.Message}",
                    "Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private static string? ExtractSemanticTag(string? fsn)
        {
            if (string.IsNullOrWhiteSpace(fsn)) return null;

            var lastOpenParen = fsn.LastIndexOf('(');
            var lastCloseParen = fsn.LastIndexOf(')');

            if (lastOpenParen >= 0 && lastCloseParen > lastOpenParen)
            {
                return fsn.Substring(lastOpenParen + 1, lastCloseParen - lastOpenParen - 1).Trim();
            }

            return null;
        }

        private static string ExtractTermFromFsn(string? fsn)
        {
            if (string.IsNullOrWhiteSpace(fsn)) return string.Empty;

            // Remove semantic tag (text in parentheses at end)
            var lastOpenParen = fsn.LastIndexOf('(');
            if (lastOpenParen > 0)
            {
                return fsn.Substring(0, lastOpenParen).Trim();
            }

            return fsn.Trim();
        }
    }
}
