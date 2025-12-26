namespace Wysg.Musm.Radium.Services
{
    /// <summary>
    /// Abstraction over persisted, per-user local configuration required by the Radium desktop client.
    /// Backed by <see cref="RadiumLocalSettings"/> which stores an encrypted key-value file using DPAPI.
    ///
    /// Design goals:
    ///   * Keep sensitive connection strings off plain text disk (DPAPI CurrentUser scope).
    ///   * Allow environment variable override for Central DB (MUSM_CENTRAL_DB) to support packaging / CI scenarios.
    ///   * Provide forward-compatible keys for additional automation / feature flags without schema migration.
    ///
    /// Thread-safety: implementation uses simple file read/writes; callers should treat setters as relatively expensive IO.
    /// </summary>
    public interface IRadiumLocalSettings
    {
        /// <summary>Encrypted central (Supabase / hosted Postgres) connection string. Env var MUSM_CENTRAL_DB overrides persisted value.</summary>
        string? CentralConnectionString { get; set; }
        /// <summary>Encrypted local / intranet PostgreSQL connection used for on-prem edit & study storage.</summary>
        string? LocalConnectionString { get; set; }

        /// <summary>Legacy alias (was used by older code). Do NOT rely on this for fallback; will be removed.</summary>
        [System.Obsolete("Use LocalConnectionString explicitly; this alias will be removed.")]
        string? ConnectionString { get; set; }

        /// <summary>Base URL for Snowstorm API, e.g. https://snowstorm.ihtsdotools.org/snowstorm/snomed-ct</summary>
        string? SnowstormBaseUrl { get; set; }

        /// <summary>Base URL for Radium API, e.g. http://127.0.0.1:5205/</summary>
        string? ApiBaseUrl { get; set; }

        /// <summary>Ordered modules executed when creating a new study (comma/semicolon delimited).</summary>
        string? AutomationNewStudySequence { get; set; }
        /// <summary>Ordered modules executed when adding a study (comma/semicolon delimited).</summary>
        string? AutomationAddStudySequence { get; set; }
        /// <summary>Ordered modules executed when global hotkey OpenStudy pressed and patient locked is OFF.</summary>
        string? AutomationShortcutOpenNew { get; set; }
        /// <summary>Ordered modules executed when global hotkey OpenStudy pressed and patient locked is ON but study opened is OFF.</summary>
        string? AutomationShortcutOpenAdd { get; set; }
        /// <summary>Ordered modules executed when global hotkey OpenStudy pressed and patient locked is ON and study opened is ON.</summary>
        string? AutomationShortcutOpenAfterOpen { get; set; }

        // Global hotkeys (user local)
        string? GlobalHotkeyOpenStudy { get; set; }
        string? GlobalHotkeySendStudy { get; set; }
        string? GlobalHotkeyToggleSyncText { get; set; }

        // Window placement (MainWindow): serialized as "Left,Top,Width,Height,State" where State=Normal|Maximized
        string? MainWindowPlacement { get; set; }
        
        /// <summary>Comma-separated list of modalities that should not update header fields (e.g., "XR,CR,DX").</summary>
        string? ModalitiesNoHeaderUpdate { get; set; }
        
        // Auto toggles for report field generation
        bool CopyStudyRemarkToChiefComplaint { get; set; }
        bool AutoChiefComplaint { get; set; }
        bool AutoPatientHistory { get; set; }
        bool AutoConclusion { get; set; }
        bool AutoFindingsProofread { get; set; }
        bool AutoConclusionProofread { get; set; }
        
        // NEW: Editor autofocus configuration
        /// <summary>Enable automatic focus to EditorFindings when configured keys are pressed while target element has focus.</summary>
        bool EditorAutofocusEnabled { get; set; }
        /// <summary>UI element bookmark name that triggers autofocus (e.g., "PacsViewerWindow").</summary>
        string? EditorAutofocusBookmark { get; set; }
        /// <summary>Comma-separated list of key types that trigger autofocus (e.g., "Alphabet,Numbers,Space").</summary>
        string? EditorAutofocusKeyTypes { get; set; }
        /// <summary>Window title that triggers autofocus (e.g., "INFINITT PACS"). If empty, uses bookmark-based detection.</summary>
        string? EditorAutofocusWindowTitle { get; set; }
        
        // NEW: Always on Top setting
        /// <summary>Keep the main window always on top of other windows.</summary>
        bool AlwaysOnTop { get; set; }
        
        // NEW: Session-based caching configuration
        /// <summary>
        /// Comma-separated list of bookmark names that should only be cached per-session (cleared on each automation run).
        /// Bookmarks NOT in this list are cached globally (persist across sessions for better performance).
        /// Use this for bookmarks that point to dynamic/changing elements (e.g., report text fields, worklist selections).
        /// </summary>
        string? SessionBasedCacheBookmarks { get; set; }
        
        // NEW: Header format template (Report Format tab)
        /// <summary>
        /// User-configurable header format template with placeholders:
        /// {Chief Complaint}, {Patient History Lines}, {Techniques}, {Comparison}
        /// </summary>
        string? HeaderFormatTemplate { get; set; }
    }
}
