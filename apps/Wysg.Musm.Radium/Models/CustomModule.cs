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
        AbortIf
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
    /// </summary>
    public static class CustomModuleProperties
    {
        public const string CurrentPatientName = "Current Patient Name";
        public const string CurrentPatientNumber = "Current Patient Number";
        public const string CurrentPatientAge = "Current Patient Age";
        public const string CurrentPatientSex = "Current Patient Sex";
        public const string CurrentStudyStudyname = "Current Study Studyname";
        public const string CurrentStudyDatetime = "Current Study Datetime";
        public const string CurrentStudyRemark = "Current Study Remark";
        public const string CurrentPatientRemark = "Current Patient Remark";
        public const string PreviousStudyStudyname = "Previous Study Studyname";
        public const string PreviousStudyDatetime = "Previous Study Datetime";
        public const string PreviousStudyReportDatetime = "Previous Study Report Datetime";
        public const string PreviousStudyReportReporter = "Previous Study Report Reporter";
        public const string PreviousStudyReportHeaderAndFindings = "Previous Study Report Header and Findings";
        public const string PreviousStudyReportConclusion = "Previous Study Report Conclusion";
        
        public static readonly string[] AllProperties = new[]
        {
            CurrentPatientName,
            CurrentPatientNumber,
            CurrentPatientAge,
            CurrentPatientSex,
            CurrentStudyStudyname,
            CurrentStudyDatetime,
            CurrentStudyRemark,
            CurrentPatientRemark,
            PreviousStudyStudyname,
            PreviousStudyDatetime,
            PreviousStudyReportDatetime,
            PreviousStudyReportReporter,
            PreviousStudyReportHeaderAndFindings,
            PreviousStudyReportConclusion
        };
    }
}
