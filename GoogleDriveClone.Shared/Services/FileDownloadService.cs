using Microsoft.JSInterop;
using GoogleDriveClone.SharedModels.DTOs;
using GoogleDriveClone.SharedModels.Results;

namespace GoogleDriveClone.Shared.Services;

public interface IFileDownloadService
{
    Task<Result> DownloadFileAsync(string fileId, string fileName);
}

public class FileDownloadService : IFileDownloadService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly HttpClient _httpClient;
    private readonly INotificationService _notificationService;

    public FileDownloadService(
        IJSRuntime jsRuntime,
        HttpClient httpClient,
        INotificationService notificationService)
    {
        _jsRuntime = jsRuntime;
        _httpClient = httpClient;
        _notificationService = notificationService;
    }

    public async Task<Result> DownloadFileAsync(string fileId, string fileName)
    {
        try
        {
            await _notificationService.ShowInfoAsync($"Завантаження {fileName}...");

            // Прямо викликаємо download endpoint
            var downloadUrl = $"api/files/{fileId}/download";
            
            // Використовуємо JavaScript для завантаження файлу
            await _jsRuntime.InvokeVoidAsync("downloadFileFromUrl", downloadUrl, fileName);

            await _notificationService.ShowSuccessAsync($"Завантаження {fileName} розпочато!");
            return Result.Success();
        }
        catch (JSException jsEx)
        {
            // JavaScript помилки - проблеми з браузером
            await _notificationService.ShowErrorAsync($"Помилка браузера: {jsEx.Message}");
            return DomainErrors.General.UnexpectedError;
        }
        catch (HttpRequestException)
        {
            // Мережеві помилки
            await _notificationService.ShowErrorAsync($"Помилка мережі при завантаженні {fileName}");
            return DomainErrors.General.UnexpectedError;
        }
        catch (TaskCanceledException)
        {
            // Таймаут або скасування
            await _notificationService.ShowErrorAsync($"Завантаження {fileName} скасовано");
            return DomainErrors.General.UnexpectedError;
        }
        catch (Exception)
        {
            // Будь-яка інша помилка
            await _notificationService.ShowErrorAsync($"Не вдалося завантажити {fileName}");
            return DomainErrors.General.UnexpectedError;
        }
    }
}