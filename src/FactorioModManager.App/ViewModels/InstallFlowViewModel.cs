using FactorioModManager.App.Models.Portal;

namespace FactorioModManager.App.ViewModels;

public sealed record PortalModToInstall(string ModName, PortalModRelease Release);

public sealed class DepsResolutionResult
{
    public required IReadOnlyList<PortalModToInstall> ToInstall { get; init; }
    public required IReadOnlyList<string> ExpansionDeps { get; init; }
    public required IReadOnlyList<string> AlreadySatisfied { get; init; }
    public required IReadOnlyList<string> Incompatibilities { get; init; }
    public string? ErrorMessage { get; init; }
}

public sealed class InstallFlowViewModel : ViewModelBase
{
    public enum FlowState { Picking, ResolvingDeps, ReviewingDeps, Downloading, Done, Error }

    private readonly Func<string, PortalModRelease, CancellationToken, Task<DepsResolutionResult>> _onResolveDeps;
    private readonly Func<string?, string, IReadOnlyList<PortalModToInstall>, IReadOnlyList<string>, IProgress<(string Label, double Value)>, CancellationToken, Task<string?>> _onConfirm;
    private readonly Action _onClose;
    private CancellationTokenSource? _cts;

    private FlowState _state = FlowState.Picking;
    private ModListItemViewModel? _selectedList;
    private bool _isNewList;
    private string _newListName = string.Empty;
    private double _progress;
    private string _currentDownloadLabel = string.Empty;
    private string? _errorMessage;
    private DepsResolutionResult? _resolution;

    public InstallFlowViewModel(
        string modName,
        string modTitle,
        PortalModRelease release,
        IReadOnlyList<ModListItemViewModel> availableLists,
        Func<string, PortalModRelease, CancellationToken, Task<DepsResolutionResult>> onResolveDeps,
        Func<string?, string, IReadOnlyList<PortalModToInstall>, IReadOnlyList<string>, IProgress<(string Label, double Value)>, CancellationToken, Task<string?>> onConfirm,
        Action onClose)
    {
        ModName = modName;
        ModTitle = modTitle;
        Release = release;
        AvailableLists = availableLists;
        _onResolveDeps = onResolveDeps;
        _onConfirm = onConfirm;
        _onClose = onClose;

        _selectedList = availableLists.Count > 0 ? availableLists[0] : null;

        ConfirmCommand = new AsyncRelayCommand(ConfirmAsync, () => CanConfirm);
        ProceedCommand = new AsyncRelayCommand(ExecuteDownloadAsync, () => _state == FlowState.ReviewingDeps);
        CancelCommand = new RelayCommand(Cancel);
        SelectNewListCommand = new RelayCommand(() => IsNewList = true);
    }

    public string ModName { get; }
    public string ModTitle { get; }
    public PortalModRelease Release { get; }
    public IReadOnlyList<ModListItemViewModel> AvailableLists { get; }

