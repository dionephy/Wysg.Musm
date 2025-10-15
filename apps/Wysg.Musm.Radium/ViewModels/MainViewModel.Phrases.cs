using System;
using System.Collections.Generic;
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
                
                // Load combined phrases (global + account)
                var list = await _phrases.GetCombinedPhrasesAsync(accountId).ConfigureAwait(false);
                CurrentPhraseSnapshot = list ?? Array.Empty<string>();
                
                // Load SNOMED semantic tags for global phrases (account phrases don't have SNOMED mappings yet)
                var semanticTags = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
                
                if (_snomedMapService != null)
                {
                    try
                    {
                        // Get all global phrase metadata
                        var globalPhrases = await _phrases.GetAllGlobalPhraseMetaAsync().ConfigureAwait(false);
                        
                        // Load mappings for each global phrase
                        foreach (var phrase in globalPhrases)
                        {
                            try
                            {
                                var mapping = await _snomedMapService.GetMappingAsync(phrase.Id).ConfigureAwait(false);
                                if (mapping != null)
                                {
                                    var semanticTag = mapping.GetSemanticTag();
                                    if (!string.IsNullOrWhiteSpace(semanticTag))
                                    {
                                        semanticTags[phrase.Text] = semanticTag;
                                        System.Diagnostics.Debug.WriteLine($"[SemanticTag] Loaded: '{phrase.Text}' ¡æ '{semanticTag}'");
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"[SemanticTag] Error loading mapping for '{phrase.Text}': {ex.Message}");
                            }
                        }
                        
                        System.Diagnostics.Debug.WriteLine($"[SemanticTag] Total semantic tags loaded: {semanticTags.Count}");
                    }
                    catch
                    {
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
