using FactorioModManager.App.Services;

namespace FactorioModManager.Tests;

internal sealed class TestDialogService : IDialogService
{
    public List<string> Errors { get; } = [];

    public Task<string?> PickFolderAsync(string title)
    {
        return Task.FromResult<string?>(null);
    }

    public Task<string?> PromptAsync(string title, string message, string? initialValue = null)
    {
        return Task.FromResult<string?>(initialValue);
    }

    public Task<bool> ConfirmAsync(string title, string message, string confirmText)
    {
        return Task.FromResult(true);
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
