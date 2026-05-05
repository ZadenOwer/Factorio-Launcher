using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using FactorioModManager.App.ViewModels;

namespace FactorioModManager.App.Views;

public sealed partial class MainWindow : Window
{
    private const string ModListRowTag = "ModListRow";
    private const double DragStartDistance = 4;

    private ModListItemViewModel? _modListDragSource;
    private Avalonia.Point? _modListDragStartPoint;
    private bool _isModListDragActive;

    public MainWindow()
    {
        InitializeComponent();
        ModListsBox.AddHandler(
            InputElement.PointerMovedEvent,
            ModListsBox_PointerMoved,
            RoutingStrategies.Bubble,
            handledEventsToo: true);
        ModListsBox.AddHandler(
            InputElement.PointerReleasedEvent,
            ModListsBox_PointerReleased,
            RoutingStrategies.Bubble,
            handledEventsToo: true);
        ModListsBox.AddHandler(
            InputElement.PointerCaptureLostEvent,
            ModListsBox_PointerCaptureLost,
            RoutingStrategies.Bubble,
            handledEventsToo: true);
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
        _modListDragStartPoint = e.GetPosition(ModListsBox);
        _isModListDragActive = false;
    }

    private void ModListsBox_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (_modListDragSource is null || _modListDragStartPoint is null)
        {
            return;
        }

        if (!e.GetCurrentPoint(ModListsBox).Properties.IsLeftButtonPressed)
        {
            ClearModListDragState(e.Pointer);
            return;
        }

        var currentPoint = e.GetPosition(ModListsBox);
        if (Math.Abs(currentPoint.X - _modListDragStartPoint.Value.X) < DragStartDistance &&
            Math.Abs(currentPoint.Y - _modListDragStartPoint.Value.Y) < DragStartDistance)
        {
            return;
        }

        _isModListDragActive = true;
        e.Pointer.Capture(ModListsBox);
        e.Handled = true;
    }

    private async void ModListsBox_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        try
        {
            if (!_isModListDragActive ||
                _modListDragSource is null ||
                DataContext is not MainWindowViewModel viewModel)
            {
                return;
            }

            var pointerPosition = e.GetPosition(this);
            var target = FindModListRowAt(pointerPosition, out var targetControl);
            if (target is null || target == _modListDragSource || targetControl is null)
            {
                return;
            }

            var targetPoint = e.GetPosition(targetControl);
            var placeAfterTarget = targetPoint.Y > targetControl.Bounds.Height / 2;
            await viewModel.MoveModListAsync(_modListDragSource, target, placeAfterTarget);
            e.Handled = true;
        }
        finally
        {
            ClearModListDragState(e.Pointer);
        }
    }

    private void ModListsBox_PointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        ClearModListDragState();
    }

    private ModListItemViewModel? FindModListRowAt(Avalonia.Point pointerPosition, out Control? targetControl)
    {
        targetControl = null;
        if (this.InputHitTest(pointerPosition) is not Avalonia.Visual hit)
        {
            return null;
        }

        foreach (var visual in hit.GetSelfAndVisualAncestors())
        {
            if (visual is Control { Tag: ModListRowTag, DataContext: ModListItemViewModel item } control)
            {
                targetControl = control;
                return item;
            }
        }

        return null;
    }

    private void ClearModListDragState(IPointer? pointer = null)
    {
        pointer?.Capture(null);
        _modListDragSource = null;
        _modListDragStartPoint = null;
        _isModListDragActive = false;
    }
}
