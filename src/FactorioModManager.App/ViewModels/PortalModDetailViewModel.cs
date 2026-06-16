using Avalonia.Media.Imaging;
using Avalonia.Threading;
using FactorioModManager.App.Models.Portal;

namespace FactorioModManager.App.ViewModels;

public sealed class PortalModDetailViewModel : ViewModelBase
{
    private static readonly HttpClient ThumbnailClient = new();

    private Bitmap? _thumbnail;

    public PortalModDetailViewModel(PortalModResult result)
    {
        Name = result.Name ?? "";
        Title = string.IsNullOrWhiteSpace(result.Title) ? Name : result.Title;
        Owner = result.Owner ?? "";
        Summary = result.Summary ?? "";
        Description = result.Description ?? "";
        Category = result.Category ?? "";
        DownloadsCount = result.DownloadsCount ?? 0;
        Homepage = result.Homepage;
        SourceUrl = result.SourceUrl;

        Releases = (result.Releases ?? [])
            .OrderByDescending(r => r.ReleasedAt)
            .ToList();

        var latestRelease = result.LatestRelease ?? Releases.FirstOrDefault();
        LatestRelease = latestRelease;
        LatestVersion = latestRelease?.Version ?? result.LatestReleaseVersion ?? "-";
        FactorioVersion = latestRelease?.InfoJson?.FactorioVersion
            ?? result.FactorioVersions?.LastOrDefault()
            ?? "-";

        if (!string.IsNullOrWhiteSpace(result.Thumbnail))
            _ = LoadThumbnailAsync("https://mods-data.factorio.com" + result.Thumbnail);
    }

    public string Name { get; }
    public string Title { get; }
    public string Owner { get; }
    public string Summary { get; }
    public string Description { get; }
    public string Category { get; }
    public int DownloadsCount { get; }
    public string? Homepage { get; }
    public string? SourceUrl { get; }
    public string LatestVersion { get; }
    public string FactorioVersion { get; }
    public PortalModRelease? LatestRelease { get; }
    public List<PortalModRelease> Releases { get; }
    public bool CanInstall => LatestRelease?.DownloadUrl is not null;

    public string DownloadsLabel => DownloadsCount switch
    {
        >= 1_000_000 => $"{DownloadsCount / 1_000_000.0:0.0}M downloads",
        >= 1_000 => $"{DownloadsCount / 1_000.0:0.0}K downloads",
        _ => $"{DownloadsCount} downloads"
    };

    public bool HasDescription => !string.IsNullOrWhiteSpace(Description);
    public bool HasHomepage => !string.IsNullOrWhiteSpace(Homepage);
    public bool HasSourceUrl => !string.IsNullOrWhiteSpace(SourceUrl);

    public Bitmap? Thumbnail
    {
        get => _thumbnail;
        private set => SetProperty(ref _thumbnail, value);
    }

    private async Task LoadThumbnailAsync(string url)
    {
        try
        {
            var bytes = await ThumbnailClient.GetByteArrayAsync(url);
            using var ms = new MemoryStream(bytes);
            var bitmap = new Bitmap(ms);
            Dispatcher.UIThread.Post(() => Thumbnail = bitmap);
        }
        catch { }
    }
}
