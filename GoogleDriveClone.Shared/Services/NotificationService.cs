using Microsoft.JSInterop;

namespace GoogleDriveClone.Shared.Services;

public interface INotificationService
{
    Task ShowSuccessAsync(string message);
    Task ShowErrorAsync(string message);
    Task ShowInfoAsync(string message);
    Task ShowWarningAsync(string message);
}

public class NotificationService : INotificationService
{
    private readonly IJSRuntime _jsRuntime;

    public NotificationService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task ShowSuccessAsync(string message)
    {
        await _jsRuntime.InvokeVoidAsync("showNotification", message, "success");
    }

    public async Task ShowErrorAsync(string message)
    {
        await _jsRuntime.InvokeVoidAsync("showNotification", message, "error");
    }

    public async Task ShowInfoAsync(string message)
    {
        await _jsRuntime.InvokeVoidAsync("showNotification", message, "info");
    }

    public async Task ShowWarningAsync(string message)
    {
        await _jsRuntime.InvokeVoidAsync("showNotification", message, "warning");
    }
}