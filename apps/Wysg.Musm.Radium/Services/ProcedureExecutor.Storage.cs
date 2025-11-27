using System;
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
                try { return _getProcPathOverride(); } catch { }
            }
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Wysg.Musm", "Radium");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "ui-procedures.json");
        }

        private static ProcStore Load()
        {
            try
            {
                var p = GetProcPath();
                if (!File.Exists(p)) return new ProcStore();
                return JsonSerializer.Deserialize<ProcStore>(File.ReadAllText(p), new JsonSerializerOptions(JsonSerializerDefaults.Web)) ?? new ProcStore();
            }
            catch { return new ProcStore(); }
        }

        private static void Save(ProcStore s)
        {
            try
            {
                var p = GetProcPath();
                File.WriteAllText(p, JsonSerializer.Serialize(s, new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true }));
            }
            catch { }
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
