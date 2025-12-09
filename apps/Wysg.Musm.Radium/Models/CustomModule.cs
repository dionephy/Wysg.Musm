using System;
using System.Collections.Generic;
using System.Linq;

namespace Wysg.Musm.Radium.Models
{
    /// <summary>
    /// Represents a custom automation module created by the user.
    /// Can be Run, Set, Abort/conditional, Goto, etc.
    /// </summary>
    public class CustomModule
    {
        public string Name { get; set; } = string.Empty;
        public CustomModuleType Type { get; set; }
        public string ProcedureName { get; set; } = string.Empty;
        public string? PropertyName { get; set; } // Only for Set type
        public string? TargetLabelName { get; set; } // Only for Goto type
        
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
        IfNot,
        Goto,
        IfMessageYes
    }
    
    /// <summary>
    /// Storage and management for custom modules and labels.
    /// </summary>
    public class CustomModuleStore
    {
        public List<CustomModule> Modules { get; set; } = new();
        public List<string> Labels { get; set; } = new();
        
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
                var store = System.Text.Json.JsonSerializer.Deserialize<CustomModuleStore>(json) ?? new CustomModuleStore();
                store.Modules ??= new List<CustomModule>();
                store.Labels ??= new List<string>();
                return store;
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
            if (Modules.Any(m => string.Equals(m.Name, module.Name, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException($"Module '{module.Name}' already exists");
            }
            
            if (Labels.Any(l => string.Equals(ToLabelDisplay(l), module.Name, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException($"Module '{module.Name}' conflicts with an existing label");
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
        
        public void AddLabel(string labelName)
        {
            var normalized = NormalizeLabelName(labelName);
            if (string.IsNullOrWhiteSpace(normalized))
                throw new ArgumentException("Label name cannot be empty", nameof(labelName));
            
            if (Labels.Any(l => string.Equals(l, normalized, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException($"Label '{labelName}' already exists");
            
            if (Modules.Any(m => string.Equals(m.Name, ToLabelDisplay(normalized), StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException($"Label '{labelName}' conflicts with an existing module");
            
            Labels.Add(normalized);
        }
        
        public bool RemoveLabel(string labelName)
        {
            var normalized = NormalizeLabelName(labelName);
            var existing = Labels.FirstOrDefault(l => string.Equals(l, normalized, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                Labels.Remove(existing);
                return true;
            }
            return false;
        }
        
        public bool LabelExists(string labelName)
        {
            var normalized = NormalizeLabelName(labelName);
            return Labels.Any(l => string.Equals(l, normalized, StringComparison.OrdinalIgnoreCase));
        }
        
        public static string NormalizeLabelName(string labelName)
        {
            if (string.IsNullOrWhiteSpace(labelName))
                return string.Empty;
            return labelName.Trim().TrimEnd(':').Trim();
        }
        
        public static string ToLabelDisplay(string labelName)
        {
            var normalized = NormalizeLabelName(labelName);
            return string.IsNullOrEmpty(normalized) ? string.Empty : normalized + ":";
        }
        
        public static bool TryParseLabelDisplay(string moduleName, out string labelName)
        {
            labelName = string.Empty;
            if (string.IsNullOrWhiteSpace(moduleName))
                return false;
            var trimmed = moduleName.Trim();
            if (!trimmed.EndsWith(":", StringComparison.Ordinal))
                return false;
            var candidate = trimmed[..^1].Trim();
            if (string.IsNullOrEmpty(candidate))
                return false;
            labelName = candidate;
            return true;
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
        public const string TempHeader = "Temp Header";
         
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
            PreviousSplitted,
            TempHeader
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
            TempHeader,
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
