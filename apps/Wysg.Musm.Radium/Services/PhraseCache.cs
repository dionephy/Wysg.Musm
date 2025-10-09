using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Wysg.Musm.Radium.Services
{
    public sealed class PhraseCache : IPhraseCache
    {
        private readonly ConcurrentDictionary<long, IReadOnlyList<string>> _map = new();
        public IReadOnlyList<string> Get(long tenantId) => _map.TryGetValue(tenantId, out var v) ? v : new List<string>();
        public void Set(long tenantId, IReadOnlyList<string> phrases) => _map[tenantId] = phrases;
        public bool Has(long tenantId) => _map.ContainsKey(tenantId);
        public void Clear(long tenantId) => _map.TryRemove(tenantId, out _);
        public void ClearAll() => _map.Clear();
    }
}
