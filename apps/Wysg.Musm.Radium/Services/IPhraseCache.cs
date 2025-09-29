using System.Collections.Generic;

namespace Wysg.Musm.Radium.Services
{
    public interface IPhraseCache
    {
        IReadOnlyList<string> Get(long tenantId);
        void Set(long tenantId, IReadOnlyList<string> phrases);
        bool Has(long tenantId);
        void Clear(long tenantId);
    }
}
