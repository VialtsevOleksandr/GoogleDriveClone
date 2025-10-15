using Microsoft.AspNetCore.Components.Forms;
using GoogleDriveClone.SharedModels.DTOs;
using GoogleDriveClone.SharedModels.Results;
using GoogleDriveClone.Shared.Utils;
using System.Net.Http.Json;

namespace GoogleDriveClone.Shared.Services;

public interface IFileUploadService
{
    Task<Result<List<FileResponseDto>>> UploadFilesAsync(IReadOnlyList<IBrowserFile> files);
    Task<Result<FileResponseDto>> UploadFileAsync(IBrowserFile file);
}

public class FileUploadService : IFileUploadService
{
    private readonly HttpClient _httpClient;
    private readonly INotificationService _notificationService;
    
    // Максимальний розмір файлу для завантаження (тимчасове обмеження браузера)
    private const long MaxBrowserFileSize = 50 * 1024 * 1024;

    public FileUploadService(HttpClient httpClient, INotificationService notificationService)
    {
        _httpClient = httpClient;
        _notificationService = notificationService;
    }

    public async Task<Result<List<FileResponseDto>>> UploadFilesAsync(IReadOnlyList<IBrowserFile> files)
    {
        var uploadedFiles = new List<FileResponseDto>();
        var errors = new List<string>();
        
        // Показуємо одне повідомлення на початку
        if (files.Count > 1)
        {
            await _notificationService.ShowInfoAsync($"Завантаження {files.Count} файлів...");
        }
        
        foreach (var file in files)
        {
            var result = await UploadFileAsync(file, showNotifications: files.Count == 1);
            if (result.IsSuccess)
            {
                uploadedFiles.Add(result.Value!);
            }
            else
            {
                errors.Add($"{file.Name}: {result.Error?.Message}");
            }
        }
        
        // Показуємо підсумкове повідомлення
        if (files.Count > 1)
        {
            if (errors.Any())
            {
                if (uploadedFiles.Any())
                {
                    await _notificationService.ShowWarningAsync($"Завантажено {uploadedFiles.Count} з {files.Count} файлів. Помилки: {string.Join(", ", errors)}");
                }
                else
                {
                    await _notificationService.ShowErrorAsync($"Не вдалося завантажити файли. Помилки: {string.Join(", ", errors)}");
                }
            }
            else
            {
                await _notificationService.ShowSuccessAsync($"Всі {files.Count} файлів успішно завантажено!");
            }
        }
        
        if (errors.Any() && uploadedFiles.Count == 0)
        {
            return DomainErrors.File.UploadFailed;
        }
        
        return uploadedFiles;
    }

    public async Task<Result<FileResponseDto>> UploadFileAsync(IBrowserFile file)
    {
        return await UploadFileAsync(file, showNotifications: true);
    }

    private async Task<Result<FileResponseDto>> UploadFileAsync(IBrowserFile file, bool showNotifications)
    {
        try
        {
            if (showNotifications)
            {
                await _notificationService.ShowInfoAsync($"Завантаження {file.Name}...");
            }

            // Створюємо MultipartFormDataContent для завантаження файлу
            using var content = new MultipartFormDataContent();
            var fileContent = new StreamContent(file.OpenReadStream(MaxBrowserFileSize));
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
            
            content.Add(fileContent, "file", file.Name);

            // Відправляємо файл на API (валідація буде на сервері)
            var response = await _httpClient.PostAsync("api/files/upload", content);
            
            if (response.IsSuccessStatusCode)
            {
                // Читаємо відповідь як ApiResponse<FileResponseDto>
                var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<FileResponseDto>>();
                
                if (apiResponse != null && apiResponse.Success && apiResponse.Data != null)
                {
                    if (showNotifications)
                    {
                        await _notificationService.ShowSuccessAsync($"Файл {file.Name} успішно завантажено!");
                    }
                    return apiResponse.Data;
                }
                else
                {
                    if (showNotifications)
                    {
                        await _notificationService.ShowErrorAsync($"Помилка завантаження: {apiResponse?.Message ?? "Невідома помилка"}");
                    }
                    return DomainErrors.File.UploadFailed;
                }
            }
            else
            {
                // Обробляємо помилку API
                try
                {
                    var errorResponse = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
                    var errorMessage = errorResponse?.Error?.Message ?? $"Помилка сервера: {response.StatusCode}";
                    if (showNotifications)
                    {
                        await _notificationService.ShowErrorAsync($"Не вдалося завантажити {file.Name}: {errorMessage}");
                    }
                    return errorResponse?.Error ?? DomainErrors.General.UnexpectedError;
                }
                catch
                {
                    if (showNotifications)
                    {
                        await _notificationService.ShowErrorAsync($"Помилка завантаження {file.Name}: {response.StatusCode}");
                    }
                    return DomainErrors.File.UploadFailed;
                }
            }
        }
        catch (HttpRequestException)
        {
            if (showNotifications)
            {
                await _notificationService.ShowErrorAsync($"Помилка мережі при завантаженні {file.Name}");
            }
            return DomainErrors.General.UnexpectedError;
        }
        catch (TaskCanceledException)
        {
            if (showNotifications)
            {
                await _notificationService.ShowErrorAsync($"Завантаження {file.Name} скасовано");
            }
            return DomainErrors.General.UnexpectedError;
        }
        catch (Exception)
        {
            if (showNotifications)
            {
                await _notificationService.ShowErrorAsync($"Не вдалося завантажити {file.Name}");
            }
            return DomainErrors.General.UnexpectedError;
        }
    }
}