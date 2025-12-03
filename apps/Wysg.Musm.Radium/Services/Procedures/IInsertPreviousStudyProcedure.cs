using System.Threading.Tasks;
using Wysg.Musm.Radium.ViewModels;

namespace Wysg.Musm.Radium.Services.Procedures
{
    /// <summary>
    /// Interface for the InsertPreviousStudy built-in module.
    /// Inserts a previous study to the database using settable variables.
    /// </summary>
    public interface IInsertPreviousStudyProcedure
    {
        /// <summary>
        /// Executes the InsertPreviousStudy procedure.
        /// Validates required variables (Current Patient Number, Previous Study Studyname, Previous Study Datetime)
        /// and inserts the study to med.rad_study if it doesn't already exist.
        /// </summary>
        /// <param name="vm">MainViewModel containing the settable variables</param>
        Task ExecuteAsync(MainViewModel vm);
    }
}
