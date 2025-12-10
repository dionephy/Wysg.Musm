using System.Threading.Tasks;
using Wysg.Musm.Radium.ViewModels;

namespace Wysg.Musm.Radium.Services.Procedures
{
    /// <summary>
    /// Interface for the InsertCurrentStudyReport built-in module.
    /// Inserts the current study report to med.rad_report if it doesn't already exist.
    /// </summary>
    public interface IInsertCurrentStudyReportProcedure
    {
        Task ExecuteAsync(MainViewModel vm);
    }
}
