using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using Wysg.Musm.SnomedTools.Abstractions;
using Wysg.Musm.SnomedTools.ViewModels;
using Wysg.Musm.SnomedTools.Views;
using Wysg.Musm.SnomedTools.Services;

namespace Wysg.Musm.SnomedTools
{
    /// <summary>
    /// Main test window for standalone SNOMED Tools development.
    /// </summary>
    public partial class TestWindow : Window
    {
        private readonly SnomedToolsLocalSettings _settings;
        private bool _useMockServices = true;
        private BackgroundSnomedFetcher? _backgroundFetcher; // Keep reference to background fetcher

        public TestWindow()
        {
            InitializeComponent();
            _settings = new SnomedToolsLocalSettings();
            
            // Cleanup background fetcher when window closes
            Closing += OnWindowClosing;
        }
        
        private void OnWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            // Dispose background fetcher if it exists
            _backgroundFetcher?.Dispose();
            _backgroundFetcher = null;
        }

        private void OnServiceModeChanged(object sender, RoutedEventArgs e)
        {
            _useMockServices = rbMockMode?.IsChecked.GetValueOrDefault(true) ?? true;
        
            // Guard against null during initialization
            if (txtModeDescription == null)
                return;

            if (_useMockServices)
            {
                txtModeDescription.Text = "Using mock services with sample SNOMED concepts (Heart, MI, etc.). No database or network required.";
            }
            else
            {
                txtModeDescription.Text = "Using real Azure SQL and Snowstorm API. Requires valid connection configuration in Settings.";
            }
        }

        private void OnOpenSettings(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow(_settings)
            {
                Owner = this
            };
            settingsWindow.ShowDialog();
        }

