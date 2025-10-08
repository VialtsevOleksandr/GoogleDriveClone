using Microsoft.AspNetCore.Components.Forms;
using GoogleDriveClone.SharedModels.DTOs;
using GoogleDriveClone.SharedModels.Results;
using System.Net.Http.Json;

namespace GoogleDriveClone.Shared.Services;

public interface IFileUploadService
{
    Task<Result<List<FileResponseDto>>> UploadFilesAsync(IReadOnlyList<IBrowserFile> files);
    Task<Result<FileResponseDto>> UploadFileAsync(IBrowserFile file);
    Result ValidateFile(IBrowserFile file);
    string FormatFileSize(long bytes);
}

public class FileUploadService : IFileUploadService
{
    private readonly HttpClient _httpClient;
    private readonly INotificationService _notificationService;
    
    // File constraints
    private const long MaxFileSize = 50 * 1024 * 1024; // 50MB
    private readonly string[] AllowedExtensions = { 
        ".jpg", ".jpeg", ".png", ".gif", ".bmp", 
        ".txt", ".md", ".pdf", ".doc", ".docx",
        ".py", ".c", ".cpp", ".cs", ".js", ".html", ".css",
        ".zip", ".rar", ".7z"
    };

    public FileUploadService(HttpClient httpClient, INotificationService notificationService)
    {
        _httpClient = httpClient;
        _notificationService = notificationService;
    }

    public async Task<Result<List<FileResponseDto>>> UploadFilesAsync(IReadOnlyList<IBrowserFile> files)
    {
        var uploadedFiles = new List<FileResponseDto>();
        var errors = new List<string>();
        
        foreach (var file in files)
        {
            var result = await UploadFileAsync(file);
            if (result.IsSuccess)
            {
                uploadedFiles.Add(result.Value!);
            }
            else
            {
                errors.Add($"{file.Name}: {result.Error?.Message}");
            }
        }
        
        if (errors.Any())
        {
            await _notificationService.ShowErrorAsync($"������� ������������: {string.Join(", ", errors)}");
            return DomainErrors.File.UploadFailed;
        }
        
        return uploadedFiles;
    }

    public async Task<Result<FileResponseDto>> UploadFileAsync(IBrowserFile file)
    {
        try
        {
            // �������� �����
            var validationResult = ValidateFile(file);
            if (!validationResult.IsSuccess)
            {
                return validationResult.Error!;
            }

            await _notificationService.ShowInfoAsync($"������������ {file.Name}...");

            // ��������� MultipartFormDataContent ��� ������������ �����
            using var content = new MultipartFormDataContent();
            var fileContent = new StreamContent(file.OpenReadStream(MaxFileSize));
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
            
            content.Add(fileContent, "file", file.Name);

            // ³���������� ���� �� API
            var response = await _httpClient.PostAsync("api/files/upload", content);
            
            if (response.IsSuccessStatusCode)
            {
                // ������ ������� �� ApiResponse<FileResponseDto>
                var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<FileResponseDto>>();
                
                if (apiResponse != null && apiResponse.Success && apiResponse.Data != null)
                {
                    await _notificationService.ShowSuccessAsync($"���� {file.Name} ������ �����������!");
                    return apiResponse.Data;
                }
                else
                {
                    await _notificationService.ShowErrorAsync($"������� ������������: {apiResponse?.Message ?? "������� �������"}");
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
                    await _notificationService.ShowErrorAsync($"�� ������� ����������� {file.Name}: {errorMessage}");
                    return errorResponse?.Error ?? DomainErrors.General.UnexpectedError;
                }
                catch
                {
                    await _notificationService.ShowErrorAsync($"������� ������������ {file.Name}: {response.StatusCode}");
                    return DomainErrors.File.UploadFailed;
                }
            }
        }
        catch (HttpRequestException)
        {
            await _notificationService.ShowErrorAsync($"������� ����� ��� ����������� {file.Name}");
            return DomainErrors.General.UnexpectedError;
        }
        catch (TaskCanceledException)
        {
            await _notificationService.ShowErrorAsync($"������������ {file.Name} ���������");
            return DomainErrors.General.UnexpectedError;
        }
        catch (Exception)
        {
            await _notificationService.ShowErrorAsync($"�� ������� ����������� {file.Name}");
            return DomainErrors.General.UnexpectedError;
        }
    }

    public Result ValidateFile(IBrowserFile file)
    {
        // �������� ������ �����
        if (file.Size > MaxFileSize)
        {
            _ = _notificationService.ShowErrorAsync($"���� {file.Name} ������� �������. ������������ �����: {FormatFileSize(MaxFileSize)}");
            return DomainErrors.File.InvalidFileSize;
        }

        // �������� ���������� �����
        var extension = Path.GetExtension(file.Name).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            _ = _notificationService.ShowErrorAsync($"��� ����� {extension} �� �����������");
            return DomainErrors.File.InvalidFileType;
        }

        return Result.Success();
    }

    public string FormatFileSize(long bytes)
    {
        string[] sizes = { "�", "��", "��", "��", "��" };
        double len = bytes;
        int order = 0;
        
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        
        return $"{len:0.##} {sizes[order]}";
    }
}