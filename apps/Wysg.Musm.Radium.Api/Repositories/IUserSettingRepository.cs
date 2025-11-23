using Wysg.Musm.Radium.Api.Models.Dtos;

namespace Wysg.Musm.Radium.Api.Repositories
{
    public interface IUserSettingRepository
    {
        Task<UserSettingDto?> GetByAccountAsync(long accountId);
        Task<UserSettingDto> UpsertAsync(long accountId, UpdateUserSettingRequest request);
        Task<bool> DeleteAsync(long accountId);
    }
}
