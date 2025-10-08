using GoogleDriveClone.Shared.Interfaces;
using GoogleDriveClone.SharedModels.DTOs;
using GoogleDriveClone.SharedModels.Results;
using System.Net.Http.Json;

namespace GoogleDriveClone.Shared.Services;

public class AuthApiService : IAuthApiService
{
    private readonly HttpClient _httpClient;

    public AuthApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<Result<AuthResponseDto>> LoginAsync(LoginDto loginDto)
    {
        return await HandleAuthenticationAsync("api/auth/login", loginDto);
    }

    public async Task<Result<AuthResponseDto>> RegisterAsync(RegisterDto registerDto)
    {
        return await HandleAuthenticationAsync("api/auth/register", registerDto);
    }

    private async Task<Result<AuthResponseDto>> HandleAuthenticationAsync<T>(string url, T dto)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(url, dto);

            if (!response.IsSuccessStatusCode)
            {
                var errorResponse = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
                return errorResponse?.Error ?? DomainErrors.General.UnexpectedError;
            }

            var successResponse = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponseDto>>();
            
            if (successResponse?.Data == null)
            {
                return DomainErrors.General.UnexpectedError;
            }

            return successResponse.Data;
        }
        catch (HttpRequestException)
        {
            return DomainErrors.General.UnexpectedError;
        }
        catch (TaskCanceledException)
        {
            return DomainErrors.General.UnexpectedError;
        }
    }
}