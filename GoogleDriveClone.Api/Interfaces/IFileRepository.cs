using GoogleDriveClone.Api.Entities;
using GoogleDriveClone.SharedModels.Results;

namespace GoogleDriveClone.Api.Interfaces;

public interface IFileRepository
{
    Result Create(FileMetadata file);
    Task<Result<IEnumerable<FileMetadata>>> GetByUserIdAsync(string userId);
    Task<Result<FileMetadata>> GetByIdAsync(string id);
    Task<Result<FileMetadata>> GetOwnedFileByIdAsync(string id, string userId);
    Result Delete(FileMetadata file);
    Task<Result> DeleteFilesAsync(IEnumerable<FileMetadata> files);
    Result Update(FileMetadata file);
}