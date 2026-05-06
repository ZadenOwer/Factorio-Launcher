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
    public bool HasMultipleVersions { get; init; }
    public string? NewestVersion { get; init; }
    public string Subtitle => IsMissing
        ? "Selected but no matching zip was found"
        : string.IsNullOrWhiteSpace(Description) ? $"{Author ?? "Unknown"} - {Name}" : $"{Author ?? "Unknown"} - {Description}";
    public string DisplayVersion => string.IsNullOrWhiteSpace(Version) ? "-" : Version;
    public bool IsUsingOlderVersion => HasMultipleVersions &&
        !string.IsNullOrWhiteSpace(NewestVersion) &&
        !string.Equals(DisplayVersion, NewestVersion, StringComparison.OrdinalIgnoreCase);
    public string VersionToolTip => IsUsingOlderVersion
        ? $"Selected version is older than latest available version {NewestVersion}."
        : "Selected mod version";
    public string VersionBackground => IsUsingOlderVersion ? "#3A2412" : "#221D18";
    public string VersionBorderBrush => IsUsingOlderVersion ? "#D97A2C" : "#3A342C";
    public string VersionForeground => IsUsingOlderVersion ? "#F0A455" : "#A89C84";
}
