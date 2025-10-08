using GoogleDriveClone.SharedModels.DTOs;
using GoogleDriveClone.SharedModels.Results;

namespace GoogleDriveClone.Shared.Interfaces;

public interface IUserStatsService
{
    Task<Result<UserStatsDto>> GetUserStatsAsync();
}