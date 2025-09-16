using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Wysg.Musm.Radium.Services
{
    public sealed class RadiumLocalSettings : IRadiumLocalSettings
    {
        private static readonly string FilePath = Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
            "Wysg.Musm", "Radium", "settings.dat");

        public string? ConnectionString
        {
            get => ReadSecret();
            set { WriteSecret(value ?? string.Empty); }
        }

        private static string? ReadSecret()
        {
            try
            {
                if (!File.Exists(FilePath)) return null;
                var enc = File.ReadAllBytes(FilePath);
                var plain = ProtectedData.Unprotect(enc, null, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(plain);
            }
            catch { return null; }
        }

        private static void WriteSecret(string text)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
                var bytes = Encoding.UTF8.GetBytes(text);
                var enc = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
                File.WriteAllBytes(FilePath, enc);
            }
            catch { /* ignore */ }
        }
    }
}
