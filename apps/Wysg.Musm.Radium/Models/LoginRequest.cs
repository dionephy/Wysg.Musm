namespace Wysg.Musm.Radium.Models
{
    public class LoginRequest
    {
        public string TenantCode { get; set; } = string.Empty;
        public string? UserName { get; set; }
    }
}