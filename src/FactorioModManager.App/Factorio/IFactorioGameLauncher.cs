namespace FactorioModManager.App.Factorio;

public interface IFactorioGameLauncher
{
    bool CanLaunch(string? installFolderPath);
    string? GetExecutablePath(string? installFolderPath);
    void Launch(string installFolderPath);
}
