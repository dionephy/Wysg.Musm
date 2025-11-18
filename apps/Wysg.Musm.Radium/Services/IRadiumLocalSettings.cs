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
    }
}
