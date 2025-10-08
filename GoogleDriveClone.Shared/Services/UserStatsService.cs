using GoogleDriveClone.Shared.Interfaces;
using GoogleDriveClone.SharedModels.DTOs;
using GoogleDriveClone.SharedModels.Results;
using System.Net.Http.Json;

namespace GoogleDriveClone.Shared.Services;

public class UserStatsService : IUserStatsService
{
    private readonly HttpClient _httpClient;
    private readonly INotificationService _notificationService;

    public UserStatsService(HttpClient httpClient, INotificationService notificationService)
    {
        _httpClient = httpClient;
        _notificationService = notificationService;
    }

    public async Task<Result<UserStatsDto>> GetUserStatsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/files/stats");
            
            if (response.IsSuccessStatusCode)
            {
                var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<UserStatsDto>>();
                
                if (apiResponse != null && apiResponse.Success && apiResponse.Data != null)
                {
                    return apiResponse.Data;
                }
                else
                {
                    await _notificationService.ShowErrorAsync($"Помилка отримання статистики: {apiResponse?.Message ?? "Невідома помилка"}");
                    return DomainErrors.General.UnexpectedError;
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                await _notificationService.ShowErrorAsync($"Помилка API {response.StatusCode}: {errorContent}");
                return DomainErrors.General.UnexpectedError;
            }
        }
        catch (Exception ex)
        {
            await _notificationService.ShowErrorAsync($"Помилка підключення: {ex.Message}");
            return DomainErrors.General.UnexpectedError;
        }
    }
}