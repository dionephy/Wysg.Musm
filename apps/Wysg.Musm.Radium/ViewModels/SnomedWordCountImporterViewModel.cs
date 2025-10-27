using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Wysg.Musm.Radium.Services;

namespace Wysg.Musm.Radium.ViewModels
{
    /// <summary>
    /// ViewModel for importing SNOMED-CT concepts as global phrases filtered by word count.
    /// Searches ALL domains - no semantic tag filtering.
    /// Supports session persistence to resume imports across window closes.
    /// </summary>
    public sealed class SnomedWordCountImporterViewModel : INotifyPropertyChanged
    {
        private readonly ISnowstormClient _snowstormClient;
        private readonly IPhraseService _phraseService;
        private readonly ISnomedMapService _snomedMapService;

        private int _targetWordCount = 1;
        private bool _isBusy;
    private string _statusMessage = string.Empty;
        private string _currentTerm = string.Empty;
        private string _currentConceptInfo = string.Empty;
  private int _addedCount = 0;
        private int _ignoredCount = 0;
        private int _totalProcessedCount = 0;

        // Pagination state for browsing concepts
        private readonly Queue<(SnomedConceptWithTerms concept, SnomedTerm term)> _candidateQueue = new();
   private string? _nextSearchAfter = null;
    private int _currentPage = 0;
        private bool _hasMoreConcepts = true;

        // Session persistence
      private static readonly string SessionFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Wysg.Musm.Radium",
    "snomed_wordcount_session.json");

        public int TargetWordCount
        {
          get => _targetWordCount;
         set
      {
       if (_targetWordCount != value && value >= 1 && value <= 10)
   {
         _targetWordCount = value;
    OnPropertyChanged();
     ResetSearch();
   }
          }
   }

        public bool IsBusy
        {
       get => _isBusy;
            private set
  {
   if (_isBusy != value)
      {
   _isBusy = value;
      OnPropertyChanged();
        ((AsyncRelayCommand)AddCommand).NotifyCanExecuteChanged();
              ((AsyncRelayCommand)IgnoreCommand).NotifyCanExecuteChanged();
    ((AsyncRelayCommand)StartCommand).NotifyCanExecuteChanged();
     }
       }
      }

        public string StatusMessage
     {
         get => _statusMessage;
            private set
   {
      if (_statusMessage != value)
              {
              _statusMessage = value;
            OnPropertyChanged();
         }
   }
        }

      public string CurrentTerm
        {
  get => _currentTerm;
            private set
    {
                if (_currentTerm != value)
    {
     _currentTerm = value;
             OnPropertyChanged();
   }
            }
      }

        public string CurrentConceptInfo
  {
       get => _currentConceptInfo;
  private set
  {
    if (_currentConceptInfo != value)
       {
_currentConceptInfo = value;
   OnPropertyChanged();
}
   }
   }

      public int AddedCount
{
    get => _addedCount;
         private set
    {
     if (_addedCount != value)
       {
    _addedCount = value;
           OnPropertyChanged();
          UpdateStatusMessage();
          }
          }
        }

        public int IgnoredCount
        {
            get => _ignoredCount;
 private set
    {
      if (_ignoredCount != value)
                {
           _ignoredCount = value;
 OnPropertyChanged();
          UpdateStatusMessage();
     }
       }
    }

        public int TotalProcessedCount
        {
            get => _totalProcessedCount;
            private set
            {
           if (_totalProcessedCount != value)
       {
            _totalProcessedCount = value;
     OnPropertyChanged();
       UpdateStatusMessage();
        }
     }
        }

        public IAsyncRelayCommand StartCommand { get; }
        public IAsyncRelayCommand AddCommand { get; }
        public IAsyncRelayCommand IgnoreCommand { get; }

   public SnomedWordCountImporterViewModel(
            ISnowstormClient snowstormClient,
    IPhraseService phraseService,
     ISnomedMapService snomedMapService)
        {
       _snowstormClient = snowstormClient;
            _phraseService = phraseService;
    _snomedMapService = snomedMapService;

   StartCommand = new AsyncRelayCommand(StartImportAsync, () => !IsBusy);
     AddCommand = new AsyncRelayCommand(AddCurrentTermAsync, () => !IsBusy && !string.IsNullOrEmpty(CurrentTerm));
        IgnoreCommand = new AsyncRelayCommand(IgnoreCurrentTermAsync, () => !IsBusy && !string.IsNullOrEmpty(CurrentTerm));

            // Try to restore previous session
   TryRestoreSession();
        }

