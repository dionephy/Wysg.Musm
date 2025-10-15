using System.Collections.Generic;
using System.Threading.Tasks;

namespace Wysg.Musm.Radium.Services
{
    public interface ISnowstormClient
    {
        Task<IReadOnlyList<SnomedConcept>> SearchConceptsAsync(string query, int limit = 50);
    }
}
