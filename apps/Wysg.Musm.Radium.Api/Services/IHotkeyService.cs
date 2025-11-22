using Wysg.Musm.Radium.Api.Models.Dtos;

namespace Wysg.Musm.Radium.Api.Services
{
    public interface IHotkeyService
    {
        Task<IReadOnlyList<HotkeyDto>> GetAllByAccountAsync(long accountId);
        Task<HotkeyDto?> GetByIdAsync(long accountId, long hotkeyId);
        Task<HotkeyDto> UpsertAsync(long accountId, UpsertHotkeyRequest request);
        Task<HotkeyDto?> ToggleActiveAsync(long accountId, long hotkeyId);
        Task<bool> DeleteAsync(long accountId, long hotkeyId);
    }
}
