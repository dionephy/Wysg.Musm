namespace Wysg.Musm.Radium.Services
{
    public interface IRadiumLocalSettings
    {
        // Central (Supabase) DB
        string? CentralConnectionString { get; set; }
        // Local/Intranet DB used by the editor
        string? LocalConnectionString { get; set; }

        // Backward compat: map to LocalConnectionString
        string? ConnectionString { get; set; }
    }
}
