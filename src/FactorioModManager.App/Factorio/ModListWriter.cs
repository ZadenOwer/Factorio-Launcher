using System.Text.Json;

namespace FactorioModManager.App.Factorio;

public sealed class ModListWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public void Write(string modListFolderPath, IEnumerable<string> selectedModNames)
    {
        Directory.CreateDirectory(modListFolderPath);

        var mods = new List<FactorioModJson>
        {
            new() { Name = "base", Enabled = true }
        };

        foreach (var name in selectedModNames
            .Where(n => !string.IsNullOrWhiteSpace(n) &&
                        !string.Equals(n, "base", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase))
        {
            mods.Add(new() { Name = name, Enabled = true });
        }

        var payload = new FactorioModListJson { Mods = mods };
        var path = Path.Combine(modListFolderPath, FactorioFileNames.ModListJson);
        using var stream = File.Create(path);
        JsonSerializer.Serialize(stream, payload, JsonOptions);
    }

    private sealed class FactorioModListJson
    {
        public required IReadOnlyList<FactorioModJson> Mods { get; init; }
    }

    private sealed class FactorioModJson
    {
        public required string Name { get; init; }
        public required bool Enabled { get; init; }
    }
}
