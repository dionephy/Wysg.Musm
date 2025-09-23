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
        Task<IReadOnlyList<CommonPartRow>> GetCommonPartsAsync(int limit = 50);
        Task<IReadOnlyList<PlaybookMatchRow>> GetPlaybookMatchesAsync(IEnumerable<string> partNumbers);
        Task<IReadOnlyList<PlaybookPartDetailRow>> GetPlaybookPartsAsync(string loincNumber);
    }

    public sealed record StudynameRow(long Id, string Studyname);
    public sealed record PartRow(string PartNumber, string PartTypeName, string PartName);
    public sealed record MappingRow(string PartNumber, string PartSequenceOrder);
    public sealed record CommonPartRow(string PartNumber, string PartTypeName, string PartName, long UsageCount);
    public sealed record PlaybookMatchRow(string LoincNumber, string LongCommonName);
    public sealed record PlaybookPartDetailRow(string PartNumber, string PartName, string PartSequenceOrder);
}
