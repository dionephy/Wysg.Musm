using Wysg.Musm.Radium.Models;

namespace Wysg.Musm.Radium.Services
{
    public interface ITenantService
    {
        Task<TenantModel?> GetTenantByCodeAsync(string tenantCode);
        Task<bool> ValidateLoginAsync(LoginRequest request);
    }
}