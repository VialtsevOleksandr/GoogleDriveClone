using GoogleDriveClone.Shared.Auth;
using GoogleDriveClone.Shared.Interfaces;
using GoogleDriveClone.SharedModels.DTOs;
using GoogleDriveClone.SharedModels.Results;

namespace GoogleDriveClone.Shared.Services;

/// <summary>
/// Менеджер стану автентифікації для Blazor додатку
/// Координує роботу між IAuthApiService та CustomAuthenticationStateProvider
/// </summary>
public class AuthenticationService : IAuthenticationService
{
    private readonly IAuthApiService _authApiService;
    private readonly CustomAuthenticationStateProvider _authStateProvider;

    public AuthenticationService(
        IAuthApiService authApiService,
        CustomAuthenticationStateProvider authStateProvider)
    {
        _authApiService = authApiService;
        _authStateProvider = authStateProvider;
    }

    public async Task<Result> LoginAsync(LoginDto loginDto)
    {
        // 1. Викликаємо API через IAuthApiService
        var apiResult = await _authApiService.LoginAsync(loginDto);
        
        if (!apiResult.IsSuccess)
        {
            return Result.Failure(apiResult.Error!);
        }

        // 2. Якщо успішно - оновлюємо стан автентифікації
        await _authStateProvider.MarkUserAsAuthenticated(apiResult.Value!.Token);
        
        return Result.Success();
    }

    public async Task<Result> RegisterAsync(RegisterDto registerDto)
    {
        // 1. Викликаємо API через IAuthApiService
        var apiResult = await _authApiService.RegisterAsync(registerDto);
        
        if (!apiResult.IsSuccess)
        {
            return Result.Failure(apiResult.Error!);
        }

        // 2. Якщо успішно - автоматично логінимо користувача
        await _authStateProvider.MarkUserAsAuthenticated(apiResult.Value!.Token);
        
        return Result.Success();
    }

    public async Task LogoutAsync()
    {
        // Очищаємо стан автентифікації локально
        await _authStateProvider.MarkUserAsLoggedOut();
    }

    // Делегуємо до AuthStateProvider замість дублювання
    public async Task<bool> IsAuthenticatedAsync()
    {
        return await _authStateProvider.IsUserAuthenticatedAsync();
    }

    // Делегуємо до AuthStateProvider замість дублювання
    public async Task<string?> GetTokenAsync()
    {
        return await _authStateProvider.GetTokenAsync();
    }
}