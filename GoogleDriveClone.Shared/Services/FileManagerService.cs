using GoogleDriveClone.SharedModels.DTOs;
using GoogleDriveClone.SharedModels.Results;
using System.Net.Http.Json;
using System.Text;

namespace GoogleDriveClone.Shared.Services;

public interface IFileManagerService
{
    Task<Result<List<FileResponseDto>>> GetFilesAsync();
    Task<Result<FileResponseDto>> GetFileAsync(string fileId);
    Task<Result> DeleteFileAsync(string fileId);
    Task<Result<string>> GetFileContentAsync(string fileId);
    Task<Result<FileResponseDto>> UpdateFileContentAsync(string fileId, string newContent);
    Task<Result<List<FileResponseDto>>> SearchFilesAsync(string query);
    List<FileResponseDto> FilterFiles(List<FileResponseDto> files, string filter);
    List<FileResponseDto> SortFiles(List<FileResponseDto> files, string sortBy, bool ascending = true);
    bool CanPreviewFile(string fileName);
    bool CanEditFile(string fileName);
}

public class FileManagerService : IFileManagerService
{
    private readonly HttpClient _httpClient;
    private readonly INotificationService _notificationService;

    // ����� �� ����� �����������
    private static readonly string[] PreviewableExtensions = 
    {
        ".txt", ".md", ".json", ".xml", ".csv", ".log",
        ".cs", ".js", ".html", ".css", ".py", ".c", ".cpp"
    };

    // ����� �� ����� ����������
    private static readonly string[] EditableExtensions = 
    {
        ".txt", ".md", ".json", ".xml", ".csv",
        ".cs", ".js", ".html", ".css", ".py", ".c", ".cpp"
    };

    public FileManagerService(HttpClient httpClient, INotificationService notificationService)
    {
        _httpClient = httpClient;
        _notificationService = notificationService;
    }

    public async Task<Result<List<FileResponseDto>>> GetFilesAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("api/files");
            