        private void OnOpenWordCountImporter(object sender, RoutedEventArgs e)
        {
            try
            {
                ISnowstormClient snowstormClient;
                IPhraseService phraseService;
                ISnomedMapService snomedMapService;

                if (_useMockServices)
                {
                    // Use mock services
                    snowstormClient = new MockSnowstormClient();
                    phraseService = new MockPhraseService();
                    snomedMapService = new MockSnomedMapService();
                }
                else
                {
                    // Use real services
                    try
                    {
                        snowstormClient = new RealSnowstormClient(_settings);
                        phraseService = new RealPhraseService(_settings);
                        snomedMapService = new RealSnomedMapService(_settings);
                    }
                    catch (InvalidOperationException ex)
                    {
                        MessageBox.Show(
                            $"Configuration Error:\n\n{ex.Message}\n\nPlease configure connection settings first.",
                            "Configuration Required",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning
                        );
                        return;
                    }
                }

                // Create ViewModel with selected services
                var viewModel = new SnomedWordCountImporterViewModel(
                    snowstormClient,
                    phraseService,
                    snomedMapService
                );

                // Create and show the window
                var window = new SnomedWordCountImporterWindow(viewModel)
                {
                    Owner = this
                };

                window.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error opening Word Count Importer:\n\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }
        
        private void OnOpenCacheReview(object sender, RoutedEventArgs e)
        {
            try
            {
                ISnowstormClient snowstormClient;
                IPhraseService phraseService;
                ISnomedMapService snomedMapService;
                ISnomedCacheService cacheService;

                if (_useMockServices)
                {
                    // Use mock services
                    snowstormClient = new MockSnowstormClient();
                    phraseService = new MockPhraseService();
                    snomedMapService = new MockSnomedMapService();
                    cacheService = new SqliteSnomedCacheService(phraseService); // Pass phraseService for Azure SQL checking
                }
                else
                {
                    // Use real services
                    try
                    {
                        snowstormClient = new RealSnowstormClient(_settings);
                        phraseService = new RealPhraseService(_settings);
                        snomedMapService = new RealSnomedMapService(_settings);
                        cacheService = new SqliteSnomedCacheService(phraseService); // Pass phraseService for Azure SQL checking
                    }
                    catch (InvalidOperationException ex)
                    {
                        MessageBox.Show(
                            $"Configuration Error:\n\n{ex.Message}\n\nPlease configure connection settings first.",
                            "Configuration Required",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning
                        );
                        return;
                    }
                }

                // Create or reuse background fetcher
                if (_backgroundFetcher == null)
                {
                    _backgroundFetcher = new BackgroundSnomedFetcher(
                        snowstormClient,
                        cacheService,
                        targetWordCount: 1 // Start with 1-word terms
                    );
                    
                    // Try to restore progress from last session
                    _ = Task.Run(async () =>
                    {
                        var restored = await _backgroundFetcher.RestoreProgressAsync();
                        if (restored)
                        {
                            System.Diagnostics.Debug.WriteLine("[TestWindow] Fetch progress restored from last session");
                        }
                    });
                }

                // Create ViewModel
                var viewModel = new CacheReviewViewModel(
                    cacheService,
                    phraseService,
                    snomedMapService,
                    _backgroundFetcher
                );

                // Create and show window
                var window = new CacheReviewWindow(viewModel)
                {
                    Owner = this
                };

                window.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error opening Cache Review:\n\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        #region Mock Service Implementations

        /// <summary>
        /// Mock implementation of ISnowstormClient for testing.
        /// </summary>
        internal class MockSnowstormClient : ISnowstormClient
        {
            public Task<(IReadOnlyList<SnomedConceptWithTerms> concepts, string? nextSearchAfter)> BrowseBySemanticTagAsync(
                string semanticTag,
                int offset = 0,
                int limit = 10,
                string? searchAfterToken = null)
            {
                // Return sample concepts
                var concepts = new List<SnomedConceptWithTerms>
                {
                    new SnomedConceptWithTerms(
                        80891009,
                        "80891009",
                        "Heart structure (body structure)",
                        "Heart structure",
                        true,
                        DateTime.UtcNow,
                        new List<SnomedTerm>
                        {
                            new SnomedTerm("Heart structure (body structure)", "FSN"),
                            new SnomedTerm("Heart structure", "PT"),
                            new SnomedTerm("Heart", "Synonym"),
                            new SnomedTerm("Cardiac structure", "Synonym")
                        }
                    ),
                    new SnomedConceptWithTerms(
                        22298006,
                        "22298006",
                        "Myocardial infarction (disorder)",
                        "Myocardial infarction",
                        true,
                        DateTime.UtcNow,
                        new List<SnomedTerm>
                        {
                            new SnomedTerm("Myocardial infarction (disorder)", "FSN"),
                            new SnomedTerm("Myocardial infarction", "PT"),
                            new SnomedTerm("Heart attack", "Synonym"),
                            new SnomedTerm("MI", "Synonym")
                        }
                    )
                };

                return Task.FromResult<(IReadOnlyList<SnomedConceptWithTerms>, string?)>((concepts, null));
            }
        }

        /// <summary>
        /// Mock implementation of ISnomedMapService for testing.
        /// </summary>
        internal class MockSnomedMapService : ISnomedMapService
        {
            public Task CacheConceptAsync(SnomedConcept concept)
            {
                // Mock: Do nothing
                return Task.CompletedTask;
            }

            public Task<bool> MapPhraseAsync(
                long phraseId,
                long? accountId,
                long conceptId,
                string mappingType = "exact",
                decimal? confidence = null,
                string? notes = null,
                long? mappedBy = null)
            {
                // Mock: Always succeed
                return Task.FromResult(true);
            }
        }

        /// <summary>
        /// Mock implementation of IPhraseService for testing.
        /// </summary>
        internal class MockPhraseService : IPhraseService
        {
            public Task<IReadOnlyList<PhraseInfo>> GetAllGlobalPhraseMetaAsync()
            {
                // Mock: Return sample global phrases
                var phrases = new List<PhraseInfo>
                {
                    new PhraseInfo(1, null, "heart", true, DateTime.UtcNow, 1),
                    new PhraseInfo(2, null, "myocardial infarction", true, DateTime.UtcNow, 1),
                    new PhraseInfo(3, null, "pulmonary embolism", true, DateTime.UtcNow, 1)
                };
                return Task.FromResult<IReadOnlyList<PhraseInfo>>(phrases);
            }

            public Task<PhraseInfo> UpsertPhraseAsync(long? accountId, string text, bool active = true)
            {
                // Mock: Return a new phrase info
                var phraseInfo = new PhraseInfo(
                    Random.Shared.NextInt64(1000, 9999),
                    accountId,
                    text,
                    active,
                    DateTime.UtcNow,
                    1
                );
                return Task.FromResult(phraseInfo);
            }

            public Task RefreshGlobalPhrasesAsync()
            {
                // Mock: Do nothing
                return Task.CompletedTask;
            }
        }

        #endregion
    }
}
