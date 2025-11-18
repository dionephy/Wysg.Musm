using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Diagnostics;
using System.Threading;
using Wysg.Musm.Infrastructure.ViewModels;
using Wysg.Musm.Radium.Services;
using Wysg.Musm.Radium.Services.Procedures;

namespace Wysg.Musm.Radium.ViewModels
{
    /// <summary>
    /// MainViewModel (core partial).
    ///
    /// The original file was very large. It has been split into multiple partial class files for readability
    /// and maintainability. Each partial groups a cohesive concern:
    ///   MainViewModel.cs (this file)          : Core fields, constructor, high-level status helpers
    ///   MainViewModel.CurrentStudy.cs         : Current study metadata fetch & persistence
    ///   MainViewModel.Editor.cs               : Current editor (reportified) text + JSON sync logic
    ///   MainViewModel.PreviousStudies.cs      : Previous studies loading, selection, JSON sync
    ///   MainViewModel.ReportifyHelpers.cs     : Reportify / dereportify helpers & regex utilities
    ///   MainViewModel.Commands.cs             : Commands, delegate command, automation sequences
    ///
    /// Splitting keeps cross-cutting private fields centralized here so other partials only focus on logic.
    /// New developers: start reading from this file, then navigate to the specific concern you need.
    /// </summary>
    public partial class MainViewModel : BaseViewModel
    {
        // ------------------------------------------------------------------
        // Core service dependencies (injected)
        // ------------------------------------------------------------------
        private readonly IPhraseService _phrases;
        private readonly ITenantContext _tenant;
        private readonly IPhraseCache _cache;
        private readonly IHotkeyService _hotkeys; // new: hotkey service for editor completion
        private readonly ISnippetService _snippets; // added
        private readonly PacsService _pacs = new();
        private readonly ICentralDataSourceProvider? _centralProvider; // (reserved for future use)
        private readonly IRadStudyRepository? _studyRepo;
        private readonly INewStudyProcedure? _newStudyProc;
        private readonly IRadiumLocalSettings? _localSettings;
        private readonly ILockStudyProcedure? _lockStudyProc;
        private readonly ISnomedMapService? _snomedMapService; // SNOMED mapping service for semantic tags
        private readonly TextSyncService? _textSyncService; // Text sync service for foreign textbox sync
        private readonly IStudynameLoincRepository? _studynameLoincRepo; // LOINC mapping repository for modality extraction

        // ------------------------------------------------------------------
        // Status / UI flags
        // ------------------------------------------------------------------
        private string _statusText = "Ready"; public string StatusText { get => _statusText; set => SetProperty(ref _statusText, value); }
        private bool _statusIsError; public bool StatusIsError { get => _statusIsError; set => SetProperty(ref _statusIsError, value); }

        // Text sync toggle
        private bool _textSyncEnabled;
        public bool TextSyncEnabled
        {
            get => _textSyncEnabled;
            set
            {
                if (SetProperty(ref _textSyncEnabled, value))
                {
                    _textSyncService?.SetEnabled(value);
                    if (value)
                    {
                        SetStatus("Text sync enabled");
                    }
                    else
                    {
                        // On sync OFF: merge ForeignText into FindingsText and clear, preserving caret position
                        if (!string.IsNullOrEmpty(_foreignText))
                        {
                            // Calculate foreign text length including newline separator
                            int foreignLength = _foreignText.Length + Environment.NewLine.Length;
                            
                            // Merge: FindingsText = ForeignText + newline + FindingsText
                            string merged = _foreignText + Environment.NewLine + FindingsText;
                            FindingsText = merged;
                            
                            // Clear foreign text (both property and bound element)
                            ForeignText = string.Empty;
                            
                            // Write empty string to foreign textbox to clear it, then request focus return to Findings
                            _ = _textSyncService?.WriteToForeignAsync(string.Empty, avoidFocus: true, afterFocusCallback: () =>
                            {
                                // Notify UI to focus Findings editor after foreign element is focused
                                OnPropertyChanged(nameof(RequestFocusFindings));
                            });
                            
                            SetStatus("Text sync disabled - foreign text merged into findings");
                            
                            // Notify EditorFindings to adjust caret: new position = old position + foreign text length
                            OnPropertyChanged(nameof(FindingsCaretOffsetAdjustment));
                            FindingsCaretOffsetAdjustment = foreignLength;
                        }
                        else
                        {
                            SetStatus("Text sync disabled");
                        }
                    }
                }
            }
        }
        
