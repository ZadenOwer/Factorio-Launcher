using System.Text.Json;

namespace FactorioModManager.App.Factorio;

public sealed class ModListEntryWriter
{
    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    // Adds modName as enabled to the mod-list.json in folderPath.
    // If the entry already exists it is updated to enabled=true.
    // If the file doesn't exist it is created with base + modName.
    public void AddMod(string folderPath, string modName)
    {
        var path = Path.Combine(folderPath, FactorioFileNames.ModListJson);

        var entries = new List<(string Name, bool Enabled)>();

        if (File.Exists(path))
        {
            using var stream = File.OpenRead(path);
            using var doc = JsonDocument.Parse(stream);
            if (doc.RootElement.TryGetProperty("mods", out var modsEl) &&
                modsEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var el in modsEl.EnumerateArray())
                {
                    var name = el.TryGetProperty("name", out var n) ? n.GetString() : null;
                    if (string.IsNullOrWhiteSpace(name)) continue;
                    var enabled = !el.TryGetProperty("enabled", out var e) || e.GetBoolean();
                    entries.Add((name, enabled));
                }
            }
        }
        else
        {
            entries.Add(("base", true));
        }

        var idx = entries.FindIndex(e =>
            string.Equals(e.Name, modName, StringComparison.OrdinalIgnoreCase));

        if (idx >= 0)
            entries[idx] = (entries[idx].Name, true);
        else
            entries.Add((modName, true));

        var payload = new
        {
            mods = entries.Select(e => new { name = e.Name, enabled = e.Enabled }).ToArray()
        };

        Directory.CreateDirectory(folderPath);
        using var outStream = File.Create(path);
        JsonSerializer.Serialize(outStream, payload, WriteOptions);
    }

    // Sets modName to enabled=true or enabled=false in folderPath's mod-list.json.
    // Adds the entry if it doesn't exist yet.
    public void SetModEnabled(string folderPath, string modName, bool enabled)
    {
        var path = Path.Combine(folderPath, FactorioFileNames.ModListJson);

        var entries = new List<(string Name, bool Enabled)>();
        if (File.Exists(path))
        {
            using (var stream = File.OpenRead(path))
            using (var doc = JsonDocument.Parse(stream))
            {
                if (doc.RootElement.TryGetProperty("mods", out var modsEl) && modsEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var el in modsEl.EnumerateArray())
                    {
                        var name = el.TryGetProperty("name", out var n) ? n.GetString() : null;
                        if (string.IsNullOrWhiteSpace(name)) continue;
                        var isEnabled = !el.TryGetProperty("enabled", out var e) || e.GetBoolean();
                        entries.Add((name, isEnabled));
                    }
                }
            }
        }
        else
        {
            entries.Add(("base", true));
        }

        var idx = entries.FindIndex(e => string.Equals(e.Name, modName, StringComparison.OrdinalIgnoreCase));
        if (idx >= 0)
            entries[idx] = (entries[idx].Name, enabled);
        else
            entries.Add((modName, enabled));

        var payload = new
        {
            mods = entries.Select(e => new { name = e.Name, enabled = e.Enabled }).ToArray()
        };

        Directory.CreateDirectory(folderPath);
        using var outStream = File.Create(path);
        JsonSerializer.Serialize(outStream, payload, WriteOptions);
    }

    // Adds each modName to the mod-list.json in folderPath if it is not already present.
    // Existing entries (enabled or disabled) are left unchanged.
    public void MergeFrom(string folderPath, IEnumerable<string> modNames)
    {
        var path = Path.Combine(folderPath, FactorioFileNames.ModListJson);

        var entries = new List<(string Name, bool Enabled)>();

        if (File.Exists(path))
        {
            using var stream = File.OpenRead(path);
            using var doc = JsonDocument.Parse(stream);
            if (doc.RootElement.TryGetProperty("mods", out var modsEl) && modsEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var el in modsEl.EnumerateArray())
                {
                    var name = el.TryGetProperty("name", out var n) ? n.GetString() : null;
                    if (string.IsNullOrWhiteSpace(name)) continue;
                    var enabled = !el.TryGetProperty("enabled", out var e) || e.GetBoolean();
                    entries.Add((name, enabled));
                }
            }
        }
        else
        {
            entries.Add(("base", true));
        }

        var existing = entries.Select(e => e.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var modName in modNames)
        {
            if (existing.Add(modName))
                entries.Add((modName, true));
        }

        var payload = new
        {
            mods = entries.Select(e => new { name = e.Name, enabled = e.Enabled }).ToArray()
        };

        Directory.CreateDirectory(folderPath);
        using var outStream = File.Create(path);
        JsonSerializer.Serialize(outStream, payload, WriteOptions);
    }

    // Removes modName from the mod-list.json in folderPath.
    // If the entry is not present, does nothing.
    public void RemoveMod(string folderPath, string modName)
    {
        var path = Path.Combine(folderPath, FactorioFileNames.ModListJson);
        if (!File.Exists(path)) return;

        List<(string Name, bool Enabled)> entries;
        using (var stream = File.OpenRead(path))
        using (var doc = JsonDocument.Parse(stream))
        {
            if (!doc.RootElement.TryGetProperty("mods", out var modsEl) || modsEl.ValueKind != JsonValueKind.Array)
                return;

            entries = [];
            foreach (var el in modsEl.EnumerateArray())
            {
                var name = el.TryGetProperty("name", out var n) ? n.GetString() : null;
                if (string.IsNullOrWhiteSpace(name)) continue;
                if (string.Equals(name, modName, StringComparison.OrdinalIgnoreCase)) continue;
                var enabled = !el.TryGetProperty("enabled", out var e) || e.GetBoolean();
                entries.Add((name, enabled));
            }
        }

        var payload = new
        {
            mods = entries.Select(e => new { name = e.Name, enabled = e.Enabled }).ToArray()
        };

        using var outStream = File.Create(path);
        JsonSerializer.Serialize(outStream, payload, WriteOptions);
    }
}
