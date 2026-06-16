namespace FactorioModManager.App.Services;

public sealed class AppSettings
{
    public string? LastModsFolderPath { get; set; }
    public string? FactorioInstallFolderPath { get; set; }
    public string? ActiveModListFolderPath { get; set; }
    public Dictionary<string, List<string>> ModListOrders { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public string? PortalUsername { get; set; }
    public string? PortalToken { get; set; }
}
