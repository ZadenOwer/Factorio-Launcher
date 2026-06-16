using System.Text.Json;
using FactorioModManager.App.Models;

namespace FactorioModManager.App.Factorio;

public sealed class ModListActivator
{
    private readonly BackupService _backupService;

    public ModListActivator(BackupService backupService)
    {
        _backupService = backupService;
    }

    public ModListActivationResult Activate(string modsFolderPath, string modListFolderPath)
    {
        var backupFolder = default(string);
        var rootFilesModified = false;

        try
        {
            if (!Directory.Exists(modsFolderPath))
            {
                return Failure("The Factorio mods folder does not exist.", backupFolder, rootFilesModified);
            }

            if (!ManagerWorkspacePaths.IsManagedListPath(modsFolderPath, modListFolderPath))
            {
                return Failure("The selected mod list is not inside the manager lists folder.", backupFolder, rootFilesModified);
            }

            if (!ModListDetector.IsManagedListFolder(modListFolderPath))
            {
                return Failure("The selected folder is not a managed mod list.", backupFolder, rootFilesModified);
            }

            var sourceModList = Path.Combine(modListFolderPath, FactorioFileNames.ModListJson);
            var sourceModSettings = Path.Combine(modListFolderPath, FactorioFileNames.ModSettingsDat);
            var rootModList = Path.Combine(modsFolderPath, FactorioFileNames.ModListJson);
            var rootModSettings = Path.Combine(modsFolderPath, FactorioFileNames.ModSettingsDat);

            using (File.OpenRead(sourceModList))
            {
            }

            using (File.OpenRead(sourceModSettings))
            {
            }

            backupFolder = _backupService.CreateBackup(modsFolderPath);

            MergeModListIntoRoot(sourceModList, rootModList);
            rootFilesModified = true;
            File.Copy(sourceModSettings, rootModSettings, overwrite: true);

            return new ModListActivationResult
            {
                Success = true,
                BackupFolderPath = backupFolder,
                RootFilesModified = true
            };
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            var message = rootFilesModified
                ? $"Activation failed after one root file was modified. Backup folder: {backupFolder}. Error: {ex.Message}"
                : $"Activation failed before root files were modified. Error: {ex.Message}";
            return Failure(message, backupFolder, rootFilesModified);
        }
    }

    private static ModListActivationResult Failure(string message, string? backupFolderPath, bool rootFilesModified)
    {
        return new ModListActivationResult
        {
            Success = false,
            ErrorMessage = message,
            BackupFolderPath = backupFolderPath,
            RootFilesModified = rootFilesModified
        };
    }

    private static void MergeModListIntoRoot(string sourceModList, string rootModList)
    {
        var listMods = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        using (var stream = File.OpenRead(sourceModList))
        using (var doc = JsonDocument.Parse(stream))
        {
            if (doc.RootElement.TryGetProperty("mods", out var modsEl) && modsEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var el in modsEl.EnumerateArray())
                {
                    var name = el.TryGetProperty("name", out var n) ? n.GetString() : null;
                    if (string.IsNullOrWhiteSpace(name)) continue;
                    var enabled = !el.TryGetProperty("enabled", out var e) || e.GetBoolean();
                    listMods[name] = enabled;
                }
            }
        }

        var rootEntries = new List<(string Name, bool Enabled)>();

        if (File.Exists(rootModList))
        {
            using var stream = File.OpenRead(rootModList);
            using var doc = JsonDocument.Parse(stream);
            if (doc.RootElement.TryGetProperty("mods", out var modsEl) && modsEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var el in modsEl.EnumerateArray())
                {
                    var name = el.TryGetProperty("name", out var n) ? n.GetString() : null;
                    if (string.IsNullOrWhiteSpace(name)) continue;
                    var enabled = listMods.TryGetValue(name, out var listEnabled) ? listEnabled : false;
                    rootEntries.Add((name, enabled));
                    listMods.Remove(name);
                }
            }
        }

        foreach (var (name, enabled) in listMods)
            rootEntries.Add((name, enabled));

        var payload = new
        {
            mods = rootEntries.Select(e => new { name = e.Name, enabled = e.Enabled }).ToArray()
        };

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true };
        using var outStream = File.Create(rootModList);
        JsonSerializer.Serialize(outStream, payload, options);
    }

    internal static bool IsImmediateChild(string parentFolder, string childFolder)
    {
        return ManagerWorkspacePaths.IsImmediateChild(parentFolder, childFolder);
    }
}
