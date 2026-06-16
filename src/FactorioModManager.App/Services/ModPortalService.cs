using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using FactorioModManager.App.Models.Portal;

namespace FactorioModManager.App.Services;

public sealed class ModPortalService
{
    private static readonly HttpClient Client = new();
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly string LogFile = Path.Combine(Path.GetTempPath(), "factorio_portal_debug.log");

    private const string SearchUrl = "https://mods.factorio.com/api/search";
    private const string ModsUrl = "https://mods.factorio.com/api/mods";

    private static void Log(string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
        Debug.WriteLine(line);
        File.AppendAllText(LogFile, line + Environment.NewLine);
    }

    public async Task<(PortalModResult? Result, string? ErrorMessage)> GetModAsync(
        string modName,
        string? factorioVersion,
        CancellationToken cancellationToken)
    {
        var urlBuilder = new StringBuilder($"{ModsUrl}/{Uri.EscapeDataString(modName)}/full?lang=en");
        if (!string.IsNullOrWhiteSpace(factorioVersion))
            urlBuilder.Append($"&version={Uri.EscapeDataString(factorioVersion)}");
        var url = urlBuilder.ToString();
        try
        {
            Log($"GET {url}");
            using var response = await Client.GetAsync(url, cancellationToken);
            var rawJson = await response.Content.ReadAsStringAsync(cancellationToken);
            Log($"Response: {(int)response.StatusCode} {response.ReasonPhrase} ({rawJson.Length} chars)");
            Log($"Raw JSON: {rawJson}");
            if (!response.IsSuccessStatusCode)
                return (null, $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}");
            var result = JsonSerializer.Deserialize<PortalModResult>(rawJson, JsonOptions);
            return (result, null);
        }
        catch (OperationCanceledException) { Log("Request cancelled."); throw; }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or InvalidOperationException)
        {
            Log($"Exception ({ex.GetType().Name}): {ex.Message}");
            return (null, ex.Message);
        }
    }

    public Task<(ModPortalResponse? Result, string? RawJson, string? ErrorMessage)> SearchAsync(
        string username,
        string token,
        string query,
        string? factorioVersion,
        string[]? categories,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        return PostAsync(username, token, query.Trim(), factorioVersion, categories, page, pageSize, cancellationToken);
    }

    private static async Task<(ModPortalResponse? Result, string? RawJson, string? ErrorMessage)> PostAsync(
        string username,
        string token,
        string query,
        string? factorioVersion,
        string[]? categories,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        try
        {
            var bodyObj = new Dictionary<string, object>
            {
                ["username"] = username,
                ["token"] = token,
                ["query"] = query,
                ["lang"] = "en",
                ["sort_attribute"] = "relevancy",
                ["show_deprecated"] = false,
                ["only_bookmarks"] = false,
                ["exclude_category"] = Array.Empty<string>(),
                ["page"] = page,
                ["page_size"] = pageSize,
            };
            if (!string.IsNullOrWhiteSpace(factorioVersion))
                bodyObj["version"] = factorioVersion;
            if (categories is { Length: > 0 })
                bodyObj["category"] = categories;

            var body = JsonSerializer.Serialize(bodyObj);
            using var content = new StringContent(body, Encoding.UTF8, "application/json");

            var categoryPart = categories is { Length: > 0 }
                ? $",\"category\":[{string.Join(",", categories.Select(c => $"\"{c}\""))}]"
                : string.Empty;
            Log($"POST {SearchUrl}");
            Log($"Body: {{\"username\":\"{username}\",\"token\":\"***\",\"query\":\"{query}\",\"page\":{page},\"page_size\":{pageSize}{categoryPart}}}");

            using var response = await Client.PostAsync(SearchUrl, content, cancellationToken);
            var rawJson = await response.Content.ReadAsStringAsync(cancellationToken);

            Log($"Response: {(int)response.StatusCode} {response.ReasonPhrase} ({rawJson.Length} chars)");
            Log($"Raw JSON: {rawJson}");

            if (!response.IsSuccessStatusCode)
                return (null, rawJson, $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}");

            var result = JsonSerializer.Deserialize<ModPortalResponse>(rawJson, JsonOptions);
            Log($"Parsed: {result?.Results?.Count ?? 0} results, page {result?.Pagination?.Page}/{result?.Pagination?.PageCount}, total {result?.Pagination?.Count}");
            return (result, rawJson, null);
        }
        catch (OperationCanceledException)
        {
            Log("Request cancelled.");
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or InvalidOperationException)
        {
            Log($"Exception ({ex.GetType().Name}): {ex.Message}");
            return (null, null, ex.Message);
        }
    }
}
