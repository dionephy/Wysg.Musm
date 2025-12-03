using System.Threading.Tasks;
using Wysg.Musm.Radium.ViewModels;

namespace Wysg.Musm.Radium.Services.Procedures
{
    /// <summary>
    /// Interface for FetchPreviousStudiesProcedure.
    /// Fetches all previous studies for current patient from database and selects specific study/report.
    /// </summary>
    public interface IFetchPreviousStudiesProcedure
    {
        Task ExecuteAsync(MainViewModel vm);
    }
}
