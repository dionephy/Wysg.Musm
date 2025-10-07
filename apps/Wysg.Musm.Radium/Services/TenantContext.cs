namespace Wysg.Musm.Radium.Services
{
    /// <summary>
    /// Concrete in-memory implementation of <see cref="ITenantContext"/>.
    /// Lightweight evented container for the active account/tenant identity.
    /// Designed for simple desktop usage; no locking or cross-process propagation.
    /// </summary>
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
                    AccountIdChanged?.Invoke(old, _tenantId); // notify subscribers (e.g., phrase UI) of account switch
                }
            }
        }
        public string TenantCode { get; set; } = string.Empty; // external auth provider user id
        public long AccountId { get => TenantId; set => TenantId = value; } // alias for readability
        public string? ReportifySettingsJson { get; set; } // cached per-account settings JSON
        public event System.Action<long, long>? AccountIdChanged; // subscription point for reload / clear behaviors
    }
}
