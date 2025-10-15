using Microsoft.JSInterop;
using GoogleDriveClone.SharedModels.DTOs;
using GoogleDriveClone.SharedModels.Results;
using System.Net.Http.Headers;
using System.Net.Http.Json;

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
            await _notificationService.ShowInfoAsync($"������������ {fileName}...");

            // �������� ���� ����� HttpClient, ���� ��� �� ����������� �����������
            var response = await _httpClient.GetAsync($"api/files/{fileId}/download");
            
            if (!response.IsSuccessStatusCode)
            {
                // ���������� ������� API
                try
                {
                    var errorResponse = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
                    var errorMessage = errorResponse?.Error?.Message ?? $"HTTP {response.StatusCode}";
                    await _notificationService.ShowErrorAsync($"�� ������� ����������� {fileName}: {errorMessage}");
                    return errorResponse?.Error ?? DomainErrors.General.UnexpectedError;
                }
                catch
                {
                    await _notificationService.ShowErrorAsync($"������� ������������ {fileName}: {response.StatusCode}");
                    return DomainErrors.General.UnexpectedError;
                }
            }

            // �������� ��� �����
            var fileBytes = await response.Content.ReadAsByteArrayAsync();
            
            // ��������� data URL �� ����������� ����� JavaScript
            var base64String = Convert.ToBase64String(fileBytes);
            
            // ��������� MIME type � Content-Type ��������� ��� �� ����������� �����
            var contentType = response.Content.Headers.ContentType?.MediaType ?? GetMimeType(fileName);
            var dataUrl = $"data:{contentType};base64,{base64String}";
            
            // ������������� JavaScript ��� ������������
            await _jsRuntime.InvokeVoidAsync("downloadFile", dataUrl, fileName);

            await _notificationService.ShowSuccessAsync($"������������ {fileName} ���������!");
            return Result.Success();
        }
        catch (JSException jsEx)
        {
            // JavaScript ������� - �������� � ���������
            await _notificationService.ShowErrorAsync($"������� ��������: {jsEx.Message}");
            return DomainErrors.General.UnexpectedError;
        }
        catch (HttpRequestException)
        {
            // ������� �������
            await _notificationService.ShowErrorAsync($"������� ����� ��� ����������� {fileName}");
            return DomainErrors.General.UnexpectedError;
        }
        catch (TaskCanceledException)
        {
            // ������� ��� ����������
            await _notificationService.ShowErrorAsync($"������������ {fileName} ���������");
            return DomainErrors.General.UnexpectedError;
        }
        catch (Exception ex)
        {
            // ����-��� ���� �������
            await _notificationService.ShowErrorAsync($"�� ������� ����������� {fileName}: {ex.Message}");
            return DomainErrors.General.UnexpectedError;
        }
    }

    private static string GetMimeType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".txt" => "text/plain",
            ".pdf" => "application/pdf",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".webp" => "image/webp",
            ".svg" => "image/svg+xml",
            ".zip" => "application/zip",
            ".rar" => "application/x-rar-compressed",
            ".7z" => "application/x-7z-compressed",
            ".json" => "application/json",
            ".xml" => "application/xml",
            ".css" => "text/css",
            ".js" => "application/javascript",
            ".html" => "text/html",
            ".md" => "text/markdown",
            ".csv" => "text/csv",
            ".py" => "text/x-python",
            ".cs" => "text/x-csharp",
            ".cpp" => "text/x-c++src",
            ".c" => "text/x-csrc",
            _ => "application/octet-stream"
        };
    }
}