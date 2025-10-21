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
    }
}
