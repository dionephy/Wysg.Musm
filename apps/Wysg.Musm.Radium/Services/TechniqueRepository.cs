using System.Collections.Generic;
using System.Threading.Tasks;

namespace Wysg.Musm.Radium.Services
{
    public partial interface ITechniqueRepository
    {
        Task<IReadOnlyList<SimpleTextRow>> GetPrefixesAsync();
        Task<IReadOnlyList<SimpleTextRow>> GetTechsAsync();
        Task<IReadOnlyList<SimpleTextRow>> GetSuffixesAsync();
        Task<long> AddPrefixAsync(string text);
        Task<long> AddTechAsync(string text);
        Task<long> AddSuffixAsync(string text);
        Task<IReadOnlyList<StudynameCombinationRow>> GetCombinationsForStudynameAsync(long studynameId);
        Task SetDefaultForStudynameAsync(long studynameId, long combinationId);
        Task<long> EnsureTechniqueAsync(long? prefixId, long techId, long? suffixId);
        Task<long> CreateCombinationAsync(string? name);
        Task AddCombinationItemsAsync(long combinationId, IEnumerable<(long techniqueId, int sequenceOrder)> items);
        Task LinkStudynameCombinationAsync(long studynameId, long combinationId, bool isDefault = false);
        Task DeleteStudynameCombinationLinkAsync(long studynameId, long combinationId);
    }

    public sealed record SimpleTextRow(long Id, string Text);
    public sealed record StudynameCombinationRow(long CombinationId, string Display, bool IsDefault);
}