        private void ResetSearch()
   {
    _candidateQueue.Clear();
    _nextSearchAfter = null;
      _currentPage = 0;
          _hasMoreConcepts = true;
       CurrentTerm = string.Empty;
            CurrentConceptInfo = string.Empty;
        AddedCount = 0;
            IgnoredCount = 0;
          TotalProcessedCount = 0;
         StatusMessage = $"Ready to search all SNOMED concepts for {TargetWordCount}-word synonyms";
   }

        private async Task StartImportAsync()
        {
   try
{
  IsBusy = true;
            ResetSearch();
         StatusMessage = $"Starting import for {TargetWordCount}-word synonyms across all domains...";

        // Load first candidate
     await LoadNextCandidateAsync();
      }
         catch (Exception ex)
            {
                StatusMessage = $"Error starting import: {ex.Message}";
    System.Diagnostics.Debug.WriteLine($"[SnomedWordCountImporter] Error: {ex}");
            }
       finally
   {
           IsBusy = false;
         }
        }

        private async Task AddCurrentTermAsync()
    {
            if (string.IsNullOrEmpty(CurrentTerm)) return;

            try
      {
       IsBusy = true;
          StatusMessage = $"Adding '{CurrentTerm}' as global phrase...";

 // Extract concept info from CurrentConceptInfo
     var parts = CurrentConceptInfo.Split(new[] { " | " }, StringSplitOptions.None);
                if (parts.Length < 2)
         {
       StatusMessage = "Error: Invalid concept info";
  return;
             }

  var conceptIdStr = parts[0].Trim('[', ']');
      if (!long.TryParse(conceptIdStr, out var conceptId))
         {
      StatusMessage = "Error: Invalid concept ID";
      return;
                }

       var fsn = parts[1];

  // Create the phrase (active)
       var newPhrase = await _phraseService.UpsertPhraseAsync(
 accountId: null,  // NULL = global phrase
         text: CurrentTerm.ToLowerInvariant().Trim(),
    active: true
       );

            // Cache the SNOMED concept
       var concept = new SnomedConcept(conceptId, conceptIdStr, fsn, null, true, DateTime.UtcNow);
       await _snomedMapService.CacheConceptAsync(concept);

     // Map phrase to concept
    await _snomedMapService.MapPhraseAsync(
     newPhrase.Id,
    accountId: null,
 conceptId,
        mappingType: "exact",
           confidence: 1.0m,
       notes: $"Imported via Word Count Importer ({TargetWordCount}-word)"
                );

          AddedCount++;
      TotalProcessedCount++;

      // Save session before loading next
                SaveSession();

 // Load next candidate
     await LoadNextCandidateAsync();
         }
  catch (Exception ex)
            {
         StatusMessage = $"Error adding phrase: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"[SnomedWordCountImporter] Error adding: {ex}");
            }
            finally
     {
        IsBusy = false;
            }
        }

