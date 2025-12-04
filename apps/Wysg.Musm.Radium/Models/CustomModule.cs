using System;
using System.Collections.Generic;
using System.Linq;

namespace Wysg.Musm.Radium.Models
{
    /// <summary>
    /// Represents a custom automation module created by the user.
    /// Can be Run, Set, or Abort If type.
    /// </summary>
    public class CustomModule
    {
        public string Name { get; set; } = string.Empty;
        public CustomModuleType Type { get; set; }
        public string ProcedureName { get; set; } = string.Empty;
        public string? PropertyName { get; set; } // Only for Set type
        
        public override string ToString() => Name;
    }
    
    /// <summary>
    /// Type of custom module operation.
    /// </summary>
    public enum CustomModuleType
    {
        Run,
        Set,
        AbortIf,
        If,
        IfNot
    }
    
    /// <summary>
    /// Storage and management for custom modules.
    /// </summary>
    public class CustomModuleStore
    {
        public List<CustomModule> Modules { get; set; } = new();
        
        private static string GetStorePath()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var dir = System.IO.Path.Combine(appData, "Wysg.Musm", "Radium");
            System.IO.Directory.CreateDirectory(dir);
            return System.IO.Path.Combine(dir, "custom-modules.json");
        }
        
        public static CustomModuleStore Load()
        {
            try
            {
                var path = GetStorePath();
                if (!System.IO.File.Exists(path))
                    return new CustomModuleStore();
                
                var json = System.IO.File.ReadAllText(path);
                var store = System.Text.Json.JsonSerializer.Deserialize<CustomModuleStore>(json);
                return store ?? new CustomModuleStore();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CustomModuleStore] Load error: {ex.Message}");
                return new CustomModuleStore();
            }
        }
        
        public static void Save(CustomModuleStore store)
        {
            try
            {
                var path = GetStorePath();
                var json = System.Text.Json.JsonSerializer.Serialize(store, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                });
                System.IO.File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CustomModuleStore] Save error: {ex.Message}");
            }
        }
        
        public void AddModule(CustomModule module)
        {
            // Check for duplicate names
            if (Modules.Any(m => string.Equals(m.Name, module.Name, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException($"Module '{module.Name}' already exists");
            }
            
            Modules.Add(module);
        }
        
        public bool RemoveModule(string name)
        {
            var module = Modules.FirstOrDefault(m => string.Equals(m.Name, name, StringComparison.OrdinalIgnoreCase));
            if (module != null)
            {
                Modules.Remove(module);
                return true;
            }
            return false;
        }
        
        public CustomModule? GetModule(string name)
        {
            return Modules.FirstOrDefault(m => string.Equals(m.Name, name, StringComparison.OrdinalIgnoreCase));
        }
    }
    
    /// <summary>
    /// Property options for Set type custom modules.
    /// These properties can be SET by custom modules (via Set type)
    /// and READ in procedure operations (via Var type).
    /// </summary>
    public static class CustomModuleProperties
    {
        // Current patient/study properties (read/write)
        public const string CurrentPatientName = "Current Patient Name";
        public const string CurrentPatientNumber = "Current Patient Number";
        public const string CurrentPatientAge = "Current Patient Age";
        public const string CurrentPatientSex = "Current Patient Sex";
        public const string CurrentStudyStudyname = "Current Study Studyname";
        public const string CurrentStudyDatetime = "Current Study Datetime";
        public const string CurrentStudyRemark = "Current Study Remark";
        public const string CurrentPatientRemark = "Current Patient Remark";
        
        // Previous study properties (read/write)
        public const string PreviousStudyStudyname = "Previous Study Studyname";
        public const string PreviousStudyDatetime = "Previous Study Datetime";
        public const string PreviousStudyReportDatetime = "Previous Study Report Datetime";
        public const string PreviousStudyReportReporter = "Previous Study Report Reporter";
        public const string PreviousStudyReportHeaderAndFindings = "Previous Study Report Header and Findings";
        public const string PreviousStudyReportConclusion = "Previous Study Report Conclusion";
        
        // Toggle properties (read/write) - boolean values returned as "true"/"false"
        public const string StudyLocked = "Study Locked";
        public const string StudyOpened = "Study Opened";
        public const string CurrentReportified = "Current Reportified";
        public const string CurrentProofread = "Current Proofread";
        public const string PreviousProofread = "Previous Proofread";
        public const string PreviousSplitted = "Previous Splitted";
        
        // Current editor text properties (read-only)
        public const string CurrentHeader = "Current Header";
        public const string CurrentFindings = "Current Findings";
        public const string CurrentConclusion = "Current Conclusion";
        
        /// <summary>
        /// All properties available for SET operations (in custom module Property dropdown).
        /// </summary>
        public static readonly string[] AllProperties = new[]
        {
            // Patient/study
            CurrentPatientName,
            CurrentPatientNumber,
            CurrentPatientAge,
            CurrentPatientSex,
            CurrentStudyStudyname,
            CurrentStudyDatetime,
            CurrentStudyRemark,
            CurrentPatientRemark,
            // Previous study
            PreviousStudyStudyname,
            PreviousStudyDatetime,
            PreviousStudyReportDatetime,
            PreviousStudyReportReporter,
            PreviousStudyReportHeaderAndFindings,
            PreviousStudyReportConclusion,
            // Toggles (writable)
            StudyLocked,
            StudyOpened,
            CurrentReportified,
            CurrentProofread,
            PreviousProofread,
            PreviousSplitted
        };
        
        /// <summary>
        /// All properties available for READ operations (in procedure Var dropdown).
        /// Includes read-only properties like editor text.
        /// </summary>
        public static readonly string[] AllReadableProperties = new[]
        {
            // Patient/study
            CurrentPatientName,
            CurrentPatientNumber,
            CurrentPatientAge,
            CurrentPatientSex,
            CurrentStudyStudyname,
            CurrentStudyDatetime,
            CurrentStudyRemark,
            CurrentPatientRemark,
            // Previous study
            PreviousStudyStudyname,
            PreviousStudyDatetime,
            PreviousStudyReportDatetime,
            PreviousStudyReportReporter,
            PreviousStudyReportHeaderAndFindings,
            PreviousStudyReportConclusion,
            // Toggles
            StudyLocked,
            StudyOpened,
            CurrentReportified,
            CurrentProofread,
            PreviousProofread,
            PreviousSplitted,
            // Read-only editor text
            CurrentHeader,
            CurrentFindings,
            CurrentConclusion
        };
        
        /// <summary>
        /// Check if a property name is a built-in property (for Var resolution).
        /// </summary>
        public static bool IsBuiltInProperty(string name)
        {
            return Array.Exists(AllReadableProperties, p => string.Equals(p, name, StringComparison.OrdinalIgnoreCase));
        }
    }
}
