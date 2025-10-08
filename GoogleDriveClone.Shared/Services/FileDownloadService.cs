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
            await _notificationService.ShowInfoAsync($"������������ {fileName}...");

            // ����� ��������� download endpoint
            var downloadUrl = $"api/files/{fileId}/download";
            
            // ������������� JavaScript ��� ������������ �����
            await _jsRuntime.InvokeVoidAsync("downloadFileFromUrl", downloadUrl, fileName);

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
        catch (Exception)
        {
            // ����-��� ���� �������
            await _notificationService.ShowErrorAsync($"�� ������� ����������� {fileName}");
            return DomainErrors.General.UnexpectedError;
        }
    }
}