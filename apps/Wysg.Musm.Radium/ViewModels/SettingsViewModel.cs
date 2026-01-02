using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Npgsql;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using Wysg.Musm.Radium.Services;
using System.Text.Json;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Patterns;

namespace Wysg.Musm.Radium.ViewModels
{
    /// <summary>
    /// Helper class for key type multiselect options in editor autofocus settings.
    /// </summary>
    public class KeyTypeOption : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        public string Name { get => _name; set => SetProperty(ref _name, value); }
        
        private bool _isChecked;
        public bool IsChecked { get => _isChecked; set => SetProperty(ref _isChecked, value); }
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
    
    public partial class SettingsViewModel : ObservableObject
    {
        private const string DefaultHeaderFormatTemplate = "Clinical information: {Chief Complaint}\n- {Patient History Lines}\nTechniques: {Techniques}\nComparison: {Comparison}";
        private const string HardcodedPassword = "`123qweas";

        private readonly IRadiumLocalSettings _local;
        private readonly ITenantRepository? _tenantRepo; // added

        [ObservableProperty]
        private bool isBusy; // used by async commands in PACS profiles partial

        [ObservableProperty]
        private string? localConnectionString;

        [ObservableProperty]
        private string? snowstormBaseUrl; // new: Snowstorm server base URL

        [ObservableProperty]
        private string? apiBaseUrl;

        // Back-compat binding (legacy alias)
        public string? ConnectionString
        {
            get => LocalConnectionString;
            set => LocalConnectionString = value;
        }

        [ObservableProperty]
        private ObservableCollection<string> availableModules = new(new[] { "NewStudy(obs)", "SetStudyLocked(obs)", "SetStudyOpened(obs)", "UnlockStudy(obs)", "SetCurrentTogglesOff(obs)", "AutofillCurrentHeader", "ClearCurrentFields", "ClearPreviousFields", "ClearPreviousStudies", "ClearTempVariables", "SetCurrentStudyTechniques", "GetStudyRemark(obs)", "GetPatientRemark(obs)", "AddPreviousStudy(obs)", "FetchPreviousStudies", "SetComparison", "GetUntilReportDateTime", "GetReportedReport", "OpenStudy(obs)", "MouseClick1", "MouseClick2", "TestInvoke", "ShowTestMessage", "SetCurrentInMainScreen(obs)", "AbortIfWorklistClosed(obs)", "AbortIfPatientNumberNotMatch", "AbortIfStudyDateTimeNotMatch", "OpenWorklist", "ResultsListSetFocus", "SendReport", "Reportify", "Delay", "SaveCurrentStudyToDB", "SavePreviousStudyToDB", "InsertPreviousStudy", "InsertPreviousStudyReport", "InsertCurrentStudy", "InsertCurrentStudyReport", "FocusEditorFindings", "If Modality with Header", "Else If Message is No", "Abort", "End if" });
        [ObservableProperty]
        private ObservableCollection<string> newStudyModules = new();
        [ObservableProperty]
        private ObservableCollection<string> addStudyModules = new();
        [ObservableProperty]
        private ObservableCollection<string> shortcutOpenNewModules = new();
        [ObservableProperty]
        private ObservableCollection<string> sendReportModules = new();
        [ObservableProperty]
        private ObservableCollection<string> sendReportPreviewModules = new();
        [ObservableProperty]
        private ObservableCollection<string> shortcutSendReportPreviewModules = new();
        [ObservableProperty]
        private ObservableCollection<string> shortcutSendReportReportifiedModules = new();

        [ObservableProperty]
        private ObservableCollection<string> testModules = new();
        
        [ObservableProperty]
        private bool voiceToTextEnabled;

        [ObservableProperty]
        private string? voiceToTextTextboxBookmark;

        [ObservableProperty]
        private string? voiceToTextToggleBookmark;

