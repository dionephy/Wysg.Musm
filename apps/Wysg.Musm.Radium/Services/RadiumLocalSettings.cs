using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Diagnostics;

namespace Wysg.Musm.Radium.Services
{
    /// <summary>
    /// Concrete implementation of <see cref="IRadiumLocalSettings"/> storing values in an encrypted DPAPI blob.
    ///
    /// Storage format (simple KV):
    ///   Plain text (before encryption) lines: key=value\n
    ///   The whole concatenated UTF-8 buffer is encrypted via <see cref="ProtectedData.Protect"/> with CurrentUser scope.
    ///   This keeps implementation compact while avoiding per-key encryption overhead / metadata complexity.
    ///
    /// Characteristics:
    ///   * Atomic writes: entire file rewritten on each change; acceptable due to small key set.
    ///   * Resilience: read errors fail silently returning null (callers treat as unset / default).
    ///   * Environment override: CentralConnectionString can be injected via MUSM_CENTRAL_DB without touching disk.
    ///
    /// Performance: Suitable for infrequent updates (settings dialog). For high-frequency writes a journaled format would be preferable.
    /// Thread-safety: No locking; concurrent writes could race but typical desktop usage avoids multi-thread mutation.
    /// </summary>
    public sealed class RadiumLocalSettings : IRadiumLocalSettings
    {
        private static readonly string Dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Wysg.Musm", "Radium");
        private static readonly string MainPath = Path.Combine(Dir, "settings.dat");

        // Central: now strictly local (env var MUSM_CENTRAL_DB override removed per request)
        public string? CentralConnectionString
        {
            get => ReadSecret("central");
            set => WriteSecret("central", value ?? string.Empty);
        }

        public string? LocalConnectionString
        {
            get => ReadSecret("local");
            set => WriteSecret("local", value ?? string.Empty);
        }

        [Obsolete("Use LocalConnectionString explicitly; this alias will be removed.")]
        public string? ConnectionString { get => LocalConnectionString; set => LocalConnectionString = value; }

        public string? AutomationNewStudySequence { get => ReadSecret("auto_newstudy"); set => WriteSecret("auto_newstudy", value ?? string.Empty); }
        public string? AutomationAddStudySequence { get => ReadSecret("auto_addstudy"); set => WriteSecret("auto_addstudy", value ?? string.Empty); }
        public string? AutomationShortcutOpenNew { get => ReadSecret("auto_shortcut_open_new"); set => WriteSecret("auto_shortcut_open_new", value ?? string.Empty); }
        public string? AutomationShortcutOpenAdd { get => ReadSecret("auto_shortcut_open_add"); set => WriteSecret("auto_shortcut_open_add", value ?? string.Empty); }
        public string? AutomationShortcutOpenAfterOpen { get => ReadSecret("auto_shortcut_open_after_open"); set => WriteSecret("auto_shortcut_open_after_open", value ?? string.Empty); }

        // Global hotkeys
        public string? GlobalHotkeyOpenStudy { get => ReadSecret("hotkey_open_study"); set => WriteSecret("hotkey_open_study", value ?? string.Empty); }
        public string? GlobalHotkeySendStudy { get => ReadSecret("hotkey_send_study"); set => WriteSecret("hotkey_send_study", value ?? string.Empty); }

        // Window placement (left,top,width,height,state)
        public string? MainWindowPlacement { get => ReadSecret("main_window_placement"); set => WriteSecret("main_window_placement", value ?? string.Empty); }

        /// <summary>
        /// Decrypts settings file (if present) and returns the value for a key. Failures are swallowed to avoid
        /// disruptive UX (caller sees null and can prompt for settings).
        /// </summary>
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

        /// <summary>
        /// Writes or updates a key. Implementation loads existing data first to preserve other keys, then rewrites
        /// the entire plaintext buffer and encrypts. Errors are swallowed (best effort) ? caller actions do not throw.
        /// </summary>
        private static void WriteSecret(string key, string value)
        {
            try
            {
                Directory.CreateDirectory(Dir);
                var dict = new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
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
            catch { /* Swallow write errors to avoid crashing settings UI */ }
        }
    }
}
