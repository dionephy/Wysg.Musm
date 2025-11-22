using Wysg.Musm.Radium.Api.Models.Dtos;

namespace Wysg.Musm.Radium.Api.Services
{
    public interface ISnippetService
    {
        Task<IReadOnlyList<SnippetDto>> GetAllByAccountAsync(long accountId);
        Task<SnippetDto?> GetByIdAsync(long accountId, long snippetId);
        Task<SnippetDto> UpsertAsync(long accountId, UpsertSnippetRequest request);
        Task<SnippetDto?> ToggleActiveAsync(long accountId, long snippetId);
        Task<bool> DeleteAsync(long accountId, long snippetId);
    }
}
