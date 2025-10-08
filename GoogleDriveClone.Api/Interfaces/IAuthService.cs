using GoogleDriveClone.SharedModels.Results;
using GoogleDriveClone.SharedModels.DTOs;

namespace GoogleDriveClone.Api.Interfaces;

public interface IAuthService
{
    Task<Result<AuthResponseDto>> RegisterAsync(RegisterDto registerDto);
    Task<Result<AuthResponseDto>> LoginAsync(LoginDto loginDto);
}