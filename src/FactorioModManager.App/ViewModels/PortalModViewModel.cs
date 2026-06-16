using Avalonia.Media.Imaging;
using Avalonia.Threading;
using FactorioModManager.App.Models.Portal;

namespace FactorioModManager.App.ViewModels;

public sealed class PortalModViewModel : ViewModelBase
{
    private static readonly HttpClient ThumbnailClient = new();

    private Bitmap? _thumbnail;

    public PortalModViewModel(PortalModResult result)
    {
        Name = result.Name ?? "(unknown)";
        Title = string.IsNullOrWhiteSpace(result.Title) ? Name : result.Title;
        Owner = result.Owner ?? "Unknown";
        Summary = result.Summary ?? result.Description ?? string.Empty;
        DownloadsCount = result.DownloadsCount ?? 0;
        Category = result.Category ?? string.Empty;

        var latestRelease = result.LatestRelease ?? result.Releases?.LastOrDefault();
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
    public int DownloadsCount { get; }
    public string Category { get; }
    public string LatestVersion { get; }
    public string FactorioVersion { get; }

    public Bitmap? Thumbnail
    {
        get => _thumbnail;
        private set => SetProperty(ref _thumbnail, value);
    }

    public string DownloadsLabel => DownloadsCount switch
    {
        >= 1_000_000 => $"{DownloadsCount / 1_000_000.0:0.0}M",
        >= 1_000 => $"{DownloadsCount / 1_000.0:0.0}K",
        _ => DownloadsCount.ToString()
    };

    private static readonly string LogFile = Path.Combine(Path.GetTempPath(), "factorio_portal_debug.log");

    private async Task LoadThumbnailAsync(string url)
    {
        try
        {
            var bytes = await ThumbnailClient.GetByteArrayAsync(url);
            using var ms = new MemoryStream(bytes);
            var bitmap = new Bitmap(ms);
            Dispatcher.UIThread.Post(() => Thumbnail = bitmap);
            File.AppendAllText(LogFile, $"[{DateTime.Now:HH:mm:ss.fff}] Thumbnail OK ({bytes.Length} bytes): {url}{Environment.NewLine}");
        }
        catch (Exception ex)
        {
            File.AppendAllText(LogFile, $"[{DateTime.Now:HH:mm:ss.fff}] Thumbnail FAIL ({ex.GetType().Name}: {ex.Message}): {url}{Environment.NewLine}");
        }
    }
}
