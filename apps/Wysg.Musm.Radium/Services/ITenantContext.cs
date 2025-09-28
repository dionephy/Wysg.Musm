namespace Wysg.Musm.Radium.Services
{
    public interface ITenantContext
    {
        long TenantId { get; set; }
        string TenantCode { get; set; }
        // New: central DB now uses account_id; keep backward compatibility
        long AccountId { get; set; }
    }
}
