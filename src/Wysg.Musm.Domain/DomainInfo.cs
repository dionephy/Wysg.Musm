namespace Wysg.Musm.Domain;

// Marker + simple version info provider for the Domain layer.
// Useful to verify assembly loading & for diagnostics endpoints.
public static class DomainInfo
{
    public const string Name = "Wysg.Musm.Domain";
    public static string Version => typeof(DomainInfo).Assembly.GetName().Version?.ToString() ?? "0.0.0";
}