    public FlowState State
    {
        get => _state;
        private set
        {
            if (SetProperty(ref _state, value))
            {
                OnPropertyChanged(nameof(IsPicking));
                OnPropertyChanged(nameof(IsResolvingDeps));
                OnPropertyChanged(nameof(IsReviewingDeps));
                OnPropertyChanged(nameof(IsDownloading));
                OnPropertyChanged(nameof(IsDone));
                OnPropertyChanged(nameof(IsError));
                ConfirmCommand.RaiseCanExecuteChanged();
                ProceedCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public ModListItemViewModel? SelectedList
    {
        get => _selectedList;
        set
        {
            if (SetProperty(ref _selectedList, value))
            {
                if (value is not null)
                    IsNewList = false;
                ConfirmCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsNewList
    {
        get => _isNewList;
        set
        {
            if (SetProperty(ref _isNewList, value))
            {
                OnPropertyChanged(nameof(IsExistingList));
                if (value)
                    SelectedList = null;
                ConfirmCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsExistingList => !_isNewList;

    public string NewListName
    {
        get => _newListName;
        set
        {
            if (SetProperty(ref _newListName, value))
                ConfirmCommand.RaiseCanExecuteChanged();
        }
    }

    public double Progress
    {
        get => _progress;
        private set => SetProperty(ref _progress, value);
    }

    public string CurrentDownloadLabel
    {
        get => _currentDownloadLabel;
        private set => SetProperty(ref _currentDownloadLabel, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public IReadOnlyList<PortalModToInstall> AllModsToInstall => _resolution?.ToInstall ?? [];
    public IReadOnlyList<string> ExpansionDeps => _resolution?.ExpansionDeps ?? [];
    public IReadOnlyList<string> AlreadySatisfied => _resolution?.AlreadySatisfied ?? [];
    public IReadOnlyList<string> Incompatibilities => _resolution?.Incompatibilities ?? [];
    public bool HasIncompatibilities => Incompatibilities.Count > 0;
    public bool HasExpansionDeps => ExpansionDeps.Count > 0;
    public bool HasAlreadySatisfied => AlreadySatisfied.Count > 0;
    public bool HasExtraDeps => AllModsToInstall.Count > 1 || ExpansionDeps.Count > 0 || AlreadySatisfied.Count > 0;

    public bool IsPicking => _state == FlowState.Picking;
    public bool IsResolvingDeps => _state == FlowState.ResolvingDeps;
    public bool IsReviewingDeps => _state == FlowState.ReviewingDeps;
    public bool IsDownloading => _state == FlowState.Downloading;
    public bool IsDone => _state == FlowState.Done;
    public bool IsError => _state == FlowState.Error;

    private bool CanConfirm =>
        _state == FlowState.Picking &&
        (_isNewList
            ? !string.IsNullOrWhiteSpace(_newListName)
            : _selectedList is not null);

    public AsyncRelayCommand ConfirmCommand { get; }
    public AsyncRelayCommand ProceedCommand { get; }
    public RelayCommand CancelCommand { get; }
    public RelayCommand SelectNewListCommand { get; }

    private async Task ConfirmAsync()
    {
        State = FlowState.ResolvingDeps;
        _cts = new CancellationTokenSource();

        DepsResolutionResult resolution;
        try
        {
            resolution = await _onResolveDeps(ModName, Release, _cts.Token);
        }
        catch (OperationCanceledException)
        {
            State = FlowState.Picking;
            return;
        }
        finally
        {
            _cts.Dispose();
            _cts = null;
        }

        if (resolution.ErrorMessage is not null)
        {
            ErrorMessage = resolution.ErrorMessage;
            State = FlowState.Error;
            return;
        }

        _resolution = resolution;
        OnPropertyChanged(nameof(AllModsToInstall));
        OnPropertyChanged(nameof(ExpansionDeps));
        OnPropertyChanged(nameof(AlreadySatisfied));
        OnPropertyChanged(nameof(Incompatibilities));
        OnPropertyChanged(nameof(HasIncompatibilities));
        OnPropertyChanged(nameof(HasExpansionDeps));
        OnPropertyChanged(nameof(HasAlreadySatisfied));
        OnPropertyChanged(nameof(HasExtraDeps));

        if (!HasExtraDeps && !HasIncompatibilities)
            await ExecuteDownloadAsync();
        else
            State = FlowState.ReviewingDeps;
    }

    private async Task ExecuteDownloadAsync()
    {
        State = FlowState.Downloading;
        Progress = 0;
        _cts = new CancellationTokenSource();

        var modsToInstall = _resolution?.ToInstall ?? [new PortalModToInstall(ModName, Release)];

        var progressReporter = new Progress<(string Label, double Value)>(p =>
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                CurrentDownloadLabel = p.Label;
                Progress = p.Value;
            }));

        try
        {
            var folderPath = _isNewList ? null : _selectedList?.FolderPath;
            var newName = _isNewList ? _newListName.Trim() : string.Empty;

            var expansionDeps = _resolution?.ExpansionDeps ?? [];
            var error = await _onConfirm(folderPath, newName, modsToInstall, expansionDeps, progressReporter, _cts.Token);

            if (error is not null)
            {
                ErrorMessage = error;
                State = FlowState.Error;
            }
            else
            {
                State = FlowState.Done;
            }
        }
        catch (OperationCanceledException)
        {
            Progress = 0;
            State = _resolution is not null ? FlowState.ReviewingDeps : FlowState.Picking;
        }
        finally
        {
            _cts?.Dispose();
            _cts = null;
        }
    }

    private void Cancel()
    {
        _cts?.Cancel();
        _onClose();
    }
}
