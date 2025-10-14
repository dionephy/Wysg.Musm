using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Wysg.Musm.Radium.Models;

namespace Wysg.Musm.Radium.Services
{
    /// <summary>
    /// Manages PACS profiles stored locally as JSON.
    /// Each profile contains PACS-specific automation sequences and UI spy settings.
    /// </summary>
    public sealed class PacsProfileManager
    {
        private static readonly string ProfilesDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Wysg.Musm", "Radium", "PacsProfiles");
        
        private static readonly string ProfilesFile = Path.Combine(ProfilesDir, "profiles.json");
        private static readonly string CurrentProfileFile = Path.Combine(ProfilesDir, "current.txt");

        private List<PacsProfile> _profiles;
        private string _currentProfileName;

        public PacsProfileManager()
        {
            Directory.CreateDirectory(ProfilesDir);
            _profiles = LoadProfiles();
            _currentProfileName = LoadCurrentProfileName();
            
            // Ensure at least a default profile exists
            if (_profiles.Count == 0)
            {
                var defaultProfile = new PacsProfile("Default PACS", "INFINITT");
                _profiles.Add(defaultProfile);
                SaveProfiles();
            }
        }

        public IReadOnlyList<PacsProfile> GetAllProfiles() => _profiles.AsReadOnly();

        public PacsProfile? GetCurrentProfile()
        {
            var profile = _profiles.FirstOrDefault(p => p.Name == _currentProfileName);
            if (profile == null && _profiles.Count > 0)
            {
                profile = _profiles[0];
                _currentProfileName = profile.Name;
                SaveCurrentProfileName();
            }
            return profile;
        }

        public void SetCurrentProfile(string name)
        {
            if (_profiles.Any(p => p.Name == name))
            {
                _currentProfileName = name;
                SaveCurrentProfileName();
            }
        }

        public bool AddProfile(string name, string processName = "INFINITT")
        {
            if (string.IsNullOrWhiteSpace(name) || _profiles.Any(p => p.Name == name))
                return false;

            var profile = new PacsProfile(name.Trim(), processName.Trim());
            _profiles.Add(profile);
            SaveProfiles();
            return true;
        }

        public bool RenameProfile(string oldName, string newName)
        {
            if (string.IsNullOrWhiteSpace(newName) || oldName == newName)
                return false;

            var profile = _profiles.FirstOrDefault(p => p.Name == oldName);
            if (profile == null || _profiles.Any(p => p.Name == newName))
                return false;

            profile.Name = newName.Trim();
            profile.UpdatedAt = DateTime.UtcNow;
            
            // Update current profile name if it was renamed
            if (_currentProfileName == oldName)
            {
                _currentProfileName = newName;
                SaveCurrentProfileName();
            }
            
            SaveProfiles();
            return true;
        }

        public bool RemoveProfile(string name)
        {
            // Don't allow removing the last profile
            if (_profiles.Count <= 1)
                return false;

            var profile = _profiles.FirstOrDefault(p => p.Name == name);
            if (profile == null)
                return false;

            _profiles.Remove(profile);
            
            // If current profile was removed, set to first available
            if (_currentProfileName == name)
            {
                _currentProfileName = _profiles[0].Name;
                SaveCurrentProfileName();
            }
            
            SaveProfiles();
            return true;
        }

        public void UpdateProfile(PacsProfile profile)
        {
            var existing = _profiles.FirstOrDefault(p => p.Name == profile.Name);
            if (existing != null)
            {
                existing.ProcessName = profile.ProcessName;
                existing.AutomationNewStudySequence = profile.AutomationNewStudySequence;
                existing.AutomationAddStudySequence = profile.AutomationAddStudySequence;
                existing.AutomationShortcutOpenNew = profile.AutomationShortcutOpenNew;
                existing.AutomationShortcutOpenAdd = profile.AutomationShortcutOpenAdd;
                existing.AutomationShortcutOpenAfterOpen = profile.AutomationShortcutOpenAfterOpen;
                existing.UpdatedAt = DateTime.UtcNow;
                SaveProfiles();
            }
        }

        private List<PacsProfile> LoadProfiles()
        {
            try
            {
                if (!File.Exists(ProfilesFile))
                    return new List<PacsProfile>();

                var json = File.ReadAllText(ProfilesFile);
                return JsonSerializer.Deserialize<List<PacsProfile>>(json, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                }) ?? new List<PacsProfile>();
            }
            catch
            {
                return new List<PacsProfile>();
            }
        }

        private void SaveProfiles()
        {
            try
            {
                var json = JsonSerializer.Serialize(_profiles, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                File.WriteAllText(ProfilesFile, json);
            }
            catch
            {
                // Swallow errors to prevent UI disruption
            }
        }

        private string LoadCurrentProfileName()
        {
            try
            {
                if (File.Exists(CurrentProfileFile))
                    return File.ReadAllText(CurrentProfileFile).Trim();
            }
            catch { }
            
            return _profiles.Count > 0 ? _profiles[0].Name : "Default PACS";
        }

        private void SaveCurrentProfileName()
        {
            try
            {
                File.WriteAllText(CurrentProfileFile, _currentProfileName);
            }
            catch { }
        }

        /// <summary>
        /// Gets the UI spy settings directory for the specified profile.
        /// Each profile has its own ui-procedures.json file.
        /// </summary>
        public string GetSpySettingsPath(string profileName)
        {
            var dir = Path.Combine(ProfilesDir, SanitizeFileName(profileName));
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "ui-procedures.json");
        }

        /// <summary>
        /// Gets the UI bookmarks file path for the specified profile.
        /// Each profile has its own bookmarks.json file.
        /// </summary>
        public string GetBookmarksPath(string profileName)
        {
            var dir = Path.Combine(ProfilesDir, SanitizeFileName(profileName));
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "bookmarks.json");
        }

        private static string SanitizeFileName(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            return string.Join("_", name.Split(invalid, StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.');
        }
    }
}