        // Load custom modules into available modules
        private void LoadCustomModulesIntoAvailable()
        {
            try
            {
                var store = Wysg.Musm.Radium.Models.CustomModuleStore.Load();
                foreach (var module in store.Modules)
                {
                    if (!AvailableModules.Contains(module.Name))
                    {
                        AvailableModules.Add(module.Name);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SettingsVM] Error loading custom modules: {ex.Message}");
            }
        }


        // ===== Reportify Settings (manual properties) =====
        private bool _removeExcessiveBlanks = true; public bool RemoveExcessiveBlanks { get => _removeExcessiveBlanks; set { if (SetProperty(ref _removeExcessiveBlanks, value)) UpdateReportifyJson(); } }
        private bool _removeExcessiveBlankLines = true; public bool RemoveExcessiveBlankLines { get => _removeExcessiveBlankLines; set { if (SetProperty(ref _removeExcessiveBlankLines, value)) UpdateReportifyJson(); } }
        private bool _capitalizeSentence = true; public bool CapitalizeSentence { get => _capitalizeSentence; set { if (SetProperty(ref _capitalizeSentence, value)) UpdateReportifyJson(); } }
        private bool _ensureTrailingPeriod = true; public bool EnsureTrailingPeriod { get => _ensureTrailingPeriod; set { if (SetProperty(ref _ensureTrailingPeriod, value)) UpdateReportifyJson(); } }
        // CHANGED: Granular arrow/bullet spacing instead of normalize
        private bool _spaceBeforeArrows = false; public bool SpaceBeforeArrows { get => _spaceBeforeArrows; set { if (SetProperty(ref _spaceBeforeArrows, value)) UpdateReportifyJson(); } }
        private bool _spaceAfterArrows = true; public bool SpaceAfterArrows { get => _spaceAfterArrows; set { if (SetProperty(ref _spaceAfterArrows, value)) UpdateReportifyJson(); } }
        private bool _spaceBeforeBullets = false; public bool SpaceBeforeBullets { get => _spaceBeforeBullets; set { if (SetProperty(ref _spaceBeforeBullets, value)) UpdateReportifyJson(); } }
        private bool _spaceAfterBullets = true; public bool SpaceAfterBullets { get => _spaceAfterBullets; set { if (SetProperty(ref _spaceAfterBullets, value)) UpdateReportifyJson(); } }
        private bool _spaceAfterPunctuation = true; public bool SpaceAfterPunctuation { get => _spaceAfterPunctuation; set { if (SetProperty(ref _spaceAfterPunctuation, value)) UpdateReportifyJson(); } }
        private bool _normalizeParentheses = true; public bool NormalizeParentheses { get => _normalizeParentheses; set { if (SetProperty(ref _normalizeParentheses, value)) UpdateReportifyJson(); } }
        private bool _spaceNumberUnit = true; public bool SpaceNumberUnit { get => _spaceNumberUnit; set { if (SetProperty(ref _spaceNumberUnit, value)) UpdateReportifyJson(); } }
        private bool _collapseWhitespace = true; public bool CollapseWhitespace { get => _collapseWhitespace; set { if (SetProperty(ref _collapseWhitespace, value)) UpdateReportifyJson(); } }
        private bool _numberConclusionParagraphs = true; public bool NumberConclusionParagraphs { get => _numberConclusionParagraphs; set { if (SetProperty(ref _numberConclusionParagraphs, value)) UpdateReportifyJson(); } }
        private bool _indentContinuationLines = true; public bool IndentContinuationLines { get => _indentContinuationLines; set { if (SetProperty(ref _indentContinuationLines, value)) UpdateReportifyJson(); } }
        // NEW: Number each line on one paragraph instead of numbering paragraphs
        private bool _numberConclusionLinesOnOneParagraph = false; public bool NumberConclusionLinesOnOneParagraph { get => _numberConclusionLinesOnOneParagraph; set { if (SetProperty(ref _numberConclusionLinesOnOneParagraph, value)) UpdateReportifyJson(); } }
        // NEW: Capitalize first letter after bullet or conclusion number
        private bool _capitalizeAfterBulletOrNumber = false; public bool CapitalizeAfterBulletOrNumber { get => _capitalizeAfterBulletOrNumber; set { if (SetProperty(ref _capitalizeAfterBulletOrNumber, value)) UpdateReportifyJson(); } }
        // NEW: Consider arrow/bullet as continuation of previous line (not a new numbered sentence)
        private bool _considerArrowBulletContinuation = false; public bool ConsiderArrowBulletContinuation { get => _considerArrowBulletContinuation; set { if (SetProperty(ref _considerArrowBulletContinuation, value)) UpdateReportifyJson(); } }
        // Removed: PreserveKnownTokens (deprecated)

        private string _defaultArrow = "-->;"; public string DefaultArrow { get => _defaultArrow; set { if (SetProperty(ref _defaultArrow, value)) UpdateReportifyJson(); } }
        private string _defaultConclusionNumbering = "1."; public string DefaultConclusionNumbering { get => _defaultConclusionNumbering; set { if (SetProperty(ref _defaultConclusionNumbering, value)) UpdateReportifyJson(); } }
        private string _defaultDetailingPrefix = "-"; public string DefaultDetailingPrefix { get => _defaultDetailingPrefix; set { if (SetProperty(ref _defaultDetailingPrefix, value)) UpdateReportifyJson(); } }
        private string _defaultDifferentialDiagnosis = "DDx:"; public string DefaultDifferentialDiagnosis { get => _defaultDifferentialDiagnosis; set { if (SetProperty(ref _defaultDifferentialDiagnosis, value)) UpdateReportifyJson(); } }

        private string _reportifySettingsJson = "{}"; public string ReportifySettingsJson { get => _reportifySettingsJson; private set => SetProperty(ref _reportifySettingsJson, value); }

        private string _sampleBeforeText = string.Empty; public string SampleBeforeText { get => _sampleBeforeText; set => SetProperty(ref _sampleBeforeText, value); }
        private string _sampleAfterText = string.Empty; public string SampleAfterText { get => _sampleAfterText; set => SetProperty(ref _sampleAfterText, value); }
        public IRelayCommand SaveCommand { get; }
        public IRelayCommand TestLocalCommand { get; }
        public IRelayCommand SaveAutomationCommand { get; }
        public IRelayCommand ShowReportifySampleCommand { get; }
        public IRelayCommand SaveReportifySettingsCommand { get; }
        public IRelayCommand SaveKeyboardCommand { get; }
        public IRelayCommand SaveVoiceToTextCommand { get; }
        public IRelayCommand TestVoiceToTextCommand { get; }
        public IRelayCommand InvokeVoiceToTextToggleCommand { get; }
        public IRelayCommand CheckVoiceToTextToggleCommand { get; }

        // Keyboard (global hotkeys) settings
        private string? _openStudyHotkey;
        public string? OpenStudyHotkey { get => _openStudyHotkey; set => SetProperty(ref _openStudyHotkey, value); }
        private string? _sendStudyHotkey;
        public string? SendStudyHotkey { get => _sendStudyHotkey; set => SetProperty(ref _sendStudyHotkey, value); }
        private string? _toggleSyncTextHotkey;
        public string? ToggleSyncTextHotkey { get => _toggleSyncTextHotkey; set => SetProperty(ref _toggleSyncTextHotkey, value); }
        
        // NEW: Editor autofocus settings
        private bool _editorAutofocusEnabled;
        public bool EditorAutofocusEnabled { get => _editorAutofocusEnabled; set => SetProperty(ref _editorAutofocusEnabled, value); }
        
        private string? _editorAutofocusBookmark;
        public string? EditorAutofocusBookmark { get => _editorAutofocusBookmark; set => SetProperty(ref _editorAutofocusBookmark, value); }
        
        private string _editorAutofocusKeyTypes = string.Empty;
        public string EditorAutofocusKeyTypes { get => _editorAutofocusKeyTypes; set => SetProperty(ref _editorAutofocusKeyTypes, value); }
        
        private string? _editorAutofocusWindowTitle;
        public string? EditorAutofocusWindowTitle { get => _editorAutofocusWindowTitle; set => SetProperty(ref _editorAutofocusWindowTitle, value); }
        
        // NEW: Available UI bookmarks for autofocus target selection
        // Populated from dynamic bookmarks in ui-bookmarks.json instead of hardcoded enum
        public ObservableCollection<string> AvailableBookmarks { get; } = new ObservableCollection<string>();
        
        // NEW: Available key types for autofocus trigger selection (multiselect via checkboxes)
        public ObservableCollection<KeyTypeOption> AvailableKeyTypes { get; } = new ObservableCollection<KeyTypeOption>
        {
            new KeyTypeOption { Name = "Alphabet", IsChecked = false },
            new KeyTypeOption { Name = "Numbers", IsChecked = false },
            new KeyTypeOption { Name = "Space", IsChecked = false },
            new KeyTypeOption { Name = "Tab", IsChecked = false },
            new KeyTypeOption { Name = "Symbols", IsChecked = false }
        };

        private readonly IReportifySettingsService? _reportifySvc;
        private readonly ITenantContext? _tenant;
        public PhrasesViewModel? Phrases { get; }
        public bool IsAccountValid => _tenant?.AccountId > 0;

        public SettingsViewModel() : this(new RadiumLocalSettings()) { }

        public SettingsViewModel(IRadiumLocalSettings local, IReportifySettingsService? reportifySvc = null, ITenantContext? tenant = null, PhrasesViewModel? phrases = null, ITenantRepository? tenantRepo = null)
        {
            _local = local;
            _reportifySvc = reportifySvc; _tenant = tenant; Phrases = phrases; _tenantRepo = tenantRepo;
            // Initialize header template from local (preserve user customization even if central JSON lacks the field)
            HeaderFormatTemplate = _local.HeaderFormatTemplate ?? DefaultHeaderFormatTemplate;

            var persistedLocal = _local.LocalConnectionString ?? "Host=127.0.0.1;Port=5432;Database=wysg_dev;Username=postgres";
            LocalConnectionString = StripPassword(persistedLocal);
            SnowstormBaseUrl = _local.SnowstormBaseUrl ?? "http://127.0.0.1:8080/"; // sensible default
            ApiBaseUrl = _local.ApiBaseUrl ?? "http://127.0.0.1:5205/";
            
            SaveCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(Save);
            TestLocalCommand = new AsyncRelayCommand(TestLocalAsync);
            SaveAutomationCommand = new RelayCommand(SaveAutomation);
            ShowReportifySampleCommand = new RelayCommand<string?>(ShowSample);
            SaveReportifySettingsCommand = new AsyncRelayCommand(SaveReportifySettingsAsync, CanPersistSettings);
            SaveKeyboardCommand = new RelayCommand(SaveKeyboard);
            SaveVoiceToTextCommand = new RelayCommand(SaveVoiceToText);
            TestVoiceToTextCommand = new RelayCommand(TestVoiceToTextBookmark);
            InvokeVoiceToTextToggleCommand = new RelayCommand(InvokeVoiceToTextToggle);
            CheckVoiceToTextToggleCommand = new RelayCommand(CheckVoiceToTextToggle);
            UpdateReportifyJson();
            if (_tenant != null)
            {
                _tenant.AccountIdChanged += (_, _) =>
                {
                    SaveReportifySettingsCommand.NotifyCanExecuteChanged();
                    OnPropertyChanged(nameof(IsAccountValid));
                };
            }
            if (_tenant?.ReportifySettingsJson != null)
            {
                ApplyReportifyJson(_tenant.ReportifySettingsJson);
            }

            // Load keyboard hotkeys from local settings
            OpenStudyHotkey = _local.GlobalHotkeyOpenStudy;
            SendStudyHotkey = _local.GlobalHotkeySendStudy;
            ToggleSyncTextHotkey = _local.GlobalHotkeyToggleSyncText;
            
            // NEW: Load editor autofocus settings from local settings
            EditorAutofocusEnabled = _local.EditorAutofocusEnabled;
            EditorAutofocusBookmark = _local.EditorAutofocusBookmark ?? string.Empty;
            EditorAutofocusWindowTitle = _local.EditorAutofocusWindowTitle ?? string.Empty;
            LoadKeyTypesFromString(_local.EditorAutofocusKeyTypes ?? string.Empty);
            
            // NEW: Load available bookmarks from UiBookmarks
            LoadAvailableBookmarks();
            LoadVoiceToTextSettings();

            // Initialize PACS profile commands and load from DB if repository is available
            InitializePacsProfileCommands();
            if (_tenantRepo != null)
            {
                // Only load PACS profiles if we have a PostgreSQL connection configured
                // (Skip if using API-only mode)
                try
                {
                    _ = LoadPacsProfilesAsync();
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("Central DB is not configured"))
                {
                    System.Diagnostics.Debug.WriteLine("[SettingsVM] Skipping PACS profile load - no PostgreSQL configured (API mode)");
                }
            }
            
            // Load ModalitiesNoHeaderUpdate from local settings (global setting, not PACS-specific)
            ModalitiesNoHeaderUpdate = _local.ModalitiesNoHeaderUpdate ?? string.Empty;
            
            // Load SessionBasedCacheBookmarks from local settings (global setting)
            SessionBasedCacheBookmarks = _local.SessionBasedCacheBookmarks ?? string.Empty;
            
            // Load custom modules into available modules
            LoadCustomModulesIntoAvailable();

            // HeaderFormatTemplate: Load from central settings first (via ApplyReportifyJson above),
            // then fall back to local settings if central is empty
            if (string.IsNullOrEmpty(HeaderFormatTemplate))
            {
                HeaderFormatTemplate = _local.HeaderFormatTemplate ?? DefaultHeaderFormatTemplate;
            }
            System.Diagnostics.Debug.WriteLine($"[SettingsVM] Constructor: Final HeaderFormatTemplate = '{HeaderFormatTemplate}'");
        }
        
        /// <summary>
        /// Load available bookmarks from UiBookmarks into AvailableBookmarks collection.
        /// This populates the dropdown in Settings  Keyboard  Editor Autofocus target.
        /// </summary>
        private void LoadAvailableBookmarks()
        {
            try
            {
                AvailableBookmarks.Clear();
                
                var store = UiBookmarks.Load();
                foreach (var bookmark in store.Bookmarks.OrderBy(b => b.Name))
                {
                    AvailableBookmarks.Add(bookmark.Name);
                }
                
                System.Diagnostics.Debug.WriteLine($"[SettingsVM] Loaded {AvailableBookmarks.Count} bookmarks into AvailableBookmarks");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SettingsVM] Error loading available bookmarks: {ex.Message}");
            }
        }

