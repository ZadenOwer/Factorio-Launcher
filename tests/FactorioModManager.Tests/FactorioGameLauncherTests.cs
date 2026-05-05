using System.Diagnostics;
using FactorioModManager.App.Factorio;

namespace FactorioModManager.Tests;

public sealed class FactorioGameLauncherTests
{
    [Fact]
    public void GetExecutablePath_resolves_windows_executable()
    {
        using var temp = new TempDirectory();
        var executablePath = CreateExecutable(temp.Path, "factorio.exe");

        var result = new FactorioGameLauncher(FactorioRuntimePlatform.Windows).GetExecutablePath(temp.Path);

        Assert.Equal(executablePath, result);
    }

    [Fact]
    public void GetExecutablePath_resolves_linux_executable()
    {
        using var temp = new TempDirectory();
        var executablePath = CreateExecutable(temp.Path, "factorio");

        var result = new FactorioGameLauncher(FactorioRuntimePlatform.Linux).GetExecutablePath(temp.Path);

        Assert.Equal(executablePath, result);
    }

    [Fact]
    public void Launch_starts_factorio_from_install_folder()
    {
        using var temp = new TempDirectory();
        var executablePath = CreateExecutable(temp.Path, "factorio");
        ProcessStartInfo? capturedStartInfo = null;
        var launcher = new FactorioGameLauncher(
            FactorioRuntimePlatform.Linux,
            startInfo => capturedStartInfo = startInfo);

        launcher.Launch(temp.Path);

        Assert.NotNull(capturedStartInfo);
        Assert.Equal(executablePath, capturedStartInfo.FileName);
        Assert.Equal(Path.GetFullPath(temp.Path), capturedStartInfo.WorkingDirectory);
        Assert.False(capturedStartInfo.UseShellExecute);
    }

    [Fact]
    public void Launch_rejects_missing_executable()
    {
        using var temp = new TempDirectory();
        var launcher = new FactorioGameLauncher(FactorioRuntimePlatform.Linux, _ => { });

        var exception = Assert.Throws<FileNotFoundException>(() => launcher.Launch(temp.Path));

        Assert.EndsWith(Path.Combine("bin", "x64", "factorio"), exception.FileName);
    }

    private static string CreateExecutable(string installFolderPath, string fileName)
    {
        var executablePath = Path.Combine(installFolderPath, "bin", "x64", fileName);
        Directory.CreateDirectory(Path.GetDirectoryName(executablePath)!);
        File.WriteAllText(executablePath, string.Empty);
        return executablePath;
    }
}
