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
        /// Get all PACS methods (all are user-defined, no built-in methods)
        /// </summary>
        public List<PacsMethod> GetAllMethods()
        {
            var store = Load();
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
            // IsBuiltIn flag removed - all methods are custom/dynamic

            Save(store);
        }

        /// <summary>
        /// Delete a PACS method (all methods are user-defined and can be deleted)
        /// </summary>
        public void DeleteMethod(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag)) throw new ArgumentException("Method tag cannot be empty");

            var store = Load();
            var method = store.Methods.FirstOrDefault(m => string.Equals(m.Tag, tag, StringComparison.OrdinalIgnoreCase));
            
            if (method == null)
                throw new InvalidOperationException($"PACS method with tag '{tag}' not found");

            // All methods are custom/dynamic and can be deleted
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
    }
}
