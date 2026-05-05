using FactorioModManager.App.Models;

namespace FactorioModManager.App.ViewModels;

public sealed class ModListItemViewModel : ViewModelBase
{
    private bool _isBeingDragged;

    public ModListItemViewModel(
        ModList modList,
        bool isActive,
        string selectedSizeLabel,
        Func<ModListItemViewModel, Task> activate,
        Action<ModListItemViewModel> edit,
        Func<ModListItemViewModel, Task> rename,
        Func<ModListItemViewModel, Task> delete)
    {
        ModList = modList;
        IsActive = isActive;
        SelectedSizeLabel = selectedSizeLabel;
        ActivateCommand = new AsyncRelayCommand(() => activate(this));
        EditCommand = new RelayCommand(() => edit(this));
        RenameCommand = new AsyncRelayCommand(() => rename(this));
        DeleteCommand = new AsyncRelayCommand(() => delete(this));
    }

    public ModList ModList { get; }
    public string Name => ModList.Name;
    public string FolderPath => ModList.FolderPath;
    public int ModCount => ModList.SelectedMods.Count;
    public bool IsActive { get; }
    public string Description => string.IsNullOrWhiteSpace(ModList.Description) ? "No description." : ModList.Description;
    public string SelectedSizeLabel { get; }
    public string CountLabel => ModCount == 1 ? "1 mod" : $"{ModCount} mods";
    public string ActiveLabel => IsActive ? "Active" : string.Empty;
    public string LastPlayedLabel => ModList.LastActivatedUtc is null
        ? "never activated"
        : $"last activated {ModList.LastActivatedUtc.Value.LocalDateTime:g}";
    public string SummaryLabel => $"{CountLabel} - {SelectedSizeLabel} - {LastPlayedLabel}";

    public bool IsBeingDragged
    {
        get => _isBeingDragged;
        set
        {
            if (SetProperty(ref _isBeingDragged, value))
            {
                OnPropertyChanged(nameof(RowOpacity));
            }
        }
    }

    public double RowOpacity => _isBeingDragged ? 0.18 : 1.0;

    public AsyncRelayCommand ActivateCommand { get; }
    public RelayCommand EditCommand { get; }
    public AsyncRelayCommand RenameCommand { get; }
    public AsyncRelayCommand DeleteCommand { get; }
}
