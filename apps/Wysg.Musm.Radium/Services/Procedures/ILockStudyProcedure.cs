using System.Threading.Tasks;
using Wysg.Musm.Radium.ViewModels;

namespace Wysg.Musm.Radium.Services.Procedures
{
    public interface ILockStudyProcedure
    {
        Task ExecuteAsync(MainViewModel vm);
    }
}
