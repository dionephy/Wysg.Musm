using System;
using System.Diagnostics;
using System.IO;

namespace Wysg.Musm.Radium.Services
{
    /// <summary>
    /// Centralized service for resolving per-account storage paths.
    /// All automation-related file paths (ui-procedures.json, bookmarks.json, custom-modules.json)
    /// are scoped per-account and per-PACS profile.
    /// 
    /// Path structure:
    /// %APPDATA%/Wysg.Musm/Radium/Accounts/{accountId}/Pacs/{pacsKey}/
    ///   - ui-procedures.json (custom procedures)
    ///   - bookmarks.json (UI bookmarks)
    ///   - custom-modules.json (automation modules)
    /// 
    /// This service dynamically resolves paths based on the current tenant context,
    /// ensuring that switching accounts loads the correct automation settings.
    /// </summary>
    public static class AccountStoragePaths
    {
        private static ITenantContext? _tenantContext;

        /// <summary>
        /// Initialize the path resolver with the tenant context from DI.
        /// Must be called once after login success (before any path resolution).
        /// </summary>
        public static void Initialize(ITenantContext tenantContext)
        {
            _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
            Debug.WriteLine($"[AccountStoragePaths] Initialized with tenant context (AccountId={tenantContext.AccountId}, PacsKey={tenantContext.CurrentPacsKey})");
        }

