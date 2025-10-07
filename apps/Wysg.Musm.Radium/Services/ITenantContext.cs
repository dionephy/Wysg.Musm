namespace Wysg.Musm.Radium.Services
{
    /// <summary>
    /// Holds the currently active tenant (account) identity for the running desktop session.
    /// Acts as an in-memory, mutable context shared across view models / services that need the account id.
    ///
    /// Semantics:
    ///   * <see cref="TenantId"/> and <see cref="AccountId"/> are aliases (central DB now standardizes on account terminology).
    ///   * Setting either raises <see cref="AccountIdChanged"/> when the underlying numeric id changes.
    ///   * <see cref="TenantCode"/> stores the external UID (e.g. auth provider user id) for correlation / logging.
    ///   * <see cref="ReportifySettingsJson"/> caches the last fetched reportify settings blob for fast reuse.
    ///
    /// Thread-safety: Not synchronized; update only from UI or coordinated background tasks.
    /// </summary>
    public interface ITenantContext
    {
        long TenantId { get; set; }
        string TenantCode { get; set; }
        long AccountId { get; set; }
        string? ReportifySettingsJson { get; set; }
        /// <summary>Event fired after the tenant/account id actually changes value. Args=(oldId,newId).</summary>
        event System.Action<long,long>? AccountIdChanged;
    }
}