        // Property to communicate focus request to UI (MainWindow listens to property change)
        public bool RequestFocusFindings { get; private set; }
        
        // NEW: Event to communicate focus request for Study Remark textbox (after SetCurrentInMainScreen completes)
        public event EventHandler? RequestFocusStudyRemark;
        
        // Property to communicate caret offset adjustment to EditorFindings
        private int _findingsCaretOffsetAdjustment;
        public int FindingsCaretOffsetAdjustment
        {
            get => _findingsCaretOffsetAdjustment;
            set => SetProperty(ref _findingsCaretOffsetAdjustment, value);
        }

        // Foreign text from external textbox (read-only, synced from foreign source)
        private string _foreignText = string.Empty;
        public string ForeignText
        {
            get => _foreignText;
            private set => SetProperty(ref _foreignText, value);
        }

        // Cumulative status buffer (last 50 lines)
        private readonly object _statusSync = new();
        private readonly Queue<string> _statusLines = new();
        private const int MaxStatusLines = 50;
        private void SetStatus(string message, bool isError = false)
        {
            try
            {
                void update()
                {
                    lock (_statusSync)
                    {
                        // trim to MaxStatusLines - 1 so enqueue keeps <= MaxStatusLines
                        while (_statusLines.Count >= MaxStatusLines) _statusLines.Dequeue();
                        _statusLines.Enqueue(message ?? string.Empty);
                        StatusText = string.Join(Environment.NewLine, _statusLines);
                        StatusIsError = isError;
                    }
                }

                if (System.Windows.Application.Current?.Dispatcher?.CheckAccess() == true) update();
                else System.Windows.Application.Current?.Dispatcher?.Invoke(update);
            }
            catch
            {
                // Fallback to single-line update if dispatcher unavailable
                StatusText = message ?? string.Empty;
                StatusIsError = isError;
            }
        }

        // ------------------------------------------------------------------
        // Current user and PACS display for status bar
        // ------------------------------------------------------------------
        private string _currentUserDisplay = "User: (not logged in)";
        public string CurrentUserDisplay { get => _currentUserDisplay; set => SetProperty(ref _currentUserDisplay, value); }
        
        private string _currentPacsDisplay = "PACS: (not set)";
        public string CurrentPacsDisplay { get => _currentPacsDisplay; set => SetProperty(ref _currentPacsDisplay, value); }