        /// <summary>
        /// Get the base directory for the current account and PACS profile.
        /// Creates the directory if it doesn't exist.
        /// </summary>
        public static string GetBaseDirectory()
        {
            // Try to get tenant context from DI if not initialized
            if (_tenantContext == null)
            {
                Debug.WriteLine("[AccountStoragePaths] WARNING: _tenantContext is null, attempting to resolve from App.Services");
                try
                {
                    if (System.Windows.Application.Current is App app)
                    {
                        _tenantContext = app.Services.GetService(typeof(ITenantContext)) as ITenantContext;
                        if (_tenantContext != null)
                        {
                            Debug.WriteLine($"[AccountStoragePaths] Resolved tenant context from DI: AccountId={_tenantContext.AccountId}, PacsKey={_tenantContext.CurrentPacsKey}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[AccountStoragePaths] Failed to resolve tenant context from DI: {ex.Message}");
                }
            }

            if (_tenantContext == null)
            {
                Debug.WriteLine("[AccountStoragePaths] ERROR: Tenant context still null after DI resolution attempt, using LEGACY fallback path");
                // CHANGED: Fall back to legacy Pacs/default_pacs instead of root Radium directory
                var legacyDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Wysg.Musm", "Radium", "Pacs", "default_pacs");
                Directory.CreateDirectory(legacyDir);
                Debug.WriteLine($"[AccountStoragePaths] Legacy fallback path: {legacyDir}");
                return legacyDir;
            }

            // CRITICAL: Read current values from tenant context each time (not cached!)
            long accountId = _tenantContext.AccountId;
            string pacsKey = _tenantContext.CurrentPacsKey;
            
            string accountSegment = accountId > 0 
                ? accountId.ToString() 
                : "account0";
            string safePacsKey = string.IsNullOrWhiteSpace(pacsKey) 
                ? "default_pacs" 
                : pacsKey;

            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Wysg.Musm", "Radium", "Accounts", accountSegment, "Pacs", SanitizeFileName(safePacsKey));
            
            Directory.CreateDirectory(dir);
            Debug.WriteLine($"[AccountStoragePaths] GetBaseDirectory: AccountId={accountId}, PacsKey={safePacsKey}, Path={dir}");
            return dir;
        }

        /// <summary>
        /// Get the path for ui-procedures.json for the current account and PACS.
        /// Handles migration from legacy path if needed.
        /// </summary>
        public static string GetProceduresPath()
        {
            var baseDir = GetBaseDirectory();
            var path = Path.Combine(baseDir, "ui-procedures.json");
            
            // Try migration from legacy location
            MigrateFromLegacyIfNeeded(path, "ui-procedures.json");
            
            Debug.WriteLine($"[AccountStoragePaths] GetProceduresPath: {path}");
            return path;
        }

        /// <summary>
        /// Get the path for bookmarks.json for the current account and PACS.
        /// Handles migration from legacy path if needed.
        /// </summary>
        public static string GetBookmarksPath()
        {
            var baseDir = GetBaseDirectory();
            var path = Path.Combine(baseDir, "bookmarks.json");
            
            // Try migration from legacy location
            MigrateFromLegacyIfNeeded(path, "bookmarks.json");
            
            Debug.WriteLine($"[AccountStoragePaths] GetBookmarksPath: {path}");
            return path;
        }

        /// <summary>
        /// Get the path for custom-modules.json for the current account and PACS.
        /// Handles migration from legacy path if needed.
        /// </summary>
        public static string GetCustomModulesPath()
        {
            var baseDir = GetBaseDirectory();
            var path = Path.Combine(baseDir, "custom-modules.json");
            
            // Try migration from legacy location
            MigrateFromLegacyIfNeeded(path, "custom-modules.json");
            
            Debug.WriteLine($"[AccountStoragePaths] GetCustomModulesPath: {path}");
            return path;
        }

        /// <summary>
        /// Get the path for automation.json for the current account and PACS.
        /// Handles migration from legacy path if needed.
        /// </summary>
        public static string GetAutomationPath()
        {
            var baseDir = GetBaseDirectory();
            var path = Path.Combine(baseDir, "automation.json");
            
            // Try migration from legacy location
            MigrateFromLegacyIfNeeded(path, "automation.json");
            
            Debug.WriteLine($"[AccountStoragePaths] GetAutomationPath: {path}");
            return path;
        }

        /// <summary>
        /// Migrate a file from the legacy (non-account-scoped) location if the new file doesn't exist.
        /// </summary>
        private static void MigrateFromLegacyIfNeeded(string newPath, string fileName)
        {
            if (File.Exists(newPath)) return;
            
            // Try legacy location (before account separation)
            string pacsKey = _tenantContext != null && !string.IsNullOrWhiteSpace(_tenantContext.CurrentPacsKey) 
                ? _tenantContext.CurrentPacsKey 
                : "default_pacs";
                
            var legacyPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Wysg.Musm", "Radium", "Pacs", SanitizeFileName(pacsKey), fileName);
            
            if (File.Exists(legacyPath))
            {
                try
                {
                    var targetDir = Path.GetDirectoryName(newPath);
                    if (!string.IsNullOrEmpty(targetDir))
                        Directory.CreateDirectory(targetDir);
                    
                    File.Copy(legacyPath, newPath, overwrite: false);
                    Debug.WriteLine($"[AccountStoragePaths] Migrated {fileName} from legacy location: {legacyPath} -> {newPath}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[AccountStoragePaths] Migration failed for {fileName}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Configure all path overrides for automation services.
        /// Call this after Initialize() to set up all services with dynamic paths.
        /// </summary>
        public static void ConfigurePathOverrides()
        {
            // ProcedureExecutor - uses dynamic resolution each time
            ProcedureExecutor.SetProcPathOverride(() => GetProceduresPath());
            
            // UiBookmarks - uses dynamic resolution each time
            UiBookmarks.GetStorePathOverride = () => GetBookmarksPath();
            
            // CustomModuleStore - uses dynamic resolution each time
            Wysg.Musm.Radium.Models.CustomModuleStore.GetStorePathOverride = () => GetCustomModulesPath();
            
            Debug.WriteLine("[AccountStoragePaths] Path overrides configured for all automation services");
            
            // Log current paths for verification
            Debug.WriteLine($"[AccountStoragePaths] Procedures path: {GetProceduresPath()}");
            Debug.WriteLine($"[AccountStoragePaths] Bookmarks path: {GetBookmarksPath()}");
            Debug.WriteLine($"[AccountStoragePaths] CustomModules path: {GetCustomModulesPath()}");
        }

        private static string SanitizeFileName(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            return string.Join("_", name.Split(invalid, StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.');
        }
    }
}
