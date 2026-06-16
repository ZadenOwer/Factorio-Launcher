using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json;
using FactorioModManager.App.Factorio;
using FactorioModManager.App.Models;
using FactorioModManager.App.Models.Portal;
using FactorioModManager.App.Services;

namespace FactorioModManager.App.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    private readonly IDialogService _dialogService;
    private readonly AppSettingsService _appSettingsService;
    private readonly FolderValidator _folderValidator;
    private readonly FactorioInstallLocator _factorioInstallLocator;
    private readonly IFactorioGameLauncher _factorioGameLauncher;
    private readonly IFactorioGameRunningDetector _factorioGameRunningDetector;
    private readonly ModScanner _modScanner;
    private readonly ModListDetector _modListDetector;
    private readonly ModListReader _modListReader = new();
    private readonly ModListWriter _modListWriter;
    private readonly ModListMetadataService _metadataService;
    private readonly ModSettingsManager _modSettingsManager;
    private readonly ModListActivator _modListActivator;
    private readonly ModListFileManager _modListFileManager;
    private readonly NameValidator _nameValidator;

    private readonly ModPortalService _modPortalService;
    private readonly ModDownloadService _modDownloadService;
    private readonly ModListEntryWriter _modListEntryWriter = new();
    private CancellationTokenSource? _portalSearchCts;
    private bool _portalInitialLoadDone;
    private PortalModDetailViewModel? _portalDetail;
    private bool _isPortalDetailLoading;
    private string? _portalDetailError;
    private InstallFlowViewModel? _installFlow;
    private readonly List<ModInfo> _availableMods = [];
    private readonly List<ModInfo> _disabledMods = [];
    private readonly List<EditableModViewModel> _allEditableMods = [];
    private readonly List<ModListItemViewModel> _allModListItems = [];
    private ModListItemViewModel? _importSourceList;
    private AppSettings _settings = new();

    private string? _modsFolderPath;
    private string? _factorioInstallFolderPath;
    private string _folderStatus = "Select your Factorio mods folder to begin.";
    private bool _isFolderValid;
    private string _statusMessage = "Ready.";
    private string? _errorMessage;
    private ModListItemViewModel? _selectedModList;
    private bool _isEditMode;
    private bool _isCreating;
    private string? _duplicateSourceFolderPath;
    private bool _importRootSettingsOnSave;
    private string? _searchText;
    private string? _listSearchText;
    private string _activeTab = "Lists";
    private string _editorSortMode = EditorSortModes.Active;
    private string _portalSearchText = string.Empty;
    private string _portalStatusMessage = string.Empty;
    private bool _isPortalSearching;
    private bool _hasPortalError;
    private string? _portalErrorMessage;
    private int _portalPage = 1;
    private int _portalPageCount = 1;
    private int _portalTotalCount;
    private string? _portalDebugRawJson;
    private string _draftName = string.Empty;
    private string _draftDescription = string.Empty;
    private int _draftSelectedCount;
    private string _draftSizeLabel = "-";
    private string? _activeListName;
    private bool _isFactorioRunning;
    private bool _isHandlingFactorioClosed;

    public MainWindowViewModel(
        IDialogService dialogService,
        AppSettingsService appSettingsService,
        FolderValidator folderValidator,
        FactorioInstallLocator factorioInstallLocator,
        IFactorioGameLauncher factorioGameLauncher,
        IFactorioGameRunningDetector factorioGameRunningDetector,
        ModScanner modScanner,
        ModListDetector modListDetector,
        ModListWriter modListWriter,
        ModListMetadataService modListMetadataService,
        ModSettingsManager modSettingsManager,
        BackupService backupService,
        ModListActivator modListActivator,
        ModListFileManager modListFileManager,
        NameValidator nameValidator,
        ModPortalService modPortalService,
        ModDownloadService modDownloadService)
    {
        _dialogService = dialogService;
        _appSettingsService = appSettingsService;
        _folderValidator = folderValidator;
        _factorioInstallLocator = factorioInstallLocator;
        _factorioGameLauncher = factorioGameLauncher;
        _factorioGameRunningDetector = factorioGameRunningDetector;
        _modScanner = modScanner;
        _modListDetector = modListDetector;
        _modListWriter = modListWriter;
        _metadataService = modListMetadataService;
        _modSettingsManager = modSettingsManager;
        _modListActivator = modListActivator;
        _modListFileManager = modListFileManager;
        _nameValidator = nameValidator;
        _modPortalService = modPortalService;
        _modDownloadService = modDownloadService;

        BrowseFolderCommand = new AsyncRelayCommand(BrowseFolderAsync);
        BrowseInstallFolderCommand = new AsyncRelayCommand(BrowseInstallFolderAsync);
        LaunchFactorioCommand = new AsyncRelayCommand(LaunchFactorioAsync, () => CanLaunchFactorio);
        RefreshCommand = new AsyncRelayCommand(RefreshAsync, () => IsFolderValid);
        CreateModListCommand = new RelayCommand(StartCreateDraft, () => IsFolderValid && IsNormalMode);
        SaveEditCommand = new AsyncRelayCommand(SaveEditAsync, () => CanSaveEdit);
        CancelEditCommand = new RelayCommand(CancelEdit, () => IsEditMode);
        EditSelectedCommand = new RelayCommand(EditSelected, () => CanEditSelected);
        DuplicateSelectedCommand = new RelayCommand(DuplicateSelected, () => SelectedModList is not null && IsNormalMode);
        ImportCurrentToDraftCommand = new AsyncRelayCommand(ImportCurrentToDraftAsync, () => IsEditMode);
        ActivateSelectedCommand = new AsyncRelayCommand(ActivateSelectedAsync, () => CanActivateSelected);
        DeleteSelectedCommand = new AsyncRelayCommand(DeleteSelectedAsync, () => SelectedModList is not null && IsNormalMode);
        ImportFromListCommand = new AsyncRelayCommand(ImportFromListAsync, () => CanImportFromList);
        ShowListsCommand = new RelayCommand(() => ActiveTab = "Lists");
        ShowModsCommand = new RelayCommand(() => ActiveTab = "Mods");
        ShowExploreCommand = new RelayCommand(() => ActiveTab = "Explore");
        ShowSettingsCommand = new RelayCommand(() => ActiveTab = "Settings");
        SelectActiveListCommand = new RelayCommand(SelectActiveList, () => HasActiveList);
        SortEditorByNameCommand = new RelayCommand(() => EditorSortMode = EditorSortModes.Name);
        SortEditorByActiveCommand = new RelayCommand(() => EditorSortMode = EditorSortModes.Active);
        PortalSearchCommand = new AsyncRelayCommand(PortalSearchAsync, () => HasPortalCredentials && !IsPortalSearching);
        PortalPrevPageCommand = new AsyncRelayCommand(() => RunPortalAsync(PortalPage - 1), () => CanGoPortalPrev);
        PortalNextPageCommand = new AsyncRelayCommand(() => RunPortalAsync(PortalPage + 1), () => CanGoPortalNext);
        SavePortalCredentialsCommand = new AsyncRelayCommand(SavePortalCredentialsAsync);
        BackToPortalListCommand = new RelayCommand(() => { PortalDetail = null; InstallFlow = null; });
        StartInstallCommand = new RelayCommand(
            () => { if (PortalDetail is not null) StartInstall(PortalDetail); },
            () => PortalDetail?.CanInstall == true && !IsInstallFlowActive);

        foreach (var cat in PortalCategories)
            cat.PropertyChanged += (_, _) =>
            {
                if (_portalInitialLoadDone)
                    _ = RunPortalAsync(1);
            };
    }

    public string? ModsFolderPath
    {
        get => _modsFolderPath;
        private set
        {
            if (SetProperty(ref _modsFolderPath, value))
            {
                OnPropertyChanged(nameof(FolderPathDisplay));
                OnPropertyChanged(nameof(FolderPathCompact));
            }
        }
    }

    public string FolderPathDisplay => string.IsNullOrWhiteSpace(ModsFolderPath)
        ? "No folder selected"
        : ModsFolderPath;

    public string FolderPathCompact
    {
        get
        {
            if (string.IsNullOrWhiteSpace(ModsFolderPath))
            {
                return "no folder";
            }

            var parts = ModsFolderPath
                .Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries)
                .TakeLast(3);
            return string.Join("/", parts);
        }
    }

    public string? FactorioInstallFolderPath
    {
        get => _factorioInstallFolderPath;
        private set
        {
            if (SetProperty(ref _factorioInstallFolderPath, value))
            {
                OnPropertyChanged(nameof(FactorioInstallFolderDisplay));
                OnPropertyChanged(nameof(FactorioInstallFolderCompact));
                OnPropertyChanged(nameof(CanLaunchFactorio));
                OnPropertyChanged(nameof(LaunchFactorioToolTip));
                LaunchFactorioCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string FactorioInstallFolderDisplay => string.IsNullOrWhiteSpace(FactorioInstallFolderPath)
        ? "No Factorio install folder selected"
        : FactorioInstallFolderPath;

    public string FactorioInstallFolderCompact
    {
        get
        {
            if (string.IsNullOrWhiteSpace(FactorioInstallFolderPath))
            {
                return "install folder";
            }

            var parts = FactorioInstallFolderPath
                .Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries)
                .TakeLast(2);
            return string.Join("/", parts);
        }
    }

    public bool CanLaunchFactorio => !IsFactorioRunning && _factorioGameLauncher.CanLaunch(FactorioInstallFolderPath);

    public string LaunchFactorioToolTip => CanLaunchFactorio
        ? "Start Factorio"
        : IsFactorioRunning
            ? "Factorio is already running."
            : "Factorio executable was not found in the selected installation folder.";

    public string FolderStatus
    {
        get => _folderStatus;
        private set => SetProperty(ref _folderStatus, value);
    }

    public bool IsFolderValid
    {
        get => _isFolderValid;
        private set
        {
            if (SetProperty(ref _isFolderValid, value))
            {
                OnPropertyChanged(nameof(CanUseWorkspace));
                RefreshCommand.RaiseCanExecuteChanged();
                CreateModListCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool CanUseWorkspace => IsFolderValid;

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (SetProperty(ref _errorMessage, value))
            {
                OnPropertyChanged(nameof(HasError));
            }
        }
    }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public ObservableCollection<ModListItemViewModel> ModLists { get; } = [];
    public ObservableCollection<DisplayModViewModel> SelectedMods { get; } = [];
    public ObservableCollection<EditableModViewModel> EditableMods { get; } = [];
    public ObservableCollection<InstalledModViewModel> InstalledMods { get; } = [];

    public ModListItemViewModel? SelectedModList
    {
        get => _selectedModList;
        set
        {
            var previousSelection = _selectedModList;
            if (SetProperty(ref _selectedModList, value))
            {
                if (previousSelection is not null)
                {
                    previousSelection.IsSelected = false;
                }

                if (_selectedModList is not null)
                {
                    _selectedModList.IsSelected = true;
                }

                OnPropertyChanged(nameof(HasSelectedModList));
                OnPropertyChanged(nameof(SelectedListName));
                OnPropertyChanged(nameof(SelectedListDescription));
                OnPropertyChanged(nameof(SelectedListSummary));
                OnPropertyChanged(nameof(CanActivateSelected));
                OnPropertyChanged(nameof(CanEditSelected));
                OnPropertyChanged(nameof(EditSelectedToolTip));
                OnPropertyChanged(nameof(ActivateSelectedToolTip));
                OnPropertyChanged(nameof(CanSaveEdit));
                OnPropertyChanged(nameof(ImportSourceOptions));
                OnPropertyChanged(nameof(CanImportFromList));
                RaiseSelectionCommandStates();
                if (!IsEditMode)
                {
                    LoadSelectedMods();
                }
            }
        }
    }

    public bool HasSelectedModList => SelectedModList is not null;

    public ModListItemViewModel? ImportSourceList
    {
        get => _importSourceList;
        set
        {
            if (SetProperty(ref _importSourceList, value))
            {
                OnPropertyChanged(nameof(CanImportFromList));
                ImportFromListCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public IReadOnlyList<ModListItemViewModel> ImportSourceOptions =>
        _allModListItems.Where(m => m != SelectedModList).ToList();

    public bool CanImportFromList =>
        SelectedModList is not null &&
        ImportSourceList is not null &&
        IsNormalMode &&
        !IsFactorioRunning;

    public string SelectedListName => SelectedModList?.Name ?? "No mod list selected";
    public string SelectedListDescription => SelectedModList?.Description ?? string.Empty;
    public string SelectedListSummary => SelectedModList?.SummaryLabel ?? "Select or create a mod list.";

    public bool IsEditMode
    {
        get => _isEditMode;
        private set
        {
            if (SetProperty(ref _isEditMode, value))
            {
                OnPropertyChanged(nameof(IsNormalMode));
                OnPropertyChanged(nameof(IsEditorVisible));
                OnPropertyChanged(nameof(CanActivateSelected));
                OnPropertyChanged(nameof(CanEditSelected));
                OnPropertyChanged(nameof(CanSaveEdit));
                OnPropertyChanged(nameof(EditSelectedToolTip));
                OnPropertyChanged(nameof(ActivateSelectedToolTip));
                SaveEditCommand.RaiseCanExecuteChanged();
                CancelEditCommand.RaiseCanExecuteChanged();
                ImportCurrentToDraftCommand.RaiseCanExecuteChanged();
                CreateModListCommand.RaiseCanExecuteChanged();
                RaiseSelectionCommandStates();
            }
        }
    }

    public bool IsNormalMode => !IsEditMode;
    public bool IsEditorVisible => IsEditMode;

    public string? SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                ApplyEditorFilter();
            }
        }
    }

    public string? ListSearchText
    {
        get => _listSearchText;
        set
        {
            if (SetProperty(ref _listSearchText, value))
            {
                ApplyListFilter();
            }
        }
    }

    public string ActiveTab
    {
        get => _activeTab;
        private set
        {
            if (SetProperty(ref _activeTab, value))
            {
                OnPropertyChanged(nameof(IsListsTabActive));
                OnPropertyChanged(nameof(IsModsTabActive));
                OnPropertyChanged(nameof(IsExploreTabActive));
                OnPropertyChanged(nameof(IsSettingsTabActive));
                OnPropertyChanged(nameof(IsListsTabInactive));
                OnPropertyChanged(nameof(IsModsTabInactive));
                OnPropertyChanged(nameof(IsExploreTabInactive));
                OnPropertyChanged(nameof(IsSettingsTabInactive));

                if (string.Equals(value, "Explore", StringComparison.Ordinal) &&
                    !_portalInitialLoadDone &&
                    HasPortalCredentials)
                {
                    _ = RunPortalAsync(1);
                }
            }
        }
    }

    public bool IsListsTabActive => string.Equals(ActiveTab, "Lists", StringComparison.Ordinal);
    public bool IsModsTabActive => string.Equals(ActiveTab, "Mods", StringComparison.Ordinal);
    public bool IsExploreTabActive => string.Equals(ActiveTab, "Explore", StringComparison.Ordinal);
    public bool IsSettingsTabActive => string.Equals(ActiveTab, "Settings", StringComparison.Ordinal);
    public bool IsListsTabInactive => !IsListsTabActive;
    public bool IsModsTabInactive => !IsModsTabActive;
    public bool IsExploreTabInactive => !IsExploreTabActive;
    public bool IsSettingsTabInactive => !IsSettingsTabActive;

    public string PortalSearchText
    {
        get => _portalSearchText;
        set => SetProperty(ref _portalSearchText, value);
    }

    public string PortalStatusMessage
    {
        get => _portalStatusMessage;
        private set => SetProperty(ref _portalStatusMessage, value);
    }

    public bool IsPortalSearching
    {
        get => _isPortalSearching;
        private set
        {
            if (SetProperty(ref _isPortalSearching, value))
            {
                OnPropertyChanged(nameof(CanGoPortalPrev));
                OnPropertyChanged(nameof(CanGoPortalNext));
            }
        }
    }

    public bool HasPortalError
    {
        get => _hasPortalError;
        private set => SetProperty(ref _hasPortalError, value);
    }

    public string? PortalErrorMessage
    {
        get => _portalErrorMessage;
        private set => SetProperty(ref _portalErrorMessage, value);
    }

    public int PortalPage
    {
        get => _portalPage;
        private set
        {
            if (SetProperty(ref _portalPage, value))
            {
                OnPropertyChanged(nameof(CanGoPortalPrev));
                OnPropertyChanged(nameof(CanGoPortalNext));
                OnPropertyChanged(nameof(PortalPageLabel));
            }
        }
    }

    public int PortalPageCount
    {
        get => _portalPageCount;
        private set
        {
            if (SetProperty(ref _portalPageCount, value))
            {
                OnPropertyChanged(nameof(CanGoPortalPrev));
                OnPropertyChanged(nameof(CanGoPortalNext));
                OnPropertyChanged(nameof(PortalPageLabel));
            }
        }
    }

    public int PortalTotalCount
    {
        get => _portalTotalCount;
        private set => SetProperty(ref _portalTotalCount, value);
    }

    public string PortalPageLabel => $"Page {PortalPage} of {PortalPageCount}";
    public bool CanGoPortalPrev => PortalPage > 1 && !IsPortalSearching;
    public bool CanGoPortalNext => PortalPage < PortalPageCount && !IsPortalSearching;

    public bool HasPortalCredentials =>
        !string.IsNullOrWhiteSpace(_settings.PortalUsername) &&
        !string.IsNullOrWhiteSpace(_settings.PortalToken);
    public bool MissingPortalCredentials => !HasPortalCredentials;

    public string PortalUsernameEntry
    {
        get => _settings.PortalUsername ?? string.Empty;
        set
        {
            _settings.PortalUsername = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasPortalCredentials));
            OnPropertyChanged(nameof(MissingPortalCredentials));
            PortalSearchCommand.RaiseCanExecuteChanged();
        }
    }

    public string PortalTokenEntry
    {
        get => _settings.PortalToken ?? string.Empty;
        set
        {
            _settings.PortalToken = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasPortalCredentials));
            OnPropertyChanged(nameof(MissingPortalCredentials));
            PortalSearchCommand.RaiseCanExecuteChanged();
        }
    }

    public ObservableCollection<PortalModViewModel> PortalMods { get; } = [];

    public IReadOnlyList<PortalCategoryViewModel> PortalCategories { get; } = InitPortalCategories();

    private static IReadOnlyList<PortalCategoryViewModel> InitPortalCategories() =>
    [
        new("content",       "Content"),
        new("overhaul",      "Overhaul"),
        new("tweaks",        "Tweaks"),
        new("utilities",     "Utilities"),
        new("mod-packs",     "Mod Packs"),
        new("scenarios",     "Scenarios"),
        new("no-category",   "No Category"),
        new("localizations", "Localizations"),
        new("internal",      "Internal"),
    ];

    public string EditorSortMode
    {
        get => _editorSortMode;
        private set
        {
            if (SetProperty(ref _editorSortMode, value))
            {
                OnPropertyChanged(nameof(IsEditorSortedByName));
                OnPropertyChanged(nameof(IsEditorSortedByActive));
                OnPropertyChanged(nameof(IsEditorNotSortedByName));
                OnPropertyChanged(nameof(IsEditorNotSortedByActive));
                ApplyEditorFilter();
            }
        }
    }

    public bool IsEditorSortedByName => string.Equals(EditorSortMode, EditorSortModes.Name, StringComparison.Ordinal);
    public bool IsEditorSortedByActive => string.Equals(EditorSortMode, EditorSortModes.Active, StringComparison.Ordinal);
    public bool IsEditorNotSortedByName => !IsEditorSortedByName;
    public bool IsEditorNotSortedByActive => !IsEditorSortedByActive;

    public string DraftName
    {
        get => _draftName;
        set => SetProperty(ref _draftName, value);
    }

    public string DraftDescription
    {
        get => _draftDescription;
        set => SetProperty(ref _draftDescription, value);
    }

    public int DraftSelectedCount
    {
        get => _draftSelectedCount;
        private set
        {
            if (SetProperty(ref _draftSelectedCount, value))
            {
                OnPropertyChanged(nameof(DraftSummaryLabel));
            }
        }
    }

    public string DraftSizeLabel
    {
        get => _draftSizeLabel;
        private set
        {
            if (SetProperty(ref _draftSizeLabel, value))
            {
                OnPropertyChanged(nameof(DraftSummaryLabel));
            }
        }
    }

    public string DraftSummaryLabel => $"{DraftSelectedCount} mods selected - {DraftSizeLabel}";

    public bool HasActiveList => !string.IsNullOrWhiteSpace(ActiveListName);
    public bool IsFactorioRunning
    {
        get => _isFactorioRunning;
        private set
        {
            if (SetProperty(ref _isFactorioRunning, value))
            {
                OnPropertyChanged(nameof(CanLaunchFactorio));
                OnPropertyChanged(nameof(LaunchFactorioToolTip));
                OnPropertyChanged(nameof(CanActivateSelected));
                OnPropertyChanged(nameof(ActivateSelectedToolTip));
                OnPropertyChanged(nameof(CanEditSelected));
                OnPropertyChanged(nameof(EditSelectedToolTip));
                OnPropertyChanged(nameof(CanSaveEdit));
                LaunchFactorioCommand.RaiseCanExecuteChanged();
                SaveEditCommand.RaiseCanExecuteChanged();
                RaiseSelectionCommandStates();
            }
        }
    }

    public bool CanActivateSelected => SelectedModList is not null
        && !SelectedModList.IsActive
        && IsNormalMode
        && !IsFactorioRunning
        && FindMissingDependencies(SelectedModList).Count == 0;
    public string ActivateSelectedToolTip
    {
        get
        {
            if (IsFactorioRunning)
                return "Close Factorio before activating a different mod list.";
            if (SelectedModList is not null)
            {
                var missing = FindMissingDependencies(SelectedModList);
                if (missing.Count > 0)
                {
                    var lines = missing
                        .GroupBy(x => x.Mod, StringComparer.OrdinalIgnoreCase)
                        .Select(g => $"• {g.Key} requires: {string.Join(", ", g.Select(x => x.Dep))}");
                    return $"Missing required dependencies:\n{string.Join("\n", lines)}";
                }
            }
            return "Activate mod list";
        }
    }
    public bool CanEditSelected => SelectedModList is not null && IsNormalMode && !(IsFactorioRunning && SelectedModList.IsActive);
    public string EditSelectedToolTip => SelectedModList is null
        ? "Select a mod list to edit."
        : IsFactorioRunning && SelectedModList.IsActive
            ? "Close Factorio before editing the active mod list."
            : "Edit mod list";
    public bool CanSaveEdit => IsEditMode && !(!_isCreating && SelectedModList?.IsActive == true && IsFactorioRunning);

    public string? ActiveListName
    {
        get => _activeListName;
        private set
        {
            if (SetProperty(ref _activeListName, value))
            {
                OnPropertyChanged(nameof(HasActiveList));
                SelectActiveListCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public int AvailableModCount => _availableMods.Count;
    public int DisabledModCount => _disabledMods.Count;
    public int ModListCount => _allModListItems.Count;
    public int VisibleModListCount => ModLists.Count;

    public AsyncRelayCommand BrowseFolderCommand { get; }
    public AsyncRelayCommand BrowseInstallFolderCommand { get; }
    public AsyncRelayCommand LaunchFactorioCommand { get; }
    public AsyncRelayCommand RefreshCommand { get; }
    public RelayCommand CreateModListCommand { get; }
    public AsyncRelayCommand SaveEditCommand { get; }
    public RelayCommand CancelEditCommand { get; }
    public RelayCommand EditSelectedCommand { get; }
    public RelayCommand DuplicateSelectedCommand { get; }
    public AsyncRelayCommand ImportCurrentToDraftCommand { get; }
    public AsyncRelayCommand ActivateSelectedCommand { get; }
    public AsyncRelayCommand DeleteSelectedCommand { get; }
    public AsyncRelayCommand ImportFromListCommand { get; }
    public RelayCommand ShowListsCommand { get; }
    public RelayCommand ShowModsCommand { get; }
    public RelayCommand ShowExploreCommand { get; }
    public RelayCommand ShowSettingsCommand { get; }
    public RelayCommand SelectActiveListCommand { get; }
    public RelayCommand SortEditorByNameCommand { get; }
    public RelayCommand SortEditorByActiveCommand { get; }
    public AsyncRelayCommand PortalSearchCommand { get; }
    public AsyncRelayCommand PortalPrevPageCommand { get; }
    public AsyncRelayCommand PortalNextPageCommand { get; }
    public AsyncRelayCommand SavePortalCredentialsCommand { get; }
    public RelayCommand BackToPortalListCommand { get; }
    public RelayCommand StartInstallCommand { get; }

    public PortalModDetailViewModel? PortalDetail
    {
        get => _portalDetail;
        private set
        {
            if (SetProperty(ref _portalDetail, value))
            {
                OnPropertyChanged(nameof(IsPortalDetailActive));
                OnPropertyChanged(nameof(IsPortalListActive));
                StartInstallCommand?.RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsPortalDetailLoading
    {
        get => _isPortalDetailLoading;
        private set => SetProperty(ref _isPortalDetailLoading, value);
    }

    public string? PortalDetailError
    {
        get => _portalDetailError;
        private set
        {
            if (SetProperty(ref _portalDetailError, value))
                OnPropertyChanged(nameof(HasPortalDetailError));
        }
    }

    public bool HasPortalDetailError => !string.IsNullOrEmpty(_portalDetailError);
    public bool IsPortalDetailActive => _portalDetail is not null || _isPortalDetailLoading;
    public bool IsPortalListActive => !IsPortalDetailActive;

    public InstallFlowViewModel? InstallFlow
    {
        get => _installFlow;
        private set
        {
            if (SetProperty(ref _installFlow, value))
            {
                OnPropertyChanged(nameof(IsInstallFlowActive));
                StartInstallCommand?.RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsInstallFlowActive => _installFlow is not null;

    public async Task InitializeAsync()
    {
        _settings = await _appSettingsService.LoadAsync();
        ModsFolderPath = _settings.LastModsFolderPath;
        FactorioInstallFolderPath = _settings.FactorioInstallFolderPath;
        RefreshFactorioRunningState();
        ValidateFolder();
        await AutoDetectFactorioInstallFolderAsync();

        if (IsFolderValid)
        {
            await RefreshAsync();
        }

        OnPropertyChanged(nameof(HasPortalCredentials));
        OnPropertyChanged(nameof(MissingPortalCredentials));
        OnPropertyChanged(nameof(PortalUsernameEntry));
        OnPropertyChanged(nameof(PortalTokenEntry));
        PortalSearchCommand.RaiseCanExecuteChanged();
    }

    private async Task BrowseFolderAsync()
    {
        var selected = await _dialogService.PickFolderAsync("Select Factorio mods folder");
        if (string.IsNullOrWhiteSpace(selected))
        {
            return;
        }

        ModsFolderPath = selected;
        if (!PathsEqual(_settings.LastModsFolderPath, selected))
        {
            _settings.ActiveModListFolderPath = null;
        }

        _settings.LastModsFolderPath = selected;
        await _appSettingsService.SaveAsync(_settings);
        ValidateFolder();
        await RefreshAsync();
    }

    private async Task BrowseInstallFolderAsync()
    {
        var selected = await _dialogService.PickFolderAsync("Select Factorio installation folder");
        if (string.IsNullOrWhiteSpace(selected))
        {
            return;
        }

        if (!Directory.Exists(Path.Combine(selected, "data")))
        {
            await _dialogService.ShowErrorAsync(
                "Invalid Factorio installation folder",
                "Select the Factorio installation folder that contains the data folder.");
            return;
        }

        FactorioInstallFolderPath = selected;
        _settings.FactorioInstallFolderPath = selected;
        await _appSettingsService.SaveAsync(_settings);

        if (IsFolderValid)
        {
            await RefreshAsync();
        }
    }

    private async Task LaunchFactorioAsync()
    {
        RefreshFactorioRunningState();
        if (IsFactorioRunning)
        {
            await _dialogService.ShowErrorAsync("Launch failed", "Factorio is already running.");
            return;
        }

        if (string.IsNullOrWhiteSpace(FactorioInstallFolderPath))
        {
            await _dialogService.ShowErrorAsync("Launch failed", "Select a Factorio installation folder before starting the game.");
            return;
        }

        try
        {
            _factorioGameLauncher.Launch(FactorioInstallFolderPath);
            IsFactorioRunning = true;
            StatusMessage = "Started Factorio.";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            ErrorMessage = ex.Message;
            await _dialogService.ShowErrorAsync("Launch failed", ex.Message);
        }
    }

    private async Task AutoDetectFactorioInstallFolderAsync()
    {
        var detectedFolderPath = _factorioInstallLocator.Locate(FactorioInstallFolderPath, ModsFolderPath);
        if ((string.IsNullOrWhiteSpace(detectedFolderPath) && string.IsNullOrWhiteSpace(FactorioInstallFolderPath)) ||
            PathsEqual(detectedFolderPath, FactorioInstallFolderPath))
        {
            return;
        }

        FactorioInstallFolderPath = detectedFolderPath;
        _settings.FactorioInstallFolderPath = detectedFolderPath;
        await _appSettingsService.SaveAsync(_settings);
    }

    private void ValidateFolder()
    {
        var result = _folderValidator.Validate(ModsFolderPath);
        IsFolderValid = result.IsValid;
        FolderStatus = result.Message;
        if (!result.IsValid)
        {
            _allModListItems.Clear();
            ModLists.Clear();
            SelectedMods.Clear();
            EditableMods.Clear();
            InstalledMods.Clear();
            _availableMods.Clear();
            _disabledMods.Clear();
            ActiveListName = null;
        }
    }

    private async Task RefreshAsync()
    {
        ErrorMessage = null;
        RefreshFactorioRunningState();
        ValidateFolder();
        if (!IsFolderValid || string.IsNullOrWhiteSpace(ModsFolderPath))
        {
            StatusMessage = FolderStatus;
            return;
        }

        try
        {
            var previousSelection = SelectedModList?.Name;
            await AutoDetectFactorioInstallFolderAsync();
            var allMods = _modScanner.Scan(ModsFolderPath, FactorioInstallFolderPath);
            _availableMods.Clear();
            _availableMods.AddRange(allMods.Where(m => !m.IsDisabled));
            _disabledMods.Clear();
            _disabledMods.AddRange(allMods.Where(m => m.IsDisabled));
            LoadInstalledMods();

            var detectedModLists = _modListDetector.Detect(ModsFolderPath).ToList();
            var activeFolderPath = GetRememberedActiveFolderPath(detectedModLists);

            _allModListItems.Clear();
            ImportSourceList = null;
            foreach (var modList in ApplySavedModListOrder(detectedModLists))
            {
                var isActive = PathsEqual(activeFolderPath, modList.FolderPath);
                _allModListItems.Add(new ModListItemViewModel(
                    modList,
                    isActive,
                    CalculateSelectedSizeLabel(modList.SelectedMods),
                    ActivateAsync,
                    StartEdit,
                    RenameAsync,
                    DeleteAsync));
            }

            await SaveCurrentModListOrderAsync();
            ActiveListName = _allModListItems.FirstOrDefault(item => item.IsActive)?.Name;
            ApplyListFilter();

            SelectedModList = _allModListItems.FirstOrDefault(item =>
                string.Equals(item.Name, previousSelection, StringComparison.OrdinalIgnoreCase)) ??
                _allModListItems.FirstOrDefault();

            if (!IsEditMode)
            {
                LoadSelectedMods();
            }

            OnPropertyChanged(nameof(AvailableModCount));
            OnPropertyChanged(nameof(DisabledModCount));
            OnPropertyChanged(nameof(ModListCount));
            StatusMessage = $"Loaded {_availableMods.Count} mods and {_allModListItems.Count} mod lists.";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ErrorMessage = ex.Message;
            await _dialogService.ShowErrorAsync("Refresh failed", ex.Message);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            ErrorMessage = ex.Message;
            StatusMessage = "Refresh failed while reading mod data.";
            await _dialogService.ShowErrorAsync("Refresh failed", ex.Message);
        }
    }

    private void StartCreateDraft()
    {
        if (!EnsureWorkspace())
        {
            return;
        }

        _isCreating = true;
        _duplicateSourceFolderPath = null;
        _importRootSettingsOnSave = false;
        SelectedModList = null;
        DraftName = GenerateUniqueDraftName();
        DraftDescription = string.Empty;
        BeginEdit([], new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
        ActiveTab = "Lists";
        StatusMessage = "Creating a new mod list.";
    }

    private void EditSelected()
    {
        if (SelectedModList is not null)
        {
            StartEdit(SelectedModList);
        }
    }

    private void DuplicateSelected()
    {
        if (SelectedModList is not null)
        {
            StartDuplicate(SelectedModList);
        }
    }

    private void StartEdit(ModListItemViewModel item)
    {
        RefreshFactorioRunningState();
        if (IsFactorioRunning && item.IsActive)
        {
            ErrorMessage = "Close Factorio before editing the active mod list.";
            StatusMessage = "Editing the active mod list is blocked while Factorio is running.";
            return;
        }

        SelectedModList = item;
        _isCreating = false;
        _duplicateSourceFolderPath = null;
        _importRootSettingsOnSave = false;
        DraftName = item.Name;
        DraftDescription = item.ModList.Description;
        BeginEdit(item.ModList.SelectedMods, item.ModList.SelectedVersions);
        ActiveTab = "Lists";
        StatusMessage = $"Editing {item.Name}.";
    }

    private void StartDuplicate(ModListItemViewModel item)
    {
        SelectedModList = item;
        _isCreating = true;
        _duplicateSourceFolderPath = item.FolderPath;
        _importRootSettingsOnSave = false;
        DraftName = GenerateUniqueDuplicateName(item.Name);
        DraftDescription = item.ModList.Description;
        BeginEdit(item.ModList.SelectedMods, item.ModList.SelectedVersions);
        ActiveTab = "Lists";
        StatusMessage = $"Duplicating {item.Name}.";
    }

    private void BeginEdit(IEnumerable<string> selectedModNames, IReadOnlyDictionary<string, string> selectedVersions)
    {
        var selected = selectedModNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        _allEditableMods.Clear();

        foreach (var mod in _availableMods)
        {
            selectedVersions.TryGetValue(mod.Name, out var selectedVersion);
            var editable = new EditableModViewModel(mod, selected.Contains(mod.Name), selectedVersion);
            editable.PropertyChanged += EditableModChanged;
            _allEditableMods.Add(editable);
        }

        SearchText = string.Empty;
        EditorSortMode = EditorSortModes.Active;
        IsEditMode = true;
        ApplyEditorFilter();
        UpdateDraftSummary();
    }

    private async Task SaveEditAsync()
    {
        if (!EnsureWorkspace())
        {
            return;
        }

        RefreshFactorioRunningState();
        if (!_isCreating && SelectedModList?.IsActive == true && IsFactorioRunning)
        {
            await _dialogService.ShowErrorAsync(
                "Save blocked",
                "Close Factorio before saving changes to the active mod list.");
            return;
        }

        var selectedNames = _allEditableMods
            .Where(mod => mod.IsSelected)
            .Select(mod => mod.Name)
            .ToList();
        var selectedVersions = _allEditableMods
            .Where(mod => mod.IsSelected && !string.IsNullOrWhiteSpace(mod.SelectedVersion))
            .ToDictionary(mod => mod.Name, mod => mod.SelectedVersion, StringComparer.OrdinalIgnoreCase);

        if (_isCreating)
        {
            await SaveNewListAsync(selectedNames, selectedVersions);
            return;
        }

        if (SelectedModList is null)
        {
            await _dialogService.ShowErrorAsync("Nothing selected", "Select a mod list before saving.");
            return;
        }

        var newName = DraftName.Trim();
        var validation = _nameValidator.Validate(newName, ModsFolderPath!, _allModListItems.Select(list => list.Name), SelectedModList.Name);
        if (!validation.IsValid)
        {
            await _dialogService.ShowErrorAsync("Invalid name", validation.Message);
            return;
        }

        var wasActive = SelectedModList.IsActive;

        try
        {
            var targetFolder = SelectedModList.FolderPath;
            if (!string.Equals(newName, SelectedModList.Name, StringComparison.OrdinalIgnoreCase))
            {
                targetFolder = _modListFileManager.RenameManagedList(ModsFolderPath!, SelectedModList.FolderPath, newName);
                await RenameSavedModListOrderEntryAsync(SelectedModList.Name, newName);
            }

            _modListWriter.Write(targetFolder, selectedNames);
            if (_importRootSettingsOnSave)
            {
                _modSettingsManager.CopyRootSettingsToModList(ModsFolderPath!, targetFolder);
            }

            _metadataService.Save(
                targetFolder,
                DraftDescription,
                selectedVersions,
                SelectedModList.ModList.CreatedUtc,
                SelectedModList.ModList.LastActivatedUtc);

            var reactivated = false;
            string? reactivationWarning = null;
            if (wasActive)
            {
                (reactivated, reactivationWarning) = await TryReactivateAfterSaveAsync(targetFolder);
            }

            FinishEditing();
            StatusMessage = reactivated ? $"Saved and re-activated {newName}." : $"Saved {newName}.";
            await RefreshAsync();
            SelectedModList = _allModListItems.FirstOrDefault(item =>
                string.Equals(item.Name, newName, StringComparison.OrdinalIgnoreCase));
            if (reactivationWarning is not null)
            {
                ErrorMessage = reactivationWarning;
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            ErrorMessage = ex.Message;
            await _dialogService.ShowErrorAsync("Save failed", ex.Message);
        }
    }

    private async Task<(bool reactivated, string? warning)> TryReactivateAfterSaveAsync(string targetFolder)
    {
        var result = _modListActivator.Activate(ModsFolderPath!, targetFolder);
        if (!result.Success)
        {
            return (false, $"Saved, but re-activation failed: {result.ErrorMessage}");
        }

        string? warning = null;
        try
        {
            _metadataService.RecordActivation(targetFolder);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            warning = $"Re-activated, but metadata could not be updated: {ex.Message}";
        }

        try
        {
            await RememberActiveListAsync(targetFolder);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            warning = $"Re-activated, but the active mod list could not be remembered: {ex.Message}";
        }

        return (true, warning);
    }

    private async Task SaveNewListAsync(
        IReadOnlyList<string> selectedNames,
        IReadOnlyDictionary<string, string> selectedVersions)
    {
        var settingsSourcePath = GetNewListSettingsSourcePath();
        if (!File.Exists(settingsSourcePath))
        {
            await _dialogService.ShowErrorAsync(
                "Missing mod-settings.dat",
                _duplicateSourceFolderPath is null
                    ? "The root mods folder has no mod-settings.dat. Create or launch Factorio once so the file exists, then create the mod list."
                    : "The source mod list has no mod-settings.dat, so it cannot be duplicated.");
            return;
        }

        var createdName = DraftName.Trim();
        var validation = _nameValidator.Validate(createdName, ModsFolderPath!, _allModListItems.Select(list => list.Name));
        if (!validation.IsValid)
        {
            await _dialogService.ShowErrorAsync("Invalid name", validation.Message);
            return;
        }

        var createdFolder = default(string);
        try
        {
            createdFolder = _modListFileManager.CreateManagedListFolder(ModsFolderPath!, createdName);
            _modSettingsManager.CopySettingsToModList(settingsSourcePath, createdFolder);
            _modListWriter.Write(createdFolder, selectedNames);
            _metadataService.Save(createdFolder, DraftDescription, selectedVersions, null, null);
            await AddSavedModListOrderEntryAsync(createdName);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            if (!string.IsNullOrWhiteSpace(createdFolder) && Directory.Exists(createdFolder))
            {
                Directory.Delete(createdFolder, recursive: true);
            }

            ErrorMessage = ex.Message;
            await _dialogService.ShowErrorAsync("Create failed", ex.Message);
            return;
        }

        FinishEditing();
        StatusMessage = $"Created {createdName}.";
        await RefreshAsync();
        SelectedModList = _allModListItems.FirstOrDefault(item =>
            string.Equals(item.Name, createdName, StringComparison.OrdinalIgnoreCase));
    }

    private void CancelEdit()
    {
        FinishEditing();
        LoadSelectedMods();
        StatusMessage = "Edit cancelled.";
    }

    private void SelectActiveList()
    {
        var activeList = _allModListItems.FirstOrDefault(item => item.IsActive);
        if (activeList is null)
        {
            return;
        }

        if (IsEditMode)
        {
            FinishEditing();
        }

        ListSearchText = string.Empty;
        ActiveTab = "Lists";
        SelectedModList = activeList;
        LoadSelectedMods();
        StatusMessage = $"Opened active list {activeList.Name}.";
    }

    private void FinishEditing()
    {
        _isCreating = false;
        _duplicateSourceFolderPath = null;
        _importRootSettingsOnSave = false;
        IsEditMode = false;
        foreach (var mod in _allEditableMods)
        {
            mod.PropertyChanged -= EditableModChanged;
        }

        _allEditableMods.Clear();
        EditableMods.Clear();
        DraftName = string.Empty;
        DraftDescription = string.Empty;
        DraftSelectedCount = 0;
        DraftSizeLabel = "-";
    }

    private async Task ActivateSelectedAsync()
    {
        if (SelectedModList is not null)
        {
            await ActivateAsync(SelectedModList);
        }
    }

    private async Task DeleteSelectedAsync()
    {
        if (SelectedModList is not null)
        {
            await DeleteAsync(SelectedModList);
        }
    }

    private async Task ActivateAsync(ModListItemViewModel item)
    {
        if (!EnsureWorkspace())
        {
            return;
        }

        RefreshFactorioRunningState();
        if (IsFactorioRunning && !item.IsActive)
        {
            await _dialogService.ShowErrorAsync(
                "Activation blocked",
                "Close Factorio before activating a different mod list.");
            return;
        }

        var missingDeps = FindMissingDependencies(item);
        if (missingDeps.Count > 0)
        {
            var lines = missingDeps
                .GroupBy(x => x.Mod, StringComparer.OrdinalIgnoreCase)
                .Select(g => $"• {g.Key} requires: {string.Join(", ", g.Select(x => x.Dep))}");
            await _dialogService.ShowErrorAsync(
                "Missing dependencies",
                $"Cannot activate. Some required dependencies are not in this mod list:\n\n{string.Join("\n", lines)}");
            return;
        }

        var confirmed = await _dialogService.ConfirmAsync(
            "Activate mod list",
            $"Activate {item.Name}?",
            "Confirm");
        if (!confirmed)
        {
            return;
        }

        var result = _modListActivator.Activate(ModsFolderPath!, item.FolderPath);
        if (result.Success)
        {
            // Disable expansion mods that mods in this list declare incompatible.
            // If space-age is disabled, also disable elevated-rails (same DLC bundle).
            // Then overwrite root with the updated list json so both stay in sync.
            var incompatibleExpansions = FindExpansionIncompatibilities(item);
            var toDisable = incompatibleExpansions.ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (toDisable.Contains("space-age"))
                toDisable.Add("elevated-rails");

            if (toDisable.Count > 0)
            {
                try
                {
                    foreach (var expMod in toDisable)
                        _modListEntryWriter.SetModEnabled(item.FolderPath, expMod, false);

                    var listJson = Path.Combine(item.FolderPath, FactorioFileNames.ModListJson);
                    var rootJson = Path.Combine(ModsFolderPath!, FactorioFileNames.ModListJson);
                    File.Copy(listJson, rootJson, overwrite: true);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    ErrorMessage = $"Activated, but could not disable incompatible expansion mods: {ex.Message}";
                }
            }

            try
            {
                _metadataService.RecordActivation(item.FolderPath);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                ErrorMessage = $"Activated, but metadata could not be updated: {ex.Message}";
            }

            try
            {
                await RememberActiveListAsync(item.FolderPath);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                ErrorMessage = $"Activated, but the active mod list could not be remembered: {ex.Message}";
            }

            await RefreshAsync();
            StatusMessage = $"Activated {item.Name}.";
            var launchGame = await _dialogService.ConfirmAsync(
                "Activation complete",
                $"Activated {item.Name}.\nBackup created at:\n{result.BackupFolderPath}\n\nLaunch Factorio now?",
                "Launch");
            if (launchGame)
            {
                await LaunchFactorioAsync();
            }

            return;
        }

        ErrorMessage = result.ErrorMessage;
        await _dialogService.ShowErrorAsync("Activation failed", result.ErrorMessage ?? "Activation failed.");
    }

    private IReadOnlyList<(string Mod, string Dep)> FindMissingDependencies(ModListItemViewModel? item)
    {
        if (item is null)
            return [];

        // Include disabled zips so deps on them are flagged as missing (not silently skipped as built-ins)
        var availableByName = _availableMods.Concat(_disabledMods)
            .GroupBy(m => m.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        var selectedNames = item.ModList.SelectedMods.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missing = new List<(string, string)>();

        foreach (var modName in item.ModList.SelectedMods)
        {
            if (!availableByName.TryGetValue(modName, out var modInfo))
                continue;

            foreach (var raw in modInfo.Dependencies)
            {
                var s = raw.TrimStart();
                if (s.StartsWith('?') || s.StartsWith('!') || s.StartsWith('~') || s.StartsWith("(?)"))
                    continue;

                var depName = s.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries)[0];
                if (!availableByName.ContainsKey(depName))
                    continue; // built-in (e.g. "base") — always present

                if (!selectedNames.Contains(depName))
                    missing.Add((modName, depName));
            }
        }

        return missing;
    }

    private IReadOnlyList<string> FindExpansionIncompatibilities(ModListItemViewModel item)
    {
        var availableByName = _availableMods.Concat(_disabledMods)
            .GroupBy(m => m.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var modName in item.ModList.SelectedMods)
        {
            if (!availableByName.TryGetValue(modName, out var modInfo))
                continue;

            foreach (var raw in modInfo.Dependencies)
            {
                var (depType, depName) = ParseDependencyString(raw);
                if (depType == PortalDepType.Incompatible && IsExpansionMod(depName))
                    result.Add(depName);
            }
        }

        return result.ToList();
    }

    private async Task RenameAsync(ModListItemViewModel item)
    {
        if (!EnsureWorkspace())
        {
            return;
        }

        var newName = await _dialogService.PromptAsync("Rename mod list", $"Rename {item.Name}.", item.Name);
        if (string.IsNullOrWhiteSpace(newName) ||
            string.Equals(newName, item.Name, StringComparison.Ordinal))
        {
            return;
        }

        var validation = _nameValidator.Validate(newName, ModsFolderPath!, _allModListItems.Select(list => list.Name), item.Name);
        if (!validation.IsValid)
        {
            await _dialogService.ShowErrorAsync("Invalid name", validation.Message);
            return;
        }

        try
        {
            _modListFileManager.RenameManagedList(ModsFolderPath!, item.FolderPath, newName.Trim());
            await RenameSavedModListOrderEntryAsync(item.Name, newName.Trim());
            StatusMessage = $"Renamed {item.Name} to {newName.Trim()}.";
            await RefreshAsync();
            SelectedModList = _allModListItems.FirstOrDefault(list =>
                string.Equals(list.Name, newName.Trim(), StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            ErrorMessage = ex.Message;
            await _dialogService.ShowErrorAsync("Rename failed", ex.Message);
        }
    }

    private async Task ImportCurrentToDraftAsync()
    {
        if (!EnsureWorkspace() || !IsEditMode)
        {
            return;
        }

        var confirmed = await _dialogService.ConfirmAsync(
            "Import current game mods",
            "Replace the draft selection with the current root mod-list.json? Save to write these imported mods to the list.",
            "Import");
        if (!confirmed)
        {
            return;
        }

        try
        {
            var selectedMods = _modListReader
                .ReadSelectedMods(ModsFolderPath!)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var mod in _allEditableMods)
            {
                mod.IsSelected = selectedMods.Contains(mod.Name);
            }

            if (_isCreating)
            {
                _duplicateSourceFolderPath = null;
            }

            _importRootSettingsOnSave = true;

            SearchText = string.Empty;
            ApplyEditorFilter();
            UpdateDraftSummary();
            StatusMessage = $"Imported {DraftSelectedCount} current game mods into the draft.";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            ErrorMessage = ex.Message;
            await _dialogService.ShowErrorAsync("Import failed", ex.Message);
        }
    }

    private async Task ImportFromListAsync()
    {
        if (SelectedModList is null || ImportSourceList is null || !EnsureWorkspace()) return;

        RefreshFactorioRunningState();
        if (IsFactorioRunning)
        {
            await _dialogService.ShowErrorAsync("Blocked", "Close Factorio before modifying a mod list.");
            return;
        }

        var sourceMods = ImportSourceList.ModList.SelectedMods
            .Where(m => !string.Equals(m, "base", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var targetAll = SelectedModList.ModList.SelectedMods
            .Concat(SelectedModList.ModList.DisabledMods)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var newMods = sourceMods.Where(m => !targetAll.Contains(m)).ToList();

        if (newMods.Count == 0)
        {
            StatusMessage = $"All mods from {ImportSourceList.Name} are already in {SelectedModList.Name}.";
            return;
        }

        try
        {
            _modListEntryWriter.MergeFrom(SelectedModList.FolderPath, newMods);

            if (PathsEqual(_settings.ActiveModListFolderPath, SelectedModList.FolderPath))
            {
                File.Copy(
                    Path.Combine(SelectedModList.FolderPath, FactorioFileNames.ModListJson),
                    Path.Combine(ModsFolderPath!, FactorioFileNames.ModListJson),
                    overwrite: true);
            }

            StatusMessage = $"Added {newMods.Count} mod(s) from {ImportSourceList.Name} to {SelectedModList.Name}.";
            await RefreshAsync();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ErrorMessage = $"Import failed: {ex.Message}";
        }
    }

    private async Task DeleteAsync(ModListItemViewModel item)
    {
        if (!EnsureWorkspace())
        {
            return;
        }

        var confirmed = await _dialogService.ConfirmAsync(
            "Delete mod list",
            $"Delete {item.Name}? This only removes the recognized managed mod-list folder.",
            "Delete");
        if (!confirmed)
        {
            return;
        }

        try
        {
            _modListFileManager.DeleteManagedList(ModsFolderPath!, item.FolderPath);
            await RemoveSavedModListOrderEntryAsync(item.Name);
            StatusMessage = $"Deleted {item.Name}.";
            await RefreshAsync();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            ErrorMessage = ex.Message;
            await _dialogService.ShowErrorAsync("Delete failed", ex.Message);
        }
    }

    private void LoadSelectedMods()
    {
        SelectedMods.Clear();

        if (SelectedModList is null)
            return;

        var allModsByName = BuildAllModLookup();
        var allListEntries = SelectedModList.ModList.SelectedMods.Select(n => (Name: n, IsDisabledInList: false))
            .Concat(SelectedModList.ModList.DisabledMods.Select(n => (Name: n, IsDisabledInList: true)))
            .OrderBy(e => e.Name, StringComparer.CurrentCultureIgnoreCase);

        foreach (var (entryName, isDisabledInList) in allListEntries)
        {
            DisplayModViewModel vm;
            if (allModsByName.TryGetValue(entryName, out var modInfo))
            {
                SelectedModList.ModList.SelectedVersions.TryGetValue(entryName, out var selectedVersion);
                vm = new DisplayModViewModel
                {
                    Name = modInfo.Name,
                    Title = modInfo.DisplayTitle,
                    Version = string.IsNullOrWhiteSpace(selectedVersion) ? modInfo.DisplayVersion : selectedVersion,
                    Author = modInfo.DisplayAuthor,
                    Description = modInfo.Description,
                    Size = modInfo.DisplaySize,
                    HasMultipleVersions = modInfo.AvailableVersions.Count > 1,
                    NewestVersion = modInfo.AvailableVersions.FirstOrDefault(),
                    Dependencies = modInfo.Dependencies,
                    IsDisabledInList = isDisabledInList
                };
            }
            else
            {
                vm = new DisplayModViewModel
                {
                    Name = entryName,
                    Title = entryName,
                    Version = SelectedModList.ModList.SelectedVersions.TryGetValue(entryName, out var selectedVersion) ? selectedVersion : null,
                    IsMissing = true,
                    IsDisabledInList = isDisabledInList
                };
            }

            if (!string.Equals(entryName, "base", StringComparison.OrdinalIgnoreCase))
            {
                var capturedName = entryName;
                vm.OnRemove = () => RemoveModFromListAsync(capturedName);
                vm.OnToggleListDisabled = () => ToggleListModDisabledAsync(capturedName, isDisabledInList);
            }

            SelectedMods.Add(vm);
        }
    }

    private Dictionary<string, ModInfo> BuildAvailableModLookup()
    {
        return _availableMods
            .GroupBy(mod => mod.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderBy(mod => mod.HasMetadataWarning)
                    .ThenByDescending(mod => Version.TryParse(mod.Version, out var parsed) ? parsed : new Version(0, 0))
                    .First(),
                StringComparer.OrdinalIgnoreCase);
    }

    private Dictionary<string, ModInfo> BuildAllModLookup()
    {
        return _availableMods
            .Concat(_disabledMods)
            .GroupBy(mod => mod.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderBy(mod => mod.HasMetadataWarning)
                    .ThenByDescending(mod => Version.TryParse(mod.Version, out var parsed) ? parsed : new Version(0, 0))
                    .First(),
                StringComparer.OrdinalIgnoreCase);
    }

    private void ApplyEditorFilter()
    {
        EditableMods.Clear();
        var filter = SearchText?.Trim();
        var visible = string.IsNullOrWhiteSpace(filter)
            ? _allEditableMods
            : _allEditableMods.Where(mod =>
                mod.Title.Contains(filter, StringComparison.CurrentCultureIgnoreCase) ||
                mod.Name.Contains(filter, StringComparison.OrdinalIgnoreCase));

        var ordered = IsEditorSortedByActive
            ? visible
                .OrderByDescending(mod => mod.IsSelected)
                .ThenBy(mod => mod.Name, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(mod => mod.Title, StringComparer.CurrentCultureIgnoreCase)
            : visible
                .OrderBy(mod => mod.Name, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(mod => mod.Title, StringComparer.CurrentCultureIgnoreCase);

        foreach (var mod in ordered)
        {
            EditableMods.Add(mod);
        }
    }

    private void ApplyListFilter()
    {
        ModLists.Clear();
        var filter = ListSearchText?.Trim();
        var visible = string.IsNullOrWhiteSpace(filter)
            ? _allModListItems
            : _allModListItems.Where(list =>
                list.Name.Contains(filter, StringComparison.CurrentCultureIgnoreCase) ||
                list.Description.Contains(filter, StringComparison.CurrentCultureIgnoreCase));

        foreach (var list in visible)
        {
            ModLists.Add(list);
        }

        OnPropertyChanged(nameof(VisibleModListCount));
    }

    private void LoadInstalledMods()
    {
        InstalledMods.Clear();
        var all = _availableMods
            .Concat(_disabledMods)
            .OrderBy(m => m.DisplayTitle, StringComparer.CurrentCultureIgnoreCase);
        foreach (var mod in all)
            InstalledMods.Add(new InstalledModViewModel(mod, RemoveInstalledModAsync, ToggleModDisabledAsync));
    }

    private async Task ToggleModDisabledAsync(InstalledModViewModel mod)
    {
        if (!EnsureWorkspace()) return;

        RefreshFactorioRunningState();
        if (IsFactorioRunning)
        {
            await _dialogService.ShowErrorAsync("Blocked", "Close Factorio before enabling or disabling mods.");
            return;
        }

        try
        {
            if (mod.IsDisabled)
            {
                foreach (var path in mod.SourceZipPaths)
                {
                    if (File.Exists(path) && path.EndsWith(".zip.disabled", StringComparison.OrdinalIgnoreCase))
                        File.Move(path, path[..^".disabled".Length]);
                }
                StatusMessage = $"Enabled {mod.Title}.";
            }
            else
            {
                foreach (var path in mod.SourceZipPaths)
                {
                    if (File.Exists(path) && path.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                        File.Move(path, path + ".disabled");
                }
                StatusMessage = $"Disabled {mod.Title}.";
            }
            await RefreshAsync();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ErrorMessage = ex.Message;
            await _dialogService.ShowErrorAsync("Toggle failed", ex.Message);
        }
    }

    private async Task ToggleListModDisabledAsync(string modName, bool currentlyDisabled)
    {
        if (!EnsureWorkspace() || SelectedModList is null) return;

        RefreshFactorioRunningState();
        if (IsFactorioRunning)
        {
            await _dialogService.ShowErrorAsync("Blocked", "Close Factorio before modifying a mod list.");
            return;
        }

        try
        {
            _modListEntryWriter.SetModEnabled(SelectedModList.FolderPath, modName, currentlyDisabled);
            StatusMessage = currentlyDisabled
                ? $"Enabled {modName} in {SelectedModList.Name}."
                : $"Disabled {modName} in {SelectedModList.Name}.";
            await RefreshAsync();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ErrorMessage = ex.Message;
            await _dialogService.ShowErrorAsync("Toggle failed", ex.Message);
        }
    }

    private async Task RemoveModFromListAsync(string modName)
    {
        if (!EnsureWorkspace() || SelectedModList is null) return;

        RefreshFactorioRunningState();
        if (IsFactorioRunning)
        {
            await _dialogService.ShowErrorAsync("Removal blocked", "Close Factorio before modifying a mod list.");
            return;
        }

        var confirmed = await _dialogService.ConfirmAsync(
            "Remove from list",
            $"Remove \"{modName}\" from \"{SelectedModList.Name}\"?",
            "Remove");
        if (!confirmed) return;

        try
        {
            _modListEntryWriter.RemoveMod(SelectedModList.FolderPath, modName);
            StatusMessage = $"Removed {modName} from {SelectedModList.Name}.";
            await RefreshAsync();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ErrorMessage = ex.Message;
            await _dialogService.ShowErrorAsync("Remove failed", ex.Message);
        }
    }

    private async Task RemoveInstalledModAsync(InstalledModViewModel mod)
    {
        if (!EnsureWorkspace()) return;

        RefreshFactorioRunningState();
        if (IsFactorioRunning)
        {
            await _dialogService.ShowErrorAsync("Deletion blocked", "Close Factorio before removing a mod.");
            return;
        }

        var fileCount = mod.SourceZipPaths.Count;
        var fileInfo = fileCount == 1 ? "1 file" : $"{fileCount} files (all versions)";
        var confirmed = await _dialogService.ConfirmAsync(
            "Remove installed mod",
            $"Delete {fileInfo} for \"{mod.Title}\"? This cannot be undone.",
            "Delete");
        if (!confirmed) return;

        try
        {
            foreach (var zipPath in mod.SourceZipPaths)
            {
                if (File.Exists(zipPath))
                    File.Delete(zipPath);
            }
            StatusMessage = $"Removed {mod.Title}.";
            await RefreshAsync();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ErrorMessage = ex.Message;
            await _dialogService.ShowErrorAsync("Remove failed", ex.Message);
        }
    }

    public bool CanLiveReorder => IsNormalMode && string.IsNullOrWhiteSpace(_listSearchText);

    public bool TryBeginDrag(ModListItemViewModel source)
    {
        return CanLiveReorder && _allModListItems.Contains(source);
    }

    public void UpdateDragPosition(ModListItemViewModel source, int newIndex)
    {
        if (!CanLiveReorder)
        {
            return;
        }

        var visibleIndex = ModLists.IndexOf(source);
        if (visibleIndex < 0)
        {
            return;
        }

        newIndex = Math.Clamp(newIndex, 0, ModLists.Count - 1);
        if (newIndex == visibleIndex)
        {
            return;
        }

        ModLists.Move(visibleIndex, newIndex);

        var allIndex = _allModListItems.IndexOf(source);
        if (allIndex < 0)
        {
            return;
        }

        _allModListItems.RemoveAt(allIndex);
        _allModListItems.Insert(Math.Clamp(newIndex, 0, _allModListItems.Count), source);
    }

    public async Task EndDragAsync(ModListItemViewModel source)
    {
        SelectedModList = source;
        StatusMessage = $"Moved {source.Name}.";
        await SaveCurrentModListOrderAsync();
    }

    public async Task MoveModListAsync(ModListItemViewModel source, ModListItemViewModel target, bool placeAfterTarget)
    {
        if (source == target || !IsNormalMode)
        {
            return;
        }

        var sourceIndex = _allModListItems.IndexOf(source);
        var targetIndex = _allModListItems.IndexOf(target);
        if (sourceIndex < 0 || targetIndex < 0)
        {
            return;
        }

        _allModListItems.RemoveAt(sourceIndex);
        targetIndex = _allModListItems.IndexOf(target);
        var insertIndex = placeAfterTarget ? targetIndex + 1 : targetIndex;
        insertIndex = Math.Clamp(insertIndex, 0, _allModListItems.Count);
        _allModListItems.Insert(insertIndex, source);

        ApplyListFilter();
        SelectedModList = source;
        await SaveCurrentModListOrderAsync();
        StatusMessage = $"Moved {source.Name}.";
    }

    private IReadOnlyList<ModList> ApplySavedModListOrder(IReadOnlyList<ModList> modLists)
    {
        var savedOrder = GetSavedModListOrder();
        if (savedOrder.Count == 0)
        {
            return modLists
                .OrderBy(list => list.Name, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }

        var positions = savedOrder
            .Select((name, index) => new { name, index })
            .GroupBy(item => item.name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().index, StringComparer.OrdinalIgnoreCase);

        return modLists
            .OrderBy(list => positions.TryGetValue(list.Name, out var position) ? position : int.MaxValue)
            .ThenBy(list => positions.ContainsKey(list.Name) ? string.Empty : list.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    private IReadOnlyList<string> GetSavedModListOrder()
    {
        var key = GetSettingsFolderKey(ModsFolderPath);
        if (key is null || !_settings.ModListOrders.TryGetValue(key, out var order))
        {
            return [];
        }

        return order
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private async Task SaveCurrentModListOrderAsync()
    {
        var key = GetSettingsFolderKey(ModsFolderPath);
        if (key is null)
        {
            return;
        }

        _settings.LastModsFolderPath = ModsFolderPath;
        _settings.ModListOrders[key] = _allModListItems
            .Select(item => item.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        await _appSettingsService.SaveAsync(_settings);
    }

    private async Task AddSavedModListOrderEntryAsync(string name)
    {
        var key = GetSettingsFolderKey(ModsFolderPath);
        if (key is null)
        {
            return;
        }

        var order = GetSavedModListOrder().ToList();
        if (order.Count == 0)
        {
            order.AddRange(_allModListItems.Select(item => item.Name));
        }

        order.RemoveAll(existing => string.Equals(existing, name, StringComparison.OrdinalIgnoreCase));
        order.Add(name);
        _settings.ModListOrders[key] = order;
        await _appSettingsService.SaveAsync(_settings);
    }

    private async Task RenameSavedModListOrderEntryAsync(string oldName, string newName)
    {
        var key = GetSettingsFolderKey(ModsFolderPath);
        if (key is null)
        {
            return;
        }

        var order = GetSavedModListOrder().ToList();
        var index = order.FindIndex(name => string.Equals(name, oldName, StringComparison.OrdinalIgnoreCase));
        order.RemoveAll(name => string.Equals(name, newName, StringComparison.OrdinalIgnoreCase));

        if (index >= 0)
        {
            order[index] = newName;
        }
        else
        {
            order.Add(newName);
        }

        _settings.ModListOrders[key] = order;
        await _appSettingsService.SaveAsync(_settings);
    }

    private async Task RemoveSavedModListOrderEntryAsync(string name)
    {
        var key = GetSettingsFolderKey(ModsFolderPath);
        if (key is null)
        {
            return;
        }

        var order = GetSavedModListOrder().ToList();
        order.RemoveAll(existing => string.Equals(existing, name, StringComparison.OrdinalIgnoreCase));
        _settings.ModListOrders[key] = order;
        await _appSettingsService.SaveAsync(_settings);
    }

    private void EditableModChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(EditableModViewModel.IsSelected) or nameof(EditableModViewModel.SelectedVersion))
        {
            UpdateDraftSummary();
            if (IsEditorSortedByActive && e.PropertyName == nameof(EditableModViewModel.IsSelected))
            {
                ApplyEditorFilter();
            }
        }
    }

    private void UpdateDraftSummary()
    {
        var selected = _allEditableMods.Where(mod => mod.IsSelected).Select(mod => mod.Name).ToList();
        DraftSelectedCount = selected.Count;
        DraftSizeLabel = CalculateSelectedSizeLabel(selected);
    }

    private string? GetRememberedActiveFolderPath(IReadOnlyList<ModList> detectedModLists)
    {
        var rememberedFolderPath = _settings.ActiveModListFolderPath;
        if (string.IsNullOrWhiteSpace(rememberedFolderPath))
            return null;

        return detectedModLists
            .FirstOrDefault(list => PathsEqual(list.FolderPath, rememberedFolderPath))
            ?.FolderPath;
    }

    private async Task RememberActiveListAsync(string modListFolderPath)
    {
        _settings.LastModsFolderPath = ModsFolderPath;
        _settings.ActiveModListFolderPath = modListFolderPath;
        await _appSettingsService.SaveAsync(_settings);
    }


    private string CalculateSelectedSizeLabel(IEnumerable<string> selectedModNames)
    {
        var availableByName = BuildAvailableModLookup();
        var bytes = selectedModNames
            .Where(name => availableByName.ContainsKey(name))
            .Sum(name => availableByName[name].TotalSizeBytes > 0 ? availableByName[name].TotalSizeBytes : availableByName[name].SizeBytes);
        return ModInfo.FormatBytes(bytes);
    }

    private string GenerateUniqueDraftName()
    {
        const string baseName = "New Mod List";
        var existing = _allModListItems.Select(list => list.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!existing.Contains(baseName))
        {
            return baseName;
        }

        var suffix = 2;
        while (existing.Contains($"{baseName} {suffix}"))
        {
            suffix++;
        }

        return $"{baseName} {suffix}";
    }

    private string GenerateUniqueDuplicateName(string sourceName)
    {
        var baseName = $"{sourceName} Copy";
        var existing = _allModListItems.Select(list => list.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!existing.Contains(baseName))
        {
            return baseName;
        }

        var suffix = 2;
        while (existing.Contains($"{baseName} {suffix}"))
        {
            suffix++;
        }

        return $"{baseName} {suffix}";
    }

    private string GetNewListSettingsSourcePath()
    {
        return _duplicateSourceFolderPath is null
            ? Path.Combine(ModsFolderPath!, FactorioFileNames.ModSettingsDat)
            : Path.Combine(_duplicateSourceFolderPath, FactorioFileNames.ModSettingsDat);
    }

    private void RaiseSelectionCommandStates()
    {
        OnPropertyChanged(nameof(CanActivateSelected));
        OnPropertyChanged(nameof(ActivateSelectedToolTip));
        OnPropertyChanged(nameof(CanEditSelected));
        OnPropertyChanged(nameof(EditSelectedToolTip));
        OnPropertyChanged(nameof(CanSaveEdit));
        EditSelectedCommand.RaiseCanExecuteChanged();
        DuplicateSelectedCommand.RaiseCanExecuteChanged();
        ImportCurrentToDraftCommand.RaiseCanExecuteChanged();
        ActivateSelectedCommand.RaiseCanExecuteChanged();
        DeleteSelectedCommand.RaiseCanExecuteChanged();
        ImportFromListCommand.RaiseCanExecuteChanged();
    }

    public void RefreshFactorioRunningState()
    {
        IsFactorioRunning = _factorioGameRunningDetector.IsRunning();
    }

    public async Task PollFactorioRunningStateAsync()
    {
        var wasRunning = IsFactorioRunning;
        RefreshFactorioRunningState();
        if (!wasRunning || IsFactorioRunning || _isHandlingFactorioClosed)
        {
            return;
        }

        _isHandlingFactorioClosed = true;
        try
        {
            await RefreshAsync();
        }
        finally
        {
            _isHandlingFactorioClosed = false;
        }
    }

    private Task PortalSearchAsync() => RunPortalAsync(1);

    private async Task RunPortalAsync(int page)
    {
        if (!HasPortalCredentials)
            return;

        _portalSearchCts?.Cancel();
        _portalSearchCts?.Dispose();
        _portalSearchCts = new CancellationTokenSource();
        var ct = _portalSearchCts.Token;

        IsPortalSearching = true;
        HasPortalError = false;
        PortalErrorMessage = null;
        PortalStatusMessage = "Loading…";
        PortalMods.Clear();
        PortalSearchCommand.RaiseCanExecuteChanged();
        PortalPrevPageCommand.RaiseCanExecuteChanged();
        PortalNextPageCommand.RaiseCanExecuteChanged();

        try
        {
            var categories = PortalCategories
                .Where(c => c.IsSelected)
                .Select(c => c.Name)
                .ToArray();

            var response = await _modPortalService.SearchAsync(
                _settings.PortalUsername!,
                _settings.PortalToken!,
                query: PortalSearchText.Trim(),
                factorioVersion: ReadFactorioVersion(),
                categories: categories.Length > 0 ? categories : null,
                page: page,
                pageSize: 20,
                cancellationToken: ct);

            if (ct.IsCancellationRequested)
                return;

            _portalDebugRawJson = response.RawJson;

            if (response.ErrorMessage is not null)
            {
                HasPortalError = true;
                PortalErrorMessage = response.ErrorMessage;
                PortalStatusMessage = "Request failed.";
                return;
            }

            var results = response.Result?.Results ?? [];
            foreach (var r in results)
                PortalMods.Add(new PortalModViewModel(r));

            PortalPage = response.Result?.Pagination?.Page ?? page;
            PortalPageCount = Math.Max(1, response.Result?.Pagination?.PageCount ?? 1);
            PortalTotalCount = response.Result?.Pagination?.Count ?? results.Count;

            _portalInitialLoadDone = true;

            PortalStatusMessage = PortalTotalCount == 0
                ? "No results found."
                : $"{PortalTotalCount} mods — {PortalPageLabel}";
        }
        catch (OperationCanceledException)
        {
            // Superseded by a newer request — discard silently
        }
        finally
        {
            if (!ct.IsCancellationRequested)
            {
                IsPortalSearching = false;
                PortalSearchCommand.RaiseCanExecuteChanged();
                PortalPrevPageCommand.RaiseCanExecuteChanged();
                PortalNextPageCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public async Task LoadPortalModDetailAsync(string modName)
    {
        PortalDetail = null;
        PortalDetailError = null;
        IsPortalDetailLoading = true;

        try
        {
            var (result, error) = await _modPortalService.GetModAsync(modName, ReadFactorioVersion(), CancellationToken.None);
            if (error is not null)
            {
                PortalDetailError = error;
                return;
            }
            PortalDetail = result is not null ? new PortalModDetailViewModel(result) : null;
        }
        finally
        {
            IsPortalDetailLoading = false;
        }
    }

    public void StartInstall(PortalModDetailViewModel detail)
    {
        if (detail.LatestRelease is null || string.IsNullOrWhiteSpace(detail.LatestRelease.DownloadUrl))
            return;

        InstallFlow = new InstallFlowViewModel(
            modName: detail.Name,
            modTitle: detail.Title,
            release: detail.LatestRelease,
            availableLists: _allModListItems.ToList(),
            onResolveDeps: ResolveDependenciesAsync,
            onConfirm: ExecuteInstallAsync,
            onClose: () => InstallFlow = null);
    }

    private async Task<DepsResolutionResult> ResolveDependenciesAsync(
        string mainModName,
        PortalModRelease mainRelease,
        CancellationToken ct)
    {
        var toInstall = new List<PortalModToInstall> { new(mainModName, mainRelease) };
        var expansionDeps = new List<string>();
        var alreadySatisfied = new List<string>();
        var incompatibilities = new List<string>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { mainModName };

        var installedNames = _availableMods.Concat(_disabledMods)
            .Select(m => m.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var queue = new Queue<(string Name, PortalModRelease Release)>();
        queue.Enqueue((mainModName, mainRelease));

        while (queue.Count > 0)
        {
            var (_, release) = queue.Dequeue();
            var deps = release.InfoJson?.Dependencies ?? [];

            foreach (var depStr in deps)
            {
                var (depType, depName) = ParseDependencyString(depStr);

                if (depType == PortalDepType.Incompatible)
                {
                    if (!incompatibilities.Contains(depName, StringComparer.OrdinalIgnoreCase))
                        incompatibilities.Add(depName);
                    continue;
                }

                if (depType != PortalDepType.Required)
                    continue;

                if (IsBuiltInMod(depName) || visited.Contains(depName))
                    continue;

                visited.Add(depName);

                if (IsExpansionMod(depName))
                {
                    CollectExpansionMod(depName, expansionDeps, visited);
                    continue;
                }

                if (installedNames.Contains(depName))
                {
                    alreadySatisfied.Add(depName);
                    continue;
                }

                var (modResult, error) = await _modPortalService.GetModAsync(depName, ReadFactorioVersion(), ct);
                if (error is not null)
                    return new DepsResolutionResult
                    {
                        ToInstall = toInstall,
                        ExpansionDeps = expansionDeps,
                        AlreadySatisfied = alreadySatisfied,
                        Incompatibilities = incompatibilities,
                        ErrorMessage = $"Could not fetch dependency '{depName}': {error}",
                    };

                var depRelease = modResult?.Releases?
                                     .OrderByDescending(r => Version.TryParse(r.Version, out var v) ? v : new Version())
                                     .FirstOrDefault()
                                 ?? modResult?.LatestRelease;
                if (depRelease is null)
                    return new DepsResolutionResult
                    {
                        ToInstall = toInstall,
                        ExpansionDeps = expansionDeps,
                        AlreadySatisfied = alreadySatisfied,
                        Incompatibilities = incompatibilities,
                        ErrorMessage = $"No release found for dependency '{depName}'.",
                    };

                toInstall.Add(new PortalModToInstall(depName, depRelease));
                queue.Enqueue((depName, depRelease));
            }
        }

        return new DepsResolutionResult
        {
            ToInstall = toInstall,
            ExpansionDeps = expansionDeps,
            AlreadySatisfied = alreadySatisfied,
            Incompatibilities = incompatibilities,
        };
    }

    private enum PortalDepType { Required, Optional, Incompatible, NoEffect, HiddenOptional }

    private static (PortalDepType Type, string ModName) ParseDependencyString(string raw)
    {
        var s = raw.Trim();
        PortalDepType type;
        if (s.StartsWith("(?)"))
        {
            type = PortalDepType.HiddenOptional;
            s = s[3..].TrimStart();
        }
        else if (s.StartsWith('!'))
        {
            type = PortalDepType.Incompatible;
            s = s[1..].TrimStart();
        }
        else if (s.StartsWith('?'))
        {
            type = PortalDepType.Optional;
            s = s[1..].TrimStart();
        }
        else if (s.StartsWith('~'))
        {
            type = PortalDepType.NoEffect;
            s = s[1..].TrimStart();
        }
        else
        {
            type = PortalDepType.Required;
        }

        var spaceIdx = s.IndexOf(' ');
        var modName = spaceIdx >= 0 ? s[..spaceIdx].Trim() : s.Trim();
        return (type, modName);
    }

    private static bool IsBuiltInMod(string name) =>
        name.Equals("base", StringComparison.OrdinalIgnoreCase);

    private static bool IsExpansionMod(string name) =>
        name.Equals("quality", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("space-age", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("elevated-rails", StringComparison.OrdinalIgnoreCase);

    // space-age depends on quality and elevated-rails; the others have no expansion sub-deps
    private static string[] GetExpansionSubDeps(string name) =>
        name.Equals("space-age", StringComparison.OrdinalIgnoreCase)
            ? ["quality", "elevated-rails"]
            : [];

    private static void CollectExpansionMod(string name, List<string> expansionDeps, HashSet<string> visited)
    {
        // depName is already in visited when called from the main loop, so we add directly here.
        // For sub-deps we guard with visited so duplicates are skipped.
        if (!expansionDeps.Contains(name, StringComparer.OrdinalIgnoreCase))
            expansionDeps.Add(name);

        foreach (var sub in GetExpansionSubDeps(name))
        {
            if (visited.Add(sub))
                CollectExpansionMod(sub, expansionDeps, visited);
        }
    }

    private async Task<string?> ExecuteInstallAsync(
        string? existingListFolderPath,
        string newListName,
        IReadOnlyList<PortalModToInstall> modsToInstall,
        IReadOnlyList<string> expansionDeps,
        IProgress<(string Label, double Value)> progress,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(ModsFolderPath))
            return "Mods folder path is not set.";
        if (string.IsNullOrWhiteSpace(_settings.PortalUsername) || string.IsNullOrWhiteSpace(_settings.PortalToken))
            return "Portal credentials are not set.";

        var targetListFolder = existingListFolderPath;

        if (targetListFolder is null)
        {
            var validation = _nameValidator.Validate(newListName, ModsFolderPath, _allModListItems.Select(l => l.Name));
            if (!validation.IsValid)
                return validation.Message;

            try
            {
                targetListFolder = _modListFileManager.CreateManagedListFolder(ModsFolderPath, newListName);
                var settingsSrc = Path.Combine(ModsFolderPath, FactorioFileNames.ModSettingsDat);
                if (File.Exists(settingsSrc))
                    File.Copy(settingsSrc, Path.Combine(targetListFolder, FactorioFileNames.ModSettingsDat));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return ex.Message;
            }
        }

        for (int i = 0; i < modsToInstall.Count; i++)
        {
            var mod = modsToInstall[i];
            if (string.IsNullOrWhiteSpace(mod.Release.DownloadUrl) || string.IsNullOrWhiteSpace(mod.Release.FileName))
                return $"{mod.ModName}: Release is missing download information.";

            var label = modsToInstall.Count == 1
                ? $"Downloading {mod.ModName}…"
                : $"Downloading {mod.ModName} ({i + 1}/{modsToInstall.Count})…";

            var baseProgress = (double)i / modsToInstall.Count;
            var modProgress = new Progress<double>(v =>
                progress.Report((label, baseProgress + v / modsToInstall.Count)));

            progress.Report((label, baseProgress));

            var error = await _modDownloadService.DownloadAsync(
                relativeUrl: mod.Release.DownloadUrl,
                fileName: mod.Release.FileName,
                destFolder: ModsFolderPath,
                username: _settings.PortalUsername,
                token: _settings.PortalToken,
                expectedSha1: mod.Release.Sha1,
                progress: modProgress,
                ct: ct);

            if (error is not null)
                return $"{mod.ModName}: {error}";

            try
            {
                _modListEntryWriter.AddMod(targetListFolder, mod.Release.FileName[..mod.Release.FileName.LastIndexOf('_')]);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return ex.Message;
            }
        }

        foreach (var expMod in expansionDeps)
        {
            try
            {
                _modListEntryWriter.AddMod(targetListFolder, expMod);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return ex.Message;
            }
        }

        // Keep root mod-list.json in sync if installing into the currently active list
        if (PathsEqual(_settings.ActiveModListFolderPath, targetListFolder))
        {
            try
            {
                File.Copy(
                    Path.Combine(targetListFolder, FactorioFileNames.ModListJson),
                    Path.Combine(ModsFolderPath, FactorioFileNames.ModListJson),
                    overwrite: true);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return ex.Message;
            }
        }

        _ = RefreshAsync();
        return null;
    }

    private async Task SavePortalCredentialsAsync()
    {
        await _appSettingsService.SaveAsync(_settings);
        PortalStatusMessage = "Credentials saved.";
        PortalSearchCommand.RaiseCanExecuteChanged();
    }

    private string? ReadFactorioVersion()
    {
        if (string.IsNullOrWhiteSpace(FactorioInstallFolderPath))
            return null;

        var infoPath = Path.Combine(FactorioInstallFolderPath, "data", "base", "info.json");
        if (!File.Exists(infoPath))
            return null;

        try
        {
            using var stream = File.OpenRead(infoPath);
            using var doc = JsonDocument.Parse(stream);
            if (doc.RootElement.TryGetProperty("version", out var v) && v.ValueKind == JsonValueKind.String)
                return v.GetString();
            return null;
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private bool EnsureWorkspace()
    {
        ValidateFolder();
        return IsFolderValid && !string.IsNullOrWhiteSpace(ModsFolderPath);
    }

    private static class EditorSortModes
    {
        public const string Name = "Name";
        public const string Active = "Active";
    }

    private static bool PathsEqual(string? left, string? right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
        {
            return false;
        }

        try
        {
            var normalizedLeft = Path.TrimEndingDirectorySeparator(Path.GetFullPath(left));
            var normalizedRight = Path.TrimEndingDirectorySeparator(Path.GetFullPath(right));
            return string.Equals(normalizedLeft, normalizedRight, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return false;
        }
    }

    private static string? GetSettingsFolderKey(string? folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            return null;
        }

        try
        {
            return Path.TrimEndingDirectorySeparator(Path.GetFullPath(folderPath));
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return folderPath.Trim();
        }
    }
}
