using System;
using System.Windows.Input;

namespace Wysg.Musm.Radium.ViewModels
{
    /// <summary>
    /// Partial: Helper classes for command automation (AutomationSettings, DelegateCommand).
    /// </summary>
    public partial class MainViewModel
    {
        // Internal AutomationSettings class for deserialization
        private sealed class AutomationSettings
        {
            public string? NewStudySequence { get; set; }
            public string? AddStudySequence { get; set; }
            public string? ShortcutOpenNew { get; set; }
            public string? ShortcutOpenAdd { get; set; }
            public string? ShortcutOpenAfterOpen { get; set; }
            public string? SendReportSequence { get; set; }
            public string? SendReportPreviewSequence { get; set; }
            public string? ShortcutSendReportPreview { get; set; }
            public string? ShortcutSendReportReportified { get; set; }
            public string? TestSequence { get; set; }
        }

        // Helper to get automation sequence for current PACS
        private string GetAutomationSequenceForCurrentPacs(Func<AutomationSettings, string?> selector)
        {
            try
            {
                var pacsKey = _tenant?.CurrentPacsKey ?? "default_pacs";
                var automationFile = GetAutomationFilePath(pacsKey);
                
                if (!System.IO.File.Exists(automationFile))
                {
                    System.Diagnostics.Debug.WriteLine($"[GetAutomationSequence] No automation file found at {automationFile}");
                    return string.Empty;
                }
                
                var json = System.IO.File.ReadAllText(automationFile);
                var settings = System.Text.Json.JsonSerializer.Deserialize<AutomationSettings>(json);
                
                if (settings == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[GetAutomationSequence] Failed to deserialize automation settings");
                    return string.Empty;
                }
                
                var sequence = selector(settings);
                System.Diagnostics.Debug.WriteLine($"[GetAutomationSequence] PACS={pacsKey}, Sequence='{sequence}'");
                return sequence ?? string.Empty;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GetAutomationSequence] Error: {ex.Message}");
                return string.Empty;
            }
        }

        private string GetAutomationFilePath(string pacsKey)
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var accountSegment = _tenant?.AccountId > 0 ? _tenant.AccountId.ToString() : "account0";
            var path = System.IO.Path.Combine(appData, "Wysg.Musm", "Radium", "Accounts", accountSegment, "Pacs", SanitizeFileName(pacsKey), "automation.json");
            if (!System.IO.File.Exists(path))
            {
                var legacy = System.IO.Path.Combine(appData, "Wysg.Musm", "Radium", "Pacs", SanitizeFileName(pacsKey), "automation.json");
                if (System.IO.File.Exists(legacy))
                {
                    var dir = System.IO.Path.GetDirectoryName(path);
                    if (!string.IsNullOrEmpty(dir)) System.IO.Directory.CreateDirectory(dir);
                    System.IO.File.Copy(legacy, path, overwrite: true);
                }
            }
            return path;
        }

        private static string SanitizeFileName(string name)
        {
            var invalid = System.IO.Path.GetInvalidFileNameChars();
            return string.Join("_", name.Split(invalid, StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.');
        }

        // DelegateCommand helper class
        private sealed class DelegateCommand : ICommand
        {
            private readonly Action<object?> _execute;
            private readonly Predicate<object?>? _canExecute;

            public DelegateCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
            {
                _execute = execute ?? throw new ArgumentNullException(nameof(execute));
                _canExecute = canExecute;
            }

            public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

            public void Execute(object? parameter) => _execute(parameter);

            public event EventHandler? CanExecuteChanged
            {
                add { CommandManager.RequerySuggested += value; }
                remove { CommandManager.RequerySuggested -= value; }
            }

            public void RaiseCanExecuteChanged() => CommandManager.InvalidateRequerySuggested();
        }
    }
}