        // ------------------------------------------------------------------
        // Constructor wires commands (definitions in Commands partial) & loads caches in Editor partial.
        // ------------------------------------------------------------------
        public MainViewModel(
            IPhraseService phrases,
            ITenantContext tenant,
            IPhraseCache cache,
            IHotkeyService hotkeys,
            ISnippetService snippets,
            IRadStudyRepository? studyRepo = null,
            INewStudyProcedure? newStudyProc = null,
            IRadiumLocalSettings? localSettings = null,
            ILockStudyProcedure? lockStudyProc = null,
            IAuthStorage? authStorage = null,
            ISnomedMapService? snomedMapService = null,
            IStudynameLoincRepository? studynameLoincRepo = null)
        {
            Debug.WriteLine("[MainViewModel] Constructor START");
            try
            {
                Debug.WriteLine("[MainViewModel] Setting dependencies...");
                _phrases = phrases; _tenant = tenant; _cache = cache; _hotkeys = hotkeys; _snippets = snippets;
                _studyRepo = studyRepo; _newStudyProc = newStudyProc; _localSettings = localSettings; _lockStudyProc = lockStudyProc;
                _snomedMapService = snomedMapService;
                _studynameLoincRepo = studynameLoincRepo;
                
                // ============================================================================
                // CRITICAL: Clear phrase cache on startup to force reload with new filtering
                // ============================================================================
                // FR-completion-filter-2025-01-20: Global phrases are now filtered to ¡Â3 words
                // for completion. Old cached data may contain unfiltered phrases, so we clear
                // the cache on every app start to ensure fresh, filtered data is loaded.
                //
                // Cache versioning (PhraseCache.CACHE_VERSION) also handles this, but explicit
                // clear here ensures no stale data from previous sessions.
                //
                // Performance impact: Negligible (~1ms). Cache repopulates on first completion.
                Debug.WriteLine("[MainViewModel] Clearing phrase cache to force fresh load with filtering...");
                _cache.ClearAll();
                
                // Initialize text sync service
                _textSyncService = new TextSyncService(System.Windows.Application.Current.Dispatcher);
                _textSyncService.ForeignTextChanged += OnForeignTextChanged;
                
                Debug.WriteLine("[MainViewModel] Creating PreviousStudies collection...");
                PreviousStudies = new ObservableCollection<PreviousStudyTab>();
                
                Debug.WriteLine("[MainViewModel] Initializing commands...");
                InitializeCommands(); // implemented in Commands partial
                // Initialize split commands for previous report panel
                try { InitializePreviousSplitCommands(); } catch { }
                
                // Set current user display from auth storage
                try
                {
                    if (authStorage != null && !string.IsNullOrWhiteSpace(authStorage.Email))
                    {
                        CurrentUserDisplay = $"User: {authStorage.Email}";
                    }
                }
                catch { }

                // Initialize current PACS display from tenant context
                try
                {
                    var pacsKey = string.IsNullOrWhiteSpace(_tenant.CurrentPacsKey) ? "default_pacs" : _tenant.CurrentPacsKey;
                    CurrentPacsDisplay = $"PACS: {pacsKey}";
                    _tenant.PacsKeyChanged += (oldKey, newKey) =>
                    {
                        var key = string.IsNullOrWhiteSpace(newKey) ? "default_pacs" : newKey;
                        CurrentPacsDisplay = $"PACS: {key}";
                    };
                }
                catch { }
                
                Debug.WriteLine("[MainViewModel] Setting initialization flag...");
                // Mark as initialized after all construction is complete
                _isInitialized = true;
                
                // Load toggle settings from local settings
                LoadToggleSettings();
                
                Debug.WriteLine("[MainViewModel] Constructor COMPLETE");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MainViewModel] Constructor EXCEPTION: {ex.GetType().Name}");
                Debug.WriteLine($"[MainViewModel] Message: {ex.Message}");
                Debug.WriteLine($"[MainViewModel] StackTrace: {ex.StackTrace}");
                throw;
            }
        }

        private void OnForeignTextChanged(object? sender, string foreignText)
        {
            // Update ForeignText property when foreign textbox changes (one-way sync)
            if (_textSyncEnabled && foreignText != null)
            {
                ForeignText = foreignText;
                Debug.WriteLine($"[TextSync] Updated ForeignText from foreign: {foreignText.Length} chars");
            }
        }

        // ------------------------------------------------------------------
        // Internal wrappers (used by automation / procedures) kept here so they remain visible from other partials
        // ------------------------------------------------------------------
        internal async Task FetchCurrentStudyAsyncInternal() => await FetchCurrentStudyAsync();
        internal void UpdateCurrentStudyLabelInternal() => UpdateCurrentStudyLabel();
        internal void SetStatusInternal(string msg, bool err = false) => SetStatus(msg, err);
    }
}