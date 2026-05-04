using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Platform.Storage;
using Avalonia.Media;

namespace FactorioModManager.App.Services;

public sealed class DialogService : IDialogService
{
    private readonly Window _owner;

    public DialogService(Window owner)
    {
        _owner = owner;
    }

    public async Task<string?> PickFolderAsync(string title)
    {
        var folders = await _owner.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = title,
            AllowMultiple = false
        });

        return folders.FirstOrDefault()?.TryGetLocalPath();
    }

    public async Task<string?> PromptAsync(string title, string message, string? initialValue = null)
    {
        var input = new TextBox
        {
            Text = initialValue ?? string.Empty,
            MinWidth = 360
        };

        var window = CreateDialogWindow(title);
        var okButton = new Button
        {
            Content = "Save",
            MinWidth = 88,
            HorizontalContentAlignment = HorizontalAlignment.Center
        };
        var cancelButton = new Button
        {
            Content = "Cancel",
            MinWidth = 88,
            HorizontalContentAlignment = HorizontalAlignment.Center
        };

        okButton.Click += (_, _) => window.Close(input.Text);
        cancelButton.Click += (_, _) => window.Close(null);

        window.Content = CreateDialogContent(
            message,
            input,
            new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Spacing = 8,
                Children =
                {
                    cancelButton,
                    okButton
                }
            });

        return await window.ShowDialog<string?>(_owner);
    }

    public async Task<bool> ConfirmAsync(string title, string message, string confirmText)
    {
        var window = CreateDialogWindow(title);
        var cancelButton = new Button
        {
            Content = "Cancel",
            MinWidth = 88,
            HorizontalContentAlignment = HorizontalAlignment.Center
        };
        var confirmButton = new Button
        {
            Content = confirmText,
            MinWidth = 88,
            HorizontalContentAlignment = HorizontalAlignment.Center
        };

        cancelButton.Click += (_, _) => window.Close(false);
        confirmButton.Click += (_, _) => window.Close(true);

        window.Content = CreateDialogContent(
            message,
            null,
            new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Spacing = 8,
                Children =
                {
                    cancelButton,
                    confirmButton
                }
            });

        return await window.ShowDialog<bool>(_owner);
    }

    public Task ShowMessageAsync(string title, string message)
    {
        return ShowNoticeAsync(title, message, "OK");
    }

    public Task ShowErrorAsync(string title, string message)
    {
        return ShowNoticeAsync(title, message, "OK");
    }

    private async Task ShowNoticeAsync(string title, string message, string buttonText)
    {
        var window = CreateDialogWindow(title);
        var okButton = new Button
        {
            Content = buttonText,
            MinWidth = 88,
            HorizontalContentAlignment = HorizontalAlignment.Center
        };

        okButton.Click += (_, _) => window.Close();
        window.Content = CreateDialogContent(
            message,
            null,
            new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Children =
                {
                    okButton
                }
            });

        await window.ShowDialog(_owner);
    }

    private static Window CreateDialogWindow(string title)
    {
        return new Window
        {
            Title = title,
            Width = 460,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            Background = Brushes.White
        };
    }

    private static Control CreateDialogContent(string message, Control? body, Control actions)
    {
        var panel = new StackPanel
        {
            Margin = new Avalonia.Thickness(20),
            Spacing = 16
        };

        panel.Children.Add(new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap,
            FontSize = 14,
            Foreground = new SolidColorBrush(Color.Parse("#1f2933"))
        });

        if (body is not null)
        {
            panel.Children.Add(body);
        }

        panel.Children.Add(actions);
        return panel;
    }
}
