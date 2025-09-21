using System.Collections.Generic;
using System.Threading.Tasks;

namespace Wysg.Musm.Radium.Services
{
    public interface IStudynameLoincRepository
    {
        Task<IReadOnlyList<StudynameRow>> GetStudynamesAsync();
        Task<long> EnsureStudynameAsync(string studyname);
        Task<IReadOnlyList<PartRow>> GetPartsAsync();
        Task<IReadOnlyList<MappingRow>> GetMappingsAsync(long studynameId);
        Task SaveMappingsAsync(long studynameId, IEnumerable<MappingRow> items);
    }

    public sealed record StudynameRow(long Id, string Studyname);
    public sealed record PartRow(string PartNumber, string PartTypeName, string PartName);
    public sealed record MappingRow(string PartNumber, string PartSequenceOrder);
}
