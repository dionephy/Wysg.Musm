namespace Wysg.Musm.Radium.Services
{
    public sealed class TenantContext : ITenantContext
    {
        public long TenantId { get; set; }
        public string TenantCode { get; set; } = string.Empty;
    }
}
