using System.Threading.Tasks;
using Wysg.Musm.Radium.ViewModels;

namespace Wysg.Musm.Radium.Services.Procedures
{
    public interface INewStudyProcedure
    {
        Task ExecuteAsync(MainViewModel vm);
    }
}