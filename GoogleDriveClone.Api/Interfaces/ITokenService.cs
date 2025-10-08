using GoogleDriveClone.Api.Entities;

namespace GoogleDriveClone.Api.Interfaces;

public interface ITokenService
{
    string GenerateJwtToken(User user);
}