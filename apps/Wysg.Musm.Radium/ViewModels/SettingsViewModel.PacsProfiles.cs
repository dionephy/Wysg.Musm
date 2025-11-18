using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Diagnostics;
using Wysg.Musm.Radium.Models;
using Wysg.Musm.Radium.Services;

namespace Wysg.Musm.Radium.ViewModels
{
    public partial class SettingsViewModel
    {
        private string? _selectedPacsForAutomation;

        [ObservableProperty]
        private ObservableCollection<PacsProfileItem> pacsProfiles = new();

        [ObservableProperty]
        private PacsProfileItem? selectedPacsProfile;
        
        public string? SelectedPacsForAutomation
        {
            get => _selectedPacsForAutomation;
            set
            {
                if (SetProperty(ref _selectedPacsForAutomation, value))
                {
                    LoadAutomationForPacs(value);
                }
            }
        }

        public IRelayCommand AddPacsProfileCommand { get; private set; } = null!;
        public IRelayCommand<PacsProfileItem> RenamePacsProfileCommand { get; private set; } = null!;
        public IRelayCommand<PacsProfileItem> RemovePacsProfileCommand { get; private set; } = null!;

        public class PacsProfileItem
        {
            public long TenantId { get; set; }
            public string PacsKey { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string ProcessName { get; set; } = "INFINITT";
            public DateTime CreatedAt { get; set; }
            public DateTime UpdatedAt { get; set; }
        }

        partial void OnSelectedPacsProfileChanged(PacsProfileItem? oldValue, PacsProfileItem? newValue)
        {
            if (newValue != null && _tenant != null)
            {
                Debug.WriteLine($"[SettingsVM] Switching PACS from '{oldValue?.PacsKey}' to '{newValue.PacsKey}'");
                _tenant.CurrentPacsKey = newValue.PacsKey;

                var pacsKey = string.IsNullOrWhiteSpace(newValue.PacsKey) ? "default_pacs" : newValue.PacsKey;
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var baseDir = System.IO.Path.Combine(appData, "Wysg.Musm", "Radium", "Pacs", SanitizeFileName(pacsKey));
                System.IO.Directory.CreateDirectory(baseDir);
                var spyPath = System.IO.Path.Combine(baseDir, "ui-procedures.json");
                ProcedureExecutor.SetProcPathOverride(() => spyPath);
                var bookmarksPath = System.IO.Path.Combine(baseDir, "bookmarks.json");
                UiBookmarks.GetStorePathOverride = () => bookmarksPath;

                // Immediately switch Automation tab to the selected PACS
                SelectedPacsForAutomation = pacsKey;
            }
        }

        private void InitializePacsProfileCommands()
        {
            AddPacsProfileCommand = new AsyncRelayCommand(OnAddPacsProfileAsync);
            RenamePacsProfileCommand = new AsyncRelayCommand<PacsProfileItem>(OnRenamePacsProfileAsync);
            RemovePacsProfileCommand = new AsyncRelayCommand<PacsProfileItem>(OnRemovePacsProfileAsync, p => PacsProfiles.Count > 1);
        }

