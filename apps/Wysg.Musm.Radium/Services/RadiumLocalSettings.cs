using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Wysg.Musm.Radium.Services
{
    public sealed class RadiumLocalSettings : IRadiumLocalSettings
    {
        private static readonly string Dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Wysg.Musm", "Radium");
        private static readonly string MainPath = Path.Combine(Dir, "settings.dat");

        public string? CentralConnectionString
        {
            get
            {
                // Allow override via environment variable in production
                var fromEnv = Environment.GetEnvironmentVariable("MUSM_CENTRAL_DB");
                if (!string.IsNullOrWhiteSpace(fromEnv)) return fromEnv;
                return ReadSecret("central");
            }
            set => WriteSecret("central", value ?? string.Empty);
        }

        public string? LocalConnectionString
        {
            get => ReadSecret("local");
            set => WriteSecret("local", value ?? string.Empty);
        }

        // Backward compat shim to current usage
        public string? ConnectionString
        {
            get => LocalConnectionString;
            set => LocalConnectionString = value;
        }

        // Add keys for automation sequences
        public string? AutomationNewStudySequence { get => ReadSecret("auto_newstudy"); set => WriteSecret("auto_newstudy", value ?? string.Empty); }
        public string? AutomationAddStudySequence { get => ReadSecret("auto_addstudy"); set => WriteSecret("auto_addstudy", value ?? string.Empty); }

        private static string? ReadSecret(string key)
        {
            try
            {
                if (!File.Exists(MainPath)) return null;
                var enc = File.ReadAllBytes(MainPath);
                var plain = ProtectedData.Unprotect(enc, null, DataProtectionScope.CurrentUser);
                var text = Encoding.UTF8.GetString(plain);
                foreach (var line in text.Split('\n'))
                {
                    var idx = line.IndexOf('=');
                    if (idx <= 0) continue;
                    var k = line[..idx];
                    var v = line[(idx + 1)..];
                    if (k == key) return v;
                }
                return null;
            }
            catch { return null; }
        }

        private static void WriteSecret(string key, string value)
        {
            try
            {
                Directory.CreateDirectory(Dir);
                var dict = new System.Collections.Generic.Dictionary<string, string>();
                if (File.Exists(MainPath))
                {
                    var enc = File.ReadAllBytes(MainPath);
                    var plain = ProtectedData.Unprotect(enc, null, DataProtectionScope.CurrentUser);
                    var text = Encoding.UTF8.GetString(plain);
                    foreach (var line in text.Split('\n'))
                    {
                        var idx = line.IndexOf('=');
                        if (idx <= 0) continue;
                        dict[line[..idx]] = line[(idx + 1)..];
                    }
                }
                dict[key] = value;
                var sb = new StringBuilder();
                foreach (var kv in dict)
                {
                    sb.Append(kv.Key).Append('=').Append(kv.Value).Append('\n');
                }
                var bytes = Encoding.UTF8.GetBytes(sb.ToString());
                var encOut = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
                File.WriteAllBytes(MainPath, encOut);
            }
            catch { }
        }
    }
}
