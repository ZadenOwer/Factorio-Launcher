using FactorioModManager.App.Factorio;

namespace FactorioModManager.Tests;

public sealed class ModListFileManagerTests
{
    [Fact]
    public void CreateManagedListFolder_creates_inside_manager_lists_folder()
    {
        using var temp = new TempDirectory();

        var folder = new ModListFileManager().CreateManagedListFolder(temp.Path, "New");

        Assert.True(Directory.Exists(folder));
        Assert.Equal(ManagerWorkspacePaths.GetManagedListFolder(temp.Path, "New"), folder);
        Assert.False(Directory.Exists(Path.Combine(temp.Path, "New")));
    }

    [Fact]
    public void DeleteManagedList_rejects_unmanaged_folder()
    {
        using var temp = new TempDirectory();
        var folder = Path.Combine(temp.Path, "Unknown");
        Directory.CreateDirectory(folder);

        Assert.Throws<InvalidOperationException>(() =>
            new ModListFileManager().DeleteManagedList(temp.Path, folder));
        Assert.True(Directory.Exists(folder));
    }

    [Fact]
    public void RenameManagedList_renames_only_managed_folder()
    {
        using var temp = new TempDirectory();
        var folder = ManagerWorkspacePaths.GetManagedListFolder(temp.Path, "Old");
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, FactorioFileNames.ModListJson), """{"mods":[]}""");
        File.WriteAllBytes(Path.Combine(folder, FactorioFileNames.ModSettingsDat), [1]);

        var newPath = new ModListFileManager().RenameManagedList(temp.Path, folder, "New");

        Assert.False(Directory.Exists(folder));
        Assert.True(Directory.Exists(newPath));
        Assert.True(File.Exists(Path.Combine(newPath, FactorioFileNames.ModSettingsDat)));
        Assert.StartsWith(ManagerWorkspacePaths.GetListsRoot(temp.Path), newPath, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DeleteManagedList_rejects_root_level_managed_looking_folder()
    {
        using var temp = new TempDirectory();
        var folder = Path.Combine(temp.Path, "OldRootLayout");
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, FactorioFileNames.ModListJson), """{"mods":[]}""");
        File.WriteAllBytes(Path.Combine(folder, FactorioFileNames.ModSettingsDat), [1]);

        Assert.Throws<InvalidOperationException>(() =>
            new ModListFileManager().DeleteManagedList(temp.Path, folder));
        Assert.True(Directory.Exists(folder));
    }

    [Fact]
    public void ApplyRootFilesToManagedList_overwrites_only_managed_list_files()
    {
        using var temp = new TempDirectory();
        File.WriteAllText(Path.Combine(temp.Path, FactorioFileNames.ModListJson), """{"mods":[{"name":"root","enabled":true}]}""");
        File.WriteAllBytes(Path.Combine(temp.Path, FactorioFileNames.ModSettingsDat), [9, 8, 7]);
        var folder = ManagerWorkspacePaths.GetManagedListFolder(temp.Path, "Target");
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, FactorioFileNames.ModListJson), """{"mods":[{"name":"old","enabled":true}]}""");
        File.WriteAllBytes(Path.Combine(folder, FactorioFileNames.ModSettingsDat), [1]);

        new ModListFileManager().ApplyRootFilesToManagedList(temp.Path, folder);

        Assert.Equal(
            """{"mods":[{"name":"root","enabled":true}]}""",
            File.ReadAllText(Path.Combine(folder, FactorioFileNames.ModListJson)));
        Assert.Equal([9, 8, 7], File.ReadAllBytes(Path.Combine(folder, FactorioFileNames.ModSettingsDat)));
    }

    [Fact]
    public void ApplyRootFilesToManagedList_rejects_unmanaged_folder()
    {
        using var temp = new TempDirectory();
        File.WriteAllText(Path.Combine(temp.Path, FactorioFileNames.ModListJson), """{"mods":[]}""");
        File.WriteAllBytes(Path.Combine(temp.Path, FactorioFileNames.ModSettingsDat), [1]);
        var folder = Path.Combine(temp.Path, "Unknown");
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, FactorioFileNames.ModListJson), """{"mods":[{"name":"old","enabled":true}]}""");
        File.WriteAllBytes(Path.Combine(folder, FactorioFileNames.ModSettingsDat), [2]);

        Assert.Throws<InvalidOperationException>(() =>
            new ModListFileManager().ApplyRootFilesToManagedList(temp.Path, folder));
        Assert.Equal([2], File.ReadAllBytes(Path.Combine(folder, FactorioFileNames.ModSettingsDat)));
    }
}
