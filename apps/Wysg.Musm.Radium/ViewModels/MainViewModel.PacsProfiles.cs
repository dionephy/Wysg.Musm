using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Wysg.Musm.Radium.Models;
using Wysg.Musm.Radium.Services;

namespace Wysg.Musm.Radium.ViewModels
{
    /// <summary>
    /// Partial: PACS profile management for MainViewModel
    /// Handles PACS profile selection and switching in the main window
    /// </summary>
    public partial class MainViewModel
    {
        private PacsProfileManager? _pacsProfileManager;
        private string? _selectedPacsProfileName;

        // PACS profiles collection for ComboBox binding
        [ObservableProperty]
        private ObservableCollection<PacsProfile> pacsProfiles = new();

        // Selected PACS profile name
        public string? SelectedPacsProfileName
        {
            get => _selectedPacsProfileName;
            set
            {
                if (SetProperty(ref _selectedPacsProfileName, value))
                {
                    OnPacsProfileChanged(value);
                }
            }
        }

        public void InitializePacsProfilesForMain()
        {
            _pacsProfileManager = new PacsProfileManager();
            LoadPacsProfilesForMain();
        }

        private void LoadPacsProfilesForMain()
        {
            if (_pacsProfileManager == null) return;

            PacsProfiles.Clear();
            var profiles = _pacsProfileManager.GetAllProfiles();
            foreach (var profile in profiles)
            {
                PacsProfiles.Add(profile);
            }

            var current = _pacsProfileManager.GetCurrentProfile();
            _selectedPacsProfileName = current?.Name;
            OnPropertyChanged(nameof(SelectedPacsProfileName));
            
            // Update PACS display
            if (current != null)
            {
                CurrentPacsDisplay = $"PACS: {current.Name}";
            }
        }

        private void OnPacsProfileChanged(string? pacsName)
        {
            if (string.IsNullOrWhiteSpace(pacsName) || _pacsProfileManager == null)
                return;

            // Update current profile
            _pacsProfileManager.SetCurrentProfile(pacsName);

            // Load automation settings for the selected PACS
            var profile = PacsProfiles.FirstOrDefault(p => p.Name == pacsName);
            if (profile != null && _localSettings != null)
            {
                // Update local settings to reflect current PACS automation sequences
                // These will be used by the automation execution logic
                _localSettings.AutomationNewStudySequence = profile.AutomationNewStudySequence ?? string.Empty;
                _localSettings.AutomationAddStudySequence = profile.AutomationAddStudySequence ?? string.Empty;
                _localSettings.AutomationShortcutOpenNew = profile.AutomationShortcutOpenNew ?? string.Empty;
                _localSettings.AutomationShortcutOpenAdd = profile.AutomationShortcutOpenAdd ?? string.Empty;
                _localSettings.AutomationShortcutOpenAfterOpen = profile.AutomationShortcutOpenAfterOpen ?? string.Empty;
            }

            // Update PACS display in status bar
            try 
            { 
                StatusText = $"Switched to PACS: {pacsName}";
                CurrentPacsDisplay = $"PACS: {pacsName}";
            } 
            catch { }
        }

        public PacsProfileManager? GetPacsProfileManager() => _pacsProfileManager;
    }
}