        private bool CanPersistSettings() => _reportifySvc != null && _tenant != null && _tenant.AccountId > 0;

        private async Task SaveReportifySettingsAsync()
        {
            if (!CanPersistSettings()) return;
            try 
            { 
                System.Diagnostics.Debug.WriteLine($"[SettingsVM] Saving reportify settings to central (len={ReportifySettingsJson.Length})");
                var res = await _reportifySvc!.UpsertAsync(_tenant!.AccountId, ReportifySettingsJson); 
                _tenant.ReportifySettingsJson = res.settingsJson; 
                System.Diagnostics.Debug.WriteLine($"[SettingsVM] Saved reportify settings, rev={res.rev}");
                
                // Force immediate notification to MainViewModel to reload config
                // This ensures the next reportify operation uses the new settings
                System.Diagnostics.Debug.WriteLine($"[SettingsVM] Saved reportify settings, triggering tenant reload");
            }
            catch (System.Exception ex) { System.Diagnostics.Debug.WriteLine("[SettingsVM] Save reportify error: " + ex.Message); }
        }

        private void ApplyReportifyJson(string json)
        {
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                var root = doc.RootElement;
                bool GetBool(string name, bool def) => root.TryGetProperty(name, out var el) && el.ValueKind == System.Text.Json.JsonValueKind.True ? true : (el.ValueKind == System.Text.Json.JsonValueKind.False ? false : def);
                string GetDef(string prop, string def)
                {
                    if (root.TryGetProperty("defaults", out var d) && d.TryGetProperty(prop, out var el) && el.ValueKind == System.Text.Json.JsonValueKind.String) return el.GetString() ?? def;
                    return def;
                }
                string GetStr(string name, string def)
                {
                    if (root.TryGetProperty(name, out var el) && el.ValueKind == System.Text.Json.JsonValueKind.String) return el.GetString() ?? def;
                    return def;
                }
                RemoveExcessiveBlanks = GetBool("remove_excessive_blanks", RemoveExcessiveBlanks);
                RemoveExcessiveBlankLines = GetBool("remove_excessive_blank_lines", RemoveExcessiveBlankLines);
                CapitalizeSentence = GetBool("capitalize_sentence", CapitalizeSentence);
                EnsureTrailingPeriod = GetBool("ensure_trailing_period", EnsureTrailingPeriod);
                // CHANGED: Load new granular settings with backward compatibility
                SpaceBeforeArrows = GetBool("space_before_arrows", SpaceBeforeArrows);
                SpaceAfterArrows = GetBool("space_after_arrows", SpaceAfterArrows);
                SpaceBeforeBullets = GetBool("space_before_bullets", SpaceBeforeBullets);
                SpaceAfterBullets = GetBool("space_after_bullets", SpaceAfterBullets);
                // Backward compatibility: if old "normalize_arrows" or "normalize_bullets" exist, migrate to space_after
                if (root.TryGetProperty("normalize_arrows", out var oldArrows) && !root.TryGetProperty("space_after_arrows", out _))
                    SpaceAfterArrows = oldArrows.ValueKind == System.Text.Json.JsonValueKind.True;
                if (root.TryGetProperty("normalize_bullets", out var oldBullets) && !root.TryGetProperty("space_after_bullets", out _))
                    SpaceAfterBullets = oldBullets.ValueKind == System.Text.Json.JsonValueKind.True;
                SpaceAfterPunctuation = GetBool("space_after_punctuation", SpaceAfterPunctuation);
                NormalizeParentheses = GetBool("normalize_parentheses", NormalizeParentheses);
                SpaceNumberUnit = GetBool("space_number_unit", SpaceNumberUnit);
                CollapseWhitespace = GetBool("collapse_whitespace", CollapseWhitespace);
                NumberConclusionParagraphs = GetBool("number_conclusion_paragraphs", NumberConclusionParagraphs);
                IndentContinuationLines = GetBool("indent_continuation_lines", IndentContinuationLines);
                // NEW: Load the new settings
                NumberConclusionLinesOnOneParagraph = GetBool("number_conclusion_lines_on_one_paragraph", NumberConclusionLinesOnOneParagraph);
                CapitalizeAfterBulletOrNumber = GetBool("capitalize_after_bullet_or_number", CapitalizeAfterBulletOrNumber);
                ConsiderArrowBulletContinuation = GetBool("consider_arrow_bullet_continuation", ConsiderArrowBulletContinuation);
                // NEW: Load header format template from central settings (fallback to current/local/default instead of hardcoded default)
                var headerFallback = HeaderFormatTemplate ?? _local.HeaderFormatTemplate ?? DefaultHeaderFormatTemplate;
                HeaderFormatTemplate = GetStr("header_format_template", headerFallback);
                System.Diagnostics.Debug.WriteLine($"[SettingsVM] ApplyReportifyJson: Loaded HeaderFormatTemplate = '{HeaderFormatTemplate}'");
                // Deprecated: ignore preserve_known_tokens from stored JSON
                DefaultArrow = GetDef("arrow", DefaultArrow);
                DefaultConclusionNumbering = GetDef("conclusion_numbering", DefaultConclusionNumbering);
                DefaultDetailingPrefix = GetDef("detailing_prefix", DefaultDetailingPrefix);
                DefaultDifferentialDiagnosis = GetDef("differential_diagnosis", DefaultDifferentialDiagnosis);
                UpdateReportifyJson();
            }
            catch { }
        }

