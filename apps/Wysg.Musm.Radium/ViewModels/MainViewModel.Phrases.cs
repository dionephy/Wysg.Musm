using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Wysg.Musm.Radium.ViewModels
{
    /// <summary>
    /// Phrase with SNOMED semantic tag information for syntax highlighting.
    /// </summary>
    public sealed record PhraseWithSemanticTag(string Text, string? SemanticTag);

    public partial class MainViewModel
    {
        private IReadOnlyList<string> _currentPhraseSnapshot = Array.Empty<string>();
        private IReadOnlyDictionary<string, string?> _phraseSemanticTags = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyList<string> CurrentPhraseSnapshot
        {
            get => _currentPhraseSnapshot;
            set
            {
                _currentPhraseSnapshot = value ?? Array.Empty<string>();
                OnPropertyChanged(nameof(CurrentPhraseSnapshot));
            }
        }

        /// <summary>
        /// Dictionary mapping phrase text (case-insensitive) to SNOMED semantic tag.
        /// Used by editor for semantic tag-based syntax highlighting.
        /// </summary>
        public IReadOnlyDictionary<string, string?> PhraseSemanticTags
        {
            get => _phraseSemanticTags;
            private set
            {
                _phraseSemanticTags = value ?? new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
                OnPropertyChanged(nameof(PhraseSemanticTags));
            }
        }

        public async Task LoadPhrasesAsync()
        {
            try
            {
                var accountId = _tenant?.AccountId ?? 0;
                if (accountId <= 0)
                {
                    CurrentPhraseSnapshot = Array.Empty<string>();
                    PhraseSemanticTags = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
                    return;
                }
                
                // FIXED: Use unfiltered phrase list for syntax highlighting (includes ALL phrases regardless of word count)
                // Completion window uses GetCombinedPhrasesByPrefixAsync which has its own 3-word filter
                var list = await _phrases.GetAllPhrasesForHighlightingAsync(accountId).ConfigureAwait(false);
                CurrentPhraseSnapshot = list ?? Array.Empty<string>();
                
                System.Diagnostics.Debug.WriteLine($"[LoadPhrases] Loaded {CurrentPhraseSnapshot.Count} phrases for highlighting (unfiltered)");
                
                // Load SNOMED semantic tags for global phrases (account phrases don't have SNOMED mappings yet)
                var semanticTags = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
                
                if (_snomedMapService != null)
                {
                    try
                    {
                        // Get all global phrase metadata (no limit - load ALL)
                        var globalPhrases = await _phrases.GetAllGlobalPhraseMetaAsync().ConfigureAwait(false);
                        System.Diagnostics.Debug.WriteLine($"[SemanticTag] Loading mappings for {globalPhrases.Count} global phrases...");
                        
                        // Use batch loading to avoid N+1 query problem (single query instead of thousands)
                        var phraseIds = globalPhrases.Select(p => p.Id).ToList();
                        var mappings = await _snomedMapService.GetMappingsBatchAsync(phraseIds).ConfigureAwait(false);
                        
                        System.Diagnostics.Debug.WriteLine($"[SemanticTag] Batch loaded {mappings.Count} mappings");
                        
                        // Extract semantic tags from mappings
                        foreach (var phrase in globalPhrases)
                        {
                            if (mappings.TryGetValue(phrase.Id, out var mapping))
                            {
                                var semanticTag = mapping.GetSemanticTag();
                                if (!string.IsNullOrWhiteSpace(semanticTag))
                                {
                                    semanticTags[phrase.Text] = semanticTag;
                                }
                            }
                        }
                        
                        System.Diagnostics.Debug.WriteLine($"[SemanticTag] Total semantic tags loaded: {semanticTags.Count} from {globalPhrases.Count} global phrases");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[SemanticTag] Error loading semantic tags: {ex.Message}");
                        // If SNOMED service fails, just use empty semantic tags
                    }
                }
                
                PhraseSemanticTags = semanticTags;
            }
            catch
            {
                CurrentPhraseSnapshot = Array.Empty<string>();
                PhraseSemanticTags = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            }
        }

        public async Task RefreshPhrasesAsync()
        {
            try
            {
                var accountId = _tenant?.AccountId ?? 0;
                if (accountId <= 0) return;
                await _phrases.RefreshGlobalPhrasesAsync().ConfigureAwait(false);
                await _phrases.RefreshPhrasesAsync(accountId).ConfigureAwait(false);
                await LoadPhrasesAsync().ConfigureAwait(false);
            }
            catch { }
        }
    }
}

