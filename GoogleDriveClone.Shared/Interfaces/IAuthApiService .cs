using GoogleDriveClone.SharedModels.DTOs;
using GoogleDriveClone.SharedModels.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleDriveClone.Shared.Interfaces;

public interface IAuthApiService
{
    Task<Result<AuthResponseDto>> LoginAsync(LoginDto loginDto);
    Task<Result<AuthResponseDto>> RegisterAsync(RegisterDto registerDto);
}
