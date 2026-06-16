using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;
using FactorioModManager.App.ViewModels;

namespace FactorioModManager.App.Views;

public sealed partial class MainWindow : Window
{
    private const double DragStartDistance = 4;

    private ModListItemViewModel? _modListDragSource;
    private Avalonia.Point? _modListDragStartPoint;
    private Avalonia.Point _modListDragGhostOffset;
    private bool _isModListDragActive;

    public MainWindow()
    {
        InitializeComponent();
        ModListsBox.AddHandler(
            InputElement.PointerMovedEvent,
            ModListsBox_PointerMoved,
            RoutingStrategies.Tunnel | RoutingStrategies.Bubble,
            handledEventsToo: true);
        ModListsBox.AddHandler(
            InputElement.PointerReleasedEvent,
            ModListsBox_PointerReleased,
            RoutingStrategies.Tunnel | RoutingStrategies.Bubble,
            handledEventsToo: true);
        ModListsBox.AddHandler(
            InputElement.PointerCaptureLostEvent,
            ModListsBox_PointerCaptureLost,
            RoutingStrategies.Bubble,
            handledEventsToo: true);
    }

    private void InstalledModRow_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control control &&
            control.DataContext is InstalledModViewModel vm &&
            e.GetCurrentPoint(control).Properties.IsLeftButtonPressed)
        {
            vm.IsExpanded = !vm.IsExpanded;
        }
    }

    private void PortalSearchBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is MainWindowViewModel vm)
            _ = vm.PortalSearchCommand.ExecuteAsync();
    }

    private void PortalModRow_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(sender as Control).Properties.IsLeftButtonPressed &&
            (sender as Control)?.DataContext is PortalModViewModel mod &&
            DataContext is MainWindowViewModel vm)
        {
            PortalDetailScrollViewer.Offset = Vector.Zero;
            _ = vm.LoadPortalModDetailAsync(mod.Name);
        }
    }


    private void SelectedModRow_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control control &&
            control.DataContext is DisplayModViewModel vm &&
            e.GetCurrentPoint(control).Properties.IsLeftButtonPressed)
        {
            vm.IsExpanded = !vm.IsExpanded;
        }
    }

    private void EditableModRow_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control control &&
            control.DataContext is EditableModViewModel vm &&
            e.GetCurrentPoint(control).Properties.IsLeftButtonPressed &&
            e.Source is Visual source &&
            !source.GetSelfAndVisualAncestors().OfType<ToggleButton>().Any() &&
            !source.GetSelfAndVisualAncestors().OfType<ComboBox>().Any())
        {
            vm.IsExpanded = !vm.IsExpanded;
        }
    }

    private void ModListItem_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control control ||
            control.DataContext is not ModListItemViewModel item ||
            DataContext is not MainWindowViewModel viewModel ||
            !e.GetCurrentPoint(control).Properties.IsLeftButtonPressed ||
            !viewModel.CanLiveReorder)
        {
            return;
        }

        _modListDragSource = item;
        _modListDragStartPoint = e.GetPosition(ModListsBox);
        _modListDragGhostOffset = e.GetPosition(control);
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
            _ = EndModListDragAsync(e.Pointer, commit: false);
            return;
        }

        var listPoint = e.GetPosition(ModListsBox);
        if (!_isModListDragActive)
        {
            if (Math.Abs(listPoint.X - _modListDragStartPoint.Value.X) < DragStartDistance &&
                Math.Abs(listPoint.Y - _modListDragStartPoint.Value.Y) < DragStartDistance)
            {
                return;
            }

            if (!BeginModListDragVisual(e.Pointer))
            {
                return;
            }
        }

        UpdateModListDragGhost(listPoint);
        UpdateModListDragTarget(listPoint);
        e.Handled = true;
    }

    private bool BeginModListDragVisual(IPointer pointer)
    {
        if (_modListDragSource is null || DataContext is not MainWindowViewModel viewModel)
        {
            return false;
        }

        if (!viewModel.TryBeginDrag(_modListDragSource))
        {
            return false;
        }

        var sourceContainer = ModListsBox.ContainerFromItem(_modListDragSource);
        if (sourceContainer is null ||
            sourceContainer.Bounds.Width <= 0 ||
            sourceContainer.Bounds.Height <= 0)
        {
            return false;
        }

        var scaling = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1.0;
        var pixelSize = new PixelSize(
            Math.Max(1, (int)Math.Ceiling(sourceContainer.Bounds.Width * scaling)),
            Math.Max(1, (int)Math.Ceiling(sourceContainer.Bounds.Height * scaling)));
        var bitmap = new RenderTargetBitmap(pixelSize, new Vector(96 * scaling, 96 * scaling));
        bitmap.Render(sourceContainer);

        ModListDragGhostImage.Source = bitmap;
        ModListDragGhost.Width = sourceContainer.Bounds.Width;
        ModListDragGhost.Height = sourceContainer.Bounds.Height;
        ModListDragGhost.IsVisible = true;

        _modListDragSource.IsBeingDragged = true;
        _isModListDragActive = true;
        pointer.Capture(ModListsBox);
        return true;
    }

    private void UpdateModListDragGhost(Avalonia.Point listPoint)
    {
        Canvas.SetLeft(ModListDragGhost, listPoint.X - _modListDragGhostOffset.X);
        Canvas.SetTop(ModListDragGhost, listPoint.Y - _modListDragGhostOffset.Y);
    }

    private void UpdateModListDragTarget(Avalonia.Point listPoint)
    {
        if (DataContext is not MainWindowViewModel viewModel || _modListDragSource is null)
        {
            return;
        }

        var newIndex = FindModListInsertionIndex(listPoint.Y, viewModel);
        if (newIndex >= 0)
        {
            viewModel.UpdateDragPosition(_modListDragSource, newIndex);
        }
    }

    private int FindModListInsertionIndex(double pointerY, MainWindowViewModel viewModel)
    {
        for (var i = 0; i < viewModel.ModLists.Count; i++)
        {
            var container = ModListsBox.ContainerFromIndex(i);
            if (container is null)
            {
                continue;
            }

            var topLeft = container.TranslatePoint(new Avalonia.Point(0, 0), ModListsBox);
            if (topLeft is null)
            {
                continue;
            }

            var midY = topLeft.Value.Y + container.Bounds.Height / 2;
            if (pointerY < midY)
            {
                return i;
            }
        }

        return Math.Max(0, viewModel.ModLists.Count - 1);
    }

    private async void ModListsBox_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        var wasActive = _isModListDragActive;
        await EndModListDragAsync(e.Pointer, commit: true);
        if (wasActive)
        {
            e.Handled = true;
        }
    }

    private async void ModListsBox_PointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        await EndModListDragAsync(null, commit: true);
    }

    private async Task EndModListDragAsync(IPointer? pointer, bool commit)
    {
        var source = _modListDragSource;
        if (source is null)
        {
            return;
        }

        var wasActive = _isModListDragActive;
        _modListDragSource = null;
        _modListDragStartPoint = null;
        _isModListDragActive = false;

        pointer?.Capture(null);

        if (!wasActive)
        {
            return;
        }

        ModListDragGhost.IsVisible = false;
        ModListDragGhostImage.Source = null;
        source.IsBeingDragged = false;

        if (commit && DataContext is MainWindowViewModel viewModel)
        {
            await viewModel.EndDragAsync(source);
        }
    }
}
