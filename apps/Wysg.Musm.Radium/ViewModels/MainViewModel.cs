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
        private readonly IRadStudyRepository? _studyRepo;
        private readonly INewStudyProcedure? _newStudyProc;
        private readonly IRadiumLocalSettings? _localSettings;
        private readonly ISetStudyLockedProcedure? _setStudyLockedProc;
        private readonly ISetStudyOpenedProcedure? _setStudyOpenedProc;
        private readonly IClearCurrentFieldsProcedure? _clearCurrentFieldsProc;
        private readonly IClearPreviousFieldsProcedure? _clearPreviousFieldsProc;
        private readonly IClearPreviousStudiesProcedure? _clearPreviousStudiesProc;
        private readonly ISetCurrentStudyTechniquesProcedure? _setCurrentStudyTechniquesProc;
        private readonly IInsertPreviousStudyProcedure? _insertPreviousStudyProc;
        private readonly IInsertCurrentStudyProcedure? _insertCurrentStudyProc;
        private readonly IInsertCurrentStudyReportProcedure? _insertCurrentStudyReportProc;
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
                    // Align external toggle bookmark state before proceeding
                    TrySyncExternalToggleBookmark(value);

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
                        // Add timestamp prefix in format: YYYY-MM-dd HH:mm:ss- (no space after dash)
                        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        var messageWithTimestamp = $"{timestamp}-{message ?? string.Empty}";
                        
                        // trim to MaxStatusLines - 1 so enqueue keeps <= MaxStatusLines
                        while (_statusLines.Count >= MaxStatusLines) _statusLines.Dequeue();
                        _statusLines.Enqueue(messageWithTimestamp);
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
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                StatusText = $"{timestamp}-{message ?? string.Empty}";
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
            ISetStudyLockedProcedure? setStudyLockedProc = null,
            ISetStudyOpenedProcedure? setStudyOpenedProc = null,
            IClearCurrentFieldsProcedure? clearCurrentFieldsProc = null,
            IClearPreviousFieldsProcedure? clearPreviousFieldsProc = null,
            IClearPreviousStudiesProcedure? clearPreviousStudiesProc = null,
            ISetCurrentStudyTechniquesProcedure? setCurrentStudyTechniquesProc = null,
            IInsertPreviousStudyProcedure? insertPreviousStudyProc = null,
            IInsertCurrentStudyProcedure? insertCurrentStudyProc = null,
            IInsertCurrentStudyReportProcedure? insertCurrentStudyReportProc = null,
            IAuthStorage? authStorage = null,
            ISnomedMapService? snomedMapService = null,
            IStudynameLoincRepository? studynameLoincRepo = null)
        {
            Debug.WriteLine("[MainViewModel] Constructor START");
            try
            {
                Debug.WriteLine("[MainViewModel] Setting dependencies...");
                _phrases = phrases; _tenant = tenant; _cache = cache; _hotkeys = hotkeys; _snippets = snippets;
                _studyRepo = studyRepo; _newStudyProc = newStudyProc; _localSettings = localSettings; _setStudyLockedProc = setStudyLockedProc;
                _setStudyOpenedProc = setStudyOpenedProc;
                _clearCurrentFieldsProc = clearCurrentFieldsProc;
                _clearPreviousFieldsProc = clearPreviousFieldsProc;
                _clearPreviousStudiesProc = clearPreviousStudiesProc;
                _setCurrentStudyTechniquesProc = setCurrentStudyTechniquesProc;
                _insertPreviousStudyProc = insertPreviousStudyProc;
                _insertCurrentStudyProc = insertCurrentStudyProc;
                _insertCurrentStudyReportProc = insertCurrentStudyReportProc;
                _snomedMapService = snomedMapService;
                _studynameLoincRepo = studynameLoincRepo;

                // Initialize text sync service with bookmark getter
                _textSyncService = new TextSyncService(System.Windows.Application.Current.Dispatcher, () =>
                {
                    var name = _localSettings?.VoiceToTextTextboxBookmark;
                    return string.IsNullOrWhiteSpace(name) ? "ForeignTextbox" : name;
                });
                _textSyncService.ForeignTextChanged += OnForeignTextChanged;
                
                // Initialize voice-to-text toggle visibility from local settings
                if (_localSettings != null)
                {
                    _voiceToTextEnabled = _localSettings.VoiceToTextEnabled;
                    OnPropertyChanged(nameof(VoiceToTextEnabled));
                }
                
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
        
        /// <summary>
        /// Format a value for status display: truncate to single line and add ellipsis if needed.
        /// Used by automation modules to create concise status messages.
        /// </summary>
        /// <param name="value">The value to format</param>
        /// <param name="maxLength">Maximum length before truncation (default: 80)</param>
        /// <returns>Formatted single-line string</returns>
        internal string FormatValueForStatus(string? value, int maxLength = 80)
        {
            if (string.IsNullOrEmpty(value)) return "(empty)";
            
            // Replace all newlines and multiple spaces with single space
            var singleLine = System.Text.RegularExpressions.Regex.Replace(value, @"\s+", " ").Trim();
            
            // Truncate if needed
            if (singleLine.Length > maxLength)
            {
                return singleLine.Substring(0, maxLength) + " ...";
            }
            
            return singleLine;
        }
        
        private bool _voiceToTextEnabled;
        public bool VoiceToTextEnabled
        {
            get => _voiceToTextEnabled;
            set
            {
                if (SetProperty(ref _voiceToTextEnabled, value))
                {
                    if (!value && TextSyncEnabled)
                    {
                        TextSyncEnabled = false;
                    }
                }
            }
        }

        private void TrySyncExternalToggleBookmark(bool turningOn)
        {
            try
            {
                var toggleBookmark = _localSettings?.VoiceToTextToggleBookmark;
                if (string.IsNullOrWhiteSpace(toggleBookmark)) return;

                var (_, element) = UiBookmarks.Resolve(toggleBookmark);
                if (element == null) return;

                FlaUI.Core.Definitions.ToggleState? state = null;
                if (element.Patterns.Toggle.IsSupported)
                {
                    state = element.Patterns.Toggle.Pattern?.ToggleState?.Value;
                }

                bool isOn = state == FlaUI.Core.Definitions.ToggleState.On;
                bool shouldInvoke;

                if (state == null)
                {
                    // Unknown state: invoke once to align with UI toggle intent
                    shouldInvoke = true;
                }
                else
                {
                    shouldInvoke = turningOn ? !isOn : isOn;
                }

                if (shouldInvoke)
                {
                    if (element.Patterns.Invoke.IsSupported)
                    {
                        element.Patterns.Invoke.Pattern?.Invoke();
                    }
                    else if (element.Patterns.Toggle.IsSupported)
                    {
                        element.Patterns.Toggle.Pattern?.Toggle();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TextSync] External toggle sync failed: {ex.Message}");
            }
        }
    }
}