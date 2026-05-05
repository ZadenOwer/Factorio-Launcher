using FactorioModManager.App.Factorio;

namespace FactorioModManager.Tests;

public sealed class FactorioInstallLocatorTests
{
    [Fact]
    public void Locate_returns_valid_preferred_install_folder()
    {
        using var installTemp = new TempDirectory();
        CreateDataFolder(installTemp.Path);
        using var otherTemp = new TempDirectory();
        CreateDataFolder(otherTemp.Path);

        var result = new FactorioInstallLocator([otherTemp.Path], []).Locate(installTemp.Path, null);

        Assert.Equal(installTemp.Path, result);
    }

    [Fact]
    public void Locate_finds_candidate_install_folder_when_preferred_is_missing()
    {
        using var installTemp = new TempDirectory();
        CreateDataFolder(installTemp.Path);

        var result = new FactorioInstallLocator([installTemp.Path], []).Locate(null, null);

        Assert.Equal(installTemp.Path, result);
    }

    [Fact]
    public void Locate_finds_portable_install_folder_from_mods_folder_parent()
    {
        using var installTemp = new TempDirectory();
        CreateDataFolder(installTemp.Path);
        var modsFolder = Path.Combine(installTemp.Path, "mods");
        Directory.CreateDirectory(modsFolder);

        var result = new FactorioInstallLocator([], []).Locate(null, modsFolder);

        Assert.Equal(installTemp.Path, result);
    }

    [Fact]
    public void Locate_reads_steam_libraryfolders_file()
    {
        using var steamLibraryTemp = new TempDirectory();
        using var configTemp = new TempDirectory();
        var installFolder = Path.Combine(steamLibraryTemp.Path, "steamapps", "common", "Factorio");
        CreateDataFolder(installFolder);
        var libraryFile = Path.Combine(configTemp.Path, "libraryfolders.vdf");
        File.WriteAllText(libraryFile, $$"""
        "libraryfolders"
        {
            "0"
            {
                "path" "{{steamLibraryTemp.Path.Replace(@"\", @"\\")}}"
            }
        }
        """);

        var result = new FactorioInstallLocator([], [libraryFile]).Locate(null, null);

        Assert.Equal(installFolder, result);
    }

    [Fact]
    public void Locate_returns_null_when_preferred_and_candidates_are_not_install_folders()
    {
        using var temp = new TempDirectory();

        var result = new FactorioInstallLocator([temp.Path], []).Locate(temp.Path, null);

        Assert.Null(result);
    }

    private static void CreateDataFolder(string installFolder)
    {
        Directory.CreateDirectory(Path.Combine(installFolder, "data"));
    }
}
