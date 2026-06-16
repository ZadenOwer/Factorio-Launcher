namespace FactorioModManager.App.ViewModels;

public sealed class PortalCategoryViewModel : ViewModelBase
{
    private bool _isSelected;

    public PortalCategoryViewModel(string name, string label)
    {
        Name = name;
        Label = label;
    }

    public string Name { get; }
    public string Label { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}
