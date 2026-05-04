using FactorioModManager.App.Models;

namespace FactorioModManager.App.ViewModels;

public sealed class InstalledModViewModel
{
    public InstalledModViewModel(ModInfo modInfo)
    {
        Name = modInfo.Name;
        Title = modInfo.DisplayTitle;
        Author = modInfo.DisplayAuthor;
        Description = modInfo.Description ?? modInfo.Name;
        Size = modInfo.DisplaySize;
        VersionCount = modInfo.VersionCountLabel;
        Versions = modInfo.AvailableVersions.Count == 0 ? modInfo.DisplayVersion : string.Join(", ", modInfo.AvailableVersions);
        HasWarning = modInfo.HasMetadataWarning;
        WarningMessage = modInfo.WarningMessage;
    }

    public string Name { get; }
    public string Title { get; }
    public string Author { get; }
    public string Description { get; }
    public string Size { get; }
    public string VersionCount { get; }
    public string Versions { get; }
    public bool HasWarning { get; }
    public string? WarningMessage { get; }
    public string Subtitle => $"{Author} - {Description}";
}
