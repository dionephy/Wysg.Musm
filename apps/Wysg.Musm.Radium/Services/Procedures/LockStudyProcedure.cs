using System.Threading.Tasks;
using Wysg.Musm.Radium.ViewModels;

namespace Wysg.Musm.Radium.Services.Procedures
{
    public sealed class LockStudyProcedure : ILockStudyProcedure
    {
        public Task ExecuteAsync(MainViewModel vm)
        {
            if (vm == null) return Task.CompletedTask;
            vm.PatientLocked = true; // Only lock, no clearing
            vm.SetStatusInternal("Study locked");
            return Task.CompletedTask;
        }
    }
}
