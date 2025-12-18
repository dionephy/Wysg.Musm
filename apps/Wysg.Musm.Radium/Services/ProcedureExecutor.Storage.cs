using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace Wysg.Musm.Radium.Services
{
    internal static partial class ProcedureExecutor
    {
        private static Func<string>? _getProcPathOverride;

        public static void SetProcPathOverride(Func<string> resolver) => _getProcPathOverride = resolver;

        private static string GetProcPath()
        {
            if (_getProcPathOverride != null)
            {
                try 
                { 
                    var path = _getProcPathOverride();
                    Debug.WriteLine($"[ProcedureExecutor][GetProcPath] Override path: {path}");
                    return path; 
                } 
                catch (Exception ex) 
                { 
                    Debug.WriteLine($"[ProcedureExecutor][GetProcPath] Override failed: {ex.Message}");
                }
            }
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Wysg.Musm", "Radium");
            Directory.CreateDirectory(dir);
            var fallback = Path.Combine(dir, "ui-procedures.json");
            Debug.WriteLine($"[ProcedureExecutor][GetProcPath] Fallback path: {fallback}");
            return fallback;
        }

        private static ProcStore Load()
        {
            try
            {
                var p = GetProcPath();
                Debug.WriteLine($"[ProcedureExecutor][Load] Loading from: {p}");
                if (!File.Exists(p)) 
                {
                    Debug.WriteLine($"[ProcedureExecutor][Load] File does not exist, returning empty store");
                    return new ProcStore();
                }
                var json = File.ReadAllText(p);
                var store = JsonSerializer.Deserialize<ProcStore>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web)) ?? new ProcStore();
                Debug.WriteLine($"[ProcedureExecutor][Load] Loaded {store.Methods.Count} procedures: {string.Join(", ", store.Methods.Keys)}");
                return store;
            }
            catch (Exception ex) 
            { 
                Debug.WriteLine($"[ProcedureExecutor][Load] Error: {ex.Message}");
                return new ProcStore(); 
            }
        }

        private static void Save(ProcStore s)
        {
            try
            {
                var p = GetProcPath();
                File.WriteAllText(p, JsonSerializer.Serialize(s, new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true }));
            }
            catch (Exception ex) 
            { 
                Debug.WriteLine($"[ProcedureExecutor][Save] Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Get all procedure names from ui-procedures.json
        /// </summary>
        public static System.Collections.Generic.List<string> GetAllProcedureNames()
        {
            var store = Load();
            return new System.Collections.Generic.List<string>(store.Methods.Keys);
        }

        /// <summary>
        /// Check if a procedure exists
        /// </summary>
        public static bool ProcedureExists(string procedureName)
        {
            var store = Load();
            return store.Methods.ContainsKey(procedureName);
        }

        /// <summary>
        /// Create a new empty procedure
        /// </summary>
        public static bool CreateEmptyProcedure(string procedureName)
        {
            if (string.IsNullOrWhiteSpace(procedureName))
                return false;

            var store = Load();
            
            // Check if procedure already exists
            if (store.Methods.ContainsKey(procedureName))
                return false;

            // Create empty procedure with no operations
            store.Methods[procedureName] = new System.Collections.Generic.List<ProcOpRow>();
            
            Save(store);
            return true;
        }

        /// <summary>
        /// Rename a procedure (both in storage and any automation sequences that reference it)
        /// </summary>
        public static bool RenameProcedure(string oldName, string newName)
        {
            if (string.IsNullOrWhiteSpace(oldName) || string.IsNullOrWhiteSpace(newName))
                return false;

            if (string.Equals(oldName, newName, StringComparison.OrdinalIgnoreCase))
                return true; // No change needed

            var store = Load();
            
            // Check if old procedure exists
            if (!store.Methods.ContainsKey(oldName))
                return false;

            // Check if new name already exists
            if (store.Methods.ContainsKey(newName))
                return false; // Conflict

            // Rename by copying and removing old
            var operations = store.Methods[oldName];
            store.Methods[newName] = operations;
            store.Methods.Remove(oldName);

            Save(store);
            return true;
        }

        /// <summary>
        /// Delete a procedure
        /// </summary>
        public static bool DeleteProcedure(string procedureName)
        {
            if (string.IsNullOrWhiteSpace(procedureName))
                return false;

            var store = Load();
            
            if (!store.Methods.ContainsKey(procedureName))
                return false;

            store.Methods.Remove(procedureName);
            Save(store);
            return true;
        }
    }
}
