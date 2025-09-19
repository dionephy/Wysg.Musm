using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Wysg.Musm.Radium.Services
{
    public sealed class DpapiAuthStorage : IAuthStorage
    {
        private static readonly string Dir = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "Wysg.Musm", "Radium");
        private static readonly string FilePath = Path.Combine(Dir, "auth.dat");

        public string? RefreshToken
        {
            get => Read("rt");
            set => Write("rt", value);
        }

        public string? Email
        {
            get => Read("em");
            set => Write("em", value);
        }

        public string? DisplayName
        {
            get => Read("dn");
            set => Write("dn", value);
        }

        public bool RememberMe
        {
            get => Read("rm") == "1";
            set => Write("rm", value ? "1" : "0");
        }

        private static string? Read(string key)
        {
            try
            {
                if (!File.Exists(FilePath)) return null;
                var enc = File.ReadAllBytes(FilePath);
                var plain = ProtectedData.Unprotect(enc, null, DataProtectionScope.CurrentUser);
                var text = Encoding.UTF8.GetString(plain);
                foreach (var line in text.Split('\n'))
                {
                    var idx = line.IndexOf('=');
                    if (idx <= 0) continue;
                    var k = line.Substring(0, idx);
                    if (k != key) continue;
                    return line.Substring(idx + 1);
                }
                return null;
            }
            catch { return null; }
        }

        private static void Write(string key, string? value)
        {
            try
            {
                Directory.CreateDirectory(Dir);
                var dict = new System.Collections.Generic.Dictionary<string, string>();
                if (File.Exists(FilePath))
                {
                    var enc = File.ReadAllBytes(FilePath);
                    var plain = ProtectedData.Unprotect(enc, null, DataProtectionScope.CurrentUser);
                    var text = Encoding.UTF8.GetString(plain);
                    foreach (var line in text.Split('\n'))
                    {
                        var idx = line.IndexOf('=');
                        if (idx <= 0) continue;
                        dict[line.Substring(0, idx)] = line.Substring(idx + 1);
                    }
                }
                if (value == null) dict.Remove(key); else dict[key] = value;
                var sb = new StringBuilder();
                foreach (var kv in dict)
                {
                    sb.Append(kv.Key).Append('=').Append(kv.Value).Append('\n');
                }
                var bytes = Encoding.UTF8.GetBytes(sb.ToString());
                var encOut = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
                File.WriteAllBytes(FilePath, encOut);
            }
            catch { }
        }
    }
}
