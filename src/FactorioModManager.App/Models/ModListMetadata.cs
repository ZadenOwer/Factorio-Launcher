namespace FactorioModManager.App.Models;

public sealed class ModListMetadata
{
    public string Description { get; init; } = string.Empty;
    public Dictionary<string, string> SelectedVersions { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public DateTimeOffset? CreatedUtc { get; init; }
    public DateTimeOffset? UpdatedUtc { get; init; }
    public DateTimeOffset? LastActivatedUtc { get; init; }

    public static ModListMetadata Empty()
    {
        return new ModListMetadata();
    }
}
