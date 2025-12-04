using System.Threading.Tasks;
using Wysg.Musm.Radium.ViewModels;

namespace Wysg.Musm.Radium.Services.Procedures
{
    public sealed class SetStudyOpenedProcedure : ISetStudyOpenedProcedure
    {
        public Task ExecuteAsync(MainViewModel vm)
        {
            if (vm == null) return Task.CompletedTask;
            vm.StudyOpened = true; // Toggle on Study opened
            vm.SetStatusInternal("[SetStudyOpened] Done.");
            return Task.CompletedTask;
        }
    }
}
