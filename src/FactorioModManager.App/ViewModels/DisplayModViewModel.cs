namespace FactorioModManager.App.ViewModels;

public sealed class DisplayModViewModel
{
    public required string Name { get; init; }
    public required string Title { get; init; }
    public string? Version { get; init; }
    public string? Author { get; init; }
    public string? Description { get; init; }
    public string Size { get; init; } = "-";
    public bool IsMissing { get; init; }
    public string Subtitle => IsMissing
        ? "Selected but no matching zip was found"
        : string.IsNullOrWhiteSpace(Description) ? $"{Author ?? "Unknown"} - {Name}" : $"{Author ?? "Unknown"} - {Description}";
    public string DisplayVersion => string.IsNullOrWhiteSpace(Version) ? "-" : Version;
}
