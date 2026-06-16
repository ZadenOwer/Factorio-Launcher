using FactorioModManager.App.Models;

namespace FactorioModManager.App.ViewModels;

public sealed class InstalledModViewModel : ViewModelBase
{
    private bool _isExpanded;

    public InstalledModViewModel(
        ModInfo modInfo,
        Func<InstalledModViewModel, Task> deleteAsync,
        Func<InstalledModViewModel, Task> toggleDisabledAsync)
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
        DependencyCount = modInfo.DependencyCountLabel;
        DependencyItems = DependencyDisplayItem.ParseAll(modInfo.Dependencies);
        SourceZipPaths = modInfo.SourceZipPaths.Count > 0 ? modInfo.SourceZipPaths : [modInfo.SourceZipPath];
        IsDisabled = modInfo.IsDisabled;
        IsExpansionMod = modInfo.Name.Equals("space-age", StringComparison.OrdinalIgnoreCase)
                      || modInfo.Name.Equals("quality", StringComparison.OrdinalIgnoreCase)
                      || modInfo.Name.Equals("elevated-rails", StringComparison.OrdinalIgnoreCase);
        DeleteCommand = new AsyncRelayCommand(() => deleteAsync(this));
        ToggleDisabledCommand = new AsyncRelayCommand(() => toggleDisabledAsync(this));
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
    public string DependencyCount { get; }
    public IReadOnlyList<DependencyDisplayItem> DependencyItems { get; }
    public IReadOnlyList<string> SourceZipPaths { get; }
    public bool IsDisabled { get; }
    public bool IsExpansionMod { get; }
    public bool CanRemove => !IsExpansionMod;
    public AsyncRelayCommand DeleteCommand { get; }
    public AsyncRelayCommand ToggleDisabledCommand { get; }
    public string ToggleDisabledLabel => IsDisabled ? "Enable" : "Disable";
    public double RowOpacity => IsDisabled ? 0.45 : 1.0;
    public bool HasDependencies => DependencyItems.Count > 0;
    public string Subtitle => $"{Author} - {Description}";

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (SetProperty(ref _isExpanded, value))
                OnPropertyChanged(nameof(ExpandChevron));
        }
    }

    public string ExpandChevron => IsExpanded ? "▾" : "▸";
}
