namespace FactorioModManager.App.Services;

public interface IDialogService
{
    Task<string?> PickFolderAsync(string title);
    Task<string?> PromptAsync(string title, string message, string? initialValue = null);
    Task<bool> ConfirmAsync(string title, string message, string confirmText);
    Task ShowMessageAsync(string title, string message);
    Task ShowErrorAsync(string title, string message);
}
