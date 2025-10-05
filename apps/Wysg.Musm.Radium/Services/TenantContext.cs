namespace Wysg.Musm.Radium.Services
{
    public sealed class TenantContext : ITenantContext
    {
        private long _tenantId;
        public long TenantId
        {
            get => _tenantId;
            set
            {
                if (_tenantId != value)
                {
                    var old = _tenantId;
                    _tenantId = value;
                    AccountIdChanged?.Invoke(old, _tenantId);
                }
            }
        }
        public string TenantCode { get; set; } = string.Empty;
        public long AccountId { get => TenantId; set => TenantId = value; }
        public string? ReportifySettingsJson { get; set; }
        public event System.Action<long, long>? AccountIdChanged;
    }
}
