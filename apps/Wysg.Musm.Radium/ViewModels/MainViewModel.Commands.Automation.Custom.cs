using System;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Wysg.Musm.Radium.ViewModels
{
    /// <summary>
    /// Partial: Custom automation module execution.
    /// Contains methods for executing custom modules and setting/getting properties.
    /// </summary>
    public partial class MainViewModel
    {
        private async Task RunCustomModuleAsync(Wysg.Musm.Radium.Models.CustomModule module)
        {
            var sw = Stopwatch.StartNew();
            
            try
            {
                Debug.WriteLine($"[CustomModule] Executing '{module.Name}' type={module.Type} proc={module.ProcedureName}");
                
                // Run the procedure and get result using ProcedureExecutor
                var result = await Services.ProcedureExecutor.ExecuteAsync(module.ProcedureName);
                
                sw.Stop();
                
                switch (module.Type)
                {
                    case Wysg.Musm.Radium.Models.CustomModuleType.Run:
                        // Just run the procedure, result ignored
                        SetStatus($"[{module.Name}] Done. ({sw.ElapsedMilliseconds} ms)");
                        break;
                        
                    case Wysg.Musm.Radium.Models.CustomModuleType.AbortIf:
                        // Abort if result is true/non-empty
                        var shouldAbort = !string.IsNullOrWhiteSpace(result) && 
                                          !string.Equals(result, "false", StringComparison.OrdinalIgnoreCase);
                        if (shouldAbort)
                        {
                            SetStatus($"[{module.Name}] Aborted sequence. ({sw.ElapsedMilliseconds} ms)", true);
                            throw new OperationCanceledException($"Aborted by {module.Name}");
                        }
                        SetStatus($"[{module.Name}] Condition not met, continuing. ({sw.ElapsedMilliseconds} ms)");
                        break;
                        
                    case Wysg.Musm.Radium.Models.CustomModuleType.Set:
                        // Set property value
                        if (string.IsNullOrWhiteSpace(module.PropertyName))
                        {
                            Debug.WriteLine($"[CustomModule] Set module '{module.Name}' has no property name");
                            break;
                        }
                        
                        SetPropertyValue(module.PropertyName, result ?? string.Empty);
                        
                        // Format: [ModuleName] PropertyName = value (N ms)
                        var formattedValue = FormatValueForStatus(result);
                        SetStatus($"[{module.Name}] {module.PropertyName} = {formattedValue} ({sw.ElapsedMilliseconds} ms)");
                        break;
                }
            }
            catch (OperationCanceledException)
            {
                throw; // Propagate abort
            }
            catch (Exception ex)
            {
                sw.Stop();
                Debug.WriteLine($"[CustomModule] Error executing '{module.Name}': {ex.Message}");
                SetStatus($"[{module.Name}] Error: {ex.Message} ({sw.ElapsedMilliseconds} ms)", true);
                throw;
            }
        }

        private void SetPropertyValue(string propertyName, string value)
        {
            switch (propertyName)
            {
                case Wysg.Musm.Radium.Models.CustomModuleProperties.CurrentPatientName:
                    PatientName = value;
                    break;
                case Wysg.Musm.Radium.Models.CustomModuleProperties.CurrentPatientNumber:
                    PatientNumber = value;
                    break;
                case Wysg.Musm.Radium.Models.CustomModuleProperties.CurrentPatientAge:
                    PatientAge = value;
                    break;
                case Wysg.Musm.Radium.Models.CustomModuleProperties.CurrentPatientSex:
                    PatientSex = value;
                    break;
                case Wysg.Musm.Radium.Models.CustomModuleProperties.CurrentStudyStudyname:
                    StudyName = value;
                    break;
                case Wysg.Musm.Radium.Models.CustomModuleProperties.CurrentStudyDatetime:
                    if (DateTime.TryParse(value, out var dt))
                        StudyDateTime = dt.ToString("yyyy-MM-dd HH:mm:ss");
                    break;
                case Wysg.Musm.Radium.Models.CustomModuleProperties.CurrentStudyRemark:
                    StudyRemark = value;
                    break;
                case Wysg.Musm.Radium.Models.CustomModuleProperties.CurrentPatientRemark:
                    PatientRemark = value;
                    break;
                    
                // Previous study properties - store in temporary fields
                case Wysg.Musm.Radium.Models.CustomModuleProperties.PreviousStudyStudyname:
                    TempPreviousStudyStudyname = value;
                    break;
                case Wysg.Musm.Radium.Models.CustomModuleProperties.PreviousStudyDatetime:
                    if (DateTime.TryParse(value, out var pdt))
                        TempPreviousStudyDatetime = pdt;
                    break;
                case Wysg.Musm.Radium.Models.CustomModuleProperties.PreviousStudyReportDatetime:
                    if (DateTime.TryParse(value, out var prdt))
                        TempPreviousStudyReportDatetime = prdt;
                    break;
                case Wysg.Musm.Radium.Models.CustomModuleProperties.PreviousStudyReportReporter:
                    TempPreviousStudyReportReporter = value;
                    break;
                case Wysg.Musm.Radium.Models.CustomModuleProperties.PreviousStudyReportHeaderAndFindings:
                    TempPreviousStudyReportHeaderAndFindings = value;
                    break;
                case Wysg.Musm.Radium.Models.CustomModuleProperties.PreviousStudyReportConclusion:
                    TempPreviousStudyReportConclusion = value;
                    break;
                    
                // Toggle properties - parse boolean from string
                case Wysg.Musm.Radium.Models.CustomModuleProperties.StudyLocked:
                    PatientLocked = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
                    break;
                case Wysg.Musm.Radium.Models.CustomModuleProperties.StudyOpened:
                    StudyOpened = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
                    break;
                case Wysg.Musm.Radium.Models.CustomModuleProperties.CurrentReportified:
                    Reportified = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
                    break;
                case Wysg.Musm.Radium.Models.CustomModuleProperties.CurrentProofread:
                    ProofreadMode = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
                    break;
                case Wysg.Musm.Radium.Models.CustomModuleProperties.PreviousProofread:
                    PreviousProofreadMode = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
                    break;
                case Wysg.Musm.Radium.Models.CustomModuleProperties.PreviousSplitted:
                    PreviousReportSplitted = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
                    break;
                    
                default:
                    Debug.WriteLine($"[CustomModule] Unknown property: {propertyName}");
                    break;
            }
        }
        
        /// <summary>
        /// Get the value of a built-in property by name.
        /// Returns null if property is not recognized.
        /// Boolean properties return "true" or "false" as strings.
        /// </summary>
        internal string? GetPropertyValue(string propertyName)
        {
            switch (propertyName)
            {
                // Patient/study properties
                case Wysg.Musm.Radium.Models.CustomModuleProperties.CurrentPatientName:
                    return PatientName;
                case Wysg.Musm.Radium.Models.CustomModuleProperties.CurrentPatientNumber:
                    return PatientNumber;
                case Wysg.Musm.Radium.Models.CustomModuleProperties.CurrentPatientAge:
                    return PatientAge;
                case Wysg.Musm.Radium.Models.CustomModuleProperties.CurrentPatientSex:
                    return PatientSex;
                case Wysg.Musm.Radium.Models.CustomModuleProperties.CurrentStudyStudyname:
                    return StudyName;
                case Wysg.Musm.Radium.Models.CustomModuleProperties.CurrentStudyDatetime:
                    return StudyDateTime;
                case Wysg.Musm.Radium.Models.CustomModuleProperties.CurrentStudyRemark:
                    return StudyRemark;
                case Wysg.Musm.Radium.Models.CustomModuleProperties.CurrentPatientRemark:
                    return PatientRemark;
                    
                // Previous study properties
                case Wysg.Musm.Radium.Models.CustomModuleProperties.PreviousStudyStudyname:
                    return TempPreviousStudyStudyname;
                case Wysg.Musm.Radium.Models.CustomModuleProperties.PreviousStudyDatetime:
                    return TempPreviousStudyDatetime?.ToString("yyyy-MM-dd HH:mm:ss");
                case Wysg.Musm.Radium.Models.CustomModuleProperties.PreviousStudyReportDatetime:
                    return TempPreviousStudyReportDatetime?.ToString("yyyy-MM-dd HH:mm:ss");
                case Wysg.Musm.Radium.Models.CustomModuleProperties.PreviousStudyReportReporter:
                    return TempPreviousStudyReportReporter;
                case Wysg.Musm.Radium.Models.CustomModuleProperties.PreviousStudyReportHeaderAndFindings:
                    return TempPreviousStudyReportHeaderAndFindings;
                case Wysg.Musm.Radium.Models.CustomModuleProperties.PreviousStudyReportConclusion:
                    return TempPreviousStudyReportConclusion;
                    
                // Toggle properties - return "true" or "false"
                case Wysg.Musm.Radium.Models.CustomModuleProperties.StudyLocked:
                    return PatientLocked ? "true" : "false";
                case Wysg.Musm.Radium.Models.CustomModuleProperties.StudyOpened:
                    return StudyOpened ? "true" : "false";
                case Wysg.Musm.Radium.Models.CustomModuleProperties.CurrentReportified:
                    return Reportified ? "true" : "false";
                case Wysg.Musm.Radium.Models.CustomModuleProperties.CurrentProofread:
                    return ProofreadMode ? "true" : "false";
                case Wysg.Musm.Radium.Models.CustomModuleProperties.PreviousProofread:
                    return PreviousProofreadMode ? "true" : "false";
                case Wysg.Musm.Radium.Models.CustomModuleProperties.PreviousSplitted:
                    return PreviousReportSplitted ? "true" : "false";
                    
                // Read-only editor text properties
                case Wysg.Musm.Radium.Models.CustomModuleProperties.CurrentHeader:
                    return HeaderText;
                case Wysg.Musm.Radium.Models.CustomModuleProperties.CurrentFindings:
                    return FindingsText;
                case Wysg.Musm.Radium.Models.CustomModuleProperties.CurrentConclusion:
                    return ConclusionText;
                    
                default:
                    Debug.WriteLine($"[CustomModule] Unknown property for get: {propertyName}");
                    return null;
            }
        }
    }
}
