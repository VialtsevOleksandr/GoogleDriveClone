using GoogleDriveClone.SharedModels.DTOs;
using GoogleDriveClone.SharedModels.Results;

namespace GoogleDriveClone.Shared.Interfaces;

/// <summary>
/// ����� ��� ��������� ������ �������������� � Blazor �������
/// ³� �������� ������ �� API ��������� �� ���������� ����� ��������������
/// </summary>
public interface IAuthenticationService
{
    Task<Result> LoginAsync(LoginDto loginDto);
    Task<Result> RegisterAsync(RegisterDto registerDto);
    Task LogoutAsync();
    Task<bool> IsAuthenticatedAsync();
    Task<string?> GetTokenAsync();
}