        private async Task IgnoreCurrentTermAsync()
        {
            if (string.IsNullOrEmpty(CurrentTerm)) return;

 try
         {
      IsBusy = true;
    StatusMessage = $"Ignoring '{CurrentTerm}' (adding as inactive)...";

                // Extract concept info
         var parts = CurrentConceptInfo.Split(new[] { " | " }, StringSplitOptions.None);
  if (parts.Length < 2)
       {
   StatusMessage = "Error: Invalid concept info";
 return;
     }

    var conceptIdStr = parts[0].Trim('[', ']');
     if (!long.TryParse(conceptIdStr, out var conceptId))
{
                 StatusMessage = "Error: Invalid concept ID";
  return;
                }

        var fsn = parts[1];

    // Create the phrase (inactive)
         var newPhrase = await _phraseService.UpsertPhraseAsync(
            accountId: null,  // NULL = global phrase
              text: CurrentTerm.ToLowerInvariant().Trim(),
        active: false
        );

         // Cache the SNOMED concept
        var concept = new SnomedConcept(conceptId, conceptIdStr, fsn, null, true, DateTime.UtcNow);
         await _snomedMapService.CacheConceptAsync(concept);

       // Map phrase to concept
  await _snomedMapService.MapPhraseAsync(
       newPhrase.Id,
         accountId: null,
        conceptId,
             mappingType: "exact",
       confidence: 1.0m,
 notes: $"Ignored via Word Count Importer ({TargetWordCount}-word)"
       );

   IgnoredCount++;
        TotalProcessedCount++;

      // Save session before loading next
          SaveSession();

    // Load next candidate
  await LoadNextCandidateAsync();
            }
            catch (Exception ex)
   {
           StatusMessage = $"Error ignoring phrase: {ex.Message}";
    System.Diagnostics.Debug.WriteLine($"[SnomedWordCountImporter] Error ignoring: {ex}");
   }
            finally
     {
      IsBusy = false;
       }
        }

private async Task LoadNextCandidateAsync()
      {
            while (true)
    {
     // Check if we have candidates in queue
   if (_candidateQueue.Count > 0)
           {
var (concept, term) = _candidateQueue.Dequeue();

      // Check if this phrase already exists
    var phraseText = term.Term.ToLowerInvariant().Trim();
    var existingPhrases = await _phraseService.GetAllGlobalPhraseMetaAsync();
       var exists = existingPhrases.Any(p => p.Text.Trim().ToLowerInvariant() == phraseText);

         if (exists)
   {
                    System.Diagnostics.Debug.WriteLine($"[SnomedWordCountImporter] Skipping existing phrase: {phraseText}");
             continue; // Skip to next candidate
        }

      // Display this candidate
         CurrentTerm = term.Term;
    CurrentConceptInfo = $"[{concept.ConceptIdStr}] | {concept.Fsn}";
StatusMessage = $"Found candidate: {term.Term} ({term.Type})";
           
   // Save session with current candidate
          SaveSession();
        return;
    }

             // No more candidates in queue - fetch next page
      if (!_hasMoreConcepts)
                {
         CurrentTerm = string.Empty;
       CurrentConceptInfo = string.Empty;
   StatusMessage = $"Import complete! Added: {AddedCount}, Ignored: {IgnoredCount}, Total: {TotalProcessedCount}";
  
         // Clear session on completion
  ClearSession();
             return;
       }

    StatusMessage = $"Loading more concepts (page {_currentPage + 1})...";
                await FetchNextPageAsync();
     }
   }

     private async Task FetchNextPageAsync()
        {
       try
      {
const int pageSize = 50; // Fetch 50 concepts at a time
         var offset = _currentPage * pageSize;

       // Search ALL domains by using "all" semantic tag
         var (concepts, nextSearchAfter) = await _snowstormClient.BrowseBySemanticTagAsync(
          "all",
    offset,
       pageSize,
            _nextSearchAfter
           );

    _nextSearchAfter = nextSearchAfter;
                _currentPage++;

     if (concepts.Count == 0)
   {
           _hasMoreConcepts = false;
                  System.Diagnostics.Debug.WriteLine("[SnomedWordCountImporter] No more concepts available");
   return;
        }

     System.Diagnostics.Debug.WriteLine($"[SnomedWordCountImporter] Fetched {concepts.Count} concepts from page {_currentPage}");

         // Filter concepts for target word count
      foreach (var concept in concepts)
         {
           foreach (var term in concept.AllTerms)
   {
   // Only consider synonyms (not FSN or PT which are usually longer)
           if (!string.Equals(term.Type, "Synonym", StringComparison.OrdinalIgnoreCase))
     continue;

       var wordCount = CountWords(term.Term);
      if (wordCount == _targetWordCount)
         {
     _candidateQueue.Enqueue((concept, term));
      System.Diagnostics.Debug.WriteLine($"[SnomedWordCountImporter] Added candidate: {term.Term} ({wordCount} words)");
  }
       }
    }

         System.Diagnostics.Debug.WriteLine($"[SnomedWordCountImporter] Queue now has {_candidateQueue.Count} candidates");
                
        // Save session after successful fetch
SaveSession();
     }
            catch (Exception ex)
            {
 StatusMessage = $"Error fetching concepts: {ex.Message}";
  System.Diagnostics.Debug.WriteLine($"[SnomedWordCountImporter] Error fetching page: {ex}");
_hasMoreConcepts = false;
            }
 }

        private static int CountWords(string text)
  {
            if (string.IsNullOrWhiteSpace(text)) return 0;
          return text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length;
        }

        private void UpdateStatusMessage()
   {
      if (!string.IsNullOrEmpty(CurrentTerm))
                StatusMessage = $"Processing... Added: {AddedCount}, Ignored: {IgnoredCount}, Total: {TotalProcessedCount}";
        }

