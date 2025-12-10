using System.Threading.Tasks;
using Wysg.Musm.Radium.ViewModels;

namespace Wysg.Musm.Radium.Services.Procedures
{
    /// <summary>
    /// Interface for the InsertCurrentStudy built-in module.
    /// Ensures the current study exists in the database based on current context variables.
    /// </summary>
    public interface IInsertCurrentStudyProcedure
    {
        /// <summary>
        /// Validates required variables (Current Patient Number, Current Study Studyname, Current Study Datetime)
        /// and inserts the study to med.rad_study if it doesn't already exist.
        /// </summary>
        Task ExecuteAsync(MainViewModel vm);
    }
}