        public void LoadAutomation()
        {
            newStudyModules.Clear(); addStudyModules.Clear();
            shortcutOpenNewModules.Clear(); sendReportModules.Clear();
            var ns = _local.AutomationNewStudySequence;
            if (!string.IsNullOrWhiteSpace(ns)) foreach (var m in ns.Split(',', ';')) if (!string.IsNullOrWhiteSpace(m)) newStudyModules.Add(m.Trim());
            var ad = _local.AutomationAddStudySequence;
            if (!string.IsNullOrWhiteSpace(ad)) foreach (var m in ad.Split(',', ';')) if (!string.IsNullOrWhiteSpace(m)) addStudyModules.Add(m.Trim());
            var s1 = _local.AutomationShortcutOpenNew;
            if (!string.IsNullOrWhiteSpace(s1)) foreach (var m in s1.Split(',', ';')) if (!string.IsNullOrWhiteSpace(m)) shortcutOpenNewModules.Add(m.Trim());
            var s2 = _local.AutomationShortcutOpenAdd;
            if (!string.IsNullOrWhiteSpace(s2)) foreach (var m in s2.Split(',', ';')) if (!string.IsNullOrWhiteSpace(m)) addStudyModules.Add(m.Trim());
            // Note: SendReport sequence loaded from PACS-scoped automation.json, not local settings
        }

