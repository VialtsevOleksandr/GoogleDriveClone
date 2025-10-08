namespace GoogleDriveClone.Api.Interfaces;

public interface IFileStorageService
{
    Task SaveFileAsync(Stream fileStream, string storedFileName, string userId);
    Task<Stream?> GetFileAsync(string storedFileName, string userId);
    Task DeleteFileAsync(string storedFileName, string userId);
    Task DeleteFilesAsync(IEnumerable<string> storedFileNames, string userId);
}