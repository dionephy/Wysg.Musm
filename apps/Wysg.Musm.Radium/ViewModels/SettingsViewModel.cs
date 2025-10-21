using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Npgsql;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using Wysg.Musm.Radium.Services;
using System.Text.Json;
using System.Collections.Generic;
using Microsoft.Data.SqlClient; // added for Azure SQL

namespace Wysg.Musm.Radium.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly IRadiumLocalSettings _local;
        private readonly ITenantRepository? _tenantRepo; // added

        [ObservableProperty]
        private bool isBusy; // used by async commands in PACS profiles partial

        // User-requested default (note: quoted Authentication as provided)
        private const string DefaultCentralAzureSqlConnection = "Server=tcp:musm-server.database.windows.net,1433;Initial Catalog=musmdb;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;Authentication=\"Active Directory Default\";";

        [ObservableProperty]
        private string? localConnectionString;

        [ObservableProperty]
        private string? centralConnectionString;

        [ObservableProperty]
        private string? snowstormBaseUrl; // new: Snowstorm server base URL

        // Back-compat binding
        // New: editable central (Azure SQL) connection string (passwordless AAD)
        public string? ConnectionString
        {
            get => LocalConnectionString;
            set => LocalConnectionString = value;
        }

        [ObservableProperty]
        private ObservableCollection<string> availableModules = new(new[] { "NewStudy", "LockStudy", "UnlockStudy", "GetStudyRemark", "GetPatientRemark", "AddPreviousStudy", "OpenStudy", "MouseClick1", "MouseClick2", "TestInvoke", "ShowTestMessage", "SetCurrentInMainScreen", "AbortIfWorklistClosed", "AbortIfPatientNumberNotMatch", "AbortIfStudyDateTimeNotMatch", "OpenWorklist", "ResultsListSetFocus", "SendReport", "Reportify", "Delay" });
        [ObservableProperty]
        private ObservableCollection<string> newStudyModules = new();
        [ObservableProperty]
        private ObservableCollection<string> addStudyModules = new();
        [ObservableProperty]
        private ObservableCollection<string> shortcutOpenNewModules = new();
        [ObservableProperty]
        private ObservableCollection<string> shortcutOpenAddModules = new();
        [ObservableProperty]
        private ObservableCollection<string> shortcutOpenAfterOpenModules = new();
        [ObservableProperty]
        private ObservableCollection<string> sendReportModules = new();
        [ObservableProperty]
        private ObservableCollection<string> sendReportPreviewModules = new();
        [ObservableProperty]
        private ObservableCollection<string> shortcutSendReportPreviewModules = new();
        [ObservableProperty]
        private ObservableCollection<string> shortcutSendReportReportifiedModules = new();

        // ===== Reportify Settings (manual properties) =====
        private bool _removeExcessiveBlanks = true; public bool RemoveExcessiveBlanks { get => _removeExcessiveBlanks; set { if (SetProperty(ref _removeExcessiveBlanks, value)) UpdateReportifyJson(); } }
        private bool _removeExcessiveBlankLines = true; public bool RemoveExcessiveBlankLines { get => _removeExcessiveBlankLines; set { if (SetProperty(ref _removeExcessiveBlankLines, value)) UpdateReportifyJson(); } }
        private bool _capitalizeSentence = true; public bool CapitalizeSentence { get => _capitalizeSentence; set { if (SetProperty(ref _capitalizeSentence, value)) UpdateReportifyJson(); } }
        private bool _ensureTrailingPeriod = true; public bool EnsureTrailingPeriod { get => _ensureTrailingPeriod; set { if (SetProperty(ref _ensureTrailingPeriod, value)) UpdateReportifyJson(); } }
        private bool _normalizeArrows = true; public bool NormalizeArrows { get => _normalizeArrows; set { if (SetProperty(ref _normalizeArrows, value)) UpdateReportifyJson(); } }
        private bool _normalizeBullets = true; public bool NormalizeBullets { get => _normalizeBullets; set { if (SetProperty(ref _normalizeBullets, value)) UpdateReportifyJson(); } }
        private bool _spaceAfterPunctuation = true; public bool SpaceAfterPunctuation { get => _spaceAfterPunctuation; set { if (SetProperty(ref _spaceAfterPunctuation, value)) UpdateReportifyJson(); } }
        private bool _normalizeParentheses = true; public bool NormalizeParentheses { get => _normalizeParentheses; set { if (SetProperty(ref _normalizeParentheses, value)) UpdateReportifyJson(); } }
        private bool _spaceNumberUnit = true; public bool SpaceNumberUnit { get => _spaceNumberUnit; set { if (SetProperty(ref _spaceNumberUnit, value)) UpdateReportifyJson(); } }
        private bool _collapseWhitespace = true; public bool CollapseWhitespace { get => _collapseWhitespace; set { if (SetProperty(ref _collapseWhitespace, value)) UpdateReportifyJson(); } }
        private bool _numberConclusionParagraphs = true; public bool NumberConclusionParagraphs { get => _numberConclusionParagraphs; set { if (SetProperty(ref _numberConclusionParagraphs, value)) UpdateReportifyJson(); } }
        private bool _indentContinuationLines = true; public bool IndentContinuationLines { get => _indentContinuationLines; set { if (SetProperty(ref _indentContinuationLines, value)) UpdateReportifyJson(); } }
        // Removed: PreserveKnownTokens (deprecated)

        private string _defaultArrow = "-->"; public string DefaultArrow { get => _defaultArrow; set { if (SetProperty(ref _defaultArrow, value)) UpdateReportifyJson(); } }
        private string _defaultConclusionNumbering = "1."; public string DefaultConclusionNumbering { get => _defaultConclusionNumbering; set { if (SetProperty(ref _defaultConclusionNumbering, value)) UpdateReportifyJson(); } }
        private string _defaultDetailingPrefix = "-"; public string DefaultDetailingPrefix { get => _defaultDetailingPrefix; set { if (SetProperty(ref _defaultDetailingPrefix, value)) UpdateReportifyJson(); } }

        private string _reportifySettingsJson = "{}"; public string ReportifySettingsJson { get => _reportifySettingsJson; private set => SetProperty(ref _reportifySettingsJson, value); }

        private string _sampleBeforeText = string.Empty; public string SampleBeforeText { get => _sampleBeforeText; set => SetProperty(ref _sampleBeforeText, value); }
        private string _sampleAfterText = string.Empty; public string SampleAfterText { get => _sampleAfterText; set => SetProperty(ref _sampleAfterText, value); }
        public IRelayCommand SaveCommand { get; }
        public IRelayCommand TestLocalCommand { get; }
        public IRelayCommand TestCentralCommand { get; } // new
        public IRelayCommand SaveAutomationCommand { get; }
        public IRelayCommand ShowReportifySampleCommand { get; }
        public IRelayCommand SaveReportifySettingsCommand { get; }
        public IRelayCommand SaveKeyboardCommand { get; }

        // Keyboard (global hotkeys) settings
        private string? _openStudyHotkey;
        public string? OpenStudyHotkey { get => _openStudyHotkey; set => SetProperty(ref _openStudyHotkey, value); }
        private string? _sendStudyHotkey;
        public string? SendStudyHotkey { get => _sendStudyHotkey; set => SetProperty(ref _sendStudyHotkey, value); }
        private string? _toggleSyncTextHotkey;
        public string? ToggleSyncTextHotkey { get => _toggleSyncTextHotkey; set => SetProperty(ref _toggleSyncTextHotkey, value); }

        private readonly IReportifySettingsService? _reportifySvc;
        private readonly ITenantContext? _tenant;
        public PhrasesViewModel? Phrases { get; }
        public bool IsAccountValid => _tenant?.AccountId > 0;

        public SettingsViewModel() : this(new RadiumLocalSettings()) { }

        public SettingsViewModel(IRadiumLocalSettings local, IReportifySettingsService? reportifySvc = null, ITenantContext? tenant = null, PhrasesViewModel? phrases = null, ITenantRepository? tenantRepo = null)
        {
            _local = local;
            _reportifySvc = reportifySvc; _tenant = tenant; Phrases = phrases; _tenantRepo = tenantRepo;
            LocalConnectionString = _local.LocalConnectionString ?? "Host=127.0.0.1;Port=5432;Database=wysg_dev;Username=postgres;Password=`123qweas";
            // Initialize central connection string with provided default if none persisted
            CentralConnectionString = string.IsNullOrWhiteSpace(_local.CentralConnectionString) ? DefaultCentralAzureSqlConnection: _local.CentralConnectionString;
            SnowstormBaseUrl = _local.SnowstormBaseUrl ?? "https://snowstorm.ihtsdotools.org/snowstorm/snomed-ct"; // sensible default
            SaveCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(Save);
            TestLocalCommand = new AsyncRelayCommand(TestLocalAsync);
            TestCentralCommand = new AsyncRelayCommand(TestCentralAsync); // new
            SaveAutomationCommand = new RelayCommand(SaveAutomation);
            ShowReportifySampleCommand = new RelayCommand<string?>(ShowSample);
            SaveReportifySettingsCommand = new AsyncRelayCommand(SaveReportifySettingsAsync, CanPersistSettings);
            SaveKeyboardCommand = new RelayCommand(SaveKeyboard);
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

            // Initialize PACS profile commands and load from DB if repository is available
            InitializePacsProfileCommands();
            if (_tenantRepo != null)
            {
                _ = LoadPacsProfilesAsync();
            }
        }

        private bool CanPersistSettings() => _reportifySvc != null && _tenant != null && _tenant.AccountId > 0;

        private async Task SaveReportifySettingsAsync()
        {
            if (!CanPersistSettings()) return;
            try { var res = await _reportifySvc!.UpsertAsync(_tenant!.AccountId, ReportifySettingsJson); _tenant.ReportifySettingsJson = res.settingsJson; }
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
                RemoveExcessiveBlanks = GetBool("remove_excessive_blanks", RemoveExcessiveBlanks);
                RemoveExcessiveBlankLines = GetBool("remove_excessive_blank_lines", RemoveExcessiveBlankLines);
                CapitalizeSentence = GetBool("capitalize_sentence", CapitalizeSentence);
                EnsureTrailingPeriod = GetBool("ensure_trailing_period", EnsureTrailingPeriod);
                NormalizeArrows = GetBool("normalize_arrows", NormalizeArrows);
                NormalizeBullets = GetBool("normalize_bullets", NormalizeBullets);
                SpaceAfterPunctuation = GetBool("space_after_punctuation", SpaceAfterPunctuation);
                NormalizeParentheses = GetBool("normalize_parentheses", NormalizeParentheses);
                SpaceNumberUnit = GetBool("space_number_unit", SpaceNumberUnit);
                CollapseWhitespace = GetBool("collapse_whitespace", CollapseWhitespace);
                NumberConclusionParagraphs = GetBool("number_conclusion_paragraphs", NumberConclusionParagraphs);
                IndentContinuationLines = GetBool("indent_continuation_lines", IndentContinuationLines);
                // Deprecated: ignore preserve_known_tokens from stored JSON
                DefaultArrow = GetDef("arrow", DefaultArrow);
                DefaultConclusionNumbering = GetDef("conclusion_numbering", DefaultConclusionNumbering);
                DefaultDetailingPrefix = GetDef("detailing_prefix", DefaultDetailingPrefix);
                UpdateReportifyJson();
            }
            catch { }
        }

        public void LoadAutomation()
        {
            newStudyModules.Clear(); addStudyModules.Clear();
            shortcutOpenNewModules.Clear(); shortcutOpenAddModules.Clear(); shortcutOpenAfterOpenModules.Clear(); sendReportModules.Clear();
            var ns = _local.AutomationNewStudySequence;
            if (!string.IsNullOrWhiteSpace(ns)) foreach (var m in ns.Split(',', ';')) if (!string.IsNullOrWhiteSpace(m)) newStudyModules.Add(m.Trim());
            var ad = _local.AutomationAddStudySequence;
            if (!string.IsNullOrWhiteSpace(ad)) foreach (var m in ad.Split(',', ';')) if (!string.IsNullOrWhiteSpace(m)) addStudyModules.Add(m.Trim());
            var s1 = _local.AutomationShortcutOpenNew;
            if (!string.IsNullOrWhiteSpace(s1)) foreach (var m in s1.Split(',', ';')) if (!string.IsNullOrWhiteSpace(m)) shortcutOpenNewModules.Add(m.Trim());
            var s2 = _local.AutomationShortcutOpenAdd;
            if (!string.IsNullOrWhiteSpace(s2)) foreach (var m in s2.Split(',', ';')) if (!string.IsNullOrWhiteSpace(m)) shortcutOpenAddModules.Add(m.Trim());
            var s3 = _local.AutomationShortcutOpenAfterOpen;
            if (!string.IsNullOrWhiteSpace(s3)) foreach (var m in s3.Split(',', ';')) if (!string.IsNullOrWhiteSpace(m)) shortcutOpenAfterOpenModules.Add(m.Trim());
            // Note: SendReport sequence loaded from PACS-scoped automation.json, not local settings
        }

        private void Save()
        {
            _local.LocalConnectionString = LocalConnectionString ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(CentralConnectionString))
                _local.CentralConnectionString = CentralConnectionString!.Trim();
            _local.SnowstormBaseUrl = SnowstormBaseUrl ?? string.Empty;
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
            MessageBox.Show("Keyboard hotkeys saved.", "Keyboard", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async Task TestLocalAsync() => await TestAsync(LocalConnectionString, "Local");
        private async Task TestCentralAsync()
        {
            // Prefer unsaved typed value if present
            var cs = !string.IsNullOrWhiteSpace(CentralConnectionString) ? CentralConnectionString : _local.CentralConnectionString;
            await TestAsync(cs, "Central");
        }

        private static async Task TestAsync(string? cs, string label)
        {
            if (string.IsNullOrWhiteSpace(cs))
            {
                MessageBox.Show($"{label} connection string empty.", "Test", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            bool isAzureSql = cs.IndexOf("database.windows.net", System.StringComparison.OrdinalIgnoreCase) >= 0 || cs.Contains("Initial Catalog=");
            try
            {
                if (isAzureSql)
                {
                    await using var con = new SqlConnection(cs);
                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    await con.OpenAsync();
                    await using var cmd = new SqlCommand("SELECT TOP (1) name FROM sys.tables", con);
                    await cmd.ExecuteScalarAsync();
                    sw.Stop();
                    MessageBox.Show($"{label} Azure SQL OK ({sw.ElapsedMilliseconds} ms).", "Test", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    await using var con = new NpgsqlConnection(cs);
                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    await con.OpenAsync();
                    await using var cmd = new NpgsqlCommand("SELECT 1", con);
                    await cmd.ExecuteScalarAsync();
                    sw.Stop();
                    MessageBox.Show($"{label} Postgres OK ({sw.ElapsedMilliseconds} ms).", "Test", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (System.Exception ex)
            {
                // Azure AD passwordless fallback: try interactive if default failed
                if (isAzureSql && cs.Contains("Authentication=Active Directory Default", System.StringComparison.OrdinalIgnoreCase)
                    && (ex.Message.IndexOf("DefaultAzureCredential", System.StringComparison.OrdinalIgnoreCase) >= 0
                        || ex.Message.IndexOf("failed to retrieve a token", System.StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    try
                    {
                        var interactiveCs = cs.Replace("Authentication=Active Directory Default", "Authentication=Active Directory Interactive", System.StringComparison.OrdinalIgnoreCase);
                        await using var conI = new SqlConnection(interactiveCs);
                        var swI = System.Diagnostics.Stopwatch.StartNew();
                        await conI.OpenAsync(); // will show interactive login dialog
                        await using var cmdI = new SqlCommand("SELECT TOP (1) name FROM sys.tables", conI);
                        await cmdI.ExecuteScalarAsync();
                        swI.Stop();
                        MessageBox.Show($"{label} Azure SQL OK via Interactive ({swI.ElapsedMilliseconds} ms). Revert to Default after successful token acquisition.", "Test", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                    catch (System.Exception ex2)
                    {
                        MessageBox.Show($"{label} interactive fallback failed: {ex2.Message}", "Test", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
                MessageBox.Show($"{label} failed: {ex.Message}", "Test", MessageBoxButton.OK, MessageBoxImage.Error);
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
                normalize_arrows = NormalizeArrows,
                normalize_bullets = NormalizeBullets,
                space_after_punctuation = SpaceAfterPunctuation,
                normalize_parentheses = NormalizeParentheses,
                space_number_unit = SpaceNumberUnit,
                collapse_whitespace = CollapseWhitespace,
                number_conclusion_paragraphs = NumberConclusionParagraphs,
                indent_continuation_lines = IndentContinuationLines,
                // preserve_known_tokens removed (deprecated)
                defaults = new
                {
                    arrow = DefaultArrow,
                    conclusion_numbering = DefaultConclusionNumbering,
                    detailing_prefix = DefaultDetailingPrefix
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

        private static Dictionary<string,(string before,string after)> GetSamples() => new()
        {
            ["remove_excessive_blanks"] = ("Liver  size  is  normal", "Liver size is normal"),
            ["remove_excessive_blank_lines"] = ("Line1\n\n\nLine2", "Line1\n\nLine2"),
            ["capitalize_sentence"] = ("multiple hypodense lesions", "Multiple hypodense lesions"),
            ["ensure_trailing_period"] = ("Gall bladder unremarkable", "Gall bladder unremarkable."),
            ["normalize_arrows"] = ("-->Finding", "--> Finding"),
            ["normalize_bullets"] = ("-mass\n*calcification", "- mass\n- calcification"),
            ["space_after_punctuation"] = ("Size:10mm;Shape:round", "Size: 10 mm; Shape: round"),
            ["normalize_parentheses"] = ("( left lobe ) normal", "(left lobe) normal"),
            ["space_number_unit"] = ("Measured 10mm lesion", "Measured 10 mm lesion"),
            ["collapse_whitespace"] = ("Kidney   cortex   smooth", "Kidney cortex smooth"),
            ["number_conclusion_paragraphs"] = ("Para A\n\nPara B", "1. Para A\n\n2. Para B"),
            ["indent_continuation_lines"] = ("1. First line\nSecond line", "1. First line\n   Second line"),
            // preserve_known_tokens sample removed
        };
    }
}
