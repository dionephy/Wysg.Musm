using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Wysg.Musm.Radium.Services.ApiClients
{
    /// <summary>
    /// Account count DTO (matches API)
    /// </summary>
    public class AccountsCountDto
    {
        public long Count { get; set; }
    }

    /// <summary>
    /// API client for account-level operations
    /// </summary>
    public interface IAccountsApiClient
    {
        Task<long> GetCountAsync();
        Task<bool> IsAvailableAsync();
    }

    /// <summary>
    /// Implementation of accounts API client
    /// </summary>
    public class AccountsApiClient : ApiClientBase, IAccountsApiClient
    {
        public AccountsApiClient(HttpClient httpClient, string baseUrl)
            : base(httpClient, baseUrl)
        {
        }

        public async Task<long> GetCountAsync()
        {
            try
            {
                Debug.WriteLine("[AccountsApiClient] Getting account count");
                var result = await GetAsync<AccountsCountDto>("/api/accounts/count");
                return result?.Count ?? 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AccountsApiClient] Error getting account count: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> IsAvailableAsync()
        {
            return await IsApiAvailableAsync();
        }
    }
}
