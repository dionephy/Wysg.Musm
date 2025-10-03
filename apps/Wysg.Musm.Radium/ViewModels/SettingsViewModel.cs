using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Npgsql;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using Wysg.Musm.Radium.Services;
using System.Text.Json;
using System.Collections.Generic;

namespace Wysg.Musm.Radium.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly IRadiumLocalSettings _local;

        [ObservableProperty]
        private string? localConnectionString;

        // Back-compat binding
        public string? ConnectionString
        {
            get => LocalConnectionString;
            set => LocalConnectionString = value;
        }

        [ObservableProperty]
        private ObservableCollection<string> availableModules = new(new[] { "NewStudy", "LockStudy" });
        [ObservableProperty]
        private ObservableCollection<string> newStudyModules = new();
        [ObservableProperty]
        private ObservableCollection<string> addStudyModules = new();

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
        private bool _preserveKnownTokens = true; public bool PreserveKnownTokens { get => _preserveKnownTokens; set { if (SetProperty(ref _preserveKnownTokens, value)) UpdateReportifyJson(); } }

        private string _defaultArrow = "-->"; public string DefaultArrow { get => _defaultArrow; set { if (SetProperty(ref _defaultArrow, value)) UpdateReportifyJson(); } }
        private string _defaultConclusionNumbering = "1."; public string DefaultConclusionNumbering { get => _defaultConclusionNumbering; set { if (SetProperty(ref _defaultConclusionNumbering, value)) UpdateReportifyJson(); } }
        private string _defaultDetailingPrefix = "-"; public string DefaultDetailingPrefix { get => _defaultDetailingPrefix; set { if (SetProperty(ref _defaultDetailingPrefix, value)) UpdateReportifyJson(); } }

        private string _reportifySettingsJson = "{}"; public string ReportifySettingsJson { get => _reportifySettingsJson; private set => SetProperty(ref _reportifySettingsJson, value); }

        private string _sampleBeforeText = string.Empty; public string SampleBeforeText { get => _sampleBeforeText; set => SetProperty(ref _sampleBeforeText, value); }
        private string _sampleAfterText = string.Empty; public string SampleAfterText { get => _sampleAfterText; set => SetProperty(ref _sampleAfterText, value); }
        public IRelayCommand SaveCommand { get; }
        public IRelayCommand TestLocalCommand { get; }
        public IRelayCommand SaveAutomationCommand { get; }
        public IRelayCommand ShowReportifySampleCommand { get; }

        public SettingsViewModel() : this(new RadiumLocalSettings()) { }

        public SettingsViewModel(IRadiumLocalSettings local)
        {
            _local = local;
            LocalConnectionString = _local.LocalConnectionString ?? "Host=127.0.0.1;Port=5432;Database=wysg_dev;Username=postgres;Password=`123qweas";
            SaveCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(Save);
            TestLocalCommand = new AsyncRelayCommand(TestLocalAsync);
            SaveAutomationCommand = new RelayCommand(SaveAutomation);
            ShowReportifySampleCommand = new RelayCommand<string?>(ShowSample);
            UpdateReportifyJson();
        }

        public void LoadAutomation()
        {
            newStudyModules.Clear(); addStudyModules.Clear();
            var ns = _local.AutomationNewStudySequence;
            if (!string.IsNullOrWhiteSpace(ns)) foreach (var m in ns.Split(',', ';')) if (!string.IsNullOrWhiteSpace(m)) newStudyModules.Add(m.Trim());
            var ad = _local.AutomationAddStudySequence;
            if (!string.IsNullOrWhiteSpace(ad)) foreach (var m in ad.Split(',', ';')) if (!string.IsNullOrWhiteSpace(m)) addStudyModules.Add(m.Trim());
        }

        private void Save()
        {
            _local.LocalConnectionString = LocalConnectionString ?? string.Empty;
            MessageBox.Show("Saved.", "Settings", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SaveAutomation()
        {
            _local.AutomationNewStudySequence = string.Join(",", NewStudyModules);
            _local.AutomationAddStudySequence = string.Join(",", AddStudyModules);
            MessageBox.Show("Automation saved.", "Automation", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async Task TestLocalAsync()
        {
            await TestAsync(LocalConnectionString, "Local");
        }

        private static async Task TestAsync(string? cs, string label)
        {
            try
            {
                await using var con = new NpgsqlConnection(cs);
                await con.OpenAsync();
                MessageBox.Show($"{label} connection OK.", "Test", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (System.Exception ex)
            {
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
                preserve_known_tokens = PreserveKnownTokens,
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
            ["preserve_known_tokens"] = ("Hepatic artery normal", "Hepatic artery normal (token 'Hepatic' preserved)")
        };
    }
}
