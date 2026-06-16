using System.Diagnostics;
using System.Net.Http;
using System.Security.Cryptography;

namespace FactorioModManager.App.Services;

public sealed class ModDownloadService
{
    private static readonly HttpClient Client = new() { Timeout = Timeout.InfiniteTimeSpan };
    private static readonly string LogFile = Path.Combine(Path.GetTempPath(), "factorio_portal_debug.log");
    private const string BaseUrl = "https://mods.factorio.com";

    private static void Log(string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
        Debug.WriteLine(line);
        File.AppendAllText(LogFile, line + Environment.NewLine);
    }

    // relativeUrl: e.g. "/download/Krastorio2/abc123"
    // Returns null on success, error message on failure.
    public async Task<string?> DownloadAsync(
        string relativeUrl,
        string fileName,
        string destFolder,
        string username,
        string token,
        string? expectedSha1,
        IProgress<double> progress,
        CancellationToken ct)
    {
        var url = $"{BaseUrl}{relativeUrl}?username={Uri.EscapeDataString(username)}&token={Uri.EscapeDataString(token)}";
        var destPath = Path.Combine(destFolder, fileName);
        var partPath = destPath + ".part";

        Log($"Download START: {BaseUrl}{relativeUrl}?username={username}&token=***");
        try
        {
            using var response = await Client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
            if (!response.IsSuccessStatusCode)
            {
                Log($"Download HTTP error: {(int)response.StatusCode} {response.ReasonPhrase}");
                return $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}";
            }

            var total = response.Content.Headers.ContentLength;
            Log($"Download size: {total?.ToString() ?? "unknown"} bytes → {fileName}");

            await using (var src = await response.Content.ReadAsStreamAsync(ct))
            await using (var dst = File.Create(partPath))
            {
                var buffer = new byte[81920];
                long received = 0;
                int read;
                while ((read = await src.ReadAsync(buffer, ct)) > 0)
                {
                    await dst.WriteAsync(buffer.AsMemory(0, read), ct);
                    received += read;
                    if (total > 0)
                        progress.Report((double)received / total.Value);
                }
            }

            if (!string.IsNullOrWhiteSpace(expectedSha1))
            {
                var actual = ComputeSha1(partPath);
                if (!string.Equals(actual, expectedSha1, StringComparison.OrdinalIgnoreCase))
                {
                    File.Delete(partPath);
                    Log($"SHA1 mismatch: expected {expectedSha1}, got {actual}");
                    return "Download integrity check failed (SHA1 mismatch). The file has been removed.";
                }
                Log($"SHA1 OK: {actual}");
            }

            if (File.Exists(destPath))
                File.Delete(destPath);
            File.Move(partPath, destPath);

            progress.Report(1.0);
            Log($"Download DONE: {fileName}");
            return null;
        }
        catch (OperationCanceledException)
        {
            TryDelete(partPath);
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or UnauthorizedAccessException)
        {
            TryDelete(partPath);
            Log($"Download FAIL ({ex.GetType().Name}): {ex.Message}");
            return ex.Message;
        }
    }

    private static string ComputeSha1(string path)
    {
        using var stream = File.OpenRead(path);
        var hash = SHA1.HashData(stream);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { }
    }
}
