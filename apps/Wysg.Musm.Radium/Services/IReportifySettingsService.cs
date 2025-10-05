using System.Threading.Tasks;

namespace Wysg.Musm.Radium.Services
{
    public interface IReportifySettingsService
    {
        Task<string?> GetSettingsJsonAsync(long accountId);
        Task<(string settingsJson,long rev)> UpsertAsync(long accountId, string settingsJson);
        Task<bool> DeleteAsync(long accountId);
    }
}
