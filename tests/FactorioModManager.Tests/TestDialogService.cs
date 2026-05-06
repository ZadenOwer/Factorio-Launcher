using FactorioModManager.App.Services;

namespace FactorioModManager.Tests;

internal sealed class TestDialogService : IDialogService
{
    public sealed record ConfirmCall(string Title, string Message, string ConfirmText);

    public List<string> Errors { get; } = [];
    public Queue<string?> PickedFolders { get; } = [];
    public List<string> PickFolderTitles { get; } = [];
    public Queue<bool> ConfirmResponses { get; } = [];
    public List<ConfirmCall> ConfirmCalls { get; } = [];

    public Task<string?> PickFolderAsync(string title)
    {
        PickFolderTitles.Add(title);
        return Task.FromResult(PickedFolders.Count == 0 ? null : PickedFolders.Dequeue());
    }

    public Task<string?> PromptAsync(string title, string message, string? initialValue = null)
    {
        return Task.FromResult<string?>(initialValue);
    }

    public Task<bool> ConfirmAsync(string title, string message, string confirmText)
    {
        ConfirmCalls.Add(new ConfirmCall(title, message, confirmText));
        if (ConfirmResponses.Count > 0)
        {
            return Task.FromResult(ConfirmResponses.Dequeue());
        }

        return Task.FromResult(!string.Equals(confirmText, "Launch", StringComparison.OrdinalIgnoreCase));
    }

    public Task ShowMessageAsync(string title, string message)
    {
        return Task.CompletedTask;
    }

    public Task ShowErrorAsync(string title, string message)
    {
        Errors.Add($"{title}: {message}");
        return Task.CompletedTask;
    }
}
