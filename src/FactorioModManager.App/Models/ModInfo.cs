namespace FactorioModManager.App.Models;

public sealed class ModInfo
{
    public required string Name { get; init; }
    public string? Title { get; init; }
    public string? Version { get; init; }
    public string? Author { get; init; }
    public string? Description { get; init; }
    public required string SourceZipPath { get; init; }
    public IReadOnlyList<string> SourceZipPaths { get; init; } = [];
    public IReadOnlyList<string> AvailableVersions { get; init; } = [];
    public IReadOnlyList<string> Dependencies { get; init; } = [];
    public long SizeBytes { get; init; }
    public long TotalSizeBytes { get; init; }
    public bool HasMetadataWarning { get; init; }
    public string? WarningMessage { get; init; }
    public bool IsDisabled { get; init; }

    public ModInfo WithDisabled() => new()
    {
        Name = Name, Title = Title, Version = Version, Author = Author, Description = Description,
        SourceZipPath = SourceZipPath, SourceZipPaths = SourceZipPaths,
        AvailableVersions = AvailableVersions, Dependencies = Dependencies,
        SizeBytes = SizeBytes, TotalSizeBytes = TotalSizeBytes,
        HasMetadataWarning = HasMetadataWarning, WarningMessage = WarningMessage,
        IsDisabled = true
    };

    public string DisplayTitle => string.IsNullOrWhiteSpace(Title) ? Name : Title;
    public string DisplayDescription => string.IsNullOrWhiteSpace(Description) ? Name : Description;
    public string DisplayAuthor => string.IsNullOrWhiteSpace(Author) ? "Unknown" : Author;
    public string DisplayVersion => string.IsNullOrWhiteSpace(Version) ? "-" : Version;
    public string DisplaySize => FormatBytes(TotalSizeBytes > 0 ? TotalSizeBytes : SizeBytes);
    public string VersionCountLabel => AvailableVersions.Count == 1
        ? "1 version"
        : $"{AvailableVersions.Count} versions";
    public string DependencyCountLabel
    {
        get
        {
            var count = Dependencies.Count(d =>
            {
                var s = d.TrimStart();
                return !s.StartsWith('?') && !s.StartsWith('!') && !s.StartsWith('~') && !s.StartsWith("(?)");
            });
            return count == 1 ? "1 dep" : $"{count} deps";
        }
    }

    public static string FormatBytes(long bytes)
    {
        if (bytes <= 0)
        {
            return "-";
        }

        var mb = bytes / 1024d / 1024d;
        if (mb >= 0.1d)
        {
            return $"{mb:0.0} MB";
        }

        var kb = bytes / 1024d;
        return $"{Math.Max(1d, kb):0} KB";
    }
}
