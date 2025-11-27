using System.Threading.Tasks;
using Wysg.Musm.Radium.ViewModels;

namespace Wysg.Musm.Radium.Services.Procedures
{
    /// <summary>
    /// Interface for SetCurrentStudyTechniques procedure that auto-fills study techniques
    /// based on the current studyname (assumes studyname is already set by other modules).
    /// </summary>
    public interface ISetCurrentStudyTechniquesProcedure
    {
        Task ExecuteAsync(MainViewModel vm);
    }
}
