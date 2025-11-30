using System;

namespace Wysg.Musm.Radium.ViewModels
{
    /// <summary>
    /// Partial: Automation module execution - SPLIT INTO MULTIPLE FILES
    /// 
    /// This file has been split into the following specialized files:
    /// - MainViewModel.Commands.Automation.Core.cs: Core orchestration (RunNewStudyProcedureAsync, RunModulesSequentially)
    /// - MainViewModel.Commands.Automation.Acquire.cs: Data acquisition (AcquireStudyRemarkAsync, AcquirePatientRemarkAsync)
    /// - MainViewModel.Commands.Automation.Screen.cs: Screen/worklist operations (OpenStudy, SetCurrentInMainScreen, etc.)
    /// - MainViewModel.Commands.Automation.Report.cs: Report operations (GetUntilReportDateTime, GetReportedReport, SendReport)
    /// - MainViewModel.Commands.Automation.Database.cs: Database operations (SaveCurrentStudyToDB, SavePreviousStudyToDB)
    /// - MainViewModel.Commands.Automation.Custom.cs: Custom module execution (RunCustomModuleAsync, SetPropertyValue)
    /// 
    /// This file is kept for historical reference and to maintain the namespace structure.
    /// All functionality has been moved to the specialized files listed above.
    /// </summary>
    public partial class MainViewModel
    {
        // All automation methods have been moved to specialized partial files.
        // See file header comment for the complete list of new files.
    }
}
