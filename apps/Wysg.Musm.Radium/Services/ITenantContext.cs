namespace Wysg.Musm.Radium.Services
{
    public interface ITenantContext
    {
        long TenantId { get; set; }
        string TenantCode { get; set; }
    }
}
