using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Wysg.Musm.Radium.Models;

namespace Wysg.Musm.Radium.Services
{
    /// <summary>
    /// Manages PACS methods (stored per PACS profile).
    /// Provides CRUD operations and persistence for custom PACS method configurations.
    /// </summary>
    public class PacsMethodManager
    {
        private readonly string _pacsKey;
        private readonly string _storageFilePath;

        private sealed class PacsMethodStore
        {
            public List<PacsMethod> Methods { get; set; } = new();
        }

        public PacsMethodManager(string pacsKey = "default_pacs")
        {
            _pacsKey = string.IsNullOrWhiteSpace(pacsKey) ? "default_pacs" : pacsKey;
            _storageFilePath = GetStorageFilePath(_pacsKey);
        }

        /// <summary>
        /// Get all PACS methods (built-in + user-defined)
        /// </summary>
        public List<PacsMethod> GetAllMethods()
        {
            var store = Load();
            
            // If empty, seed with built-in methods
            if (store.Methods.Count == 0)
            {
                store.Methods = GetBuiltInMethods();
                Save(store);
            }

            return store.Methods;
        }

        /// <summary>
        /// Add a new PACS method
        /// </summary>
        public void AddMethod(PacsMethod method)
        {
            if (method == null) throw new ArgumentNullException(nameof(method));
            if (string.IsNullOrWhiteSpace(method.Tag)) throw new ArgumentException("Method tag cannot be empty");

            var store = Load();
            
            // Check for duplicate tag
            if (store.Methods.Any(m => string.Equals(m.Tag, method.Tag, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException($"PACS method with tag '{method.Tag}' already exists");

            store.Methods.Add(method);
            Save(store);
        }

        /// <summary>
        /// Update an existing PACS method
        /// </summary>
        public void UpdateMethod(string oldTag, PacsMethod method)
        {
            if (method == null) throw new ArgumentNullException(nameof(method));
            if (string.IsNullOrWhiteSpace(method.Tag)) throw new ArgumentException("Method tag cannot be empty");

            var store = Load();
            var existing = store.Methods.FirstOrDefault(m => string.Equals(m.Tag, oldTag, StringComparison.OrdinalIgnoreCase));
            
            if (existing == null)
                throw new InvalidOperationException($"PACS method with tag '{oldTag}' not found");

            // Check if new tag conflicts with another method
            if (!string.Equals(oldTag, method.Tag, StringComparison.OrdinalIgnoreCase))
            {
                if (store.Methods.Any(m => string.Equals(m.Tag, method.Tag, StringComparison.OrdinalIgnoreCase)))
                    throw new InvalidOperationException($"PACS method with tag '{method.Tag}' already exists");
            }

            existing.Name = method.Name;
            existing.Tag = method.Tag;
            existing.Description = method.Description;
            existing.IsBuiltIn = method.IsBuiltIn;

            Save(store);
        }

        /// <summary>
        /// Delete a PACS method (only user-defined methods can be deleted)
        /// </summary>
        public void DeleteMethod(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag)) throw new ArgumentException("Method tag cannot be empty");

            var store = Load();
            var method = store.Methods.FirstOrDefault(m => string.Equals(m.Tag, tag, StringComparison.OrdinalIgnoreCase));
            
            if (method == null)
                throw new InvalidOperationException($"PACS method with tag '{tag}' not found");

            if (method.IsBuiltIn)
                throw new InvalidOperationException("Cannot delete built-in PACS methods");

            store.Methods.Remove(method);
            Save(store);
        }

        /// <summary>
        /// Get a specific PACS method by tag
        /// </summary>
        public PacsMethod? GetMethod(string tag)
        {
            var store = Load();
            return store.Methods.FirstOrDefault(m => string.Equals(m.Tag, tag, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Reset to built-in methods only (clears all user-defined methods)
        /// </summary>
        public void ResetToBuiltIn()
        {
            var store = new PacsMethodStore
            {
                Methods = GetBuiltInMethods()
            };
            Save(store);
        }

        private static string GetStorageFilePath(string pacsKey)
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var pacsDir = Path.Combine(appData, "Wysg.Musm", "Radium", "Pacs", SanitizeFileName(pacsKey));
            Directory.CreateDirectory(pacsDir);
            return Path.Combine(pacsDir, "pacs-methods.json");
        }

        private PacsMethodStore Load()
        {
            try
            {
                if (!File.Exists(_storageFilePath))
                    return new PacsMethodStore();

                var json = File.ReadAllText(_storageFilePath);
                return JsonSerializer.Deserialize<PacsMethodStore>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web))
                    ?? new PacsMethodStore();
            }
            catch
            {
                return new PacsMethodStore();
            }
        }

        private void Save(PacsMethodStore store)
        {
            try
            {
                var json = JsonSerializer.Serialize(store, new JsonSerializerOptions(JsonSerializerDefaults.Web)
                {
                    WriteIndented = true
                });
                File.WriteAllText(_storageFilePath, json);
            }
            catch
            {
                // Silent fail - user will see defaults next time
            }
        }

        private static string SanitizeFileName(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            return string.Join("_", name.Split(invalid, StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.');
        }

        /// <summary>
        /// Get the default/built-in PACS methods
        /// </summary>
        private static List<PacsMethod> GetBuiltInMethods()
        {
            return new List<PacsMethod>
            {
                // Search Results List
                new PacsMethod { Tag = "GetSelectedIdFromSearchResults", Name = "Get selected ID from search results list", IsBuiltIn = true },
                new PacsMethod { Tag = "GetSelectedNameFromSearchResults", Name = "Get selected name from search results list", IsBuiltIn = true },
                new PacsMethod { Tag = "GetSelectedSexFromSearchResults", Name = "Get selected sex from search results list", IsBuiltIn = true },
                new PacsMethod { Tag = "GetSelectedBirthDateFromSearchResults", Name = "Get selected birth date from search results list", IsBuiltIn = true },
                new PacsMethod { Tag = "GetSelectedAgeFromSearchResults", Name = "Get selected age from search results list", IsBuiltIn = true },
                new PacsMethod { Tag = "GetSelectedStudynameFromSearchResults", Name = "Get selected studyname from search results list", IsBuiltIn = true },
                new PacsMethod { Tag = "GetSelectedStudyDateTimeFromSearchResults", Name = "Get selected study date time from search results list", IsBuiltIn = true },
                new PacsMethod { Tag = "GetSelectedRadiologistFromSearchResults", Name = "Get selected radiologist from search results list", IsBuiltIn = true },
                new PacsMethod { Tag = "GetSelectedStudyRemarkFromSearchResults", Name = "Get selected study remark from search results list", IsBuiltIn = true },
                new PacsMethod { Tag = "GetSelectedReportDateTimeFromSearchResults", Name = "Get selected report date time from search results list", IsBuiltIn = true },
                
                // Related Studies List
                new PacsMethod { Tag = "GetSelectedIdFromRelatedStudies", Name = "Get selected ID from related studies list", IsBuiltIn = true },
                new PacsMethod { Tag = "GetSelectedStudynameFromRelatedStudies", Name = "Get selected studyname from related studies list", IsBuiltIn = true },
                new PacsMethod { Tag = "GetSelectedStudyDateTimeFromRelatedStudies", Name = "Get selected study date time from related studies list", IsBuiltIn = true },
                new PacsMethod { Tag = "GetSelectedRadiologistFromRelatedStudies", Name = "Get selected radiologist from related studies list", IsBuiltIn = true },
                new PacsMethod { Tag = "GetSelectedReportDateTimeFromRelatedStudies", Name = "Get selected report date time from related studies list", IsBuiltIn = true },
                
                // Current Study Data
                new PacsMethod { Tag = "GetCurrentPatientNumber", Name = "Get banner patient number", IsBuiltIn = true },
                new PacsMethod { Tag = "GetCurrentStudyDateTime", Name = "Get banner study date time", IsBuiltIn = true },
                new PacsMethod { Tag = "GetCurrentStudyRemark", Name = "Get current study remark", IsBuiltIn = true },
                new PacsMethod { Tag = "GetCurrentPatientRemark", Name = "Get current patient remark", IsBuiltIn = true },
                new PacsMethod { Tag = "GetCurrentFindings", Name = "Get current findings", IsBuiltIn = true },
                new PacsMethod { Tag = "GetCurrentFindingsWait", Name = "Get current findings wait", IsBuiltIn = true },
                new PacsMethod { Tag = "GetCurrentConclusion", Name = "Get current conclusion", IsBuiltIn = true },
                new PacsMethod { Tag = "GetCurrentFindings2", Name = "Get current findings 2", IsBuiltIn = true },
                new PacsMethod { Tag = "GetCurrentConclusion2", Name = "Get current conclusion 2", IsBuiltIn = true },
                
                // Matching / Validation
                new PacsMethod { Tag = "PatientNumberMatch", Name = "Patient number match", IsBuiltIn = true },
                new PacsMethod { Tag = "StudyDateTimeMatch", Name = "Study date time match", IsBuiltIn = true },
                
                // Actions
                new PacsMethod { Tag = "InvokeOpenStudy", Name = "Invoke open study", IsBuiltIn = true },
                new PacsMethod { Tag = "InvokeTest", Name = "Invoke test", IsBuiltIn = true },
                
                // Custom Mouse Clicks
                new PacsMethod { Tag = "CustomMouseClick1", Name = "Custom mouse click 1", IsBuiltIn = true },
                new PacsMethod { Tag = "CustomMouseClick2", Name = "Custom mouse click 2", IsBuiltIn = true },
                
                // Screen Control
                new PacsMethod { Tag = "SetCurrentStudyInMainScreen", Name = "Set current study in main screen", IsBuiltIn = true },
                new PacsMethod { Tag = "SetPreviousStudyInSubScreen", Name = "Set previous study in sub screen", IsBuiltIn = true },
                
                // Visibility Check
                new PacsMethod { Tag = "WorklistIsVisible", Name = "Worklist is visible", IsBuiltIn = true },
                new PacsMethod { Tag = "ReportTextIsVisible", Name = "ReportText is visible", IsBuiltIn = true },
                
                // UI Actions
                new PacsMethod { Tag = "InvokeOpenWorklist", Name = "Invoke open worklist", IsBuiltIn = true },
                new PacsMethod { Tag = "SetFocusSearchResultsList", Name = "Set focus search results list", IsBuiltIn = true },
                new PacsMethod { Tag = "SendReport", Name = "Send report", IsBuiltIn = true },
                
                // Send Report Actions
                new PacsMethod { Tag = "InvokeSendReport", Name = "Invoke send report", IsBuiltIn = true },
                new PacsMethod { Tag = "SendReportWithoutHeader", Name = "Send report without header", IsBuiltIn = true },
                
                // Report Actions
                new PacsMethod { Tag = "ClearReport", Name = "Clear report", IsBuiltIn = true }
            };
        }
    }
}
