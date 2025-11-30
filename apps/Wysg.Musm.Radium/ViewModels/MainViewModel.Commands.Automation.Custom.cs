using System;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Wysg.Musm.Radium.ViewModels
{
    /// <summary>
    /// Partial: Custom automation module execution.
    /// Contains methods for executing custom modules and setting properties.
    /// </summary>
    public partial class MainViewModel
    {
        private async Task RunCustomModuleAsync(Wysg.Musm.Radium.Models.CustomModule module)
        {
            try
            {
                Debug.WriteLine($"[CustomModule] Executing '{module.Name}' type={module.Type} proc={module.ProcedureName}");
                
                // Run the procedure and get result using ProcedureExecutor
                var result = await Services.ProcedureExecutor.ExecuteAsync(module.ProcedureName);
                
                switch (module.Type)
                {
                    case Wysg.Musm.Radium.Models.CustomModuleType.Run:
                        // Just run the procedure, result ignored
                        SetStatus($"Custom module '{module.Name}' executed");
                        break;
                        
                    case Wysg.Musm.Radium.Models.CustomModuleType.AbortIf:
                        // Abort if result is true/non-empty
                        var shouldAbort = !string.IsNullOrWhiteSpace(result) && 
                                          !string.Equals(result, "false", StringComparison.OrdinalIgnoreCase);
                        if (shouldAbort)
                        {
                            SetStatus($"Custom module '{module.Name}' aborted sequence", true);
                            throw new OperationCanceledException($"Aborted by {module.Name}");
                        }
                        SetStatus($"Custom module '{module.Name}' condition not met, continuing");
                        break;
                        
                    case Wysg.Musm.Radium.Models.CustomModuleType.Set:
                        // Set property value
                        if (string.IsNullOrWhiteSpace(module.PropertyName))
                        {
                            Debug.WriteLine($"[CustomModule] Set module '{module.Name}' has no property name");
                            break;
                        }
                        
                        SetPropertyValue(module.PropertyName, result ?? string.Empty);
                        SetStatus($"Custom module '{module.Name}' set {module.PropertyName} = {result}");
                        break;
                }
            }
            catch (OperationCanceledException)
            {
                throw; // Propagate abort
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CustomModule] Error executing '{module.Name}': {ex.Message}");
                SetStatus($"Custom module '{module.Name}' failed: {ex.Message}", true);
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
                    
                default:
                    Debug.WriteLine($"[CustomModule] Unknown property: {propertyName}");
                    break;
            }
        }
    }
}
