using Avalonia.Controls;
using Avalonia.Input;
using FactorioModManager.App.ViewModels;

namespace FactorioModManager.App.Views;

public sealed partial class MainWindow : Window
{
    private static readonly DataFormat<string> ModListDragFormat =
        DataFormat.CreateStringApplicationFormat("factorio-mod-list");
    private ModListItemViewModel? _modListDragSource;
    private Avalonia.Point? _modListDragStartPoint;
    private bool _isModListDragActive;

    public MainWindow()
    {
        InitializeComponent();
    }

    private void ModListItem_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control control ||
            control.DataContext is not ModListItemViewModel item ||
            !e.GetCurrentPoint(control).Properties.IsLeftButtonPressed)
        {
            return;
        }

        _modListDragSource = item;
        _modListDragStartPoint = e.GetPosition(control);
    }

    private async void ModListItem_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (_isModListDragActive ||
            _modListDragSource is null ||
            _modListDragStartPoint is null ||
            sender is not Control control)
        {
            return;
        }

        if (!e.GetCurrentPoint(control).Properties.IsLeftButtonPressed)
        {
            ClearModListDragState();
            return;
        }

        var currentPoint = e.GetPosition(control);
        if (Math.Abs(currentPoint.X - _modListDragStartPoint.Value.X) < 4 &&
            Math.Abs(currentPoint.Y - _modListDragStartPoint.Value.Y) < 4)
        {
            return;
        }

        _isModListDragActive = true;
        var data = new DataTransfer();
        data.Add(DataTransferItem.Create(ModListDragFormat, _modListDragSource.Name));

        try
        {
            await DragDrop.DoDragDropAsync(e, data, DragDropEffects.Move);
        }
        finally
        {
            ClearModListDragState();
        }
    }

    private void ModListItem_DragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = CanDropModList(sender, e) ? DragDropEffects.Move : DragDropEffects.None;
        e.Handled = true;
    }

    private async void ModListItem_Drop(object? sender, DragEventArgs e)
    {
        if (!CanDropModList(sender, e) ||
            sender is not Control targetControl ||
            targetControl.DataContext is not ModListItemViewModel target ||
            _modListDragSource is null ||
            DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        var placeAfterTarget = e.GetPosition(targetControl).Y > targetControl.Bounds.Height / 2;
        await viewModel.MoveModListAsync(_modListDragSource, target, placeAfterTarget);
        e.Handled = true;
    }

    private bool CanDropModList(object? sender, DragEventArgs e)
    {
        return sender is Control control &&
            control.DataContext is ModListItemViewModel target &&
            _modListDragSource is not null &&
            _modListDragSource != target &&
            e.DataTransfer.Contains(ModListDragFormat);
    }

    private void ClearModListDragState()
    {
        _modListDragSource = null;
        _modListDragStartPoint = null;
        _isModListDragActive = false;
    }
}
