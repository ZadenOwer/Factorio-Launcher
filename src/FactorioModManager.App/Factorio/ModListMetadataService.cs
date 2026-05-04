using System.Text.Json;
using FactorioModManager.App.Models;

namespace FactorioModManager.App.Factorio;

public sealed class ModListMetadataService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public ModListMetadata Load(string modListFolderPath)
    {
        var path = GetMetadataPath(modListFolderPath);
        if (!File.Exists(path))
        {
            return ModListMetadata.Empty();
        }

        try
        {
            using var stream = File.OpenRead(path);
            var metadata = JsonSerializer.Deserialize<ModListMetadata>(stream, JsonOptions) ?? ModListMetadata.Empty();
            return new ModListMetadata
            {
                Description = metadata.Description ?? string.Empty,
                SelectedVersions = new Dictionary<string, string>(metadata.SelectedVersions ?? [], StringComparer.OrdinalIgnoreCase),
                CreatedUtc = metadata.CreatedUtc,
                UpdatedUtc = metadata.UpdatedUtc,
                LastActivatedUtc = metadata.LastActivatedUtc
            };
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return ModListMetadata.Empty();
        }
    }

    public void Save(
        string modListFolderPath,
        string description,
        IReadOnlyDictionary<string, string> selectedVersions,
        DateTimeOffset? createdUtc,
        DateTimeOffset? lastActivatedUtc)
    {
        Directory.CreateDirectory(modListFolderPath);
        var now = DateTimeOffset.UtcNow;
        var metadata = new ModListMetadata
        {
            Description = description.Trim(),
            SelectedVersions = selectedVersions
                .Where(pair => !string.IsNullOrWhiteSpace(pair.Key) && !string.IsNullOrWhiteSpace(pair.Value))
                .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase),
            CreatedUtc = createdUtc ?? now,
            UpdatedUtc = now,
            LastActivatedUtc = lastActivatedUtc
        };

        using var stream = File.Create(GetMetadataPath(modListFolderPath));
        JsonSerializer.Serialize(stream, metadata, JsonOptions);
    }

    public void RecordActivation(string modListFolderPath)
    {
        var existing = Load(modListFolderPath);
        var now = DateTimeOffset.UtcNow;
        var metadata = new ModListMetadata
        {
            Description = existing.Description,
            SelectedVersions = new Dictionary<string, string>(existing.SelectedVersions, StringComparer.OrdinalIgnoreCase),
            CreatedUtc = existing.CreatedUtc ?? now,
            UpdatedUtc = now,
            LastActivatedUtc = now
        };

        using var stream = File.Create(GetMetadataPath(modListFolderPath));
        JsonSerializer.Serialize(stream, metadata, JsonOptions);
    }

    public static string GetMetadataPath(string modListFolderPath)
    {
        return Path.Combine(modListFolderPath, FactorioFileNames.ManagerMetadataJson);
    }
}
