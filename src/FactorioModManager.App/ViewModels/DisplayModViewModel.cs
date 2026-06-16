namespace FactorioModManager.App.ViewModels;

public sealed class DisplayModViewModel : ViewModelBase
{
    private bool _isExpanded;
    private IReadOnlyList<DependencyDisplayItem>? _dependencyItems;

    public required string Name { get; init; }
    public required string Title { get; init; }
    public string? Version { get; init; }
    public string? Author { get; init; }
    public string? Description { get; init; }
    public string Size { get; init; } = "-";
    public bool IsMissing { get; init; }
    public bool HasMultipleVersions { get; init; }
    public string? NewestVersion { get; init; }
    public IReadOnlyList<string> Dependencies { get; init; } = [];

    public string Subtitle => IsMissing
        ? "Selected but no matching zip was found"
        : string.IsNullOrWhiteSpace(Description) ? $"{Author ?? "Unknown"} - {Name}" : $"{Author ?? "Unknown"} - {Description}";
    public string DisplayVersion => string.IsNullOrWhiteSpace(Version) ? "-" : Version;
    public bool IsUsingOlderVersion => HasMultipleVersions &&
        !string.IsNullOrWhiteSpace(NewestVersion) &&
        !string.Equals(DisplayVersion, NewestVersion, StringComparison.OrdinalIgnoreCase);
    public string VersionToolTip => IsUsingOlderVersion
        ? $"Selected version is older than latest available version {NewestVersion}."
        : "Selected mod version";
    public string VersionBackground => IsUsingOlderVersion ? "#3A2412" : "#221D18";
    public string VersionBorderBrush => IsUsingOlderVersion ? "#D97A2C" : "#3A342C";
    public string VersionForeground => IsUsingOlderVersion ? "#F0A455" : "#A89C84";

    public IReadOnlyList<DependencyDisplayItem> DependencyItems =>
        _dependencyItems ??= DependencyDisplayItem.ParseAll(Dependencies);
    public bool HasDependencies => Dependencies.Count > 0;

    public bool IsDisabledInList { get; init; }
    public double RowOpacity => IsDisabledInList ? 0.45 : 1.0;
    public string ToggleListDisabledLabel => IsDisabledInList ? "Enable in list" : "Disable in list";

    public Func<Task>? OnRemove { private get; set; }
    public bool CanRemoveFromList => OnRemove is not null;

    private AsyncRelayCommand? _removeCommand;
    public AsyncRelayCommand RemoveCommand => _removeCommand ??= new AsyncRelayCommand(
        () => OnRemove?.Invoke() ?? Task.CompletedTask,
        () => CanRemoveFromList);

    public Func<Task>? OnToggleListDisabled { private get; set; }
    public bool CanToggleListDisabled => OnToggleListDisabled is not null;

    private AsyncRelayCommand? _toggleListDisabledCommand;
    public AsyncRelayCommand ToggleListDisabledCommand => _toggleListDisabledCommand ??= new AsyncRelayCommand(
        () => OnToggleListDisabled?.Invoke() ?? Task.CompletedTask,
        () => CanToggleListDisabled);

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
