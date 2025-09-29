namespace Wysg.Musm.Radium.Services
{
    public interface ISupabaseService
    {
        Task<long> EnsureAccountAsync(string uid, string email, string displayName);
        Task UpdateLastLoginAsync(long accountId);
        Task UpdateLastLoginAsync(long accountId, bool silent); // added silent overload
        Task<(bool ok, string message)> TestConnectionAsync();
    }
}
