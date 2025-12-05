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

        // Diagnostic logging flag - set to true only when debugging settings issues
        private const bool ENABLE_DIAGNOSTIC_LOGGING = false;

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
        public string? GlobalHotkeyToggleSyncText { get => ReadSecret("hotkey_toggle_sync_text"); set => WriteSecret("hotkey_toggle_sync_text", value ?? string.Empty); }

        // Snowstorm network settings
        public string? SnowstormBaseUrl { get => ReadSecret("snowstorm_base_url"); set => WriteSecret("snowstorm_base_url", value ?? string.Empty); }

        // Window placement (left,top,width,height,state)
        public string? MainWindowPlacement { get => ReadSecret("main_window_placement"); set => WriteSecret("main_window_placement", value ?? string.Empty); }

        // Comma-separated list of modalities that should not update header fields (e.g., "XR,CR,DX")
        public string? ModalitiesNoHeaderUpdate { get => ReadSecret("modalities_no_header_update"); set => WriteSecret("modalities_no_header_update", value ?? string.Empty); }

        // Auto toggles for report field generation
        public bool CopyStudyRemarkToChiefComplaint { get => ReadBool("copy_study_remark_to_chief_complaint"); set => WriteBool("copy_study_remark_to_chief_complaint", value); }
        public bool AutoChiefComplaint { get => ReadBool("auto_chief_complaint"); set => WriteBool("auto_chief_complaint", value); }
        public bool AutoPatientHistory { get => ReadBool("auto_patient_history"); set => WriteBool("auto_patient_history", value); }
        public bool AutoConclusion { get => ReadBool("auto_conclusion"); set => WriteBool("auto_conclusion", value); }
        public bool AutoFindingsProofread { get => ReadBool("auto_findings_proofread"); set => WriteBool("auto_findings_proofread", value); }
        public bool AutoConclusionProofread { get => ReadBool("auto_conclusion_proofread"); set => WriteBool("auto_conclusion_proofread", value); }

        // NEW: Editor autofocus configuration
        public bool EditorAutofocusEnabled { get => ReadBool("editor_autofocus_enabled"); set => WriteBool("editor_autofocus_enabled", value); }
        public string? EditorAutofocusBookmark { get => ReadSecret("editor_autofocus_bookmark"); set => WriteSecret("editor_autofocus_bookmark", value ?? string.Empty); }
        public string? EditorAutofocusKeyTypes { get => ReadSecret("editor_autofocus_key_types"); set => WriteSecret("editor_autofocus_key_types", value ?? string.Empty); }
        public string? EditorAutofocusWindowTitle { get => ReadSecret("editor_autofocus_window_title"); set => WriteSecret("editor_autofocus_window_title", value ?? string.Empty); }

        // NEW: Always on Top setting
        public bool AlwaysOnTop { get => ReadBool("always_on_top"); set => WriteBool("always_on_top", value); }

        // NEW: Session-based caching configuration
        public string? SessionBasedCacheBookmarks { get => ReadSecret("session_based_cache_bookmarks"); set => WriteSecret("session_based_cache_bookmarks", value ?? string.Empty); }

        /// <summary>
        /// Decrypts settings file (if present) and returns the value for a key. Failures are swallowed to avoid
        /// disruptive UX (caller sees null and can prompt for settings).
        /// </summary>
        private static string? ReadSecret(string key)
        {
            try
            {
                if (!File.Exists(MainPath))
                {
                    if (ENABLE_DIAGNOSTIC_LOGGING)
                        Debug.WriteLine($"[RadiumLocalSettings] Settings file does not exist: {MainPath}");
                    return null;
                }
                
                var enc = File.ReadAllBytes(MainPath);
                if (ENABLE_DIAGNOSTIC_LOGGING)
                    Debug.WriteLine($"[RadiumLocalSettings] Read {enc.Length} encrypted bytes from {MainPath}");
                
                var plain = ProtectedData.Unprotect(enc, null, DataProtectionScope.CurrentUser);
                var text = Encoding.UTF8.GetString(plain);
                if (ENABLE_DIAGNOSTIC_LOGGING)
                    Debug.WriteLine($"[RadiumLocalSettings] Decrypted {text.Length} chars");
                
                foreach (var line in text.Split('\n'))
                {
                    var idx = line.IndexOf('=');
                    if (idx <= 0) continue;
                    var k = line[..idx];
                    var v = line[(idx + 1)..];
                    if (k == key)
                    {
                        if (ENABLE_DIAGNOSTIC_LOGGING)
                            Debug.WriteLine($"[RadiumLocalSettings] Found key '{key}' with value length {v.Length}");
                        return v;
                    }
                }
                if (ENABLE_DIAGNOSTIC_LOGGING)
                    Debug.WriteLine($"[RadiumLocalSettings] Key '{key}' not found in settings");
                return null;
            }
            catch (CryptographicException ex)
            {
                Debug.WriteLine($"[RadiumLocalSettings] Cryptographic error reading key '{key}': {ex.Message}");
                Debug.WriteLine($"[RadiumLocalSettings] Settings file may be corrupted. Attempting to delete: {MainPath}");
                try
                {
                    // Delete corrupted file so user can reconfigure
                    if (File.Exists(MainPath))
                    {
                        File.Delete(MainPath);
                        Debug.WriteLine($"[RadiumLocalSettings] Deleted corrupted settings file");
                    }
                }
                catch (Exception deleteEx)
                {
                    Debug.WriteLine($"[RadiumLocalSettings] Failed to delete corrupted file: {deleteEx.Message}");
                }
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[RadiumLocalSettings] Error reading key '{key}': {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Reads a boolean value from settings file. Returns false if key not found.
        /// </summary>
        private static bool ReadBool(string key)
        {
            var value = ReadSecret(key);
            return !string.IsNullOrWhiteSpace(value) && (value == "1" || value.Equals("true", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Writes or updates a key. Implementation loads existing data first to preserve other keys, then rewrites
        /// the entire plaintext buffer and encrypts. Errors are swallowed (best effort) ¡æ caller actions do not throw.
        /// </summary>
        private static void WriteSecret(string key, string value)
        {
            try
            {
                if (ENABLE_DIAGNOSTIC_LOGGING)
                    Debug.WriteLine($"[RadiumLocalSettings] WriteSecret key='{key}' valueLength={value.Length}");
                Directory.CreateDirectory(Dir);
                if (ENABLE_DIAGNOSTIC_LOGGING)
                    Debug.WriteLine($"[RadiumLocalSettings] Ensured directory exists: {Dir}");
                
                var dict = new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                if (File.Exists(MainPath))
                {
                    if (ENABLE_DIAGNOSTIC_LOGGING)
                        Debug.WriteLine($"[RadiumLocalSettings] Loading existing settings from {MainPath}");
                    try
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
                        if (ENABLE_DIAGNOSTIC_LOGGING)
                            Debug.WriteLine($"[RadiumLocalSettings] Loaded {dict.Count} existing keys");
                    }
                    catch (CryptographicException ex)
                    {
                        Debug.WriteLine($"[RadiumLocalSettings] Cryptographic error loading existing settings: {ex.Message}");
                        Debug.WriteLine($"[RadiumLocalSettings] Starting with empty settings (corrupted file will be overwritten)");
                        // Continue with empty dict - will overwrite corrupted file
                    }
                }
                else
                {
                    if (ENABLE_DIAGNOSTIC_LOGGING)
                        Debug.WriteLine($"[RadiumLocalSettings] No existing settings file, creating new");
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
                if (ENABLE_DIAGNOSTIC_LOGGING)
                    Debug.WriteLine($"[RadiumLocalSettings] Successfully wrote {encOut.Length} encrypted bytes to {MainPath}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[RadiumLocalSettings] Error writing key '{key}': {ex.GetType().Name} - {ex.Message}");
                Debug.WriteLine($"[RadiumLocalSettings] Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Writes a boolean value to settings file.
        /// </summary>
        private static void WriteBool(string key, bool value)
        {
            WriteSecret(key, value ? "1" : "0");
        }
    }
}
