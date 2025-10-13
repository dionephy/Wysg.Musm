using System.Threading.Tasks;
using Wysg.Musm.Radium.Services;
using Wysg.Musm.Radium.ViewModels;

namespace Wysg.Musm.Radium.Services.Procedures
{
    public sealed partial class NewStudyProcedure : INewStudyProcedure
    {
        private readonly ITechniqueRepository _techRepo;
        public NewStudyProcedure(ITechniqueRepository techRepo)
        {
            _techRepo = techRepo;
        }
    }
}