        // Session persistence methods
    public void SaveSession()
   {
 try
   {
         var session = new ImportSession
       {
  TargetWordCount = _targetWordCount,
      NextSearchAfter = _nextSearchAfter,
     CurrentPage = _currentPage,
 HasMoreConcepts = _hasMoreConcepts,
    AddedCount = _addedCount,
 IgnoredCount = _ignoredCount,
       TotalProcessedCount = _totalProcessedCount,
       CandidateQueue = _candidateQueue.Select(c => new CandidateDto
   {
        ConceptId = c.concept.ConceptId,
  ConceptIdStr = c.concept.ConceptIdStr,
         Fsn = c.concept.Fsn,
   Pt = c.concept.Pt,
       Active = c.concept.Active,
  TermText = c.term.Term,
      TermType = c.term.Type
      }).ToList()
          };

             var dir = Path.GetDirectoryName(SessionFilePath);
       if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
      Directory.CreateDirectory(dir);

      var json = JsonSerializer.Serialize(session, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(SessionFilePath, json);
 
    var tokenPreview = _nextSearchAfter != null ? _nextSearchAfter.Substring(0, Math.Min(20, _nextSearchAfter.Length)) : "null";
    System.Diagnostics.Debug.WriteLine($"[SnomedWordCountImporter] Session saved: page={_currentPage}, queue={_candidateQueue.Count}, token={tokenPreview}...");
     }
  catch (Exception ex)
      {
   System.Diagnostics.Debug.WriteLine($"[SnomedWordCountImporter] Failed to save session: {ex.Message}");
 }
        }

   private void TryRestoreSession()
        {
        try
 {
                if (!File.Exists(SessionFilePath))
     {
         StatusMessage = $"Ready to search all SNOMED concepts for {TargetWordCount}-word synonyms";
      return;
      }

          var json = File.ReadAllText(SessionFilePath);
var session = JsonSerializer.Deserialize<ImportSession>(json);
     
            if (session == null)
     return;

 // Restore state
         _targetWordCount = session.TargetWordCount;
       _nextSearchAfter = session.NextSearchAfter;
    _currentPage = session.CurrentPage;
  _hasMoreConcepts = session.HasMoreConcepts;
                _addedCount = session.AddedCount;
        _ignoredCount = session.IgnoredCount;
       _totalProcessedCount = session.TotalProcessedCount;

    // Restore candidate queue
     _candidateQueue.Clear();
          foreach (var candidate in session.CandidateQueue ?? new List<CandidateDto>())
     {
         var concept = new SnomedConceptWithTerms(
      candidate.ConceptId,
      candidate.ConceptIdStr,
             candidate.Fsn,
         candidate.Pt,
 candidate.Active,
 DateTime.UtcNow,
       Array.Empty<SnomedTerm>()
         );
     var term = new SnomedTerm(candidate.TermText, candidate.TermType);
         _candidateQueue.Enqueue((concept, term));
         }

                // Notify UI of restored values
    OnPropertyChanged(nameof(TargetWordCount));
       OnPropertyChanged(nameof(AddedCount));
       OnPropertyChanged(nameof(IgnoredCount));
     OnPropertyChanged(nameof(TotalProcessedCount));

            StatusMessage = $"Resumed previous session: {_candidateQueue.Count} candidates ready, {TotalProcessedCount} already processed";
            System.Diagnostics.Debug.WriteLine($"[SnomedWordCountImporter] Session restored: page={_currentPage}, queue={_candidateQueue.Count}");
}
            catch (Exception ex)
            {
         System.Diagnostics.Debug.WriteLine($"[SnomedWordCountImporter] Failed to restore session: {ex.Message}");
         StatusMessage = $"Ready to search all SNOMED concepts for {TargetWordCount}-word synonyms";
       }
        }

    public void ClearSession()
        {
            try
         {
      if (File.Exists(SessionFilePath))
         {
        File.Delete(SessionFilePath);
   System.Diagnostics.Debug.WriteLine("[SnomedWordCountImporter] Session cleared");
   }
            }
catch (Exception ex)
      {
      System.Diagnostics.Debug.WriteLine($"[SnomedWordCountImporter] Failed to clear session: {ex.Message}");
            }
   }

        // Session DTOs
      private class ImportSession
        {
   public int TargetWordCount { get; set; }
          public string? NextSearchAfter { get; set; }
            public int CurrentPage { get; set; }
     public bool HasMoreConcepts { get; set; }
        public int AddedCount { get; set; }
  public int IgnoredCount { get; set; }
       public int TotalProcessedCount { get; set; }
   public List<CandidateDto> CandidateQueue { get; set; } = new();
        }

        private class CandidateDto
        {
            public long ConceptId { get; set; }
  public string ConceptIdStr { get; set; } = string.Empty;
       public string Fsn { get; set; } = string.Empty;
     public string? Pt { get; set; }
          public bool Active { get; set; }
        public string TermText { get; set; } = string.Empty;
      public string TermType { get; set; } = string.Empty;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

      private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
  PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
   }
    }
}