            if (response.IsSuccessStatusCode)
            {
                var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<List<FileResponseDto>>>();
                
                if (apiResponse != null && apiResponse.Success && apiResponse.Data != null)
                {
                    return apiResponse.Data;
                }
                else
                {
                    await _notificationService.ShowErrorAsync($"������� ��������� �����: {apiResponse?.Message ?? "������� �������"}");
                    return new List<FileResponseDto>();
                }
            }
            else
            {
                // ���������� ������� API
                try
                {
                    var errorResponse = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
                    var errorMessage = errorResponse?.Error?.Message ?? $"������� �������: {response.StatusCode}";
                    await _notificationService.ShowErrorAsync($"�� ������� ����������� �����: {errorMessage}");
                    return errorResponse?.Error ?? DomainErrors.File.NotFound;
                }
                catch
                {
                    await _notificationService.ShowErrorAsync($"������� ������������ �����: {response.StatusCode}");
                    return DomainErrors.File.NotFound;
                }
            }
        }
        catch (HttpRequestException)
        {
            await _notificationService.ShowErrorAsync("������� ����� ��� ����������� �����");
            return DomainErrors.General.UnexpectedError;
        }
        catch (TaskCanceledException)
        {
            await _notificationService.ShowErrorAsync("������������ ����� ���������");
            return DomainErrors.General.UnexpectedError;
        }
        catch (Exception)
        {
            await _notificationService.ShowErrorAsync("�� ������� ����������� �����");
            return DomainErrors.General.UnexpectedError;
        }
    }

    public async Task<Result<FileResponseDto>> GetFileAsync(string fileId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/files/{fileId}");
            
            if (response.IsSuccessStatusCode)
            {
                var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<FileResponseDto>>();
                
                if (apiResponse != null && apiResponse.Success && apiResponse.Data != null)
                {
                    return apiResponse.Data;
                }
                return DomainErrors.File.NotFound;
            }
            
            // ���������� ������� API
            try
            {
                var errorResponse = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
                return errorResponse?.Error ?? DomainErrors.File.NotFound;
            }
            catch
            {
                return DomainErrors.File.NotFound;
            }
        }
        catch (HttpRequestException)
        {
            await _notificationService.ShowErrorAsync("������� ����� ��� �������� �����");
            return DomainErrors.General.UnexpectedError;
        }
        catch (TaskCanceledException)
        {
            await _notificationService.ShowErrorAsync("��������� ����� ���������");
            return DomainErrors.General.UnexpectedError;
        }
        catch (Exception)
        {
            await _notificationService.ShowErrorAsync("�� ������� �������� ����");
            return DomainErrors.General.UnexpectedError;
        }
    }

    public async Task<Result> DeleteFileAsync(string fileId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/files/{fileId}");
            
            if (response.IsSuccessStatusCode)
            {
                var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse>();
                
                if (apiResponse != null && apiResponse.Success)
                {
                    return Result.Success();
                }
                else
                {
                    await _notificationService.ShowErrorAsync($"��������� �� �������: {apiResponse?.Message ?? "������� �������"}");
                    return DomainErrors.General.UnexpectedError;
                }
            }
            else
            {
                // ���������� ������� API
                try
                {
                    var errorResponse = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
                    var errorMessage = errorResponse?.Error?.Message ?? $"������� �������: {response.StatusCode}";
                    await _notificationService.ShowErrorAsync($"�� ������� �������� ����: {errorMessage}");
                    return errorResponse?.Error ?? DomainErrors.General.UnexpectedError;
                }
                catch
                {
                    await _notificationService.ShowErrorAsync($"������� ��������� �����: {response.StatusCode}");
                    return DomainErrors.General.UnexpectedError;
                }
            }
        }
        catch (HttpRequestException)
        {
            await _notificationService.ShowErrorAsync("������� ����� ��� �������� �����");
            return DomainErrors.General.UnexpectedError;
        }
        catch (TaskCanceledException)
        {
            await _notificationService.ShowErrorAsync("��������� ����� ���������");
            return DomainErrors.General.UnexpectedError;
        }
        catch (Exception)
        {
            await _notificationService.ShowErrorAsync("�� ������� �������� ����");
            return DomainErrors.General.UnexpectedError;
        }
    }

    public async Task<Result<string>> GetFileContentAsync(string fileId)
    {
        try
        {
            // �������� �������� ���������� ��� ����, ��� ��������� �� ����� ���� �����������
            var fileInfoResult = await GetFileAsync(fileId);
            if (!fileInfoResult.IsSuccess)
            {
                return fileInfoResult.Error!;
            }

            var fileInfo = fileInfoResult.Value!;
            if (!CanPreviewFile(fileInfo.OriginalName))
            {
                await _notificationService.ShowErrorAsync($"���� {fileInfo.OriginalName} �� ����� �����������");
                return new Error("File.NotPreviewable", "��� ��� ����� �� ������� ��������", ErrorType.Validation);
            }

            // �������� ���� ����� ����� download endpoint �� ������ �� �����
            var response = await _httpClient.GetAsync($"api/files/{fileId}/download");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return content;
            }
            else
            {
                // ���������� ������� API
                try
                {
                    var errorResponse = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
                    var errorMessage = errorResponse?.Error?.Message ?? $"������� �������: {response.StatusCode}";
                    await _notificationService.ShowErrorAsync($"�� ������� �������� ���� �����: {errorMessage}");
                    return errorResponse?.Error ?? DomainErrors.File.NotFound;
                }
                catch
                {
                    await _notificationService.ShowErrorAsync($"������� ��������� ����� �����: {response.StatusCode}");
                    return DomainErrors.File.NotFound;
                }
            }
        }
        catch (HttpRequestException)
        {
            await _notificationService.ShowErrorAsync("������� ����� ��� �������� ����� �����");
            return DomainErrors.General.UnexpectedError;
        }
        catch (TaskCanceledException)
        {
            await _notificationService.ShowErrorAsync("��������� ����� ����� ���������");
            return DomainErrors.General.UnexpectedError;
        }
        catch (Exception)
        {
            await _notificationService.ShowErrorAsync("�� ������� �������� ���� �����");
            return DomainErrors.General.UnexpectedError;
        }
    }

    public async Task<Result<FileResponseDto>> UpdateFileContentAsync(string fileId, string newContent)
    {
        try
        {
            // �������� �������� ���������� ��� ����, ��� ��������� �� ����� ���� ����������
            var fileInfoResult = await GetFileAsync(fileId);
            if (!fileInfoResult.IsSuccess)
            {
                return fileInfoResult.Error!;
            }

            var fileInfo = fileInfoResult.Value!;
            if (!CanEditFile(fileInfo.OriginalName))
            {
                await _notificationService.ShowErrorAsync($"���� {fileInfo.OriginalName} �� ����� ����������");
                return new Error("File.NotEditable", "��� ��� ����� �� ������� �����������", ErrorType.Validation);
            }

            await _notificationService.ShowInfoAsync($"��������� ����� {fileInfo.OriginalName}...");

            // ��������� ����� ��� ��������� ����� �����
            var updateRequest = new UpdateFileContentRequest { Content = newContent };
            var response = await _httpClient.PutAsJsonAsync($"api/files/{fileId}", updateRequest);
            
            if (response.IsSuccessStatusCode)
            {
                var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<FileResponseDto>>();
                
                if (apiResponse != null && apiResponse.Success && apiResponse.Data != null)
                {
                    await _notificationService.ShowSuccessAsync($"���� {fileInfo.OriginalName} ������ ��������!");
                    return apiResponse.Data;
                }
                else
                {
                    await _notificationService.ShowErrorAsync($"��������� �� �������: {apiResponse?.Message ?? "������� �������"}");
                    return DomainErrors.General.UnexpectedError;
                }
            }
            else
            {
                // ���������� ������� API
                try
                {
                    var errorResponse = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
                    var errorMessage = errorResponse?.Error?.Message ?? $"������� �������: {response.StatusCode}";
                    await _notificationService.ShowErrorAsync($"�� ������� ������� ����: {errorMessage}");
                    return errorResponse?.Error ?? DomainErrors.General.UnexpectedError;
                }
                catch
                {
                    await _notificationService.ShowErrorAsync($"������� ��������� �����: {response.StatusCode}");
                    return DomainErrors.General.UnexpectedError;
                }
            }
        }
        catch (HttpRequestException)
        {
            await _notificationService.ShowErrorAsync("������� ����� ��� �������� �����");
            return DomainErrors.General.UnexpectedError;
        }
        catch (TaskCanceledException)
        {
            await _notificationService.ShowErrorAsync("��������� ����� ���������");
            return DomainErrors.General.UnexpectedError;
        }
        catch (Exception)
        {
            await _notificationService.ShowErrorAsync("�� ������� ������� ����");
            return DomainErrors.General.UnexpectedError;
        }
    }

    public async Task<Result<List<FileResponseDto>>> SearchFilesAsync(string query)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/files/search?q={Uri.EscapeDataString(query)}");
            
            if (response.IsSuccessStatusCode)
            {
                var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<List<FileResponseDto>>>();
                
                if (apiResponse != null && apiResponse.Success && apiResponse.Data != null)
                {
                    return apiResponse.Data;
                }
                return new List<FileResponseDto>();
            }
            else
            {
                // ���������� ������� API
                try
                {
                    var errorResponse = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
                    return errorResponse?.Error ?? DomainErrors.File.NotFound;
                }
                catch
                {
                    return DomainErrors.File.NotFound;
                }
            }
        }
        catch (HttpRequestException)
        {
            await _notificationService.ShowErrorAsync("������� ����� ��� ������ �����");
            return DomainErrors.General.UnexpectedError;
        }
        catch (TaskCanceledException)
        {
            await _notificationService.ShowErrorAsync("����� ����� ���������");
            return DomainErrors.General.UnexpectedError;
        }
        catch (Exception)
        {
            await _notificationService.ShowErrorAsync("�� ������� �������� ����� �����");
            return DomainErrors.General.UnexpectedError;
        }
    }

    public List<FileResponseDto> FilterFiles(List<FileResponseDto> files, string filter)
    {
        return filter.ToLower() switch
        {
            "images" => files.Where(f => IsImageFile(f.OriginalName)).ToList(),
            "code" => files.Where(f => IsCodeFile(f.OriginalName)).ToList(),
            "documents" => files.Where(f => IsDocumentFile(f.OriginalName)).ToList(),
            "archives" => files.Where(f => IsArchiveFile(f.OriginalName)).ToList(),
            _ => files
        };
    }

    public List<FileResponseDto> SortFiles(List<FileResponseDto> files, string sortBy, bool ascending = true)
    {
        var sortedFiles = sortBy.ToLower() switch
        {
            "name" => ascending 
                ? files.OrderBy(f => f.OriginalName).ToList()
                : files.OrderByDescending(f => f.OriginalName).ToList(),
            "size" => ascending
                ? files.OrderBy(f => f.Size).ToList()
                : files.OrderByDescending(f => f.Size).ToList(),
            "modified" => ascending
                ? files.OrderBy(f => f.ModifiedAt ?? f.CreatedAt).ToList()
                : files.OrderByDescending(f => f.ModifiedAt ?? f.CreatedAt).ToList(),
            "owner" => ascending
                ? files.OrderBy(f => f.OwnerUsername).ToList()
                : files.OrderByDescending(f => f.OwnerUsername).ToList(),
            _ => ascending
                ? files.OrderBy(f => f.CreatedAt).ToList()
                : files.OrderByDescending(f => f.CreatedAt).ToList()
        };

        return sortedFiles;
    }

    public bool CanPreviewFile(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return PreviewableExtensions.Contains(extension);
    }

    public bool CanEditFile(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return EditableExtensions.Contains(extension);
    }

    // Helper methods
    private static bool IsImageFile(string fileName)
    {
        var extensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".svg" };
        return extensions.Contains(Path.GetExtension(fileName).ToLowerInvariant());
    }

    private static bool IsCodeFile(string fileName)
    {
        var extensions = new[] { ".py", ".c", ".cpp", ".cs", ".js", ".html", ".css", ".java", ".php", ".rb", ".go", ".ts" };
        return extensions.Contains(Path.GetExtension(fileName).ToLowerInvariant());
    }

    private static bool IsDocumentFile(string fileName)
    {
        var extensions = new[] { ".txt", ".md", ".pdf", ".doc", ".docx", ".rtf", ".odt" };
        return extensions.Contains(Path.GetExtension(fileName).ToLowerInvariant());
    }

    private static bool IsArchiveFile(string fileName)
    {
        var extensions = new[] { ".zip", ".rar", ".7z", ".tar", ".gz", ".bz2" };
        return extensions.Contains(Path.GetExtension(fileName).ToLowerInvariant());
    }
}