        public async Task LoadPacsProfilesAsync()
        {
            if (_tenant == null || _tenant.AccountId <= 0)
            {
                Debug.WriteLine("[SettingsVM] Cannot load PACS profiles: no valid account");
                return;
            }

            try
            {
                Debug.WriteLine($"[SettingsVM] Loading PACS profiles for account={_tenant.AccountId}");

                var tenants = await _tenantRepo!.GetTenantsForAccountAsync(_tenant.AccountId);

                PacsProfiles.Clear();
                foreach (var tenant in tenants)
                {
                    PacsProfiles.Add(new PacsProfileItem
                    {
                        TenantId = tenant.Id,
                        PacsKey = tenant.PacsKey,
                        Name = tenant.PacsKey,
                        ProcessName = "INFINITT",
                        CreatedAt = tenant.CreatedAt,
                        UpdatedAt = tenant.CreatedAt
                    });
                }

                Debug.WriteLine($"[SettingsVM] Loaded {PacsProfiles.Count} PACS profiles");

                var currentPacsKey = _tenant.CurrentPacsKey ?? "default_pacs";
                SelectedPacsProfile = PacsProfiles.FirstOrDefault(p => p.PacsKey == currentPacsKey);
                _selectedPacsForAutomation = currentPacsKey;
                OnPropertyChanged(nameof(SelectedPacsForAutomation));

                LoadAutomationForPacs(currentPacsKey);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SettingsVM] Error loading PACS profiles: {ex.Message}");
                MessageBox.Show($"Failed to load PACS profiles: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadAutomationForPacs(string? pacsKey)
        {
            if (string.IsNullOrWhiteSpace(pacsKey))
                return;

            Debug.WriteLine($"[SettingsVM] Loading automation settings for PACS={pacsKey}");

            NewStudyModules.Clear();
            AddStudyModules.Clear();
            ShortcutOpenNewModules.Clear();
            ShortcutOpenAddModules.Clear();
            ShortcutOpenAfterOpenModules.Clear();
            SendReportModules.Clear();
            SendReportPreviewModules.Clear();
            ShortcutSendReportPreviewModules.Clear();
            ShortcutSendReportReportifiedModules.Clear();
            TestModules.Clear();

            try
            {
                var automationFile = GetAutomationFilePath(pacsKey);
                if (System.IO.File.Exists(automationFile))
                {
                    var json = System.IO.File.ReadAllText(automationFile);
                    var settings = System.Text.Json.JsonSerializer.Deserialize<AutomationSettings>(json);

                    if (settings != null)
                    {
                        LoadModuleSequence(settings.NewStudySequence, NewStudyModules);
                        LoadModuleSequence(settings.AddStudySequence, AddStudyModules);
                        LoadModuleSequence(settings.ShortcutOpenNew, ShortcutOpenNewModules);
                        LoadModuleSequence(settings.ShortcutOpenAdd, ShortcutOpenAddModules);
                        LoadModuleSequence(settings.ShortcutOpenAfterOpen, ShortcutOpenAfterOpenModules);
                        LoadModuleSequence(settings.SendReportSequence, SendReportModules);
                        LoadModuleSequence(settings.SendReportPreviewSequence, SendReportPreviewModules);
                        LoadModuleSequence(settings.ShortcutSendReportPreview, ShortcutSendReportPreviewModules);
                        LoadModuleSequence(settings.ShortcutSendReportReportified, ShortcutSendReportReportifiedModules);
                        LoadModuleSequence(settings.TestSequence, TestModules);
                        
                        Debug.WriteLine($"[SettingsVM] Loaded automation settings from {automationFile}");
                    }
                }
                else
                {
                    Debug.WriteLine($"[SettingsVM] No automation file found at {automationFile}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SettingsVM] Error loading automation: {ex.Message}");
            }
        }

        private static void LoadModuleSequence(string? sequence, ObservableCollection<string> target)
        {
            if (string.IsNullOrWhiteSpace(sequence)) return;

            foreach (var m in sequence.Split(',', ';'))
            {
                if (!string.IsNullOrWhiteSpace(m))
                    target.Add(m.Trim());
            }
        }

        public void SaveAutomationForPacs()
        {
            if (string.IsNullOrWhiteSpace(SelectedPacsForAutomation))
                return;

            try
            {
                Debug.WriteLine($"[SettingsVM] Saving automation for PACS={SelectedPacsForAutomation}");
                Debug.WriteLine($"[SettingsVM] ModalitiesNoHeaderUpdate current value: {ModalitiesNoHeaderUpdate}");

                var settings = new AutomationSettings
                {
                    NewStudySequence = string.Join(",", NewStudyModules),
                    AddStudySequence = string.Join(",", AddStudyModules),
                    ShortcutOpenNew = string.Join(",", ShortcutOpenNewModules),
                    ShortcutOpenAdd = string.Join(",", ShortcutOpenAddModules),
                    ShortcutOpenAfterOpen = string.Join(",", ShortcutOpenAfterOpenModules),
                    SendReportSequence = string.Join(",", SendReportModules),
                    SendReportPreviewSequence = string.Join(",", SendReportPreviewModules),
                    ShortcutSendReportPreview = string.Join(",", ShortcutSendReportPreviewModules),
                    ShortcutSendReportReportified = string.Join(",", ShortcutSendReportReportifiedModules),
                    TestSequence = string.Join(",", TestModules)
                };

                var automationFile = GetAutomationFilePath(SelectedPacsForAutomation);
                var dir = System.IO.Path.GetDirectoryName(automationFile);
                if (!string.IsNullOrEmpty(dir))
                    System.IO.Directory.CreateDirectory(dir);

                var json = System.Text.Json.JsonSerializer.Serialize(settings, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                System.IO.File.WriteAllText(automationFile, json);

                Debug.WriteLine($"[SettingsVM] Saved automation to {automationFile}");
                
                // Save ModalitiesNoHeaderUpdate to local settings (global setting)
                try
                {
                    Debug.WriteLine($"[SettingsVM] Attempting to save ModalitiesNoHeaderUpdate to local settings...");
                    Debug.WriteLine($"[SettingsVM] _local is null: {_local == null}");
                    if (_local != null)
                    {
                        Debug.WriteLine($"[SettingsVM] Calling _local.ModalitiesNoHeaderUpdate = '{ModalitiesNoHeaderUpdate}'");
                        _local.ModalitiesNoHeaderUpdate = ModalitiesNoHeaderUpdate;
                        Debug.WriteLine($"[SettingsVM] Successfully set ModalitiesNoHeaderUpdate in local settings");
                        
                        // Verify it was saved by reading it back
                        var readBack = _local.ModalitiesNoHeaderUpdate;
                        Debug.WriteLine($"[SettingsVM] Read back value: '{readBack}'");
                    }
                    else
                    {
                        Debug.WriteLine($"[SettingsVM] ERROR: _local is null, cannot save ModalitiesNoHeaderUpdate");
                    }
                }
                catch (Exception localEx)
                {
                    Debug.WriteLine($"[SettingsVM] EXCEPTION while saving ModalitiesNoHeaderUpdate: {localEx.Message}");
                    Debug.WriteLine($"[SettingsVM] Stack trace: {localEx.StackTrace}");
                }
                
                MessageBox.Show($"Automation saved for {SelectedPacsForAutomation}.", "Automation", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SettingsVM] Error saving automation: {ex.Message}");
                MessageBox.Show($"Failed to save automation: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static string GetAutomationFilePath(string pacsKey)
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return System.IO.Path.Combine(appData, "Wysg.Musm", "Radium", "Pacs", SanitizeFileName(pacsKey), "automation.json");
        }

        private static string GetSpySettingsPath(string pacsKey)
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return System.IO.Path.Combine(appData, "Wysg.Musm", "Radium", "Pacs", SanitizeFileName(pacsKey), "ui-procedures.json");
        }

        private static string SanitizeFileName(string name)
        {
            var invalid = System.IO.Path.GetInvalidFileNameChars();
            return string.Join("_", name.Split(invalid, StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.');
        }

        private class AutomationSettings
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
            // REMOVED: DoNotUpdateHeaderInXR - now stored in local settings, not PACS-scoped
        }

        private static string? Prompt(string title, string message, string defaultText)
        {
            var win = new Window
            {
                Title = title,
                Width = 380,
                Height = 170,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.ToolWindow,
                Owner = Application.Current?.MainWindow
            };

            var grid = new System.Windows.Controls.Grid { Margin = new Thickness(12) };
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = GridLength.Auto });
            var txtMsg = new System.Windows.Controls.TextBlock { Text = message, Margin = new Thickness(0, 0, 0, 8) };
            var txt = new System.Windows.Controls.TextBox { Text = defaultText, Margin = new Thickness(0, 0, 0, 12) };
            var panel = new System.Windows.Controls.StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var ok = new System.Windows.Controls.Button { Content = "OK", Width = 80, Margin = new Thickness(0, 0, 8, 0), IsDefault = true };
            var cancel = new System.Windows.Controls.Button { Content = "Cancel", Width = 80, IsCancel = true };
            ok.Click += (_, __) => { win.DialogResult = true; };
            panel.Children.Add(ok);
            panel.Children.Add(cancel);
            System.Windows.Controls.Grid.SetRow(txtMsg, 0);
            System.Windows.Controls.Grid.SetRow(txt, 1);
            System.Windows.Controls.Grid.SetRow(panel, 2);
            grid.Children.Add(txtMsg);
            grid.Children.Add(txt);
            grid.Children.Add(panel);
            win.Content = grid;
            var result = win.ShowDialog();
            return result == true ? txt.Text : null;
        }

        private async Task OnAddPacsProfileAsync()
        {
            if (_tenant == null || _tenant.AccountId <= 0)
            {
                MessageBox.Show("No account selected.", "PACS", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var input = Prompt("Add PACS", "Enter PACS key (e.g., 'infinitt_main', 'pacs_backup'):", "new_pacs");
            if (string.IsNullOrWhiteSpace(input)) return;

            var pacsKey = input.Trim().ToLowerInvariant().Replace(" ", "_");

            try
            {
                Debug.WriteLine($"[SettingsVM] Adding PACS with key={pacsKey}");
                IsBusy = true;

                var tenant = await _tenantRepo!.EnsureTenantAsync(_tenant.AccountId, pacsKey);

                Debug.WriteLine($"[SettingsVM] PACS added: tenant_id={tenant.Id} pacs_key={tenant.PacsKey}");

                await LoadPacsProfilesAsync();
                MessageBox.Show($"PACS '{pacsKey}' added.", "PACS", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SettingsVM] Error adding PACS: {ex.Message}");
                MessageBox.Show($"Failed to add PACS: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task OnRenamePacsProfileAsync(PacsProfileItem? profile)
        {
            if (profile == null) return;

            MessageBox.Show("Renaming PACS key is not supported. Create a new PACS profile instead.", "PACS", MessageBoxButton.OK, MessageBoxImage.Information);
            await Task.CompletedTask;
        }

        private async Task OnRemovePacsProfileAsync(PacsProfileItem? profile)
        {
            if (profile == null || PacsProfiles.Count <= 1) return;

            var result = MessageBox.Show($"Remove PACS '{profile.PacsKey}'?\n\nThis will delete the database tenant record.\nAutomation and spy settings files will remain on disk.", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                Debug.WriteLine($"[SettingsVM] Removing PACS tenant_id={profile.TenantId} pacs_key={profile.PacsKey}");
                IsBusy = true;

                await _tenantRepo!.DeleteTenantAsync(profile.TenantId);

                Debug.WriteLine($"[SettingsVM] PACS removed: {profile.PacsKey}");

                await LoadPacsProfilesAsync();
                MessageBox.Show($"PACS '{profile.PacsKey}' removed.", "PACS", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SettingsVM] Error removing PACS: {ex.Message}");
                MessageBox.Show($"Failed to remove PACS: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        // NEW: Comma-separated list of modalities that should not update header fields
        private string _modalitiesNoHeaderUpdate = string.Empty;
        public string ModalitiesNoHeaderUpdate
        {
            get => _modalitiesNoHeaderUpdate;
            set => SetProperty(ref _modalitiesNoHeaderUpdate, value);
        }
    }
}
