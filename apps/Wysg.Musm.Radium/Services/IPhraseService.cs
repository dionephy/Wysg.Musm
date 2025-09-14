using System.Collections.Generic;
using System.Threading.Tasks;

namespace Wysg.Musm.Radium.Services
{
    public interface IPhraseService
    {
        Task<IReadOnlyList<string>> GetPhrasesForTenantAsync(long tenantId);
        Task<IReadOnlyList<string>> GetPhrasesByPrefixAsync(long tenantId, string prefix, int limit = 50);
    }
}
