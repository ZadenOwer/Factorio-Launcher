using System.Text.Json;
using FactorioModManager.App.Factorio;

namespace FactorioModManager.Tests;

public sealed class BackupAndActivationTests
{
    [Fact]
    public void BackupService_copies_current_root_files()
    {
        using var temp = new TempDirectory();
        File.WriteAllText(Path.Combine(temp.Path, FactorioFileNames.ModListJson), "root-list");
        File.WriteAllBytes(Path.Combine(temp.Path, FactorioFileNames.ModSettingsDat), [9, 8, 7]);

        var backupPath = new BackupService().CreateBackup(temp.Path);

        Assert.Equal("root-list", File.ReadAllText(Path.Combine(backupPath, FactorioFileNames.ModListJson)));
        Assert.Equal([9, 8, 7], File.ReadAllBytes(Path.Combine(backupPath, FactorioFileNames.ModSettingsDat)));
        Assert.StartsWith(ManagerWorkspacePaths.GetBackupsRoot(temp.Path), backupPath, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Activator_merges_list_into_root_and_copies_settings()
    {
        using var temp = new TempDirectory();
        var rootListJson = """{"mods":[{"name":"base","enabled":true},{"name":"mod-a","enabled":true},{"name":"mod-b","enabled":true}]}""";
        File.WriteAllText(Path.Combine(temp.Path, FactorioFileNames.ModListJson), rootListJson);
        File.WriteAllBytes(Path.Combine(temp.Path, FactorioFileNames.ModSettingsDat), [1]);

        var listFolder = ManagerWorkspacePaths.GetManagedListFolder(temp.Path, "NewPack");
        Directory.CreateDirectory(listFolder);
        var listJson = """{"mods":[{"name":"base","enabled":true},{"name":"mod-a","enabled":true}]}""";
        File.WriteAllText(Path.Combine(listFolder, FactorioFileNames.ModListJson), listJson);
        File.WriteAllBytes(Path.Combine(listFolder, FactorioFileNames.ModSettingsDat), [2, 3]);

        var result = new ModListActivator(new BackupService()).Activate(temp.Path, listFolder);

        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal([2, 3], File.ReadAllBytes(Path.Combine(temp.Path, FactorioFileNames.ModSettingsDat)));
        Assert.NotNull(result.BackupFolderPath);
        Assert.Equal(rootListJson, File.ReadAllText(Path.Combine(result.BackupFolderPath!, FactorioFileNames.ModListJson)));

        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(temp.Path, FactorioFileNames.ModListJson)));
        var mods = doc.RootElement.GetProperty("mods").EnumerateArray().ToList();
        Assert.Contains(mods, m => m.GetProperty("name").GetString() == "mod-a" && m.GetProperty("enabled").GetBoolean());
        Assert.Contains(mods, m => m.GetProperty("name").GetString() == "mod-b" && !m.GetProperty("enabled").GetBoolean());
    }

    [Fact]
    public void Activator_rejects_folder_outside_mods_root()
    {
        using var root = new TempDirectory();
        using var outside = new TempDirectory();

        var result = new ModListActivator(new BackupService()).Activate(root.Path, outside.Path);

        Assert.False(result.Success);
    }

    [Fact]
    public void Activator_rejects_root_level_managed_looking_folder()
    {
        using var temp = new TempDirectory();
        File.WriteAllText(Path.Combine(temp.Path, FactorioFileNames.ModListJson), "old-list");
        File.WriteAllBytes(Path.Combine(temp.Path, FactorioFileNames.ModSettingsDat), [1]);

        var listFolder = Path.Combine(temp.Path, "RootPack");
        Directory.CreateDirectory(listFolder);
        File.WriteAllText(Path.Combine(listFolder, FactorioFileNames.ModListJson), "new-list");
        File.WriteAllBytes(Path.Combine(listFolder, FactorioFileNames.ModSettingsDat), [2, 3]);

        var result = new ModListActivator(new BackupService()).Activate(temp.Path, listFolder);

        Assert.False(result.Success);
        Assert.Equal("old-list", File.ReadAllText(Path.Combine(temp.Path, FactorioFileNames.ModListJson)));
    }
}