        private void Save()
        {
            _local.LocalConnectionString = AppendPassword(LocalConnectionString);
            _local.SnowstormBaseUrl = SnowstormBaseUrl ?? string.Empty;
            _local.ApiBaseUrl = ApiBaseUrl ?? string.Empty;

            // Save voice-to-text settings with main Save button
            _local.VoiceToTextEnabled = VoiceToTextEnabled;
            _local.VoiceToTextTextboxBookmark = VoiceToTextTextboxBookmark;
            _local.VoiceToTextToggleBookmark = VoiceToTextToggleBookmark;
            try
            {
                if (System.Windows.Application.Current is App app)
                {
                    var mainVm = app.Services.GetService(typeof(MainViewModel)) as MainViewModel;
                    if (mainVm != null)
                    {
                        mainVm.VoiceToTextEnabled = VoiceToTextEnabled;
                    }
                }
            }
            catch { }
            
            // Save ModalitiesNoHeaderUpdate to local settings (global setting)
            _local.ModalitiesNoHeaderUpdate = ModalitiesNoHeaderUpdate ?? string.Empty;
            
            // Save SessionBasedCacheBookmarks to local settings (global setting)
            _local.SessionBasedCacheBookmarks = SessionBasedCacheBookmarks ?? string.Empty;
            
            // CHANGED: Header format template is saved centrally via reportify settings
            // Also keep local copy as fallback
            _local.HeaderFormatTemplate = HeaderFormatTemplate ?? string.Empty;
            
            // Refresh JSON to ensure latest template/flags are included
            UpdateReportifyJson();
            System.Diagnostics.Debug.WriteLine($"[SettingsVM] Save() ReportifySettingsJson payload length={ReportifySettingsJson.Length}");
            
            // Save reportify settings to central DB (includes header_format_template)
            if (CanPersistSettings())
            {
                _ = SaveReportifySettingsAsync();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[SettingsVM] CanPersistSettings=false; central save skipped (header template only stored locally)");
            }
            
            // NEW: notify MainViewModel to refresh header if there is existing content
            try
            {
                if (System.Windows.Application.Current is App app)
                {
                    var svcVm = app.Services.GetService(typeof(MainViewModel)) as MainViewModel;
                    svcVm?.OnHeaderFormatTemplateChanged();
                }
            }
            catch { }
            
            MessageBox.Show("Saved.", "Settings", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SaveAutomation()
        {
            // Persist automation to PACS-scoped file (by current selection)
            SaveAutomationForPacs();
        }

        private void SaveKeyboard()
        {
            _local.GlobalHotkeyOpenStudy = OpenStudyHotkey ?? string.Empty;
            _local.GlobalHotkeySendStudy = SendStudyHotkey ?? string.Empty;
            _local.GlobalHotkeyToggleSyncText = ToggleSyncTextHotkey ?? string.Empty;
            
            // NEW: Save editor autofocus settings
            _local.EditorAutofocusEnabled = EditorAutofocusEnabled;
            _local.EditorAutofocusBookmark = EditorAutofocusBookmark ?? string.Empty;
            _local.EditorAutofocusKeyTypes = GetKeyTypesAsString();
            _local.EditorAutofocusWindowTitle = EditorAutofocusWindowTitle ?? string.Empty;
            
            // NEW: Save voice-to-text settings
            _local.VoiceToTextEnabled = VoiceToTextEnabled;
            _local.VoiceToTextTextboxBookmark = VoiceToTextTextboxBookmark;
            _local.VoiceToTextToggleBookmark = VoiceToTextToggleBookmark;
            
            // NEW: Try to immediately re-register hotkeys without restart
            try
            {
                // Find MainWindow and trigger hotkey re-registration
                var mainWindow = System.Windows.Application.Current.MainWindow as Views.MainWindow;
                if (mainWindow != null)
                {
                    // Call a public method on MainWindow to re-register hotkeys
                    mainWindow.ReregisterGlobalHotkeys();
                    MessageBox.Show("Keyboard hotkeys saved and applied immediately.", "Keyboard", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Keyboard hotkeys saved. Please restart the application to apply changes.", "Keyboard", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SettingsVM] Failed to re-register hotkeys: {ex.Message}");
                MessageBox.Show("Keyboard hotkeys saved, but failed to apply immediately. Please restart the application.", "Keyboard", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        
        /// <summary>
        /// Parses comma-separated key types string and sets checkboxes.
        /// </summary>
        private void LoadKeyTypesFromString(string keyTypes)
        {
            if (string.IsNullOrWhiteSpace(keyTypes))
            {
                // Default: uncheck all
                foreach (var kt in AvailableKeyTypes)
                    kt.IsChecked = false;
                return;
            }
            
            var selectedTypes = keyTypes.Split(',', System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries);
            var selectedSet = new System.Collections.Generic.HashSet<string>(selectedTypes, System.StringComparer.OrdinalIgnoreCase);
            
            foreach (var kt in AvailableKeyTypes)
            {
                kt.IsChecked = selectedSet.Contains(kt.Name);
            }
        }
        
        /// <summary>
        /// Returns comma-separated string of checked key types.
        /// </summary>
        private string GetKeyTypesAsString()
        {
            var checkedTypes = AvailableKeyTypes.Where(kt => kt.IsChecked).Select(kt => kt.Name);
            return string.Join(",", checkedTypes);
        }

        private async Task TestLocalAsync() => await TestAsync(LocalConnectionString, "Local");

        private static async Task TestAsync(string? cs, string label)
         {
            if (string.IsNullOrWhiteSpace(cs))
            {
                MessageBox.Show($"{label} connection string empty.", "Test", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            try
            {
                var effective = AppendPassword(cs);
                await using var con = new NpgsqlConnection(effective);
                var sw = System.Diagnostics.Stopwatch.StartNew();
                await con.OpenAsync();
                await using var cmd = new NpgsqlCommand("SELECT 1", con);
                await cmd.ExecuteScalarAsync();
                sw.Stop();
                MessageBox.Show($"{label} connection OK ({sw.ElapsedMilliseconds} ms).", "Test", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"{label} failed: {ex.Message}", "Test", MessageBoxButton.OK, MessageBoxImage.Error);
            }
         }
        
        private static string StripPassword(string? cs)
        {
            if (string.IsNullOrWhiteSpace(cs)) return string.Empty;
            try
            {
                var builder = new NpgsqlConnectionStringBuilder(cs);
                if (builder.ContainsKey("Password")) builder.Remove("Password");
                if (builder.ContainsKey("Pwd")) builder.Remove("Pwd");
                return builder.ConnectionString;
            }
            catch
            {
                // Fallback: best-effort removal via simple replace
                return cs.Replace("Password=`123qweas", string.Empty, System.StringComparison.OrdinalIgnoreCase)
                         .Replace("Pwd=`123qweas", string.Empty, System.StringComparison.OrdinalIgnoreCase);
            }
        }

        private static string AppendPassword(string? cs)
        {
            var baseCs = cs ?? string.Empty;
            try
            {
                var builder = new NpgsqlConnectionStringBuilder(baseCs)
                {
                    Password = HardcodedPassword
                };
                return builder.ConnectionString;
            }
            catch
            {
                return string.IsNullOrWhiteSpace(baseCs)
                    ? $"Password={HardcodedPassword}"
                    : (baseCs.Contains("Password=", System.StringComparison.OrdinalIgnoreCase) ? baseCs : baseCs.TrimEnd(';') + $";Password={HardcodedPassword}");
            }
        }
     
         private void UpdateReportifyJson()
         {
             var obj = new
             {
                remove_excessive_blanks = RemoveExcessiveBlanks,
                remove_excessive_blank_lines = RemoveExcessiveBlankLines,
                capitalize_sentence = CapitalizeSentence,
                ensure_trailing_period = EnsureTrailingPeriod,
                // CHANGED: Use new granular settings
                space_before_arrows = SpaceBeforeArrows,
                space_after_arrows = SpaceAfterArrows,
                space_before_bullets = SpaceBeforeBullets,
                space_after_bullets = SpaceAfterBullets,
                space_after_punctuation = SpaceAfterPunctuation,
                normalize_parentheses = NormalizeParentheses,
                space_number_unit = SpaceNumberUnit,
                collapse_whitespace = CollapseWhitespace,
                number_conclusion_paragraphs = NumberConclusionParagraphs,
                indent_continuation_lines = IndentContinuationLines,
                // NEW: Include the new settings
                number_conclusion_lines_on_one_paragraph = NumberConclusionLinesOnOneParagraph,
                capitalize_after_bullet_or_number = CapitalizeAfterBulletOrNumber,
                consider_arrow_bullet_continuation = ConsiderArrowBulletContinuation,
                // NEW: Include header format template for central persistence
                header_format_template = HeaderFormatTemplate,
                defaults = new
                {
                    arrow = DefaultArrow,
                    conclusion_numbering = DefaultConclusionNumbering,
                    detailing_prefix = DefaultDetailingPrefix,
                    differential_diagnosis = DefaultDifferentialDiagnosis
                }
             };
             ReportifySettingsJson = JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true });
         }

        private void ShowSample(string? key)
        {
            key = key?.ToLowerInvariant();
            var samples = GetSamples();
            if (key != null && samples.TryGetValue(key, out var tup))
            {
                SampleBeforeText = tup.before;
                SampleAfterText = tup.after;
            }
            else
            {
                SampleBeforeText = "(no sample)";
                SampleAfterText = "(no sample)";
            }
        }

        private static Dictionary<string,(string before,String after)> GetSamples() => new()
        {
            ["remove_excessive_blanks"] = ("Liver  size  is  normal", "Liver size is normal"),
            ["remove_excessive_blank_lines"] = ("Line1\n\n\nLine2", "Line1\n\nLine2"),
            ["capitalize_sentence"] = ("multiple hypodense lesions", "Multiple hypodense lesions"),
            ["ensure_trailing_period"] = ("Gall bladder unremarkable", "Gall bladder unremarkable."),
            ["space_before_arrows"] = ("Finding-->recommend", "Finding --> recommend"),
            ["space_after_arrows"] = ("-->Finding", "--> Finding"),
            ["space_before_bullets"] = ("Finding-mass", "Finding - mass"),
            ["space_after_bullets"] = ("-mass\n*calcification", "- mass\n- calcification"),
            ["space_after_punctuation"] = ("Size:10mm;Shape:round", "Size: 10 mm; Shape: round"),
            ["normalize_parentheses"] = ("( left lobe ) normal", "(left lobe) normal"),
            ["space_number_unit"] = ("Measured 10mm lesion", "Measured 10 mm lesion"),
            ["collapse_whitespace"] = ("Kidney   cortex   smooth", "Kidney cortex smooth"),
            ["number_conclusion_paragraphs"] = ("Para A\n\nPara B", "1. Para A\n\n2. Para B"),
            ["indent_continuation_lines"] = ("1. First line\nSecond line", "1. First line\n   Second line"),
            ["number_conclusion_lines_on_one_paragraph"] = ("apple\nbanana\n\nmelon", "1. Apple.\n   Banana.\n\n2. Melon."),
            ["capitalize_after_bullet_or_number"] = ("1. apple\n2. banana", "1. Apple\n2. Banana"),
        };
        
        // Voice-to-text integration load/persist helpers
        partial void OnVoiceToTextEnabledChanged(bool value)
        {
            _local.VoiceToTextEnabled = value;
        }

        partial void OnVoiceToTextTextboxBookmarkChanged(string? value)
        {
            _local.VoiceToTextTextboxBookmark = value;
        }

        partial void OnVoiceToTextToggleBookmarkChanged(string? value)
        {
            _local.VoiceToTextToggleBookmark = value;
        }

        private void LoadVoiceToTextSettings()
        {
            VoiceToTextEnabled = _local.VoiceToTextEnabled;
            VoiceToTextTextboxBookmark = _local.VoiceToTextTextboxBookmark;
            VoiceToTextToggleBookmark = _local.VoiceToTextToggleBookmark;
        }

        private void SaveVoiceToText()
        {
            _local.VoiceToTextEnabled = VoiceToTextEnabled;
            _local.VoiceToTextTextboxBookmark = VoiceToTextTextboxBookmark;
            _local.VoiceToTextToggleBookmark = VoiceToTextToggleBookmark;
            
            // Notify main view model so UI reflects updated visibility immediately
            try
            {
                if (System.Windows.Application.Current is App app)
                {
                    var mainVm = app.Services.GetService(typeof(MainViewModel)) as MainViewModel;
                    if (mainVm != null)
                    {
                        mainVm.VoiceToTextEnabled = VoiceToTextEnabled;
                    }
                }
            }
            catch { }
            
            MessageBox.Show("Voice to text settings saved for current user.", "Voice to text", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void TestVoiceToTextBookmark()
        {
            var name = VoiceToTextTextboxBookmark;
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Select a textbox bookmark first.", "Voice to text", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var (_, element) = UiBookmarks.Resolve(name);
            if (element == null)
            {
                MessageBox.Show($"Bookmark '{name}' not found.", "Voice to text", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            string text = element.Patterns.Value.IsSupported ? element.Patterns.Value.Pattern?.Value ?? string.Empty : element.Name ?? string.Empty;
            MessageBox.Show(string.IsNullOrEmpty(text) ? "(empty)" : text, "Voice to text: textbox value", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void InvokeVoiceToTextToggle()
        {
            var name = VoiceToTextToggleBookmark;
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Select a toggle button bookmark first.", "Voice to text", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var (_, element) = UiBookmarks.Resolve(name);
            if (element == null)
            {
                MessageBox.Show($"Bookmark '{name}' not found.", "Voice to text", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (element.Patterns.Invoke.IsSupported)
            {
                element.Patterns.Invoke.Pattern?.Invoke();
            }
            else
            {
                MessageBox.Show("Toggle bookmark does not support Invoke pattern.", "Voice to text", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CheckVoiceToTextToggle()
        {
            var name = VoiceToTextToggleBookmark;
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Select a toggle button bookmark first.", "Voice to text", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var (_, element) = UiBookmarks.Resolve(name);
            if (element == null)
            {
                MessageBox.Show($"Bookmark '{name}' not found.", "Voice to text", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (element.Patterns.Toggle.IsSupported)
            {
                var togglePattern = element.Patterns.Toggle.Pattern;
                var stateValue = togglePattern?.ToggleState?.Value;
                var stateText = stateValue switch
                {
                    FlaUI.Core.Definitions.ToggleState.Off => "Off",
                    FlaUI.Core.Definitions.ToggleState.On => "On",
                    FlaUI.Core.Definitions.ToggleState.Indeterminate => "Indeterminate",
                    _ => "Unknown"
                };
                MessageBox.Show($"Toggle state: {stateText}", "Voice to text", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Toggle bookmark does not support Toggle pattern.", "Voice to text", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
