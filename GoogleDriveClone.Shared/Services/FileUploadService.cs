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
    
    // ������������ ����� ����� ��� ������������ (��������� ��������� ��������)
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
        
        // �������� ���� ����������� �� �������
        if (files.Count > 1)
        {
            await _notificationService.ShowInfoAsync($"������������ {files.Count} �����...");
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
        
        // �������� ��������� �����������
        if (files.Count > 1)
        {
            if (errors.Any())
            {
                if (uploadedFiles.Any())
                {
                    await _notificationService.ShowWarningAsync($"����������� {uploadedFiles.Count} � {files.Count} �����. �������: {string.Join(", ", errors)}");
                }
                else
                {
                    await _notificationService.ShowErrorAsync($"�� ������� ����������� �����. �������: {string.Join(", ", errors)}");
                }
            }
            else
            {
                await _notificationService.ShowSuccessAsync($"�� {files.Count} ����� ������ �����������!");
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
                await _notificationService.ShowInfoAsync($"������������ {file.Name}...");
            }

            // ��������� MultipartFormDataContent ��� ������������ �����
            using var content = new MultipartFormDataContent();
            var fileContent = new StreamContent(file.OpenReadStream(MaxBrowserFileSize));
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
            
            content.Add(fileContent, "file", file.Name);

            // ³���������� ���� �� API (�������� ���� �� ������)
            var response = await _httpClient.PostAsync("api/files/upload", content);
            
            if (response.IsSuccessStatusCode)
            {
                // ������ ������� �� ApiResponse<FileResponseDto>
                var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<FileResponseDto>>();
                
                if (apiResponse != null && apiResponse.Success && apiResponse.Data != null)
                {
                    if (showNotifications)
                    {
                        await _notificationService.ShowSuccessAsync($"���� {file.Name} ������ �����������!");
                    }
                    return apiResponse.Data;
                }
                else
                {
                    if (showNotifications)
                    {
                        await _notificationService.ShowErrorAsync($"������� ������������: {apiResponse?.Message ?? "������� �������"}");
                    }
                    return DomainErrors.File.UploadFailed;
                }
            }
            else
            {
                // ���������� ������� API
                try
                {
                    var errorResponse = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
                    var errorMessage = errorResponse?.Error?.Message ?? $"������� �������: {response.StatusCode}";
                    if (showNotifications)
                    {
                        await _notificationService.ShowErrorAsync($"�� ������� ����������� {file.Name}: {errorMessage}");
                    }
                    return errorResponse?.Error ?? DomainErrors.General.UnexpectedError;
                }
                catch
                {
                    if (showNotifications)
                    {
                        await _notificationService.ShowErrorAsync($"������� ������������ {file.Name}: {response.StatusCode}");
                    }
                    return DomainErrors.File.UploadFailed;
                }
            }
        }
        catch (HttpRequestException)
        {
            if (showNotifications)
            {
                await _notificationService.ShowErrorAsync($"������� ����� ��� ����������� {file.Name}");
            }
            return DomainErrors.General.UnexpectedError;
        }
        catch (TaskCanceledException)
        {
            if (showNotifications)
            {
                await _notificationService.ShowErrorAsync($"������������ {file.Name} ���������");
            }
            return DomainErrors.General.UnexpectedError;
        }
        catch (Exception)
        {
            if (showNotifications)
            {
                await _notificationService.ShowErrorAsync($"�� ������� ����������� {file.Name}");
            }
            return DomainErrors.General.UnexpectedError;
        }
    }
}