namespace Wysg.Musm.Radium.Models
{
    public class TenantModel
    {
        public long Id { get; set; }
        public long AccountId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string PacsKey { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}