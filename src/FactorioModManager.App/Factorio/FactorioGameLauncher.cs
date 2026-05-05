using System.Diagnostics;

namespace FactorioModManager.App.Factorio;

public sealed class FactorioGameLauncher : IFactorioGameLauncher
{
    private readonly FactorioRuntimePlatform _platform;
    private readonly Action<ProcessStartInfo> _startProcess;

    public FactorioGameLauncher()
        : this(DetectPlatform())
    {
    }

    public FactorioGameLauncher(FactorioRuntimePlatform platform, Action<ProcessStartInfo>? startProcess = null)
    {
        _platform = platform;
        _startProcess = startProcess ?? StartProcess;
    }

    public bool CanLaunch(string? installFolderPath)
    {
        return GetExecutablePath(installFolderPath) is not null;
    }

    public string? GetExecutablePath(string? installFolderPath)
    {
        if (string.IsNullOrWhiteSpace(installFolderPath))
        {
            return null;
        }

        var executablePath = GetExpectedExecutablePath(installFolderPath);
        return File.Exists(executablePath) ? executablePath : null;
    }

    public void Launch(string installFolderPath)
    {
        var executablePath = GetExecutablePath(installFolderPath);
        if (executablePath is null)
        {
            throw new FileNotFoundException("Factorio executable was not found.", GetExpectedExecutablePath(installFolderPath));
        }

        _startProcess(new ProcessStartInfo
        {
            FileName = executablePath,
            WorkingDirectory = Path.GetFullPath(installFolderPath),
            UseShellExecute = false
        });
    }

    private string GetExpectedExecutablePath(string installFolderPath)
    {
        var fileName = _platform == FactorioRuntimePlatform.Windows ? "factorio.exe" : "factorio";
        return Path.Combine(installFolderPath, "bin", "x64", fileName);
    }

    private static FactorioRuntimePlatform DetectPlatform()
    {
        return OperatingSystem.IsWindows()
            ? FactorioRuntimePlatform.Windows
            : FactorioRuntimePlatform.Linux;
    }

    private static void StartProcess(ProcessStartInfo startInfo)
    {
        if (Process.Start(startInfo) is null)
        {
            throw new InvalidOperationException("Factorio could not be started.");
        }
    }
}
