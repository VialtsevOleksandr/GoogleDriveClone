using GoogleDriveClone.SharedModels.DTOs;
using GoogleDriveClone.SharedModels.Results;

namespace GoogleDriveClone.Shared.Interfaces;

/// <summary>
/// Сервіс для управління станом автентифікації в Blazor додатку
/// Він координує роботу між API викликами та оновленням стану автентифікації
/// </summary>
public interface IAuthenticationService
{
    Task<Result> LoginAsync(LoginDto loginDto);
    Task<Result> RegisterAsync(RegisterDto registerDto);
    Task LogoutAsync();
    Task<bool> IsAuthenticatedAsync();
    Task<string?> GetTokenAsync();
}