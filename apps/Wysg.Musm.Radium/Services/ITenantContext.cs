namespace Wysg.Musm.Radium.Services
{
    public interface ITenantContext
    {
        long TenantId { get; set; }
        string TenantCode { get; set; }
        long AccountId { get; set; }
        string? ReportifySettingsJson { get; set; }
        // Event raised whenever AccountId (TenantId) changes: (oldId,newId)
        event System.Action<long,long>? AccountIdChanged;
    }
}
