using GoogleDriveClone.SharedModels.DTOs;
using GoogleDriveClone.SharedModels.Results;

namespace GoogleDriveClone.Api.Interfaces;

public interface IFileService
{
    Task<Result<FileResponseDto>> UploadFileAsync(IFormFile file, string userId);
    Task<Result<IEnumerable<FileResponseDto>>> GetUserFilesAsync(string userId);
    Task<Result<FileResponseDto>> GetFileByIdAsync(string fileId, string userId);
    Task<Result> DeleteFileAsync(string fileId, string userId);
    Task<Result<FileDownloadResult>> DownloadFileAsync(string fileId, string userId);
    Task<Result<UserStatsDto>> GetUserStatsAsync(string userId);
    Task<Result> DeleteFilesAsync(IEnumerable<string> fileIds, string userId);
    Task<Result<FileResponseDto>> UpdateFileContentAsync(string fileId, string userId, Stream newContentStream, string newFileHash);
}